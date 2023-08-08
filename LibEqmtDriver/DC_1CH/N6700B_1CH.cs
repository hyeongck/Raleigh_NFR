using System;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.DC_1CH
{
    public class N6700B : Base_DC_1CH,iDCSupply_1CH
    {
        public static string ClassName = "N6700B 4-Channel PowerSupply Class";
        private FormattedIO488 myVisaEq = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "N6700B"; }

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
                    myVisaEq.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 20000, OptionString);
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
        public N6700B(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~N6700B() { }

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
                myVisaEq.IO.WriteString("OUTP ON, (@" + Channel.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B: DcOn -> " + ex.Message);
            }
        }
        public override void DcOff(int Channel)
        {
            try
            {
                myVisaEq.IO.WriteString("OUTP OFF, (@" + Channel.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B: DcOff -> " + ex.Message);
            }
        }

        public override void SetVolt(int Channel, double Volt, double iLimit)
        {
            VSourceSet(Channel, Volt);
            ISourceSet(Channel, iLimit);
        }

        public override float MeasI(int Channel)
        {
            try
            {
                return WRITE_READ_SINGLE("MEAS:CURR? (@" + Channel.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B:: MeasI -> " + ex.Message);
            }
        }

        public override float MeasV(int Channel)
        {
            try
            {
                return WRITE_READ_SINGLE("MEAS:VOLT? (@" + Channel.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B:: MeasV -> " + ex.Message);
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
                throw new Exception("N6700B: Initialize -> " + ex.Message);
            }
        }

        private void VSourceSet(int val, double dblVoltage)
        {
            try
            {
                myVisaEq.IO.WriteString("VOLT " + dblVoltage.ToString() + ",(@" + val.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B: VSourceSet -> " + ex.Message);
            }
        }
        private void ISourceSet(int val, double dblAmps)
        {
            try
            {
                myVisaEq.IO.WriteString("CURR " + dblAmps.ToString() + ",(@" + val.ToString() + ")");
            }
            catch (Exception ex)
            {
                throw new Exception("N6700B: ISourceSet -> " + ex.Message);
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
