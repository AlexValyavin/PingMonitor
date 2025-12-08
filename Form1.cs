using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMonitor
{
    public partial class Form1 : Form
    {
        private const int TileWidth = 240;
        private const int TileHeight = 110;
        private const int MarginSize = 10;

        // Добавляем новые контролы как поля класса, чтобы иметь к ним доступ
        private TextBox textBoxName;
        private CheckBox checkAlwaysOnTop;

        public Form1()
        {
            InitializeComponent();
            SetupFormDesign();

            // События нажатия Enter привяжем в SetupFormDesign
        }

        private void SetupFormDesign()
        {
            this.Text = "NetMonitor Pro";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application; // Стандартная иконка

            // --- ВЕРХНЯЯ ПАНЕЛЬ ---
            // Очищаем панель из дизайнера, будем добавлять кодом для точности (или настрой в дизайнере)
            // Но проще настроить существующие. Предполжим, у нас есть panel1, textBoxIP, buttonAdd

            // Настраиваем panel1
            panel1.Height = 50;
            panel1.BackColor = Color.FromArgb(45, 45, 48);
            panel1.Dock = DockStyle.Top;

            // 1. Поле IP (уже есть, просто настраиваем)
            textBoxIP.Width = 120;
            textBoxIP.Location = new Point(10, 12);
            // Placeholder/Hint можно сделать через WinAPI, но пока просто Label сверху
            // (оставим пока как есть)

            // 2. <--- НОВОЕ: Поле Имени
            textBoxName = new TextBox();
            textBoxName.Location = new Point(140, 12); // Справа от IP
            textBoxName.Width = 150;
            textBoxName.Font = textBoxIP.Font;
            textBoxName.Text = ""; // Пусто по умолчанию
            // Логика Enter для поля Имени тоже должна работать
            textBoxName.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    buttonAdd_Click(s, e);
                    e.Handled = true; e.SuppressKeyPress = true;
                }
            };
            panel1.Controls.Add(textBoxName);

            // Добавим подсказки (Labels) над полями, чтобы было понятно
            Label lblIpHint = new Label { Text = "IP / Host", ForeColor = Color.White, Location = new Point(10, 0), AutoSize = true, Font = new Font("Arial", 7) };
            Label lblNameHint = new Label { Text = "Имя (Опц.)", ForeColor = Color.White, Location = new Point(140, 0), AutoSize = true, Font = new Font("Arial", 7) };
            panel1.Controls.Add(lblIpHint);
            panel1.Controls.Add(lblNameHint);

            // 3. Кнопка Добавить (сдвигаем правее)
            buttonAdd.Location = new Point(300, 10);

            // 4. <--- НОВОЕ: Чекбокс "Поверх всех"
            // 4. Кнопка-тумблер "Поверх всех"
            checkAlwaysOnTop = new CheckBox();
            checkAlwaysOnTop.Appearance = Appearance.Button; // Превращаем галочку в кнопку
            checkAlwaysOnTop.Text = "📌 Поверх всех";
            checkAlwaysOnTop.TextAlign = ContentAlignment.MiddleCenter;
            checkAlwaysOnTop.AutoSize = false;
            checkAlwaysOnTop.Size = new Size(120, 28);

            // Прижимаем к правому краю (Anchor)
            // Начальная позиция: ширина панели минус ширина кнопки минус отступ
            checkAlwaysOnTop.Location = new Point(panel1.Width - 135, 12);
            checkAlwaysOnTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Стили
            checkAlwaysOnTop.FlatStyle = FlatStyle.Flat;
            checkAlwaysOnTop.FlatAppearance.BorderSize = 0;
            checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60); // Темно-серый по дефолту
            checkAlwaysOnTop.ForeColor = Color.LightGray;
            checkAlwaysOnTop.Cursor = Cursors.Hand;

            // Логика переключения цвета
            checkAlwaysOnTop.CheckedChanged += (s, e) => {
                this.TopMost = checkAlwaysOnTop.Checked;

                if (checkAlwaysOnTop.Checked)
                {
                    // Активное состояние
                    checkAlwaysOnTop.BackColor = Color.FromArgb(46, 204, 113); // Зеленый
                    checkAlwaysOnTop.ForeColor = Color.Black; // Черный текст для контраста
                    checkAlwaysOnTop.Text = "📌 ЗАКРЕПЛЕНО";
                }
                else
                {
                    // Неактивное состояние
                    checkAlwaysOnTop.BackColor = Color.FromArgb(60, 60, 60);
                    checkAlwaysOnTop.ForeColor = Color.LightGray;
                    checkAlwaysOnTop.Text = "📌 Поверх всех";
                }
            };
            panel1.Controls.Add(checkAlwaysOnTop);

            // --- ОСНОВНАЯ ОБЛАСТЬ ---
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30);
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Padding = new Padding(10);

            // Событие Enter для textBoxIP
            textBoxIP.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    // Если ввели IP и нажали Enter -> фокус на имя
                    textBoxName.Focus();
                    e.Handled = true; e.SuppressKeyPress = true;
                }
            };

            ResizeWindowToFit(4);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            // Передаем и IP, и Имя
            AddTile(textBoxIP.Text, textBoxName.Text);
        }

        // Обновленный метод AddTile принимает два аргумента
        private void AddTile(string ip, string alias)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            PingTile tile = new PingTile(ip, alias); // <--- Передаем в конструктор

            tile.RemoveRequested += (s, ev) => {
                tile.Stop();
                flowLayoutPanel1.Controls.Remove(tile);
                tile.Dispose();
                AdjustWindowSize();
            };

            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);

            // Очищаем оба поля
            textBoxIP.Clear();
            textBoxName.Clear();

            // Фокус обратно на IP для следующего ввода
            textBoxIP.Focus();

            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            // (Этот код оставляем без изменений из прошлого шага)
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
            // (Оставляем без изменений)
            int targetWidth = (TileWidth + MarginSize) * tilesCount + 50;
            int targetHeight = (TileHeight + MarginSize) * 2 + panel1.Height + 50;
            this.Size = new Size(targetWidth, targetHeight);
        }
    }
}