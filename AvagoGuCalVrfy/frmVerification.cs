using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;



namespace AvagoGU
{
    public partial class frmVerification : Form
    {
        static int passwordAttempts = 0;

        public frmVerification()
        {

            InitializeComponent();

            if (!GU.passwordProtectUI)
            {
                txtbxPassword.Text = "DISABLED";
                txtbxPassword.UseSystemPasswordChar = false;
                btnPasswordEnter.Enabled = false;
                lblGuPassword.Enabled = false;
                txtbxPassword.Enabled = false;
                listboxGuSites.Enabled = true;
                lblSelectSites.Enabled = true;
            }

            listboxGuBatch.DataSource = GU.dutIdAllLoose.Keys.ToList();
            listboxGuDevices.DataSource = GU.dutIdAllLoose.First().Value;
            listboxGuSites.DataSource = GU.sitesAllExistingList;

            listboxGuDevices.SetSelected(0, false);
            listboxGuSites.SetSelected(0, true);

        }

        private void listboxGuDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            GU.dutIdLooseUserReducedList.Clear();
            foreach (object o in listboxGuDevices.SelectedItems)
            {
                GU.dutIdLooseUserReducedList.Add(Convert.ToInt32(o));
            }

            updateButtons();
        }

        private void listboxGuSites_SelectedIndexChanged(object sender, EventArgs e)
        {
            GU.sitesUserReducedList.Clear();
            foreach (object o in listboxGuSites.SelectedItems)
            {
                GU.sitesUserReducedList.Add(Convert.ToInt32(o));
            }

            if (listboxGuSites.SelectedItems.Count > 0)
            {
                lblSelectDevices.Enabled = true;
                listboxGuDevices.Enabled = true;
            }
            else
            {
                lblSelectDevices.Enabled = false;
                listboxGuDevices.Enabled = false;
            }

            updateButtons();

        }

        private void updateButtons()
        {
            //enable/disable the accept button based on how many GU devices/sites selected
            if (listboxGuDevices.SelectedItems.Count >= GU.minDutsForGu & listboxGuSites.SelectedItems.Count > 0)
            {
                runGU.Enabled = true;
                runCorrVrfy.Enabled = true;
                runVrfy.Enabled = true;
                //noRunGU.Enabled = true;    // always keep enabled
            }
            else
            {
                runGU.Enabled = false;
                runCorrVrfy.Enabled = false;
                runVrfy.Enabled = false;
                //noRunGU.Enabled = false;    // always keep enabled
            }

        }

        private void txtbxPassword_TextChanged_new(object sender, EventArgs e)
        {

        }

        private void btnPasswordEnter_Click(object sender, EventArgs e)
        {
            if (txtbxPassword.Text == GU.password)
            {
                txtbxPassword.Text = "CORRECT";
                txtbxPassword.UseSystemPasswordChar = false;
                btnPasswordEnter.Enabled = false;
                lblGuPassword.Enabled = false;
                txtbxPassword.Enabled = false;
                listboxGuSites.Enabled = true;
                lblSelectSites.Enabled = true;
                //SelectNextControl(txtbxPassword, true, true, false, true);
            }
            else
            {
                passwordAttempts++;

                if (passwordAttempts < 5)   // allow user to try again
                {
                    txtbxPassword.Text = "INCORRECT";
                    txtbxPassword.UseSystemPasswordChar = false;
                    txtbxPassword.Refresh();
                    Thread.Sleep(600);
                    txtbxPassword.ResetText();
                    txtbxPassword.UseSystemPasswordChar = true;
                    txtbxPassword.Select();
                }
                else
                {
                    txtbxPassword.Text = "MAX ATTEMPTS";
                    txtbxPassword.UseSystemPasswordChar = false;
                    lblGuPassword.Enabled = false;
                    txtbxPassword.Enabled = false;
                    btnPasswordEnter.Enabled = false;
                }
            }

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            GU.selectedBatch = Convert.ToInt16(listboxGuBatch.SelectedItem);

            listboxGuDevices.DataSource = GU.dutIdAllLoose[GU.selectedBatch];
        }

    }
}
