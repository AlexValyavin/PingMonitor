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
        private const int MinWindowWidth = 780;

        private TextBox textBoxName;
        //private CheckBox checkAlwaysOnTop;
        private Label btnPin;
        private AppSettings _appSettings;

        private ComboBox comboTemplates;
        private Label lblPrefix;
        private Label lblSuffix;

        // --- ДЛЯ ПЕРЕТАСКИВАНИЯ ПЛИТОК ---
        private Point _dragStartPoint;
        private bool _isMouseDown = false;
        private PingTile _potentialDragTile = null;
        // ---------------------------------

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
            this.Icon = SystemIcons.Application; // Или Properties.Resources.icon
            this.Padding = new Padding(1);

            panel1.Height = 60;
            panel1.BackColor = Color.FromArgb(45, 45, 48);
            panel1.Dock = DockStyle.Top;
            panel1.MouseDown += DragWindow;

            Font fontInputs = new Font("Segoe UI", 10F);
            Font fontHints = new Font("Segoe UI", 8F);
            
            // --- HEADER CONTROLS ---
            Label lblMode = new Label { Text = "Режим / Шаблон", ForeColor = Color.DarkGray, Location = new Point(12, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblMode);

            comboTemplates = new ComboBox();
            comboTemplates.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTemplates.Font = fontInputs;
            comboTemplates.Location = new Point(12, 28);
            comboTemplates.Width = 140;
            comboTemplates.SelectedIndexChanged += ComboTemplates_SelectedIndexChanged;
            panel1.Controls.Add(comboTemplates);

            Label lblIpHint = new Label { Text = "IP / ID", ForeColor = Color.DarkGray, Location = new Point(165, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblIpHint);

            lblPrefix = new Label { Text = "", ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent };
            lblPrefix.Location = new Point(165, 30);
            panel1.Controls.Add(lblPrefix);

            textBoxIP.Font = fontInputs;
            textBoxIP.Location = new Point(165, 28);
            textBoxIP.Width = 100;
            textBoxIP.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { textBoxName.Focus(); e.Handled = true; e.SuppressKeyPress = true; } };
            textBoxIP.BackColor = Color.FromArgb(60, 60, 60); // Чуть светлее фона, но темный
            textBoxIP.ForeColor = Color.White;
            textBoxIP.BorderStyle = BorderStyle.FixedSingle; // Плоская рамка

            // То же самое для textBoxName и textBoxSuffix (если есть)
            lblSuffix = new Label { Text = "", ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent };
            lblSuffix.Location = new Point(265, 30);
            panel1.Controls.Add(lblSuffix);

            Label lblNameHint = new Label { Text = "Имя (Опц.)", ForeColor = Color.DarkGray, Location = new Point(360, 8), AutoSize = true, Font = fontHints };
            panel1.Controls.Add(lblNameHint);

            textBoxName = new TextBox { Font = fontInputs, Location = new Point(360, 28), Width = 140 };
            textBoxName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { buttonAdd_Click(s, e); e.Handled = true; e.SuppressKeyPress = true; } };
            panel1.Controls.Add(textBoxName);
            textBoxName.BackColor = Color.FromArgb(60, 60, 60);
            textBoxName.ForeColor = Color.White;
            textBoxName.BorderStyle = BorderStyle.FixedSingle;

            buttonAdd.Text = "Добавить";
            buttonAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAdd.Height = 27; buttonAdd.Width = 90;
            buttonAdd.Location = new Point(510, 27);
            buttonAdd.FlatStyle = FlatStyle.Flat; buttonAdd.BackColor = Color.FromArgb(0, 122, 204); buttonAdd.ForeColor = Color.White; buttonAdd.FlatAppearance.BorderSize = 0; buttonAdd.Cursor = Cursors.Hand;

            // --- ПРАВАЯ ЧАСТЬ ПАНЕЛИ ---

            // 1. Кнопка НАСТРОЙКИ (Шестеренка)
            Label btnSettings = new Label();
            btnSettings.Font = new Font("Segoe MDL2 Assets", 14); // Или Segoe UI Symbol
            btnSettings.Text = "\uE713"; // Код иконки
            btnSettings.ForeColor = Color.Gray;
            btnSettings.AutoSize = true;
            btnSettings.Cursor = Cursors.Hand;
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(panel1.Width - 140, 20); // Y=20 - центр
            btnSettings.Click += BtnSettings_Click;
            btnSettings.MouseEnter += (s, e) => btnSettings.ForeColor = Color.White;
            btnSettings.MouseLeave += (s, e) => btnSettings.ForeColor = Color.Gray;

            // Тултип
            new ToolTip().SetToolTip(btnSettings, "Настройки");
            panel1.Controls.Add(btnSettings);

            // 2. Кнопка ИНФО (i)
            Label btnInfo = new Label();
            btnInfo.Font = new Font("Segoe MDL2 Assets", 14);
            btnInfo.Text = "\uE946";
            btnInfo.ForeColor = Color.Gray;
            btnInfo.AutoSize = true;
            btnInfo.Cursor = Cursors.Hand;
            btnInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnInfo.Location = new Point(panel1.Width - 180, 20); // Y=20
            btnInfo.Click += (s, e) => { new AboutForm().ShowDialog(); };
            btnInfo.MouseEnter += (s, e) => btnInfo.ForeColor = Color.White;
            btnInfo.MouseLeave += (s, e) => btnInfo.ForeColor = Color.Gray;
            new ToolTip().SetToolTip(btnInfo, "Справка");
            panel1.Controls.Add(btnInfo);

            // 3. Кнопка ЗАКРЕПИТЬ (Скрепка) - ТЕПЕРЬ LABEL
            btnPin = new Label();
            btnPin.Font = new Font("Segoe MDL2 Assets", 14);
            btnPin.Text = "\uE718"; // Иконка "Откреплено"
            btnPin.ForeColor = Color.Gray;
            btnPin.AutoSize = true;
            btnPin.Cursor = Cursors.Hand;
            btnPin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPin.Location = new Point(panel1.Width - 100, 20); // Y=20 - Идеально ровно

            btnPin.Click += (s, e) => {
                this.TopMost = !this.TopMost; // Переключаем
                if (this.TopMost)
                {
                    btnPin.Text = "\uE840"; // Иконка "Закреплено" (Закрашенная скрепка)
                    btnPin.ForeColor = Color.FromArgb(46, 204, 113); // Зеленый
                }
                else
                {
                    btnPin.Text = "\uE718"; // Иконка "Откреплено"
                    btnPin.ForeColor = Color.Gray;
                }
            };
            new ToolTip().SetToolTip(btnPin, "Поверх всех окон");
            panel1.Controls.Add(btnPin);
            Label btnMinimize = new Label { Text = "—", Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel1.Width - 65, 5) };
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, e) => btnMinimize.ForeColor = Color.White; btnMinimize.MouseLeave += (s, e) => btnMinimize.ForeColor = Color.Gray;
            panel1.Controls.Add(btnMinimize);

            Label btnExit = new Label { Text = "✕", Font = new Font("Arial", 11, FontStyle.Regular), ForeColor = Color.Gray, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel1.Width - 30, 6) };
            btnExit.Click += (s, e) => Application.Exit();
            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = Color.Red; btnExit.MouseLeave += (s, e) => btnExit.ForeColor = Color.Gray;
            panel1.Controls.Add(btnExit);

            // --- DRAG & DROP ---
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30);
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Padding = new Padding(10);

            flowLayoutPanel1.AllowDrop = true;
            flowLayoutPanel1.DragEnter += FlowLayoutPanel1_DragEnter;
            flowLayoutPanel1.DragOver += FlowLayoutPanel1_DragOver;

            UpdateTemplatesList();
            ResizeWindowToFit(4);
        }

        // --- ЛОГИКА ПЕРЕТАСКИВАНИЯ (DRAG & DROP) ---

        // 1. Mouse Down
        private void Tile_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _dragStartPoint = e.Location;

                // Ищем саму плитку (вдруг нажали на label внутри)
                Control c = sender as Control;
                while (c != null && !(c is PingTile)) c = c.Parent;
                _potentialDragTile = c as PingTile;
            }
        }

        // 2. Mouse Move
        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown && _potentialDragTile != null)
            {
                // Если мышь сдвинулась, начинаем Drag
                if (Math.Abs(e.X - _dragStartPoint.X) > SystemInformation.DragSize.Width ||
                    Math.Abs(e.Y - _dragStartPoint.Y) > SystemInformation.DragSize.Height)
                {
                    _potentialDragTile.DoDragDrop(_potentialDragTile, DragDropEffects.Move);
                    _isMouseDown = false;
                    _potentialDragTile = null;
                }
            }
        }

        // 3. Mouse Up
        private void Tile_MouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
            _potentialDragTile = null;
        }

        private void FlowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PingTile))) e.Effect = DragDropEffects.Move;
            else e.Effect = DragDropEffects.None;
        }

        private void FlowLayoutPanel1_DragOver(object sender, DragEventArgs e)
        {
            PingTile draggedTile = (PingTile)e.Data.GetData(typeof(PingTile));
            Point pt = flowLayoutPanel1.PointToClient(new Point(e.X, e.Y));
            Control targetControl = flowLayoutPanel1.GetChildAtPoint(pt);

            if (targetControl != null && targetControl != draggedTile && targetControl is PingTile)
            {
                int targetIndex = flowLayoutPanel1.Controls.GetChildIndex(targetControl);
                flowLayoutPanel1.Controls.SetChildIndex(draggedTile, targetIndex);
            }
        }
        // ---------------------------------------------

        private void AddTile(string ip, string alias)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            PingTile tile = new PingTile(ip, alias, _appSettings);

            // ВОТ ЗДЕСЬ БЫЛА ОШИБКА. Теперь мы вызываем правильный метод:
            tile.EnableMouseEvents(Tile_MouseDown, Tile_MouseMove, Tile_MouseUp);

            tile.RemoveRequested += (s, ev) => { tile.Stop(); flowLayoutPanel1.Controls.Remove(tile); tile.Dispose(); AdjustWindowSize(); };

            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);

            textBoxIP.Clear(); textBoxName.Clear(); textBoxIP.Focus();
            AdjustWindowSize();
        }

        // ... СТАНДАРТНЫЙ КОД ...

        private void UpdateTemplatesList()
        {
            comboTemplates.Items.Clear();
            comboTemplates.Items.Add("Обычный ввод (IP)");
            foreach (var t in _appSettings.IpTemplates) comboTemplates.Items.Add(t);
            if (_appSettings.LastTemplateIndex >= 0 && _appSettings.LastTemplateIndex < comboTemplates.Items.Count)
                comboTemplates.SelectedIndex = _appSettings.LastTemplateIndex;
            else comboTemplates.SelectedIndex = 0;
        }

        private void ComboTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            _appSettings.LastTemplateIndex = comboTemplates.SelectedIndex;
            AppSettings.Save(_appSettings);
            string selected = comboTemplates.SelectedItem.ToString();
            if (comboTemplates.SelectedIndex == 0)
            {
                lblPrefix.Text = ""; lblSuffix.Text = ""; textBoxIP.Location = new Point(165, 28); textBoxIP.Width = 180;
            }
            else
            {
                string[] parts = selected.Split('*');
                lblPrefix.Text = parts.Length > 0 ? parts[0] : "";
                lblSuffix.Text = parts.Length > 1 ? parts[1] : "";
                int startX = 165;
                lblPrefix.Location = new Point(startX, 30);
                textBoxIP.Location = new Point(startX + lblPrefix.Width - 5, 28);
                textBoxIP.Width = 70;
                lblSuffix.Location = new Point(textBoxIP.Location.X + textBoxIP.Width, 30);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm(_appSettings);
            if (sf.ShowDialog() == DialogResult.OK)
            {
                sf.ApplySettings(); _appSettings = sf.Settings;
                UpdateTemplatesList();
                foreach (Control c in flowLayoutPanel1.Controls) if (c is PingTile pt) pt.UpdateSettings(_appSettings);
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string rawInput = textBoxIP.Text.Trim();
            string aliasInput = textBoxName.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawInput)) return;
            string finalAddress = rawInput; string finalAlias = aliasInput;
            if (comboTemplates.SelectedIndex > 0)
            {
                string template = comboTemplates.SelectedItem.ToString();
                finalAddress = template.Replace("*", rawInput);
                if (string.IsNullOrEmpty(finalAlias)) finalAlias = rawInput;
            }
            AddTile(finalAddress, finalAlias);
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