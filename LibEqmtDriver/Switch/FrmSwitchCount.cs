using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibEqmtDriver
{
    public partial class FrmSwitchCount : Form
    {
        public FrmSwitchCount(string Message)
        {
            InitializeComponent();
            txtMsgBox.Text = Message;
        }

        private void BtnContinue_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
