using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.TuneableFilter
{
    public class cKnL_D5BT : Base_TuneFilter, iTuneFilterDriver
    {
        public static string ClassName = "KnL D5BT Tuneable Filter Class";
        private FormattedIO488 myVisaEq = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "KnL_D5BT"; }

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
        public cKnL_D5BT(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        cKnL_D5BT() { }

        #region iTuneFileterDriver Members
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
                myVisaEq.WriteString("SYST:PRES DEF", true);
            }
            catch (Exception ex)
            {
                throw new Exception("KnL D5BT: Initialize -> " + ex.Message);
            }
        }
        public override double ReadFreqMHz()
        {
            string result;
            double freqMHz;
            result = WRITE_READ_STRING("F?");
            result = result.TrimStart('?');               //Remove '?' from data of the 1st character
            freqMHz = (Convert.ToDouble(result))/1e3;     //Convert from KHz to MHz
            return freqMHz;
        }
        public override void SetFreqMHz(double freqMHz)
        {
            double freqKHz = freqMHz * 1e3;
            myVisaEq.WriteString("F" + freqKHz, true);
        }
        public override void Reset()
        {
            myVisaEq.WriteString("RST");
        }
        #endregion iTuneFilterDriver Members

        #region generic READ and WRITE function
        public float WRITE_READ_SINGLE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return Convert.ToSingle(myVisaEq.ReadString());
        }
        public float[] READ_IEEEBlock(IEEEBinaryType _type)
        {
            return (float[])myVisaEq.ReadIEEEBlock(_type, true, true);
        }
        public float[] WRITE_READ_IEEEBlock(string _cmd, IEEEBinaryType _type)
        {
            myVisaEq.WriteString(_cmd, true);
            return (float[])myVisaEq.ReadIEEEBlock(_type, true, true);
        }
        public void WRITE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
        }
        public double WRITE_READ_DOUBLE(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return Convert.ToDouble(myVisaEq.ReadString());
        }
        public string WRITE_READ_STRING(string _cmd)
        {
            myVisaEq.WriteString(_cmd, true);
            return myVisaEq.ReadString();
        }
        public void WriteInt16Array(string command, Int16[] data)
        {
            myVisaEq.WriteIEEEBlock(command, data, true);
        }

        public void WriteByteArray(string command, byte[] data)
        {
            myVisaEq.WriteIEEEBlock(command, data, true);
        }
        #endregion
    }
}
