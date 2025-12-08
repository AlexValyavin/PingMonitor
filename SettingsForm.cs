using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;

namespace PingMonitor
{
    public class SettingsForm : Form
    {
        public AppSettings Settings { get; private set; }

        // Элементы управления
        private CheckBox chkLossEnable;
        private ComboBox cmbLossSound;
        private TrackBar trackLossVol;

        private CheckBox chkPingEnable;
        private ComboBox cmbPingSound;
        private TrackBar trackPingVol;
        private NumericUpDown numPingThreshold;

        // Перетаскивание окна
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        public SettingsForm(AppSettings currentSettings)
        {
            Settings = currentSettings;
            InitializeComponent();
            LoadValues();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(400, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(2); // Внешняя рамка

            // --- ЗАГОЛОВОК ---
            Label lblTitle = new Label
            {
                Text = "Настройки оповещений",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitle.MouseDown += DragWindow;
            this.Controls.Add(lblTitle);

            // --- ПАНЕЛЬ КНОПОК (СНИЗУ) ---
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Transparent };

            Button btnSave = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(46, 204, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Black,
                Size = new Size(120, 35),
                Location = new Point(60, 10),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Size = new Size(120, 35),
                Location = new Point(220, 10),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);

            // --- ПАНЕЛЬ КОНТЕНТА ---
            Panel pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            this.Controls.Add(pnlContent);

            // === ГРУППА 1: LOSS (Потеря) ===
            // Сдвигаем группу ниже (Y=20)
            GroupBox grpLoss = CreateGroup("🔴 При потере связи (Loss)", 60, pnlContent);

            // Чекбокс
            chkLossEnable = new CheckBox { Text = "Включить звук", ForeColor = Color.White, Location = new Point(15, 30), AutoSize = true };
            grpLoss.Controls.Add(chkLossEnable);

            // Звук
            Label lblSnd1 = new Label { Text = "Звук:", ForeColor = Color.Gray, Location = new Point(15, 60), AutoSize = true };
            grpLoss.Controls.Add(lblSnd1);

            cmbLossSound = CreateSoundCombo(15, 80);
            grpLoss.Controls.Add(cmbLossSound);
            grpLoss.Controls.Add(CreateTestButton(230, 79, cmbLossSound, () => trackLossVol.Value));

            // Громкость
            Label lblVol1 = new Label { Text = "Громкость:", ForeColor = Color.Gray, Location = new Point(15, 110), AutoSize = true };
            grpLoss.Controls.Add(lblVol1);
            trackLossVol = CreateTrackBar(15, 130);
            grpLoss.Controls.Add(trackLossVol);


            // === ГРУППА 2: HIGH PING ===
            // Сдвигаем вторую группу еще ниже (Y=210)
            GroupBox grpPing = CreateGroup("🟡 При высоком пинге", 250, pnlContent);

            // Чекбокс
            chkPingEnable = new CheckBox { Text = "Включить звук", ForeColor = Color.White, Location = new Point(15, 30), AutoSize = true };
            grpPing.Controls.Add(chkPingEnable);

            // Порог
            Label lblThres = new Label { Text = "Порог (мс):", ForeColor = Color.Gray, Location = new Point(150, 31), AutoSize = true };
            grpPing.Controls.Add(lblThres);
            numPingThreshold = new NumericUpDown { Location = new Point(230, 29), Width = 60, Minimum = 10, Maximum = 5000, Value = 200 };
            grpPing.Controls.Add(numPingThreshold);

            // Звук
            Label lblSnd2 = new Label { Text = "Звук:", ForeColor = Color.Gray, Location = new Point(15, 60), AutoSize = true };
            grpPing.Controls.Add(lblSnd2);

            cmbPingSound = CreateSoundCombo(15, 80);
            grpPing.Controls.Add(cmbPingSound);
            grpPing.Controls.Add(CreateTestButton(230, 79, cmbPingSound, () => trackPingVol.Value));

            // Громкость
            Label lblVol2 = new Label { Text = "Громкость:", ForeColor = Color.Gray, Location = new Point(15, 110), AutoSize = true };
            grpPing.Controls.Add(lblVol2);
            trackPingVol = CreateTrackBar(15, 130);
            grpPing.Controls.Add(trackPingVol);


            // Рамка окна
            this.Paint += (s, e) => { e.Graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1); };
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
        private GroupBox CreateGroup(string text, int y, Panel parent)
        {
            GroupBox g = new GroupBox
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(360, 180), // Увеличили высоту группы
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat // Плоский стиль для лучшей отрисовки
            };
            parent.Controls.Add(g);
            return g;
        }

        private ComboBox CreateSoundCombo(int x, int y)
        {
            ComboBox cb = new ComboBox { Location = new Point(x, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            string mediaPath = @"C:\Windows\Media";
            if (Directory.Exists(mediaPath))
            {
                var files = Directory.GetFiles(mediaPath, "*.wav").Select(Path.GetFileName).ToArray();
                cb.Items.AddRange(files);
            }
            return cb;
        }

        private TrackBar CreateTrackBar(int x, int y) => new TrackBar { Location = new Point(x, y), Width = 300, Maximum = 100, TickFrequency = 10, SmallChange = 5 };

        private Button CreateTestButton(int x, int y, ComboBox cb, Func<int> getVol)
        {
            Button b = new Button { Text = "Play", Location = new Point(x, y), Width = 50, Height = 23, BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.Click += (s, e) => {
                string path = Path.Combine(@"C:\Windows\Media", cb.SelectedItem?.ToString() ?? "");
                AudioManager.PlaySound(path, getVol());
            };
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
            AppSettings.Save(Settings);
        }
    }
}