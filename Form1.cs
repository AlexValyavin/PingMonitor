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
        // Константы для расчета размера
        private const int TileWidth = 240;  // Должно совпадать с PingTile
        private const int TileHeight = 110;
        private const int MarginSize = 10;  // Отступ (Padding/Margin)
        public Form1()
        {
            InitializeComponent();
            SetupFormDesign();
            
            // <--- НОВОЕ: Подписываемся на нажатие клавиш в поле ввода
            textBoxIP.KeyDown += TextBoxIP_KeyDown;
        }

        // <--- НОВОЕ: Логика нажатия Enter
        private void TextBoxIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Эмулируем нажатие кнопки "Добавить"
                buttonAdd_Click(sender, e);

                // Убираем звук "дин" и предотвращаем дальнейшую обработку
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SetupFormDesign()
        {
            // 1. Настройка самого окна
            this.Text = "PingMonitor";
            this.BackColor = Color.FromArgb(30, 30, 30); // Очень темный фон (почти черный)
            this.StartPosition = FormStartPosition.CenterScreen;

            // 2. Настройка контейнера FlowLayoutPanel
            // Важно: Убедись в дизайнере, что flowLayoutPanel1 имеет Dock = Fill
            // или используй код ниже:
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.BackColor = Color.FromArgb(30, 30, 30);
            flowLayoutPanel1.AutoScroll = true; // Включает скролл, если места мало
            flowLayoutPanel1.Padding = new Padding(10);

            // Задаем начальный размер (например под 4 плитки в ширину и 2 в высоту)
            ResizeWindowToFit(4);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            AddTile(textBoxIP.Text);
        }

        private void AddTile(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            PingTile tile = new PingTile(ip);

            // Подписываемся на событие закрытия плитки
            tile.RemoveRequested += (s, ev) => {
                tile.Stop();
                flowLayoutPanel1.Controls.Remove(tile);
                tile.Dispose();
                AdjustWindowSize(); // Пересчитать размер окна при удалении
            };

            // Добавляем плитку В НАЧАЛО (SetChildIndex), чтобы новые были сверху
            flowLayoutPanel1.Controls.Add(tile);
            flowLayoutPanel1.Controls.SetChildIndex(tile, 0);

            textBoxIP.Clear();
            textBoxIP.Focus(); // Возвращаем курсор в поле ввода

            AdjustWindowSize();
        }

        private void AdjustWindowSize()
        {
            int count = flowLayoutPanel1.Controls.Count;
            if (count == 0) return;

            // Логика:
            // Пытаемся вместить максимум 4 плитки в ширину.
            // Если плиток меньше 4, ширина окна подстраивается под них.
            // Высота растет, пока влезает в экран.

            int cols = Math.Min(count, 4); // Максимум 4 колонки
            // Вычисляем ряды (округление вверх)
            int rows = (int)Math.Ceiling((double)count / 4);

            // Расчет идеальной ширины и высоты
            // (Ширина плитки + Отступ) * Кол-во + Скроллбар(25) + Рамки окна(20)
            int targetWidth = (TileWidth + MarginSize) * cols + 40 + flowLayoutPanel1.Padding.Horizontal;
            int targetHeight = (TileHeight + MarginSize) * rows + panel1.Height + 50 + flowLayoutPanel1.Padding.Vertical;

            // Ограничение по размеру экрана пользователя
            Rectangle screen = Screen.FromControl(this).WorkingArea;

            // Не больше 90% экрана
            int maxWidth = screen.Width;
            int maxHeight = (int)(screen.Height * 0.9);

            // Применяем размеры, но не меньше минимальных
            this.Width = Math.Min(targetWidth, maxWidth);
            this.Height = Math.Min(targetHeight, maxHeight);
        }

        // Вспомогательный метод для старта
        private void ResizeWindowToFit(int tilesCount)
        {
            // Эмуляция размеров для старта
            int targetWidth = (TileWidth + MarginSize) * tilesCount + 50;
            int targetHeight = (TileHeight + MarginSize) * 2 + panel1.Height + 50;
            this.Size = new Size(targetWidth, targetHeight);
        }
        private void RunTracert(string ip)
        {
            // Запуск стандартного tracert в отдельном окне cmd, которое не исчезнет само (/k)
            System.Diagnostics.Process.Start("cmd.exe", $"/k tracert {ip}");
        }
    }
}