using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.PS
{
    public class E4417A : Base_PowerSensor, iPowerSensor
    {
        public static string ClassName = "E4417A PowerMeter Class";
        private FormattedIO488 myVisaPM = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "E4417A"; }

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
                return myVisaPM;
            }
            set
            {
                myVisaPM = parseIO;
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    myVisaPM.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, OptionString);
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Class Name: " + ClassName + "\nParameters: OpenIO" + "\n\nErrorDesciption: \n"
                        + ex, "Error found in Class " + ClassName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    myVisaPM.IO = null;
                    return;
                }
            }
        }
        //Constructor
        public E4417A(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        E4417A() { }

        #region iPowerSensor Members
        public override void Close()
        {
            if (myVisaPM.IO != null)
            {
                myVisaPM.IO.Close();
            }
        }

        public override void Initialize(int chNo)
        {
            try
            {
                myVisaPM.WriteString("SYST:PRES DEF", true);
                bool dummy = OPERATION_COMPLETE();
            }
            catch (Exception ex)
            {
                throw new Exception("E4417A: Initialize -> " + ex.Message);
            }
        }

        public override void SetFreq(int chNo, double freqMHz, int measuretype)
        {
            try
            {
                double freqHz = freqMHz * 1e6;
                myVisaPM.WriteString("SENS" + chNo + ":FREQ " + freqHz, true);
            }
            catch (Exception ex)
            {
                throw new Exception("E4417A: SetFreq -> " + ex.Message);
            }
        }

        public override void SetOffset(int chNo, double val)
        {
            try
            {
                myVisaPM.WriteString("CALC" + chNo + ":GAIN " + val, true);
            }
            catch (Exception ex)
            {
                throw new Exception("E4417A: SetPath -> " + ex.Message);
            }
        }

        public override void EnableOffset(int chNo, bool state)
        {
            try
            {
                switch (state)
                {
                    case true:
                        myVisaPM.WriteString("CALC" + chNo + ":GAIN:STAT ON", true);
                        break;
                    case false:
                        myVisaPM.WriteString("CALC" + chNo + ":GAIN:STAT OFF", true);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("E4417A: SetPath -> " + ex.Message);
            }
        }

        public override float MeasPwr(int chNo)
        {
            try
            {
                return WRITE_READ_SINGLE("FETC" + chNo + "?");
            }
            catch (Exception ex)
            {
                throw new Exception("E4471A:: MeasPwr -> " + ex.Message);
            }
        }

        public override void Reset()
        {
             try
             {
                 myVisaPM.WriteString("*CLS; *RST", true);
             }
             catch (Exception ex)
             {
                 throw new Exception("E4417A: Reset -> " + ex.Message);
             }
        }

        #endregion iPowerSensor Members

        public bool OPERATION_COMPLETE()
        {
            try
            {
                bool _complete = false;
                double _dummy = -99;
                do
                {
                    _dummy = WRITE_READ_DOUBLE("*OPC?");
                } while (_dummy == 0);
                _complete = true;
                return _complete;

            }
            catch (Exception ex)
            {
                throw new Exception("E4417A: OPERATION_COMPLETE -> " + ex.Message);
            }
        }

        #region generic READ function

        public double WRITE_READ_DOUBLE(string _cmd)
        {
            myVisaPM.WriteString(_cmd, true);
            return Convert.ToDouble(myVisaPM.ReadString());
        }
        public string WRITE_READ_STRING(string _cmd)
        {
            myVisaPM.WriteString(_cmd, true);
            return myVisaPM.ReadString();
        }
        public float WRITE_READ_SINGLE(string _cmd)
        {
            myVisaPM.WriteString(_cmd, true);
            return Convert.ToSingle(myVisaPM.ReadString());
        }
        public float[] READ_IEEEBlock(IEEEBinaryType _type)
        {
            return (float[])myVisaPM.ReadIEEEBlock(_type, true, true);
        }
        public float[] WRITE_READ_IEEEBlock(string _cmd, IEEEBinaryType _type)
        {
            myVisaPM.WriteString(_cmd, true);
            return (float[])myVisaPM.ReadIEEEBlock(_type, true, true);
        }

        #endregion
    }

}
