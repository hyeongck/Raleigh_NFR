namespace ProductionLib2
{
    partial class InsepctRevisionMessage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsepctRevisionMessage));
            this.btnConfirm = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tbxSampleRevision = new System.Windows.Forms.TextBox();
            this.lblRevIDTCF = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblRevIDSample = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnConfirm
            // 
            this.btnConfirm.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnConfirm.ForeColor = System.Drawing.Color.Blue;
            this.btnConfirm.Location = new System.Drawing.Point(417, 334);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(397, 51);
            this.btnConfirm.TabIndex = 0;
            this.btnConfirm.Text = "&Acknowledge";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(417, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(367, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Check your Revision ID with Sample!";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(417, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "RevID from TCF:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(10, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(401, 372);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // tbxSampleRevision
            // 
            this.tbxSampleRevision.BackColor = System.Drawing.Color.Yellow;
            this.tbxSampleRevision.Font = new System.Drawing.Font("Tahoma", 24F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxSampleRevision.ForeColor = System.Drawing.Color.Red;
            this.tbxSampleRevision.Location = new System.Drawing.Point(47, 198);
            this.tbxSampleRevision.Name = "tbxSampleRevision";
            this.tbxSampleRevision.Size = new System.Drawing.Size(71, 46);
            this.tbxSampleRevision.TabIndex = 4;
            this.tbxSampleRevision.Text = "A0A";
            this.tbxSampleRevision.Click += new System.EventHandler(this.tbxSampleRevision_Click);
            this.tbxSampleRevision.TextChanged += new System.EventHandler(this.tbxSampleRevision_TextChanged);
            this.tbxSampleRevision.Enter += new System.EventHandler(this.tbxSampleRevision_Enter);
            // 
            // lblRevIDTCF
            // 
            this.lblRevIDTCF.AutoSize = true;
            this.lblRevIDTCF.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRevIDTCF.Location = new System.Drawing.Point(571, 79);
            this.lblRevIDTCF.Name = "lblRevIDTCF";
            this.lblRevIDTCF.Size = new System.Drawing.Size(146, 39);
            this.lblRevIDTCF.TabIndex = 5;
            this.lblRevIDTCF.Text = "tcf label";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(417, 156);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(146, 15);
            this.label4.TabIndex = 3;
            this.label4.Text = "RevID from userInput:";
            // 
            // lblRevIDSample
            // 
            this.lblRevIDSample.AutoSize = true;
            this.lblRevIDSample.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRevIDSample.Location = new System.Drawing.Point(571, 156);
            this.lblRevIDSample.Name = "lblRevIDSample";
            this.lblRevIDSample.Size = new System.Drawing.Size(219, 39);
            this.lblRevIDSample.TabIndex = 5;
            this.lblRevIDSample.Text = "sample label";
            this.lblRevIDSample.TextChanged += new System.EventHandler(this.lblRevIDSample_TextChanged);
            // 
            // InsepctRevisionMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(826, 399);
            this.Controls.Add(this.lblRevIDSample);
            this.Controls.Add(this.lblRevIDTCF);
            this.Controls.Add(this.tbxSampleRevision);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnConfirm);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(823, 399);
            this.Name = "InsepctRevisionMessage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "InspectSocketMessage";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbxSampleRevision;
        private System.Windows.Forms.Label lblRevIDTCF;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblRevIDSample;
    }
}