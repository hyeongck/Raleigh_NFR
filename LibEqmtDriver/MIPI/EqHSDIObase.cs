using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibEqmtDriver.MIPI
{
    public partial class EqHSDIO
    {
        public enum UNIO_EEPROMType : int
        {
            Socket = 0,
            Loadboard = 7
        }

        private enum UNIO_EEPROMOpCode : int
        {
            Access = 0x0A,
            SecurityRegisterAccess = 0x0B,
            LockSecurityRegister = 0x02,
            ROMZoneRegisterAccess = 0x07,
            FreezeROMZoneState = 0x01,
            ManufacturerIDRead = 0x0C,
            StandardSpeedMode = 0x0D,
            HighSpeedMode = 0x0E
        }

        public enum PPMUVioOverrideString
        {
            RESET, VIOOFF, VIOON,
            RESET_TX, VIOOFF_TX, VIOON_TX,
            RESET_RX, VIOOFF_RX, VIOON_RX
        };

        public enum ReadTimeset
        {
            FULL,
            HALF,
            QUAD
        }

        public static bool TRXQC { get; set; }
        public static bool usingMIPI = false;
        public static bool skipVio = true;
        public static bool isVioTxPpmu; // Pinot added (Pcon)
        public const string dacQ2NamePrefix = "dacQ2";  // check and get rid of KH
        public const string dacQ1NamePrefix = "dacQ1";
        public const string Reset = "RESET", HiZ = "HiZ", RegIO = "regIO", Eeprom = "eeprom", VioOff = "viooff", VioOn = "vioon";
        public static string Sclk1ChanName = "", Sdata1ChanName = "", Vio1ChanName = "", Sclk2ChanName = "", Sdata2ChanName = "", Vio2ChanName = "", TrigChanName = ""; //ShieldChanName = "";
        public static string SkChanName = "SK", DiChanName = "DI", DoChanName = "DO";
        public static double TempSenseRaw;
        public static Dictionary<byte, double> Tempsenseraw = new Dictionary<byte, double>();

        //public static Dictionary<string, bool> datalogResults = new Dictionary<string, bool>();
        public static string dutSlaveAddress;

        public static int dutSlavePairIndex;
        public static int Num_Mipi_Bus;
        public static Dictionary<string, string> ConfigRegisterSettings = new Dictionary<string, string>();
        public static decimal I2CTempSensorDeviceAddress;
        public static double MIPIClockRate;
        public static double StrobePoint;

        // Added for HLS2.
        public static double StrobePointNRZHalf;

        public static double StrobePointRZ;
        public static double StrobePointRZHalf;

        public static EqHSDIObase Get(string VisaAlias, byte site)
        {
            EqHSDIObase hsdio;

            if (VisaAlias.Contains("9195"))
            {
                //hsdio = new EqHSDIO.KeysightDSR();
                hsdio = null;
            }
            else if (VisaAlias.Contains("6570"))
            {
                hsdio = new EqHSDIO.NI6570();
            }
            else
            {
                throw new Exception("HSDIO visa alias not recognized, model unknown");
            }

            hsdio.VisaAlias = VisaAlias;
            hsdio.Site = site;
            return hsdio;
        }

        //TODO Project specific. For future EqHSDIO redesign.
        public static EqHSDIObase Get(string VisaAlias, byte site, string project)
        {
            EqHSDIObase hsdio = null;

            switch (project)
            {
                case "NIGHTHWARK":
                case "HLS2":
                    //hsdio = new EqHSDIO.NI6570_Hls2();
                    hsdio = null;   
                    break;

                case "JOKER":
                case "PINOT":
                case "LIGHTNING":
                case "SPARK":
                case "CHEDDAR":
                default:
                    hsdio = new EqHSDIO.NI6570();
                    break;
            }

            hsdio.VisaAlias = VisaAlias;
            hsdio.Site = site;
            return hsdio;
        }

        public static List<string> CreateWaveformList(params string[] waveformNames)
        {
            List<string> MipiWaveformNames = new List<string>();

            foreach (string waveformName in waveformNames)
            {
                if (waveformName != null && waveformName != "" && waveformName != dacQ1NamePrefix && waveformName != dacQ2NamePrefix)
                {
                    string name = waveformName.Replace("_", "");
                    MipiWaveformNames.Add(name);
                }
            }

            return MipiWaveformNames;
        }

        public static void selectorMipiPair(int Pair, string slaveaddress = null)
        {
            dutSlaveAddress = slaveaddress ?? LibEqmtDriver.MIPI.Site[0].HSDIO.Digital_Definitions["MIPI" + Pair + "_SLAVE_ADDR"];
            dutSlavePairIndex = Pair;
        }

        private static string TranslateNiTriggerLine(TriggerLine trigLine)
        {
            switch (trigLine)
            {
                case TriggerLine.None:
                    return "";

                case TriggerLine.FrontPanel0:
                    return "PFI0";

                case TriggerLine.FrontPanel1:
                    return "PFI1";

                case TriggerLine.FrontPanel2:
                    return "PFI2";

                case TriggerLine.FrontPanel3:
                    return "PFI3";

                case TriggerLine.PxiTrig0:
                    return "PXI_Trig0";

                case TriggerLine.PxiTrig1:
                    return "PXI_Trig1";

                case TriggerLine.PxiTrig2:
                    return "PXI_Trig2";

                case TriggerLine.PxiTrig3:
                    return "PXI_Trig3";

                case TriggerLine.PxiTrig4:
                    return "PXI_Trig4";

                case TriggerLine.PxiTrig5:
                    return "PXI_Trig5";

                case TriggerLine.PxiTrig6:
                    return "PXI_Trig6";

                case TriggerLine.PxiTrig7:
                    return "PXI_Trig7";

                default:
                    throw new Exception("NI HSDIO trigger line not supported");
            }
        }

        public class Config
        {
            public List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands;
            public eMipiTestType _eMipiTestType;

            public Config(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands, eMipiTestType _eMipiTestType)
            {
                this.MipiCommands = MipiCommands;
                this._eMipiTestType = _eMipiTestType;
            }
        }

        public abstract class EqHSDIObase
        {
            public bool FirstVector = false;
            public bool LastVector = false;
            public bool isRZ = true;
            public bool isShareBus = true;
            public string scriptFull = "";
            public byte Site { get; set; }
            public string VisaAlias { get; set; }
            public Dictionary<string, string> PinNamesAndChans { get; set; }
            public List<Dictionary<string, string>> customMIPIlist { get; set; }
            public ConcurrentBag<Task> _VectorTaskBags = new ConcurrentBag<Task>();

            public double BeforeDelay;
            public double AfterDelay;
            public int nBeforeCmd;
            public List<string> QC_Fail_list = new List<string>();

            public ManualResetEvent ThreadMipi;
            public Stopwatch SW_MIPI;
            public ReadTimeset ReadTimeset { get; set; }
            public virtual TriggerLine TriggerOut { get; set; }

            public Dictionary<string, string> Digital_Definitions;
            public Dictionary<string, double> DicTestPA2a;
            public Dictionary<string, uint[]> Digital_Mipi_Trig;
            public Dictionary<string, int> TRXQC_Errors = new Dictionary<string, int>();

            public enum Adv_OTP_2DIDs { ADV_PCB_LOT_ID_D0_4, ADV_PCB_PANEL_ID_D4_2, ADV_PCB_STRIP_ID_D6_2, ADV_PCB_MOD_ID_D8_4 };

            public Dictionary<Adv_OTP_2DIDs, List<MipiSyntaxParser.ClsMIPIFrame>> Adv_OTP_Frames = new Dictionary<Adv_OTP_2DIDs, List<MipiSyntaxParser.ClsMIPIFrame>>();

            public EqHSDIObase()
            {
                ReadTimeset = ReadTimeset.HALF;
            }

            public bool Initialize(Dictionary<string, EqDC.iEqDC> DcResources)
            {
                try
                {
                    Num_Mipi_Bus = Convert.ToUInt16(Get_Digital_Definition("NUM_MIPI_BUS"));
                    PinNamesAndChans = new Dictionary<string, string>();

                    Sclk1ChanName = Get_Digital_Definition("SCLK1_VEC_NAME");
                    Sdata1ChanName = Get_Digital_Definition("SDATA1_VEC_NAME");
                    Vio1ChanName = Get_Digital_Definition("VIO1_VEC_NAME");

                    if (Num_Mipi_Bus == 2)
                    {
                        Sclk2ChanName = Get_Digital_Definition("SCLK2_VEC_NAME");
                        Sdata2ChanName = Get_Digital_Definition("SDATA2_VEC_NAME");
                        Vio2ChanName = Get_Digital_Definition("VIO2_VEC_NAME");
                    }

                    // retrieve MIPI pin Visa Alias and Channel Number from our SMU Resource dictionary
                    foreach (string pinName in DcResources.Keys)
                    {
                        if (pinName.ToUpper() == Sdata1ChanName || pinName.ToUpper() == Sclk1ChanName || pinName.ToUpper() == Vio1ChanName || pinName.ToUpper() == Sdata2ChanName || pinName.ToUpper() == Sclk2ChanName || pinName.ToUpper() == Vio2ChanName)
                        {
                            PinNamesAndChans.Add(pinName.ToUpper(), DcResources[pinName].ChanNumber);
                        }
                    }

                    if (!(PinNamesAndChans.ContainsKey(Sclk1ChanName) & PinNamesAndChans.ContainsKey(Sdata1ChanName) & PinNamesAndChans.ContainsKey(Vio1ChanName)))
                    {
                        throw new Exception("Did not find one of the following MIPI channel names in TCF:\n" + Sclk1ChanName + ",  " + Sdata1ChanName + ",  " + Vio1ChanName);
                    }

                    Initialize();

                    LoadVector_UNIO_EEPROM();

                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiReset()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiVioOff()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiVioOn()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiHiZ()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiRegIO()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_EEPROM()));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_TEMPSENSEI2C(0)));
                    // _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_UNIO_EEPROM()));

                    //LoadVector_RFOnOffTest();
                    //LoadVector_RFOnOffTestRx(); //Rx Trigger

                    //this.LoadVector_RFOnOffSwitchTest();
                    //LoadVector_RFOnOffSwitchTest_WithPreMipi(); //Tx Trigger: RFOnOff + SwitchingTime
                    ////LoadVector_RFOnOffSwitchTest_With3TxPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime, 3TXPreMipi, For TX Band-To-Band

                    //LoadVector_RFOnOffSwitchTestRx();   //Rx Trigger: RFOnOff + SwitchingTime
                    //LoadVector_RFOnOffSwitchTestRx_WithPreMipi();   //Rx Trigger: RFOnOff + SwitchingTime, 1RXPreMipi, for LNA Output Switching
                    ////LoadVector_RFOnOffSwitchTestRx_With1Tx2RxPreMipi();    //Rx Trigger: RFOnOff + SwitchingTime, 1TXPreMipi, 2RXPreMipi, For LNA switching time (same output) G0 only

                    //LoadVector_RFOnOffSwitchTest2();     //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2
                    //LoadVector_RFOnOffSwitchTest2_WithPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, For CPL
                    ////LoadVector_RFOnOffSwitchTest2_With1Tx2RxPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, 2RXPreMipi, For TERM:TX:RX:TX

                    //LoadVector_RFOnOffSwitchTest2Rx();  //Rx Trigger: RFOnOff + SwitchingTime + SwitchingTime2

                    return true;
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.ToString(), "HSDIO MIPI");
                }

                return false;
            }

            #region Virtual Methods

            public virtual bool Initialize()
            {
                return true;
            }

            public virtual bool ReInitializeVIO(double violevel)
            {
                return true;
            }

            public virtual string GetInstrumentInfo()
            {
                return string.Empty;
            }

            public virtual bool LoadVector(List<string> fullPaths, string nameInMemory)
            {
                return true;
            }

            public virtual bool LoadVector_MipiHiZ()
            {
                return true;
            }

            public virtual bool LoadVector_MipiReset()
            {
                return true;
            }

            public virtual bool LoadVector_MipiVioOff()
            {
                return true;
            }

            public virtual bool LoadVector_MipiVioOn()
            {
                return true;
            }

            public virtual bool LoadVector_MipiRegIO()
            {
                return true;
            }

            public virtual bool LoadVector_EEPROM()
            {
                return true;
            }

            public virtual bool LoadVector_TEMPSENSEI2C(decimal TempSensorAddress = 3)
            {
                return false;
            }

            public virtual bool SendVector(string nameInMemory)
            {
                return true;
            }

            public virtual void QCFail_Log(string nameInMemory)
            {
            }

            public virtual bool SendVector(PPMUVioOverrideString vioEnum)
            {
                return SendVector(vioEnum.ToString());
            }

            public virtual bool Burn(string data_hex, bool invertData = false, int efuseDataByteNum = 0, string efuseCtlAddress = "C0")
            {
                return true;
            }

            public virtual bool SendVectorOTP(string TargetData, string CurrentData = "00", bool isEfuseBurn = false)
            {
                return true;
            }

            public virtual void SendNextVectors(bool firstTest, List<string> MipiWaveformNames)
            {
            }

            public virtual void SendMipiCommands(object inf)
            {
            }

            public virtual void SendMipiCommands(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands, eMipiTestType _eMipiTestType = eMipiTestType.Write, int TryCount = 1)
            {
            }

            public virtual void AddVectorsToScript(List<string> namesInMemory, bool finalizeScript)
            {
            }

            public virtual int GetNumExecErrors(string nameInMemory)
            {
                return 0;
            }

            public virtual void RegWriteMultiple(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)
            {
            }

            public virtual void RegWriteMultiplePair(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)
            {
            }

            public virtual void RegWrite(string registerAddress_hex, string data_hex, string slave_address = null, bool sendTrigger = false)
            {
            }

            public virtual string RegRead(string registerAddress_hex, string slave_address = null)
            {
                return string.Empty;
            }

            public virtual void EepromWrite(string dataWrite)
            {
            }

            public virtual string EepromRead()
            {
                return string.Empty;
            }

            public virtual bool LoadVector_UNIO_EEPROM()
            {
                return false;
            }

            public virtual bool LoadVector_UNIO_EEPROM_Discovery(int bus_no = 1)
            {
                return true;
            }

            public virtual bool LoadVector_UNIO_EEPROM_Write(int bus_no = 1)
            {
                return true;
            }

            public virtual bool LoadVector_UNIO_EEPROM_ReadID(int bus_no = 1)
            {
                return true;
            }

            public virtual bool LoadVector_UNIO_EEPROM_ReadCounter(int bus_no = 1)
            {
                return true;
            }

            public virtual bool LoadVector_UNIO_EEPPROM_ReadSerialNumber(int bus_no = 1)
            {
                return true;
            }

            public virtual bool LoadVector_UNIO_EEPROM_ReadMID(int bus_no = 1)
            {
                return true;
            }

            public virtual bool UNIO_EEPROMDiscovery(int bus_no = 1)
            {
                return false;
            }

            public virtual bool UNIO_EEPROMWriteID(UNIO_EEPROMType device, string dataWrite, int bus_no = 1)
            {
                return false;
            }

            public virtual bool UNIO_EEPROMWriteCounter(UNIO_EEPROMType device, uint count, int bus_no = 1)
            {
                return false;
            }

            public virtual bool UNIO_EEPROMFreeze(UNIO_EEPROMType device, int bus_no = 1)
            {
                return false;
            }

            public virtual string UNIO_EEPROMReadID(UNIO_EEPROMType device, int bus_no = 1)
            {
                return string.Empty;
            }

            public virtual uint UNIO_EEPROMReadCounter(UNIO_EEPROMType device, int bus_no = 1)
            {
                return 0;
            }

            public virtual string UNIO_EEPROMReadSerialNumber(UNIO_EEPROMType device, int bus_no = 1)
            {
                return string.Empty;
            }

            public virtual string UNIO_EEPROMReadMID(UNIO_EEPROMType device, int bus_no = 1)
            {
                return string.Empty;
            }

            public virtual double I2CTEMPSENSERead()
            {
                return 0;
            }

            public virtual bool I2CTEMPSENSEConfigure()
            {
                return false;
            }

            public virtual void Close()
            {
            }

            public virtual void SendTRIGVectors()
            {
            }

            public virtual void shmoo(string nameInMemory)
            {
            }

            //keng shan Added
            //           public abstract bool LoadVector_RFOnOffTest(bool isNRZ = false);
            public virtual bool LoadVector_RFOnOffTest(bool isNRZ = false)
            {
                return true;
            }// Trigger

            public virtual bool LoadVector_RFOnOffTestRx(bool isNRZ = false)
            {
                return true;
            } //Rx Trigger

            public virtual bool LoadVector_RFOnOffSwitchTest(bool isNRZ = false)
            {
                return true;
            }

            public virtual bool LoadVector_RFOnOffSwitchTest_WithPreMipi(bool isNRZ = false)
            {
                return true;
            } //Tx Trigger: RFOnOff + SwitchingTime

            public virtual bool LoadVector_RFOnOffSwitchTest_With3TxPreMipi(bool isNRZ = false)
            {
                return true;
            }    //Tx Trigger: RFOnOff + SwitchingTime, 3TXPreMipi, For TX Band-To-Band

            public virtual bool LoadVector_RFOnOffSwitchTestRx(bool isNRZ = false)
            {
                return true;
            }   //Rx Trigger: RFOnOff + SwitchingTime

            public virtual bool LoadVector_RFOnOffSwitchTestRx_WithPreMipi(bool isNRZ = false)
            {
                return true;
            }   //Rx Trigger: RFOnOff + SwitchingTime, 1RXPreMipi, for LNA Output Switching

            public virtual bool LoadVector_RFOnOffSwitchTestRx_With1Tx2RxPreMipi(bool isNRZ = false)
            {
                return true;
            }    //Rx Trigger: RFOnOff + SwitchingTime, 1TXPreMipi, 2RXPreMipi, For LNA switching time (same output) G0 only

            public virtual bool LoadVector_RFOnOffSwitchTest2(bool isNRZ = false)
            {
                return true;
            }     //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2

            public virtual bool LoadVector_RFOnOffSwitchTest2_WithPreMipi(bool isNRZ = false)
            {
                return true;
            }    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, For CPL

            public virtual bool LoadVector_RFOnOffSwitchTest2_With1Tx2RxPreMipi(bool isNRZ = false)
            {
                return true;
            }    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, 2RXPreMipi, For TERM:TX:RX:TX

            public virtual bool LoadVector_RFOnOffSwitchTest2Rx(bool isNRZ = false)
            {
                return true;
            }  //Rx Trigger: RFOnOff + SwitchingTime + SwitchingTime2

            public virtual bool SendRFOnOffTestVector(bool RxMode, string[] SwTimeCustomArry)
            {
                return true;
            }

            public virtual void SetSourceWaveformArry(string customMIPIlist)
            {
            }

            #endregion Virtual Methods

            public void AddOrReplaceTRxQCDic(string key, int val)
            {
                if (TRXQC_Errors.ContainsKey(key)) TRXQC_Errors[key] = val;
                else TRXQC_Errors.Add(key, val);
            }

            public string[] Get_Specific_Key(string Value, string MatchKey = "")
            {
                if (MatchKey != "")
                {
                    List<string> sarr = new List<string>();

                    string[] arr = Digital_Definitions.Where(c => c.Value == Value).ToDictionary(c => c.Key, c => c.Value).Keys.ToArray();
                    foreach (string item in arr)
                    {
                        if (item.Contains(MatchKey))
                        {
                            sarr.Add(item);
                        }
                    }
                    return sarr.ToArray();
                }
                else
                {
                    string CloestKey = Value;
                    string[] strArray = Digital_Definitions.Where(x => x.Key.Contains(CloestKey)).ToDictionary(x => x.Key, x => x.Value).Keys.ToArray();
                    return strArray;
                }

                return null;
            }

            public string Get_Digital_Definition(string key, string initVal)
            {
                if (Digital_Definitions.ContainsKey(key.ToUpper()))
                    return Digital_Definitions[key.ToUpper()].ToUpper();
                else
                    return initVal.ToUpper();
            }

            public string Get_Digital_Definition(string key)
            {
                if (Digital_Definitions.ContainsKey(key.ToUpper()))
                    return Digital_Definitions[key.ToUpper()].ToUpper();
                else
                {
                    MessageBox.Show("Warning: The Register definition for: " + key + " does not exist in OTP_Registers_Part_Specific found in Digital_Definitions_Part_Specific.xml", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }
            }

            public bool LoadVector(string nameInMemory, string fullPath)
            {
                return LoadVector(new List<string> { fullPath }, nameInMemory); // All the other LoadVector wrappers are obsolete with new MIPICommand syntax
            }

            public bool IsMipiChannel(string pinName)
            {
                bool isMipiCh = false;

                isMipiCh = PinNamesAndChans.ContainsKey(pinName);
                if (EqHSDIO.isVioTxPpmu & pinName.ToUpper().Contains("VIO"))
                    isMipiCh = false;

                return isMipiCh;
            }
        }
    }

    public static class MipiSyntaxParser
    {
        public static bool isnull = true;
        private static List<ClsMIPIFrame> CheckMipiDic;

        public static List<ClsMIPIFrame> CreateListOfMipiFrames(string mipiCode)
        {
            List<ClsMIPIFrame> _mipiFramesToSend = new List<ClsMIPIFrame>();

            if (string.IsNullOrWhiteSpace(mipiCode)) return _mipiFramesToSend;

            Regex expr = new Regex(@"(.*?)\((.*?)\)");

            var matches = expr.Matches(mipiCode);

            string currentSlaveAddress = "";
            int pair = 0;
            int Count = 0;

            bool Flag = false;

            if (CheckMipiDic == null)
            {
                CheckMipiDic = new List<ClsMIPIFrame>(); Flag = true;
            }
            else if (CheckMipiDic.Count != matches.Count)
            {
                CheckMipiDic = new List<ClsMIPIFrame>(); Flag = true;
            }

            foreach (Match match in matches)
            {
                string mipiFrame_found = match.Groups[0].Value;
                string slaveAddress_found = match.Groups[1].Value;
                string mipiFrameContents_found = match.Groups[2].Value;

                slaveAddress_found = ConvertAllValueTypesToHex(slaveAddress_found);

                if (!string.IsNullOrEmpty(slaveAddress_found))
                {
                    currentSlaveAddress = slaveAddress_found;
                    pair++;
                }

                ClsMIPIFrame stdMipiFrame = ParseStdMipiFrame(mipiFrameContents_found, currentSlaveAddress, Count, pair);

                if (stdMipiFrame != null)
                {
                    _mipiFramesToSend.Add(stdMipiFrame);

                    if (Flag)
                    {
                        _mipiFramesToSend[Count].Duplication = false;

                        CheckMipiDic.Add(stdMipiFrame);
                    }
                    else
                    {
                        bool Check = Checkduplication(stdMipiFrame, CheckMipiDic[Count]);
                        if (Check)
                        {
                            _mipiFramesToSend[Count].Duplication = true;
                            CheckMipiDic[Count] = stdMipiFrame;
                        }
                        else
                        {
                            _mipiFramesToSend[Count].Duplication = false;
                            CheckMipiDic[Count] = stdMipiFrame;
                        }
                    }

                    Count++;
                    continue;
                }

                ClsMIPIFrame delayFrame = ParseDelayFrame(mipiFrameContents_found);

                if (delayFrame != null)
                {
                    _mipiFramesToSend.Add(delayFrame);
                    if (isnull) CheckMipiDic.Add(stdMipiFrame);

                    continue;
                }
            }

            if (Flag) Flag = false;
            return _mipiFramesToSend;
        }

        public class ClsMIPIFrame
        {
            public int Pair, Data_Target;
            public string SlaveAddress_hex, Register_hex, Data_hex, EFUSE_Register_hex;
            public readonly int Delay_ms;
            public bool Duplication;
            public bool IsMaskedWrite;
            private string v1;
            private bool v2;

            public bool IsValidFrame
            {
                get
                {
                    long output;
                    bool valid = true;

                    valid &= long.TryParse(SlaveAddress_hex, System.Globalization.NumberStyles.HexNumber, null, out output);
                    valid &= long.TryParse(Register_hex, System.Globalization.NumberStyles.HexNumber, null, out output);
                    valid &= long.TryParse(Data_hex, System.Globalization.NumberStyles.HexNumber, null, out output);

                    return valid;
                }
            }

            public ClsMIPIFrame(string SlaveAddress_hex, string Register_hex, string Data_hex, bool Duplication, int Pair = 0, bool IsMaskedWrite = false)
            {
                this.SlaveAddress_hex = SlaveAddress_hex;
                this.Register_hex = Register_hex;
                this.Data_hex = Data_hex;
                this.Duplication = Duplication;
                this.Pair = Pair;
                this.IsMaskedWrite = IsMaskedWrite;
            }

            /// <summary>
            /// Clone base frame, Jay 2021.05.07
            /// </summary>
            /// <param name="frame">original frame</param>
            public ClsMIPIFrame(ClsMIPIFrame frame)
                : this(frame.SlaveAddress_hex, frame.Register_hex, frame.Data_hex, frame.Duplication, frame.Pair, frame.IsMaskedWrite)
            {
                this.Data_Target = frame.Data_Target;
                this.EFUSE_Register_hex = frame.EFUSE_Register_hex;
            }

            public ClsMIPIFrame()
            {
            }

            public ClsMIPIFrame(int Delay_ms)
                : this()
            {
                this.Delay_ms = Delay_ms;
            }

            public ClsMIPIFrame(string slaveAddress_hex, string v1, bool v2, string data_hex, int pair)
            {
                SlaveAddress_hex = slaveAddress_hex;
                this.v1 = v1;
                this.v2 = v2;
                Register_hex = v1;
                Data_hex = data_hex;
                Pair = pair;
            }
        }

        private static ClsMIPIFrame ParseStdMipiFrame(string mipiFrameContents_found, string currentSlaveAddress, int Count, int Pair = 0)
        {
            if (string.IsNullOrEmpty(currentSlaveAddress))
            {
                return null;
            }

            Regex exprStdMipiFrame = new Regex(@"(.*),(.*)");

            Match matchStdMipiFrame = exprStdMipiFrame.Match(mipiFrameContents_found);

            if (matchStdMipiFrame.Success)
            {
                string register_found = matchStdMipiFrame.Groups[1].Value;
                string data_found = matchStdMipiFrame.Groups[2].Value;
                bool IsMaskedwrite = false;

                if (register_found.Contains("@"))
                {
                    IsMaskedwrite = true;
                    register_found = register_found.Replace("@", "");
                }

                register_found = ConvertAllValueTypesToHex(register_found);
                data_found = ConvertAllValueTypesToHex(data_found);

                return new ClsMIPIFrame(currentSlaveAddress, register_found, data_found, true, Pair, IsMaskedwrite);
            }
            else
            {
                return null;
            }
        }

        private static bool Checkduplication(ClsMIPIFrame Before, ClsMIPIFrame Current)
        {
            if (Before.Data_hex != Current.Data_hex) return false;
            if (Before.Delay_ms != Current.Delay_ms) return false;
            if (Before.IsMaskedWrite != Current.IsMaskedWrite) return false;
            if (Before.IsValidFrame != Current.IsValidFrame) return false;
            if (Before.Pair != Current.Pair) return false;
            if (Before.Register_hex != Current.Register_hex) return false;
            if (Before.SlaveAddress_hex != Current.SlaveAddress_hex) return false;

            return true;
        }

        private static ClsMIPIFrame ParseDelayFrame(string mipiFrameContents_found)
        {
            Regex exprDelay = new Regex(@"\+\s*(\d+)");

            Match matchDelay = exprDelay.Match(mipiFrameContents_found);

            if (matchDelay.Success)
            {
                int delay_found = Convert.ToInt32(matchDelay.Groups[1].Value);
                return new ClsMIPIFrame(delay_found);
            }
            else
            {
                return null;
            }
        }

        private static string ConvertAllValueTypesToHex(string val)
        {
            string val_hex;

            if (string.IsNullOrWhiteSpace(val))
            {
                return "";
            }
            else if (BeginsWithHexPrefix(val))
            {
                val_hex = TrimHex(val);
            }
            else if (BeginsWithBinaryPrefix(val))
            {
                val_hex = BinaryToHex(val);
            }
            else
            {
                val_hex = DecimalToHex(val);
            }

            return val_hex.ToUpper();
        }

        private static bool BeginsWithHexPrefix(string val)
        {
            return val.Trim().ToUpper().StartsWith("0X");
        }

        private static bool BeginsWithBinaryPrefix(string val)
        {
            return val.Trim().ToUpper().StartsWith("0B");
        }

        private static string TrimHex(string val_hex)
        {
            var val_hex_trimmed = val_hex.ToUpper().Replace("0X", "").Trim();

            long val_dec;
            bool isHex = long.TryParse(val_hex_trimmed, System.Globalization.NumberStyles.HexNumber, null, out val_dec);

            if (isHex)
            {
                return val_hex_trimmed;
            }
            else
            {
                return val_hex;
            }
        }

        private static string BinaryToHex(string val_bin)
        {
            try
            {
                var val_bin_trimmed = val_bin.ToUpper().Replace("0B", "").Trim();

                return Convert.ToInt32(val_bin_trimmed, 2).ToString("X");
            }
            catch
            {
                return val_bin;
            }
        }

        private static string DecimalToHex(string val_dec)
        {
            int val_dec_int = -1;

            if (int.TryParse(val_dec, out val_dec_int))
            {
                return Convert.ToString(val_dec_int, 16);
            }
            else
            {
                return val_dec;
            }
        }

        /// <summary>
        /// Only a-e are valid for MIPI hex conversion.
        /// </summary>
        private static Dictionary<string, string> m_hexConversion =
            new Dictionary<string, string>
            {
                { "10", "A" },
                { "11", "B" },
                { "12", "C" },
                { "13", "D" },
                { "14", "E" },
                { "A", "A" },
                { "B", "B" },
                { "C", "C" },
                { "D", "D" },
                { "E", "E" },
                { "a", "A" },
                { "b", "B" },
                { "c", "C" },
                { "d", "D" },
                { "e", "E" },
            };
    }
}