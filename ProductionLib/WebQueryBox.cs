using System;
using System.Windows.Forms;

namespace ProductionLib2
{
    public partial class WebQueryBox : Form
    {
        public WebQueryBox()
        {
            InitializeComponent();
        }

        private void WebQueryBox_Load(object sender, EventArgs e)
        {
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (textBoxPass.Text == "cheese")
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                DialogResult = DialogResult.Retry;
                labelStatus.Text = "Password incorrect! Try again..";
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}