using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace PingMonitor
{
    public partial class PingTile : UserControl
    {
        // Настройки внешнего вида
        private const int TileWidth = 240;
        private const int TileHeight = 110;
        private readonly Color ColorBgNormal = Color.FromArgb(45, 45, 48); // Темно-серый
        private readonly Color ColorTextMain = Color.White;
        private readonly Color ColorTextDim = Color.LightGray;

        // Логика
        private string _address;
        private CancellationTokenSource _cts;
        private Queue<bool> _history = new Queue<bool>();
        private long _totalPings = 0;
        private long _lostPings = 0;

        // Событие, чтобы сообщить форме, что нас надо удалить
        public event EventHandler RemoveRequested;

        // Элементы управления (создаем кодом, чтобы не мучиться с дизайнером)
       // private Label lblAddress;
        //private Label lblPing;
        //private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose; // Крестик

        public PingTile(string address)
        {
            _address = address;
            InitializeCustomUI(); // Рисуем интерфейс
            StartPing();          // Запускаем пинг
        }

        private void InitializeCustomUI()
        {
            // 1. Настройки самой плитки
            this.Size = new Size(TileWidth, TileHeight);
            this.BackColor = ColorBgNormal;
            this.Margin = new Padding(5); // Отступы между плитками

            // 2. Индикатор статуса (полоска сверху)
            pnlStatusIndicator = new Panel();
            pnlStatusIndicator.Dock = DockStyle.Top;
            pnlStatusIndicator.Height = 6;
            pnlStatusIndicator.BackColor = Color.Gray; // Пока неизвестно
            this.Controls.Add(pnlStatusIndicator);

            // 3. Кнопка закрытия (Крестик)
            btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.ForeColor = Color.Gray;
            btnClose.Font = new Font("Arial", 10, FontStyle.Bold);
            btnClose.AutoSize = true;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Location = new Point(this.Width - 25, 10); // Правый верхний угол
            btnClose.Click += (s, e) => {
                _cts?.Cancel(); // Остановить пинг
                RemoveRequested?.Invoke(this, EventArgs.Empty); // Сообщить родителю
            };
            // Эффект наведения
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            // 4. Адрес (Заголовок)
            lblAddress = new Label();
            lblAddress.Text = _address;
            lblAddress.ForeColor = ColorTextMain;
            lblAddress.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblAddress.AutoSize = false;
            lblAddress.TextAlign = ContentAlignment.MiddleCenter;
            lblAddress.Dock = DockStyle.Top;
            lblAddress.Height = 30;
            // Добавляем отступ сверху, чтобы не наезжать на индикатор
            lblAddress.Padding = new Padding(0, 5, 0, 0);
            this.Controls.Add(lblAddress);

            // 5. Статистика (Снизу)
            lblStats = new Label();
            lblStats.Text = "Waiting...";
            lblStats.ForeColor = ColorTextDim;
            lblStats.Font = new Font("Segoe UI", 8);
            lblStats.Dock = DockStyle.Bottom;
            lblStats.TextAlign = ContentAlignment.MiddleCenter;
            lblStats.Height = 25;
            this.Controls.Add(lblStats);

            // 6. Значение Пинга (По центру)
            lblPing = new Label();
            lblPing.Text = "--";
            lblPing.ForeColor = ColorTextMain;
            lblPing.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            lblPing.Dock = DockStyle.Fill; // Занять все оставшееся место
            lblPing.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblPing);

            // Порядок слоев (чтобы Label не перекрывали друг друга)
            lblPing.BringToFront();
            btnClose.BringToFront();
        }

        // --- ВЕСЬ КОД PING ОСТАЕТСЯ ТЕМ ЖЕ, ЧТО БЫЛ ---
        // (Я скопирую основную часть для целостности, но логика та же)

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
                    }
                    catch { success = false; }

                    UpdateStats(success);
                    UpdateUI(success, rtt);
                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception) { }
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
            if (IsDisposed) return; // Защита от вылета при закрытии

            int recentLossCount = _history.Count(x => !x);
            double recentLossPercent = _history.Count > 0 ? (double)recentLossCount / _history.Count * 100 : 0;

            // Цвета (немного мягче)
            Color statusColor = Color.FromArgb(46, 204, 113); // Flat Green
            if (!success) statusColor = Color.FromArgb(231, 76, 60); // Flat Red
            else if (recentLossPercent > 20) statusColor = Color.FromArgb(243, 156, 18); // Flat Orange
            else if (rtt > 100) statusColor = Color.FromArgb(241, 196, 15); // Flat Yellow (High Ping)

            string statsText = $"Loss: {recentLossPercent:F1}% (10m)";

            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(() => UpdateUI(success, rtt))); } catch { }
                return;
            }

            lblPing.Text = success ? $"{rtt} ms" : "TIMEOUT";
            lblPing.ForeColor = success ? ColorTextMain : Color.FromArgb(231, 76, 60); // Красный текст если ошибка
            pnlStatusIndicator.BackColor = statusColor;
            lblStats.Text = statsText;
        }

        public void Stop() { _cts?.Cancel(); }
    }
}