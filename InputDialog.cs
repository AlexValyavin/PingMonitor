using System;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    public static class InputDialog
    {
        public static string Show(string title, string promptText, string defaultValue = "")
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = defaultValue;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            // Стилизация под темную тему
            form.BackColor = Color.FromArgb(45, 45, 48);
            form.ForeColor = Color.White;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ClientSize = new Size(350, 150);

            label.SetBounds(15, 20, 320, 13);
            label.Font = new Font("Segoe UI", 10);

            textBox.SetBounds(15, 50, 305, 20);
            textBox.Font = new Font("Segoe UI", 10);

            buttonOk.SetBounds(135, 90, 80, 30);
            buttonCancel.SetBounds(225, 90, 80, 30);

            // Кнопки
            buttonOk.BackColor = Color.FromArgb(46, 204, 113);
            buttonOk.ForeColor = Color.Black;
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonOk.FlatAppearance.BorderSize = 0;

            buttonCancel.BackColor = Color.FromArgb(60, 60, 60);
            buttonCancel.ForeColor = Color.White;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatAppearance.BorderSize = 0;

            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            return dialogResult == DialogResult.OK ? textBox.Text : null;
        }
    }
}