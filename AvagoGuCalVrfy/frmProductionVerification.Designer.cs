namespace AvagoGU
{
    partial class frmProductionVerification
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
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lb_Select = new System.Windows.Forms.ListBox();
            this.chkICC_Cal = new System.Windows.Forms.CheckBox();
            this.chkGU_Cal = new System.Windows.Forms.CheckBox();
            this.btnBypass = new System.Windows.Forms.Button();
            this.lblICC_CalStatus = new System.Windows.Forms.Label();
            this.lbl_GU_Cal_Status = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblGUVerify_Date = new System.Windows.Forms.Label();
            this.lblGUVerify_Status = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(63, 167);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 33);
            this.button1.TabIndex = 0;
            this.button1.Text = "Run Gu Cal";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Select Batch";
            // 
            // lb_Select
            // 
            this.lb_Select.FormattingEnabled = true;
            this.lb_Select.Items.AddRange(new object[] {
            "1",
            "2",
            "3"});
            this.lb_Select.Location = new System.Drawing.Point(111, 21);
            this.lb_Select.Name = "lb_Select";
            this.lb_Select.Size = new System.Drawing.Size(53, 121);
            this.lb_Select.TabIndex = 3;
            this.lb_Select.SelectedIndexChanged += new System.EventHandler(this.lb_Select_SelectedIndexChanged);
            this.lb_Select.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.lb_Select_KeyPress);
            // 
            // chkICC_Cal
            // 
            this.chkICC_Cal.AutoSize = true;
            this.chkICC_Cal.Location = new System.Drawing.Point(200, 21);
            this.chkICC_Cal.Name = "chkICC_Cal";
            this.chkICC_Cal.Size = new System.Drawing.Size(95, 17);
            this.chkICC_Cal.TabIndex = 4;
            this.chkICC_Cal.Text = "ICC Calibration";
            this.chkICC_Cal.UseVisualStyleBackColor = true;
            this.chkICC_Cal.Visible = false;
            this.chkICC_Cal.CheckedChanged += new System.EventHandler(this.chkICC_Cal_CheckedChanged);
            // 
            // chkGU_Cal
            // 
            this.chkGU_Cal.AutoSize = true;
            this.chkGU_Cal.Checked = true;
            this.chkGU_Cal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGU_Cal.Location = new System.Drawing.Point(200, 44);
            this.chkGU_Cal.Name = "chkGU_Cal";
            this.chkGU_Cal.Size = new System.Drawing.Size(80, 17);
            this.chkGU_Cal.TabIndex = 5;
            this.chkGU_Cal.Text = "CorrFactors";
            this.chkGU_Cal.UseVisualStyleBackColor = true;
            this.chkGU_Cal.CheckedChanged += new System.EventHandler(this.chkGU_Cal_CheckedChanged);
            // 
            // btnBypass
            // 
            this.btnBypass.Enabled = false;
            this.btnBypass.Location = new System.Drawing.Point(238, 167);
            this.btnBypass.Name = "btnBypass";
            this.btnBypass.Size = new System.Drawing.Size(106, 32);
            this.btnBypass.TabIndex = 6;
            this.btnBypass.Text = "Run Production";
            this.btnBypass.UseVisualStyleBackColor = true;
            this.btnBypass.Click += new System.EventHandler(this.btnBypass_Click);
            // 
            // lblICC_CalStatus
            // 
            this.lblICC_CalStatus.AutoSize = true;
            this.lblICC_CalStatus.Location = new System.Drawing.Point(304, 23);
            this.lblICC_CalStatus.Name = "lblICC_CalStatus";
            this.lblICC_CalStatus.Size = new System.Drawing.Size(35, 13);
            this.lblICC_CalStatus.TabIndex = 7;
            this.lblICC_CalStatus.Text = "PASS";
            this.lblICC_CalStatus.Visible = false;
            // 
            // lbl_GU_Cal_Status
            // 
            this.lbl_GU_Cal_Status.AutoSize = true;
            this.lbl_GU_Cal_Status.Location = new System.Drawing.Point(304, 46);
            this.lbl_GU_Cal_Status.Name = "lbl_GU_Cal_Status";
            this.lbl_GU_Cal_Status.Size = new System.Drawing.Size(35, 13);
            this.lbl_GU_Cal_Status.TabIndex = 8;
            this.lbl_GU_Cal_Status.Text = "PASS";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblGUVerify_Date);
            this.groupBox1.Controls.Add(this.lblGUVerify_Status);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(191, 71);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(209, 73);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Last GU Verified Status";
            // 
            // lblGUVerify_Date
            // 
            this.lblGUVerify_Date.AutoSize = true;
            this.lblGUVerify_Date.Location = new System.Drawing.Point(68, 45);
            this.lblGUVerify_Date.Name = "lblGUVerify_Date";
            this.lblGUVerify_Date.Size = new System.Drawing.Size(35, 13);
            this.lblGUVerify_Date.TabIndex = 11;
            this.lblGUVerify_Date.Text = "PASS";
            // 
            // lblGUVerify_Status
            // 
            this.lblGUVerify_Status.AutoSize = true;
            this.lblGUVerify_Status.Location = new System.Drawing.Point(69, 26);
            this.lblGUVerify_Status.Name = "lblGUVerify_Status";
            this.lblGUVerify_Status.Size = new System.Drawing.Size(35, 13);
            this.lblGUVerify_Status.TabIndex = 10;
            this.lblGUVerify_Status.Text = "PASS";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Date :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Status :";
            // 
            // frmProductionVerification
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 214);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lbl_GU_Cal_Status);
            this.Controls.Add(this.lblICC_CalStatus);
            this.Controls.Add(this.btnBypass);
            this.Controls.Add(this.chkGU_Cal);
            this.Controls.Add(this.chkICC_Cal);
            this.Controls.Add(this.lb_Select);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmProductionVerification";
            this.Text = "GU Cal Batch Selection - GU Verification";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frmProductionVerification_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lb_Select;
        private System.Windows.Forms.CheckBox chkICC_Cal;
        private System.Windows.Forms.CheckBox chkGU_Cal;
        private System.Windows.Forms.Button btnBypass;
        private System.Windows.Forms.Label lblICC_CalStatus;
        private System.Windows.Forms.Label lbl_GU_Cal_Status;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblGUVerify_Date;
        private System.Windows.Forms.Label lblGUVerify_Status;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
    }
}