namespace clsfrmInputUI
{
    partial class FrmDataInput
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmDataInput));
            this.lblOperatorID = new System.Windows.Forms.Label();
            this.txtLotID = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txtSubLotID = new System.Windows.Forms.TextBox();
            this.txtOperatorID = new System.Windows.Forms.TextBox();
            this.txtDeviceID = new System.Windows.Forms.TextBox();
            this.txtHandlerID = new System.Windows.Forms.TextBox();
            this.txtContactorID = new System.Windows.Forms.TextBox();
            this.lblLotID = new System.Windows.Forms.Label();
            this.lblSubLotID = new System.Windows.Forms.Label();
            this.lblDeviceID = new System.Windows.Forms.Label();
            this.lblHandlerID = new System.Windows.Forms.Label();
            this.lblContactor = new System.Windows.Forms.Label();
            this.lblMfgID = new System.Windows.Forms.Label();
            this.txtMfgLotID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtLbID = new System.Windows.Forms.TextBox();
            this.lblLbID = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lblOperatorID
            // 
            this.lblOperatorID.AutoSize = true;
            this.lblOperatorID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblOperatorID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOperatorID.Location = new System.Drawing.Point(15, 73);
            this.lblOperatorID.Name = "lblOperatorID";
            this.lblOperatorID.Size = new System.Drawing.Size(79, 18);
            this.lblOperatorID.TabIndex = 0;
            this.lblOperatorID.Text = "Operator ID";
            // 
            // txtLotID
            // 
            this.txtLotID.AcceptsTab = true;
            this.txtLotID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtLotID.Enabled = false;
            this.txtLotID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLotID.Location = new System.Drawing.Point(113, 108);
            this.txtLotID.Name = "txtLotID";
            this.txtLotID.Size = new System.Drawing.Size(326, 22);
            this.txtLotID.TabIndex = 1;
            this.txtLotID.Text = "\r\n";
            this.txtLotID.Enter += new System.EventHandler(this.txtLotID_Enter);
            this.txtLotID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtLotID_KeyDown);
            this.txtLotID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtLotID_KeyPress);
            this.txtLotID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtLotID_MouseDown);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(349, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 45);
            this.button1.TabIndex = 8;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            this.button1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.button1_KeyDown);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(247, 45);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // txtSubLotID
            // 
            this.txtSubLotID.AcceptsTab = true;
            this.txtSubLotID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtSubLotID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSubLotID.Location = new System.Drawing.Point(113, 223);
            this.txtSubLotID.Name = "txtSubLotID";
            this.txtSubLotID.Size = new System.Drawing.Size(326, 22);
            this.txtSubLotID.TabIndex = 4;
            this.txtSubLotID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSubLotID_KeyDown);
            this.txtSubLotID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSubLotID_KeyPress);
            this.txtSubLotID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtSubLotID_MouseDown);
            // 
            // txtOperatorID
            // 
            this.txtOperatorID.AcceptsTab = true;
            this.txtOperatorID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtOperatorID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOperatorID.Location = new System.Drawing.Point(113, 73);
            this.txtOperatorID.Name = "txtOperatorID";
            this.txtOperatorID.Size = new System.Drawing.Size(326, 22);
            this.txtOperatorID.TabIndex = 0;
            this.txtOperatorID.Enter += new System.EventHandler(this.txtOperatorID_Enter);
            this.txtOperatorID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtOperatorID_KeyDown);
            this.txtOperatorID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOperatorID_KeyPress);
            this.txtOperatorID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtOperatorID_MouseDown);
            // 
            // txtDeviceID
            // 
            this.txtDeviceID.AcceptsTab = true;
            this.txtDeviceID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtDeviceID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDeviceID.Location = new System.Drawing.Point(113, 182);
            this.txtDeviceID.Name = "txtDeviceID";
            this.txtDeviceID.Size = new System.Drawing.Size(326, 22);
            this.txtDeviceID.TabIndex = 3;
            this.txtDeviceID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtDeviceID_KeyDown_1);
            this.txtDeviceID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtDeviceID_KeyPress_1);
            this.txtDeviceID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtDeviceID_MouseDown_1);
            // 
            // txtHandlerID
            // 
            this.txtHandlerID.AcceptsTab = true;
            this.txtHandlerID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtHandlerID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtHandlerID.Location = new System.Drawing.Point(113, 260);
            this.txtHandlerID.Name = "txtHandlerID";
            this.txtHandlerID.Size = new System.Drawing.Size(326, 22);
            this.txtHandlerID.TabIndex = 5;
            this.txtHandlerID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtHandlerID_KeyDown);
            this.txtHandlerID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtHandlerID_KeyPress);
            this.txtHandlerID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtHandlerID_MouseDown);
            // 
            // txtContactorID
            // 
            this.txtContactorID.AcceptsTab = true;
            this.txtContactorID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtContactorID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtContactorID.Location = new System.Drawing.Point(113, 299);
            this.txtContactorID.Name = "txtContactorID";
            this.txtContactorID.Size = new System.Drawing.Size(326, 22);
            this.txtContactorID.TabIndex = 6;
            this.txtContactorID.Text = "NA";
            this.txtContactorID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtContactorID_KeyDown);
            this.txtContactorID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtContactorID_KeyPress);
            this.txtContactorID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtContactorID_MouseDown);
            // 
            // lblLotID
            // 
            this.lblLotID.AutoSize = true;
            this.lblLotID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblLotID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLotID.Location = new System.Drawing.Point(15, 109);
            this.lblLotID.Name = "lblLotID";
            this.lblLotID.Size = new System.Drawing.Size(28, 18);
            this.lblLotID.TabIndex = 0;
            this.lblLotID.Text = "Lot";
            // 
            // lblSubLotID
            // 
            this.lblSubLotID.AutoSize = true;
            this.lblSubLotID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblSubLotID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSubLotID.Location = new System.Drawing.Point(15, 227);
            this.lblSubLotID.Name = "lblSubLotID";
            this.lblSubLotID.Size = new System.Drawing.Size(55, 18);
            this.lblSubLotID.TabIndex = 0;
            this.lblSubLotID.Text = "Sub Lot";
            // 
            // lblDeviceID
            // 
            this.lblDeviceID.AutoSize = true;
            this.lblDeviceID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblDeviceID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDeviceID.Location = new System.Drawing.Point(15, 185);
            this.lblDeviceID.Name = "lblDeviceID";
            this.lblDeviceID.Size = new System.Drawing.Size(53, 18);
            this.lblDeviceID.TabIndex = 0;
            this.lblDeviceID.Text = "Device";
            // 
            // lblHandlerID
            // 
            this.lblHandlerID.AutoSize = true;
            this.lblHandlerID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblHandlerID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHandlerID.Location = new System.Drawing.Point(15, 263);
            this.lblHandlerID.Name = "lblHandlerID";
            this.lblHandlerID.Size = new System.Drawing.Size(80, 18);
            this.lblHandlerID.TabIndex = 0;
            this.lblHandlerID.Text = "Handler SN";
            // 
            // lblContactor
            // 
            this.lblContactor.AutoSize = true;
            this.lblContactor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblContactor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblContactor.Location = new System.Drawing.Point(15, 299);
            this.lblContactor.Name = "lblContactor";
            this.lblContactor.Size = new System.Drawing.Size(83, 18);
            this.lblContactor.TabIndex = 0;
            this.lblContactor.Text = "Contactor ID";
            // 
            // lblMfgID
            // 
            this.lblMfgID.AutoSize = true;
            this.lblMfgID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblMfgID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMfgID.Location = new System.Drawing.Point(15, 147);
            this.lblMfgID.Name = "lblMfgID";
            this.lblMfgID.Size = new System.Drawing.Size(69, 18);
            this.lblMfgID.TabIndex = 0;
            this.lblMfgID.Text = "Mfg Lot ID";
            // 
            // txtMfgLotID
            // 
            this.txtMfgLotID.AcceptsTab = true;
            this.txtMfgLotID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtMfgLotID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMfgLotID.Location = new System.Drawing.Point(113, 147);
            this.txtMfgLotID.Name = "txtMfgLotID";
            this.txtMfgLotID.Size = new System.Drawing.Size(326, 22);
            this.txtMfgLotID.TabIndex = 2;
            this.txtMfgLotID.Enter += new System.EventHandler(this.txtMfgLotID_Enter);
            this.txtMfgLotID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMfgLotID_KeyDown);
            this.txtMfgLotID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMfgLotID_KeyPress);
            this.txtMfgLotID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtMfgLotID_MouseDown);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(384, 362);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "PA ver.01";
            // 
            // txtLbID
            // 
            this.txtLbID.AcceptsTab = true;
            this.txtLbID.BackColor = System.Drawing.Color.Gainsboro;
            this.txtLbID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLbID.Location = new System.Drawing.Point(113, 337);
            this.txtLbID.Name = "txtLbID";
            this.txtLbID.Size = new System.Drawing.Size(326, 22);
            this.txtLbID.TabIndex = 7;
            this.txtLbID.Text = "NA";
            this.txtLbID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtLbID_KeyDown);
            this.txtLbID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtLbID_KeyPress);
            this.txtLbID.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtLbID_MouseDown);
            // 
            // lblLbID
            // 
            this.lblLbID.AutoSize = true;
            this.lblLbID.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblLbID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLbID.Location = new System.Drawing.Point(15, 337);
            this.lblLbID.Name = "lblLbID";
            this.lblLbID.Size = new System.Drawing.Size(97, 18);
            this.lblLbID.TabIndex = 0;
            this.lblLbID.Text = "Load Board ID";
            // 
            // FrmDataInput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(450, 380);
            this.ControlBox = false;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblMfgID);
            this.Controls.Add(this.txtMfgLotID);
            this.Controls.Add(this.lblLbID);
            this.Controls.Add(this.lblContactor);
            this.Controls.Add(this.lblHandlerID);
            this.Controls.Add(this.lblDeviceID);
            this.Controls.Add(this.lblSubLotID);
            this.Controls.Add(this.lblLotID);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtLbID);
            this.Controls.Add(this.txtContactorID);
            this.Controls.Add(this.txtHandlerID);
            this.Controls.Add(this.txtDeviceID);
            this.Controls.Add(this.txtOperatorID);
            this.Controls.Add(this.txtSubLotID);
            this.Controls.Add(this.txtLotID);
            this.Controls.Add(this.lblOperatorID);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "FrmDataInput";
            this.Text = "Production Test Information Input";
            this.Load += new System.EventHandler(this.FrmDataInput_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        

        #endregion

        private System.Windows.Forms.Label lblOperatorID;
        private System.Windows.Forms.TextBox txtLotID;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtSubLotID;
        private System.Windows.Forms.TextBox txtOperatorID;
        private System.Windows.Forms.TextBox txtDeviceID;
        private System.Windows.Forms.TextBox txtHandlerID;
        private System.Windows.Forms.TextBox txtContactorID;
        private System.Windows.Forms.Label lblLotID;
        private System.Windows.Forms.Label lblSubLotID;
        private System.Windows.Forms.Label lblDeviceID;
        private System.Windows.Forms.Label lblHandlerID;
        private System.Windows.Forms.Label lblContactor;
        private System.Windows.Forms.Label lblMfgID;
        private System.Windows.Forms.TextBox txtMfgLotID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtLbID;
        private System.Windows.Forms.Label lblLbID;
    }
}