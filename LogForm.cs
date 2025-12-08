using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PingMonitor
{
    public class LogForm : Form
    {
        private TextBox txtLog;
        private Button btnSave;
        private Button btnClose;
        private string _hostName;

        public LogForm(string hostName, string logContent)
        {
            _hostName = hostName;
            InitializeComponent();
            txtLog.Text = string.IsNullOrEmpty(logContent) ? "Событий пока нет." : logContent;
            // Прокрутка в конец
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void InitializeComponent()
        {
            this.Text = $"Журнал событий: {_hostName}";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Information;

            // Поле текста
            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.Dock = DockStyle.Top;
            txtLog.Height = 310;
            txtLog.Font = new Font("Consolas", 9); // Моноширинный шрифт для красоты
            txtLog.BackColor = Color.White;

            // Кнопка Сохранить
            btnSave = new Button();
            btnSave.Text = "Сохранить в файл";
            btnSave.Location = new Point(10, 325);
            btnSave.Width = 150;
            btnSave.Click += BtnSave_Click;

            // Кнопка Закрыть
            btnClose = new Button();
            btnClose.Text = "Закрыть";
            btnClose.Location = new Point(370, 325);
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(txtLog);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = $"Log_{_hostName}_{DateTime.Now:yyyyMMdd}.txt";
            sfd.Filter = "Text Files|*.txt";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, txtLog.Text);
                    MessageBox.Show("Лог успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения: " + ex.Message);
                }
            }
        }
    }
}