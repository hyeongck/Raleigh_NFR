using MPAD_TestTimer;
using System;
using System.Windows.Forms;

namespace ProductionLib2
{
    public partial class InsepctRevisionMessage : Form
    {
        private const string letters = "ABCDEFGHI";
        public bool PassFlag { get; set; }

        private string RevisionToStage(string rev)
        {
            string outStage = "???";
            if (rev.Length == 2)
            {
                var stage = rev.Substring(0, 1);
                if (int.TryParse(rev.Substring(1, 1), out int substage))
                {
                    substage -= 1;

                    if (int.TryParse(stage, out int intstage))
                    {
                        outStage = string.Format("{0}{1}{2}", "A", intstage, letters[substage]);
                    }
                    else
                    {
                        outStage = string.Format("{0}{1}{2}", stage, 1, letters[substage]);
                    }
                }
            }
            return outStage;
        }

        private string StageToRevision(string stage)
        {
            string outRevision = "??";
            if (stage.Length == 3)
            {
                var BetaRev = stage.Substring(0, 1);
                var AlphaRev = stage.Substring(1, 1);
                var subRev = letters.IndexOf(stage.Substring(2, 1)) + 1;

                if (subRev > 0)
                {
                    if (BetaRev == "A" && int.TryParse(AlphaRev, out int intAlphaRev))
                    {
                        outRevision = string.Format("0x{0}{1}", intAlphaRev, subRev);
                    }
                    else if (BetaRev != "A")
                    {
                        outRevision = string.Format("0x{0}{1}", BetaRev, subRev);
                    }
                }
            }
            return outRevision;
        }

        public InsepctRevisionMessage(string currentRevision)
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();

            lblRevIDTCF.Text = string.Format("{0} (0x{1})", RevisionToStage(currentRevision), currentRevision);
            tbxSampleRevision.Text = RevisionToStage(currentRevision);
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (lblRevIDSample.Text == lblRevIDTCF.Text)
            {
                PassFlag = true;
            }
            else
            {
                var dlgResult = PromptManager.Instance.ShowDialogOKCancel($"Sample revision {lblRevIDSample.Text} is different with TCF revision {lblRevIDTCF.Text}, Do you want force start?", "Revision ID Mismatch");

                if (dlgResult == DialogResult.OK) PassFlag = true;
                else PassFlag = false;
            }
            this.Close();
        }

        private void tbxSampleRevision_TextChanged(object sender, EventArgs e)
        {
            if (tbxSampleRevision.Text.Length > 0)
            {
                lblRevIDSample.Text = string.Format("{0} ({1})", tbxSampleRevision.Text.ToUpper(), StageToRevision(tbxSampleRevision.Text.ToUpper()));
            }
        }

        private void tbxSampleRevision_Enter(object sender, EventArgs e)
        {
            tbxSampleRevision.Text = "";
        }

        private void tbxSampleRevision_Click(object sender, EventArgs e)
        {
            tbxSampleRevision.Text = "";
        }

        private void lblRevIDSample_TextChanged(object sender, EventArgs e)
        {
            if (lblRevIDSample.Text != lblRevIDTCF.Text)
            {
                lblRevIDSample.ForeColor = System.Drawing.Color.Yellow;
                lblRevIDSample.BackColor = System.Drawing.Color.Red;
            }
            else
            {
                lblRevIDSample.ForeColor = System.Drawing.SystemColors.ControlText;
                lblRevIDSample.BackColor = System.Drawing.Color.LightGreen;
            }
        }
    }
}