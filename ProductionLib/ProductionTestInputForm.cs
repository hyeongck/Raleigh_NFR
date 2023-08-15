using ClothoSharedItems;
using ProductionLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ProductionLib2
{
    public partial class ProductionTestInputForm : Form
    {
        private string T_productTag = "";
        private string T_LotID = "";
        private string T_SubLotID = "";
        private string T_OperatorID = "";
        private string T_HandlerID = "";
        private string T_ContactorID = "";
        private string T_DeviceID = "";
        private string T_LoadBoardID = "";
        private string T_MfgLotID = "";
        private string T_RevID = "";
        private bool bIsEngineeringSample = false;
        public string Date;
        public string Shift;
        public int PassCharCount = 0;
        public bool EnablePassword = false;

        private string webQueryValidation = "";
        private string webServerURL = "";
        private eUSERTYPE Clotho_User = eUSERTYPE.DEBUG;

        public string web_lotid = "PTXXXXXXXXXX";
        public string web_targetdevice = "AFEM-XXXX-AP1";
        public string web_MFGlot = "999999";
        public string web_handlerid = "NULL";

        private double BarcodeEntrySpeed = 0.13;
        private double DataEntryTotalTime = 0;
        private HiPerfTimer HpTimer = new HiPerfTimer();

        public string Set_Title
        {
            set
            {
                this.Text = value;
            }
        }

        public string productTag
        {
            get
            {
                return T_productTag;
            }
            set
            {
                T_productTag = value;
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

        //public string MfgLotID
        //{
        //    get
        //    {
        //        return T_MfgLotID;
        //    }
        //    set
        //    {
        //        T_MfgLotID = value;
        //    }
        //}

        public string MfgLotID
        {
            get
            {
                return txtMfgLotID.Text;
            }
            set
            {
                T_MfgLotID = txtMfgLotID.Text;
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

        private bool allowKeyboardException = false;

        public ProductionTestInputForm()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();
        }

        public ProductionTestInputForm(bool IsEngineeringGUI) : this()
        {
            bIsEngineeringSample = IsEngineeringGUI;

            if (!bIsEngineeringSample)
            {
                this.Text = "Production Test Information Input";
                lblMfgID.Text = "Mfg Lot ID";
                txtMfgLotID.Enabled = true;
            }
            else
            {
                this.Text = "Engineering Sample Test Information Input";
                lblMfgID.Text = "Mfg Lot ID"; //"Rev ID";
            }

            /// condition1: from TCF, GU_EngineeringMode == TRUE
            /// condition2:
            allowKeyboardException = ClothoDataObject.Instance.EngineeringModewoProduction ||
                ((ClothoDataObject.Instance.USERTYPE & (eUSERTYPE.DEBUG | eUSERTYPE.SUSER)) > 0);

            this.txtMfgLotID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMfgLotID_KeyDown);
            this.txtMfgLotID.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtMfgLotID_KeyUp);
        }

        public ProductionTestInputForm(bool IsEngineeringGUI, string webQueryValidation, string webServerURL, eUSERTYPE Clotho_User) : this(IsEngineeringGUI)
        {
            this.webQueryValidation = webQueryValidation;
            this.webServerURL = webServerURL;
            this.Clotho_User = Clotho_User;
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
            //Lib_Var.AdminLevel = false;

            txtOperatorID.Text = T_OperatorID;
            txtLotID.Text = T_LotID;
            txtSubLotID.Text = T_SubLotID;
            txtHandlerID.Text = T_HandlerID;
            txtContactorID.Text = T_ContactorID;
            txtMfgLotID.Text = T_MfgLotID.CIvStartsWith("MFG") ? T_MfgLotID.ToUpper().Replace("MFG", "") : T_MfgLotID;

            txtLbID.Text = T_LoadBoardID;
            txtDeviceID.Text = T_DeviceID;
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
                //txtMfgLotID.Focus();  // Jedi burns MFG LOT ID at assembly
                txtDeviceID.Focus();
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
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (e.KeyChar.ToString() != "-") && (e.KeyChar.ToString() != "_"))
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
            else if (!Char.IsDigit(e.KeyChar) && !Char.IsLetter(e.KeyChar) && (e.KeyChar.ToString() != "-"))
            {
                e.KeyChar = Convert.ToChar(0);
            }
        }

        //6 - Handler ID
        private void txtHandlerID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                //txtContactorID.Focus();
                button1.Focus();
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
                button1.Focus();
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

        #endregion KeyPressEvent - set focus

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
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter)
                txtMfgLotID.Focus();
        }

        private void txtLotID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                e.Handled = true;

            if (e.KeyCode == Keys.Tab)
                //txtMfgLotID.Focus();
                txtDeviceID.Focus();
        }

        private void txtMfgLotID_KeyDown_For_Engineering(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                txtDeviceID.Focus();
            else if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrWhiteSpace(txtOperatorID.Text))
                    pictureBox1_Click(null, null);
                else
                    txtOperatorID.Focus();
            }
        }

        //Say Aun (2021-04-28 - Block ProductionUser from using Keyboard)
        private void txtMfgLotID_KeyDown(object sender, KeyEventArgs e)
        {
            HpTimer.Start();
            if ((e.KeyCode == Keys.Back) || (e.KeyCode == Keys.Delete))
            {
                DataEntryTotalTime = 0;
                txtMfgLotID.Text = "";
                txtMfgLotID.Focus();
                HpTimer.Stop();
            }

            if (e.KeyCode == Keys.Enter)
            {
                HpTimer.Stop();
                DataEntryTotalTime = DataEntryTotalTime + HpTimer.Duration;

                if (allowKeyboardException)
                {
                    if (!string.IsNullOrWhiteSpace(txtOperatorID.Text))
                        pictureBox1_Click(null, null);
                    else
                        txtOperatorID.Focus();
                }

                txtDeviceID.Focus();
            }

            if (e.KeyCode == Keys.Tab)
                txtDeviceID.Focus();
        }

        private void txtMfgLotID_KeyUp(object sender, KeyEventArgs e)
        {
            HpTimer.Stop();
            DataEntryTotalTime = DataEntryTotalTime + HpTimer.Duration;
            if ((e.KeyCode == Keys.Back) || (e.KeyCode == Keys.Delete))
                DataEntryTotalTime = 0;

            if (!allowKeyboardException)  // PRODUCTIONUSER
            {
                if (DataEntryTotalTime > BarcodeEntrySpeed)
                {
                    DataEntryTotalTime = 0;
                    txtMfgLotID.Text = "";

                    string path = @"C:\Avago.ATF.Common\MFG_ID";
                    string text = txtOperatorID.Text + " " + txtLotID.Text + " " + txtHandlerID.Text + " " + System.DateTime.Now.ToString() + "\r\n";
                    File.AppendAllText(path, text);
                    txtOperatorID.Focus();

                    MessageBox.Show("YOUR ID ALREADY SUBMITTED TO HR, DUE TO VIOLATION OF USING KEYBOARD TO INPUT MFG ID!");
                }
            }
            lbBarcodeSpeed.Text = "BarcodeScannerSpeed: " + DataEntryTotalTime.ToString();
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
                button1.Focus();
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
            bool passflag_MfgID = false;
            bool passflag_RevID = false;
            bool passflag_DeviceID = false; //Jupiter only (can be used as Assembly ID for other products)
            List<KeyValuePair<bool, string>> passFails = new List<KeyValuePair<bool, string>>();
            string ENGR_Password = "AVGO" + lblPAver.Text.Split('.')[1].Trim().ToUpper();

            bool Webservice_Flag = false;

            //Admin mode
            if (EnablePassword == true && txtOperatorID.Text.CIvEqualsAnyOf("AVGO600", ENGR_Password))
            {
                txtOperatorID.Text = "A0001";
                txtLotID.Text = "PT0000000001";
                if (productTag.CIvContains("-QA"))
                {
                    txtSubLotID.Text = "1AQA";
                }
                else
                {
                    txtSubLotID.Text = "1A";
                }

                if (string.IsNullOrWhiteSpace(txtContactorID.Text) || txtContactorID.Text.CIvContains("NAN"))
                    txtContactorID.Text = "NaN";

                if (string.IsNullOrWhiteSpace(txtMfgLotID.Text))
                    txtMfgLotID.Text = ClothoDataObject.Instance.EnableOnlySeoulUser ? "000000" : "000001";
                T_MfgLotID = txtMfgLotID.Text;

                txtDeviceID.Text = "AFEM-8230-MS";
                if (string.IsNullOrWhiteSpace(txtLbID.Text) || txtLbID.Text.CIvContains("NAN"))
                    txtLbID.Text = "LB-0000-000";
                bIsEngineeringSample = true;
                Application.DoEvents();
            }

            if (webQueryValidation == "TRUE" && ClothoDataObject.Instance.ContractManufacturer != "ASEK")
            {
                if ((txtLotID.Text.StartsWith("PT") || txtLotID.Text.StartsWith("FT")) && !txtLotID.Text.Contains("-E"))
                {
                    if ((Clotho_User & (eUSERTYPE.PPUSER | eUSERTYPE.SUSER)) > 0)
                    {
                        Webservice_Flag = false;
                    }
                    else
                    {
                        Webservice_Flag = true;
                        bool querySuccess = false;

                        if (webServerURL != "")
                        {
                            querySuccess = WebServiceQuery.DisplayInariWebListNames(txtLotID.Text, webServerURL);
                        }
                        else
                        {
                            querySuccess = WebServiceQuery.DisplayInariWebListNames(txtLotID.Text);
                        }

                        web_lotid = WebServiceQuery.LotInfoArray[1].Trim().ToUpper();
                        web_targetdevice = WebServiceQuery.LotInfoArray[3].Trim().ToUpper();
                        web_MFGlot = WebServiceQuery.LotInfoArray[5].Trim().ToUpper();
                        web_handlerid = WebServiceQuery.LotInfoArray[7].Trim().ToUpper();

                        if (querySuccess == false)
                        {
                            WebQueryBox msg = new WebQueryBox();
                            msg.DialogResult = DialogResult.Retry;

                            while (msg.DialogResult == DialogResult.Retry)
                            {
                                msg.ShowDialog();
                            }
                            if (msg.DialogResult == DialogResult.OK) { Webservice_Flag = false; }
                        }
                    }
                }
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
          // rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^INT\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWI\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWN\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWM\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWP\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^ISK\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^FWR\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^T\d{4}"));
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^AM\d{1,8}")); //Amkor
            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^[0-9]{1,6}")); //ASEKr

            rxOpID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtOperatorID.Text, @"^IN[A-Z]\d{4}"));

            foreach (bool rxOP in rxOpID)
            {
                if (rxOP)
                {
                    T_OperatorID = txtOperatorID.Text;
                    passflag_OPID = true;
                    break;
                }
            }

            passFails.Add(new KeyValuePair<bool, string>(passflag_OPID, $"Operator ID ({txtOperatorID.Text})"));
            //if (!passflag_OPID)
            //MessageBox.Show("No matching for Operator ID " + "(" + txtOperatorID.Text + ")" + ", please re-enter!");

            #endregion OperatorID checking

            #region LotID checking

            List<bool> rxLotID = new List<bool>();

            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^STDUNIT\d{2}-\d{6}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^STDUNIT\d{3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^BINCHECK-\d{1,10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^ENGR-\d{1,10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^BINCHECK_\d{6}_\w{1}"));   //BINCHECK_050716_M
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^MERGE_\d{6}_\w{1}"));   //MERGE_050716_M
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^GUCAL"));

            //PA
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT123456789$"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}-\w{1}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^PT\d{10}-\w{2}"));

            //ASEKr
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\w{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{8}-\w{1,5}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{3}\w{2}\d{3}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^W\d{1,4}\w{1,3}\d{1,4}"));

            //EBR
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B\d{3}\w{2}\d{3}\w{2}$"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B\d{3}\w{2}\d{3}\w{1}$"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B\d{3}\w{2}\d{3}$"));
            //ChoonChin (20191210) - For ASEKr Proto2
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B\d{3}\w{2}\d{2}\w{2}$"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^B\d{1,3}\w{1,3}\d{1,3}\w{1,3}$"));

            //Amkor
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^M\d{8}"));
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^M\d{8}-\w{1,5}"));

            //Amkor VW
            rxLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLotID.Text, @"^M\d{1,4}\w{1,3}\d{1,4}"));

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

            passFails.Add(new KeyValuePair<bool, string>(passflag_LotID, $"Lot ID ({txtLotID.Text})"));
            //if (!passflag_LotID)
            //    MessageBox.Show("No matching for Lot ID " + "(" + txtLotID.Text + ")" + ", please re-enter!");

            if (Webservice_Flag == true)
            {
                if (web_lotid != txtLotID.Text)
                {
                    passflag_LotID = false;
                    MessageBox.Show("No matching for Lot ID (" + txtLotID.Text + ") from Inari Web Service" + ", please re-enter!");
                }
                else if (!passflag_LotID)
                {
                    MessageBox.Show("No matching for Lot ID " + "(" + txtLotID.Text + ")" + ", please re-enter!");
                }
            }
            else
            {
                if (!passflag_LotID)
                {
                    MessageBox.Show("No matching for Lot ID " + "(" + txtLotID.Text + ")" + ", please re-enter!");
                }
            }

            #endregion LotID checking

            #region MfgID checking

            if (!bIsEngineeringSample)
            {
                #region MFG ID check

                passflag_RevID = true;

                int iMfgID = 0;
                List<bool> rxMfgID = new List<bool>();

                rxMfgID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtMfgLotID.Text.ToUpper(), @"^[0-9]{6}"));

                //foreach (bool rxMfg in rxMfgID)
                //{
                //    if (rxMfg)
                //    {
                //        T_MfgLotID = txtMfgLotID.Text;
                //        passflag_MfgID = true;
                //        break;
                //    }
                //}

                try
                {
                    iMfgID = Convert.ToInt32(txtMfgLotID.Text);
                }
                catch
                {
                    passflag_MfgID = false;
                }

                //if (iMfgID > 131071)
                //{
                //    passflag_MfgID = false;
                //    MessageBox.Show("Mfg Lot Number cannot bigger than 131071 <= " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");
                //}

                //if (txtMfgLotID.Text == null)
                //    MessageBox.Show("No matching for MfgLot ID " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");

                passflag_MfgID = true;

                if (Webservice_Flag == true)
                {
                    if (web_MFGlot != txtMfgLotID.Text)
                    {
                        passflag_MfgID = false;
                        if (passflag_LotID == true)
                        {
                            MessageBox.Show("No matching for MfgLot ID " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");
                        }
                    }
                    else
                    {
                        passflag_MfgID = true;
                    }
                }
                else
                {
                    if (passflag_MfgID == false)
                    {
                        MessageBox.Show("No matching for MfgLot ID " + "(" + txtMfgLotID.Text + ")" + ", please re-enter!");
                    }
                }

                #endregion MFG ID check
            }

            #endregion MfgID checking

            #region RevID Check

            else
            {
                List<bool> rxRevID = new List<bool>();
                //rxRevID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtMfgLotID.Text.ToUpper(), @"^BOM_[A-Z]{1}[0-9]{1}[A-Z]{1}"));
                rxRevID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtMfgLotID.Text.ToUpper(), @"^[0-9]{6}"));

                foreach (bool rxRev in rxRevID)
                {
                    if (rxRev)
                    {
                        passflag_MfgID = true;
                        passflag_RevID = true;
                        T_MfgLotID = txtMfgLotID.Text;

                        break;
                    }
                }

                passFails.Add(new KeyValuePair<bool, string>(passflag_MfgID, $"Mfg ID ({txtMfgLotID.Text})"));
                //if (!passflag_MfgID)
                //{
                //    passflag_RevID = false;
                //    MessageBox.Show("Mfg ID does not match the required format, please re-enter!");
                //}
            }

            #endregion RevID Check

            #region Device ID checking

            List<bool> rxDeviceID = new List<bool>();
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-AS$")); //Only allow AFEM-8200-AS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-AL$")); //Only allow AFEM-8200-AL
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-MS$")); //Only allow AFEM-8200-MS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-ML$")); //Only allow AFEM-8200-ML
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-AT$")); //Only allow AFEM-8200-MS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-MT$")); //Only allow AFEM-8200-ML

            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-AL$")); //Only allow ENGR-8200-AL
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-MS$")); //Only allow ENGR-8200-MS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-ML$")); //Only allow ENGR-8200-ML
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^AFEM-\d{4}-MB$")); //Only allow AFEM-8100-MB
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-AL$")); //Only allow ENGR-8100-AL
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-ML$")); //Only allow ENGR-8100-ML
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-AS$")); //Only allow ENGR-8100-AS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-MS$")); //Only allow ENGR-8100-MS

            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-AT$")); //Only allow ENGR-8230-AS
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-\d{4}-MT$")); //Only allow ENGR-8230-MS


            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-8234-AF$")); //Only allow ENGR-8234-AF
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-8234-MF$")); //Only allow ENGR-8234-MF
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-8234-MH$")); //Only allow ENGR-8234-MH
            rxDeviceID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtDeviceID.Text, @"^ENGR-8234-AH$")); //Only allow ENGR-8234-AH

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

            passFails.Add(new KeyValuePair<bool, string>(passflag_DeviceID, $"Device ID ({txtDeviceID.Text})"));
            if (passflag_DeviceID)
                T_DeviceID = txtDeviceID.Text;
            //else
            //    MessageBox.Show("No matching for Device ID " + "(" + txtDeviceID.Text + ")" + ", please re-enter!");

            #endregion Device ID checking

            #region Sublot ID checking

            List<bool> rxSubLotID = new List<bool>();

            if ((!productTag.ToUpper().Contains("-REQ")) && (!productTag.ToUpper().Contains("EVAL")))
            {
                if (productTag.ToUpper().Contains("-QA"))
                {
                    // for QA
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]QA$")); //1AQA
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]QE$")); //1AQE
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]COQ$")); //1ACOQ
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]COQE$")); //1ACOQE

                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]CCOQ$")); //1ACCOQ
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]CCOQE$")); //1ACCOQE

                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]ECCOQ$")); //1AECCOQ
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]ECCOQE$")); //1AECCOQE

                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]COQSOAK$")); //1ACOQSOAK
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]CCOQSOAK$")); //1ACCOQSOAK
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]ECCOQSOAK$")); //1AECCOQSOAK

                    //ORT QA requirement
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]UHP$")); //1AUHP
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]TCP$")); //1ATCP
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]UH$")); //1AUH
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-E]TC$")); //1ATC
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-D]ATC[1-9]$")); //1A Post TMCL200x
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-5]\w{0}[A-D]HS$")); //1A Post HTSL
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]ECCOQ-\d{3}$")); //1AECCOQ-090(XXX-numeric number for time)
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]PS-\w{0}[A-Z][A-Z][A-Z]$")); //Power Soak program 1APS-ECC,CC
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]PS-\w{0}[A-Z][A-Z]$")); //Power Soak program 1APS-CC
                }
                else
                {
                    // for Production
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^[1-3]\w{0}[A-E]$"));

                    // Misc
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^RE"));
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^LYT"));
                    rxSubLotID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtSubLotID.Text, @"^PE"));
                }

                foreach (bool rxSlotID in rxSubLotID)
                {
                    if (rxSlotID)
                    {
                        T_SubLotID = txtSubLotID.Text;
                        passflag_SubLotID = true;
                        break;
                    }
                }
            }
            else
            {
                T_SubLotID = txtSubLotID.Text;
                passflag_SubLotID = true;
            }

            passFails.Add(new KeyValuePair<bool, string>(passflag_SubLotID, $"Sub lot ID ({txtSubLotID.Text})"));
            //if (!passflag_SubLotID)
            //{
            //    MessageBox.Show("Invalid Sub lot ID " + "(" + txtSubLotID.Text + ")" + ", please re-enter!");
            //}

            #endregion Sublot ID checking

            #region HandlerID checking

            List<bool> rxHandlerID = new List<bool>();
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^EIS\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^SRM\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^S9\d{4}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^HT\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^IM\d{3}", RegexOptions.IgnoreCase));
            rxHandlerID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtHandlerID.Text, @"^NS\d{4}", RegexOptions.IgnoreCase));

            //foreach (bool rxHandler in rxHandlerID)
            //{
            //    if (rxHandler)
            //    {
            //        T_HandlerID = txtHandlerID.Text;
            passflag_HandlerID = true;
            //        break;
            //    }
            //}

            passFails.Add(new KeyValuePair<bool, string>(passflag_HandlerID, $"Handler SN ({txtHandlerID.Text})"));
            //if (!passflag_HandlerID)
            //    MessageBox.Show("No matching for Handler SN " + "(" + txtHandlerID.Text + ")" + ", please re-enter!");

            #endregion HandlerID checking

            passflag_ContID = SetContactor();
            //passflag_ContID = true;

            passFails.Add(new KeyValuePair<bool, string>(passflag_ContID, $"Contactor ID ({txtContactorID.Text})"));
            //if (!passflag_ContID)
            //    MessageBox.Show("No matching for Contactor ID " + "(" + txtContactorID.Text + ")" + ", please re-enter!");

            passflag_LBID = SetLoadBoardId();
            //passflag_LBID = true;

            passFails.Add(new KeyValuePair<bool, string>(passflag_LBID, $"Load Board ID ({txtLbID.Text})"));
            //if (!passflag_LBID)
            //    MessageBox.Show("No matching for Load Board ID " + "(" + txtLbID.Text + ")" + ", please re-enter!");

            if (passflag_ContID && passflag_LBID && passflag_OPID && passflag_HandlerID && passflag_LotID && passflag_SubLotID && passflag_DeviceID && passflag_MfgID && passflag_RevID)
            {
                this.DialogResult = DialogResult.OK;
                this.FormClosing -= new System.Windows.Forms.FormClosingEventHandler(this.FrmDataInput_FormClosing);
                this.Close();
            }
            else if (passFails.Any(t => t.Key == false))
            {
                MessageBox.Show(string.Format("Check for items that don't match the rule:\n\n{0}", string.Join("\n", passFails.Where(t => t.Key == false).Select(v => v.Value))));
            }

            if (Webservice_Flag == true)
            {
                if (web_handlerid != txtHandlerID.Text)
                {
                    passflag_HandlerID = false;
                    if (passflag_LotID == true)
                    {
                        MessageBox.Show("No matching for Handler SN " + "(" + txtHandlerID.Text + ")" + ", please re-enter!");
                    }
                }
                else
                {
                    passflag_HandlerID = true;
                }
            }
            else
            {
                if (passflag_HandlerID == false)
                    MessageBox.Show("No matching for Handler SN " + "(" + txtHandlerID.Text + ")" + ", please re-enter!");
            }
        }

        private void FrmDataInput_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private bool SetContactor()
        {
            List<bool> rxContactorID = new List<bool>();
            //rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^JK\d{3}", RegexOptions.IgnoreCase)); // Joker
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^LN-\d{4}-\w{3}-\w{3}", RegexOptions.IgnoreCase));
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"NaN", RegexOptions.IgnoreCase));
            rxContactorID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtContactorID.Text, @"^NA$", RegexOptions.IgnoreCase));

            foreach (bool rxContactor in rxContactorID)
            {
                if (rxContactor)
                {
                    T_ContactorID = txtContactorID.Text;
                    return true;
                }
            }

            return false;
        }

        private bool SetLoadBoardId()
        {
            List<bool> rxLboardID = new List<bool>();
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LB-\d{4}-\d{3}", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^JP\d{4}", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LBSP", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LB-RF1", RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"NaN" , RegexOptions.IgnoreCase));
            rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^NA$", RegexOptions.IgnoreCase));
            //rxLboardID.Add(System.Text.RegularExpressions.Regex.IsMatch(txtLbID.Text, @"^LB\d{4}_LB\d{4}", RegexOptions.IgnoreCase));

            foreach (bool rxLbID in rxLboardID)
            {
                if (rxLbID)
                {
                    T_LoadBoardID = txtLbID.Text;
                    return true;
                }
            }

            return false;
        }
    }
}