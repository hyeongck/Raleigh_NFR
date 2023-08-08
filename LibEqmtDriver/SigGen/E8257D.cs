using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.SG
{
    public class E8257D : Base_SG, iSiggen
    {
        public static string ClassName = "E8257D Siggen Class";
        private FormattedIO488 myVisaSg = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "E8257D"; }

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
                return myVisaSg;
            }
            set
            {
                myVisaSg = parseIO;
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    myVisaSg.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, OptionString);
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Class Name: " + ClassName + "\nParameters: OpenIO" + "\n\nErrorDesciption: \n"
                        + ex, "Error found in Class " + ClassName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    myVisaSg.IO = null;
                    return;
                }
            }
        }

        public E8257D(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~E8257D() { }

        #region iSeggen Members
        public override void Close()
        {
            if (myVisaSg.IO != null)
            {
                myVisaSg.IO.Close();
            }
        }
        public override void Initialize()
        {
            try
            {
                myVisaSg.WriteString("*IDN?", true);
                string result = myVisaSg.ReadString();
            }
            catch (Exception ex)
            {
                throw new Exception("EquipE8257D: Initialization -> " + ex.Message);
            }

        }
        public override void Reset()
        {
            try
            {
                myVisaSg.WriteString("*CLS; *RST", true);
            }
            catch (Exception ex)
            {
                throw new Exception("EquipE8257D: RESET -> " + ex.Message);
            }
        }
        public override void EnableRF(INSTR_OUTPUT _ONOFF)
        {
            myVisaSg.WriteString("POW:STAT " + _ONOFF, true);

        }
        public override void EnableModulation(INSTR_OUTPUT _ONOFF)
        {
            myVisaSg.WriteString("OUTP:MOD " + _ONOFF, true);
        }
        public override void SetAmplitude(float _dBm)
        {
            myVisaSg.WriteString("POW:LEV:IMM:AMPL " + _dBm.ToString(), true);
        }
        public override void SetFreq(double _MHz)
        {
            myVisaSg.WriteString("FREQ:FIX " + _MHz.ToString() + "MHz", true);
        }
        public override void SetPowerMode(N5182_POWER_MODE _mode)
        {
            myVisaSg.WriteString(":POW:MODE " + _mode.ToString(), true);
        }
        public override void SetFreqMode(N5182_FREQUENCY_MODE _mode)
        {
            myVisaSg.WriteString(":FREQ:MODE " + _mode.ToString(), true);
        }
        public override void MOD_FORMAT_WITH_LOADING_CHECK(string strWaveform, string strWaveformName, bool WaveformInitalLoad)
        {
            //Not applicable
        }
        public override void SELECT_WAVEFORM(N5182A_WAVEFORM_MODE _MODE)
        {
            //Not applicable
        }
        public override void SET_LIST_TYPE(N5182_LIST_TYPE _mode)
        {
            //myVisaSg.WriteString(":LIST:TYPE " + _mode.ToString(), true);
        }
        public override void SET_LIST_MODE(INSTR_MODE _mode)
        {
            //myVisaSg.WriteString(":LIST:MODE " + _mode.ToString(), true);
        }
        public override void SET_LIST_TRIG_SOURCE(N5182_TRIG_TYPE _mode)
        {
            //myVisaSg.WriteString(":LIST:TRIG:SOUR " + _mode.ToString(), true);
        }
        public override void SET_CONT_SWEEP(INSTR_OUTPUT _ONOFF)        // Set up for single sweep
        {
            myVisaSg.WriteString(":INIT:CONT " + _ONOFF.ToString(), true);
        }
        public override void SET_START_FREQUENCY(double _MHz)
        {
            myVisaSg.WriteString("FREQ:START " + _MHz.ToString() + "MHz", true);
        }
        public override void SET_STOP_FREQUENCY(float _MHz)
        {
            myVisaSg.WriteString("FREQ:STOP " + _MHz.ToString() + "MHz", true);
        }
        public override void SET_TRIG_TIMERPERIOD(double _ms)
        {
            double second = _ms / 1e3;
            myVisaSg.WriteString("SWE:TIME " + second, true);
        }
        public override void SET_SWEEP_POINT(int _points)
        {
            myVisaSg.WriteString("SWE:POIN " + _points.ToString(), true);
        }
        public override void SINGLE_SWEEP()
        {
            myVisaSg.WriteString("INIT:IMM", true);
        }
        public override void SET_SWEEP_PARAM(int _points, double _ms, double _startFreqMHz, double _stopFreqMHz)
        {
            double totalSweepT = _ms * _points;              //calculate dwelltime(mS) per point to total sweepTime(mS)
            myVisaSg.WriteString("FREQ:START " + _startFreqMHz.ToString() + "MHz", true);
            myVisaSg.WriteString("FREQ:STOP " + _stopFreqMHz.ToString() + "MHz", true);
            myVisaSg.WriteString("SWE:POIN " + _points.ToString(), true);
            myVisaSg.WriteString("SWE:TIME " + totalSweepT.ToString() + "ms", true);
        }
        public override bool OPERATION_COMPLETE()
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
                throw new Exception("E8257D: OPERATION_COMPLETE -> " + ex.Message);
            }
        }
        public override void SET_ROUTE_CONN_EVENT(N5182A_ROUTE_SUBSYS _MODE)
        {
            //Not applicable
        }
        public override void SET_ROUTE_CONN_TOUT(N5182A_ROUTE_SUBSYS _MODE)
        {
            //Not applicable
        }
        public override void SET_ROUTE_CONN_SOUT(N5182A_ROUTE_SUBSYS _MODE)
        {
            //Not applicable
        }
        public override void SET_ALC_TRAN_REF(N5182A_ALC_TRAN_REF _MODE)
        {
            //Not applicable
        }
        public override void QueryError_SG(out bool status)
        {
            status = false;
        }
        #endregion iSiggen Members

        public string QUERY_ERROR()
        {
            string ErrMsg, TempErrMsg = "";
            int ErrNum;
            try
            {
                ErrMsg = WRITE_READ_STRING("SYST:ERR?");
                TempErrMsg = ErrMsg;
                // Remove the error number
                ErrNum = Convert.ToInt16(ErrMsg.Remove((ErrMsg.IndexOf(",")),
                    (ErrMsg.Length) - (ErrMsg.IndexOf(","))));
                if (ErrNum != 0)
                {
                    while (ErrNum != 0)
                    {
                        TempErrMsg = ErrMsg;

                        // Check for next error(s)
                        ErrMsg = WRITE_READ_STRING("SYST:ERR?");

                        // Remove the error number
                        ErrNum = Convert.ToInt16(ErrMsg.Remove((ErrMsg.IndexOf(",")),
                            (ErrMsg.Length) - (ErrMsg.IndexOf(","))));
                    }
                }
                return TempErrMsg;
            }
            catch (Exception ex)
            {
                throw new Exception("EquipE8257D: QUERY_ERROR --> " + ex.Message);
            }
        }

        #region generic READ and WRITE function
        public float WRITE_READ_SINGLE(string _cmd)
        {
            myVisaSg.WriteString(_cmd, true);
            return Convert.ToSingle(myVisaSg.ReadString());
        }
        public float[] READ_IEEEBlock(IEEEBinaryType _type)
        {
            return (float[])myVisaSg.ReadIEEEBlock(_type, true, true);
        }
        public float[] WRITE_READ_IEEEBlock(string _cmd, IEEEBinaryType _type)
        {
            myVisaSg.WriteString(_cmd, true);
            return (float[])myVisaSg.ReadIEEEBlock(_type, true, true);
        }
        public void WRITE(string _cmd)
        {
            myVisaSg.WriteString(_cmd, true);
        }
        public double WRITE_READ_DOUBLE(string _cmd)
        {
            myVisaSg.WriteString(_cmd, true);
            return Convert.ToDouble(myVisaSg.ReadString());
        }
        public string WRITE_READ_STRING(string _cmd)
        {
            myVisaSg.WriteString(_cmd, true);
            return myVisaSg.ReadString();
        }
        public void WriteInt16Array(string command, Int16[] data)
        {
            myVisaSg.WriteIEEEBlock(command, data, true);
        }

        public void WriteByteArray(string command, byte[] data)
        {
            myVisaSg.WriteIEEEBlock(command, data, true);
        }
        #endregion
    }
}
