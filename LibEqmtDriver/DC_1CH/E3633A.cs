using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.DC_1CH
{
    public class E3633A : Base_DC_1CH,iDCSupply_1CH
    {
        public static string ClassName = "E3633A/E3644A 1-Channel PowerSupply Class";
        private FormattedIO488 myVisaEq = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "E3633A"; }

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
        public E3633A(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~E3633A() { }

        #region iDCSupply_1CH Members

        public override void Init()
        {
            RESET();
        }

        public override void Close()
        {
            if (myVisaEq.IO != null)
            {
                myVisaEq.IO.Close();
            }   
        }

        public override void DcOn(int Channel)
        {
            try
            {
                myVisaEq.IO.WriteString("OUTP ON");
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A: DcOn -> " + ex.Message);
            }
        }
        public override void DcOff(int Channel)
        {
            try
            {
                myVisaEq.IO.WriteString("OUTP OFF");
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A: DcOff -> " + ex.Message);
            }
        }

        public override void SetVolt(int Channel, double Volt, double iLimit)
        {
            ISourceSet(Channel, iLimit);
            VSourceSet(Channel, Volt);
        }

        public override float MeasI(int Channel)
        {
            try
            {
                return WRITE_READ_SINGLE("MEAS:CURR?");
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A:: MeasI -> " + ex.Message);
            }
        }

        public override float MeasV(int Channel)
        {
            try
            {
                return WRITE_READ_SINGLE("MEAS:VOLT?");
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A:: MeasV -> " + ex.Message);
            }
        }

        #endregion

        private void RESET()
        {
            try
            {
                myVisaEq.WriteString("*CLS; *RST", true);
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A: Initialize -> " + ex.Message);
            }
        }

        private void VSourceSet(int val, double dblVoltage)
        {
            try
            {
                myVisaEq.IO.WriteString("VOLT " + dblVoltage.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A: VSourceSet -> " + ex.Message);
            }
        }
        private void ISourceSet(int val, double dblAmps)
        {
            try
            {
                myVisaEq.IO.WriteString("CURR " + dblAmps.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("E3633A: ISourceSet -> " + ex.Message);
            }
        }

        #region generic READ function

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

        #endregion

    }
}
