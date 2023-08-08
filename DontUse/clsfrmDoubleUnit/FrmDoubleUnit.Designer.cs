namespace clsfrmDoubleUnit
{
    partial class FrmDoubleUnit
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.txtErrMsg = new System.Windows.Forms.TextBox();
            this.txtRectifyMsg = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::clsfrmDoubleUnit.Properties.Resources.DUTSocket;
            this.pictureBox1.Location = new System.Drawing.Point(126, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(399, 285);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Blue;
            this.button1.Location = new System.Drawing.Point(212, 387);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(209, 60);
            this.button1.TabIndex = 5;
            this.button1.Text = "&Acknowledge";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtErrMsg
            // 
            this.txtErrMsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtErrMsg.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtErrMsg.ForeColor = System.Drawing.Color.Red;
            this.txtErrMsg.Location = new System.Drawing.Point(12, 303);
            this.txtErrMsg.Name = "txtErrMsg";
            this.txtErrMsg.Size = new System.Drawing.Size(620, 19);
            this.txtErrMsg.TabIndex = 6;
            this.txtErrMsg.Text = "Duplicated Module ID detected, test aborted. ";
            this.txtErrMsg.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtRectifyMsg
            // 
            this.txtRectifyMsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRectifyMsg.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRectifyMsg.Location = new System.Drawing.Point(12, 342);
            this.txtRectifyMsg.Name = "txtRectifyMsg";
            this.txtRectifyMsg.Size = new System.Drawing.Size(636, 19);
            this.txtRectifyMsg.TabIndex = 7;
            this.txtRectifyMsg.Text = "Please inspect test socket and then rectify the problem to resume test";
            this.txtRectifyMsg.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // FrmDoubleUnit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(660, 469);
            this.Controls.Add(this.txtRectifyMsg);
            this.Controls.Add(this.txtErrMsg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox1);
            this.ForeColor = System.Drawing.Color.Red;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmDoubleUnit";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "!!! Double Unit Detected !!!";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
        public System.Windows.Forms.TextBox txtErrMsg;
        public System.Windows.Forms.TextBox txtRectifyMsg;
    }
}

