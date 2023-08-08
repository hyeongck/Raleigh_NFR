using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace LibEqmtDriver.SA
{
    public class N9020A : Base_SA, iSigAnalyzer
    {
        public static string ClassName = "N9020A MXA Class";
        private FormattedIO488 myVisaSa = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "N9020A"; }
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
                return myVisaSa;
            }
            set
            {
                myVisaSa = parseIO;
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    myVisaSa.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, OptionString);
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Class Name: " + ClassName + "\nParameters: OpenIO" + "\n\nErrorDesciption: \n"
                        + ex, "Error found in Class " + ClassName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    myVisaSa.IO = null;
                    return;
                }
            }
        }
        public N9020A(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~N9020A() { }

        #region iSigAnalyzer Members
        public override void Close()
        {
            if (myVisaSa.IO != null)
            {
                myVisaSa.IO.Close();
            }
        }
        public override void Initialize(int EquipId)
        {
            myVisaSa.WriteString(":INST SA", true);

            RESET();
            if (EquipId == 1)   // PSA
            {
                myVisaSa.WriteString("CORR:CSET1 OFF", true);
                myVisaSa.WriteString("CORR:CSET2 OFF", true);
                myVisaSa.WriteString("CORR:CSET3 OFF", true);
                myVisaSa.WriteString("CORR:CSET4 OFF", true);
                myVisaSa.WriteString("CORR:CSET:ALL OFF", true);
            }
            else if (EquipId == 3) // MXA
                myVisaSa.WriteString(":CORR:SA:GAIN 0", true);

            myVisaSa.WriteString(":FORM:DATA REAL,32", true);
            //myVisaSa.WriteString(":DET AVER", true);
            //myVisaSa.WriteString(":AVER:TYPE RMS", true);
            myVisaSa.WriteString(":INIT:CONT 1", true);
            myVisaSa.WriteString(":BWID:VID:RAT " + "10", true);
            myVisaSa.WriteString("SWE:POIN 301", true);

            // Alignment
            if (EquipId == 1)   // PSA
            {

            }
        }
        public override void Preset()
        {
            myVisaSa.WriteString("SYST:PRES", true);
        }
        public override void Select_Instrument(N9020A_INSTRUMENT_MODE _MODE)
        {
            switch (_MODE)
            {
                case N9020A_INSTRUMENT_MODE.SpectrumAnalyzer: myVisaSa.WriteString("INST:SEL SA", true); break;
                case N9020A_INSTRUMENT_MODE.Basic: myVisaSa.WriteString("INST:SEL BASIC", true); break;
                case N9020A_INSTRUMENT_MODE.Wcdma: myVisaSa.WriteString("INST:SEL WCDMA", true); break;
                case N9020A_INSTRUMENT_MODE.WIMAX: myVisaSa.WriteString("INST:SEL WIMAXOFDMA", true); break;
                case N9020A_INSTRUMENT_MODE.EDGE_GSM: myVisaSa.WriteString("INST:SEL EDGEGSM", true); break;
                default: throw new Exception("Not such a intrument mode!");
            }
        }
        public override void Select_Triggering(N9020A_TRIGGERING_TYPE _TYPE)
        {
            switch (_TYPE)
            {
                ///******************************************
                /// SweptSA mode trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.RF_Ext1: myVisaSa.WriteString("TRIG:RF:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.RF_Ext2: myVisaSa.WriteString("TRIG:RF:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.RF_RFBurst: myVisaSa.WriteString("TRIG:RF:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.RF_Video: myVisaSa.WriteString("TRIG:RF:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.RF_FreeRun: myVisaSa.WriteString("TRIG:RF:SOUR IMM", true); break;

                ///******************************************
                /// EDGEGSM Transmit power type trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.TXP_Ext1: myVisaSa.WriteString("TRIG:TXP:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.TXP_Ext2: myVisaSa.WriteString("TRIG:TXP:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.TXP_RFBurst: myVisaSa.WriteString("TRIG:TXP:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.TXP_Video: myVisaSa.WriteString("TRIG:TXP:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.TXP_FreeRun: myVisaSa.WriteString("TRIG:TXP:SOUR IMM", true); break;

                ///******************************************
                /// EDGEGSM Power Vs Time type trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.PVT_Ext1: myVisaSa.WriteString("TRIG:PVT:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.PVT_Ext2: myVisaSa.WriteString("TRIG:PVT:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.PVT_RFBurst: myVisaSa.WriteString("TRIG:PVT:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.PVT_Video: myVisaSa.WriteString("TRIG:PVT:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.PVT_FreeRun: myVisaSa.WriteString("TRIG:PVT:SOUR IMM", true); break;

                ///******************************************
                /// EDGEGSM Power Vs Time type trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.EPVT_Ext1: myVisaSa.WriteString("TRIG:EPVT:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.EPVT_Ext2: myVisaSa.WriteString("TRIG:EPVT:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.EPVT_RFBurst: myVisaSa.WriteString("TRIG:EPVT:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.EPVT_Video: myVisaSa.WriteString("TRIG:EPVT:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.EPVT_FreeRun: myVisaSa.WriteString("TRIG:EPVT:SOUR IMM", true); break;

                ///******************************************
                /// EDGEGSM Edge Power Vs Time type trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.EORFS_Ext1: myVisaSa.WriteString("TRIG:EORF:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.EORFS_Ext2: myVisaSa.WriteString("TRIG:EORF:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.EORFS_RFBurst: myVisaSa.WriteString("TRIG:EORF:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.EORFS_Video: myVisaSa.WriteString("TRIG:EORF:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.EORFS_FreeRun: myVisaSa.WriteString("TRIG:EORF:SOUR IMM", true); break;

                ///******************************************
                /// EDGEGSM Edge EVM type trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.EEVM_Ext1: myVisaSa.WriteString("TRIG:EEVM:SOUR EXT1", true); break;
                case N9020A_TRIGGERING_TYPE.EEVM_Ext2: myVisaSa.WriteString("TRIG:EEVM:SOUR EXT2", true); break;
                case N9020A_TRIGGERING_TYPE.EEVM_RFBurst: myVisaSa.WriteString("TRIG:EEVM:SOUR RFB", true); break;
                case N9020A_TRIGGERING_TYPE.EEVM_Video: myVisaSa.WriteString("TRIG:EEVM:SOUR VID", true); break;
                case N9020A_TRIGGERING_TYPE.EEVM_FreeRun: myVisaSa.WriteString("TRIG:EEVM:SOUR IMM", true); break;
                default: throw new Exception("Not such a Trigger Mode!");
            }

        }
        public override void Measure_Setup(N9020A_MEAS_TYPE _TYPE)
        {
            switch (_TYPE)
            {
                case N9020A_MEAS_TYPE.SweptSA: myVisaSa.WriteString(":INIT:SAN", true); break;
                case N9020A_MEAS_TYPE.ChanPower: myVisaSa.WriteString(":INIT:CHP", true); break;
                case N9020A_MEAS_TYPE.ACP: myVisaSa.WriteString(":CONF:ACP:NDEF", true); break;
                case N9020A_MEAS_TYPE.BTxPow: myVisaSa.WriteString(":INIT:TXP", true); break;
                case N9020A_MEAS_TYPE.GPowVTM: myVisaSa.WriteString(":INIT:PVT", true); break;
                case N9020A_MEAS_TYPE.GPHaseFreq: myVisaSa.WriteString(":INIT:PFER", true); break;
                case N9020A_MEAS_TYPE.GOutRFSpec: myVisaSa.WriteString(":INIT:ORFS", true); break;
                case N9020A_MEAS_TYPE.GTxSpur: myVisaSa.WriteString(":INIT:TSP", true); break;
                case N9020A_MEAS_TYPE.EPowVTM: myVisaSa.WriteString(":INIT:EPVT", true); break;
                case N9020A_MEAS_TYPE.EEVM: myVisaSa.WriteString(":INIT:EEVM", true); break;
                case N9020A_MEAS_TYPE.EOutRFSpec: myVisaSa.WriteString(":INIT:EORF", true); break;
                case N9020A_MEAS_TYPE.ETxSpur: myVisaSa.WriteString(":INIT:ETSP", true); break;
                case N9020A_MEAS_TYPE.MonitorSpec: break;
                default: throw new Exception("Not such a Measure setup type!");
            }

        }
        public override void Enable_Display(N9020A_DISPLAY _ONOFF)
        {
            myVisaSa.WriteString(":DISP:ENAB " + _ONOFF, true);
        }
        public override void VBW_RATIO(double _ratio)
        {
            myVisaSa.WriteString("BAND:VID:RAT " + _ratio.ToString(), true);
        }
        public override void SPAN(double _freq_MHz)
        {
            myVisaSa.WriteString("FREQ:SPAN " + _freq_MHz.ToString() + " MHz", true);
        }
        public override void MARKER_TURN_ON_NORMAL_POINT(int _markerNum, float _MarkerFreq_MHz)
        {
            myVisaSa.WriteString("CALC:MARK" + _markerNum.ToString() + ":MODE POS", true);
            myVisaSa.WriteString("CALC:MARK" + _markerNum.ToString() + ":X " + _MarkerFreq_MHz.ToString() + " MHz", true);
        }
        public override void TURN_ON_INTERNAL_PREAMP()
        {
            myVisaSa.WriteString("POW:GAIN ON", true);
            myVisaSa.WriteString("POW:GAIN:BAND FULL", true);
        }
        public override void TURN_OFF_INTERNAL_PREAMP()
        {
            myVisaSa.WriteString("POW:GAIN OFF", true);
        }
        public override void TURN_OFF_MARKER()
        {
            myVisaSa.WriteString(":CALC:MARK:AOFF", true);
        }
        public override double READ_MARKER(int _markerNum)
        {
            return WRITE_READ_DOUBLE("CALC:MARK" + _markerNum.ToString() + ":Y?");
        }
        public override void SWEEP_TIMES(int _sweeptime_ms)
        {
            myVisaSa.WriteString(":SWE:TIME " + _sweeptime_ms.ToString() + " ms", true);
        }
        public override void SWEEP_POINTS(int _sweepPoints)
        {
            myVisaSa.WriteString(":SWE:POIN " + _sweepPoints.ToString(), true);
        }
        public override void CONTINUOUS_MEASUREMENT_ON()
        {
            myVisaSa.WriteString("INIT:CONT 1", true);
        }
        public override void CONTINUOUS_MEASUREMENT_OFF()
        {
            myVisaSa.WriteString("INIT:CONT 0", true);
        }
        public override void RESOLUTION_BW(double _BW)
        {
            myVisaSa.WriteString(":BAND " + _BW.ToString(), true);
        }
        public override double MEASURE_PEAK_POINT(int delayMs)
        {
            myVisaSa.WriteString("CALC:MARK:MAX", true);
            Thread.Sleep(delayMs);
            bool status = Operation_Complete();
            return WRITE_READ_DOUBLE("CALC:MARK:Y?");
        }
        public override double MEASURE_PEAK_FREQ(int delayMs)
        {
            myVisaSa.WriteString("CALC:MARK:MAX", true);
            Thread.Sleep(delayMs);
            bool status = Operation_Complete();
            return WRITE_READ_DOUBLE("CALC:MARK:X?");
        }
        public override void VIDEO_BW(double _VBW_Hz)
        {
            myVisaSa.WriteString(":BAND:VID " + _VBW_Hz, true);
        }
        public override void TRIGGER_CONTINUOUS()
        {
            myVisaSa.WriteString("INIT:CONT ON", true);
        }
        public override void TRIGGER_SINGLE()
        {
            myVisaSa.WriteString("INIT:CONT OFF", true);
        }
        public override void TRIGGER_IMM()
        {
            myVisaSa.WriteString("INIT:IMM", true);
        }
        public override void TRACE_AVERAGE(int _AVG)
        {
            myVisaSa.WriteString(":AVERage:COUN " + _AVG.ToString(), true);
            myVisaSa.WriteString(":TRAC:TYPE AVER", true);
        }
        public override void AVERAGE_OFF()
        {
            myVisaSa.WriteString(":AVER:STAT OFF", true);
        }
        public override void AVERAGE_ON()
        {
            myVisaSa.WriteString(":AVER:STAT ON", true);
        }
        public override void SET_TRACE_DETECTOR(string mode)
        {
            switch (mode.ToUpper())
            {
                case "WRIT":
                case "WRITE":
                    myVisaSa.WriteString("TRAC:TYPE WRIT", true);
                    break;
                case "MAXH":
                case "MAXHOLD":
                    myVisaSa.WriteString("TRAC:TYPE MAXH", true);
                    break;
                case "MINH":
                case "MINHOLD":
                    myVisaSa.WriteString("TRAC:TYPE MINH", true);
                    break;
            }
        }
        public override void CLEAR_WRITE()
        {
            myVisaSa.WriteString(":TRAC:TYPE WRIT", true);
        }
        public override void AMPLITUDE_REF_LEVEL_OFFSET(double _RefLvlOffset)
        {
            myVisaSa.WriteString("DISP:WIND:TRAC:Y:RLEV:OFFS " + _RefLvlOffset, true);
        }
        public override void AMPLITUDE_REF_LEVEL(double _RefLvl)
        {
            myVisaSa.WriteString("DISP:WIND:TRAC:Y:RLEV " + _RefLvl, true);
        }
        public override void AMPLITUDE_INPUT_ATTENUATION(double _Input_Attenuation)
        {
            myVisaSa.WriteString("POW:ATT " + _Input_Attenuation, true);
        }
        public override void AUTO_ATTENUATION(bool state)
        {
            if (state)
            {
                myVisaSa.WriteString(":SENS:POW:ATT:AUTO ON", true);
            }
            else
            {
                myVisaSa.WriteString(":SENS:POW:ATT:AUTO OFF", true);
            }
        }
        public override void ELEC_ATTENUATION(float _Input_Attenuation)
        {
            myVisaSa.WriteString("POW:EATT " + _Input_Attenuation, true);
        }
        public override void ELEC_ATTEN_ENABLE(bool _Input_Stat)
        {
            if (_Input_Stat)
                myVisaSa.WriteString("POW:EATT:STAT ON", true);
            else
                myVisaSa.WriteString("POW:EATT:STAT OFF", true);
        }
        public override void ALIGN_PARTIAL()
        {
            myVisaSa.WriteString(":CAL:AUTO PART", true);
        }
        public override void ALIGN_ONCE()
        {
            myVisaSa.WriteString(":CAL:AUTO ONCE", true);
        }
        public override void AUTOALIGN_ENABLE(bool _Input_Stat)
        {
            if (_Input_Stat)
                myVisaSa.WriteString(":CAL:AUTO ON", true);
            else
                myVisaSa.WriteString(":CAL:AUTO OFF", true);
        }
        public override void CAL()
        {
            myVisaSa.WriteString(":CAL", true);
        }
        public override bool OPERATION_COMPLETE()
        {
            bool _complete = false;
            double _dummy = -9;
            do
            {
                //timer.wait(2);
                _dummy = WRITE_READ_DOUBLE("*OPC?");
            } while (_dummy == 0);
            _complete = true;
            return _complete;
        }
        public override void START_FREQ(string strFreq, string strUnit)
        {
            myVisaSa.WriteString("SENS:FREQ:STAR " + strFreq + strUnit, true);
        }
        public override void STOP_FREQ(string strFreq, string strUnit)
        {
            myVisaSa.WriteString("SENS:FREQ:STOP " + strFreq + strUnit, true);
        }
        public override void FREQ_CENT(string strSaFreq, string strUnit)
        {
            myVisaSa.WriteString(":FREQ:CENT " + strSaFreq + strUnit, true);
        }
        public override string READ_MXATrace(int _traceNum)
        {
            myVisaSa.WriteString(":FORM ASC", true);
            return WRITE_READ_STRING(":TRAC? TRACE" + _traceNum.ToString());
        }
        public override double READ_STARTFREQ()
        {
            return WRITE_READ_DOUBLE("SENS:FREQ:STAR?");
        }
        public override double READ_STOPFREQ()
        {
            return WRITE_READ_DOUBLE("SENS:FREQ:STOP?");
        }
        public override float READ_SWEEP_POINTS()
        {
            return WRITE_READ_SINGLE(":SWE:POIN?");
        }
        public override float[] IEEEBlock_READ_MXATrace(int _traceNum)
        {
            float[] arrSaTraceData = null;
            myVisaSa.WriteString(":FORM:DATA REAL,32", true);
            arrSaTraceData = WRITE_READ_IEEEBlock(":TRAC? TRACE" + _traceNum.ToString(), IEEEBinaryType.BinaryType_R4);

            myVisaSa.WriteString(":FORM ASC", true);
            return arrSaTraceData;
        }
        public override void SET_TRIG_DELAY(N9020A_TRIGGERING_TYPE _TYPE, string delayMs)
        {
            switch (_TYPE)
            {
                ///******************************************
                /// SweptSA mode trigerring
                ///******************************************
                case N9020A_TRIGGERING_TYPE.RF_Ext1:
                    myVisaSa.WriteString("TRIG:EXT1:DEL " + delayMs + " ms", true);
                    break;
                case N9020A_TRIGGERING_TYPE.RF_Ext2:
                    myVisaSa.WriteString("TRIG:EXT2:DEL " + delayMs + " ms", true); 
                    break;
                case N9020A_TRIGGERING_TYPE.RF_RFBurst:
                    myVisaSa.WriteString("TRIG:RFB:DEL " + delayMs + " ms", true);
                    break;
                case N9020A_TRIGGERING_TYPE.RF_Video:
                    myVisaSa.WriteString("TRIG:VID:DEL " + delayMs + " ms", true); 
                    break;
                case N9020A_TRIGGERING_TYPE.RF_FreeRun:
                    //do nothing
                    break;
                default: 
                    //do notihing
                    break;
            }

        }
        public override void MARKER_NOISE(bool state, int _markerNum, double bandSpan_Hz)
        {
            if (state)
            {
                myVisaSa.WriteString("CALC:MARK" + _markerNum.ToString() + ":FUNC NOIS", true);
                myVisaSa.WriteString("CALC:MARK" + _markerNum.ToString() + ":FUNC:BAND:SPAN " + bandSpan_Hz, true);
            }
            else
            {
                myVisaSa.WriteString("CALC:MARK" + _markerNum.ToString() + ":FUNC OFF", true);
            }
        }
        #endregion iSigAnalyzer Members

        public bool Operation_Complete()
        {
            bool _complete = false;
            double _dummy = -9;
            do
            {
                //timer.wait(2);
                _dummy = WRITE_READ_DOUBLE("*OPC?");
            } while (_dummy == 0);
            _complete = true;
            return _complete;
        }
        public void RESET()
        {
            try
            {
                myVisaSa.WriteString("*CLS; *RST", true);
            }
            catch (Exception ex)
            {
                throw new Exception("EquipSA: RESET -> " + ex.Message);
            }
        }
        #region READ and WRITE function
        public string READ_STRING()
        {
            return myVisaSa.ReadString();
        }
        public void WRITE(string _cmd)
        {
            myVisaSa.WriteString(_cmd, true);
        }
        public double WRITE_READ_DOUBLE(string _cmd)
        {
            myVisaSa.WriteString(_cmd, true);
            return Convert.ToDouble(myVisaSa.ReadString());
        }
        public string WRITE_READ_STRING(string _cmd)
        {
            myVisaSa.WriteString(_cmd, true);
            return myVisaSa.ReadString();
        }
        public float WRITE_READ_SINGLE(string _cmd)
        {
            myVisaSa.WriteString(_cmd, true);
            return Convert.ToSingle(myVisaSa.ReadString());
        }
        public float[] READ_IEEEBlock(IEEEBinaryType _type)
        {
            return (float[])myVisaSa.ReadIEEEBlock(_type, true, true);
        }
        public float[] WRITE_READ_IEEEBlock(string _cmd, IEEEBinaryType _type)
        {
            myVisaSa.WriteString(_cmd, true);
            return (float[])myVisaSa.ReadIEEEBlock(_type, true, true);
        }
        #endregion


    }
}
