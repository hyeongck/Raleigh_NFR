namespace AvagoGU
{
    partial class frmVerification
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
            this.listboxGuDevices = new System.Windows.Forms.ListBox();
            this.lblSelectDevices = new System.Windows.Forms.Label();
            this.runGU = new System.Windows.Forms.Button();
            this.noRunGU = new System.Windows.Forms.Button();
            this.lblSelectSites = new System.Windows.Forms.Label();
            this.listboxGuSites = new System.Windows.Forms.ListBox();
            this.txtbxPassword = new System.Windows.Forms.TextBox();
            this.lblGuPassword = new System.Windows.Forms.Label();
            this.btnPasswordEnter = new System.Windows.Forms.Button();
            this.runVrfy = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.runCorrVrfy = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.listboxGuBatch = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listboxGuDevices
            // 
            this.listboxGuDevices.Enabled = false;
            this.listboxGuDevices.FormattingEnabled = true;
            this.listboxGuDevices.ItemHeight = 16;
            this.listboxGuDevices.Items.AddRange(new object[] {
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "24",
            "25",
            "26",
            "27",
            "28",
            "34"});
            this.listboxGuDevices.Location = new System.Drawing.Point(295, 114);
            this.listboxGuDevices.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listboxGuDevices.Name = "listboxGuDevices";
            this.listboxGuDevices.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listboxGuDevices.Size = new System.Drawing.Size(124, 228);
            this.listboxGuDevices.TabIndex = 5;
            this.listboxGuDevices.SelectedIndexChanged += new System.EventHandler(this.listboxGuDevices_SelectedIndexChanged);
            // 
            // lblSelectDevices
            // 
            this.lblSelectDevices.AutoSize = true;
            this.lblSelectDevices.Enabled = false;
            this.lblSelectDevices.Location = new System.Drawing.Point(291, 95);
            this.lblSelectDevices.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSelectDevices.Name = "lblSelectDevices";
            this.lblSelectDevices.Size = new System.Drawing.Size(126, 17);
            this.lblSelectDevices.TabIndex = 1;
            this.lblSelectDevices.Text = "Select GU Devices";
            // 
            // runGU
            // 
            this.runGU.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.runGU.Enabled = false;
            this.runGU.Location = new System.Drawing.Point(160, 386);
            this.runGU.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.runGU.Name = "runGU";
            this.runGU.Size = new System.Drawing.Size(155, 28);
            this.runGU.TabIndex = 6;
            this.runGU.Text = "Icc + Corr + Verify";
            this.runGU.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.runGU.UseVisualStyleBackColor = true;
            // 
            // noRunGU
            // 
            this.noRunGU.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.noRunGU.Location = new System.Drawing.Point(175, 533);
            this.noRunGU.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.noRunGU.Name = "noRunGU";
            this.noRunGU.Size = new System.Drawing.Size(127, 28);
            this.noRunGU.TabIndex = 3;
            this.noRunGU.Text = "Cancel";
            this.noRunGU.UseVisualStyleBackColor = true;
            // 
            // lblSelectSites
            // 
            this.lblSelectSites.AutoSize = true;
            this.lblSelectSites.Enabled = false;
            this.lblSelectSites.Location = new System.Drawing.Point(52, 223);
            this.lblSelectSites.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSelectSites.Name = "lblSelectSites";
            this.lblSelectSites.Size = new System.Drawing.Size(128, 17);
            this.lblSelectSites.TabIndex = 4;
            this.lblSelectSites.Text = "Select Sites for GU";
            // 
            // listboxGuSites
            // 
            this.listboxGuSites.Enabled = false;
            this.listboxGuSites.FormattingEnabled = true;
            this.listboxGuSites.ItemHeight = 16;
            this.listboxGuSites.Items.AddRange(new object[] {
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "24",
            "25",
            "26",
            "27",
            "28",
            "34"});
            this.listboxGuSites.Location = new System.Drawing.Point(56, 242);
            this.listboxGuSites.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listboxGuSites.Name = "listboxGuSites";
            this.listboxGuSites.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listboxGuSites.Size = new System.Drawing.Size(124, 100);
            this.listboxGuSites.TabIndex = 4;
            this.listboxGuSites.SelectedIndexChanged += new System.EventHandler(this.listboxGuSites_SelectedIndexChanged);
            // 
            // txtbxPassword
            // 
            this.txtbxPassword.AcceptsReturn = true;
            this.txtbxPassword.Location = new System.Drawing.Point(160, 23);
            this.txtbxPassword.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtbxPassword.Name = "txtbxPassword";
            this.txtbxPassword.Size = new System.Drawing.Size(140, 22);
            this.txtbxPassword.TabIndex = 1;
            this.txtbxPassword.UseSystemPasswordChar = true;
            // 
            // lblGuPassword
            // 
            this.lblGuPassword.AutoSize = true;
            this.lblGuPassword.Location = new System.Drawing.Point(52, 28);
            this.lblGuPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblGuPassword.Name = "lblGuPassword";
            this.lblGuPassword.Size = new System.Drawing.Size(98, 17);
            this.lblGuPassword.TabIndex = 7;
            this.lblGuPassword.Text = "GU Password:";
            // 
            // btnPasswordEnter
            // 
            this.btnPasswordEnter.Location = new System.Drawing.Point(309, 23);
            this.btnPasswordEnter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnPasswordEnter.Name = "btnPasswordEnter";
            this.btnPasswordEnter.Size = new System.Drawing.Size(56, 25);
            this.btnPasswordEnter.TabIndex = 2;
            this.btnPasswordEnter.Text = "Enter";
            this.btnPasswordEnter.UseVisualStyleBackColor = true;
            this.btnPasswordEnter.Click += new System.EventHandler(this.btnPasswordEnter_Click);
            // 
            // runVrfy
            // 
            this.runVrfy.DialogResult = System.Windows.Forms.DialogResult.Retry;
            this.runVrfy.Enabled = false;
            this.runVrfy.Location = new System.Drawing.Point(160, 458);
            this.runVrfy.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.runVrfy.Name = "runVrfy";
            this.runVrfy.Size = new System.Drawing.Size(155, 28);
            this.runVrfy.TabIndex = 8;
            this.runVrfy.Text = "Verify";
            this.runVrfy.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.runVrfy.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(171, 367);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(141, 17);
            this.label1.TabIndex = 9;
            this.label1.Text = "Run GU with options:";
            // 
            // runCorrVrfy
            // 
            this.runCorrVrfy.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.runCorrVrfy.Enabled = false;
            this.runCorrVrfy.Location = new System.Drawing.Point(160, 422);
            this.runCorrVrfy.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.runCorrVrfy.Name = "runCorrVrfy";
            this.runCorrVrfy.Size = new System.Drawing.Size(155, 28);
            this.runCorrVrfy.TabIndex = 10;
            this.runCorrVrfy.Text = "Corr + Verify";
            this.runCorrVrfy.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.runCorrVrfy.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(187, 513);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "Don\'t Run GU:";
            // 
            // listboxGuBatch
            // 
            this.listboxGuBatch.FormattingEnabled = true;
            this.listboxGuBatch.ItemHeight = 16;
            this.listboxGuBatch.Items.AddRange(new object[] {
            "1",
            "2",
            "3"});
            this.listboxGuBatch.Location = new System.Drawing.Point(56, 114);
            this.listboxGuBatch.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listboxGuBatch.Name = "listboxGuBatch";
            this.listboxGuBatch.Size = new System.Drawing.Size(124, 84);
            this.listboxGuBatch.TabIndex = 13;
            this.listboxGuBatch.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(52, 95);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 17);
            this.label3.TabIndex = 12;
            this.label3.Text = "Select GU Batch";
            // 
            // frmVerification
            // 
            this.AcceptButton = this.runGU;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.noRunGU;
            this.ClientSize = new System.Drawing.Size(476, 576);
            this.ControlBox = false;
            this.Controls.Add(this.listboxGuBatch);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.runCorrVrfy);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.runVrfy);
            this.Controls.Add(this.btnPasswordEnter);
            this.Controls.Add(this.lblGuPassword);
            this.Controls.Add(this.txtbxPassword);
            this.Controls.Add(this.listboxGuSites);
            this.Controls.Add(this.lblSelectSites);
            this.Controls.Add(this.noRunGU);
            this.Controls.Add(this.runGU);
            this.Controls.Add(this.lblSelectDevices);
            this.Controls.Add(this.listboxGuDevices);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmVerification";
            this.Text = "GU Calibration";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listboxGuDevices;
        private System.Windows.Forms.Label lblSelectDevices;
        private System.Windows.Forms.Button runGU;
        private System.Windows.Forms.Button noRunGU;
        private System.Windows.Forms.Label lblSelectSites;
        private System.Windows.Forms.ListBox listboxGuSites;
        private System.Windows.Forms.TextBox txtbxPassword;
        private System.Windows.Forms.Label lblGuPassword;
        private System.Windows.Forms.Button btnPasswordEnter;
        private System.Windows.Forms.Button runVrfy;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button runCorrVrfy;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listboxGuBatch;
        private System.Windows.Forms.Label label3;

    }
}

