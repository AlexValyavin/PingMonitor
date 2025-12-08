using System;
using System.Drawing;
using System.Drawing.Drawing2D; // Нужно для графики
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace PingMonitor
{
    public partial class PingTile : UserControl
    {
        private const int TileWidth = 240;
        private const int TileHeight = 110;

        // Цвета
        private readonly Color ColorBgNormal = Color.FromArgb(45, 45, 48);
        private readonly Color ColorTextMain = Color.White;
        private readonly Color ColorTextDim = Color.LightGray;

        // Данные
        private string _address;
        private string _alias;
        private CancellationTokenSource _cts;

        // История для статистики (успех/неуспех)
        private Queue<bool> _history = new Queue<bool>();
        // История для ГРАФИКА (значения пинга)
        private Queue<long> _pingValues = new Queue<long>();
        private const int MaxGraphPoints = 50; // Сколько точек храним для графика

        private long _totalPings = 0;
        private long _lostPings = 0;

        // Настройки отображения
        private bool _showGraph = true; // Показывать ли график
        private Color _currentStatusColor = Color.LimeGreen; // Текущий цвет статуса

        public event EventHandler RemoveRequested;

        // Элементы UI
       // private Label lblAddress;
        //private Label lblPing;
        //private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose;

        // Свойства для сохранения (на будущее)
        public string Address => _address;
        public string Alias => _alias;

        public PingTile(string address, string alias = "")
        {
            // Включаем двойную буферизацию, чтобы график не мерцал
            this.DoubleBuffered = true;

            _address = address;
            _alias = alias;

            InitializeCustomUI();
            StartPing();
        }

        private void InitializeCustomUI()
        {
            this.Size = new Size(TileWidth, TileHeight);
            this.BackColor = ColorBgNormal;
            this.Margin = new Padding(5);

            // 1. Полоска статуса сверху
            pnlStatusIndicator = new Panel();
            pnlStatusIndicator.Dock = DockStyle.Top;
            pnlStatusIndicator.Height = 6;
            pnlStatusIndicator.BackColor = Color.Gray;
            this.Controls.Add(pnlStatusIndicator);

            // 2. Кнопка закрытия
            btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.ForeColor = Color.Gray;
            btnClose.BackColor = Color.Transparent; // Важно для графика
            btnClose.Font = new Font("Arial", 10, FontStyle.Bold);
            btnClose.AutoSize = true;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Location = new Point(this.Width - 25, 10);
            btnClose.Click += (s, e) => { _cts?.Cancel(); RemoveRequested?.Invoke(this, EventArgs.Empty); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            // 3. Заголовок (Имя/IP)
            lblAddress = new Label();
            lblAddress.ForeColor = ColorTextMain;
            lblAddress.BackColor = Color.Transparent; // Прозрачный фон!
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

            // 4. Статистика (снизу)
            lblStats = new Label();
            lblStats.Text = "Waiting...";
            lblStats.ForeColor = ColorTextDim;
            lblStats.BackColor = Color.Transparent; // Прозрачный фон!
            lblStats.Font = new Font("Segoe UI", 8);
            lblStats.Dock = DockStyle.Bottom;
            lblStats.TextAlign = ContentAlignment.MiddleCenter;
            lblStats.Height = 25;
            this.Controls.Add(lblStats);

            // 5. Значение пинга (по центру)
            lblPing = new Label();
            lblPing.Text = "--";
            lblPing.ForeColor = ColorTextMain;
            lblPing.BackColor = Color.Transparent; // Прозрачный фон!
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

            // Пункт Tracert
            ToolStripMenuItem itemTracert = new ToolStripMenuItem("Trace Route (Tracert)");
            itemTracert.Click += (s, e) => {
                try { Process.Start("cmd.exe", $"/k tracert {_address}"); }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };
            menu.Items.Add(itemTracert);

            // Пункт переключения графика
            ToolStripMenuItem itemToggleGraph = new ToolStripMenuItem("Показывать график");
            itemToggleGraph.Checked = _showGraph;
            itemToggleGraph.CheckOnClick = true;
            itemToggleGraph.Click += (s, e) => {
                _showGraph = itemToggleGraph.Checked;
                this.Invalidate(); // Перерисовать плитку
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

        // === ГЛАВНАЯ МАГИЯ: Рисуем график ===
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); // Рисуем стандартный фон

            if (!_showGraph || _pingValues.Count < 2) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Создаем полупрозрачную кисть текущего цвета
            // 40 - это альфа-канал (прозрачность от 0 до 255)
            using (Brush brush = new SolidBrush(Color.FromArgb(40, _currentStatusColor)))
            {
                // 2. Рассчитываем координаты точек
                List<PointF> points = new List<PointF>();

                // Стартовая точка в левом нижнем углу
                points.Add(new PointF(0, this.Height));

                float xStep = (float)this.Width / (MaxGraphPoints - 1);

                // Нормализация по высоте (чтобы график влезал)
                // Ищем максимум на графике, но не меньше 50мс (иначе график 1мс будет на весь экран)
                long maxPing = _pingValues.Max();
                if (maxPing < 50) maxPing = 50;

                int i = 0;
                foreach (long val in _pingValues)
                {
                    float x = i * xStep;
                    // Чем больше пинг, тем меньше Y (ближе к верху), но не выше шапки (30px)
                    // Оставляем 30px сверху для заголовка
                    float availableHeight = this.Height - 30;
                    float y = this.Height - ((float)val / maxPing * availableHeight);

                    points.Add(new PointF(x, y));
                    i++;
                }

                // Финальная точка в правом нижнем углу
                points.Add(new PointF((_pingValues.Count - 1) * xStep, this.Height));

                // 3. Рисуем полигон (закрашенную область)
                if (points.Count > 2)
                {
                    g.FillPolygon(brush, points.ToArray());

                    // Опционально: можно нарисовать тонкую линию сверху поярче
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
                            // Если таймаут, считаем пинг как максимальный (например 2000), 
                            // чтобы график подскочил вверх
                            rtt = 2000;
                        }
                    }
                    catch { success = false; rtt = 2000; }

                    UpdateStats(success);
                    UpdateGraphData(rtt); // <--- Добавляем данные в график
                    UpdateUI(success, rtt);

                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch { }
        }

        private void UpdateGraphData(long rtt)
        {
            _pingValues.Enqueue(rtt);
            if (_pingValues.Count > MaxGraphPoints) _pingValues.Dequeue();

            // Вызываем перерисовку (OnPaint)
            // Invoke не нужен, так как Invalidate потокобезопасен (обычно), 
            // но для надежности сделаем через Invoke
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.Invalidate()));
            }
            else
            {
                this.Invalidate();
            }
        }

        private void UpdateStats(bool success)
        {
            _totalPings++;
            if (!success) _lostPings++;
            _history.Enqueue(success);
            if (_history.Count > 600) _history.Dequeue();
        }

        private void UpdateUI(bool success, long rtt)
        {
            if (IsDisposed) return;
            int recentLossCount = _history.Count(x => !x);
            double recentLossPercent = _history.Count > 0 ? (double)recentLossCount / _history.Count * 100 : 0;

            Color statusColor = Color.FromArgb(46, 204, 113); // Green
            if (!success) statusColor = Color.FromArgb(231, 76, 60); // Red
            else if (recentLossPercent > 20) statusColor = Color.FromArgb(243, 156, 18); // Orange
            else if (rtt > 100) statusColor = Color.FromArgb(241, 196, 15); // Yellow

            // Сохраняем цвет для графика
            _currentStatusColor = statusColor;

            string statsText = $"Loss: {recentLossPercent:F1}% (10m)";

            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(() => UpdateUI(success, rtt))); } catch { }
                return;
            }

            // Если был таймаут, пишем текст, иначе пинг
            // (rtt мы ставили 2000 для графика, но в текст выводим красиво)
            lblPing.Text = success ? $"{rtt} ms" : "TIMEOUT";
            lblPing.ForeColor = success ? ColorTextMain : Color.FromArgb(231, 76, 60);

            pnlStatusIndicator.BackColor = statusColor;
            lblStats.Text = statsText;

            // Invalidate вызывается в UpdateGraphData, так что здесь не обязательно
        }

        public void Stop() { _cts?.Cancel(); }
    }
}