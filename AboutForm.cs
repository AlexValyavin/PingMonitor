using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection; // <--- Нужно для чтения версии и автора

namespace PingMonitor
{
    public class AboutForm : Form
    {
        // --- МАГИЯ ПЕРЕТАСКИВАНИЯ ---
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; }
        }

        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(550, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(1);

            // Получаем данные из Свойств проекта (которые ты заполнил)
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string copyright = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute))).Copyright;
            string company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute))).Company;

            // Заголовок окна
            Label lblTitleWindow = new Label
            {
                Text = "О программе",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitleWindow.MouseDown += DragWindow;
            this.Controls.Add(lblTitleWindow);

            // Кнопка Закрыть
            Button btnClose = new Button
            {
                Text = "Закрыть",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(46, 204, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            // Текстовое поле
            RichTextBox rtb = new RichTextBox();
            rtb.Dock = DockStyle.Fill;
            rtb.BackColor = Color.FromArgb(30, 30, 30);
            rtb.ForeColor = Color.LightGray;
            rtb.Font = new Font("Segoe UI", 9);
            rtb.BorderStyle = BorderStyle.None;
            rtb.ReadOnly = true;
            rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtb.SelectionIndent = 15;
            rtb.SelectionRightIndent = 15;

            // Формируем текст
            rtb.Text =
$@"PingMonitor
Версия: {version}
Разработчик: {company}
{copyright}

=== ОПИСАНИЕ ===
Компактный инструмент для мониторинга доступности сетевых узлов.
Заменяет множество окон ping -t.

=== ФУНКЦИОНАЛ ===
• Цветовая индикация (Зеленый/Оранжевый/Красный).
• Живые графики и детальная статистика (ПКМ -> Журнал).
• Уведомления звуком при сбоях. (включается в настройках).
• Поддержка шаблонов IP для быстрого ввода (Настройки -> Шаблоны).

=== УПРАВЛЕНИЕ ===
• Добавление: Введите IP или ID и нажмите Enter.
• Удаление: Нажмите крестик на плитке.
• Контекстное меню (ПКМ по плитке):
  - Журнал событий (Лог и детальная статистика).
  - Trace Route (Трассировка маршрута).
  - Вкл/Выкл графика.
  - Копировать адрес.

ШАБЛОНЫ (БЫСТРЫЙ ВВОД):
В настройках (⚙) на вкладке 'Шаблоны IP' можно создать маски.
Пример: '192.168.1.*' или '*.corp.local'.
При выборе шаблона на главном экране, символ '*' заменяется полем ввода. Вы вводите только меняющуюся часть (ID).

=== НАСТРОЙКИ ===
Нажмите ⚙ для настройки звуков, порога высокого пинга и управления шаблонами.

ОКНО:
Окно можно растягивать за края.
Кнопка 📌 закрепляет окно поверх всех остальных.

=== ЛИЦЕНЗИЯ ===
Данная программа распространяется БЕСПЛАТНО (Freeware).

1. Разрешено использование в личных и коммерческих целях (на работе).
2. ЗАПРЕЩЕНО продавать данную программу или её модификации.
3. Программа поставляется ""КАК ЕСТЬ"", без гарантий.

Если у вас есть идеи по улучшению — пишите разработчику!
alexval419@gmail.com";

            this.Controls.Add(rtb);
            rtb.BringToFront();

            this.Paint += (s, e) => { e.Graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1); };
        }
    }
}