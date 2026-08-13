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
            buttonCancel.Text = "Отмена";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            // Стилизация под тему
            form.BackColor = Theme.BgWindow;
            form.ForeColor = Theme.Text;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ClientSize = new Size(380, 150);

            label.SetBounds(20, 18, 340, 18);
            label.Font = new Font("Segoe UI", 10);

            textBox.SetBounds(20, 44, 340, 24);
            textBox.Font = new Font("Segoe UI", 10);
            textBox.BackColor = Theme.BgInput;
            textBox.ForeColor = Theme.Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;

            buttonOk.SetBounds(180, 90, 85, 30);
            buttonCancel.SetBounds(275, 90, 85, 30);

            // Кнопки
            buttonOk.BackColor = Theme.Accent;
            buttonOk.ForeColor = Theme.AccentText;
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonOk.FlatAppearance.BorderSize = 0;
            buttonOk.Cursor = Cursors.Hand;
            buttonOk.MouseEnter += (s, e) => buttonOk.BackColor = Theme.AccentHover;
            buttonOk.MouseLeave += (s, e) => buttonOk.BackColor = Theme.Accent;

            buttonCancel.BackColor = Theme.BgInput;
            buttonCancel.ForeColor = Theme.Text;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatAppearance.BorderSize = 0;
            buttonCancel.Cursor = Cursors.Hand;
            buttonCancel.MouseEnter += (s, e) => buttonCancel.BackColor = Theme.BgHover;
            buttonCancel.MouseLeave += (s, e) => buttonCancel.BackColor = Theme.BgInput;

            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            return dialogResult == DialogResult.OK ? textBox.Text : null;
        }
    }
}