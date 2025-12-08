namespace PingMonitor
{
    partial class PingTile
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlStatus = new System.Windows.Forms.Panel();
            this.lblAddress = new System.Windows.Forms.Label();
            this.lblPing = new System.Windows.Forms.Label();
            this.lblStats = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pnlStatus
            // 
            this.pnlStatus.BackColor = System.Drawing.Color.Gray;
            this.pnlStatus.Location = new System.Drawing.Point(-6, 0);
            this.pnlStatus.Name = "pnlStatus";
            this.pnlStatus.Size = new System.Drawing.Size(226, 18);
            this.pnlStatus.TabIndex = 0;
            // 
            // lblAddress
            // 
            this.lblAddress.AutoSize = true;
            this.lblAddress.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblAddress.Location = new System.Drawing.Point(3, 21);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(86, 31);
            this.lblAddress.TabIndex = 1;
            this.lblAddress.Text = "label1";
            // 
            // lblPing
            // 
            this.lblPing.AutoSize = true;
            this.lblPing.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPing.Location = new System.Drawing.Point(3, 52);
            this.lblPing.Name = "lblPing";
            this.lblPing.Size = new System.Drawing.Size(204, 73);
            this.lblPing.TabIndex = 2;
            this.lblPing.Text = "label1";
            // 
            // lblStats
            // 
            this.lblStats.AutoSize = true;
            this.lblStats.Location = new System.Drawing.Point(-1, 122);
            this.lblStats.Name = "lblStats";
            this.lblStats.Size = new System.Drawing.Size(70, 25);
            this.lblStats.TabIndex = 3;
            this.lblStats.Text = "label1";
            // 
            // PingTile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.lblStats);
            this.Controls.Add(this.lblPing);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.pnlStatus);
            this.Name = "PingTile";
            this.Size = new System.Drawing.Size(218, 147);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlStatus;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.Label lblPing;
        private System.Windows.Forms.Label lblStats;
    }
}
