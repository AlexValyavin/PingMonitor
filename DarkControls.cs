using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    // 1. ТЕМНЫЙ ВЫПАДАЮЩИЙ СПИСОК
    public class DarkComboBox : ComboBox
    {
        public DarkComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(60, 60, 60);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 122, 204)), e.Bounds);
            else
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), e.Bounds);

            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font,
                new Point(e.Bounds.X + 2, e.Bounds.Y + 2), Color.White);
        }
    }

    // 2. ТЕМНАЯ ГРУППА
    public class DarkGroupBox : GroupBox
    {
        public DarkGroupBox()
        {
            ForeColor = Color.White;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SizeF tSize = e.Graphics.MeasureString(Text, Font);
            Rectangle borderRect = ClientRectangle;
            borderRect.Y += (int)tSize.Height / 2;
            borderRect.Height -= (int)tSize.Height / 2;

            ControlPaint.DrawBorder(e.Graphics, borderRect, Color.FromArgb(100, 100, 100), ButtonBorderStyle.Solid);

            Rectangle textRect = new Rectangle(6, 0, (int)tSize.Width + 4, (int)tSize.Height);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), textRect);
            e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), 6, 0);
        }
    }

    // 3. ТЕМНЫЙ ПОЛЗУНОК (ИСПРАВЛЕН ФОН)
    public class DarkTrackBar : Control
    {
        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = 100;
        private int _value = 0;
        public int Value
        {
            get => _value;
            set { _value = Math.Max(Minimum, Math.Min(Maximum, value)); Invalidate(); }
        }

        public DarkTrackBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 25; // Чуть выше
            Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(30, 30, 30); // <--- ВАЖНО: Цвет фона как у окна
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Заливаем фон темным цветом, чтобы не было белых пятен
            e.Graphics.Clear(this.BackColor);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackY = this.Height / 2;

            // Серый трек (фон линии)
            using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 4))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawLine(pen, 10, trackY, this.Width - 10, trackY);
            }

            // Синий трек (заполнение)
            float percent = (float)(Value - Minimum) / (Maximum - Minimum);
            int thumbX = (int)(10 + percent * (this.Width - 20));

            using (Pen pen = new Pen(Color.FromArgb(0, 122, 204), 4))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawLine(pen, 10, trackY, thumbX, trackY);
            }

            // Ползунок
            int thumbSize = 14;
            g.FillEllipse(Brushes.White, thumbX - thumbSize / 2, trackY - thumbSize / 2, thumbSize, thumbSize);
        }

        protected override void OnMouseDown(MouseEventArgs e) { UpdateValueFromMouse(e.X); }
        protected override void OnMouseMove(MouseEventArgs e) { if (e.Button == MouseButtons.Left) UpdateValueFromMouse(e.X); }

        private void UpdateValueFromMouse(int mouseX)
        {
            float percent = (float)(mouseX - 10) / (this.Width - 20);
            int newVal = Minimum + (int)(percent * (Maximum - Minimum));
            Value = newVal;
        }
    }

    // 4. ТЕМНЫЙ NUMERIC (НОВОЕ)
    public class DarkNumeric : NumericUpDown
    {
        public DarkNumeric()
        {
            BackColor = Color.FromArgb(60, 60, 60);
            ForeColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
        }
        // NumericUpDown сложный контрол, но установка BackColor в конструкторе обычно помогает для текстовой части
    }
}