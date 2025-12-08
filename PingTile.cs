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
        private CancellationTokenSource _cts;

        // --- СТАТИСТИКА И ДАННЫЕ ---
        private object _statsLock = new object();

        private Queue<bool> _history = new Queue<bool>();
        private Queue<long> _pingValues = new Queue<long>();
        private const int MaxGraphPoints = 50;

        private List<string> _logEvents = new List<string>();
        private const int MaxLogEntries = 1000;
        private bool? _lastStateWasSuccess = null;

        // Основные счетчики
        private long _totalPings = 0;
        private long _lostPings = 0;

        // Счетчики распределения задержек (Buckets)
        private long _statLt100 = 0;     // < 100 ms
        private long _stat100to200 = 0;  // 100 - 200 ms
        private long _statGt200 = 0;     // > 200 ms
                                         // ---------------------------

        private bool _showGraph = true;
        private Color _currentStatusColor = Color.LimeGreen;

        public event EventHandler RemoveRequested;

        //private Label lblAddress;
        //private Label lblPing;
        //private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose;

        public string Address => _address;
        public string Alias => _alias;

        public PingTile(string address, string alias = "")
        {
            this.DoubleBuffered = true;
            _address = address;
            _alias = alias;

            AddToLog("Мониторинг запущен");
            InitializeCustomUI();
            StartPing();
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

        // --- ГЕНЕРАЦИЯ ОТЧЕТА ---
        private void ShowLogWindow()
        {
            StringBuilder sb = new StringBuilder();

            long total = 0;
            long lost = 0;
            int recentTotal = 0;
            int recentLost = 0;

            // Переменные для гистограммы
            long cntLt100 = 0;
            long cnt100to200 = 0;
            long cntGt200 = 0;

            List<string> logsCopy = new List<string>();

            lock (_statsLock)
            {
                total = _totalPings;
                lost = _lostPings;

                recentTotal = _history.Count;
                recentLost = _history.Count(x => !x);

                cntLt100 = _statLt100;
                cnt100to200 = _stat100to200;
                cntGt200 = _statGt200;

                logsCopy.AddRange(_logEvents);
            }

            // Математика общая
            double totalLossPct = total > 0 ? (double)lost / total * 100 : 0;
            double totalUptime = 100 - totalLossPct;
            long successTotal = total - lost; // Общее кол-во успешных пакетов

            // Математика Recent
            double recentLossPct = recentTotal > 0 ? (double)recentLost / recentTotal * 100 : 0;
            double recentUptime = 100 - recentLossPct;

            // Математика Задержек (считаем процент от УСПЕШНЫХ пакетов)
            double pctLt100 = successTotal > 0 ? (double)cntLt100 / successTotal * 100 : 0;
            double pct100to200 = successTotal > 0 ? (double)cnt100to200 / successTotal * 100 : 0;
            double pctGt200 = successTotal > 0 ? (double)cntGt200 / successTotal * 100 : 0;

            string name = !string.IsNullOrEmpty(_alias) ? _alias : _address;

            sb.AppendLine($"ОТЧЕТ МОНИТОРИНГА: {name}");
            sb.AppendLine($"Адрес: {_address}");
            sb.AppendLine(new string('=', 50));

            sb.AppendLine("ОБЩАЯ СТАТИСТИКА (All Time):");
            sb.AppendLine($"• Всего пакетов:      {total}");
            sb.AppendLine($"• Потеряно:           {lost}");
            sb.AppendLine($"• Процент потерь:     {totalLossPct:F2}%");
            sb.AppendLine($"• Стабильность (Up):  {totalUptime:F2}%");
            sb.AppendLine();

            sb.AppendLine("РАСПРЕДЕЛЕНИЕ ЗАДЕРЖЕК (Latency Distribution):");
            sb.AppendLine($"• Быстро (< 100ms):       {cntLt100}\t({pctLt100:F1}%)");
            sb.AppendLine($"• Средне (100-200ms):     {cnt100to200}\t({pct100to200:F1}%)");
            sb.AppendLine($"• Медленно (> 200ms):     {cntGt200}\t({pctGt200:F1}%)");
            sb.AppendLine("* Проценты от успешных пакетов");
            sb.AppendLine();

            sb.AppendLine("ПОСЛЕДНИЕ 10 МИНУТ (Recent):");
            sb.AppendLine($"• Потерь за 10 мин:   {recentLost} из {recentTotal}");
            sb.AppendLine($"• Текущая стабильность: {recentUptime:F2}%");

            sb.AppendLine(new string('=', 50));
            sb.AppendLine("ЖУРНАЛ СОБЫТИЙ:");
            sb.AppendLine();

            foreach (var line in logsCopy)
            {
                sb.AppendLine(line);
            }

            LogForm form = new LogForm(name, sb.ToString());
            form.ShowDialog();
        }

        private void InitializeCustomUI()
        {
            this.Size = new Size(TileWidth, TileHeight);
            this.BackColor = ColorBgNormal;
            this.Margin = new Padding(5);

            pnlStatusIndicator = new Panel();
            pnlStatusIndicator.Dock = DockStyle.Top;
            pnlStatusIndicator.Height = 6;
            pnlStatusIndicator.BackColor = Color.Gray;
            this.Controls.Add(pnlStatusIndicator);

            btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.ForeColor = Color.Gray;
            btnClose.BackColor = Color.Transparent;
            btnClose.Font = new Font("Arial", 10, FontStyle.Bold);
            btnClose.AutoSize = true;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Location = new Point(this.Width - 25, 10);
            btnClose.Click += (s, e) => { _cts?.Cancel(); RemoveRequested?.Invoke(this, EventArgs.Empty); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            lblAddress = new Label();
            lblAddress.ForeColor = ColorTextMain;
            lblAddress.BackColor = Color.Transparent;
            lblAddress.AutoSize = false;
            lblAddress.TextAlign = ContentAlignment.MiddleCenter;
            lblAddress.Dock = DockStyle.Top;

            if (!string.IsNullOrEmpty(_alias))
            {
                lblAddress.Text = _alias;
                lblAddress.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                lblAddress.Height = 25;
            }
            else
            {
                lblAddress.Text = _address;
                lblAddress.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                lblAddress.Height = 30;
            }
            lblAddress.Padding = new Padding(0, 5, 0, 0);
            this.Controls.Add(lblAddress);

            lblStats = new Label();
            lblStats.Text = "Waiting...";
            lblStats.ForeColor = ColorTextDim;
            lblStats.BackColor = Color.Transparent;
            lblStats.Font = new Font("Segoe UI", 8);
            lblStats.Dock = DockStyle.Bottom;
            lblStats.TextAlign = ContentAlignment.MiddleCenter;
            lblStats.Height = 25;
            this.Controls.Add(lblStats);

            lblPing = new Label();
            lblPing.Text = "--";
            lblPing.ForeColor = ColorTextMain;
            lblPing.BackColor = Color.Transparent;
            lblPing.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            lblPing.Dock = DockStyle.Fill;
            lblPing.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblPing);

            lblPing.BringToFront();
            btnClose.BringToFront();

            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem itemLog = new ToolStripMenuItem("📄 Журнал событий и Статистика");
            itemLog.Click += (s, e) => ShowLogWindow();
            menu.Items.Add(itemLog);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem itemTracert = new ToolStripMenuItem("Trace Route (Tracert)");
            itemTracert.Click += (s, e) => {
                try { Process.Start("cmd.exe", $"/k tracert {_address}"); }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };
            menu.Items.Add(itemTracert);

            ToolStripMenuItem itemToggleGraph = new ToolStripMenuItem("Показывать график");
            itemToggleGraph.Checked = _showGraph;
            itemToggleGraph.CheckOnClick = true;
            itemToggleGraph.Click += (s, e) => {
                _showGraph = itemToggleGraph.Checked;
                this.Invalidate();
            };
            menu.Items.Add(itemToggleGraph);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem itemCopy = new ToolStripMenuItem("Копировать адрес");
            itemCopy.Click += (s, e) => Clipboard.SetText(_address);
            menu.Items.Add(itemCopy);

            this.ContextMenuStrip = menu;
            foreach (Control c in this.Controls)
            {
                if (c != btnClose) c.ContextMenuStrip = menu;
            }
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

                lock (_statsLock)
                {
                    if (_pingValues.Count > 0) maxPing = _pingValues.Max();
                }

                if (maxPing < 50) maxPing = 50;

                long[] values;
                lock (_statsLock) { values = _pingValues.ToArray(); }

                int i = 0;
                foreach (long val in values)
                {
                    float x = i * xStep;
                    float availableHeight = this.Height - 30;
                    float y = this.Height - ((float)val / maxPing * availableHeight);
                    points.Add(new PointF(x, y));
                    i++;
                }
                points.Add(new PointF((values.Length - 1) * xStep, this.Height));

                if (points.Count > 2)
                {
                    g.FillPolygon(brush, points.ToArray());
                    using (Pen pen = new Pen(Color.FromArgb(100, _currentStatusColor), 1))
                    {
                        g.DrawLines(pen, points.GetRange(1, points.Count - 2).ToArray());
                    }
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
                        PingReply reply = await pinger.SendPingAsync(_address, 2000);
                        if (reply.Status == IPStatus.Success)
                        {
                            success = true;
                            rtt = reply.RoundtripTime;
                        }
                        else
                        {
                            rtt = 2000;
                        }
                    }
                    catch { success = false; rtt = 2000; }

                    lock (_statsLock)
                    {
                        UpdateStats(success, rtt); // <-- Передаем RTT для статистики
                        UpdateGraphData(rtt);
                    }

                    CheckAndLogState(success, rtt);
                    UpdateUI(success, rtt);

                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch { }
        }

        private void CheckAndLogState(bool currentSuccess, long rtt)
        {
            if (_lastStateWasSuccess == null)
            {
                _lastStateWasSuccess = currentSuccess;
                if (!currentSuccess) AddToLog($"⚠ Инициализация: Узел недоступен!");
                else AddToLog($"✅ Инициализация: Узел доступен. Ping: {rtt} ms");
                return;
            }

            if (_lastStateWasSuccess != currentSuccess)
            {
                if (currentSuccess) AddToLog($"✅ Связь восстановлена (UP). Ping: {rtt} ms");
                else AddToLog($"⛔ Связь потеряна (DOWN). Timeout.");

                _lastStateWasSuccess = currentSuccess;
            }
        }

        private void UpdateGraphData(long rtt)
        {
            _pingValues.Enqueue(rtt);
            if (_pingValues.Count > MaxGraphPoints) _pingValues.Dequeue();

            if (this.InvokeRequired) this.Invoke(new Action(() => this.Invalidate()));
            else this.Invalidate();
        }

        // --- ОБНОВЛЕННЫЙ МЕТОД СБОРА СТАТИСТИКИ ---
        private void UpdateStats(bool success, long rtt)
        {
            _totalPings++;
            if (!success)
            {
                _lostPings++;
            }
            else
            {
                // Считаем распределение задержек только для успешных
                if (rtt < 100) _statLt100++;
                else if (rtt < 200) _stat100to200++;
                else _statGt200++;
            }

            _history.Enqueue(success);
            if (_history.Count > 600) _history.Dequeue();
        }

        private void UpdateUI(bool success, long rtt)
        {
            if (IsDisposed) return;

            int recentLossCount = 0;
            int totalCount = 0;

            lock (_statsLock)
            {
                recentLossCount = _history.Count(x => !x);
                totalCount = _history.Count;
            }

            double recentLossPercent = totalCount > 0 ? (double)recentLossCount / totalCount * 100 : 0;

            Color statusColor = Color.FromArgb(46, 204, 113);
            if (!success) statusColor = Color.FromArgb(231, 76, 60);
            else if (recentLossPercent > 20) statusColor = Color.FromArgb(243, 156, 18);
            else if (rtt > 100) statusColor = Color.FromArgb(241, 196, 15);

            _currentStatusColor = statusColor;
            string statsText = $"Loss: {recentLossPercent:F1}% (10m)";

            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(() => UpdateUI(success, rtt))); } catch { }
                return;
            }

            lblPing.Text = success ? $"{rtt} ms" : "TIMEOUT";
            lblPing.ForeColor = success ? ColorTextMain : Color.FromArgb(231, 76, 60);

            pnlStatusIndicator.BackColor = statusColor;
            lblStats.Text = statsText;
        }

        public void Stop() { _cts?.Cancel(); }
    }
}