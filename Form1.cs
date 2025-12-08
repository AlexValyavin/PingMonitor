using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices; // Обязательно для перетаскивания

namespace PingMonitor
{
    public partial class Form1 : Form
    {
        private const int TileWidth = 240;
        private const int TileHeight = 110;
        private const int MarginSize = 10;

        private TextBox textBoxName;
        private CheckBox checkAlwaysOnTop;

        // --- ИМПОРТ ФУНКЦИЙ ДЛЯ ПЕРЕТАСКИВАНИЯ ОКНА ---
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Метод, который говорит Windows: "Началось перетаскивание заголовка"
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }
        // ---------------------------------------------

        public Form1()
        {
            InitializeComponent();
            SetupFormDesign();
        }

        private void SetupFormDesign()
        {
            // 1. Убираем рамку
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "NetMonitor Pro";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.Padding = new Padding(1); // Тонкая обводка окна

            // --- ВЕРХНЯЯ ПАНЕЛЬ ---
            panel1.Height = 60; // Чуть увеличим высоту, чтобы элементы не слипались
            panel1.BackColor = Color.FromArgb(45, 45, 48);
            panel1.Dock = DockStyle.Top;

            // ВАЖНО: Подписываем панель на событие перетаскивания
            panel1.MouseDown += DragWindow;

            // Настраиваем шрифты для полей
            Font fontInputs = new Font("Segoe UI", 10F);
            Font fontHints = new Font("Segoe UI", 8F);

            // 1. Поле IP
            // Подсказка
            Label lblIpHint = new Label();
            lblIpHint.Text = "IP адрес / Хост";
            lblIpHint.ForeColor = Color.DarkGray;
            lblIpHint.Location = new Point(12, 8); // Сверху
            lblIpHint.AutoSize = true;
            lblIpHint.Font = fontHints;
            lblIpHint.MouseDown += DragWindow; // Чтобы за текст тоже можно было таскать
            panel1.Controls.Add(lblIpHint);

            // Поле ввода
            textBoxIP.Font = fontInputs;
            textBoxIP.Width = 130;
            textBoxIP.Location = new Point(12, 28); // Чуть ниже подсказки

            // 2. Поле Имени
            // Подсказка
            Label lblNameHint = new Label();
            lblNameHint.Text = "Имя (необязательно)";
            lblNameHint.ForeColor = Color.DarkGray;
            lblNameHint.Location = new Point(155, 8);
            lblNameHint.AutoSize = true;
            lblNameHint.Font = fontHints;
            lblNameHint.MouseDown += DragWindow;
            panel1.Controls.Add(lblNameHint);

            // Поле ввода
            textBoxName = new TextBox();
            textBoxName.Font = fontInputs;
            textBoxName.Location = new Point(155, 28);
            textBoxName.Width = 160;
            textBoxName.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { buttonAdd_Click(s, e); e.Handled = true; e.SuppressKeyPress = true; }
            };
            panel1.Controls.Add(textBoxName);

            // 3. Кнопка Добавить
            buttonAdd.Text = "Добавить";
            buttonAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAdd.Height = 27; // Подгоняем высоту под поля ввода
            buttonAdd.Width = 100;
            buttonAdd.Location = new Point(330, 27); // Выравниваем по вертикали с полями
            // Сделаем кнопку плоской и красивой
            buttonAdd.FlatStyle = FlatStyle.Flat;
            buttonAdd.BackColor = Color.FromArgb(0, 122, 204); // Синий акцент
            buttonAdd.ForeColor = Color.White;
            buttonAdd.FlatAppearance.BorderSize = 0;
            buttonAdd.Cursor = Cursors.Hand;

            // 4. Кнопка "Поверх всех"
            checkAlwaysOnTop = new CheckBox();
            checkAlwaysOnTop.Appearance = Appearance.Button;
            checkAlwaysOnTop.Text = "📌"; // Только иконка для компактности, или текст
            checkAlwaysOnTop.TextAlign = ContentAlignment.MiddleCenter;
            checkAlwaysOnTop.AutoSize = false;
            checkAlwaysOnTop.Size = new Size(40, 27); // Квадратная кнопка
            checkAlwaysOnTop.Location = new Point(panel1.Width - 110, 27);
            checkAlwaysOnTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkAlwaysOnTop.FlatStyle = FlatStyle.Flat;
            checkAlwaysOnTop.FlatAppearance.BorderSize = 0;
            checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60);
            checkAlwaysOnTop.ForeColor = Color.Gray;
            checkAlwaysOnTop.Cursor = Cursors.Hand;

            // Тултип (подсказка при наведении)
            ToolTip tt = new ToolTip();
            tt.SetToolTip(checkAlwaysOnTop, "Закрепить поверх всех окон");

            checkAlwaysOnTop.CheckedChanged += (s, e) =>
            {
                this.TopMost = checkAlwaysOnTop.Checked;
                if (checkAlwaysOnTop.Checked)
                {
                    checkAlwaysOnTop.BackColor = Color.FromArgb(46, 204, 113); // Зеленый
                    checkAlwaysOnTop.ForeColor = Color.Black;
                }
                else
                {
                    checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60);
                    checkAlwaysOnTop.ForeColor = Color.Gray;
                }
            };
            panel1.Controls.Add(checkAlwaysOnTop);

            // 5. Кнопка СВЕРНУТЬ (—)
            Label btnMinimize = new Label();
            btnMinimize.Text = "—";
            btnMinimize.Font = new Font("Arial", 12, FontStyle.Bold);
            btnMinimize.ForeColor = Color.Gray;
            btnMinimize.AutoSize = true;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.Location = new Point(panel1.Width - 65, 5); // Самый верхний угол
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.ForeColor = Color.White; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.ForeColor = Color.Gray; };
            panel1.Controls.Add(btnMinimize);

            // 6. Кнопка ЗАКРЫТЬ (X)
            Label btnExit = new Label();
            btnExit.Text = "✕";
            btnExit.Font = new Font("Arial", 11, FontStyle.Regular);
            btnExit.ForeColor = Color.Gray;
            btnExit.AutoSize = true;
            btnExit.Cursor = Cursors.Hand;
            btnExit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExit.Location = new Point(panel1.Width - 30, 6);
            btnExit.Click += (s, e) => Application.Exit();
            btnExit.MouseEnter += (s, e) => { btnExit.ForeColor = Color.Red; };
            btnExit.MouseLeave += (s, e) => { btnExit.ForeColor = Color.Gray; };
            panel1.Controls.Add(btnExit);

            // --- ОСНОВНАЯ ОБЛАСТЬ ---
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30);
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Padding = new Padding(10);

            // Событие Enter для IP
            textBoxIP.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { textBoxName.Focus(); e.Handled = true; e.SuppressKeyPress = true; }
            };

            ResizeWindowToFit(4);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            AddTile(textBoxIP.Text, textBoxName.Text);
        }

        private void AddTile(string ip, string alias)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            PingTile tile = new PingTile(ip, alias);
            tile.RemoveRequested += (s, ev) =>
            {
                tile.Stop();
                flowLayoutPanel1.Controls.Remove(tile);
                tile.Dispose();
                AdjustWindowSize();
            };

            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);

            textBoxIP.Clear();
            textBoxName.Clear();
            textBoxIP.Focus();
            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            int count = flowLayoutPanel1.Controls.Count;
            if (count == 0) return;
            int cols = Math.Min(count, 4);
            int rows = (int)Math.Ceiling((double)count / 4);
            int targetWidth = (TileWidth + MarginSize) * cols + 40 + flowLayoutPanel1.Padding.Horizontal;
            int targetHeight = (TileHeight + MarginSize) * rows + panel1.Height + 50 + flowLayoutPanel1.Padding.Vertical;
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            this.Width = Math.Min(targetWidth, screen.Width);
            this.Height = Math.Min(targetHeight, (int)(screen.Height * 0.9));
        }

        private void ResizeWindowToFit(int tilesCount)
        {
            int targetWidth = (TileWidth + MarginSize) * tilesCount + 50;
            int targetHeight = (TileHeight + MarginSize) * 2 + panel1.Height + 50;
            this.Size = new Size(targetWidth, targetHeight);
        }

        // --- ТОЛЬКО РЕСАЙЗ ЗА КРАЯ ОКНА (WndProc теперь только для ресайза) ---
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            // Зоны ресайза
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST)
            {
                int resizeArea = 10;
                Point screenPoint = new Point(m.LParam.ToInt32());
                Point clientPoint = this.PointToClient(screenPoint);

                if (clientPoint.Y <= resizeArea)
                {
                    if (clientPoint.X <= resizeArea) m.Result = (IntPtr)HTTOPLEFT;
                    else if (clientPoint.X >= (this.Size.Width - resizeArea)) m.Result = (IntPtr)HTTOPRIGHT;
                    else m.Result = (IntPtr)HTTOP;
                }
                else if (clientPoint.Y >= (this.Size.Height - resizeArea))
                {
                    if (clientPoint.X <= resizeArea) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (clientPoint.X >= (this.Size.Width - resizeArea)) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else m.Result = (IntPtr)HTBOTTOM;
                }
                else if (clientPoint.X <= resizeArea) m.Result = (IntPtr)HTLEFT;
                else if (clientPoint.X >= (this.Size.Width - resizeArea)) m.Result = (IntPtr)HTRIGHT;
            }
        }
    }
}