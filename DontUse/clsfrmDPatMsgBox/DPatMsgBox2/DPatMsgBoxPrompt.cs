using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace clsfrmDPatMsgBox.DPatMsgBox2
{
    public partial class DPatMsgBoxPrompt : Form
                
    {
        public DPatMsgBoxPrompt()
        {
            InitializeComponent();
        }

        private string Password = "pinot";

        //Ok button
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == Password)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                this.DialogResult = DialogResult.Retry;
                MessageBox.Show("Password incorrect, please try again or press cancel to unload program...");
            }
        }

        //Cancel button
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
