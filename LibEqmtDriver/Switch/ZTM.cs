using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModularZT64;

namespace LibEqmtDriver.SCU
{
    public class ZTM : Base_Switch, iSwitch
    {
        public ModularZT64.USB_ZT MyZT = new USB_ZT();
        public string SN = "";
        public string retStr = "";
        public string Command;
        private int i;
        public bool bSwitchState = false;

        //Constructor
        public ZTM(string SerialNumber = null)
        {
            SN = SerialNumber;

            Initialize();
        }
        ZTM() { }

        #region iSwitch Members
        public override void Initialize()
        {
            try
            {
                string availableSN = "";

                i = MyZT.Get_Available_SN_List(ref availableSN);
                var lstAvailableSNs = availableSN.Split(' ').Where(t => t.Trim().Length > 0).Select(s => s.Trim()).ToList();

                if (i == 1 && lstAvailableSNs.Count() > 0)
                {
                    if (!lstAvailableSNs.Any(s => s.Equals(SN)))
                        SN = lstAvailableSNs.First();

                    i = MyZT.Connect(ref (SN));

                    if (i == 1)
                        bSwitchState = true;
                    else
                        bSwitchState = false;
                }

                //Command = "SP6T:1:STATE?";
                //i = MyZT.Send_SCPI(ref(Command), ref(retStr));
            }
            catch (Exception ex)
            {
                MessageBox.Show("ZTM : Fail Initialize -> " + ex.Message);
            }
        }

        public override void SetPath(object state)
        {
            string val = (string)state;
            SetPath(val);
        }

        public override void SetPath(string val)
        {
            string tmp = val.ToUpper();

            try
            {
                if (!tmp.Contains("NONE") && bSwitchState)
                {
                    Command = val;
                    i = MyZT.Send_SCPI(ref (Command), ref (retStr));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ZTM : Fail SetPath -> " + ex.Message);
            }
        }

        public override void Reset()
        {
            try
            {

            }
            catch (Exception ex)
            {
                throw new Exception("ZTM : Fail Reset -> " + ex.Message);
            }
        }

        public override int SPDT1CountValue()
        {
            return 0;
        }

        public override int SPDT2CountValue()
        {
            return 0;
        }

        public override int SPDT3CountValue()
        {
            return 0;
        }

        public override int SPDT4CountValue()
        {
            return 0;
        }

        public override int SP6T1_1CountValue()
        {
            return 0;
        }

        public override int SP6T1_2CountValue()
        {
            return 0;
        }

        public override int SP6T1_3CountValue()
        {
            return 0;
        }

        public override int SP6T1_4CountValue()
        {
            return 0;
        }

        public override int SP6T1_5CountValue()
        {
            return 0;
        }

        public override int SP6T1_6CountValue()
        {
            return 0;
        }

        public override int SP6T2_1CountValue()
        {
            return 0;
        }

        public override int SP6T2_2CountValue()
        {
            return 0;
        }

        public override int SP6T2_3CountValue()
        {
            return 0;
        }

        public override int SP6T2_4CountValue()
        {
            return 0;
        }

        public override int SP6T2_5CountValue()
        {
            return 0;
        }

        public override int SP6T2_6CountValue()
        {
            return 0;
        }

        public override void SaveRemoteMechSwStatusFile()
        {

        }

        public override void SaveLocalMechSwStatusFile()
        {

        }

        public override string GetInstrumentInfo()
        {
            return "SWMatrix = " + "*" + GetSerialNumber() + "; ";
        }


        #endregion iSwitch Members

        public void UpdateRemoteXmlFile()
        {

        }
        
        public string GetSerialNumber()
        {
            string ReadSN = "";

            try
            {
                if (bSwitchState)
                {
                    i = MyZT.Read_SN(ref (ReadSN));
                    return ReadSN;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ZTM : Fail GetSerialNumber -> " + ex.Message);
            }
            return ReadSN;
        }
    }
}
