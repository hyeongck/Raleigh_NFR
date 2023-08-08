namespace ClothoSharedItems
{
    partial class FormSeoulHelper
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
            try
            {
                base.Dispose(disposing);
            }
            catch { }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.clbTestOption = new System.Windows.Forms.CheckedListBox();
            this.gbxTestOption = new System.Windows.Forms.GroupBox();
            this.btnKillExcel = new System.Windows.Forms.Button();
            this.dgvEbrs = new System.Windows.Forms.DataGridView();
            this.tbxFilterEBR = new System.Windows.Forms.TextBox();
            this.dgvWaferSet = new System.Windows.Forms.DataGridView();
            this.tbxFilterPjt = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbxWafers = new System.Windows.Forms.TextBox();
            this.tbxFilterSublot = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gbxEBR_ATE_Tracking = new System.Windows.Forms.GroupBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.gbxTempbox = new System.Windows.Forms.GroupBox();
            this.lblCurrentTempFromNI = new System.Windows.Forms.Label();
            this.btnNITemp = new System.Windows.Forms.Button();
            this.btnSetTemperature = new System.Windows.Forms.Button();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.btnTempOnOff = new System.Windows.Forms.Button();
            this.btnScpiConnect = new System.Windows.Forms.Button();
            this.tbxGPIB = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnFTPDownload = new System.Windows.Forms.Button();
            this.btnDebugFeature = new System.Windows.Forms.Button();
            this.btnUnsubscribe = new System.Windows.Forms.Button();
            this.tbxLogs = new System.Windows.Forms.TextBox();
            this.btnCheckFTP = new System.Windows.Forms.Button();
            this.gbxTestOption.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEbrs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWaferSet)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gbxEBR_ATE_Tracking.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.gbxTempbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // clbTestOption
            // 
            this.clbTestOption.CheckOnClick = true;
            this.clbTestOption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbTestOption.FormattingEnabled = true;
            this.clbTestOption.HorizontalScrollbar = true;
            this.clbTestOption.Location = new System.Drawing.Point(3, 17);
            this.clbTestOption.Name = "clbTestOption";
            this.clbTestOption.Size = new System.Drawing.Size(162, 641);
            this.clbTestOption.TabIndex = 0;
            this.clbTestOption.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbTestOption_ItemCheck);
            // 
            // gbxTestOption
            // 
            this.gbxTestOption.Controls.Add(this.clbTestOption);
            this.gbxTestOption.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbxTestOption.Location = new System.Drawing.Point(12, 12);
            this.gbxTestOption.Name = "gbxTestOption";
            this.gbxTestOption.Size = new System.Drawing.Size(168, 661);
            this.gbxTestOption.TabIndex = 1;
            this.gbxTestOption.TabStop = false;
            this.gbxTestOption.Text = "Option";
            // 
            // btnKillExcel
            // 
            this.btnKillExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKillExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnKillExcel.Location = new System.Drawing.Point(3, 3);
            this.btnKillExcel.Name = "btnKillExcel";
            this.btnKillExcel.Size = new System.Drawing.Size(79, 36);
            this.btnKillExcel.TabIndex = 2;
            this.btnKillExcel.Text = "Kill All";
            this.btnKillExcel.UseVisualStyleBackColor = true;
            this.btnKillExcel.Click += new System.EventHandler(this.btnKillExcel_Click);
            // 
            // dgvEbrs
            // 
            this.dgvEbrs.AllowUserToAddRows = false;
            this.dgvEbrs.AllowUserToDeleteRows = false;
            this.dgvEbrs.AllowUserToResizeRows = false;
            this.dgvEbrs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvEbrs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvEbrs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEbrs.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvEbrs.Location = new System.Drawing.Point(6, 45);
            this.dgvEbrs.Name = "dgvEbrs";
            this.dgvEbrs.RowHeadersVisible = false;
            this.dgvEbrs.Size = new System.Drawing.Size(668, 257);
            this.dgvEbrs.TabIndex = 3;
            // 
            // tbxFilterEBR
            // 
            this.tbxFilterEBR.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterEBR.Location = new System.Drawing.Point(309, 14);
            this.tbxFilterEBR.Margin = new System.Windows.Forms.Padding(2);
            this.tbxFilterEBR.Name = "tbxFilterEBR";
            this.tbxFilterEBR.Size = new System.Drawing.Size(108, 26);
            this.tbxFilterEBR.TabIndex = 5;
            this.tbxFilterEBR.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // dgvWaferSet
            // 
            this.dgvWaferSet.AllowUserToResizeColumns = false;
            this.dgvWaferSet.AllowUserToResizeRows = false;
            this.dgvWaferSet.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dgvWaferSet.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvWaferSet.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.dgvWaferSet.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWaferSet.Location = new System.Drawing.Point(6, 319);
            this.dgvWaferSet.Margin = new System.Windows.Forms.Padding(2);
            this.dgvWaferSet.Name = "dgvWaferSet";
            this.dgvWaferSet.RowTemplate.Height = 33;
            this.dgvWaferSet.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvWaferSet.Size = new System.Drawing.Size(480, 224);
            this.dgvWaferSet.TabIndex = 7;
            this.dgvWaferSet.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvWaferSet_CellEndEdit);
            this.dgvWaferSet.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvWaferSet_CellValidating);
            this.dgvWaferSet.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvWaferSet_EditingControlShowing);
            // 
            // tbxFilterPjt
            // 
            this.tbxFilterPjt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterPjt.Location = new System.Drawing.Point(199, 14);
            this.tbxFilterPjt.Margin = new System.Windows.Forms.Padding(2);
            this.tbxFilterPjt.Name = "tbxFilterPjt";
            this.tbxFilterPjt.Size = new System.Drawing.Size(108, 26);
            this.tbxFilterPjt.TabIndex = 4;
            this.tbxFilterPjt.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(3, 16);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(187, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "Filter (PJT/EBR/SubLot):";
            // 
            // tbxWafers
            // 
            this.tbxWafers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbxWafers.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxWafers.Location = new System.Drawing.Point(490, 319);
            this.tbxWafers.Margin = new System.Windows.Forms.Padding(2);
            this.tbxWafers.Multiline = true;
            this.tbxWafers.Name = "tbxWafers";
            this.tbxWafers.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbxWafers.Size = new System.Drawing.Size(196, 224);
            this.tbxWafers.TabIndex = 8;
            this.tbxWafers.TextChanged += new System.EventHandler(this.tbxWafers_TextChanged);
            // 
            // tbxFilterSublot
            // 
            this.tbxFilterSublot.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterSublot.Location = new System.Drawing.Point(419, 14);
            this.tbxFilterSublot.Margin = new System.Windows.Forms.Padding(2);
            this.tbxFilterSublot.Name = "tbxFilterSublot";
            this.tbxFilterSublot.Size = new System.Drawing.Size(108, 26);
            this.tbxFilterSublot.TabIndex = 6;
            this.tbxFilterSublot.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(283, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(699, 574);
            this.tabControl1.TabIndex = 9;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gbxEBR_ATE_Tracking);
            this.tabPage1.Controls.Add(this.tbxWafers);
            this.tabPage1.Controls.Add(this.dgvWaferSet);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(691, 548);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "EBR-WaferID";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gbxEBR_ATE_Tracking
            // 
            this.gbxEBR_ATE_Tracking.Controls.Add(this.label1);
            this.gbxEBR_ATE_Tracking.Controls.Add(this.tbxFilterEBR);
            this.gbxEBR_ATE_Tracking.Controls.Add(this.tbxFilterSublot);
            this.gbxEBR_ATE_Tracking.Controls.Add(this.dgvEbrs);
            this.gbxEBR_ATE_Tracking.Controls.Add(this.tbxFilterPjt);
            this.gbxEBR_ATE_Tracking.Location = new System.Drawing.Point(6, 6);
            this.gbxEBR_ATE_Tracking.Name = "gbxEBR_ATE_Tracking";
            this.gbxEBR_ATE_Tracking.Size = new System.Drawing.Size(680, 308);
            this.gbxEBR_ATE_Tracking.TabIndex = 0;
            this.gbxEBR_ATE_Tracking.TabStop = false;
            this.gbxEBR_ATE_Tracking.Text = "EBR_ATE_Tracking";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.gbxTempbox);
            this.tabPage2.Controls.Add(this.btnScpiConnect);
            this.tabPage2.Controls.Add(this.tbxGPIB);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(691, 548);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // gbxTempbox
            // 
            this.gbxTempbox.Controls.Add(this.lblCurrentTempFromNI);
            this.gbxTempbox.Controls.Add(this.btnNITemp);
            this.gbxTempbox.Controls.Add(this.btnSetTemperature);
            this.gbxTempbox.Controls.Add(this.numericUpDown1);
            this.gbxTempbox.Controls.Add(this.btnTempOnOff);
            this.gbxTempbox.Enabled = false;
            this.gbxTempbox.Location = new System.Drawing.Point(3, 42);
            this.gbxTempbox.Name = "gbxTempbox";
            this.gbxTempbox.Size = new System.Drawing.Size(685, 194);
            this.gbxTempbox.TabIndex = 2;
            this.gbxTempbox.TabStop = false;
            this.gbxTempbox.Text = "groupBox1";
            // 
            // lblCurrentTempFromNI
            // 
            this.lblCurrentTempFromNI.AutoSize = true;
            this.lblCurrentTempFromNI.Location = new System.Drawing.Point(426, 24);
            this.lblCurrentTempFromNI.Name = "lblCurrentTempFromNI";
            this.lblCurrentTempFromNI.Size = new System.Drawing.Size(35, 13);
            this.lblCurrentTempFromNI.TabIndex = 6;
            this.lblCurrentTempFromNI.Text = "label5";
            // 
            // btnNITemp
            // 
            this.btnNITemp.Location = new System.Drawing.Point(420, 16);
            this.btnNITemp.Name = "btnNITemp";
            this.btnNITemp.Size = new System.Drawing.Size(163, 82);
            this.btnNITemp.TabIndex = 5;
            this.btnNITemp.Text = "Start Monitoring NI-Temp";
            this.btnNITemp.UseVisualStyleBackColor = true;
            this.btnNITemp.Click += new System.EventHandler(this.btnNITemp_Click);
            // 
            // btnSetTemperature
            // 
            this.btnSetTemperature.Location = new System.Drawing.Point(237, 19);
            this.btnSetTemperature.Name = "btnSetTemperature";
            this.btnSetTemperature.Size = new System.Drawing.Size(75, 23);
            this.btnSetTemperature.TabIndex = 4;
            this.btnSetTemperature.Text = "SET";
            this.btnSetTemperature.UseVisualStyleBackColor = true;
            this.btnSetTemperature.Click += new System.EventHandler(this.btnSetTemperature_Click);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(6, 19);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            125,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            55,
            0,
            0,
            -2147483648});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(225, 20);
            this.numericUpDown1.TabIndex = 3;
            this.numericUpDown1.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // btnTempOnOff
            // 
            this.btnTempOnOff.Location = new System.Drawing.Point(589, 16);
            this.btnTempOnOff.Name = "btnTempOnOff";
            this.btnTempOnOff.Size = new System.Drawing.Size(90, 82);
            this.btnTempOnOff.TabIndex = 2;
            this.btnTempOnOff.Text = "button2";
            this.btnTempOnOff.UseVisualStyleBackColor = true;
            this.btnTempOnOff.Click += new System.EventHandler(this.btnTempOnOff_Click);
            // 
            // btnScpiConnect
            // 
            this.btnScpiConnect.Location = new System.Drawing.Point(109, 13);
            this.btnScpiConnect.Name = "btnScpiConnect";
            this.btnScpiConnect.Size = new System.Drawing.Size(75, 23);
            this.btnScpiConnect.TabIndex = 1;
            this.btnScpiConnect.Text = "Connect";
            this.btnScpiConnect.UseVisualStyleBackColor = true;
            this.btnScpiConnect.Click += new System.EventHandler(this.btnScpiConnect_Click);
            // 
            // tbxGPIB
            // 
            this.tbxGPIB.Location = new System.Drawing.Point(3, 13);
            this.tbxGPIB.Name = "tbxGPIB";
            this.tbxGPIB.Size = new System.Drawing.Size(100, 20);
            this.tbxGPIB.TabIndex = 0;
            this.tbxGPIB.Text = "GPIB1::5::INSTR";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.flowLayoutPanel1);
            this.groupBox2.Location = new System.Drawing.Point(186, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(91, 661);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "ToolBox";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnKillExcel);
            this.flowLayoutPanel1.Controls.Add(this.btnCheckFTP);
            this.flowLayoutPanel1.Controls.Add(this.btnFTPDownload);
            this.flowLayoutPanel1.Controls.Add(this.btnDebugFeature);
            this.flowLayoutPanel1.Controls.Add(this.btnUnsubscribe);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(85, 642);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // btnFTPDownload
            // 
            this.btnFTPDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFTPDownload.Enabled = false;
            this.btnFTPDownload.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnFTPDownload.Location = new System.Drawing.Point(3, 87);
            this.btnFTPDownload.Name = "btnFTPDownload";
            this.btnFTPDownload.Size = new System.Drawing.Size(79, 36);
            this.btnFTPDownload.TabIndex = 2;
            this.btnFTPDownload.Text = "Download Waveform";
            this.btnFTPDownload.UseVisualStyleBackColor = true;
            this.btnFTPDownload.Click += new System.EventHandler(this.btnFTPDownload_Click);
            // 
            // btnDebugFeature
            // 
            this.btnDebugFeature.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDebugFeature.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnDebugFeature.Location = new System.Drawing.Point(3, 129);
            this.btnDebugFeature.Name = "btnDebugFeature";
            this.btnDebugFeature.Size = new System.Drawing.Size(79, 36);
            this.btnDebugFeature.TabIndex = 2;
            this.btnDebugFeature.Text = "Debug_Hang Check";
            this.btnDebugFeature.UseVisualStyleBackColor = true;
            this.btnDebugFeature.Click += new System.EventHandler(this.btnDebugFeature_Click);
            // 
            // btnUnsubscribe
            // 
            this.btnUnsubscribe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUnsubscribe.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnUnsubscribe.Location = new System.Drawing.Point(3, 171);
            this.btnUnsubscribe.Name = "btnUnsubscribe";
            this.btnUnsubscribe.Size = new System.Drawing.Size(79, 36);
            this.btnUnsubscribe.TabIndex = 3;
            this.btnUnsubscribe.Text = "Debug_Unsubscribe";
            this.btnUnsubscribe.UseVisualStyleBackColor = true;
            this.btnUnsubscribe.Click += new System.EventHandler(this.btnUnsubscribe_Click);
            // 
            // tbxLogs
            // 
            this.tbxLogs.Location = new System.Drawing.Point(283, 592);
            this.tbxLogs.Multiline = true;
            this.tbxLogs.Name = "tbxLogs";
            this.tbxLogs.Size = new System.Drawing.Size(699, 81);
            this.tbxLogs.TabIndex = 2;
            // 
            // btnCheckFTP
            // 
            this.btnCheckFTP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCheckFTP.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnCheckFTP.Location = new System.Drawing.Point(3, 45);
            this.btnCheckFTP.Name = "btnCheckFTP";
            this.btnCheckFTP.Size = new System.Drawing.Size(79, 36);
            this.btnCheckFTP.TabIndex = 4;
            this.btnCheckFTP.Text = "Check FTP";
            this.btnCheckFTP.UseVisualStyleBackColor = true;
            this.btnCheckFTP.Click += new System.EventHandler(this.btnCheckFTP_Click);
            // 
            // FormSeoulHelper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(989, 680);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.tbxLogs);
            this.Controls.Add(this.gbxTestOption);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1005, 719);
            this.MinimumSize = new System.Drawing.Size(1005, 719);
            this.Name = "FormSeoulHelper";
            this.ShowIcon = false;
            this.Text = "FormSeoulHelper";
            this.Load += new System.EventHandler(this.FormSeoulHelper_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormSeoulHelper_KeyDown);
            this.gbxTestOption.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEbrs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWaferSet)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.gbxEBR_ATE_Tracking.ResumeLayout(false);
            this.gbxEBR_ATE_Tracking.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.gbxTempbox.ResumeLayout(false);
            this.gbxTempbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox clbTestOption;
        private System.Windows.Forms.GroupBox gbxTestOption;
        private System.Windows.Forms.Button btnKillExcel;
        private System.Windows.Forms.DataGridView dgvEbrs;
        private System.Windows.Forms.TextBox tbxFilterEBR;
        private System.Windows.Forms.DataGridView dgvWaferSet;
        private System.Windows.Forms.TextBox tbxFilterPjt;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbxWafers;
        private System.Windows.Forms.TextBox tbxFilterSublot;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox gbxEBR_ATE_Tracking;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnFTPDownload;
        private System.Windows.Forms.TextBox tbxLogs;
        private System.Windows.Forms.Button btnDebugFeature;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnScpiConnect;
        private System.Windows.Forms.TextBox tbxGPIB;
        private System.Windows.Forms.GroupBox gbxTempbox;
        private System.Windows.Forms.Button btnTempOnOff;
        private System.Windows.Forms.Button btnSetTemperature;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label lblCurrentTempFromNI;
        private System.Windows.Forms.Button btnNITemp;
        private System.Windows.Forms.Button btnUnsubscribe;
        private System.Windows.Forms.Button btnCheckFTP;
    }
}