using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PingMonitor
{
    public partial class Form1 : Form
    {
        private const int TileWidth = 240;
        private const int TileHeight = 110;
        private const int MarginSize = 10;
        private const int MinWindowWidth = 780; // Чуть шире, так как добавился комбобокс

        private TextBox textBoxName;
        private CheckBox checkAlwaysOnTop;
        private AppSettings _appSettings;

        // Новые элементы для шаблонов
        private ComboBox comboTemplates;
        private Label lblPrefix;
        private Label lblSuffix;

        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } }

        public Form1()
        {
            InitializeComponent();
            _appSettings = AppSettings.Load();
            SetupFormDesign();
        }

        private void SetupFormDesign()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "NetMonitor Pro";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.Padding = new Padding(1);

            panel1.Height = 60;
            panel1.BackColor = Color.FromArgb(45, 45, 48);
            panel1.Dock = DockStyle.Top;
            panel1.MouseDown += DragWindow;

            Font fontInputs = new Font("Segoe UI", 10F);
            Font fontHints = new Font("Segoe UI", 8F);

            // --- 1. ВЫБОР РЕЖИМА (ComboBox) ---
            Label lblMode = new Label { Text = "Режим / Шаблон", ForeColor = Color.DarkGray, Location = new Point(12, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblMode);

            comboTemplates = new ComboBox();
            comboTemplates.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTemplates.Font = fontInputs;
            comboTemplates.Location = new Point(12, 28);
            comboTemplates.Width = 140;
            comboTemplates.SelectedIndexChanged += ComboTemplates_SelectedIndexChanged;
            panel1.Controls.Add(comboTemplates);

            // --- 2. ПОЛЕ ВВОДА IP (С ПРЕФИКСОМ И СУФФИКСОМ) ---
            Label lblIpHint = new Label { Text = "IP / ID", ForeColor = Color.DarkGray, Location = new Point(165, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblIpHint);

            // Лейбл префикса (например "192.168.")
            lblPrefix = new Label { Text = "", ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent };
            lblPrefix.Location = new Point(165, 30); // Y чуть ниже, чтобы ровно с текстом
            panel1.Controls.Add(lblPrefix);

            // Само поле ввода
            textBoxIP.Font = fontInputs;
            textBoxIP.Location = new Point(165, 28); // X будет меняться динамически
            textBoxIP.Width = 100;
            textBoxIP.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { textBoxName.Focus(); e.Handled = true; e.SuppressKeyPress = true; } };

            // Лейбл суффикса (например ".local")
            lblSuffix = new Label { Text = "", ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent };
            lblSuffix.Location = new Point(265, 30); // X будет меняться
            panel1.Controls.Add(lblSuffix);


            // --- 3. ИМЯ ---
            Label lblNameHint = new Label { Text = "Имя (Опц.)", ForeColor = Color.DarkGray, Location = new Point(360, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblNameHint);

            textBoxName = new TextBox { Font = fontInputs, Location = new Point(360, 28), Width = 140 };
            textBoxName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { buttonAdd_Click(s, e); e.Handled = true; e.SuppressKeyPress = true; } };
            panel1.Controls.Add(textBoxName);

            // --- 4. КНОПКИ ---
            buttonAdd.Text = "Добавить";
            buttonAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAdd.Height = 27; buttonAdd.Width = 90;
            buttonAdd.Location = new Point(510, 27);
            buttonAdd.FlatStyle = FlatStyle.Flat; buttonAdd.BackColor = Color.FromArgb(0, 122, 204); buttonAdd.ForeColor = Color.White; buttonAdd.FlatAppearance.BorderSize = 0; buttonAdd.Cursor = Cursors.Hand;

            // Правые кнопки
            Label btnSettings = new Label { Text = "⚙", Font = new Font("Segoe UI", 14), ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel1.Width - 145, 25) };
            btnSettings.Click += BtnSettings_Click;
            btnSettings.MouseEnter += (s, e) => btnSettings.ForeColor = Color.White; btnSettings.MouseLeave += (s, e) => btnSettings.ForeColor = Color.Gray;
            panel1.Controls.Add(btnSettings);

            checkAlwaysOnTop = new CheckBox { Appearance = Appearance.Button, Text = "📌", TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, Size = new Size(40, 27), Location = new Point(panel1.Width - 110, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.Gray, Cursor = Cursors.Hand };
            checkAlwaysOnTop.FlatAppearance.BorderSize = 0;
            checkAlwaysOnTop.CheckedChanged += (s, e) => { TopMost = checkAlwaysOnTop.Checked; if (checkAlwaysOnTop.Checked) { checkAlwaysOnTop.BackColor = Color.FromArgb(46, 204, 113); checkAlwaysOnTop.ForeColor = Color.Black; } else { checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60); checkAlwaysOnTop.ForeColor = Color.Gray; } };
            panel1.Controls.Add(checkAlwaysOnTop);

            Label btnMinimize = new Label { Text = "—", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel1.Width - 65, 5) };
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, e) => btnMinimize.ForeColor = Color.White; btnMinimize.MouseLeave += (s, e) => btnMinimize.ForeColor = Color.Gray;
            panel1.Controls.Add(btnMinimize);

            Label btnExit = new Label { Text = "✕", Font = new Font("Arial", 11, FontStyle.Regular), ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel1.Width - 30, 6) };
            btnExit.Click += (s, e) => Application.Exit();
            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = Color.Red; btnExit.MouseLeave += (s, e) => btnExit.ForeColor = Color.Gray;
            panel1.Controls.Add(btnExit);

            flowLayoutPanel1.Dock = DockStyle.Fill; flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30); flowLayoutPanel1.AutoScroll = true; flowLayoutPanel1.Padding = new Padding(10);

            // Инициализация списка шаблонов
            UpdateTemplatesList();

            ResizeWindowToFit(4);
        }

        private void UpdateTemplatesList()
        {
            comboTemplates.Items.Clear();
            comboTemplates.Items.Add("Обычный ввод (IP)"); // Стандартный режим

            foreach (var t in _appSettings.IpTemplates)
            {
                comboTemplates.Items.Add(t);
            }

            // Восстанавливаем выбор или ставим первый
            if (_appSettings.LastTemplateIndex >= 0 && _appSettings.LastTemplateIndex < comboTemplates.Items.Count)
                comboTemplates.SelectedIndex = _appSettings.LastTemplateIndex;
            else
                comboTemplates.SelectedIndex = 0;
        }

        // --- ДИНАМИЧЕСКИЙ ИНТЕРФЕЙС ПРИ ВЫБОРЕ ШАБЛОНА ---
        private void ComboTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            _appSettings.LastTemplateIndex = comboTemplates.SelectedIndex;
            AppSettings.Save(_appSettings); // Запоминаем выбор

            string selected = comboTemplates.SelectedItem.ToString();

            if (comboTemplates.SelectedIndex == 0) // Обычный режим
            {
                lblPrefix.Text = "";
                lblSuffix.Text = "";
                // Возвращаем поле на место (X=165)
                textBoxIP.Location = new Point(165, 28);
                textBoxIP.Width = 180; // Широкое поле
            }
            else
            {
                // Режим шаблона. Разбиваем по звездочке
                string[] parts = selected.Split('*');
                string prefix = parts.Length > 0 ? parts[0] : "";
                string suffix = parts.Length > 1 ? parts[1] : "";

                lblPrefix.Text = prefix;
                lblSuffix.Text = suffix;

                // Пересчитываем координаты, чтобы поле ввода "встало" между текстом
                int startX = 165;

                // Сдвигаем префикс
                lblPrefix.Location = new Point(startX, 30);

                // Сдвигаем поле ввода сразу за префиксом
                textBoxIP.Location = new Point(startX + lblPrefix.Width - 5, 28);
                textBoxIP.Width = 70; // Узкое поле (только для ID)

                // Сдвигаем суффикс сразу за полем ввода
                lblSuffix.Location = new Point(textBoxIP.Location.X + textBoxIP.Width, 30);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm(_appSettings);
            if (sf.ShowDialog() == DialogResult.OK)
            {
                sf.ApplySettings();
                _appSettings = sf.Settings;
                UpdateTemplatesList(); // Обновляем список, если добавили новый
                foreach (Control c in flowLayoutPanel1.Controls) if (c is PingTile pt) pt.UpdateSettings(_appSettings);
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string rawInput = textBoxIP.Text.Trim();
            string aliasInput = textBoxName.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawInput)) return;

            string finalAddress = rawInput;
            string finalAlias = aliasInput;

            // Если выбран шаблон (не "Обычный ввод")
            if (comboTemplates.SelectedIndex > 0)
            {
                string template = comboTemplates.SelectedItem.ToString();
                // Заменяем * на введенный текст
                finalAddress = template.Replace("*", rawInput);

                // Если имя не задано, используем ID (rawInput) как имя
                if (string.IsNullOrEmpty(finalAlias)) finalAlias = rawInput;
            }

            AddTile(finalAddress, finalAlias);
        }

        // ... Остальные методы (AddTile, AdjustWindowSize, etc) те же ...
        // ... (Не забудь скопировать их из старого кода, если заменяешь весь файл) ...

        private void AddTile(string ip, string alias)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            PingTile tile = new PingTile(ip, alias, _appSettings);
            tile.RemoveRequested += (s, ev) => { tile.Stop(); flowLayoutPanel1.Controls.Remove(tile); tile.Dispose(); AdjustWindowSize(); };
            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);
            textBoxIP.Clear(); textBoxName.Clear(); textBoxIP.Focus();
            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            int count = flowLayoutPanel1.Controls.Count;
            if (count == 0) { this.Width = MinWindowWidth; this.Height = 150; return; }
            int cols = Math.Min(count, 4); int rows = (int)Math.Ceiling((double)count / 4);
            int targetWidth = (TileWidth + MarginSize) * cols + 40 + flowLayoutPanel1.Padding.Horizontal;
            targetWidth = Math.Max(targetWidth, MinWindowWidth);
            int targetHeight = (TileHeight + MarginSize) * rows + panel1.Height + 50 + flowLayoutPanel1.Padding.Vertical;
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            this.Width = Math.Min(targetWidth, screen.Width);
            this.Height = Math.Min(targetHeight, (int)(screen.Height * 0.9));
        }

        private void ResizeWindowToFit(int tilesCount)
        {
            int targetWidth = (TileWidth + MarginSize) * tilesCount + 50; targetWidth = Math.Max(targetWidth, MinWindowWidth);
            int targetHeight = (TileHeight + MarginSize) * 2 + panel1.Height + 50;
            this.Size = new Size(targetWidth, targetHeight);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84; const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTTOPLEFT = 13; const int HTTOPRIGHT = 14; const int HTBOTTOM = 15; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            {
                int resizeArea = 10; Point p = PointToClient(new Point(m.LParam.ToInt32()));
                if (p.Y <= resizeArea) { if (p.X <= resizeArea) m.Result = (IntPtr)HTTOPLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTTOPRIGHT; else m.Result = (IntPtr)HTTOP; }
                else if (p.Y >= Height - resizeArea) { if (p.X <= resizeArea) m.Result = (IntPtr)HTBOTTOMLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTBOTTOMRIGHT; else m.Result = (IntPtr)HTBOTTOM; }
                else if (p.X <= resizeArea) m.Result = (IntPtr)HTLEFT; else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTRIGHT;
            }
        }
    }
}