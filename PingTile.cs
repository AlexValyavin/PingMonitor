using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace PingMonitor
{
    public partial class PingTile : UserControl
    {
        private const int TileWidth = 240;
        private const int TileHeight = 118;
        private const int CornerRadius = 8;


        private string _address;
        private string _alias;
        private AppSettings _settings;
        private DateTime _lastSoundTime = DateTime.MinValue;

        private CancellationTokenSource _cts;
        private object _statsLock = new object();

        private Queue<bool> _history = new Queue<bool>();
        private Queue<long> _pingValues = new Queue<long>();
        private int _maxGraphPoints = 50;
        private int _maxHistoryEntries = 600;
        private int _statsWindowSec = 600;
        private List<string> _logEvents = new List<string>();
        private const int MaxLogEntries = 1000;
        private bool? _lastStateWasSuccess = null;

        private long _totalPings = 0;
        private long _lostPings = 0;
        private long _statLt100 = 0;
        private long _stat100to200 = 0;
        private long _statGt200 = 0;

        private bool _showGraph = true;
        private Color _currentStatusColor = Color.LimeGreen;
        private static readonly Color TileIconColor = Theme.Icon;

        public event EventHandler RemoveRequested;
        public event EventHandler<bool> PinStateChanged;
        public event EventHandler<int> StatsPeriodChanged;

        private bool _isPinned = false;
        public bool IsPinned => _isPinned;

        private Label btnPin;
        private Label lblAddress;
        private Label lblPing;
        private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose;

        public string Address => _address;
        public string Alias => _alias;
        public int StatsWindowSec => _statsWindowSec;

        public PingTile(string address, string alias, AppSettings settings)
        {
            this.DoubleBuffered = true;
            _address = address;
            _alias = alias;
            _settings = settings;

            RecalcWindowsFromSettings();
            AddToLog("Мониторинг запущен");
            InitializeCustomUI();
            StartPing();
        }

        // Включаем подписку на события мыши (для DragDrop и DoubleClick)
        public void EnableMouseEvents(MouseEventHandler mouseDownHandler, MouseEventHandler mouseMoveHandler, MouseEventHandler mouseUpHandler)
        {
            // Подписываемся на события для всех контролов, чтобы ловить клики везде
            AddMouseHandlers(this, mouseDownHandler, mouseMoveHandler, mouseUpHandler);
        }

        private void AddMouseHandlers(Control c, MouseEventHandler down, MouseEventHandler move, MouseEventHandler up)
        {
            if (c != btnClose && c != btnPin && c != lblAddress) // lblAddress исключаем, у него своя логика DoubleClick, но drag тоже нужен
            {
                c.MouseDown += down;
                c.MouseMove += move;
                c.MouseUp += up;
            }

            // Для заголовка добавляем и drag, и double click
            if (c == lblAddress)
            {
                c.MouseDown += down;
                c.MouseMove += move;
                c.MouseUp += up;
            }

            foreach (Control child in c.Controls)
            {
                AddMouseHandlers(child, down, move, up);
            }
        }

        // --- ЛОГИКА ПЕРЕИМЕНОВАНИЯ ---
        private void EditName()
        {
            string currentName = !string.IsNullOrEmpty(_alias) ? _alias : _address;
            string newName = InputDialog.Show("Переименовать", "Введите новое имя для " + _address, currentName);

            if (newName != null) // Если не нажали Отмена
            {
                _alias = newName;
                UpdateHeaderUI();
            }
        }

        private void UpdateHeaderUI()
        {
            if (!string.IsNullOrEmpty(_alias))
            {
                lblAddress.Text = _alias;
                lblAddress.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            }
            else
            {
                lblAddress.Text = _address;
                lblAddress.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            }
        }
        // -----------------------------

        /// <summary>Пересчитывает размеры окон графика и статистики из настроек.</summary>
        private void RecalcWindowsFromSettings()
        {
            int intervalSec = Math.Max(1, _settings.PingIntervalMs / 1000);
            _maxGraphPoints = Math.Max(10, _settings.GraphWindowSec / intervalSec);
            _maxHistoryEntries = _statsWindowSec <= 0 ? int.MaxValue : Math.Max(30, _statsWindowSec / intervalSec);
        }

        public void SetStatsWindowSec(int seconds)
        {
            _statsWindowSec = seconds <= 0 ? 0 : Math.Max(10, seconds);
            RecalcWindowsFromSettings();
            StatsPeriodChanged?.Invoke(this, _statsWindowSec);
            // Update stats label
            UpdateStatsLabel();
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
            RecalcWindowsFromSettings();
            // Перекрашиваем плитку под новую тему
            this.BackColor = Theme.BgTile;
            lblAddress.ForeColor = Theme.Text;
            lblPing.ForeColor = Theme.Text;
            lblStats.ForeColor = Theme.TextDim;
            btnClose.ForeColor = Theme.Icon;
            btnPin.ForeColor = _isPinned ? Theme.IconPinned : Theme.Icon;
            pnlStatusIndicator.BackColor = _currentStatusColor;
            Invalidate();
        }

        public void SetPinned(bool pinned)
        {
            _isPinned = pinned;
            btnPin.Text = _isPinned ? "\uE840" : "\uE718"; // E840=закреплено, E718=откреплено
            btnPin.ForeColor = _isPinned ? Theme.Ok : Theme.TextDim;
        }

        private void TogglePin()
        {
            SetPinned(!_isPinned);
            PinStateChanged?.Invoke(this, _isPinned);
        }

        private void AddToLog(string message)
        {
            string time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            string entry = $"[{time}] {message}";
            lock (_statsLock)
            {
                _logEvents.Add(entry);
                if (_logEvents.Count > MaxLogEntries) _logEvents.RemoveAt(0);
            }
        }

        private void ShowLogWindow()
        {
            StringBuilder sb = new StringBuilder();
            long total = 0, lost = 0, cntLt100 = 0, cnt100to200 = 0, cntGt200 = 0;
            List<string> logsCopy = new List<string>();

            lock (_statsLock)
            {
                total = _totalPings; lost = _lostPings;
                cntLt100 = _statLt100; cnt100to200 = _stat100to200; cntGt200 = _statGt200;
                logsCopy.AddRange(_logEvents);
            }

            double totalLossPct = total > 0 ? (double)lost / total * 100 : 0;
            double totalUptime = 100 - totalLossPct;
            long successTotal = total - lost;
            double pctLt100 = successTotal > 0 ? (double)cntLt100 / successTotal * 100 : 0;
            double pct100to200 = successTotal > 0 ? (double)cnt100to200 / successTotal * 100 : 0;
            double pctGt200 = successTotal > 0 ? (double)cntGt200 / successTotal * 100 : 0;

            string name = !string.IsNullOrEmpty(_alias) ? _alias : _address;
            sb.AppendLine($"ОТЧЕТ МОНИТОРИНГА: {name}");
            sb.AppendLine($"Адрес: {_address}");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine($"Всего пакетов: {total} | Потерь: {totalLossPct:F2}%");
            sb.AppendLine($"Стабильность: {totalUptime:F2}%");
            sb.AppendLine("РАСПРЕДЕЛЕНИЕ ЗАДЕРЖЕК:");
            sb.AppendLine($"< 100ms: {pctLt100:F1}%");
            sb.AppendLine($"100-200ms: {pct100to200:F1}%");
            sb.AppendLine($"> 200ms: {pctGt200:F1}%");
            sb.AppendLine(new string('=', 40));
            foreach (var line in logsCopy) sb.AppendLine(line);

            LogForm form = new LogForm(name, sb.ToString());
            form.ShowDialog();
        }

        private void InitializeCustomUI()
        {
            this.Size = new Size(TileWidth, TileHeight);
            this.BackColor = Theme.BgTile;
            this.Margin = new Padding(5);
            ApplyRoundedCorners();

            // --- Статус-индикатор (скруглённая полоска сверху) ---
            pnlStatusIndicator = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Theme.TextDim, Margin = new Padding(8, 6, 8, 0) };
            pnlStatusIndicator.Resize += (s, e) => RoundPanel(pnlStatusIndicator, 2);
            this.Controls.Add(pnlStatusIndicator);

            // --- Заголовок: адрес + кнопки ---
            lblAddress = new Label
            {
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(12, 0, 60, 0), // справа место под кнопки
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            lblAddress.DoubleClick += (s, e) => EditName();
            this.Controls.Add(lblAddress);

            // --- Кнопка закрепления ---
            btnPin = new Label
            {
                Text = "\uE718",
                Font = new Font("Segoe MDL2 Assets", 10),
                ForeColor = TileIconColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(this.Width - 44, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnPin.Click += (s, e) => TogglePin();
            btnPin.MouseEnter += (s, e) => btnPin.ForeColor = Theme.Text;
            btnPin.MouseLeave += (s, e) => btnPin.ForeColor = _isPinned ? Theme.Ok : TileIconColor;
            this.Controls.Add(btnPin);

            // --- Кнопка закрытия ---
            btnClose = new Label
            {
                Text = "✕",
                ForeColor = TileIconColor,
                BackColor = Color.Transparent,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(this.Width - 24, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => { _cts?.Cancel(); RemoveRequested?.Invoke(this, EventArgs.Empty); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = TileIconColor;
            this.Controls.Add(btnClose);

            UpdateHeaderUI(); // Устанавливаем текст адреса

            // --- Статистика снизу ---
            lblStats = new Label { Text = "Waiting...", ForeColor = Theme.TextDim, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8), Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter, Height = 20 };
            this.Controls.Add(lblStats);

            // --- Пинг по центру ---
            lblPing = new Label { Text = "--", ForeColor = Theme.Text, BackColor = Color.Transparent, Font = new Font("Segoe UI", 22, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblPing);
            // Кнопки поверх пинга
            btnPin.BringToFront();
            btnClose.BringToFront();

            SetupContextMenu();
        }

        /// <summary>Скругляет углы всей плитки.</summary>
        private void ApplyRoundedCorners()
        {
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int r = CornerRadius;
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(this.Width - r, 0, r, r, 270, 90);
                path.AddArc(this.Width - r, this.Height - r, r, r, 0, 90);
                path.AddArc(0, this.Height - r, r, r, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }

        /// <summary>Скругляет углы панели-индикатора.</summary>
        private static void RoundPanel(Panel p, int radius)
        {
            if (p.Width <= 0 || p.Height <= 0) return;
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int r = radius * 2;
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(p.Width - r, 0, r, r, 270, 90);
                path.AddArc(p.Width - r, p.Height - r, r, r, 0, 90);
                path.AddArc(0, p.Height - r, r, r, 90, 90);
                path.CloseFigure();
                p.Region = new Region(path);
            }
        }

        private void SetupContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("✏ Переименовать", null, (s, e) => EditName());

            // --- Период статистики (вложенное меню) ---
            ToolStripMenuItem statsPeriod = new ToolStripMenuItem("📊 Период статистики");
            int[] periodValues = { 600, 1800, 3600, 10800, 21600, -1 };
            string[] periodNames = { "10 минут", "30 минут", "1 час", "3 часа", "6 часов", "С начала запуска" };
            for (int i = 0; i < periodValues.Length; i++)
            {
                int val = periodValues[i];
                string name = periodNames[i];
                var item = new ToolStripMenuItem(name) { Checked = (val == -1 ? _statsWindowSec == 0 : _statsWindowSec == val) };
                item.Click += (s, ev) => {
                    int sec = val == -1 ? 0 : val;   // 0 = с начала запуска
                    SetStatsWindowSec(sec);
                    foreach (ToolStripMenuItem x in ((ToolStripMenuItem)((ToolStripMenuItem)s).OwnerItem).DropDownItems)
                        x.Checked = false;
                    ((ToolStripMenuItem)s).Checked = true;
                };
                statsPeriod.DropDownItems.Add(item);
            }
            menu.Items.Add(statsPeriod);
            // -----------------------------------------

            menu.Items.Add("📄 Журнал событий", null, (s, e) => ShowLogWindow());
            menu.Items.Add("Открыть CMD (Ping -t)", null, (s, e) => { try { Process.Start("cmd.exe", $"/k ping {_address} -t"); } catch { } });
            menu.Items.Add("Trace Route", null, (s, e) => { try { Process.Start("cmd.exe", $"/k tracert {_address}"); } catch { } });
            var itemGraph = new ToolStripMenuItem("Показывать график") { Checked = _showGraph, CheckOnClick = true };
            itemGraph.Click += (s, e) => { _showGraph = itemGraph.Checked; Invalidate(); };
            menu.Items.Add(itemGraph);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Копировать адрес", null, (s, e) => Clipboard.SetText(_address));

            this.ContextMenuStrip = menu;
            foreach (Control c in this.Controls) if (c != btnClose) c.ContextMenuStrip = menu;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // График на всю плитку, на заднем фоне (как в прошлой версии)
            if (_showGraph && _pingValues.Count >= 2)
            {
                RectangleF bounds = new RectangleF(0, 0, this.Width, this.Height);
                int graphAlpha = Theme.IsDark ? 100 : 190;
                using (LinearGradientBrush brush = new LinearGradientBrush(
                       bounds,
                       Color.FromArgb(graphAlpha, _currentStatusColor), // Верх (поярче)
                       Color.Transparent,                              // Низ (прозрачно)
                       LinearGradientMode.Vertical))                   // Вертикально
                {
                    List<PointF> points = new List<PointF>();
                    points.Add(new PointF(0, this.Height));
                    float xStep = (float)this.Width / (_maxGraphPoints - 1);
                    long maxPing = 0;
                    lock (_statsLock) { if (_pingValues.Count > 0) maxPing = _pingValues.Max(); }
                    if (maxPing < 100) maxPing = 100;

                    long[] values;
                    lock (_statsLock) { values = _pingValues.ToArray(); }

                    for (int i = 0; i < values.Length; i++)
                    {
                        float y = this.Height - ((float)values[i] / maxPing * (this.Height - 30));
                        if (y < 30) y = 30;
                        points.Add(new PointF(i * xStep, y));
                    }
                    points.Add(new PointF((values.Length - 1) * xStep, this.Height));

                    if (points.Count > 2)
                    {
                        g.FillPolygon(brush, points.ToArray());
                        using (Pen pen = new Pen(Color.FromArgb(graphAlpha, _currentStatusColor), 1))
                            g.DrawLines(pen, points.GetRange(1, points.Count - 2).ToArray());
                    }
                }
            }

            // Светлая обводка по краю плитки (аккуратная рамка)
            using (Pen border = new Pen(Theme.Border, 1))
                g.DrawRectangle(border, 0, 0, this.Width - 1, this.Height - 1);
        }

        private async void StartPing()
        {
            _cts = new CancellationTokenSource();
            Ping pinger = new Ping();
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    bool success = false;
                    long rtt = 0;
                    try
                    {
                        PingReply reply = await pinger.SendPingAsync(_address, 5000);
                        if (reply.Status == IPStatus.Success) { success = true; rtt = reply.RoundtripTime; }
                        else { rtt = 0; }
                    }
                    catch { success = false; rtt = 0; }

                    lock (_statsLock) { UpdateStats(success, rtt); UpdateGraphData(success ? rtt : 5000); }
                    CheckAndLogState(success, rtt);
                    HandleAudioAlerts(success, rtt);
                    UpdateUI(success, rtt);
                    await Task.Delay(_settings.PingIntervalMs, _cts.Token);
                }
            }
            catch { }
        }

        private void HandleAudioAlerts(bool success, long rtt)
        {
            if ((DateTime.Now - _lastSoundTime).TotalSeconds < 10) return;
            bool played = false;

            if (!success && _settings.LossAlertEnabled)
            {
                if (_lastStateWasSuccess == true)
                {
                    AudioManager.PlaySound(_settings.LossSoundFile, _settings.LossVolume);
                    played = true;
                }
            }
            if (success && _settings.HighPingAlertEnabled && rtt > _settings.HighPingThreshold)
            {
                AudioManager.PlaySound(_settings.HighPingSoundFile, _settings.HighPingVolume);
                played = true;
            }
            if (played) _lastSoundTime = DateTime.Now;
        }

        private void CheckAndLogState(bool currentSuccess, long rtt)
        {
            if (_lastStateWasSuccess == null) { _lastStateWasSuccess = currentSuccess; return; }
            if (_lastStateWasSuccess != currentSuccess)
            {
                AddToLog(currentSuccess ? $"✅ Связь восстановлена (UP). Ping: {rtt}ms" : "⛔ Связь потеряна (DOWN).");
                _lastStateWasSuccess = currentSuccess;
            }
        }

        private void UpdateGraphData(long rtt)
        {
            _pingValues.Enqueue(rtt);
            if (_pingValues.Count > _maxGraphPoints) _pingValues.Dequeue();
            if (InvokeRequired) Invoke(new Action(() => Invalidate())); else Invalidate();
        }

        private void UpdateStatsLabel()
        {
            int recentLossCount = 0, totalCount = 0;
            lock (_statsLock) { recentLossCount = _history.Count(x => !x); totalCount = _history.Count; }
            double recentLossPercent = totalCount > 0 ? (double)recentLossCount / totalCount * 100 : 0;
            string periodName = PeriodName(_statsWindowSec);
            string statsText = $"Loss: {recentLossPercent:F1}% ({periodName})";
            if (InvokeRequired) { try { Invoke(new Action(() => lblStats.Text = statsText)); } catch { } return; }
            lblStats.Text = statsText;
        }

        private static string PeriodName(int sec)
        {
            if (sec <= 0) return "All";
            if (sec <= 600) return "10m";
            if (sec <= 1800) return "30m";
            if (sec <= 3600) return "1h";
            if (sec <= 10800) return "3h";
            if (sec <= 21600) return "6h";
            return "All";
        }

        private void UpdateStats(bool success, long rtt)
        {
            _totalPings++;
            if (!success) _lostPings++;
            else { if (rtt < 100) _statLt100++; else if (rtt < 200) _stat100to200++; else _statGt200++; }
            _history.Enqueue(success);
            if (_history.Count > _maxHistoryEntries) _history.Dequeue();
        }

        private void UpdateUI(bool success, long rtt)
        {
            if (IsDisposed) return;
            int recentLossCount = 0, totalCount = 0;
            lock (_statsLock) { recentLossCount = _history.Count(x => !x); totalCount = _history.Count; }
            double recentLossPercent = totalCount > 0 ? (double)recentLossCount / totalCount * 100 : 0;

            Color statusColor = Theme.Ok;
            if (!success) statusColor = Theme.Danger;
            else if (recentLossPercent > 20) statusColor = Theme.DangerSoft;
            else if (rtt > 100) statusColor = Theme.Warning;

            _currentStatusColor = statusColor;
            string periodName = PeriodName(_statsWindowSec);
            string statsText = $"Loss: {recentLossPercent:F1}% ({periodName})";

            if (InvokeRequired) { try { Invoke(new Action(() => UpdateUI(success, rtt))); } catch { } return; }

            if (!success)
            {
                lblPing.Text = "TIMEOUT";
                lblPing.Font = new Font("Segoe UI", 16, FontStyle.Bold); // Чуть меньше для ошибки
            }
            else
            {
                lblPing.Text = $"{rtt} ms";
                lblPing.Font = new Font("Segoe UI", 22, FontStyle.Bold); // Крупно для цифр
            }
            pnlStatusIndicator.BackColor = statusColor;
            lblStats.Text = statsText;
        }

        public void Stop() { _cts?.Cancel(); }
    }
}