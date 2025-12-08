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
        private const int MinWindowWidth = 600; // Минимальная ширина для верхней панели


        private TextBox textBoxName;
        private CheckBox checkAlwaysOnTop;
        private AppSettings _appSettings; // <-- Настройки

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

        public Form1()
        {
            InitializeComponent();
            _appSettings = AppSettings.Load(); // <-- Загрузка настроек
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

            // IP
            Label lblIpHint = new Label { Text = "IP адрес / Хост", ForeColor = Color.DarkGray, Location = new Point(12, 8), AutoSize = true, Font = fontHints };
            lblIpHint.MouseDown += DragWindow;
            panel1.Controls.Add(lblIpHint);

            textBoxIP.Font = fontInputs;
            textBoxIP.Width = 130;
            textBoxIP.Location = new Point(12, 28);
            textBoxIP.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { textBoxName.Focus(); e.Handled = true; e.SuppressKeyPress = true; } };

            // Name
            Label lblNameHint = new Label { Text = "Имя (необязательно)", ForeColor = Color.DarkGray, Location = new Point(155, 8), AutoSize = true, Font = fontHints };
            lblNameHint.MouseDown += DragWindow;
            panel1.Controls.Add(lblNameHint);

            textBoxName = new TextBox { Font = fontInputs, Location = new Point(155, 28), Width = 160 };
            textBoxName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { buttonAdd_Click(s, e); e.Handled = true; e.SuppressKeyPress = true; } };
            panel1.Controls.Add(textBoxName);

            // 3. Кнопка Добавить
            buttonAdd.Text = "Добавить";
            buttonAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAdd.Height = 27;
            buttonAdd.Width = 100;
            buttonAdd.Location = new Point(325, 27); // Чуть левее
            buttonAdd.FlatStyle = FlatStyle.Flat;
            buttonAdd.BackColor = Color.FromArgb(0, 122, 204);
            buttonAdd.ForeColor = Color.White;
            buttonAdd.FlatAppearance.BorderSize = 0;
            buttonAdd.Cursor = Cursors.Hand;

            // --- ПРАВАЯ ЧАСТЬ ПАНЕЛИ ---
            // Выравниваем от правого края, чтобы при любом размере окна они были справа

            // 4. Настройки (Шестеренка)
            Label btnSettings = new Label();
            btnSettings.Text = "⚙";
            btnSettings.Font = new Font("Segoe UI", 14);
            btnSettings.ForeColor = Color.Gray;
            btnSettings.AutoSize = true;
            btnSettings.Cursor = Cursors.Hand;
            // Привязываем к правому краю: Отступ 150px справа
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(panel1.Width - 150, 25);

            btnSettings.Click += (s, e) => {
                SettingsForm sf = new SettingsForm(_appSettings);
                if (sf.ShowDialog() == DialogResult.OK)
                {
                    sf.ApplySettings();
                    _appSettings = sf.Settings;
                    foreach (Control c in flowLayoutPanel1.Controls) if (c is PingTile pt) pt.UpdateSettings(_appSettings);
                }
            };
            btnSettings.MouseEnter += (s, e) => btnSettings.ForeColor = Color.White;
            btnSettings.MouseLeave += (s, e) => btnSettings.ForeColor = Color.Gray;
            panel1.Controls.Add(btnSettings);

            // 5. Поверх всех (Скрепка)
            checkAlwaysOnTop = new CheckBox();
            checkAlwaysOnTop.Appearance = Appearance.Button;
            checkAlwaysOnTop.Text = "📌";
            checkAlwaysOnTop.TextAlign = ContentAlignment.MiddleCenter;
            checkAlwaysOnTop.AutoSize = false;
            checkAlwaysOnTop.Size = new Size(40, 27);
            // Привязываем к правому краю: Отступ 110px справа
            checkAlwaysOnTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkAlwaysOnTop.Location = new Point(panel1.Width - 110, 27);

            checkAlwaysOnTop.FlatStyle = FlatStyle.Flat;
            checkAlwaysOnTop.FlatAppearance.BorderSize = 0;
            checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60);
            checkAlwaysOnTop.ForeColor = Color.Gray;
            checkAlwaysOnTop.Cursor = Cursors.Hand;

            checkAlwaysOnTop.CheckedChanged += (s, e) => {
                this.TopMost = checkAlwaysOnTop.Checked;
                if (checkAlwaysOnTop.Checked)
                {
                    checkAlwaysOnTop.BackColor = Color.FromArgb(46, 204, 113);
                    checkAlwaysOnTop.ForeColor = Color.Black;
                }
                else
                {
                    checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60);
                    checkAlwaysOnTop.ForeColor = Color.Gray;
                }
            };
            panel1.Controls.Add(checkAlwaysOnTop);

            // 6. Кнопки окна (Свернуть и Закрыть) - сдвигаем правее

            Label btnMinimize = new Label();
            btnMinimize.Text = "—";
            btnMinimize.Font = new Font("Arial", 12, FontStyle.Bold);
            btnMinimize.ForeColor = Color.Gray;
            btnMinimize.AutoSize = true;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Location = new Point(panel1.Width - 65, 5); // 65px справа

            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, e) => btnMinimize.ForeColor = Color.White;
            btnMinimize.MouseLeave += (s, e) => btnMinimize.ForeColor = Color.Gray;
            panel1.Controls.Add(btnMinimize);

            Label btnExit = new Label();
            btnExit.Text = "✕";
            btnExit.Font = new Font("Arial", 11, FontStyle.Regular);
            btnExit.ForeColor = Color.Gray;
            btnExit.AutoSize = true;
            btnExit.Cursor = Cursors.Hand;
            btnExit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExit.Location = new Point(panel1.Width - 35, 6); // 35px справа

            btnExit.Click += (s, e) => Application.Exit();
            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = Color.Red;
            btnExit.MouseLeave += (s, e) => btnExit.ForeColor = Color.Gray;
            panel1.Controls.Add(btnExit);

            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30);
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Padding = new Padding(10);

            ResizeWindowToFit(4);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            AddTile(textBoxIP.Text, textBoxName.Text);
        }

        private void AddTile(string ip, string alias)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            PingTile tile = new PingTile(ip, alias, _appSettings); // <-- Передаем настройки
            tile.RemoveRequested += (s, ev) => { tile.Stop(); flowLayoutPanel1.Controls.Remove(tile); tile.Dispose(); AdjustWindowSize(); };
            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);
            textBoxIP.Clear(); textBoxName.Clear(); textBoxIP.Focus();
            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            int count = flowLayoutPanel1.Controls.Count;
            // Если плиток нет, ставим минимальную ширину
            if (count == 0)
            {
                this.Width = MinWindowWidth;
                this.Height = 150;
                return;
            }

            int cols = Math.Min(count, 4);
            int rows = (int)Math.Ceiling((double)count / 4);

            // Рассчитываем желаемую ширину по плиткам
            int targetWidth = (TileWidth + MarginSize) * cols + 40 + flowLayoutPanel1.Padding.Horizontal;

            // ВАЖНО: Если желаемая ширина меньше минимальной (панели инструментов), берем минимальную
            targetWidth = Math.Max(targetWidth, MinWindowWidth);

            int targetHeight = (TileHeight + MarginSize) * rows + panel1.Height + 50 + flowLayoutPanel1.Padding.Vertical;

            Rectangle screen = Screen.FromControl(this).WorkingArea;
            this.Width = Math.Min(targetWidth, screen.Width);
            this.Height = Math.Min(targetHeight, (int)(screen.Height * 0.9));
        }

        private void ResizeWindowToFit(int tilesCount)
        {
            int targetWidth = (TileWidth + MarginSize) * tilesCount + 50;
            // То же самое правило при старте
            targetWidth = Math.Max(targetWidth, MinWindowWidth);

            int targetHeight = (TileHeight + MarginSize) * 2 + panel1.Height + 50;
            this.Size = new Size(targetWidth, targetHeight);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTTOPLEFT = 13; const int HTTOPRIGHT = 14; const int HTBOTTOM = 15; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
            {
                int resizeArea = 10;
                Point p = PointToClient(new Point(m.LParam.ToInt32()));
                if (p.Y <= resizeArea)
                {
                    if (p.X <= resizeArea) m.Result = (IntPtr)HTTOPLEFT;
                    else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTTOPRIGHT;
                    else m.Result = (IntPtr)HTTOP;
                }
                else if (p.Y >= Height - resizeArea)
                {
                    if (p.X <= resizeArea) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else m.Result = (IntPtr)HTBOTTOM;
                }
                else if (p.X <= resizeArea) m.Result = (IntPtr)HTLEFT;
                else if (p.X >= Width - resizeArea) m.Result = (IntPtr)HTRIGHT;
            }
        }
    }
}