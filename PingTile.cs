using System;
using System.Drawing;
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
        private readonly Color ColorBgNormal = Color.FromArgb(45, 45, 48);
        private readonly Color ColorTextMain = Color.White;
        private readonly Color ColorTextDim = Color.LightGray;

        private string _address;
        private string _alias; // <--- Вот переменная, которой не хватало
        private CancellationTokenSource _cts;
        private Queue<bool> _history = new Queue<bool>();
        private long _totalPings = 0;
        private long _lostPings = 0;

        public event EventHandler RemoveRequested;

        //private Label lblAddress;
        //private Label lblPing;
        //private Label lblStats;
        private Panel pnlStatusIndicator;
        private Label btnClose;

        // <--- Вот обновленный конструктор с ДВУМЯ аргументами
        public PingTile(string address, string alias = "")
        {
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

            pnlStatusIndicator = new Panel();
            pnlStatusIndicator.Dock = DockStyle.Top;
            pnlStatusIndicator.Height = 6;
            pnlStatusIndicator.BackColor = Color.Gray;
            this.Controls.Add(pnlStatusIndicator);

            btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.ForeColor = Color.Gray;
            btnClose.Font = new Font("Arial", 10, FontStyle.Bold);
            btnClose.AutoSize = true;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Location = new Point(this.Width - 25, 10);
            btnClose.Click += (s, e) => { _cts?.Cancel(); RemoveRequested?.Invoke(this, EventArgs.Empty); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Red;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            // --- Логика отображения Имени или IP ---
            lblAddress = new Label();
            lblAddress.ForeColor = ColorTextMain;
            lblAddress.AutoSize = false;
            lblAddress.TextAlign = ContentAlignment.MiddleCenter;
            lblAddress.Dock = DockStyle.Top;

            // Если есть псевдоним - показываем его
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
            // ---------------------------------------

            lblStats = new Label();
            lblStats.Text = "Waiting...";
            lblStats.ForeColor = ColorTextDim;
            lblStats.Font = new Font("Segoe UI", 8);
            lblStats.Dock = DockStyle.Bottom;
            lblStats.TextAlign = ContentAlignment.MiddleCenter;
            lblStats.Height = 25;
            this.Controls.Add(lblStats);

            lblPing = new Label();
            lblPing.Text = "--";
            lblPing.ForeColor = ColorTextMain;
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
            ToolStripMenuItem itemTracert = new ToolStripMenuItem("Trace Route (Tracert)");
            itemTracert.Click += (s, e) => {
                try { Process.Start("cmd.exe", $"/k tracert {_address}"); }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };
            menu.Items.Add(itemTracert);

            ToolStripMenuItem itemCopy = new ToolStripMenuItem("Копировать адрес");
            itemCopy.Click += (s, e) => Clipboard.SetText(_address);
            menu.Items.Add(itemCopy);

            this.ContextMenuStrip = menu;
            foreach (Control c in this.Controls)
            {
                if (c != btnClose) c.ContextMenuStrip = menu;
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
                    }
                    catch { success = false; }

                    UpdateStats(success);
                    UpdateUI(success, rtt);
                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch { }
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

            Color statusColor = Color.FromArgb(46, 204, 113);
            if (!success) statusColor = Color.FromArgb(231, 76, 60);
            else if (recentLossPercent > 20) statusColor = Color.FromArgb(243, 156, 18);
            else if (rtt > 100) statusColor = Color.FromArgb(241, 196, 15);

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