using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace PingMonitor
{
    public class SettingsForm : Form
    {
        public AppSettings Settings { get; private set; }

        // --- UI ---
        private CheckBox chkLossEnable;
        private DarkComboBox cmbLossSound;
        private DarkTrackBar trackLossVol;

        private CheckBox chkPingEnable;
        private DarkComboBox cmbPingSound;
        private DarkTrackBar trackPingVol;
        private DarkNumeric numPingThreshold; // <--- Используем наш класс

        private DarkComboBox cmbTheme;
        private DarkNumeric numPingInterval;
        private DarkNumeric numGraphWindow;
        private DarkNumeric numStatsWindow;
        private ListBox lstTemplates;
        private TextBox txtNewTemplate;
        private Button btnAddTemplate;
        private Button btnDelTemplate;

        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }

        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14; const int HTBOTTOM = 15; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            {
                int resizeArea = 10; Point p = PointToClient(new Point(m.LParam.ToInt32()));
                if (p.Y <= resizeArea) { if (p.X <= resizeArea) m.Result = (IntPtr)HTTOPLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTTOPRIGHT; else m.Result = (IntPtr)HTTOP; }
                else if (p.Y >= Height - resizeArea) { if (p.X <= resizeArea) m.Result = (IntPtr)HTBOTTOMLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTBOTTOMRIGHT; else m.Result = (IntPtr)HTBOTTOM; }
                else if (p.X <= resizeArea) m.Result = (IntPtr)HTLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTRIGHT;
            }
        }

        public SettingsForm(AppSettings currentSettings)
        {
            Settings = currentSettings;
            if (Settings.IpTemplates == null) Settings.IpTemplates = new List<string>();
            SetupCustomUI();
            LoadValues();
        }

        private void SetupCustomUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(520, 620);
            this.AutoScroll = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgWindow;
            this.Padding = new Padding(1);
            this.DoubleBuffered = true;

            // Header (с кнопкой закрытия)
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.BgHeader };
            Label lblTitle = new Label { Text = "Настройки", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Theme.Text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            lblTitle.MouseDown += DragWindow;
            pnlHeader.Controls.Add(lblTitle);

            Label btnCloseSettings = new Label { Text = "✕", Font = new Font("Arial", 11, FontStyle.Regular), ForeColor = Theme.Icon, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(this.Width - 34, 12) };
            btnCloseSettings.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            btnCloseSettings.MouseEnter += (s, e) => btnCloseSettings.ForeColor = Color.Red;
            btnCloseSettings.MouseLeave += (s, e) => btnCloseSettings.ForeColor = Theme.Icon;
            pnlHeader.Controls.Add(btnCloseSettings);
            this.Controls.Add(pnlHeader);

            // Buttons (белый текст, hover-эффекты)
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgHeader };
            Button btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, BackColor = Theme.Accent, FlatStyle = FlatStyle.Flat, ForeColor = Theme.AccentText, Size = new Size(120, 35), Cursor = Cursors.Hand };
            btnSave.Location = new Point(pnlBottom.Width - 260, 13); btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; btnSave.FlatAppearance.BorderSize = 0;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Theme.AccentHover;
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = Theme.Accent;

            Button btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, BackColor = Theme.BgInput, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Text, Size = new Size(120, 35), Cursor = Cursors.Hand };
            btnCancel.Location = new Point(pnlBottom.Width - 130, 13); btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Theme.BgHover;
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Theme.BgInput;
            pnlBottom.Controls.Add(btnSave); pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);

            // Тема (полоса под заголовком)
            Panel pnlTheme = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgHeader };
            Label lblThemeHint = new Label { Text = "Тема оформления:", ForeColor = Theme.TextDim, Location = new Point(20, 13), AutoSize = true };
            pnlTheme.Controls.Add(lblThemeHint);

            cmbTheme = new DarkComboBox { Location = new Point(150, 8), Width = 150 };
            cmbTheme.Items.Add("Тёмная");
            cmbTheme.Items.Add("Светлая");
            pnlTheme.Controls.Add(cmbTheme);
            this.Controls.Add(pnlTheme);

            // Tabs
            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(130, 35);
            tabControl.Padding = new Point(0, 0);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;

            TabPage tabAlerts = new TabPage("Оповещения") { BackColor = Theme.BgWindow, AutoScroll = true };
            TabPage tabTemplates = new TabPage("Шаблоны IP") { BackColor = Theme.BgWindow, AutoScroll = true };
            TabPage tabIntervals = new TabPage("Интервалы") { BackColor = Theme.BgWindow, AutoScroll = true };

            tabControl.TabPages.Add(tabAlerts);
            tabControl.TabPages.Add(tabTemplates);
            tabControl.TabPages.Add(tabIntervals);
            this.Controls.Add(tabControl);
            tabControl.BringToFront();

            // === Tab 1 ===
            DarkGroupBox grpLoss = CreateGroup("🔴 При потере связи", 10, tabAlerts);
            grpLoss.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; grpLoss.Width = tabAlerts.Width - 20;

            chkLossEnable = new CheckBox { Text = "Включить звук", ForeColor = Theme.Text, Location = new Point(15, 30), AutoSize = true };
            grpLoss.Controls.Add(chkLossEnable);

            grpLoss.Controls.Add(new Label { Text = "Звук:", ForeColor = Theme.TextDim, Location = new Point(15, 60), AutoSize = true });

            cmbLossSound = CreateSoundCombo(15, 80);
            cmbLossSound.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbLossSound.Width = grpLoss.Width - 120;
            grpLoss.Controls.Add(cmbLossSound);

            Button btnTest1 = CreateTestButton(cmbLossSound, () => trackLossVol.Value);
            btnTest1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTest1.Location = new Point(grpLoss.Width - 90, 79);
            grpLoss.Controls.Add(btnTest1);

            grpLoss.Controls.Add(new Label { Text = "Громкость:", ForeColor = Theme.TextDim, Location = new Point(15, 115), AutoSize = true });

            trackLossVol = new DarkTrackBar { Location = new Point(15, 135), Height = 30 };
            trackLossVol.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackLossVol.Width = grpLoss.Width - 30;
            grpLoss.Controls.Add(trackLossVol);


            DarkGroupBox grpPing = CreateGroup("🟡 При высоком пинге", 210, tabAlerts);
            grpPing.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; grpPing.Width = tabAlerts.Width - 20;

            chkPingEnable = new CheckBox { Text = "Включить звук", ForeColor = Theme.Text, Location = new Point(15, 30), AutoSize = true };
            grpPing.Controls.Add(chkPingEnable);

            grpPing.Controls.Add(new Label { Text = "Порог (мс):", ForeColor = Theme.TextDim, Location = new Point(150, 31), AutoSize = true });

            // Используем DarkNumeric
            numPingThreshold = new DarkNumeric { Location = new Point(240, 29), Width = 70, Minimum = 10, Maximum = 5000 };
            grpPing.Controls.Add(numPingThreshold);

            grpPing.Controls.Add(new Label { Text = "Звук:", ForeColor = Theme.TextDim, Location = new Point(15, 60), AutoSize = true });

            cmbPingSound = CreateSoundCombo(15, 80);
            cmbPingSound.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbPingSound.Width = grpPing.Width - 120;
            grpPing.Controls.Add(cmbPingSound);

            Button btnTest2 = CreateTestButton(cmbPingSound, () => trackPingVol.Value);
            btnTest2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTest2.Location = new Point(grpPing.Width - 90, 79);
            grpPing.Controls.Add(btnTest2);

            grpPing.Controls.Add(new Label { Text = "Громкость:", ForeColor = Theme.TextDim, Location = new Point(15, 115), AutoSize = true });

            trackPingVol = new DarkTrackBar { Location = new Point(15, 135), Height = 30 };
            trackPingVol.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackPingVol.Width = grpPing.Width - 30;
            grpPing.Controls.Add(trackPingVol);


            // === Tab 2 ===
            Label lblHint = new Label { Text = "Создайте маски. Символ '*' заменяет курсор.\nПримеры: 192.168.1.*  или  *.google.com", ForeColor = Theme.TextDim, AutoSize = true, Location = new Point(20, 20) };
            tabTemplates.Controls.Add(lblHint);

            Label lblNew = new Label { Text = "Новый шаблон:", ForeColor = Theme.Text, AutoSize = true, Location = new Point(20, 70) };
            tabTemplates.Controls.Add(lblNew);

            btnAddTemplate = new Button { Text = "Добавить", Width = 100, Height = 27, BackColor = Theme.Accent, FlatStyle = FlatStyle.Flat, ForeColor = Theme.AccentText, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnAddTemplate.Location = new Point(tabTemplates.Width - 130, 94);
            btnAddTemplate.Click += BtnAddTemplate_Click;
            tabTemplates.Controls.Add(btnAddTemplate);

            txtNewTemplate = new TextBox { Location = new Point(20, 95), Font = new Font("Segoe UI", 10), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Theme.BgInput, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle };
            txtNewTemplate.Width = tabTemplates.Width - 160;
            tabTemplates.Controls.Add(txtNewTemplate);

            lstTemplates = new ListBox { Location = new Point(20, 140), Font = new Font("Segoe UI", 10), BackColor = Theme.BgTile, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            lstTemplates.Width = tabTemplates.Width - 40;
            lstTemplates.Height = tabTemplates.Height - 190;
            tabTemplates.Controls.Add(lstTemplates);

            btnDelTemplate = new Button { Text = "Удалить выбранный", Height = 35, BackColor = Theme.BgInput, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Text, Cursor = Cursors.Hand, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            btnDelTemplate.Location = new Point(20, tabTemplates.Height - 55);
            btnDelTemplate.Width = tabTemplates.Width - 40;
            btnDelTemplate.Click += (s, e) => {
                if (lstTemplates.SelectedIndex != -1) lstTemplates.Items.RemoveAt(lstTemplates.SelectedIndex);
            };
            tabTemplates.Controls.Add(btnDelTemplate);

            // === Tab 3: Интервалы ===
            DarkGroupBox grpPingInterval = CreateGroup("⏱ Период пинга", 10, tabIntervals);
            grpPingInterval.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpPingInterval.Width = tabIntervals.Width - 20;
            grpPingInterval.Controls.Add(new Label { Text = "Как часто пинговать узел", ForeColor = Theme.TextDim, Location = new Point(15, 30), AutoSize = true });
            numPingInterval = new DarkNumeric { Location = new Point(15, 55), Width = 100, Minimum = 200, Maximum = 60000, Increment = 100 };
            grpPingInterval.Controls.Add(numPingInterval);
            grpPingInterval.Controls.Add(new Label { Text = "мс", ForeColor = Theme.TextDim, Location = new Point(120, 58), AutoSize = true });

            DarkGroupBox grpGraph = CreateGroup("📊 Окно графика", 220, tabIntervals);
            grpGraph.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpGraph.Width = tabIntervals.Width - 20;
            grpGraph.Controls.Add(new Label { Text = "Сколько секунд истории показывать на графике", ForeColor = Theme.TextDim, Location = new Point(15, 30), AutoSize = true });
            numGraphWindow = new DarkNumeric { Location = new Point(15, 55), Width = 100, Minimum = 10, Maximum = 600, Increment = 10 };
            grpGraph.Controls.Add(numGraphWindow);
            grpGraph.Controls.Add(new Label { Text = "сек", ForeColor = Theme.TextDim, Location = new Point(120, 58), AutoSize = true });

            DarkGroupBox grpStats = CreateGroup("📈 Окно статистики", 430, tabIntervals);
            grpStats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpStats.Width = tabIntervals.Width - 20;
            grpStats.Controls.Add(new Label { Text = "Период для расчёта Loss% и отчётов", ForeColor = Theme.TextDim, Location = new Point(15, 30), AutoSize = true });
            numStatsWindow = new DarkNumeric { Location = new Point(15, 55), Width = 100, Minimum = 30, Maximum = 3600, Increment = 30 };
            grpStats.Controls.Add(numStatsWindow);
            grpStats.Controls.Add(new Label { Text = "сек", ForeColor = Theme.TextDim, Location = new Point(120, 58), AutoSize = true });
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl; if (e.Index >= tc.TabPages.Count) return;
            TabPage page = tc.TabPages[e.Index]; Rectangle rect = e.Bounds;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (Brush backBrush = new SolidBrush(selected ? Theme.BgTabSel : Theme.BgHeader))
                e.Graphics.FillRectangle(backBrush, rect);
            // Нижняя полоска-акцент у активной вкладки
            if (selected)
                using (SolidBrush accent = new SolidBrush(Theme.Accent))
                    e.Graphics.FillRectangle(accent, rect.X, rect.Bottom - 2, rect.Width, 2);
            Color textColor = selected ? Theme.Text : Theme.TextDim;
            TextRenderer.DrawText(e.Graphics, page.Text, new Font("Segoe UI", 9, selected ? FontStyle.Bold : FontStyle.Regular), rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnAddTemplate_Click(object sender, EventArgs e)
        {
            string tmpl = txtNewTemplate.Text.Trim();
            if (string.IsNullOrWhiteSpace(tmpl)) return;
            if (!tmpl.Contains("*")) { MessageBox.Show("Шаблон должен содержать '*'"); return; }
            if (!lstTemplates.Items.Contains(tmpl)) lstTemplates.Items.Add(tmpl);
            txtNewTemplate.Clear();
        }

        private DarkGroupBox CreateGroup(string text, int y, Control parent)
        {
            DarkGroupBox g = new DarkGroupBox { Text = text, Location = new Point(10, y), Size = new Size(390, 190), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            parent.Controls.Add(g); return g;
        }
        private DarkComboBox CreateSoundCombo(int x, int y)
        {
            DarkComboBox cb = new DarkComboBox { Location = new Point(x, y), Width = 200 };
            if (Directory.Exists(@"C:\Windows\Media")) cb.Items.AddRange(Directory.GetFiles(@"C:\Windows\Media", "*.wav").Select(Path.GetFileName).ToArray());
            return cb;
        }

        // КНОПКА PLAY: Увеличили высоту с 23 до 27
        private Button CreateTestButton(DarkComboBox cb, Func<int> getVol)
        {
            Button b = new Button { Text = "Play", Width = 50, Height = 27, BackColor = Theme.BgInput, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Text };
            b.Click += (s, e) => AudioManager.PlaySound(Path.Combine(@"C:\Windows\Media", cb.SelectedItem?.ToString() ?? ""), getVol());
            return b;
        }

        private void LoadValues()
        {
            chkLossEnable.Checked = Settings.LossAlertEnabled;
            trackLossVol.Value = Settings.LossVolume;
            SetComboValue(cmbLossSound, Settings.LossSoundFile);
            chkPingEnable.Checked = Settings.HighPingAlertEnabled;
            trackPingVol.Value = Settings.HighPingVolume;
            numPingThreshold.Value = Settings.HighPingThreshold;
            SetComboValue(cmbPingSound, Settings.HighPingSoundFile);
            cmbTheme.SelectedIndex = Settings.IsDarkTheme ? 0 : 1;
            numPingInterval.Value = Settings.PingIntervalMs;
            numGraphWindow.Value = Settings.GraphWindowSec;
            numStatsWindow.Value = Settings.StatsWindowSec;
            lstTemplates.Items.Clear();
            foreach (var t in Settings.IpTemplates) lstTemplates.Items.Add(t);
        }

        private void SetComboValue(DarkComboBox cb, string fullPath)
        {
            string fileName = Path.GetFileName(fullPath);
            if (cb.Items.Contains(fileName)) cb.SelectedItem = fileName;
            else if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        public void ApplySettings()
        {
            Settings.LossAlertEnabled = chkLossEnable.Checked;
            Settings.LossSoundFile = Path.Combine(@"C:\Windows\Media", cmbLossSound.SelectedItem?.ToString() ?? "");
            Settings.LossVolume = trackLossVol.Value;
            Settings.HighPingAlertEnabled = chkPingEnable.Checked;
            Settings.HighPingSoundFile = Path.Combine(@"C:\Windows\Media", cmbPingSound.SelectedItem?.ToString() ?? "");
            Settings.HighPingVolume = trackPingVol.Value;
            Settings.HighPingThreshold = (int)numPingThreshold.Value;
            Settings.IsDarkTheme = cmbTheme.SelectedIndex == 0;
            Settings.PingIntervalMs = (int)numPingInterval.Value;
            Settings.GraphWindowSec = (int)numGraphWindow.Value;
            Settings.StatsWindowSec = (int)numStatsWindow.Value;
            Settings.IpTemplates.Clear();
            foreach (var item in lstTemplates.Items) Settings.IpTemplates.Add(item.ToString());
            AppSettings.Save(Settings);
        }
    }
}