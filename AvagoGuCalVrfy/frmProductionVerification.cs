using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Avago.ATF.StandardLibrary;

namespace AvagoGU
{
    public partial class frmProductionVerification : Form
    {
        
        public frmProductionVerification()
        {
            InitializeComponent();
            lb_Select.Items.Clear();
            lb_Select.DataSource = GU.dutIdAllLoose.Keys.ToList();
        }
        protected void lb_Select_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            //if ((int)e.KeyChar == 13)
            //{
            //    this.DialogResult = DialogResult.OK;

            //    return;
            //}
        }

        private void lb_Select_SelectedIndexChanged(object sender, EventArgs e)
        {
            ////Assumming only 1 site for Production. Need to change for Multisites
            GU.sitesUserReducedList.Clear();
            GU.sitesUserReducedList.Add(1);

            GU.selectedBatch = Convert.ToInt32(lb_Select.SelectedItem);
            GU.dutIdLooseUserReducedList = GU.dutIdAllLoose[GU.selectedBatch];
        }

        private void frmProductionVerification_Load(object sender, EventArgs e)
        {
            if (GU.ProductionSettings.VrfyBypass_Allow)
            {
                String templotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
                if (templotID.Contains("PT") || templotID.Contains("W") || templotID.Contains("B"))
                {
                    btnBypass.Enabled = true;
                    button1.Enabled = false;
                }
                else
                {
                    btnBypass.Enabled = false;  //Ori is true
                    //button1.Enabled = false;
                    button1.Enabled = true;         //enable 'RUN GU CAL' button after pass GU Cal/Verify - requested by Inari
                }
            }
            else
            {
                String templotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
                if (templotID.Contains("PT") || templotID.Contains("W") || templotID.Contains("B"))
                {
                    btnBypass.Enabled = false;
                    button1.Enabled = false;
                }

                else
                {
                    btnBypass.Enabled = false;
                    button1.Enabled = true;
                }
            }
            if (!GU.ProductionSettings.NewDevice)
            {
                Set_ICC_Status();
                Set_GU_Cal_Status();
                Set_GU_Verify_Status();
                Set_GU_Verify_Date();
            }
            else
            {
                chkICC_Cal.Checked = true;
                lblICC_CalStatus.Text = "New";
                lblICC_CalStatus.ForeColor = Color.Blue;
                chkGU_Cal.Checked = true;
                lbl_GU_Cal_Status.Text = "New";
                lbl_GU_Cal_Status.ForeColor = Color.Blue;
                lblGUVerify_Status.Text = "New";
                lblGUVerify_Status.ForeColor = Color.Blue; 
                lblGUVerify_Date.Text = "New";
                lblGUVerify_Date.ForeColor = Color.Blue;

            }
        }
  
        private void Set_ICC_Status ()
        {
            if (GU.ProductionSettings.ICC_Cal_PassStatus)
            {
                lblICC_CalStatus.Text = "PASS";
                lblICC_CalStatus.ForeColor = Color.Green;
            }
            else
            {
                lblICC_CalStatus.Text = "FAIL";
                lblICC_CalStatus.ForeColor = Color.Red;
                chkICC_Cal.Checked = true;
            }
        }
   
        private void Set_GU_Cal_Status ()
        {
            if (GU.ProductionSettings.Corr_PassStatus)
            {
                lbl_GU_Cal_Status.Text = "PASS";
                lbl_GU_Cal_Status.ForeColor = Color.Green;
            }
            else
            {
                lbl_GU_Cal_Status.Text = "FAIL";
                lbl_GU_Cal_Status.ForeColor = Color.Red;
                chkGU_Cal.Checked = true;
            }
        }
        private void Set_GU_Verify_Status ()
        {
            if (GU.ProductionSettings.Vrfy_PassStatus)
            {
                if (GU.ProductionSettings.VrfyBypass_Allow)
                {
                    lblGUVerify_Status.Text = "PASS";
                    lblGUVerify_Status.ForeColor = Color.Green;
                }
                else
                {
                    lblGUVerify_Status.Text = "PASS - Outdated";
                    lblGUVerify_Status.ForeColor = Color.Blue;
                }
            }
            else
            {
                lblGUVerify_Status.Text = "FAIL";
                lblGUVerify_Status.ForeColor = Color.Red;
            }
        }
        private void Set_GU_Verify_Date ()
        {
            lblGUVerify_Date.Text = GU.ProductionSettings.Vrfy_Date.ToString();
        }

        private void btnBypass_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.FormClosing -= new System.Windows.Forms.FormClosingEventHandler(this.frmProductionVerification_FormClosing);  //INARI
            this.Close();  //INARI
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (chkGU_Cal.Checked && chkICC_Cal.Checked)
            {
                DialogResult = DialogResult.OK;
            }
            else if (chkGU_Cal.Checked && !chkICC_Cal.Checked)
            {
                DialogResult = DialogResult.Ignore;
            }
            else if(!chkGU_Cal.Checked && !chkICC_Cal.Checked)
            {
                DialogResult = DialogResult.Retry;
            }
            this.FormClosing -= new System.Windows.Forms.FormClosingEventHandler(this.frmProductionVerification_FormClosing); //INARI
            this.Close(); //INARI
        }

        private void chkICC_Cal_CheckedChanged(object sender, EventArgs e)
        {
            if (chkICC_Cal.Checked)
            {
                chkGU_Cal.Checked = true;
            }
            if (lblICC_CalStatus.Text == "FAIL")
            {
                chkICC_Cal.Checked = true;
                chkGU_Cal.Checked = true;
            }
        }

        private void chkGU_Cal_CheckedChanged(object sender, EventArgs e)
        {
            if (chkICC_Cal.Checked)
            {
                chkGU_Cal.Checked = true;
            }
            if (lbl_GU_Cal_Status.Text == "FAIL")
            {
                chkGU_Cal.Checked = true;
            }
        }
        private void frmProductionVerification_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void frmProductionVerification_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.KeyChar == Convert.ToChar(Keys.Enter))
            //    MessageBox.Show("Hey, I will report to HR");
        }

        private void frmProductionVerification_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
