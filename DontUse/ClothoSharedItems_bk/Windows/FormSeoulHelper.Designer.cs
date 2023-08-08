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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
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
            this.gbxTestOption.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEbrs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWaferSet)).BeginInit();
            this.SuspendLayout();
            // 
            // clbTestOption
            // 
            this.clbTestOption.CheckOnClick = true;
            this.clbTestOption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbTestOption.FormattingEnabled = true;
            this.clbTestOption.Location = new System.Drawing.Point(3, 17);
            this.clbTestOption.Name = "clbTestOption";
            this.clbTestOption.Size = new System.Drawing.Size(162, 550);
            this.clbTestOption.TabIndex = 0;
            this.clbTestOption.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbTestOption_ItemCheck);
            // 
            // gbxTestOption
            // 
            this.gbxTestOption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.gbxTestOption.Controls.Add(this.clbTestOption);
            this.gbxTestOption.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbxTestOption.Location = new System.Drawing.Point(12, 12);
            this.gbxTestOption.Name = "gbxTestOption";
            this.gbxTestOption.Size = new System.Drawing.Size(168, 570);
            this.gbxTestOption.TabIndex = 1;
            this.gbxTestOption.TabStop = false;
            this.gbxTestOption.Text = "Option";
            // 
            // btnKillExcel
            // 
            this.btnKillExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKillExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnKillExcel.Location = new System.Drawing.Point(779, 12);
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
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvEbrs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvEbrs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEbrs.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvEbrs.Location = new System.Drawing.Point(186, 52);
            this.dgvEbrs.Name = "dgvEbrs";
            this.dgvEbrs.RowHeadersVisible = false;
            this.dgvEbrs.Size = new System.Drawing.Size(672, 256);
            this.dgvEbrs.TabIndex = 3;
            // 
            // tbxFilterEBR
            // 
            this.tbxFilterEBR.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterEBR.Location = new System.Drawing.Point(490, 18);
            this.tbxFilterEBR.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
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
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvWaferSet.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvWaferSet.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWaferSet.Location = new System.Drawing.Point(186, 312);
            this.dgvWaferSet.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dgvWaferSet.Name = "dgvWaferSet";
            this.dgvWaferSet.RowTemplate.Height = 33;
            this.dgvWaferSet.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvWaferSet.Size = new System.Drawing.Size(467, 269);
            this.dgvWaferSet.TabIndex = 7;
            this.dgvWaferSet.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvWaferSet_CellEndEdit);
            this.dgvWaferSet.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvWaferSet_CellValidating);
            this.dgvWaferSet.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvWaferSet_EditingControlShowing);
            // 
            // tbxFilterPjt
            // 
            this.tbxFilterPjt.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterPjt.Location = new System.Drawing.Point(380, 18);
            this.tbxFilterPjt.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tbxFilterPjt.Name = "tbxFilterPjt";
            this.tbxFilterPjt.Size = new System.Drawing.Size(108, 26);
            this.tbxFilterPjt.TabIndex = 4;
            this.tbxFilterPjt.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(184, 20);
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
            this.tbxWafers.Location = new System.Drawing.Point(656, 312);
            this.tbxWafers.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tbxWafers.Multiline = true;
            this.tbxWafers.Name = "tbxWafers";
            this.tbxWafers.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbxWafers.Size = new System.Drawing.Size(202, 270);
            this.tbxWafers.TabIndex = 8;
            this.tbxWafers.TextChanged += new System.EventHandler(this.tbxWafers_TextChanged);
            // 
            // tbxFilterSublot
            // 
            this.tbxFilterSublot.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbxFilterSublot.Location = new System.Drawing.Point(600, 18);
            this.tbxFilterSublot.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tbxFilterSublot.Name = "tbxFilterSublot";
            this.tbxFilterSublot.Size = new System.Drawing.Size(108, 26);
            this.tbxFilterSublot.TabIndex = 6;
            this.tbxFilterSublot.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // FormSeoulHelper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(872, 604);
            this.Controls.Add(this.tbxWafers);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dgvWaferSet);
            this.Controls.Add(this.tbxFilterPjt);
            this.Controls.Add(this.tbxFilterSublot);
            this.Controls.Add(this.tbxFilterEBR);
            this.Controls.Add(this.dgvEbrs);
            this.Controls.Add(this.btnKillExcel);
            this.Controls.Add(this.gbxTestOption);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(888, 643);
            this.Name = "FormSeoulHelper";
            this.ShowIcon = false;
            this.Text = "FormSeoulHelper";
            this.Load += new System.EventHandler(this.FormSeoulHelper_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormSeoulHelper_KeyDown);
            this.gbxTestOption.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEbrs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWaferSet)).EndInit();
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
    }
}