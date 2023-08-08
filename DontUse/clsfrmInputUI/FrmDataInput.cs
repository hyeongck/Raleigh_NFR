using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Globalization;
using ClothoSharedItems;
//using ClothoLibStandard;

namespace clsfrmInputUI
{
    public partial class FrmDataInput : Form
    {
        private string T_LotID = "";
        private string T_SubLotID = "";
        private string T_OperatorID = "";
        private string T_HandlerID = "";
        private string T_ContactorID = "";
        private string T_DeviceID = "";
        private string T_LoadBoardID = "";
        private string T_MfgLotID = "";
        private string T_RevID = "";
        public string Date;
        public string Shift;
        public int PassCharCount = 0;
        public bool EnablePassword = true;
        public bool AdminLevel = false;

        public bool b_txtLotID = true;
        public bool b_txtSubLotID = true;
        public bool b_txtOperatorID = true;
        public bool b_txtHandlerID = true;
        public bool b_txtContactorID = true;
        public bool b_txtDeviceID = true;
        public bool b_txtLoadBoardID = true;
        public bool b_txtMfgLotID = true;

        public string Set_Title
        {
            set
            {
                this.Text = value;
            }
        }
        public string LotID
        {
            get
            {
                return T_LotID;
            }
            set
            {
                T_LotID = value;
            }
        }
        public string ContactorID
        {
            get
            {
                return T_ContactorID;
            }
            set
            {
                T_ContactorID = value;
            }
        }
        public string HandlerID
        {
            get
            {
                return T_HandlerID;
            }
            set
            {
                T_HandlerID = value;
            }
        }
        public string OperatorID
        {
            get
            {
                return T_OperatorID;

            }
            set
            {
                T_OperatorID = value;
            }
        }
        public string SublotID
        {
            get
            {
                return T_SubLotID;

            }
            set
            {
                T_SubLotID = value;
            }
        }
        public string LoadBoardID
        {
            get
            {
                return T_LoadBoardID;
            }
            set
            {
                T_LoadBoardID = value;
            }
        }
        public string MfgLotID
        {
            get
            {
                return T_MfgLotID;
            }
            set
            {
                T_MfgLotID = value;
            }
        }
        public string RevID
        {
            get
            {
                return T_RevID;
            }
        }
        public string DeviceID
        {
            get
            {
                return T_DeviceID;
            }
            set
            {
                T_DeviceID = value;
            }
        }

        public FrmDataInput()
        {
            InitializeComponent();
        }

        private void FrmDataInput_Load(object sender, EventArgs e)
        {
            DateTime Timenow = DateTime.Now;
            Date = Timenow.ToString("ddMMyy");
            string sttime = Timenow.ToString("HHmmss");
            CalendarWeekRule weekRule = CalendarWeekRule.FirstFourDayWeek;
            DayOfWeek firstWeekDay = DayOfWeek.Monday;
            Calendar calendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;
            int currentWeek = calendar.GetWeekOfYear(Timenow, weekRule, firstWeekDay);
            double lgtime = Convert.ToDouble(sttime);
            if (lgtime > 170000 || lgtime < 70000) Shift = "N";
            else if (lgtime > 70000 || lgtime < 170000) Shift = "M";

            this.txtOperatorID.Select();
            AdminLevel = false;

            //set to enable/disable text box input

            txtOperatorID.Text = T_OperatorID;
            txtLotID.Text = T_LotID;
            txtSubLotID.Text = T_SubLotID;
            txtHandlerID.Text = T_HandlerID;
            txtContactorID.Text = T_ContactorID;
            txtMfgLotID.Text = T_MfgLotID.CIvStartsWith("MFG") ? T_MfgLotID.ToUpper().Replace("MFG", "") : T_MfgLotID;

            txtLbID.Text = T_LoadBoardID;
            txtDeviceID.Text = T_DeviceID;

            txtContactorID.Enabled = b_txtContactorID;
            txtMfgLotID.Enabled = b_txtMfgLotID;
            txtLbID.Enabled = b_txtLoadBoardID;
        }

        #region KeyPressEvent - set focus

        //1 - Operator ID
        private void txtOperatorID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtLotID.Focus();
            }

            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (Convert.ToByte(e.KeyChar) != 0x08))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //2 - Lot ID
        private void txtLotID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtSubLotID.Focus();
            }
            else if (!Char.IsLetter(e.KeyChar) && !Char.IsDigit(e.KeyChar) && (e.KeyChar.ToString() != "-"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //3 - Mfg ID
        private void txtMfgLotID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtDeviceID.Focus();
            }
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (e.KeyChar.ToString() != "-"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //4 - Device ID
        private void txtDeviceID_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtSubLotID.Focus();
            }
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (e.KeyChar.ToString() != "-") && (e.KeyChar.ToString() != "_"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //5 - Sub Lot ID
        private void txtSubLotID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtHandlerID.Focus();
            }
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (Convert.ToByte(e.KeyChar) != 0x08))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //6 - Handler ID
        private void txtHandlerID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtContactorID.Focus();
            }
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //7 - Contactor ID
        private void txtContactorID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                txtLbID.Focus();
            }
            else if (!Char.IsLetter(e.KeyChar) && !Char.IsDigit(e.KeyChar) && (e.KeyChar.ToString() != "-"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //8 - Load board ID
        private void txtLbID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                button1.Focus();
            }
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (e.KeyChar.ToString() != "-"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        #endregion KeyPressEvent

        #region EnterEvent

        private void txtOperatorID_Enter(object sender, EventArgs e)
        {
            txtOperatorID.SelectAll();
        }
        private void txtLotID_Enter(object sender, EventArgs e)
        {
            txtLotID.SelectAll();
        }
        private void txtMfgLotID_Enter(object sender, EventArgs e)
        {
            txtMfgLotID.SelectAll();
        }
        private void txtDeviceID_Enter(object sender, EventArgs e)
        {
            txtDeviceID.SelectAll();
        }
        private void txtSubLotID_Enter(object sender, EventArgs e)
        {
            txtSubLotID.SelectAll();
        }
        private void txtHandlerID_Enter(object sender, EventArgs e)
        {
            txtHandlerID.SelectAll();
        }
        private void txtContactorID_Enter(object sender, EventArgs e)
        {
            txtContactorID.SelectAll();
        }
        private void txtLbID_Enter(object sender, EventArgs e)
        {
            txtLbID.SelectAll();
        }

        #endregion EnterEvent

        #region MouseDownEvent

        private void txtOperatorID_MouseDown(object sender, MouseEventArgs e)
        {
            txtOperatorID.SelectAll();
        }

        private void txtLotID_MouseDown(object sender, MouseEventArgs e)
        {
            txtLotID.SelectAll();
        }

        private void txtMfgLotID_MouseDown(object sender, MouseEventArgs e)
        {
            txtMfgLotID.SelectAll();
        }

        private void txtDeviceID_MouseDown_1(object sender, MouseEventArgs e)
        {
            txtDeviceID.SelectAll();
        }

        private void txtSubLotID_MouseDown(object sender, MouseEventArgs e)
        {
            txtSubLotID.SelectAll();
        }

        private void txtHandlerID_MouseDown(object sender, MouseEventArgs e)
        {
            txtHandlerID.SelectAll();
        }

        private void txtContactorID_MouseDown(object sender, MouseEventArgs e)
        {
            txtContactorID.SelectAll();
        }

        private void txtLbID_MouseDown(object sender, MouseEventArgs e)
        {
            txtLbID.SelectAll();
        }

        #endregion MouseDownEvent

        #region KeydownEvent

        private void txtOperatorID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtLotID.Focus();
        }
        private void txtLotID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                e.Handled = true;

            if (e.KeyCode == Keys.Tab)
                txtMfgLotID.Focus();
        }
        private void txtMfgLotID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtDeviceID.Focus();
        }
        private void txtDeviceID_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                e.Handled = true;

            if (e.KeyCode == Keys.Tab)
                txtSubLotID.Focus();
        }
        private void txtSubLotID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtHandlerID.Focus();
        }
        private void txtHandlerID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtContactorID.Focus();
        }
        private void txtContactorID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                e.Handled = true;

            if (e.KeyCode == Keys.Tab)
                txtLbID.Focus();
        }
        private void txtLbID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                e.Handled = true;

            if (e.KeyCode == Keys.Tab)
                button1.Focus();
        }
        private void button1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtOperatorID.Focus();
        }

        #endregion KeydownEvent

        #region Other Event

        //Picture click enable password char  
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            PassCharCount++;

            if (PassCharCount % 2 != 0) //odd   
            {
                EnablePassword = true;
                txtOperatorID.PasswordChar = '*';
            }
            else //even
            {
                EnablePassword = false;
                txtOperatorID.PasswordChar = '\0';
            }
        }

        #endregion Other Event

        //Ok button - Entry checking
        private void button1_Click(object sender, EventArgs e)
        {
            //Set to true if entry field is N/A
            bool passflag_ContID = false;
            bool passflag_OPID = false;
            bool passflag_LBID = true;
            bool passflag_HandlerID = false;
            bool passflag_LotID = false;
            bool passflag_SubLotID = false;
            bool passflag_MfgID = true;
            bool passflag_DeviceID = true; //Jupiter only (can be used as Assembly ID for other products)
            List<KeyValuePair<bool, string>> passFails = new List<KeyValuePair<bool, string>>();

            //set default value 
            T_MfgLotID = "000001";

            //Admin mode
            if (EnablePassword == true && txtOperatorID.Text.ToUpper() == "AVGO155")
            {
                txtOperatorID.Text = "A0001";
                txtLotID.Text = "PT0000000001";
                txtSubLotID.Text = "1A";
                txtHandlerID.Text = "EIS001";
                txtContactorID.Text = "YP-1234-001";
                txtMfgLotID.Text = "000001";
                txtLbID.Text = "JP0001";
                txtDeviceID.Text = "AFEM-8210-A";
                AdminLevel = true;
            }

            #region OperatorID checking

            List<bool> rxOpID = new List<bool>();

            //Inari:
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[I]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[P]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[N]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[W]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[D]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[L]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[R]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[A]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[C]\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^INT\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWI\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWN\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWM\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWP\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^ISK\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWR\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^T\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^AM\d{1,8}")); //Amkor
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[0-9]{1,6}")); //ASEKr

            foreach (bool rxOP in rxOpID)
            {
                if (rxOP)
                {
                    T_OperatorID = txtOperatorID.Text;
                    passflag_OPID = true;
                    break;
                }
            }

            //if (!passflag_OPID)
            //    MessageBox.Show("No matching for Operator ID " + "(" + txtOperatorID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_OPID, $"Operator ID ({txtOperatorID.Text})"));

            #endregion

            #region LotID checking

            List<bool> rxLotID = new List<bool>();

            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^STDUNIT\d{2}-\d{6}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^STDUNIT\d{3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^BINCHECK-\d{1,10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^ENGR-\d{1,10}"));

            //PA
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}-\w{1}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}-\w{2}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{9}-\w{1}"));

            //ASEKr
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\w{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{8}-\w{1,5}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{3}\w{2}\d{3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{1,4}\w{1,3}\d{1,4}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{1,4}\w{1,3}\d{1,4}-\w{1,3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{1,4}\w{1,3}\d{1,4}\w{1,3}-\w{1,3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B[A-Z0-9]{8}$"));    //allowed B + 8 alphanumeric only eg . B123AB123
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B[A-Z0-9]{9}$"));    //allowed B + 9 alphanumeric only eg . B123AB1234

            //Amkor
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^M\d{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^M\d{8}-\w{1,5}"));

            //Fbar
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^FT\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^FT\d{10}-\w{1}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^FT\d{10}-\w{2}"));

            //MM
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MT\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MU\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MI\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MC\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MA\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^VT\d{10}"));

            foreach (bool rxLot in rxLotID)
            {
                if (rxLot)
                {
                    if (txtLotID.Text.Contains("STDUNIT"))
                    {
                        T_LotID = txtLotID.Text + "-" + Date + "-" + Shift;
                    }
                    else if (txtLotID.Text.Contains("ENG"))
                    {
                        if (txtLotID.Text.Length > 13) T_LotID = txtLotID.Text.Remove(13);
                        else T_LotID = txtLotID.Text;
                    }
                    else
                    {
                        if (txtLotID.Text.Length > 14) T_LotID = txtLotID.Text.Remove(14);
                        else T_LotID = txtLotID.Text;
                    }

                    passflag_LotID = true;
                    break;
                }
            }

            //if (!passflag_LotID)
            //    MessageBox.Show("No matching for Lot ID " + "(" + txtLotID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_LotID, $"Lot ID ({txtLotID.Text})"));

            #endregion

            #region MfgID checking

            int iMfgID = 0;
            List<bool> rxMfgID = new List<bool>();

            rxMfgID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtMfgLotID.Text.ToUpper(), @"^[0-9]{6}"));
            rxMfgID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtMfgLotID.Text.ToUpper(), @"^\w[NA]"));

            foreach (bool rxMfg in rxMfgID)
            {
                if (rxMfg)
                {
                    T_MfgLotID = txtMfgLotID.Text;
                    passflag_MfgID = true;
                    break;
                }
            }

            try
            {
                iMfgID = Convert.ToInt32(txtMfgLotID.Text);
            }
            catch
            {
                //passflag_MfgID = false;
            }

            if (iMfgID > 131071)
            {
                passflag_MfgID = false;
                MessageBox.Show("Mfg Lot Number cannot bigger than 131071 <= " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");
            }

            //if (!passflag_MfgID)
            //    MessageBox.Show("No matching for MfgLot ID " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_MfgID, $"Device ID ({txtMfgLotID.Text})"));

            #endregion MfgID Check

            #region Device ID checking

            List<bool> rxDeviceID = new List<bool>();
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]$")); //Only allow AFEM-8030-A
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[B]$")); //Only allow AFEM-8030-MB
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[T]$")); //Only allow AFEM-8030-MT
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]\w{0}[C]$")); //Only allow AFEM-8030-AC
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[B]\w{0}[C]$")); //Only allow AFEM-8030-MBC
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[T]\w{0}[C]$")); //Only allow AFEM-8030-MTC
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-\w{0}[A]$")); //Only allow ENGR-8092-A
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-\w{0}[M]$")); //Only allow ENGR-8092-M
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]\w{0}[L]$")); //Only allow AFEM-8092-AL
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]\w{0}[L]\w{0}[F]$")); //Only allow AFEM-8092-ALF
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[S]$")); //Only allow AFEM-8092-MS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[M]\w{0}[L]$")); //Only allow AFEM-8092-AL
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^NA$"));  

            foreach (bool rxDevID in rxDeviceID)
            {
                if (rxDevID)
                {
                    T_DeviceID = txtDeviceID.Text;
                    passflag_DeviceID = true;
                    break;
                }
            }

            //ASEkr checking:
            bool KA, KE = false;
            bool A_SL, AC_SL = false;
            KA = System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{3}[K]\w{0}[A]\w{0}\d{3}$"); //from lot ID
            KE = System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{3}[K]\w{0}[E]\w{0}\d{3}$"); //from lot ID
            A_SL = System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]_SL$");
            AC_SL = System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-\w{0}[A]\w{0}[C]_SL$");
            T_DeviceID = txtDeviceID.Text;

            if (KA)
            {
                passflag_DeviceID = (A_SL == true ? true : false);
            }
            else if (KE)
            {
                passflag_DeviceID = (AC_SL == true ? true : false);
            }

            if (passflag_DeviceID)
                T_DeviceID = txtDeviceID.Text;
            //else
            //    MessageBox.Show("No matching for Device ID " + "(" + txtDeviceID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_DeviceID, $"Sub lot ID ({txtDeviceID.Text})"));

            #endregion

            #region Sublot ID checking

            List<bool> rxSubLotID = new List<bool>();

            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^1\w{0}[ABCDE]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^2\w{0}[ABCDE]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^3\w{0}[ABCDE]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^1\w{0}[ABCDE]\w[QA]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^2\w{0}[ABCDE]\w[QA]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^3\w{0}[ABCDE]\w[QA]$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^RE$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^LYT$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^PE$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^QA$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^QA1$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^QAR$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^EO$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^EO1$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^QE$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^COQ$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^CCOQ$"));
            rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^Bin3$"));

            foreach (bool rxSlotID in rxSubLotID)
            {
                if (rxSlotID)
                {
                    T_SubLotID = txtSubLotID.Text;
                    passflag_SubLotID = true;
                    break;
                }
            }
            //if (!passflag_SubLotID)
            //    MessageBox.Show("No matching for Sub lot ID " + "(" + txtSubLotID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_SubLotID, $"Sub lot ID ({txtSubLotID.Text})"));

            #endregion Sublot ID checking

            #region HandlerID checking

            List<bool> rxHandlerID = new List<bool>();
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^EIS\d{2}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^EIS\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^SRM\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^S9\d{4}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^HT\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^IM\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^NS\d{4}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^HT\d{4}", RegexOptions.IgnoreCase));

            foreach (bool rxHandler in rxHandlerID)
            {
                if (rxHandler)
                {
                    T_HandlerID = txtHandlerID.Text;
                    passflag_HandlerID = true;
                    break;
                }
            }

            //if (!passflag_HandlerID)
            //    MessageBox.Show("No matching for Handler SN " + "(" + txtHandlerID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_HandlerID, $"Handler SN ({txtHandlerID.Text})"));

            #endregion

            #region ContactorID checking

            List<bool> rxContactorID = new List<bool>();
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^LNMX\d{4}", RegexOptions.IgnoreCase)); //Jupiter
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^SP\d{2}", RegexOptions.IgnoreCase)); //Spyro
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^YP-\d{4}-\d{3}", RegexOptions.IgnoreCase));
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^LN-\d{4}-\d{3}", RegexOptions.IgnoreCase));
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^LN-\d{4}-\w{1}\d{2}-\d{3}", RegexOptions.IgnoreCase)); //Pinot
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^NA$", RegexOptions.IgnoreCase));

            //foreach (bool rxContactor in rxContactorID)
            //{
            //    if (rxContactor)
            //    {
            T_ContactorID = txtContactorID.Text;
            passflag_ContID = true;
            //        break;
            //    }
            //}

            //if (!passflag_ContID)
            //    MessageBox.Show("No matching for Contactor ID " + "(" + txtContactorID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_ContID, $"Handler SN ({txtContactorID.Text})"));

            #endregion

            #region LoadboardID checking

            List<bool> rxLboardID = new List<bool>();
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LB-\d{4}-\d{3}", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LB-\d{4}-\d{3}-\d{3}", RegexOptions.IgnoreCase)); //Pinot
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^JP\d{4}", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LBSP", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^NA$", RegexOptions.IgnoreCase));

            //foreach (bool rxLbID in rxLboardID)
            //{
            //    if (rxLbID)
            //    {
            T_LoadBoardID = txtLbID.Text;
            passflag_LBID = true;
            //        break;
            //    }
            //}

            //if (!passflag_LBID)
            //    MessageBox.Show("No matching for Load Board ID " + "(" + txtLbID.Text + ")" + ", please re-enter!");

            passFails.Add(new KeyValuePair<bool, string>(passflag_LBID, $"Handler SN ({txtLbID.Text})"));

            #endregion LoadboardID checking

            if (passflag_ContID && passflag_LBID && passflag_OPID && passflag_HandlerID && passflag_LotID && passflag_SubLotID && passflag_DeviceID && passflag_MfgID)
                this.DialogResult = DialogResult.OK;
            else if (passFails.Any(t => t.Key == false))
            {
                MessageBox.Show(string.Format("Check for items that don't match the rule:\n\n{0}", string.Join("\n", passFails.Where(t => t.Key == false).Select(v => v.Value))));
            }
        }

        private void txtContactorID_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtMfgLotID_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtDeviceID_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtLbID_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
