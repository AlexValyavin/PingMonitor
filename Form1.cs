using System;
using System.Drawing;
using System.Collections.Generic;
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
        private static readonly Color IconColor = Theme.Icon;

        private TextBox textBoxName;
        private TextBox txtSearch;
        private Panel _searchBar;
        private ComboBox comboTemplates;
        private Label lblPrefix;
        private Label lblSuffix;
        private Label btnPin;
        private Label btnSettings;
        private Label btnInfo;
        private Label btnMinimize;
        private Label btnExit;
        private AppSettings _appSettings;

        // --- ДЛЯ ПЕРЕТАСКИВАНИЯ ПЛИТОК ---
        private Point _dragStartPoint;
        private bool _isMouseDown = false;
        private PingTile _potentialDragTile = null;
        // ---------------------------------

        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }

        public Form1()
        {
            InitializeComponent();
            _appSettings = AppSettings.Load();
                        SetupFormDesign();
                        ApplyTheme();
            this.FormClosing += Form1_FormClosing;
        }

        private void ApplyTheme()
        {
            if (_appSettings.IsDarkTheme) Theme.SetDark(); else Theme.SetLight();

            // Form
            this.BackColor = Theme.BgWindow;

            // Header
            if (panel1 != null) panel1.BackColor = Theme.BgHeader;
            if (textBoxIP != null) { textBoxIP.BackColor = Theme.BgInput; textBoxIP.ForeColor = Theme.Text; }
            if (textBoxName != null) { textBoxName.BackColor = Theme.BgInput; textBoxName.ForeColor = Theme.Text; }
            if (comboTemplates != null) { comboTemplates.BackColor = Theme.BgInput; comboTemplates.ForeColor = Theme.Text; }

            // Icons
            if (btnExit != null) btnExit.ForeColor = Theme.Icon;
            if (btnMinimize != null) btnMinimize.ForeColor = Theme.Icon;
            if (btnPin != null) btnPin.ForeColor = TopMost ? Theme.IconPinned : Theme.Icon;
            if (btnInfo != null) btnInfo.ForeColor = Theme.Icon;
            if (btnSettings != null) btnSettings.ForeColor = Theme.Icon;

            // Tile area
            if (flowLayoutPanel1 != null) flowLayoutPanel1.BackColor = Theme.BgWindow;
            if (_searchBar != null) _searchBar.BackColor = Theme.BgHeader;
            foreach (Control c in flowLayoutPanel1.Controls)
                if (c is PingTile pt) pt.UpdateSettings(_appSettings);
        }

        private void SetupFormDesign()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = Lang.Get("title");
            this.BackColor = Theme.BgWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            this.Padding = new Padding(1);
            this.MinimumSize = new Size(MinWindowWidth, 150);

            panel1.Height = 62;
            panel1.BackColor = Theme.BgHeader;
            panel1.Dock = DockStyle.Top;
            panel1.MouseDown += DragWindow;

            Font fontInputs = new Font("Segoe UI", 10F);
            Font fontHints = new Font("Segoe UI", 8F);

            // ===== HEADER: TableLayoutPanel вместо ручных координат =====
            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 6;
            header.RowCount = 2;
            header.BackColor = Color.Transparent;
            header.Margin = new Padding(0);
            header.Padding = new Padding(12, 4, 12, 4);
            header.MouseDown += DragWindow;

            // Колонки: режим | IP/ID | имя | добавить | пусто | иконки
            // Процентные колонки — адаптируются при ресайзе окна
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));   // Режим/Шаблон
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));     // IP / ID
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));     // Имя
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));    // Добавить
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));     // spacer
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));   // иконки

            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // --- Col 0: Режим / Шаблон ---
            Label lblMode = new Label { Text = Lang.Get("mode"), ForeColor = Theme.TextHint,
                AutoSize = true, Font = fontHints, Margin = new Padding(0) };
            lblMode.MouseDown += DragWindow;
            header.Controls.Add(lblMode, 0, 0);

            comboTemplates = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
                Font = fontInputs, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 8, 2) };
            comboTemplates.SelectedIndexChanged += ComboTemplates_SelectedIndexChanged;
            header.Controls.Add(comboTemplates, 0, 1);

            // --- Col 1: IP / ID (prefix | input | suffix) ---
            Label lblIpHint = new Label { Text = Lang.Get("ip_hint"), ForeColor = Theme.TextHint,
                AutoSize = true, Font = fontHints, Margin = new Padding(0) };
            lblIpHint.MouseDown += DragWindow;
            header.Controls.Add(lblIpHint, 1, 0);

            Panel pnlIP = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 8, 2), BackColor = Color.Transparent };
            pnlIP.MouseDown += DragWindow;

            lblPrefix = new Label { Text = "", ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent,
                Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 4, 0) };
            lblPrefix.MouseDown += DragWindow;

            lblSuffix = new Label { Text = "", ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), BackColor = Color.Transparent,
                Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(4, 0, 0, 0) };
            lblSuffix.MouseDown += DragWindow;

            textBoxIP.Font = fontInputs;
            textBoxIP.BackColor = Theme.BgInput;
            textBoxIP.ForeColor = Color.White;
            textBoxIP.BorderStyle = BorderStyle.FixedSingle;
            textBoxIP.Dock = DockStyle.Fill;
            textBoxIP.Margin = new Padding(0);
            textBoxIP.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) { textBoxName.Focus(); ev.Handled = true; ev.SuppressKeyPress = true; } };

            // Dock-порядок: fill первым, потом left/right поверх него
            pnlIP.Controls.Add(textBoxIP);
            pnlIP.Controls.Add(lblSuffix);
            pnlIP.Controls.Add(lblPrefix);
            header.Controls.Add(pnlIP, 1, 1);

            // --- Col 2: Имя (Опц.) ---
            Label lblNameHint = new Label { Text = Lang.Get("name_hint"), ForeColor = Theme.TextHint,
                AutoSize = true, Font = fontHints, Margin = new Padding(0) };
            lblNameHint.MouseDown += DragWindow;
            header.Controls.Add(lblNameHint, 2, 0);

            textBoxName = new TextBox { Font = fontInputs, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 8, 2) };
            textBoxName.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Enter) { buttonAdd_Click(s, ev); ev.Handled = true; ev.SuppressKeyPress = true; } };
            textBoxName.BackColor = Theme.BgInput;
            textBoxName.ForeColor = Color.White;
            textBoxName.BorderStyle = BorderStyle.FixedSingle;
            header.Controls.Add(textBoxName, 2, 1);

            // --- Col 3: Добавить ---
            buttonAdd.Text = Lang.Get("add");
            buttonAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            buttonAdd.Dock = DockStyle.Fill;
            buttonAdd.Margin = new Padding(0, 2, 0, 2);
            buttonAdd.FlatStyle = FlatStyle.Flat;
            buttonAdd.BackColor = Theme.Accent;
            buttonAdd.ForeColor = Color.White;
            buttonAdd.FlatAppearance.BorderSize = 0;
            buttonAdd.Cursor = Cursors.Hand;
            header.Controls.Add(buttonAdd, 3, 1);

            // --- Col 4: кнопка поиска 🔍 (слева в spacer) ---
            Label btnSearch = new Label { Text = "🔍", Font = new Font("Segoe UI Emoji", 12),
                ForeColor = Theme.Icon, AutoSize = true, Cursor = Cursors.Hand, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            btnSearch.Click += (s, ev) => ToggleSearchBar();
            btnSearch.MouseEnter += (s, ev) => btnSearch.ForeColor = Theme.IconHover;
            btnSearch.MouseLeave += (s, ev) => btnSearch.ForeColor = Theme.Icon;
            new ToolTip().SetToolTip(btnSearch, Lang.Get("search"));
            header.Controls.Add(btnSearch, 4, 1);

            // --- Col 5: иконки (справа, справа-налево) ---
            FlowLayoutPanel fpnlIcons = new FlowLayoutPanel();
            fpnlIcons.Dock = DockStyle.Fill;
            fpnlIcons.FlowDirection = FlowDirection.RightToLeft;
            fpnlIcons.WrapContents = false;
            fpnlIcons.BackColor = Color.Transparent;
            fpnlIcons.Padding = new Padding(0, 14, 0, 0);
            fpnlIcons.MouseDown += DragWindow;
            header.Controls.Add(fpnlIcons, 5, 0);
            header.SetRowSpan(fpnlIcons, 2);

            // Порядок добавления = справа налево: exit, minimize, search, pin, info, settings
                        btnExit = new Label { Text = "✕", Font = new Font("Arial", 11, FontStyle.Regular),
                ForeColor = IconColor, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 0) };
            btnExit.Click += (s, ev) => Application.Exit();
            btnExit.MouseEnter += (s, ev) => btnExit.ForeColor = Color.Red;
            btnExit.MouseLeave += (s, ev) => btnExit.ForeColor = IconColor;
            new ToolTip().SetToolTip(btnExit, Lang.Get("exit"));
            fpnlIcons.Controls.Add(btnExit);

            btnMinimize = new Label { Text = "—", Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = IconColor, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 2, 0) };
            btnMinimize.Click += (s, ev) => WindowState = FormWindowState.Minimized;
            btnMinimize.MouseEnter += (s, ev) => btnMinimize.ForeColor = Color.White;
            btnMinimize.MouseLeave += (s, ev) => btnMinimize.ForeColor = IconColor;
            new ToolTip().SetToolTip(btnMinimize, Lang.Get("minimize"));
            fpnlIcons.Controls.Add(btnMinimize);

            btnPin = new Label { Font = new Font("Segoe MDL2 Assets", 14),
                Text = "\uE718", ForeColor = IconColor, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 2, 0) };
            btnPin.Click += (s, ev) => {
                this.TopMost = !this.TopMost;
                if (this.TopMost) { btnPin.Text = "\uE840"; btnPin.ForeColor = Theme.IconPinned; }
                else { btnPin.Text = "\uE718"; btnPin.ForeColor = IconColor; }
            };
            new ToolTip().SetToolTip(btnPin, Lang.Get("pin_window"));
            fpnlIcons.Controls.Add(btnPin);

            btnInfo = new Label { Font = new Font("Segoe MDL2 Assets", 14),
                Text = "\uE946", ForeColor = IconColor, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 2, 0) };
            btnInfo.Click += (s, ev) => { new AboutForm().ShowDialog(); };
            btnInfo.MouseEnter += (s, ev) => btnInfo.ForeColor = Color.White;
            btnInfo.MouseLeave += (s, ev) => btnInfo.ForeColor = IconColor;
            new ToolTip().SetToolTip(btnInfo, Lang.Get("info"));
            fpnlIcons.Controls.Add(btnInfo);

            btnSettings = new Label { Font = new Font("Segoe MDL2 Assets", 14),
                Text = "\uE713", ForeColor = IconColor, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 2, 0) };
            btnSettings.Click += BtnSettings_Click;
            btnSettings.MouseEnter += (s, ev) => btnSettings.ForeColor = Color.White;
            btnSettings.MouseLeave += (s, ev) => btnSettings.ForeColor = IconColor;
            new ToolTip().SetToolTip(btnSettings, Lang.Get("settings"));
            fpnlIcons.Controls.Add(btnSettings);

            panel1.Controls.Add(header);

            // --- Search bar внутри хедера (появляется по 🔍) ---
            _searchBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Theme.BgHeader, Visible = false };
            Label lblSearchIconBar = new Label { Text = "🔍", ForeColor = Theme.TextDim, Dock = DockStyle.Left, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 4, 0) };
            Label btnCloseSearch = new Label { Text = "✕", ForeColor = Theme.Icon, Dock = DockStyle.Right, AutoSize = true, Cursor = Cursors.Hand, Padding = new Padding(4, 0, 14, 0), TextAlign = ContentAlignment.MiddleCenter };
            btnCloseSearch.Click += (s, ev) => { ToggleSearchBar(); txtSearch.Clear(); };
            txtSearch = new TextBox { Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(4) };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            txtSearch.KeyDown += (s, ev) => { if (ev.KeyCode == Keys.Escape) { ToggleSearchBar(); txtSearch.Clear(); } };
            _searchBar.Controls.Add(txtSearch);
            _searchBar.Controls.Add(lblSearchIconBar);
            _searchBar.Controls.Add(btnCloseSearch);
            panel1.Controls.Add(_searchBar);

            // --- DRAG & DROP ---
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Theme.BgWindow;
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Padding = new Padding(10);
            flowLayoutPanel1.AllowDrop = true;
            flowLayoutPanel1.DragEnter += FlowLayoutPanel1_DragEnter;
            flowLayoutPanel1.DragOver += FlowLayoutPanel1_DragOver;

            UpdateTemplatesList();
            ResizeWindowToFit(4);
            LoadPinnedTiles();
        }

        // --- ЛОГИКА ПЕРЕТАСКИВАНИЯ (DRAG & DROP) ---

        private void Tile_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _dragStartPoint = e.Location;
                Control c = sender as Control;
                while (c != null && !(c is PingTile)) c = c.Parent;
                _potentialDragTile = c as PingTile;
            }
        }

        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown && _potentialDragTile != null)
            {
                if (Math.Abs(e.X - _dragStartPoint.X) > SystemInformation.DragSize.Width ||
                    Math.Abs(e.Y - _dragStartPoint.Y) > SystemInformation.DragSize.Height)
                {
                    _potentialDragTile.DoDragDrop(_potentialDragTile, DragDropEffects.Move);
                    _isMouseDown = false;
                    _potentialDragTile = null;
                }
            }
        }

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

        private PingTile AddTile(string ip, string alias, bool pinned = false)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            PingTile tile = new PingTile(ip, alias, _appSettings);

            tile.EnableMouseEvents(Tile_MouseDown, Tile_MouseMove, Tile_MouseUp);

            if (pinned) tile.SetPinned(true);

            tile.PinStateChanged += (s, ps) => SavePinnedState();
            tile.StatsPeriodChanged += (s, sec) => SavePinnedState();

            tile.RemoveRequested += (s, ev) => { tile.Stop(); flowLayoutPanel1.Controls.Remove(tile); SavePinnedState(); tile.Dispose(); AdjustWindowSize(); };

            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);

            textBoxIP.Clear(); textBoxName.Clear(); textBoxIP.Focus();
            AdjustWindowSize();
            return tile;
        }

        // --- ЗАКРЕПЛЕНИЕ ПЛИТОК ---
        private void LoadPinnedTiles()
        {
            // Снапшот данных ДО начала — SavePinnedState из SetStatsWindowSec очищает списки
            List<string> addrs = new List<string>(_appSettings.PinnedAddresses);
            List<string> aliases = new List<string>(_appSettings.PinnedAliases);
            List<int> periods = new List<int>(_appSettings.PinnedStatsPeriods);

            // 1. Сначала загружаем ВСЕ плитки (без вызова SetStatsWindowSec)
            for (int i = 0; i < addrs.Count; i++)
            {
                string alias = i < aliases.Count ? aliases[i] : "";
                AddTile(addrs[i], alias, pinned: true);
            }

            // 2. Отдельно устанавливаем периоды
            for (int i = 0; i < periods.Count; i++)
            {
                if (i < flowLayoutPanel1.Controls.Count && flowLayoutPanel1.Controls[i] is PingTile tile)
                    tile.SetStatsWindowSec(periods[i]);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SavePinnedState();
        }

        private void SavePinnedState()
        {
            _appSettings.PinnedAddresses.Clear();
            _appSettings.PinnedAliases.Clear();
            _appSettings.PinnedStatsPeriods.Clear();
            foreach (Control c in flowLayoutPanel1.Controls)
            {
                if (c is PingTile pt && pt.IsPinned)
                {
                    _appSettings.PinnedAddresses.Add(pt.Address);
                    _appSettings.PinnedAliases.Add(pt.Alias ?? "");
                    _appSettings.PinnedStatsPeriods.Add(pt.StatsWindowSec);
                }
            }
            AppSettings.Save(_appSettings);
        }
        // -------------------------

        private void UpdateTemplatesList()
        {
            comboTemplates.Items.Clear();
            comboTemplates.Items.Add(Lang.Get("normal_input"));
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
                lblPrefix.Text = "";
                lblSuffix.Text = "";
            }
            else
            {
                string[] parts = selected.Split('*');
                lblPrefix.Text = parts.Length > 0 ? parts[0] : "";
                lblSuffix.Text = parts.Length > 1 ? parts[1] : "";
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm(_appSettings);
            if (sf.ShowDialog() == DialogResult.OK)
            {
                sf.ApplySettings(); _appSettings = sf.Settings;
                Lang.SetRu(_appSettings.IsRussian);
                ApplyTheme();
                UpdateTemplatesList();
                ApplyLanguage();
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

        public void ApplyLanguage()
        {
            this.Text = Lang.Get("title");
            buttonAdd.Text = Lang.Get("add");
            foreach (Control c in flowLayoutPanel1.Controls)
                if (c is PingTile pt) pt.UpdateUI();
        }

        private void ToggleSearchBar()
        {
            _searchBar.Visible = !_searchBar.Visible;
            panel1.Height = _searchBar.Visible ? 92 : 62;
            if (_searchBar.Visible) { txtSearch.Focus(); }
            else { txtSearch.Clear(); }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string query = txtSearch.Text.Trim().ToLower();
            foreach (Control c in flowLayoutPanel1.Controls)
            {
                if (c is PingTile pt)
                {
                    bool match = string.IsNullOrEmpty(query) ||
                        pt.Address.ToLower().Contains(query) ||
                        (pt.Alias ?? "").ToLower().Contains(query);
                    if (pt.Visible != match) pt.Visible = match;
                }
            }
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
            const int WM_NCHITTEST = 0x84;
            const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
            const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
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