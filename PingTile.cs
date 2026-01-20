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
        private const int TileHeight = 110;

        private readonly Color ColorBgNormal = Color.FromArgb(45, 45, 48);
        private readonly Color ColorTextMain = Color.White;
        private readonly Color ColorTextDim = Color.LightGray;

        private string _address;
        private string _alias;
        private AppSettings _settings;
        private DateTime _lastSoundTime = DateTime.MinValue;

        private CancellationTokenSource _cts;
        private object _statsLock = new object();

        private Queue<bool> _history = new Queue<bool>();
        private Queue<long> _pingValues = new Queue<long>();
        private const int MaxGraphPoints = 50;
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

        public event EventHandler RemoveRequested;

        private Label lblAddress;
        private Label lblPing;
        private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose;

        public string Address => _address;
        public string Alias => _alias;

        public PingTile(string address, string alias, AppSettings settings)
        {
            this.DoubleBuffered = true;
            _address = address;
            _alias = alias;
            _settings = settings;

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
            if (c != btnClose && c != lblAddress) // lblAddress исключаем, у него своя логика DoubleClick, но drag тоже нужен
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
                lblAddress.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            }
            else
            {
                lblAddress.Text = _address;
                lblAddress.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            }
        }
        // -----------------------------

        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
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
            this.BackColor = ColorBgNormal;
            this.Margin = new Padding(5);

            pnlStatusIndicator = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Gray };
            this.Controls.Add(pnlStatusIndicator);

            btnClose = new Label { Text = "✕", ForeColor = Color.Gray, BackColor = Color.Transparent, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true, Cursor = Cursors.Hand, Location = new Point(this.Width - 25, 10) };
            btnClose.Click += (s, e) => { _cts?.Cancel(); RemoveRequested?.Invoke(this, EventArgs.Empty); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            lblAddress = new Label { ForeColor = ColorTextMain, BackColor = Color.Transparent, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Padding = new Padding(0, 5, 0, 0) };

            // --- ПОДПИСКА НА ДВОЙНОЙ КЛИК ---
            lblAddress.DoubleClick += (s, e) => EditName();
            // --------------------------------

            UpdateHeaderUI(); // Устанавливаем текст
            this.Controls.Add(lblAddress);

            lblStats = new Label { Text = "Waiting...", ForeColor = ColorTextDim, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8), Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter, Height = 25 };
            this.Controls.Add(lblStats);

            lblPing = new Label { Text = "--", ForeColor = ColorTextMain, BackColor = Color.Transparent, Font = new Font("Segoe UI", 22, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblPing);
            lblPing.BringToFront();
            btnClose.BringToFront();

            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            // Добавим пункт переименования и в меню
            menu.Items.Add("✏ Переименовать", null, (s, e) => EditName());
            menu.Items.Add(new ToolStripSeparator());

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
            if (!_showGraph || _pingValues.Count < 2) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Brush brush = new SolidBrush(Color.FromArgb(40, _currentStatusColor)))
            {
                List<PointF> points = new List<PointF>();
                points.Add(new PointF(0, this.Height));
                float xStep = (float)this.Width / (MaxGraphPoints - 1);
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
                    using (Pen pen = new Pen(Color.FromArgb(100, _currentStatusColor), 1))
                        g.DrawLines(pen, points.GetRange(1, points.Count - 2).ToArray());
                }
            }
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
                    await Task.Delay(1000, _cts.Token);
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
            if (_pingValues.Count > MaxGraphPoints) _pingValues.Dequeue();
            if (InvokeRequired) Invoke(new Action(() => Invalidate())); else Invalidate();
        }

        private void UpdateStats(bool success, long rtt)
        {
            _totalPings++;
            if (!success) _lostPings++;
            else { if (rtt < 100) _statLt100++; else if (rtt < 200) _stat100to200++; else _statGt200++; }
            _history.Enqueue(success);
            if (_history.Count > 600) _history.Dequeue();
        }

        private void UpdateUI(bool success, long rtt)
        {
            if (IsDisposed) return;
            int recentLossCount = 0, totalCount = 0;
            lock (_statsLock) { recentLossCount = _history.Count(x => !x); totalCount = _history.Count; }
            double recentLossPercent = totalCount > 0 ? (double)recentLossCount / totalCount * 100 : 0;

            Color statusColor = Color.FromArgb(46, 204, 113);
            if (!success) statusColor = Color.FromArgb(231, 76, 60);
            else if (recentLossPercent > 20) statusColor = Color.FromArgb(243, 156, 18);
            else if (rtt > 100) statusColor = Color.FromArgb(241, 196, 15);

            _currentStatusColor = statusColor;
            string statsText = $"Loss: {recentLossPercent:F1}% (10m)";

            if (InvokeRequired) { try { Invoke(new Action(() => UpdateUI(success, rtt))); } catch { } return; }

            lblPing.Text = success ? $"{rtt} ms" : "TIMEOUT";
            lblPing.ForeColor = success ? ColorTextMain : Color.FromArgb(231, 76, 60);
            pnlStatusIndicator.BackColor = statusColor;
            lblStats.Text = statsText;
        }

        public void Stop() { _cts?.Cancel(); }
    }
}