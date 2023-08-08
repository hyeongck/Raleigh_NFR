using System;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.SCU
{
    public class Agilent3499: Base_Switch, iSwitch 
    {
        public static string ClassName = "3449A Switch Control Unit Class";
        private FormattedIO488 myVisaEq = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "Agilent3499"; }

        /// <summary>
        /// Parsing Equpment Address
        /// </summary>
        public string Address
        {
            get
            {
                return IOAddress;
            }
            set
            {
                IOAddress = value;
            }
        }

        public FormattedIO488 parseIO
        {
            get
            {
                return myVisaEq;
            }
            set
            {
                myVisaEq = parseIO;
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    myVisaEq.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, OptionString);
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Class Name: " + ClassName + "\nParameters: OpenIO" + "\n\nErrorDesciption: \n"
                        + ex, "Error found in Class " + ClassName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    myVisaEq.IO = null;
                    return;
                }
            }
        }

        //Constructor
        public Agilent3499(string ioAddress) 
        {
            Address = ioAddress;
            OpenIO();
        }
        Agilent3499() { }

        #region iSwitch Members

        public override void Close()
        {
            if (myVisaEq.IO != null)
            {
                myVisaEq.IO.Close();
            }
        }

        public override void Initialize()
        {
            try
            {
                myVisaEq.WriteString("*CLS; *RST", true);
            }
            catch (Exception ex)
            {
                throw new Exception("Agilent3499: Initialize -> " + ex.Message);
            }
        }

        public override void SetPath(object state)
        {
            string val = (string)state;
            SetPath(val);
        }

        public override void SetPath(string val)
        {
            string[] tempdata;
            tempdata = val.Split(';');

            try
            {
                for (int i = 0; i < tempdata.Length; i++)
                {
                    myVisaEq.WriteString(tempdata[i], true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Agilent3499: SetPath -> " + ex.Message);
            }
        }

        public override void Reset()
        {
             try
             {
                 myVisaEq.WriteString("*CLS; *RST", true);
             }
             catch (Exception ex)
             {
                 throw new Exception("Agilent3499: Reset -> " + ex.Message);
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

        public override void SaveRemoteMechSwStatusFile() { }

        public override void SaveLocalMechSwStatusFile() { }

        public override string GetInstrumentInfo()
        {
            return "";
        }


        #endregion iSwitch Members

        private void WRITE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
        }
        private void SW_control(string _StatusSW,string _SwitchSlot)
        {
            myVisaEq.WriteString(_StatusSW +" (@"+_SwitchSlot+")", true);
        }
        private double WRITE_READ_DOUBLE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return Convert.ToDouble(myVisaEq.ReadString());
        }
        private string WRITE_READ_STRING(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return myVisaEq.ReadString();
        }
        private float WRITE_READ_SINGLE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return Convert.ToSingle(myVisaEq.ReadString());
        }



      
    }
}
