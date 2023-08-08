using System;
using System.Windows.Forms;

namespace ProductionLib2
{
    public partial class InspectSocketMessage : Form
    {
        public InspectSocketMessage()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}