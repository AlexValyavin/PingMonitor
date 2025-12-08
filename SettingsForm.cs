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

        // UI Controls
        private CheckBox chkLossEnable;
        private ComboBox cmbLossSound;
        private TrackBar trackLossVol;
        private CheckBox chkPingEnable;
        private ComboBox cmbPingSound;
        private TrackBar trackPingVol;
        private NumericUpDown numPingThreshold;

        private ListBox lstTemplates;
        private TextBox txtNewTemplate;
        private Button btnAddTemplate;
        private Button btnDelTemplate;

        // --- WINAPI ---
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // Тень
                return cp;
            }
        }

        // Ресайз
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
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(1); // Тонкая рамка
            this.DoubleBuffered = true;

            // 1. HEADER (Dock = Top)
            // Добавляем первым, чтобы он прилип к самому верху
            Label lblTitle = new Label
            {
                Text = "Настройки",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitle.MouseDown += DragWindow;
            this.Controls.Add(lblTitle);

            // 2. BOTTOM BUTTONS (Dock = Bottom)
            // Добавляем вторым, прилипнет к низу
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Transparent };

            Button btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, BackColor = Color.FromArgb(46, 204, 113), FlatStyle = FlatStyle.Flat, ForeColor = Color.Black, Size = new Size(120, 35), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSave.Location = new Point(pnlBottom.Width - 260, 15);
            btnSave.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Size = new Size(120, 35), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCancel.Location = new Point(pnlBottom.Width - 130, 15);
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlBottom.Controls.Add(btnSave); pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);

            // 3. TAB CONTROL (Dock = Fill)
            // Займет всё оставшееся место между Header и Bottom
            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(130, 35);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;

            TabPage tabAlerts = new TabPage("Оповещения") { BackColor = Color.FromArgb(30, 30, 30) };
            TabPage tabTemplates = new TabPage("Шаблоны IP") { BackColor = Color.FromArgb(30, 30, 30) };

            tabControl.TabPages.Add(tabAlerts);
            tabControl.TabPages.Add(tabTemplates);
            this.Controls.Add(tabControl);
            // Важно: перенести на передний план, чтобы корректно занял место
            tabControl.BringToFront();

            // === Вкладка 1: Оповещения ===
            // (Тут используем GroupBox с Anchor, так как контент фиксированный)
            GroupBox grpLoss = CreateGroup("🔴 При потере связи", 10, tabAlerts);
            grpLoss.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; grpLoss.Width = tabAlerts.Width - 20;
            chkLossEnable = new CheckBox { Text = "Включить звук", ForeColor = Color.White, Location = new Point(15, 30), AutoSize = true }; grpLoss.Controls.Add(chkLossEnable);
            grpLoss.Controls.Add(new Label { Text = "Звук:", ForeColor = Color.Gray, Location = new Point(15, 60), AutoSize = true });
            cmbLossSound = CreateSoundCombo(15, 80); cmbLossSound.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cmbLossSound.Width = grpLoss.Width - 100; grpLoss.Controls.Add(cmbLossSound);
            Button btnTest1 = CreateTestButton(cmbLossSound, () => trackLossVol.Value); btnTest1.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnTest1.Location = new Point(grpLoss.Width - 70, 79); grpLoss.Controls.Add(btnTest1);
            grpLoss.Controls.Add(new Label { Text = "Громкость:", ForeColor = Color.Gray, Location = new Point(15, 110), AutoSize = true });
            trackLossVol = CreateTrackBar(15, 130); trackLossVol.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; trackLossVol.Width = grpLoss.Width - 30; grpLoss.Controls.Add(trackLossVol);

            GroupBox grpPing = CreateGroup("🟡 При высоком пинге", 200, tabAlerts);
            grpPing.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; grpPing.Width = tabAlerts.Width - 20;
            chkPingEnable = new CheckBox { Text = "Включить звук", ForeColor = Color.White, Location = new Point(15, 30), AutoSize = true }; grpPing.Controls.Add(chkPingEnable);
            grpPing.Controls.Add(new Label { Text = "Порог (мс):", ForeColor = Color.Gray, Location = new Point(150, 31), AutoSize = true });
            numPingThreshold = new NumericUpDown { Location = new Point(230, 29), Width = 60, Minimum = 10, Maximum = 5000 }; grpPing.Controls.Add(numPingThreshold);
            grpPing.Controls.Add(new Label { Text = "Звук:", ForeColor = Color.Gray, Location = new Point(15, 60), AutoSize = true });
            cmbPingSound = CreateSoundCombo(15, 80); cmbPingSound.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; cmbPingSound.Width = grpPing.Width - 100; grpPing.Controls.Add(cmbPingSound);
            Button btnTest2 = CreateTestButton(cmbPingSound, () => trackPingVol.Value); btnTest2.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnTest2.Location = new Point(grpPing.Width - 70, 79); grpPing.Controls.Add(btnTest2);
            grpPing.Controls.Add(new Label { Text = "Громкость:", ForeColor = Color.Gray, Location = new Point(15, 110), AutoSize = true });
            trackPingVol = CreateTrackBar(15, 130); trackPingVol.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; trackPingVol.Width = grpPing.Width - 30; grpPing.Controls.Add(trackPingVol);

            // === Вкладка 2: Шаблоны (РЕЗИНОВАЯ ВЕРСТКА) ===

            // 1. Панель ввода (Сверху)
            Panel pnlTemplatesTop = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent, Padding = new Padding(10) };
            tabTemplates.Controls.Add(pnlTemplatesTop);

            Label lblHint = new Label { Text = "Создайте маски. '*' заменяет курсор.\nПримеры: 192.168.1.*", ForeColor = Color.Gray, AutoSize = true, Dock = DockStyle.Top };
            pnlTemplatesTop.Controls.Add(lblHint);

            // Контейнер для строки ввода
            Panel pnlInputRow = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            pnlTemplatesTop.Controls.Add(pnlInputRow);

            btnAddTemplate = new Button { Text = "Добавить", Width = 100, Dock = DockStyle.Right, BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnAddTemplate.FlatAppearance.BorderSize = 0;
            btnAddTemplate.Click += BtnAddTemplate_Click;
            pnlInputRow.Controls.Add(btnAddTemplate);

            txtNewTemplate = new TextBox { Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill };
            pnlInputRow.Controls.Add(txtNewTemplate);
            // Хак, чтобы TextBox не прилипал к кнопке (добавляем Panel-спейсер или просто Margin)
            txtNewTemplate.BringToFront(); // Чтобы Dock.Fill работал корректно с Dock.Right

            // 2. Кнопка удаления (Снизу)
            btnDelTemplate = new Button { Text = "Удалить выбранный", Height = 40, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnDelTemplate.FlatAppearance.BorderSize = 0;
            btnDelTemplate.Click += (s, e) => { if (lstTemplates.SelectedIndex >= 0) lstTemplates.Items.RemoveAt(lstTemplates.SelectedIndex); };
            tabTemplates.Controls.Add(btnDelTemplate);

            // 3. Список (Заполняет всё остальное)
            lstTemplates = new ListBox { Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill };
            tabTemplates.Controls.Add(lstTemplates);
            lstTemplates.BringToFront(); // Важно! Чтобы он занял центр между Top и Bottom
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (e.Index >= tc.TabPages.Count) return;

            TabPage page = tc.TabPages[e.Index]; Rectangle rect = e.Bounds;
            using (Brush backBrush = (e.State == DrawItemState.Selected) ? new SolidBrush(Color.FromArgb(50, 50, 50)) : new SolidBrush(Color.FromArgb(30, 30, 30))) { e.Graphics.FillRectangle(backBrush, rect); }
            Color textColor = (e.State == DrawItemState.Selected) ? Color.White : Color.Gray;
            TextRenderer.DrawText(e.Graphics, page.Text, new Font("Segoe UI", 9), rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnAddTemplate_Click(object sender, EventArgs e)
        {
            string tmpl = txtNewTemplate.Text.Trim();
            if (string.IsNullOrWhiteSpace(tmpl)) return;
            if (!tmpl.Contains("*")) { MessageBox.Show("Шаблон должен содержать '*'"); return; }
            if (!lstTemplates.Items.Contains(tmpl)) lstTemplates.Items.Add(tmpl);
            txtNewTemplate.Clear();
        }

        private GroupBox CreateGroup(string text, int y, Control parent)
        {
            GroupBox g = new GroupBox { Text = text, Location = new Point(10, y), Size = new Size(390, 180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            parent.Controls.Add(g); return g;
        }
        private ComboBox CreateSoundCombo(int x, int y)
        {
            ComboBox cb = new ComboBox { Location = new Point(x, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            if (Directory.Exists(@"C:\Windows\Media")) cb.Items.AddRange(Directory.GetFiles(@"C:\Windows\Media", "*.wav").Select(Path.GetFileName).ToArray());
            return cb;
        }
        private TrackBar CreateTrackBar(int x, int y) => new TrackBar { Location = new Point(x, y), Width = 300, Maximum = 100, TickFrequency = 10, SmallChange = 5 };

        private Button CreateTestButton(ComboBox cb, Func<int> getVol)
        {
            Button b = new Button { Text = "Play", Width = 50, Height = 23, BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
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
            lstTemplates.Items.Clear();
            foreach (var t in Settings.IpTemplates) lstTemplates.Items.Add(t);
        }

        private void SetComboValue(ComboBox cb, string fullPath)
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
            Settings.IpTemplates.Clear();
            foreach (var item in lstTemplates.Items) Settings.IpTemplates.Add(item.ToString());
            AppSettings.Save(Settings);
        }
    }
}