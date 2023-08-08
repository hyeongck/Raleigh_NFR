namespace TestPlanDriver
{
    partial class FormDoNOTTouch_DebugVars
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
            this.loadFromAtfConfigXmlButton = new System.Windows.Forms.Button();
            this.atfConfigXmlOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.resetButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.objectPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.infoLabel = new System.Windows.Forms.Label();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.bottomPanel.SuspendLayout();
            this.infoPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // loadFromAtfConfigXmlButton
            // 
            this.loadFromAtfConfigXmlButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.loadFromAtfConfigXmlButton.Location = new System.Drawing.Point(53, 8);
            this.loadFromAtfConfigXmlButton.Name = "loadFromAtfConfigXmlButton";
            this.loadFromAtfConfigXmlButton.Size = new System.Drawing.Size(152, 32);
            this.loadFromAtfConfigXmlButton.TabIndex = 1;
            this.loadFromAtfConfigXmlButton.Text = "Load from ATFConfig.xml";
            this.loadFromAtfConfigXmlButton.UseVisualStyleBackColor = true;
            this.loadFromAtfConfigXmlButton.Click += new System.EventHandler(this.loadFromAtfConfigXmlButton_Click);
            // 
            // atfConfigXmlOpenFileDialog
            // 
            this.atfConfigXmlOpenFileDialog.DefaultExt = "xml";
            this.atfConfigXmlOpenFileDialog.Filter = "ATFConfig files|*.xml";
            this.atfConfigXmlOpenFileDialog.Title = "Open ATFConfig";
            // 
            // resetButton
            // 
            this.resetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.resetButton.Location = new System.Drawing.Point(213, 8);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(152, 32);
            this.resetButton.TabIndex = 2;
            this.resetButton.Text = "&Reset";
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.closeButton.Location = new System.Drawing.Point(373, 8);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(152, 32);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // objectPropertyGrid
            // 
            this.objectPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectPropertyGrid.Location = new System.Drawing.Point(8, 8);
            this.objectPropertyGrid.Name = "objectPropertyGrid";
            this.objectPropertyGrid.Size = new System.Drawing.Size(518, 413);
            this.objectPropertyGrid.TabIndex = 0;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.closeButton);
            this.bottomPanel.Controls.Add(this.loadFromAtfConfigXmlButton);
            this.bottomPanel.Controls.Add(this.resetButton);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 461);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(534, 50);
            this.bottomPanel.TabIndex = 2;
            // 
            // infoPanel
            // 
            this.infoPanel.BackColor = System.Drawing.SystemColors.Info;
            this.infoPanel.Controls.Add(this.infoLabel);
            this.infoPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.infoPanel.Location = new System.Drawing.Point(0, 429);
            this.infoPanel.Name = "infoPanel";
            this.infoPanel.Padding = new System.Windows.Forms.Padding(8);
            this.infoPanel.Size = new System.Drawing.Size(534, 32);
            this.infoPanel.TabIndex = 1;
            this.infoPanel.Visible = false;
            // 
            // infoLabel
            // 
            this.infoLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLabel.Location = new System.Drawing.Point(8, 8);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(518, 16);
            this.infoLabel.TabIndex = 0;
            this.infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentPanel
            // 
            this.contentPanel.Controls.Add(this.objectPropertyGrid);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Padding = new System.Windows.Forms.Padding(8);
            this.contentPanel.Size = new System.Drawing.Size(534, 429);
            this.contentPanel.TabIndex = 0;
            // 
            // FormDoNOTTouch_DebugVars
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 511);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.infoPanel);
            this.Controls.Add(this.bottomPanel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(550, 550);
            this.Name = "FormDoNOTTouch_DebugVars";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Debug Environment Variables";
            this.Load += new System.EventHandler(this.FormDoNOTTouch_DebugVars_Load);
            this.bottomPanel.ResumeLayout(false);
            this.infoPanel.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button loadFromAtfConfigXmlButton;
        private System.Windows.Forms.OpenFileDialog atfConfigXmlOpenFileDialog;
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.PropertyGrid objectPropertyGrid;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.Panel contentPanel;
    }
}