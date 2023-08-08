using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Windows.Forms;
using NationalInstruments;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using System.Threading;
using System.Threading.Tasks;
//using LabVIEWFilters;
using LabVIEWFiltersWrapper;
using ClothoSharedItems;

namespace LibEqmtDriver.NF_VST
{
    public class NF_NiPXI_VST
    {
        /// <summary>This class defines the set of inputs used by the test.</summary>/
        public class Config
        {
            public int NumberOfRuns;
            public string Band;
            public string Modulation;
            public string WaveformName;
            public double TXFrequencyStart;
            public double TXFrequencyStop;
            public double TXFrequencyStep;
            public double DwellTime;
            public double RXFrequencyStart;
            public double RXFrequencyStop;
            public double RXFrequencyStep;
            public double SGPowerLevel;
            public double SAReferenceLevel;
            public double SoakTime;
            public double SoakFrequency;
            public double Rbw;
            public double Vbw;
            public bool preSoakSweep;
            public double multiplier_RXIQRate;
            public double[] Bandwidths;

            public Config(int NumberOfRuns, string Band, string Modulation, string WaveformName, double TXFrequencyStart, double TXFrequencyStop, double TXFrequencyStep,
                double DwellTime, double RXFrequencyStart, double RXFrequencyStop, double RXFrequencyStep, double SGPowerLevel, double SAReferenceLevel, double SoakTime, double SoakFrequency,
                double Rbw, double Vbw, bool preSoakSweep, double multiplier_RXIQRate, double[] Bandwidths)
            {
                this.NumberOfRuns = NumberOfRuns;
                this.Band = Band;
                this.Modulation = Modulation;
                this.WaveformName = WaveformName;
                this.TXFrequencyStart = TXFrequencyStart;
                this.TXFrequencyStop = TXFrequencyStop;
                this.TXFrequencyStep = TXFrequencyStep;
                this.DwellTime = DwellTime;
                this.RXFrequencyStart = RXFrequencyStart;
                this.RXFrequencyStop = RXFrequencyStop;
                this.RXFrequencyStep = RXFrequencyStep;
                this.SGPowerLevel = SGPowerLevel;
                this.SAReferenceLevel = SAReferenceLevel;
                this.SoakTime = SoakTime;
                this.SoakFrequency = SoakFrequency;
                this.Rbw = Rbw;
                this.Vbw = Vbw;
                this.preSoakSweep = preSoakSweep;
                this.multiplier_RXIQRate = multiplier_RXIQRate;
                this.Bandwidths = Bandwidths;
            }
        }

        #region Class Variables

        public string VisaAlias { get; set; }
        public string SerialNumber { get; set; }
        public string ChanNumber { get; set; }
        public string PinName { get; set; }
        public byte Site { get; set; }
        public bool Simulated { get => ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE); }
        public string OptionString { get ; set; }
        public string ModelNumber { get; set; }


        // constructor //
        private string IOAddress;
        // Initialize() //
        public NIRfsa rfsaSession;
        public NIRfsg rfsgSession;
        // PreConfigureVST() //
        private ManualResetEvent PreConfig_VSTSGFlag = new ManualResetEvent(false);
        private ManualResetEvent PreConfig_LoadLVFiltersFlag = new ManualResetEvent(false);
        // PreConfigureVSTSG() //
        // PreConfigureVSTSA() //
        // ConfigureVSTDuringTest() //
        private int NumberOfRuns;
        private string Band;
        private string Modulation;
        private string WaveformName;
        private double DwellTime;
        private double SGPowerLevel;
        private double SAReferenceLevel;
        private double SoakTime;
        private double SoakFrequency;
        private double Rbw;
        private double Vbw;
        private bool preSoakSweep;
        private double multiplier_RXIQRate;
        private double[] Bandwidths;

        private ManualResetEvent Config_VSTSGFlag = new ManualResetEvent(false);
        // Measure_VST() //
        private ManualResetEvent ConsumerFlag = new ManualResetEvent(false);
        // ConfigureVSTDuringTest() //
        List<double> TXRampFrequencyList = new List<double>();
        List<double> RXRampFrequencyList = new List<double>();
        List<double> RampFrequencyList = new List<double>();
        int TXFrequencyRange;
        int RXFrequencyRange;
        double MiddleFrequency;
        int NumberOfRecords;
        int NumberOfTracePoints;
        // Configure_VSTSA() //
        double RX_IQRate;
        // Configure_VSTSG() //
        double SG_IQRate;
        //  Producer() //
        NationalInstruments.PrecisionTimeSpan timeout = new NationalInstruments.PrecisionTimeSpan(5.0);
        // Measure_VST() //
        int RX_NumberOfSamples;
        BlockingCollection<NationalInstruments.ComplexDouble[]> dataQueue;
        NationalInstruments.ComplexDouble[] iqData;
        //  ConfigureRF()     //
        public double IQRate;           //added by Shaz 14/03/2018 to handle VST5644 and VST5646
        // Producer() //
        // Consumer() //

        #endregion

        //double[,] ResultsVBWRawTrace;
        double[,] ResultsTrace;
        double[] ResultsMaxHoldTrace;
        double[,] MultiTraceData;

        public S_MultiRBW_Data[] MultiRBW_Data;
        public S_MutSignal_Setting MutSignal_Setting;

        public ManualResetEvent DoneVSGinit = new ManualResetEvent(false); //Seoul

        /// <summary> Constructs the NF_NiPXI_VST object. </summary>
        /// <param name="IOAddress">Specifies the VST alias name.</param>
        public NF_NiPXI_VST(string IOAddress)
        {
            this.IOAddress = IOAddress;
        }
        public NF_NiPXI_VST()
        {
          
        }
        /// <summary>Initializes VST SA and SG instrument sessions.</summary>
        public void Initialize()
        {
            ModelNumber = "5646R";
            OptionString = Simulated ? $"Simulate=1, DriverSetup=Bitfile:NI-RFIC.lvbitx;Model:{ModelNumber};" : "DriverSetup=Bitfile:NI-RFIC.lvbitx";

            IntPtr niRfsaHandle = NF_NI_RFmx.InitializeInstr(IOAddress, OptionString);
            rfsaSession = new NIRfsa(niRfsaHandle); // Initialize the NIRfsa session
            //rfsgSession = new NIRfsg(IOAddress, true, false, "DriverSetup=Bitfile:NI Power Servoing for VST.lvbitx"); // Initialize the NIRfsg session
            rfsgSession = new NIRfsg(IOAddress, true, false, OptionString); // Initialize the NIRfsg session
            //Read waveforms of all modulations from file
            NF_VSTDriver.SignalType = new s_SignalType[Enum.GetNames(typeof(VST_WAVEFORM_MODE)).Length];

            ////////Original
            //rfsaSession = new NIRfsa(IOAddress, true, false); // Initialize the NIRfsa session
            //rfsgSession = new NIRfsg(IOAddress, true, false); // Initialize the NIRfsg session
            ////Read waveforms of all modulations from file
            //NF_VSTDriver.SignalType = new s_SignalType[Enum.GetNames(typeof(VST_WAVEFORM_MODE)).Length];

        }
        public void Initialize_NI5644R()
        {
            ModelNumber = "5644R";
            OptionString = Simulated ? $"Simulate=1, DriverSetup=Bitfile:NI Power Servoing for VST.lvbitx;Model:{ModelNumber};" : "DriverSetup=Bitfile:NI Power Servoing for VST.lvbitx";

            IntPtr niRfsaHandle = NF_NI_RFmx.InitializeInstr_NI5644R(IOAddress, OptionString);

            rfsaSession = new NIRfsa(niRfsaHandle); // Initialize the NIRfsa session
            rfsgSession = new NIRfsg(IOAddress, true, false, OptionString); // Initialize the NIRfsg session
            // rfsgSession = new NIRfsg(IOAddress, true, false, "DriverSetup=Bitfile:NI-RFIC.lvbitx"); // Initialize the NIRfsg session
            //Read waveforms of all modulations from file
            NF_VSTDriver.SignalType = new s_SignalType[Enum.GetNames(typeof(VST_WAVEFORM_MODE)).Length];

            ////////Original
            //rfsaSession = new NIRfsa(IOAddress, true, false); // Initialize the NIRfsa session
            //rfsgSession = new NIRfsg(IOAddress, true, false); // Initialize the NIRfsg session
            ////Read waveforms of all modulations from file
            //NF_VSTDriver.SignalType = new s_SignalType[Enum.GetNames(typeof(VST_WAVEFORM_MODE)).Length];

        }

        public class SGdata 
        {
            public List<double> SG_Idata;
            public List<double> SG_Qdata;
            public List<double> SG_Vpeak_Array;
            public double SG_Vpeak_Max;
            public double SG_Vrms;
            public double SG_PAPR;
        }

        public void MOD_FORMAT_CHECK(string strWaveform, string strWaveformName, string strmutateCond, bool WaveformInitalLoad)
        {
            #region Mutate Signal Variable
            string org_SG_IPath = NF_VSTDriver.SG_Path + strWaveform + @"\I_" + strWaveformName + ".txt";
            string org_SG_QPath = NF_VSTDriver.SG_Path + strWaveform + @"\Q_" + strWaveformName + ".txt";
            string mut_SG_IPath = NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\I_" + strWaveformName + ".txt";
            string mut_SG_QPath = NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\Q_" + strWaveformName + ".txt";
            //check mutate signal folder
            if (!Directory.Exists(NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\"))
                Directory.CreateDirectory(NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\");

            double total_time_sec;
            double mod_time_sec;
            double mod_offset_sec;
            double freq_offset_hz;
            double f_off_delay_sec;

            SGdata cSGdata = new SGdata();

            //var SG_Idata = new List<double>();
            //var SG_Qdata = new List<double>();
            
            Decode_MutSignal_Setting(strmutateCond);    //decode mutate condition
            total_time_sec = MutSignal_Setting.total_time_sec;
            mod_time_sec = MutSignal_Setting.mod_time_sec;
            mod_offset_sec = MutSignal_Setting.mod_offset_sec;
            freq_offset_hz = MutSignal_Setting.freq_offset_hz;
            f_off_delay_sec = MutSignal_Setting.f_off_delay_sec;

            double papr_dB;
            #endregion

            VST_WAVEFORM_MODE ModulationType;
            ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), strWaveform.ToUpper());
            int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString());         //to get the int value from System.Enum

            NF_VSTDriver.SignalType[arrayNo].signalMode = ModulationType.ToString();

            switch (ModulationType)
            {
                case VST_WAVEFORM_MODE.CW:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 1e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2K:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2KRC1:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.GSM850:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM900:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1800:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1900:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM850A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM900A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1800A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1900A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.IS95A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMA:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMAUL:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMAGTC1:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M1RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M12RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M20RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M48RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M50RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE15M75RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 15e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5M25RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5M8RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M100RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M18RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M48RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE15MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 15e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2KCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMACUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.NCFUQSC15B5M25R0S3X:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ10M50R0SM6REF:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ5M25R0SM5REF:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ_10M25R25SM5_NTS3X_IV8_CFR4P2:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 46.08e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;

                default: throw new Exception("Not such a waveform!");
            }

            #region mutate signal generation

            if (NF_VSTDriver.SignalType[arrayNo].status)
            {
                if (NF_VSTDriver.SignalType[arrayNo].signalMode == "CW")
                {
                    NF_VSTDriver.SignalType[arrayNo].SG_papr_dB = 0;
                }
                else
                {
                    //Read original waveform
                    cSGdata.SG_Idata = NoiseTestFileUtilities.File_ReadData(org_SG_IPath);
                    cSGdata.SG_Qdata = NoiseTestFileUtilities.File_ReadData(org_SG_QPath);

                    int trimLength = (cSGdata.SG_Idata.Count() % 2);
                    if (trimLength > 0)
                    {
                        cSGdata.SG_Idata.RemoveAt(cSGdata.SG_Idata.Count() - 2);
                        cSGdata.SG_Qdata.RemoveAt(cSGdata.SG_Idata.Count() - 2);
                    }

                    // Read IQ data and calculate PAPR offset for given modulation
                    //Filters.PAPR(SG_Idata.ToArray(), SG_Qdata.ToArray(), out papr_dB);

                    CalculatePAPR(cSGdata, out papr_dB);

                    NF_VSTDriver.SignalType[arrayNo].SG_papr_dB = papr_dB;

                    //if (MutSignal_Setting.enable)
                    //{
                    //    NF_VSTDriver.SignalType[arrayNo].SG_IPath = mut_SG_IPath;
                    //    NF_VSTDriver.SignalType[arrayNo].SG_QPath = mut_SG_QPath;

                    //    var mutSG_Idata = new double[SG_Idata.Count];
                    //    var mutSG_Qdata = new double[SG_Qdata.Count];

                    //    Filters.MutateWaveform(SG_Idata.ToArray(), SG_Qdata.ToArray(), NF_VSTDriver.SignalType[arrayNo].SG_IQRate, total_time_sec, mod_time_sec, mod_offset_sec, freq_offset_hz, f_off_delay_sec, out mutSG_Idata, out mutSG_Qdata);

                    //    string[] tempIdata = Array.ConvertAll(mutSG_Idata, Convert.ToString);
                    //    System.IO.File.WriteAllLines(mut_SG_IPath, tempIdata);

                    //    string[] tempQdata = Array.ConvertAll(mutSG_Qdata, Convert.ToString);
                    //    System.IO.File.WriteAllLines(mut_SG_QPath, tempQdata);
                    //}
                    //else
                    //{
                    //    // Set the modulation file path to default if mutate signal no required
                    //    NF_VSTDriver.SignalType[arrayNo].SG_IPath = org_SG_IPath;
                    //    NF_VSTDriver.SignalType[arrayNo].SG_QPath = org_SG_QPath;
                    //}

                    // Set the modulation file path to default if mutate signal no required
                    NF_VSTDriver.SignalType[arrayNo].SG_IPath = org_SG_IPath;
                    NF_VSTDriver.SignalType[arrayNo].SG_QPath = org_SG_QPath;
                }
            }

            #endregion
        }

        public void MOD_FORMAT_CHECK2(string strWaveform, string strWaveformName, string strmutateCond, bool WaveformInitalLoad)
        {
            NF_VSTDriver.SignalType = new s_SignalType[Enum.GetNames(typeof(VST_WAVEFORM_MODE)).Length];
            Dictionary<string, string> Sig = new Dictionary<string, string>();


            #region Mutate Signal Variable
            string org_SG_IPath = NF_VSTDriver.SG_Path + strWaveform + @"\I_" + strWaveformName + ".txt";
            string org_SG_QPath = NF_VSTDriver.SG_Path + strWaveform + @"\Q_" + strWaveformName + ".txt";
            string mut_SG_IPath = NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\I_" + strWaveformName + ".txt";
            string mut_SG_QPath = NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\Q_" + strWaveformName + ".txt";
            //check mutate signal folder
            if (!Directory.Exists(NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\"))
                Directory.CreateDirectory(NF_VSTDriver.SG_Path + strWaveform + @"\MUTSIG\");

            double total_time_sec;
            double mod_time_sec;
            double mod_offset_sec;
            double freq_offset_hz;
            double f_off_delay_sec;

            SGdata cSGdata = new SGdata();

            //var SG_Idata = new List<double>();
            //var SG_Qdata = new List<double>();

            Decode_MutSignal_Setting(strmutateCond);    //decode mutate condition
            total_time_sec = MutSignal_Setting.total_time_sec;
            mod_time_sec = MutSignal_Setting.mod_time_sec;
            mod_offset_sec = MutSignal_Setting.mod_offset_sec;
            freq_offset_hz = MutSignal_Setting.freq_offset_hz;
            f_off_delay_sec = MutSignal_Setting.f_off_delay_sec;

            double papr_dB;
            #endregion

            VST_WAVEFORM_MODE ModulationType;
            ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), strWaveform.ToUpper());
            int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString());         //to get the int value from System.Enum

            NF_VSTDriver.SignalType[arrayNo].signalMode = ModulationType.ToString();

            switch (ModulationType)
            {
                case VST_WAVEFORM_MODE.CW:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 1e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2K:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2KRC1:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.GSM850:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM900:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1800:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1900:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM850A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM900A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1800A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.GSM1900A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    break;
                case VST_WAVEFORM_MODE.IS95A:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMA:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMAUL:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMAGTC1:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M1RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M12RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M20RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M48RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10M50RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE15M75RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 15e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5M25RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5M8RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M100RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M18RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20M48RB:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.LTE5MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LTE10MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LTE15MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 15e6;
                    break;
                case VST_WAVEFORM_MODE.LTE20MCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 30.72e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 20e6;
                    break;
                case VST_WAVEFORM_MODE.CDMA2KCUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 4.9152e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.WCDMACUSTOM:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.NCFUQSC15B5M25R0S3X:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 23.04e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ10M50R0SM6REF:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 15.36e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ5M25R0SM5REF:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 7.68e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 5e6;
                    break;
                case VST_WAVEFORM_MODE.LFUQ_10M25R25SM5_NTS3X_IV8_CFR4P2:
                    NF_VSTDriver.SignalType[arrayNo].status = true;
                    NF_VSTDriver.SignalType[arrayNo].SG_IQRate = 46.08e6;
                    NF_VSTDriver.SignalType[arrayNo].SignalBandwidth = 10e6;
                    break;

                default: throw new Exception("Not such a waveform!");
            }


            #region mutate signal generation

            if (NF_VSTDriver.SignalType[arrayNo].status)
            {
                if (NF_VSTDriver.SignalType[arrayNo].signalMode == "CW")
                {
                    NF_VSTDriver.SignalType[arrayNo].SG_papr_dB = 0;
                }
                else
                {
                    //Read original waveform
                    cSGdata.SG_Idata = NoiseTestFileUtilities.File_ReadData(org_SG_IPath);
                    cSGdata.SG_Qdata = NoiseTestFileUtilities.File_ReadData(org_SG_QPath);

                    int trimLength = (cSGdata.SG_Idata.Count() % 2);
                    if (trimLength > 0)
                    {
                        cSGdata.SG_Idata.RemoveAt(cSGdata.SG_Idata.Count() - 2);
                        cSGdata.SG_Qdata.RemoveAt(cSGdata.SG_Idata.Count() - 2);
                    }

                    // Read IQ data and calculate PAPR offset for given modulation
                    //Filters.PAPR(SG_Idata.ToArray(), SG_Qdata.ToArray(), out papr_dB);

                    CalculatePAPR(cSGdata, out papr_dB);

                    NF_VSTDriver.SignalType[arrayNo].SG_papr_dB = papr_dB;



                    //if (MutSignal_Setting.enable)
                    //{
                    //    NF_VSTDriver.SignalType[arrayNo].SG_IPath = mut_SG_IPath;
                    //    NF_VSTDriver.SignalType[arrayNo].SG_QPath = mut_SG_QPath;

                    //    var mutSG_Idata = new double[SG_Idata.Count];
                    //    var mutSG_Qdata = new double[SG_Qdata.Count];

                    //    Filters.MutateWaveform(SG_Idata.ToArray(), SG_Qdata.ToArray(), NF_VSTDriver.SignalType[arrayNo].SG_IQRate, total_time_sec, mod_time_sec, mod_offset_sec, freq_offset_hz, f_off_delay_sec, out mutSG_Idata, out mutSG_Qdata);

                    //    string[] tempIdata = Array.ConvertAll(mutSG_Idata, Convert.ToString);
                    //    System.IO.File.WriteAllLines(mut_SG_IPath, tempIdata);

                    //    string[] tempQdata = Array.ConvertAll(mutSG_Qdata, Convert.ToString);
                    //    System.IO.File.WriteAllLines(mut_SG_QPath, tempQdata);
                    //}
                    //else
                    //{
                    //    // Set the modulation file path to default if mutate signal no required
                    //    NF_VSTDriver.SignalType[arrayNo].SG_IPath = org_SG_IPath;
                    //    NF_VSTDriver.SignalType[arrayNo].SG_QPath = org_SG_QPath;
                    //}

                    // Set the modulation file path to default if mutate signal no required
                    NF_VSTDriver.SignalType[arrayNo].SG_IPath = org_SG_IPath;
                    NF_VSTDriver.SignalType[arrayNo].SG_QPath = org_SG_QPath;
                }
            }

            #endregion
        }
        public void CalculatePAPR(SGdata cSGdata, out double papr_dB)
        {
            papr_dB = 0.0;

            try
            {
                cSGdata.SG_Vrms = 0.0;
                cSGdata.SG_Vpeak_Array = new List<double>();

                if (cSGdata.SG_Idata.Count != cSGdata.SG_Qdata.Count)
                    throw new Exception();

                for (int i = 0; i < cSGdata.SG_Idata.Count; i++)
                {
                    cSGdata.SG_Vpeak_Array.Add(Math.Sqrt(Math.Pow(cSGdata.SG_Idata[i], 2) + Math.Pow(cSGdata.SG_Qdata[i], 2)));
                    cSGdata.SG_Vrms = cSGdata.SG_Vrms + Math.Pow(cSGdata.SG_Vpeak_Array[i], 2);
                }

                cSGdata.SG_Vpeak_Max = cSGdata.SG_Vpeak_Array.Max();
                cSGdata.SG_Vrms = Math.Sqrt(cSGdata.SG_Vrms / cSGdata.SG_Vpeak_Array.Count);
                cSGdata.SG_PAPR = 10 * Math.Log10(Math.Pow(Math.Abs(cSGdata.SG_Vpeak_Max), 2) / Math.Pow(cSGdata.SG_Vrms,2));
                papr_dB = cSGdata.SG_PAPR;
                return;
            }
            catch (Exception Ex)
            {

            }
        }


        //Get the IORate from 'struct s_SignalType'
        public void Get_s_SignalType(string strWaveform, string strWaveformName, out double SG_IQRate)
        {
            SG_IQRate = 1e6;        //set to default CW Rate

            VST_WAVEFORM_MODE ModulationType;
            ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), strWaveformName.ToUpper());
            int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString());         //to get the int value from System.Enum

            SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
        }

        public void Get_SignalBandwidth_fromModulation(string strWaveform, string strWaveformName, out double SignalBandwidth)
        {
            VST_WAVEFORM_MODE ModulationType;
            ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), strWaveformName.ToUpper());
            int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString());         //to get the int value from System.Enum

            SignalBandwidth = NF_VSTDriver.SignalType[arrayNo].SignalBandwidth;
        }

        /// <summary>Pre-configures VST SA and SG.</summary>
        public void PreConfigureVST()
        {
            try
            {
                // reset the flags //
                PreConfig_VSTSGFlag.Reset();
                PreConfig_LoadLVFiltersFlag.Reset();
                // queue the work //
                ThreadPool.QueueUserWorkItem(PreConfig_VSTSG);
                //ThreadPool.QueueUserWorkItem(PreConfig_LoadLVFilters);
                // configure the VSTSA in this thread //
                PreConfig_VSTSA();
                // wait for spawned work to finish //
                PreConfig_VSTSGFlag.WaitOne();
                //PreConfig_LoadLVFiltersFlag.WaitOne();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in PreConfigureVST():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        private void PreConfig_VSTSG(Object o)
        {
            // Configure the reference clock source 
            rfsgSession.FrequencyReference.Configure(NF_VSTDriver.ReferenceClockSource, 10E6);
            rfsgSession.Arb.GenerationMode = RfsgWaveformGenerationMode.Script;
            rfsgSession.RF.PowerLevelType = RfsgRFPowerLevelType.PeakPower;
            // Configure the loop bandwidth 
            rfsgSession.RF.Advanced.LoopBandwidth = NF_VSTDriver.loopBandwidth;
            rfsgSession.Arb.PreFilterGain = NF_VSTDriver.ArbPreFilterGain;
            //Read waveforms of all modulations from file
            if (NF_VSTDriver.UseWaveformFile == true)
            {
                for (int i = 0; i < NF_VSTDriver.SignalType.Length; i++)
                {
                    if (NF_VSTDriver.SignalType[i].status)
                    {
                        if (NF_VSTDriver.SignalType[i].signalMode == "CW")
                        {
                            var SG_Idata = new List<double>();
                            var SG_Qdata = new List<double>();

                            //Generate CW waveform
                            for (int x = 0; x < 10000; x++)
                            {
                                SG_Idata.Add(1);
                                SG_Qdata.Add(0);
                            }

                            NF_VSTDriver.SignalType[i].signalLength = SG_Idata.Count();
                            // Write SIGNAL
                            rfsgSession.Arb.WriteWaveform("Signal" + NF_VSTDriver.SignalType[i].signalMode, SG_Idata.ToArray(), SG_Qdata.ToArray());
                            // Write IDLE
                            rfsgSession.Arb.WriteWaveform("Idle" + NF_VSTDriver.SignalType[i].signalMode, SG_Idata.ToArray(), SG_Qdata.ToArray());
                        }
                        else
                        {
                            var SG_Idata = new List<double>();
                            var SG_Qdata = new List<double>();
                            //var mutSG_Idata = new double[NF_VSTDriver.SignalType.Length];
                            //var mutSG_Qdata = new double[NF_VSTDriver.SignalType.Length];
                            //Read entire file
                            SG_Idata = NoiseTestFileUtilities.File_ReadData(NF_VSTDriver.SignalType[i].SG_IPath);
                            SG_Qdata = NoiseTestFileUtilities.File_ReadData(NF_VSTDriver.SignalType[i].SG_QPath);

                            int trimLength = (SG_Idata.Count() % 2);
                            if (trimLength > 0)
                            {
                                SG_Idata.RemoveAt(SG_Idata.Count() - 2);
                                SG_Qdata.RemoveAt(SG_Idata.Count() - 2);
                            }

                            double[] idleArrayI = new double[SG_Idata.Count()];
                            double[] idleArrayQ = new double[SG_Idata.Count()];
                            for (int j = 0; j < SG_Idata.Count(); j++)
                            { idleArrayI[j] = 1; }

                            NF_VSTDriver.SignalType[i].signalLength = SG_Idata.Count();

                            string TempSignalMode = NF_VSTDriver.SignalType[i].signalMode;

                            if (NF_VSTDriver.SignalType[i].signalMode.Contains("_"))
                                TempSignalMode = NF_VSTDriver.SignalType[i].signalMode.Replace("_", "");

                            // Write SIGNAL
                            rfsgSession.Arb.WriteWaveform("Signal" + TempSignalMode, SG_Idata.ToArray(), SG_Qdata.ToArray());
                            // Write IDLE
                            rfsgSession.Arb.WriteWaveform("Idle" + TempSignalMode, SG_Idata.ToArray(), SG_Qdata.ToArray());

                            //Filters.MutateWaveform(SG_Idata.ToArray(), SG_Qdata.ToArray(), NF_VSTDriver.SignalType[i].SG_IQRate, DwellTime, DwellTime / 2, DwellTime * 0.6, 1e6, out mutSG_Idata, out mutSG_Qdata);
                            //// Write SIGNAL
                            //rfsgSession.Arb.WriteWaveform("Signal" + NF_VSTDriver.SignalType[i].signalMode, mutSG_Idata, mutSG_Qdata);
                            //// Write IDLE
                            //rfsgSession.Arb.WriteWaveform("Idle" + NF_VSTDriver.SignalType[i].signalMode, mutSG_Idata, mutSG_Qdata);
                        }
                    }
                }
            }
            //***Configure Trigger For List Mode Ping Pong***
            // Export the marker event to the desired output terminal 
            rfsgSession.DeviceEvents.MarkerEvents[2].ExportedOutputTerminal = NF_VSTDriver.RFSG_MarkerEvents_2_ExportedOutputTerminal;
            rfsgSession.DeviceEvents.MarkerEvents[1].ExportedOutputTerminal = NF_VSTDriver.RFSG_MarkerEvents_1_ExportedOutputTerminal;
            // Configure the trigger type to advance steps in the list 
            rfsgSession.Triggers.ConfigurationListStepTrigger.TriggerType = RfsgConfigurationListStepTriggerType.DigitalEdge;
            // Configure the trigger source to advance steps in the list 
            rfsgSession.Triggers.ConfigurationListStepTrigger.DigitalEdge.Source = RfsgDigitalEdgeConfigurationListStepTriggerSource.Marker0Event;
            rfsgSession.Triggers.ConfigurationListStepTrigger.ExportedOutputTerminal = NF_VSTDriver.RFSG_ConfigurationListStepTrigger_ExportedOutputTerminal;
            rfsgSession.Utility.Commit();
            PreConfig_VSTSGFlag.Set(); // set event to signal completion of this method
        }

        public void PreConfig_VSTSA(RfsaAcquisitionType rfsaAcquisition = RfsaAcquisitionType.IQ) //Seoul
        //private void PreConfig_VSTSA() //Original
        {
            rfsaSession.Configuration.ReferenceClock.Configure(NF_VSTDriver.ReferenceClockSource, 10e6);
            rfsaSession.Configuration.AcquisitionType = rfsaAcquisition;

     
            rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
            rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);
            rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;

            SA_ReferenceTrigger = true;
            SA_StartTrigger = true;
            SA_AdvanceTrigger = true;


            rfsaSession.Configuration.BasicConfigurationList.TimerEventInterval = NF_VSTDriver.TimerEventInterval;
            rfsaSession.Utility.Commit();
        }


        public void PreConfig_VSTSA_NF(RfsaAcquisitionType rfsaAcquisition = RfsaAcquisitionType.IQ) //Seoul
        //private void PreConfig_VSTSA() //Original
        {
         //   rfsaSession.Configuration.ReferenceClock.Configure(NF_VSTDriver.ReferenceClockSource, 10e6);
         //   rfsaSession.Configuration.AcquisitionType = rfsaAcquisition;
            //rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
            //rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);
            //rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;
          //  rfsaSession.Configuration.BasicConfigurationList.TimerEventInterval = NF_VSTDriver.TimerEventInterval;
        //    rfsaSession.Utility.Commit();
        }


        /// <summary>This method loads LabVIEW into memory. This method should be called during initialization.</summary>
        private void PreConfig_LoadLVFilters(Object o)
        {
            Filters.LoadInterop();
            PreConfig_LoadLVFiltersFlag.Set();
        }

        /// <summary>Configures VST SA and SG according to input test param.</summary>
        /// <param name="config">Specifies test inputs.</param>
        public void ConfigureVSTDuringTest(Config config)
        {
            //Stopwatch cfgTimer = new Stopwatch();
            //cfgTimer.Reset();
            //cfgTimer.Start();
            try
            {
                // set inputs fed from "config" object ->
                this.NumberOfRuns = config.NumberOfRuns;
                this.Band = config.Band;
                this.Modulation = config.Modulation;
                this.WaveformName = config.WaveformName;
                this.DwellTime = config.DwellTime;
                this.SGPowerLevel = config.SGPowerLevel;
                this.SAReferenceLevel = config.SAReferenceLevel;
                this.SoakTime = config.SoakTime;
                this.SoakFrequency = config.SoakFrequency;
                this.Rbw = config.Rbw;
                this.Vbw = config.Vbw;
                this.preSoakSweep = config.preSoakSweep;
                this.multiplier_RXIQRate = config.multiplier_RXIQRate;
                this.Bandwidths = config.Bandwidths;
                // end set inputs from "config" object

                TXFrequencyRange = (int)(config.TXFrequencyStop - config.TXFrequencyStart);
                RXFrequencyRange = (int)(config.RXFrequencyStop - config.RXFrequencyStart);
                //Get frequency range for TX and RX bands

                config.TXFrequencyStart = config.TXFrequencyStart + (config.TXFrequencyStep / 2);
                config.TXFrequencyStop = config.TXFrequencyStop - (config.TXFrequencyStep / 2);
                config.RXFrequencyStart = config.RXFrequencyStart + (config.TXFrequencyStep / 2);
                config.RXFrequencyStop = config.RXFrequencyStop - (config.TXFrequencyStep / 2);



                if (TXFrequencyRange != RXFrequencyRange)
                {
                    FrequencyRampFixedTX(config.TXFrequencyStart, config.TXFrequencyStop, config.RXFrequencyStart, config.RXFrequencyStop, config.TXFrequencyStep, out NumberOfRecords, TXRampFrequencyList, RXRampFrequencyList);//Go to FrequencyRampFixedTX function if TXFreq range is not equal to RXFreq range
                    double RX_FrequencyOffsetOut = config.RXFrequencyStart - config.TXFrequencyStart;
                    int MiddleFrequency_Index = (int)(NumberOfRecords / 2); //If NumberofRecords is an odd number, the MiddleFreqeuncy_Index will be truncated towards zero.
                    MiddleFrequency = TXRampFrequencyList[MiddleFrequency_Index]; //use TXRampFrequencyList if TXFreqRange is not equal to RXFreqRange

                    Config_VSTSGFlag.Reset();
                    ThreadPool.QueueUserWorkItem(Configure_VSTSG);
                    Configure_VSTSA(RX_FrequencyOffsetOut);
                    Config_VSTSGFlag.WaitOne();

                    NumberOfTracePoints = (int)((config.RXFrequencyStop - config.RXFrequencyStart) / 100e3) / (NumberOfRecords - 1); //use RXFreqRange to determine NumberofTracePoints
                }
                else
                {
                    FrequencyRamp(config.TXFrequencyStart, config.TXFrequencyStop, config.TXFrequencyStep, out NumberOfRecords, RampFrequencyList);
                    double RX_FrequencyOffsetOut = config.RXFrequencyStart - config.TXFrequencyStart;
                    int MiddleFrequency_Index = (int)(NumberOfRecords / 2); //If NumberofRecords is an odd number, the MiddleFreqeuncy_Index will be truncated towards zero.
                    MiddleFrequency = RampFrequencyList[MiddleFrequency_Index];

                    Config_VSTSGFlag.Reset();
                    ThreadPool.QueueUserWorkItem(Configure_VSTSG);
                    Configure_VSTSA(RX_FrequencyOffsetOut);
                    Config_VSTSGFlag.WaitOne();

                    NumberOfTracePoints = (int)((config.TXFrequencyStop - config.TXFrequencyStart) / 100e3) / (NumberOfRecords - 1);
                }

                //cfgTimer.Stop();
                //string temp = "1. SA + SG Configure during test (mS) : " + cfgTimer.ElapsedMilliseconds.ToString();
                //File.AppendAllText(logPath +@"\TestTime_Clotho_" + ticktime.ToString() + ".txt", temp + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in ConfigureVSTDuringTest():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        public void ConfigureVSTDuringTest_FixedTX(Config config)
        {
            //Stopwatch cfgTimer = new Stopwatch();
            //cfgTimer.Reset();
            //cfgTimer.Start();
            try
            {
                // set inputs fed from "config" object ->
                this.NumberOfRuns = config.NumberOfRuns;
                this.Band = config.Band;
                this.Modulation = config.Modulation;
                this.WaveformName = config.WaveformName;
                this.DwellTime = config.DwellTime;
                this.SGPowerLevel = config.SGPowerLevel;
                this.SAReferenceLevel = config.SAReferenceLevel;
                this.SoakTime = config.SoakTime;
                this.SoakFrequency = config.SoakFrequency;
                this.Rbw = config.Rbw;
                this.Vbw = config.Vbw;
                this.preSoakSweep = config.preSoakSweep;
                this.multiplier_RXIQRate = config.multiplier_RXIQRate;
                this.Bandwidths = config.Bandwidths;
                // end set inputs from "config" object

                TXFrequencyRange = (int)(config.TXFrequencyStop - config.TXFrequencyStart);
                RXFrequencyRange = (int)(config.RXFrequencyStop - config.RXFrequencyStart);
                //Get frequency range for TX and RX bands

                config.TXFrequencyStart = config.TXFrequencyStart + (config.TXFrequencyStep / 2);
                config.TXFrequencyStop = config.TXFrequencyStop - (config.TXFrequencyStep / 2);
                config.RXFrequencyStart = config.RXFrequencyStart + (config.TXFrequencyStep / 2);
                config.RXFrequencyStop = config.RXFrequencyStop - (config.TXFrequencyStep / 2);

                FrequencyRampFixedTX(config.TXFrequencyStart, config.TXFrequencyStop, config.RXFrequencyStart, config.RXFrequencyStop, config.TXFrequencyStep, out NumberOfRecords, TXRampFrequencyList, RXRampFrequencyList);//Go to FrequencyRampFixedTX function if TXFreq range is not equal to RXFreq range
                double RX_FrequencyOffsetOut = config.RXFrequencyStart - config.TXFrequencyStart;
                int MiddleFrequency_Index = (int)(NumberOfRecords / 2); //If NumberofRecords is an odd number, the MiddleFreqeuncy_Index will be truncated towards zero.
                MiddleFrequency = TXRampFrequencyList[MiddleFrequency_Index]; //use TXRampFrequencyList if TXFreqRange is not equal to RXFreqRange

                Config_VSTSGFlag.Reset();
                ThreadPool.QueueUserWorkItem(Configure_VSTSG_FixedTX);
                Configure_VSTSA_FixedTX(RX_FrequencyOffsetOut);
                Config_VSTSGFlag.WaitOne();

                NumberOfTracePoints = (int)((config.RXFrequencyStop - config.RXFrequencyStart) / 100e3) / (NumberOfRecords - 1); //use RXFreqRange to determine NumberofTracePoints

                //cfgTimer.Stop();
                //string temp = "1. SA + SG Configure during test (mS) : " + cfgTimer.ElapsedMilliseconds.ToString();
                //File.AppendAllText(logPath +@"\TestTime_Clotho_" + ticktime.ToString() + ".txt", temp + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in ConfigureVSTDuringTest():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        private void Configure_VSTSA(double RX_FrequencyOffsetOut)
        {
            if (this.multiplier_RXIQRate <= 0)
            {
                this.multiplier_RXIQRate = 1.25; //force this variable to default constant
            }
            try
            {
                RX_IQRate = this.multiplier_RXIQRate * this.Rbw;

                rfsaSession.Configuration.Vertical.ReferenceLevel = this.SAReferenceLevel;
                rfsaSession.Configuration.IQ.IQRate = RX_IQRate;
                rfsaSession.Configuration.IQ.NumberOfSamples = (long)(RX_IQRate * (this.DwellTime - 2 * NF_VSTDriver.RefTrig_Marker_Delay));

                rfsaSession.Configuration.IQ.NumberOfRecords = NumberOfRecords * NumberOfRuns;
                rfsaSession.Configuration.BasicConfigurationList.CreateConfigurationList(this.Band, NF_VSTDriver.RFSA_PropertyList, true);

                for (int j = 0; j < NumberOfRecords; j++)
                {
                    rfsaSession.Configuration.BasicConfigurationList.CreateStep(true);
                    rfsaSession.Configuration.IQ.CarrierFrequency = this.RampFrequencyList[j] + RX_FrequencyOffsetOut;
                    rfsaSession.Acquisition.Advanced.DownconverterCenterFrequency = RX_FrequencyOffsetOut + this.MiddleFrequency + NF_VSTDriver.RFSA_LO_Offset;
                }

                rfsaSession.Configuration.BasicConfigurationList.ActiveStep = 0;
                rfsaSession.Utility.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Configure_VSTSA():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        private void Configure_VSTSA_FixedTX(double RX_FrequencyOffsetOut)
        {
            if (this.multiplier_RXIQRate <= 0)
            {
                this.multiplier_RXIQRate = 1.25; //force this variable to default constant
            }
            try
            {
                RX_IQRate = this.multiplier_RXIQRate * this.Rbw;

                rfsaSession.Configuration.Vertical.ReferenceLevel = this.SAReferenceLevel;
                rfsaSession.Configuration.IQ.IQRate = RX_IQRate;
                rfsaSession.Configuration.IQ.NumberOfSamples = (long)(RX_IQRate * (this.DwellTime - 2 * NF_VSTDriver.RefTrig_Marker_Delay));

                rfsaSession.Configuration.IQ.NumberOfRecords = NumberOfRecords * NumberOfRuns;
                rfsaSession.Configuration.BasicConfigurationList.CreateConfigurationList(this.Band, NF_VSTDriver.RFSA_PropertyList, true);

                //double RFSA_LO_Offset_FixedTX = RXFrequencyRange; //To solve Downconverter frequency out of range issue
                double RFSA_LO_Offset_FixedTX = 95e6; //To solve Downconverter frequency out of range issue

                for (int j = 0; j < NumberOfRecords; j++)
                {
                    rfsaSession.Configuration.BasicConfigurationList.CreateStep(true);
                    rfsaSession.Configuration.IQ.CarrierFrequency = this.RXRampFrequencyList[j]; //use independent Ramp Frequency List for RX band
                    //rfsaSession.Acquisition.Advanced.DownconverterCenterFrequency = this.RXRampFrequencyList[0] + RFSA_LO_Offset_FixedTX;
                    if (RXFrequencyRange < 95e6)
                    {
                        rfsaSession.Acquisition.Advanced.DownconverterCenterFrequency = this.RXRampFrequencyList[0] + RFSA_LO_Offset_FixedTX;
                    }
                    else
                    {
                        //Note : Max of 95MHz because we assume that the maximum Modulation bandwidth is 10MHz 
                        //So , VST NI5646R is 200MHz (LO can only be max of 100MHz) , so max RFSA_LO_Offset_FixedTX (95MHz) + 10MHz/2 (SG BW Modulation bandwitdh/2) = 100MHz
                        MessageBox.Show("RX Bandwidth bigger than 95MHz - Will cause error to NI VST");
                    }

                }

                rfsaSession.Configuration.BasicConfigurationList.ActiveStep = 0;
                rfsaSession.Utility.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Configure_VSTSA():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        private void Configure_VSTSG(object obj)
        {
            try
            {
                int signalLength = 0;

                VST_WAVEFORM_MODE ModulationType;
                ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), this.WaveformName.ToUpper());
                int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString()); // to get the int value from System.Enum

                switch (ModulationType)
                {
                    case VST_WAVEFORM_MODE.CDMA2K:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.CDMA2KRC1:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.IS95A:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMAGTC1:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMA:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M1RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M12RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M20RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M50RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M48RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5M25RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5M8RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M100RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M18RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M48RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE15M75RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE15MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.CDMA2KCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMACUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    default: throw new Exception("Not such a waveform!");

                }

                rfsgSession.RF.PowerLevel = this.SGPowerLevel;
                rfsgSession.Arb.IQRate = SG_IQRate;

                rfsgSession.BasicConfigurationList.CreateConfigurationList(this.Band, NF_VSTDriver.RFSG_PropertyList, true);

                rfsgSession.BasicConfigurationList.CreateStep(true);//for soak
                rfsgSession.RF.Frequency = SoakFrequency;//for soak
                rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency - 60e6;//for soak
                //_rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency;//temp for 5644R
                //TODO: Revert to previous statement?

                if (TXFrequencyRange != RXFrequencyRange)
                {
                    for (int j = 0; j < NumberOfRecords; j++)
                    {
                        rfsgSession.BasicConfigurationList.CreateStep(true);
                        rfsgSession.RF.Frequency = this.TXRampFrequencyList[j]; //use independent RampFrequency list for TX
                        rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency - 60e6;
                        //_rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency;//temp for 5644R
                        //TODO: Revert to previous statement?
                    }
                }
                else
                {
                    for (int j = 0; j < NumberOfRecords; j++)
                    {
                        rfsgSession.BasicConfigurationList.CreateStep(true);
                        rfsgSession.RF.Frequency = this.RampFrequencyList[j];
                        rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency - 60e6;
                        //_rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency;//temp for 5644R
                        //TODO: Revert to previous statement?
                    }
                }

                rfsgSession.BasicConfigurationList.ActiveStep = 0;

                if (SoakTime - ((this.DwellTime + NF_VSTDriver.IdleTime) * NumberOfRecords) > 0)
                {
                    SoakTime = SoakTime - ((this.DwellTime + NF_VSTDriver.IdleTime) * NumberOfRecords); //removes initial sweep time from overall soak time #NJK
                }

                int soakTimeSignalRepeat = (int)((SG_IQRate * SoakTime) / signalLength);
                int soakTimeResidualSamples = (int)((SG_IQRate * SoakTime) % signalLength);
                soakTimeResidualSamples -= soakTimeResidualSamples % 2;

                int Leading_Script_Idle_Samples = (int)(NF_VSTDriver.Leading_Script_Idle_Length * SG_IQRate);  //Truncating towards zero
                int RefTrig_Marker_Delay_Samples = (int)(NF_VSTDriver.RefTrig_Marker_Delay * SG_IQRate);  //Truncating towards zero

                double signalLengthIdle = (int)Math.Round(SG_IQRate * NF_VSTDriver.IdleTime);
                signalLengthIdle = signalLengthIdle - (signalLengthIdle % 2);
                double signalLengthWaveform = (int)Math.Round(SG_IQRate * (this.DwellTime + NF_VSTDriver.RefTrig_Marker_Delay));
                signalLengthWaveform = signalLengthWaveform - (signalLengthWaveform % 2);

                RefTrig_Marker_Delay_Samples -= RefTrig_Marker_Delay_Samples % 2;
                Leading_Script_Idle_Samples -= Leading_Script_Idle_Samples % 2;

                int numOfPresoakSweep = preSoakSweep == true ? 1 : 0;

                string Script = CreateGenerationScript(this.Band, "Signal" + this.WaveformName, "Idle" + this.WaveformName, Leading_Script_Idle_Samples, NumberOfRecords, NumberOfRuns - numOfPresoakSweep
                    , signalLengthWaveform, signalLengthIdle, RefTrig_Marker_Delay_Samples, soakTimeSignalRepeat, soakTimeResidualSamples, preSoakSweep);

                rfsgSession.Arb.Scripting.WriteScript(Script);
                rfsgSession.Arb.Scripting.SelectedScriptName = this.Band;
                rfsgSession.Utility.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Configure_VSTSG():\n" + ex.GetType() + ": " + ex.Message);
            }
            Config_VSTSGFlag.Set();
        }

        private void Configure_VSTSG_FixedTX(object obj)
        {
            try
            {
                int signalLength = 0;

                VST_WAVEFORM_MODE ModulationType;
                ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), this.WaveformName.ToUpper());
                int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString()); // to get the int value from System.Enum

                switch (ModulationType)
                {
                    case VST_WAVEFORM_MODE.CDMA2K:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.CDMA2KRC1:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.IS95A:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMAGTC1:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMA:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M1RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M12RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M20RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M50RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10M48RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5M25RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5M8RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M100RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M18RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20M48RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE15M75RB:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE5MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE10MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE20MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.LTE15MCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.CDMA2KCUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    case VST_WAVEFORM_MODE.WCDMACUSTOM:
                        SG_IQRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;
                        signalLength = NF_VSTDriver.SignalType[arrayNo].signalLength;
                        break;

                    default: throw new Exception("Not such a waveform!");

                }

                rfsgSession.RF.PowerLevel = this.SGPowerLevel;
                rfsgSession.Arb.IQRate = SG_IQRate;

                rfsgSession.BasicConfigurationList.CreateConfigurationList(this.Band, NF_VSTDriver.RFSG_PropertyList, true);

                rfsgSession.BasicConfigurationList.CreateStep(true);//for soak
                rfsgSession.RF.Frequency = SoakFrequency;//for soak
                rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency - 60e6;//for soak
                //_rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency;//temp for 5644R
                //TODO: Revert to previous statement?

                for (int j = 0; j < NumberOfRecords; j++)
                {
                    rfsgSession.BasicConfigurationList.CreateStep(true);
                    rfsgSession.RF.Frequency = this.TXRampFrequencyList[j]; //use independent RampFrequency list for TX
                    rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency - 60e6;
                    //_rfsgSession.RF.Upconverter.CenterFrequency = this.MiddleFrequency;//temp for 5644R
                    //TODO: Revert to previous statement?
                }

                rfsgSession.BasicConfigurationList.ActiveStep = 0;

                if (SoakTime - ((this.DwellTime + NF_VSTDriver.IdleTime) * NumberOfRecords) > 0)
                {
                    SoakTime = SoakTime - ((this.DwellTime + NF_VSTDriver.IdleTime) * NumberOfRecords); //removes initial sweep time from overall soak time #NJK
                }

                int soakTimeSignalRepeat = (int)((SG_IQRate * SoakTime) / signalLength);
                int soakTimeResidualSamples = (int)((SG_IQRate * SoakTime) % signalLength);
                soakTimeResidualSamples -= soakTimeResidualSamples % 2;

                int Leading_Script_Idle_Samples = (int)(NF_VSTDriver.Leading_Script_Idle_Length * SG_IQRate);  //Truncating towards zero
                int RefTrig_Marker_Delay_Samples = (int)(NF_VSTDriver.RefTrig_Marker_Delay * SG_IQRate);  //Truncating towards zero

                double signalLengthIdle = (int)Math.Round(SG_IQRate * NF_VSTDriver.IdleTime);
                signalLengthIdle = signalLengthIdle - (signalLengthIdle % 2);
                double signalLengthWaveform = (int)Math.Round(SG_IQRate * (this.DwellTime + NF_VSTDriver.RefTrig_Marker_Delay));
                signalLengthWaveform = signalLengthWaveform - (signalLengthWaveform % 2);

                RefTrig_Marker_Delay_Samples -= RefTrig_Marker_Delay_Samples % 2;
                Leading_Script_Idle_Samples -= Leading_Script_Idle_Samples % 2;

                int numOfPresoakSweep = preSoakSweep == true ? 1 : 0;

                string Script = CreateGenerationScript(this.Band, "Signal" + this.WaveformName, "Idle" + this.WaveformName, Leading_Script_Idle_Samples, NumberOfRecords, NumberOfRuns - numOfPresoakSweep
                    , signalLengthWaveform, signalLengthIdle, RefTrig_Marker_Delay_Samples, soakTimeSignalRepeat, soakTimeResidualSamples, preSoakSweep);

                rfsgSession.Arb.Scripting.WriteScript(Script);
                rfsgSession.Arb.Scripting.SelectedScriptName = this.Band;
                rfsgSession.Utility.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Configure_VSTSG():\n" + ex.GetType() + ": " + ex.Message);
            }
            Config_VSTSGFlag.Set();
        }

        /// <summary>Measures and returns the MAX Hold Trace.</summary>
        public S_MultiRBW_Data[] Measure_VST(int sortArray)
        {
            //Stopwatch tTime = new Stopwatch();
            try
            {
                RX_IQRate = rfsaSession.Configuration.IQ.IQRate;
                RX_NumberOfSamples = (int)rfsaSession.Configuration.IQ.NumberOfSamples;

                rfsgSession.Abort();
                rfsaSession.Acquisition.IQ.Initiate();
                rfsgSession.Initiate();

                //tTime.Reset();
                //tTime.Start();

                //ResultsMaxHoldTrace = new double[NumberOfRecords * NumberOfTracePoints];
                ResultsMaxHoldTrace = new double[(NumberOfRecords * NumberOfTracePoints) + 1];
                ResultsTrace = new double[sortArray, NumberOfRuns];
                //ResultsTrace = new double[NumberOfRecords * NumberOfTracePoints, NumberOfRuns];
                //ResultsVBWRawTrace = new double[NumberOfRecords * RX_NumberOfSamples, NumberOfRuns];

                MultiTraceData = new double[sortArray, NumberOfRuns];

                //Multiple RBW data declaration
                MultiRBW_Data = new S_MultiRBW_Data[this.Bandwidths.Length];
                for (int i = 0; i < MultiRBW_Data.Length; i++)
                {
                    MultiRBW_Data[i].multiTraceData = new double[sortArray, NumberOfRuns];
                    MultiRBW_Data[i].rsltMaxHoldTrace = new double[(NumberOfRecords * NumberOfTracePoints) + 1];
                    MultiRBW_Data[i].rsltTrace = new double[sortArray, NumberOfRuns];
                }

                //double ticktime1 = Stopwatch.GetTimestamp();

                for (int n = 0; n < NumberOfRuns; n++)
                {
                    // create bounded and blocking thread safe queue //
                    dataQueue = new BlockingCollection<NationalInstruments.ComplexDouble[]>
                        (new ConcurrentQueue<NationalInstruments.ComplexDouble[]>());
                    #region ProducerConsumer
                    ConsumerFlag.Reset(); // reset the consumer flag
                    Stopwatch benchmark = new Stopwatch();
                    benchmark.Reset();
                    benchmark.Start();
                    double time = 0;
                    ThreadPool.QueueUserWorkItem(Consumer, n); // start a new thread for consumer loop
                    benchmark.Stop();
                    time = benchmark.ElapsedMilliseconds;
                    Producer(n); // start producer loop  
                    ConsumerFlag.WaitOne(); // wait for Consumer to finish
                    #endregion
                }

                CalculateMaxHold();//Calculating MaxHold on the ResultsTrace (on max points not on raw VBW) 
                MultiRBWCalculateMaxHold();//Calculating MaxHold on the ResultsTrace (on max points not on raw VBW) 

                //Sorted out the trace data
                //for (int n = 0; n < NumberOfRuns; n++)
                //{
                //    CalculateTraceTrim(n, sortArray);
                //}

                //tTime.Stop();
                //double measurementTime = tTime.ElapsedMilliseconds;
                //double avgTime = (measurementTime - this.SoakTime * 1000) / NumberOfRuns;
                //string temp = "2. Avg. Measurement Time (mS) : " + avgTime.ToString();
                //File.AppendAllText(logPath +@"\TestTime_Clotho_" + ticktime.ToString() + ".txt", temp + Environment.NewLine + "Number of Trace Data_" + Modulation + " : " + ResultsTrace.GetLength(0).ToString() + Environment.NewLine + Environment.NewLine);

                rfsaSession.Acquisition.IQ.Abort();
                rfsgSession.Abort();

                rfsaSession.Configuration.BasicConfigurationList.DeleteConfigurationList(this.Band);
                rfsaSession.Configuration.BasicConfigurationList.ActiveList = string.Empty;

                rfsgSession.BasicConfigurationList.DeleteConfigurationList(this.Band);
                rfsgSession.BasicConfigurationList.ActiveList = string.Empty;

                //Log_Results_MaxHoldTrace();
                //Log_Results_Trace();
                //Log_Results_RawVBW();

            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Measure_VST():\n" + ex.GetType() + ": " + ex.Message);
            }

            //return ResultsMaxHoldTrace;
            return MultiRBW_Data;
        }

        private void Producer(object obj)
        {
            try
            {
                for (int i = 0; i < NumberOfRecords; i++)
                {
                    int recordNum = ((int)obj * NumberOfRecords) + i;
                    iqData = rfsaSession.Acquisition.IQ.FetchIQSingleRecordComplex<NationalInstruments.ComplexDouble>
                        ((long)recordNum, (long)RX_NumberOfSamples, timeout);
                    dataQueue.Add(iqData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in Producer thread:\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        private void Consumer(object obj)
        {
            try
            {
                NationalInstruments.ComplexDouble[] data_Volts;
                int queueTimeout = 10000;

                // TODO: 3/29
                //multiRBW array - sort Ascending order
                int bw_cnt = 0;
                double[] bandwidths = new double[this.Bandwidths.Length];
                double[] data_dBm = new double[RX_NumberOfSamples];
                double[][] multiRBW_data_dBm = new double[bandwidths.Length][];

                //Editted by DS for benchmarking
                bandwidths = this.Bandwidths;
                bw_cnt = bandwidths.Length;

                Parallel.For(0, bw_cnt, m =>
                {
                    multiRBW_data_dBm[m] = new double[RX_NumberOfSamples];
                });

                for (int j = 0; j < NumberOfRecords; j++)
                {

                    if (dataQueue.TryTake(out data_Volts, queueTimeout))
                    {
                        if (false)
                        {
                            One_Ohm_Voltage_to_dBm((int)RX_NumberOfSamples, ref data_Volts, ref data_dBm);
                            VBW_Filter_IIR(Vbw, data_dBm.Take(1000).Average(), ref data_dBm, (1 / RX_IQRate), RX_NumberOfSamples);
                        }
                        else
                        {
                            double[] real, imag;
                            NationalInstruments.ComplexDouble.DecomposeArray(data_Volts, out real, out imag);
                            Filters.ParallelComplexFilter(RX_IQRate, Vbw, 1000, bandwidths, 70, real, imag, false);
                            //Debug set to true for file dumping purpose

                            Parallel.For(0, bandwidths.Length, bw_cnt2 =>
                            {
                                Filters.GetFilteredData1D(bw_cnt2, out multiRBW_data_dBm[bw_cnt2]);
                                MultiRBW_Data[bw_cnt2].RBW_Hz = bandwidths[bw_cnt2];  //save current RBW for use in later stage/function
                            });

                        }

                        for (int bw_cnt1 = 0; bw_cnt1 < bandwidths.Length; bw_cnt1++)
                        {
                            ResultsTrace = MultiRBW_Data[bw_cnt1].rsltTrace;     //copy back the previous ResultsTrace from specific bandwidth, original data was overwritten by subsequence bandwidth data
                            data_dBm = multiRBW_data_dBm[bw_cnt1];
                            //Get NumberOfTracePoints of maximum samples (in the order fetched)
                            double[] dataCopy = new double[data_dBm.Length];

                            Array.Copy(data_dBm, dataCopy, data_dBm.Length);
                            Array.Sort(dataCopy);
                            Array.Reverse(dataCopy);

                            int tempNumTracePoints = NumberOfTracePoints;
                            if (j == (NumberOfRecords - 1))
                            { tempNumTracePoints += 1; }

                            double[] maxPoints = dataCopy.Take(tempNumTracePoints).ToArray();

                            int[] listOfMaxIndex = new int[tempNumTracePoints];

                            Parallel.For(0, tempNumTracePoints, k1 =>
                            {
                                int index = Array.IndexOf(data_dBm, maxPoints[k1]);
                                listOfMaxIndex[k1] = index;
                                data_dBm[index] = -999;
                            });
                            int[] sortedListOfMaxIndex = new int[listOfMaxIndex.Length];
                            Array.Copy(listOfMaxIndex, sortedListOfMaxIndex, listOfMaxIndex.Length);
                            Array.Sort(sortedListOfMaxIndex);
                            Parallel.For(0, tempNumTracePoints, k2 =>
                            {
                                ResultsTrace[j * NumberOfTracePoints + k2, (int)obj] = maxPoints[Array.IndexOf(listOfMaxIndex, sortedListOfMaxIndex[k2])];
                                MultiRBW_Data[bw_cnt1].rsltTrace = ResultsTrace;
                            });

                        }
                    }
                    else
                        throw new Exception("Timed out waiting to dequeue");
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR  in Consumer thread:\n" + ex.GetType() + ": " + ex.Message);
            }
            // notify producer consumer loop that the consumer loop is finished executing //
            ConsumerFlag.Set();
        }

        private void MultiRBWCalculateMaxHold()
        {
            double[] Max_PlaceHolder = new double[NumberOfRuns];

            for (int m = 0; m < MultiRBW_Data.Length; m++)
            {
                for (int k = 0; k < NumberOfTracePoints * NumberOfRecords; k++)
                {
                    for (int n = 0; n < NumberOfRuns; n++)
                    {
                        Max_PlaceHolder[n] = MultiRBW_Data[m].rsltTrace[k, n];
                    }
                    MultiRBW_Data[m].rsltMaxHoldTrace[k] = Max_PlaceHolder.Max();
                }
            }


        }

        private void CalculateMaxHold()
        {
            double[] Max_PlaceHolder = new double[NumberOfRuns];
            for (int k = 0; k < (NumberOfTracePoints * NumberOfRecords) + 1; k++)
            {
                for (int n = 0; n < NumberOfRuns; n++)
                {
                    Max_PlaceHolder[n] = ResultsTrace[k, n];
                }
                ResultsMaxHoldTrace[k] = Max_PlaceHolder.Max();
            }
        }

        private void CalculateTraceTrim(int runNo, int sortArray)
        {
            //Note : to trim excess data from PXI eg start freq :1920 stop freq :1980
            //For CDMA2K - No of point per 0.1 MHz step should be 600 but PXI return 610, where in 1MHz/0.1MHz = 10 points
            //For LTE - where step is 5MHz/0.1MHz = 50 points
            //Note : Step/0.1MHz = NumberOfTracePoints

            double[] tmpSortLastData = new double[NumberOfTracePoints];
            int cnt = 0;
            int x = (NumberOfTracePoints * NumberOfRecords) - NumberOfTracePoints;      //start from last NumberOfTracePoints data block
            double last10data = -999;

            for (int j = x; j < NumberOfTracePoints * NumberOfRecords; j++)
            {
                tmpSortLastData[cnt] = ResultsTrace[j, runNo];
                cnt++;
            }
            last10data = tmpSortLastData.Max();                             //return max value
            int indexdata = Array.IndexOf(tmpSortLastData, last10data);     //return index of max value

            for (int k = 0; k < (x + 1); k++)
            {
                if (k < sortArray)
                {
                    MultiTraceData[k, runNo] = ResultsTrace[k, runNo];
                }
            }
            MultiTraceData[x, runNo] = last10data;         //pass the max of 11 data to last data points
        }

        private static string CreateGenerationScript(string scriptName, string waveformName, string idleWaveformName, int Leading_Script_Idle_Samples,
           int numRec, int numRuns, double waveformLength, double idleWaveformLength, int RefTrig_Marker_Delay_Samples, int soakTimeSignalRepeat, int soakTimeResidualSamples, bool preSoakSweep)
        {

            StringBuilder scriptBuilder = new StringBuilder();
            scriptBuilder.Append("script " + scriptName + "\r\n");

            //String to repeat during script flattening for each sweep step
            string repeatingSignal = "generate " + waveformName + " subset(0, " + waveformLength.ToString() + ") marker1(" + RefTrig_Marker_Delay_Samples.ToString() + ")\r\n";
            repeatingSignal += "generate " + idleWaveformName + " subset(0, " + idleWaveformLength + ") marker0(0)\r\n";

            //String to call at the end of each sweep to index past the soak frequency (element 0 in the freuqency list)
            string repeatingExtraSamples = "generate " + waveformName + " subset(0,490)" + " marker0(0)\r\n";// Set the script line here to generate those extra samples after every run

            if (preSoakSweep)
            {
                scriptBuilder.Append("generate " + waveformName + " subset(0, " + Leading_Script_Idle_Samples.ToString() + ") marker0(0) marker2(0) \r\n");
                scriptBuilder.Append("repeat " + numRec.ToString() + " \r\n");
                scriptBuilder.Append(repeatingSignal);
                scriptBuilder.Append("end repeat \r\n");
            }

            scriptBuilder.Append("generate " + waveformName + " subset(0, " + Leading_Script_Idle_Samples.ToString() + ") marker2(0)\r\n");

            //Soak portion of script
            if (soakTimeSignalRepeat != 0)
            {
                scriptBuilder.Append("repeat " + soakTimeSignalRepeat + "\r\n" +
                                        "generate " + waveformName + "\r\n" +
                                     "end repeat \r\n");
            }
            if (soakTimeResidualSamples >= 2)
            { scriptBuilder.Append("generate " + waveformName + " subset(0, " + soakTimeResidualSamples.ToString() + ") marker0(" + (soakTimeResidualSamples - 2).ToString() + ")\r\n"); }
            else
            { scriptBuilder.Append("generate " + waveformName + " subset(0, 2) marker0(0)\r\n"); } //ensures that the generator is kicked out of the soak frequency if residual less than 2

            //Retune time from soak frequency to first sweep frequency
            scriptBuilder.Append("generate " + waveformName + " subset(0, " + Leading_Script_Idle_Samples.ToString() + ") marker2(0)\r\n");

            //Build Flattened Script
            for (int i = 0; i < numRuns; i++) //for each sweep
            {
                for (int j = 0; j < numRec; j++) //for each sweep step
                {
                    scriptBuilder.Append(repeatingSignal);
                }
                scriptBuilder.Append(repeatingExtraSamples);
            }

            scriptBuilder.Append("end script");

            return scriptBuilder.ToString();
        }

        //void Log_Results_Trace()
        //{
        //    string logFile = logPath + "\\" + Band + Modulation + "Results_Trace_" + Stopwatch.GetTimestamp().ToString() + ".csv";
        //    string delimiter = ",";

        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("#Trace" + Environment.NewLine + "#Date : " + (DateTime.Now.ToString("M/d/yyyy")) + Environment.NewLine + "#Time : " + (DateTime.Now.ToString("H:mm:ss")) + Environment.NewLine);
        //    for (int i = 0; i < NumberOfRecords * NumberOfTracePoints; i++)
        //    {
        //        for (int j = 0; j < NumberOfRuns; j++)
        //        {
        //            sb.Append(ResultsTrace[i, j].ToString("0.00") + delimiter);
        //        }
        //        sb.Append(Environment.NewLine);
        //    }
        //    File.AppendAllText(logFile, sb.ToString() + Environment.NewLine);
        //}

        //void Log_Results_MaxHoldTrace()
        //{
        //    string logFile = logPath + "\\" + Band + Modulation + "Results_MaxHoldTrace_" + Stopwatch.GetTimestamp().ToString() + ".csv";
        //    string delimiter = Environment.NewLine;

        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("#MaxHoldTrace" + Environment.NewLine + "#Date : " + (DateTime.Now.ToString("M/d/yyyy")) + Environment.NewLine + "#Time : " + (DateTime.Now.ToString("H:mm:ss")) + Environment.NewLine);
        //    for (int i = 0; i < NumberOfRecords * NumberOfTracePoints; i++)
        //    {   
        //         sb.Append(ResultsMaxHoldTrace[i].ToString("0.00") + delimiter);

        //    }
        //    File.AppendAllText(logFile, sb.ToString() + Environment.NewLine);
        //}

        //void Log_Results_RawVBW()
        //{
        //    string logFile = logPath + "\\" +  Band + Modulation + "Results_RawVBWTrace_" + Stopwatch.GetTimestamp().ToString() + ".csv";
        //    string delimiter = ",";

        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("#VBW" + Environment.NewLine + "#Date : " + (DateTime.Now.ToString("M/d/yyyy")) + Environment.NewLine + "#Time : " + (DateTime.Now.ToString("H:mm:ss")) + Environment.NewLine);
        //    for (int i = 0; i < NumberOfRecords * RX_NumberOfSamples; i++)
        //    {
        //        for (int j = 0; j < NumberOfRuns; j++)
        //        {
        //            sb.Append(ResultsVBWRawTrace[i, j].ToString("0.00") + delimiter);
        //        }
        //        sb.Append(Environment.NewLine);
        //    }
        //    File.AppendAllText(logFile, sb.ToString() + Environment.NewLine);
        //}

        /// <summary>Closes VST SA & SG instrument sessions.</summary>
        public void Close_VST()
        {
         
            rfsaSession.Close();
            rfsaSession = null;
            rfsgSession.Close();
            rfsgSession = null;
        }

        private static void VBW_Filter_IIR(double vBW_Hz, double Initial_Condition, ref double[] data_dBm, double dt, int NumberOfSamples)
        {
            double x = ((vBW_Hz / 2) * (2 * NF_VSTDriver.PI) * (dt)) / ((vBW_Hz / 2) * (2 * NF_VSTDriver.PI) * (dt) + 1);
            for (int m = 0; m < NumberOfSamples; m++)
            {
                data_dBm[m] = (data_dBm[m] * x) - (Initial_Condition * (x - 1));
                Initial_Condition = data_dBm[m];
            }
        }

        // Voltage to dBm conversion
        private static void One_Ohm_Voltage_to_dBm(int NumberOfSamples, ref NationalInstruments.ComplexDouble[] data, ref double[] data_dBm)
        {
            for (int p = 0; p < NumberOfSamples; p++)
            {
                if (data[p].Magnitude == 0 && p != 0)
                    data_dBm[p] = data_dBm[p - 1];
                else if (data[p].Magnitude == 0)
                    data_dBm[p] = -174;
                else
                    data_dBm[p] = 20 * Math.Log10(data[p].Magnitude) + 30.0 - 16.9897 - 3.0;
                /*
                    30 dB for mW to W
                    16.9897 dB for 50 ohm load
                    3 dB for power split
                */
            }
        }

        // Frequency Ramp function
        private static void FrequencyRamp(double FrequencyStart, double FrequencyEnd, double FrequencyStep, out int Ramp_NumberOfSamples, List<double> FreqRamp)
        {
            FreqRamp.Clear();
            Ramp_NumberOfSamples = (int)(1 + (FrequencyEnd - FrequencyStart) / (FrequencyStep));  //Note that if (Ramp_End - Ramp_Start) is not an integer multiple of Ramp_Step, then the (int) cast will truncate towards zero by default
            for (int i = 0; i < Ramp_NumberOfSamples; i++)
            {
                FreqRamp.Add(FrequencyStart + i * FrequencyStep);
            }
        }

        // Frequency Ramp function for fixed TX
        private static void FrequencyRampFixedTX(double TXFrequencyStart, double TXFrequencyStop, double RXFrequencyStart, double RXFrequencyStop, double TXFrequencyStep, out int Ramp_NumberOfSamples, List<double> TXFreqRamp, List<double> RXFreqRamp)
        {
            TXFreqRamp.Clear();
            RXFreqRamp.Clear();
            Ramp_NumberOfSamples = (int)(1 + (RXFrequencyStop - RXFrequencyStart) / (TXFrequencyStep));  //Note that if (Ramp_End - Ramp_Start) is not an integer multiple of Ramp_Step, then the (int) cast will truncate towards zero by default
            for (int i = 0; i < Ramp_NumberOfSamples; i++)
            {
                //TXFreqRamp.Add(TXFrequencyStart + i * TXFrequencyStep);     //note: coding bug for FIX TX
                TXFreqRamp.Add(TXFrequencyStart);
                RXFreqRamp.Add(RXFrequencyStart + i * TXFrequencyStep);
            }
        }

        // Decode the mutate signal variable
        public void Decode_MutSignal_Setting(string Mutate_Data)
        {
            string[] Tempdata1;
            string[] TempData2;

            MutSignal_Setting = new S_MutSignal_Setting();

            Tempdata1 = Mutate_Data.Split(';');

            for (int i = 0; i < Tempdata1.Length; i++)
            {
                TempData2 = Tempdata1[i].Split('@');

                switch (TempData2[0].ToUpper())
                {
                    case "SETUP":
                        MutSignal_Setting.enable = Convert.ToBoolean(TempData2[1]);
                        break;
                    case "TOTALTIME":
                        MutSignal_Setting.total_time_sec = Convert.ToDouble(TempData2[1]);
                        break;
                    case "MODOFFSET":
                        MutSignal_Setting.mod_offset_sec = Convert.ToDouble(TempData2[1]);
                        break;
                    case "MODTIME":
                        MutSignal_Setting.mod_time_sec = Convert.ToDouble(TempData2[1]);
                        break;
                    case "FREQOFFSET":
                        MutSignal_Setting.freq_offset_hz = Convert.ToDouble(TempData2[1]);
                        break;
                    case "FOFFDELAY":
                        MutSignal_Setting.f_off_delay_sec = Convert.ToDouble(TempData2[1]);
                        break;
                }
            }
        }

        #region RX Contact Path - Measure internal DUT LNA

        private double MixerPhase;
        private double StartFrequency;
        private double StopFrequency;
        private double StepFrequency;
        private double CenterFrequency;
        private int NumberOfSteps;
        private double RFSGPowerLevel;
        private int SamplesPerStep;
        private int TotalSamples;
        private int SweepSamples;
        private double[] FreqRamp;
        private double dwellT;
        private int TestNum;
        private int TotalNumber;

        //public bool SA_StartTrigger;
        //public bool SA_AdvanceTriggerr;
        //public bool SA_ReferenceTrigger;

        public static ManualResetEvent[] threadFlags;


        public static Task taskConfigMipi;
        public static Task taskConfigSW;
        public static Task taskConfigDC;

        public void RXContactCheck(double pwrLvl, double startFreq, double stopFreq, double stepFreq, double SARefLevel, int TestNum, out double[] data)
        {
            Stopwatch tTime = new Stopwatch();

            double testtime1 = 0f;
            double testtime2 = 0f;
            double testtime3 = 0f;
            double testtime4 = 0f;
            double testtime5 = 0f;
            double testtime6 = 0f;
            double testtime7 = 0f;
            double testtime8 = 0f;
            double testtime9 = 0f;
            double testtime10 = 0f;
            double testtime11 = 0f;
            double testtime12 = 0f;
            double testtime13 = 0f;
            tTime.Reset();
            tTime.Start();

            //Migration in loop below
            //StartFrequency = startFreq * 1E6;
            //StopFrequency = stopFreq * 1E6;
            //StepFrequency = stepFreq * 1E6;
            //CenterFrequency = (StartFrequency + StopFrequency) / 2;
            stopFreq = Math.Round(stopFreq, 3);
            startFreq = Math.Round(startFreq, 3);
            stepFreq = Math.Round(stepFreq, 3);

            NumberOfSteps = Convert.ToInt16(Math.Ceiling((stopFreq - startFreq) / stepFreq) + 1);
            RFSGPowerLevel = pwrLvl;
            dwellT = 1E-6;      //dwellTime fixed to 1us per point/step
            // #TODO: add ability to set reference level from this call. See corresponding note in ConfigureArbitraryWaveform

            data = new double[NumberOfSteps];

            // Modify to improve wide band gain measurement by seperating sweep - 2020-05-15
            double _startFreq = startFreq, _stopFreq = stopFreq;
            int _NumberOfSteps = NumberOfSteps;

            List<double[]> dataList = new List<double[]>();
            double[] Tempdata = new double[NumberOfSteps];

            double[] StartFreqList;
            double[] StopFreqList;

            //it is considering to sort out VST5644(120Mhz) vs 5646(250Mhz)
            #region original
            //if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6) 
            //{
            //    StartFreqList = new double[2];
            //    StopFreqList = new double[2];

            //    StartFreqList[0] = startFreq;
            //    StopFreqList[0] = startFreq + Math.Truncate((double)NumberOfSteps / 2) * stepFreq;

            //    StartFreqList[1] = StopFreqList[0] + (NumberOfSteps % 2 == 0 ? 1 : 0) * stepFreq;
            //    StopFreqList[1] = stopFreq;

            //}
            #endregion

            if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6)
            {
                StartFreqList = new double[2];
                StopFreqList = new double[2];

                StartFreqList[0] = startFreq;
                StopFreqList[0] = startFreq + Math.Ceiling(((double)NumberOfSteps / 2) - 1) * stepFreq;

                StartFreqList[1] = StopFreqList[0] + (NumberOfSteps % 2 == 0 ? 1 : 0) * stepFreq;
                StopFreqList[1] = stopFreq;

            }
            else
            {
                StartFreqList = new double[1];
                StopFreqList = new double[1];

                StartFreqList[0] = startFreq;
                StopFreqList[0] = stopFreq;
            }

            try
            {

                threadFlags = new ManualResetEvent[2];

                for (int i = 0; i < threadFlags.Length; i++)
                {
                    threadFlags[i] = new ManualResetEvent(false);
                    threadFlags[i].Reset();
                }


                testtime1 = tTime.ElapsedMilliseconds;

                //Need to re-configure ReferenceTrigger setting because after NF measurement with Rfmx, this trigger setting is disabled.
                rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);
                SA_ReferenceTrigger = true;


                testtime2 = tTime.ElapsedMilliseconds;
                double test2 = tTime.ElapsedMilliseconds;
                for (int i = 0; i < StartFreqList.Length; i++)
                {
                    startFreq = StartFreqList[i];
                    stopFreq = StopFreqList[i];

                    StartFrequency = startFreq * 1E6;
                    StopFrequency = stopFreq * 1E6;
                    StepFrequency = stepFreq * 1E6;
                    CenterFrequency = (StartFrequency + StopFrequency) / 2;
                    NumberOfSteps = Convert.ToInt16(Math.Ceiling(Math.Round((stopFreq - startFreq) / stepFreq, 6)) + 1);

                    //ThreadPool.QueueUserWorkItem(Thread_Config_SA, SARefLevel);
                    //ThreadPool.QueueUserWorkItem(Thread_Config_SG, SARefLevel);




                    ConfigureRF();
                    testtime4 = tTime.ElapsedMilliseconds;
                    ConfigureTriggers();
                    testtime5 = tTime.ElapsedMilliseconds;
                    ConfigureArbitraryWaveform(SARefLevel);
                  

                    testtime6 = tTime.ElapsedMilliseconds;
                    ConfigureScript(Convert.ToString(TestNum));
                    testtime7 = tTime.ElapsedMilliseconds;
                    CommitConfiguration();

                    //threadFlags[0].WaitOne();
                    //testtime3 = tTime.ElapsedMilliseconds;
                    //threadFlags[1].WaitOne();
                    //testtime4 = tTime.ElapsedMilliseconds;


                    //for ( i = 0; i < threadFlags.Length; i++)
                    //{
                    //    threadFlags[i] = new ManualResetEvent(false);
                    //    threadFlags[i].Reset();
                    //}


                    //ThreadPool.QueueUserWorkItem(Thread_Ininite_SA);
                    //ThreadPool.QueueUserWorkItem(Thread_Ininite_SG);

                 //   threadFlags[0].WaitOne();
                

                    testtime5 = tTime.ElapsedMilliseconds;
                    //threadFlags[1].WaitOne();

               
                    Initiate();
                    testtime6 = tTime.ElapsedMilliseconds;
                    MeasurePower(out Tempdata);


                    testtime7 = tTime.ElapsedMilliseconds;
                    dataList.Add(Tempdata);
                    testtime8 = tTime.ElapsedMilliseconds;
                }

                Array.Copy(dataList[0], data, dataList[0].Length);
                testtime11 = tTime.ElapsedMilliseconds;

                if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6)
                    Array.Copy(dataList[1], (_NumberOfSteps % 2 == 0 ? 0 : 1), data, dataList[0].Length, dataList[1].Length - (_NumberOfSteps % 2 == 0 ? 0 : 1));

                testtime12 = tTime.ElapsedMilliseconds;
                ReConfigVST();
                testtime13 = tTime.ElapsedMilliseconds;

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                 //Close_VST();
                
            }
            finally
            {

            }
        }

        public void RXContactCheck(double pwrLvl, double startFreq, double stopFreq, double stepFreq, double SARefLevel, int _TestNum, int _TotalNumber, out double[] data)
        {
            Stopwatch tTime = new Stopwatch();

            double testtime1 = 0f;
            double testtime2 = 0f;
            double testtime3 = 0f;
            double testtime4 = 0f;
            double testtime5 = 0f;
            double testtime6 = 0f;
            double testtime7 = 0f;
            double testtime8 = 0f;
            double testtime9 = 0f;
            double testtime10 = 0f;
            double testtime11 = 0f;
            double testtime12 = 0f;
            double testtime13 = 0f;
            tTime.Reset();
            tTime.Start();
            TotalNumber = _TotalNumber;
            TestNum = _TestNum;
            //Migration in loop below
            //StartFrequency = startFreq * 1E6;
            //StopFrequency = stopFreq * 1E6;
            //StepFrequency = stepFreq * 1E6;
            //CenterFrequency = (StartFrequency + StopFrequency) / 2;
            stopFreq = Math.Round(stopFreq, 3);
            startFreq = Math.Round(startFreq, 3);
            stepFreq = Math.Round(stepFreq, 3);

            NumberOfSteps = Convert.ToInt16(Math.Ceiling((stopFreq - startFreq) / stepFreq) + 1);
            RFSGPowerLevel = pwrLvl;
            dwellT = 1E-6;      //dwellTime fixed to 1us per point/step
            // #TODO: add ability to set reference level from this call. See corresponding note in ConfigureArbitraryWaveform

            data = new double[NumberOfSteps];

            // Modify to improve wide band gain measurement by seperating sweep - 2020-05-15
            double _startFreq = startFreq, _stopFreq = stopFreq;
            int _NumberOfSteps = NumberOfSteps;

            List<double[]> dataList = new List<double[]>();
            double[] Tempdata = new double[NumberOfSteps];

            double[] StartFreqList;
            double[] StopFreqList;

            //it is considering to sort out VST5644(120Mhz) vs 5646(250Mhz)
            #region original
            //if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6) 
            //{
            //    StartFreqList = new double[2];
            //    StopFreqList = new double[2];

            //    StartFreqList[0] = startFreq;
            //    StopFreqList[0] = startFreq + Math.Truncate((double)NumberOfSteps / 2) * stepFreq;

            //    StartFreqList[1] = StopFreqList[0] + (NumberOfSteps % 2 == 0 ? 1 : 0) * stepFreq;
            //    StopFreqList[1] = stopFreq;

            //}
            #endregion

            if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6)
            {
                StartFreqList = new double[2];
                StopFreqList = new double[2];

                StartFreqList[0] = startFreq;
                StopFreqList[0] = startFreq + Math.Ceiling(((double)NumberOfSteps / 2) - 1) * stepFreq;

                StartFreqList[1] = StopFreqList[0] + (NumberOfSteps % 2 == 0 ? 1 : 0) * stepFreq;
                StopFreqList[1] = stopFreq;

            }
            else
            {
                StartFreqList = new double[1];
                StopFreqList = new double[1];

                StartFreqList[0] = startFreq;
                StopFreqList[0] = stopFreq;
            }

            try
            {

                threadFlags = new ManualResetEvent[2];

                testtime1 = tTime.ElapsedMilliseconds;

                //Need to re-configure ReferenceTrigger setting because after NF measurement with Rfmx, this trigger setting is disabled.

                testtime2 = tTime.ElapsedMilliseconds;
                double test2 = tTime.ElapsedMilliseconds;
                for (int i = 0; i < StartFreqList.Length; i++)
                {
                    if (StartFreqList.Length != 1)
                {

                }
                    startFreq = StartFreqList[i];
                    stopFreq = StopFreqList[i];

                    StartFrequency = startFreq * 1E6;
                    StopFrequency = stopFreq * 1E6;
                    StepFrequency = stepFreq * 1E6;
                    CenterFrequency = (StartFrequency + StopFrequency) / 2;
                    NumberOfSteps = Convert.ToInt16(Math.Ceiling(Math.Round((stopFreq - startFreq) / stepFreq, 6)) + 1);

            
                    bool a = SA_StartTrigger;

                    for (int j = 0; j < threadFlags.Length; j++)
                    {
                        threadFlags[j] = new ManualResetEvent(false);
                        threadFlags[j].Reset();
                    }

                    ThreadPool.QueueUserWorkItem(Thread_Config_SA, SARefLevel);
                    ThreadPool.QueueUserWorkItem(Thread_Config_SG, TestNum);


                    taskConfigSW.Wait();
                    testtime3 = tTime.ElapsedMilliseconds;

                    taskConfigDC.Wait();
                    testtime4 = tTime.ElapsedMilliseconds;
                    threadFlags[0].WaitOne();
                    testtime5 = tTime.ElapsedMilliseconds;

                    threadFlags[1].WaitOne();
                    testtime6 = tTime.ElapsedMilliseconds;
                    taskConfigMipi.Wait();
               
              
                 
                    testtime7 = tTime.ElapsedMilliseconds;

                    Initiate();
                    testtime8 = tTime.ElapsedMilliseconds;

                    MeasurePower(TotalNumber, out Tempdata);
                    testtime9 = tTime.ElapsedMilliseconds;
             
                    dataList.Add(Tempdata);
                    testtime10 = tTime.ElapsedMilliseconds;
                }

                Array.Copy(dataList[0], data, dataList[0].Length);
                testtime11 = tTime.ElapsedMilliseconds;

                if (Convert.ToInt16(Math.Ceiling((_stopFreq - _startFreq))) > 100 && IQRate < 250E6)
                    Array.Copy(dataList[1], (_NumberOfSteps % 2 == 0 ? 0 : 1), data, dataList[0].Length, dataList[1].Length - (_NumberOfSteps % 2 == 0 ? 0 : 1));

                testtime12 = tTime.ElapsedMilliseconds;
                ReConfigVST();
                testtime13 = tTime.ElapsedMilliseconds;

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                //Close_VST();

            }
            //finally
            //{

            //}
        }

        private void ConfigureRF()
        {
            // set modes //
            // rfsgSession.Arb.GenerationMode = RfsgWaveformGenerationMode.Script; <-- script mode is already set
            // rfsgSession.RF.PowerLevelType = RfsgRFPowerLevelType.PeakPower; <-- peak power is already set
            // rfsaSession.Configuration.AcquisitionType = RfsaAcquisitionType.IQ; <-- rfsa session already set to IQ
            // tune generator and analyzer to center of band //
            rfsgSession.RF.Configure(CenterFrequency, RFSGPowerLevel);
            rfsaSession.Configuration.IQ.CarrierFrequency = CenterFrequency;    //Seoul amplitude offset to NF

            // set max iq rates //
            // Chii Chew change
            //double IQRate = 120E6;    for VST5644
            //double IQRate = 250E6;    for VST5646
            rfsgSession.Arb.IQRate = IQRate;
            rfsaSession.Configuration.IQ.IQRate = IQRate;

            // calculate waveform constants //
            SamplesPerStep = (int)Math.Ceiling(dwellT * IQRate);
            SweepSamples = SamplesPerStep * NumberOfSteps;
        }

        public void ConfigureIQforNF() //Not Used of Now
        {
            //Case of NF BW 4MHz, Sweep Time 10ms
            rfsaSession.Configuration.IQ.IQRate = 4160000.0000006077;
            rfsaSession.Configuration.IQ.NumberOfSamples = 41601;
        }


        //private void ConfigureTriggers() //Original        
        public void ConfigureTriggers() //Seoul
        {
            // triggers will need to be reconfigured later //
            // rfsgSession.DeviceEvents.MarkerEvents[1].ExportedOutputTerminal = RfsgMarkerEventExportedOutputTerminal.PxiTriggerLine1; <-- marker 1 already exported
            // rfsaSession.Configuration.Triggers.ReferenceTrigger.Type = RfsaReferenceTriggerType.DigitalEdge; <- ref trigger already set
            // rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Source = RfsaDigitalEdgeReferenceTriggerSource.PxiTriggerLine1; <-- ref trigger already set
            // disable start and advance triggers //

            //if (SA_StartTrigger) SA_StartTrigger = false;
            //if (SA_AdvanceTrigger) SA_AdvanceTrigger = false;
            //if (SA_ReferenceTrigger) SA_ReferenceTrigger = false;



            //if (SA_StartTrigger)
            //{
            
            //    rfsaSession.Configuration.Triggers.StartTrigger.Disable();
            //    SA_StartTrigger = false;
            //}
            //if (SA_AdvanceTriggerr)
            //{
            //    rfsaSession.Configuration.Triggers.AdvanceTrigger.Disable();
            //    SA_AdvanceTriggerr = false;
            //}
            //if (SA_ReferenceTrigger)
            //{
            //    rfsaSession.Configuration.Triggers.ReferenceTrigger.Disable(); //Disable Reference Trigger for NF Testing.
            //    SA_ReferenceTrigger = false;
            //}
        

        }

        public void ConfigureTriggers_ForCold() //Seoul
        {
            // triggers will need to be reconfigured later //
            // rfsgSession.DeviceEvents.MarkerEvents[1].ExportedOutputTerminal = RfsgMarkerEventExportedOutputTerminal.PxiTriggerLine1; <-- marker 1 already exported
            // rfsaSession.Configuration.Triggers.ReferenceTrigger.Type = RfsaReferenceTriggerType.DigitalEdge; <- ref trigger already set
            // rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Source = RfsaDigitalEdgeReferenceTriggerSource.PxiTriggerLine1; <-- ref trigger already set
            // disable start and advance triggers //
            RfsaAcquisitionType rfsaAcquisition = RfsaAcquisitionType.IQ;
            rfsaSession.Configuration.ReferenceClock.Configure(NF_VSTDriver.ReferenceClockSource, 10e6);
            rfsaSession.Configuration.AcquisitionType = rfsaAcquisition;



            //if (SA_StartTrigger) SA_StartTrigger = false;
            //if (SA_AdvanceTrigger) SA_AdvanceTrigger = false;
            //if (!SA_ReferenceTrigger)
            //{
            rfsaSession.Configuration.Triggers.StartTrigger.Disable();
            rfsaSession.Configuration.Triggers.AdvanceTrigger.Disable();
            rfsaSession.Configuration.Triggers.ReferenceTrigger.Disable();

            //rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);
            //SA_ReferenceTrigger = true;

          //  }
            //rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
            //rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);
            //rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;
            rfsaSession.Configuration.BasicConfigurationList.TimerEventInterval = NF_VSTDriver.TimerEventInterval;
       


            rfsaSession.Utility.Commit();
            //   if (SA_ReferenceTrigger) SA_ReferenceTrigger = false;



            //if (SA_StartTrigger)
            //{

            //    rfsaSession.Configuration.Triggers.StartTrigger.Disable();
            //    SA_StartTrigger = false;
            //}
            //if (SA_AdvanceTriggerr)
            //{
            //    rfsaSession.Configuration.Triggers.AdvanceTrigger.Disable();
            //    SA_AdvanceTriggerr = false;
            //}
            //if (SA_ReferenceTrigger)
            //{
            //    rfsaSession.Configuration.Triggers.ReferenceTrigger.Disable(); //Disable Reference Trigger for NF Testing.
            //    SA_ReferenceTrigger = false;
            //}


        }
        private void ConfigureArbitraryWaveform(double SARefLevel)
        {
            // cache IQ rate //
            double IQRate = rfsgSession.Arb.IQRate;
            // create frequency ramp //
            FreqRamp = RampPattern(StartFrequency, StopFrequency, StepFrequency, NumberOfSteps);
            // create tone to upconvert //
            ComplexDouble[] Tone = CarrierWave(SamplesPerStep);
            // initialize arbitrary waveform //
            ComplexDouble[] ArbitraryWaveform = new ComplexDouble[SweepSamples];
            // sythesize waveform //
            for (int i = 0; i < NumberOfSteps; i++)
            {
                // calculate baseband frequency //
                double FrequencyOffset = FreqRamp[i] - CenterFrequency;
                // upconvert waveform //
                ComplexDouble[] UpconvertedTone = Upconvert(Tone, IQRate, FrequencyOffset, i == 0);
                // copy into arbitrary waveform //
                Array.Copy(UpconvertedTone, 0, ArbitraryWaveform, i * SamplesPerStep, SamplesPerStep);
            }
            // put key at the end of the waveform //
            ComplexDouble[] Key = Upconvert(Tone, IQRate, 0, false); // keeps phase coherent between key and last frequency
            Key = DigitalGain(Key, -10); // hardcoded key drop of 10 dBm
            ArbitraryWaveform = ArbitraryWaveform.Concat(Key).Concat(Key).ToArray();
            TotalSamples = SweepSamples + Tone.Length;
            // write arb waveform to rfsg //

            try
            {
                rfsgSession.Arb.ClearWaveform("LNAFrequencySweep");
            }
            catch
            {
            }
            finally
            {
                rfsgSession.Arb.WriteWaveform("LNAFrequencySweep", ArbitraryWaveform);
            }


            //   // tell rfsa how many samples to acquire //


            rfsaSession.Configuration.IQ.NumberOfSamples = TotalSamples;
            rfsaSession.Configuration.IQ.NumberOfRecords = 1;

            //// TODO: remove constant value of -20 dBm for reference level and have it set dynamically
            //// set RFSA reference level based on peak power of arbitrary waveform //
            //// double InherentReferenceLevel = RFSGPowerLevel; //  +MaxExpectedGain; #TODO:
            //// maximize dynamic range //
            //// double[] Magnitudes = ComplexDouble.GetMagnitudes(ArbitraryWaveform);
            //// double ReferenceLevelAdjustment = 10 * Math.Log10(Magnitudes.Max());
            //// set reference level of RFSA //
            rfsaSession.Configuration.Vertical.ReferenceLevel = SARefLevel; //= InherentReferenceLevel + ReferenceLevelAdjustment;
        }
        private void ConfigureArbitraryWaveform(double SARefLevel, int TotalNumber)
        {
         //   // cache IQ rate //
         //   double IQRate = rfsgSession.Arb.IQRate;
         //   // create frequency ramp //
         //   FreqRamp = RampPattern(StartFrequency, StopFrequency, StepFrequency, NumberOfSteps);
         //   // create tone to upconvert //
         //   ComplexDouble[] Tone = CarrierWave(SamplesPerStep);
         //   // initialize arbitrary waveform //
         //   ComplexDouble[] ArbitraryWaveform = new ComplexDouble[SweepSamples];
         //   // sythesize waveform //
         //   for (int i = 0; i < NumberOfSteps; i++)
         //   {
         //       // calculate baseband frequency //
         //       double FrequencyOffset = FreqRamp[i] - CenterFrequency;
         //       // upconvert waveform //
         //       ComplexDouble[] UpconvertedTone = Upconvert(Tone, IQRate, FrequencyOffset, i == 0);
         //       // copy into arbitrary waveform //
         //       Array.Copy(UpconvertedTone, 0, ArbitraryWaveform, i * SamplesPerStep, SamplesPerStep);
         //   }
         //   // put key at the end of the waveform //
         //   ComplexDouble[] Key = Upconvert(Tone, IQRate, 0, false); // keeps phase coherent between key and last frequency
         //   Key = DigitalGain(Key, -10); // hardcoded key drop of 10 dBm
         //   ArbitraryWaveform = ArbitraryWaveform.Concat(Key).Concat(Key).ToArray();
         //   TotalSamples = SweepSamples + Tone.Length;
         //   // write arb waveform to rfsg //

         ////   SetSeletedWaveform("LNAFrequencySweep13");
         //   try
         //   {
         //      rfsgSession.Arb.ClearWaveform("LNAFrequencySweep");
         //   }
         //   catch
         //   {
         //   }
         //   finally
         //   {
         //       rfsgSession.Arb.WriteWaveform("LNAFrequencySweep", ArbitraryWaveform);
         //   }
     
                
         //   // tell rfsa how many samples to acquire //


            rfsaSession.Configuration.IQ.NumberOfSamples = TotalNumber;
            rfsaSession.Configuration.IQ.NumberOfRecords = 1;

            //// TODO: remove constant value of -20 dBm for reference level and have it set dynamically
            //// set RFSA reference level based on peak power of arbitrary waveform //
            //// double InherentReferenceLevel = RFSGPowerLevel; //  +MaxExpectedGain; #TODO:
            //// maximize dynamic range //
            //// double[] Magnitudes = ComplexDouble.GetMagnitudes(ArbitraryWaveform);
            //// double ReferenceLevelAdjustment = 10 * Math.Log10(Magnitudes.Max());
            //// set reference level of RFSA //
            rfsaSession.Configuration.Vertical.ReferenceLevel = SARefLevel; //= InherentReferenceLevel + ReferenceLevelAdjustment;
        }
        private void SetSeletedWaveform(string Name)
        {

            string Script =
                     "script powerServo\r\n"
                   + "repeat forever\r\n"
                   + "generate LNAFrequencySweep13\r\n"
                   + "end repeat\r\n"
                   + "end script";
   
                Eq.Site[0]._EqVST.rfsgSession.Arb.Scripting.WriteScript(Script);

              //  rfsgSession.Arb.Scripting.WriteScript(Name);
       
        }
        private void ConfigureArbitraryWaveform()
        {
            // cache IQ rate //
            double IQRate = rfsgSession.Arb.IQRate;
            // create frequency ramp //
            FreqRamp = RampPattern(StartFrequency, StopFrequency, StepFrequency, NumberOfSteps);
            // create tone to upconvert //
            ComplexDouble[] Tone = CarrierWave(SamplesPerStep);
            // initialize arbitrary waveform //
            ComplexDouble[] ArbitraryWaveform = new ComplexDouble[SweepSamples];
            // sythesize waveform //
            for (int i = 0; i < NumberOfSteps; i++)
            {
                // calculate baseband frequency //
                double FrequencyOffset = FreqRamp[i] - CenterFrequency;
                // upconvert waveform //
                ComplexDouble[] UpconvertedTone = Upconvert(Tone, IQRate, FrequencyOffset, i == 0);
                // copy into arbitrary waveform //
                Array.Copy(UpconvertedTone, 0, ArbitraryWaveform, i * SamplesPerStep, SamplesPerStep);
            }
            // put key at the end of the waveform //
            ComplexDouble[] Key = Upconvert(Tone, IQRate, 0, false); // keeps phase coherent between key and last frequency
            Key = DigitalGain(Key, -10); // hardcoded key drop of 10 dBm
            ArbitraryWaveform = ArbitraryWaveform.Concat(Key).Concat(Key).ToArray();
            TotalSamples = SweepSamples + Tone.Length;
            // write arb waveform to rfsg //
            try
            {
                rfsgSession.Arb.WriteWaveform("LNAFrequencySweep", ArbitraryWaveform);
    
            }
            catch
            {
            }
            finally
            {
               rfsgSession.Arb.WriteWaveform("LNAFrequencySweep", ArbitraryWaveform);
            }
            // tell rfsa how many samples to acquire //
         //   rfsaSession.Configuration.IQ.NumberOfSamples = TotalSamples;
        //    rfsaSession.Configuration.IQ.NumberOfRecords = 1;

            ////// TODO: remove constant value of -20 dBm for reference level and have it set dynamically
            ////// set RFSA reference level based on peak power of arbitrary waveform //
            ////// double InherentReferenceLevel = RFSGPowerLevel; //  +MaxExpectedGain; #TODO:
            ////// maximize dynamic range //
            ////// double[] Magnitudes = ComplexDouble.GetMagnitudes(ArbitraryWaveform);
            ////// double ReferenceLevelAdjustment = 10 * Math.Log10(Magnitudes.Max());
            ////// set reference level of RFSA //
          //  rfsaSession.Configuration.Vertical.ReferenceLevel = SARefLevel; //= InherentReferenceLevel + ReferenceLevelAdjustment;
        }
        public void writeWaveForm(string Name, ComplexDouble[] Data)
        {
     
                rfsgSession.Arb.WriteWaveform("LNAFrequencySweep" + Name, Data);

        
        }
        private void ConfigureScript(string Name)
        {
            string script = "script LNASweep\n" + "generate LNAFrequencySweep" + Name + " marker1(0)\n" + "end script";
            rfsgSession.Arb.Scripting.WriteScript(script);
            // added 10/26 - SMM: May need to also be added to NF test //
            rfsgSession.Arb.Scripting.SelectedScriptName = "LNASweep";
        }
        private void CommitConfiguration()
        {
            rfsaSession.Utility.Commit();
            rfsgSession.Utility.Commit();
        }
        private void Initiate()
        {
          //  rfsaSession.Acquisition.IQ.Initiate();
            rfsgSession.Initiate();
        }
        private void MeasurePower(int TotalNumber, out double[] pwrdata)
        {
            // fetch data from RFSA //
            ComplexDouble[] ComplexData;

            Stopwatch tTime111 = new Stopwatch();

            double testtime1 = 0f;
            double testtime2 = 0f;
            double testtime3 = 0f;
            double testtime4 = 0f;

            tTime111.Reset();
            tTime111.Start();

            pwrdata = new double[NumberOfSteps];
            rfsaSession.Acquisition.IQ.FetchIQSingleRecordComplex(0, TotalNumber, new PrecisionTimeSpan(10), out ComplexData);
            testtime1 = tTime111.ElapsedMilliseconds;


            if (ComplexData.Length != 0)
            {

                double[] Magnitudes = ComplexDouble.GetMagnitudes(ComplexData);
                double[] PowerTrace = new double[Magnitudes.Length];

                // convert to power //
                Parallel.For(0, ComplexData.Length, i =>
                {
                    PowerTrace[i] = 20 * Math.Log10(Magnitudes[i]) + 10;
                });
                testtime2 = tTime111.ElapsedMilliseconds;


                // move backward through key until a power edge is detected //
                int index = PowerTrace.Length - 1;
                double dBThreshold = 0.70 * 10 + PowerTrace[index]; // Threshold chosen as 75% of the key drop which is set at 10 - Org : double dBThreshold = 0.75 * 10 + PowerTrace[index];
                for (; index >= 0; index--)
                {
                    if (PowerTrace[index] > dBThreshold)
                    {
                        break;
                    }
                }
                testtime3 = tTime111.ElapsedMilliseconds;
                // increment index to get sample number rather than index //
                int PowerEdgeSampleNumber = index++;
                // grab the data up to the power edge //
                PowerTrace = PowerTrace.Skip(PowerEdgeSampleNumber - SweepSamples).Take(SweepSamples).ToArray();
                double[] GainTrace = new double[NumberOfSteps];
                pwrdata = new double[NumberOfSteps];
                for (int i = 0; i < NumberOfSteps; i++)
                {
                    // get sample subsets //
                    int SamplesToSkip = i * SamplesPerStep;
                    double[] PowerTraceSubset = PowerTrace.Skip(SamplesToSkip).Take(SamplesPerStep).ToArray();
                    // add padding to each side //
                    int SamplePaddingStart = 10;
                    int SamplePaddingEnd = PowerTraceSubset.Length - 2 * SamplePaddingStart;
                    PowerTraceSubset = PowerTraceSubset.Skip(SamplePaddingStart).Take(SamplePaddingEnd).ToArray();
                    GainTrace[i] = PowerTraceSubset.Average() - RFSGPowerLevel;         //Gain Data
                    pwrdata[i] = PowerTraceSubset.Average();                            //POut Data (dBm)
                }
                testtime4 = tTime111.ElapsedMilliseconds;
                tTime111.Stop();
            }

        }
        private void MeasurePower(out double[] pwrdata)
        {
            // fetch data from RFSA //
            ComplexDouble[] ComplexData;
            pwrdata = new double[NumberOfSteps];
            rfsaSession.Acquisition.IQ.FetchIQSingleRecordComplex(0, TotalSamples, new PrecisionTimeSpan(10), out ComplexData);


                if (ComplexData.Length != 0)
                {

                    double[] Magnitudes = ComplexDouble.GetMagnitudes(ComplexData);
                    double[] PowerTrace = new double[Magnitudes.Length];
                    // convert to power //
                    Parallel.For(0, ComplexData.Length, i =>
                    {
                        PowerTrace[i] = 20 * Math.Log10(Magnitudes[i]) + 10;
                    });
                    // move backward through key until a power edge is detected //
                    int index = PowerTrace.Length - 1;
                    double dBThreshold = 0.70 * 10 + PowerTrace[index]; // Threshold chosen as 75% of the key drop which is set at 10 - Org : double dBThreshold = 0.75 * 10 + PowerTrace[index];
                    for (; index >= 0; index--)
                    {
                        if (PowerTrace[index] > dBThreshold)
                        {
                            break;
                        }
                }
                // increment index to get sample number rather than index //
                int PowerEdgeSampleNumber = index++;
                // grab the data up to the power edge //
                PowerTrace = PowerTrace.Skip(PowerEdgeSampleNumber - SweepSamples).Take(SweepSamples).ToArray();
                double[] GainTrace = new double[NumberOfSteps];
                pwrdata = new double[NumberOfSteps];
                for (int i = 0; i < NumberOfSteps; i++)
                {
                    // get sample subsets //
                    int SamplesToSkip = i * SamplesPerStep;
                    double[] PowerTraceSubset = PowerTrace.Skip(SamplesToSkip).Take(SamplesPerStep).ToArray();
                    // add padding to each side //
                    int SamplePaddingStart = 10;
                    int SamplePaddingEnd = PowerTraceSubset.Length - 2 * SamplePaddingStart;

                    double sum = 0.0;

                    for (int j = SamplePaddingStart; j < SamplePaddingStart + SamplePaddingEnd; j++)
                    {
                        sum += PowerTraceSubset[j];
                    }

                    //PowerTraceSubset = PowerTraceSubset.Skip(SamplePaddingStart).Take(SamplePaddingEnd).ToArray();
                    GainTrace[i] = PowerTraceSubset.Average() - RFSGPowerLevel;         //Gain Data
                    pwrdata[i] = PowerTraceSubset.Average();                            //POut Data (dBm)
                }



            }
  


        }
        public void ReConfigVST()//Seoul
        //private void ReConfigVST() //Original
        {
            //rfsgSession.Arb.ClearWaveform("LNAFrequencySweep");

       //    SA_StartTrigger = true;
       //     SA_AdvanceTrigger = true;

            //rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
            //rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;
            //rfsaSession.Configuration.BasicConfigurationList.TimerEventInterval = NF_VSTDriver.TimerEventInterval;

            //   rfsaSession.Utility.Reset();
            //   rfsgSession.Utility.Reset();
            //      rfsgSession.Abort();


        }
        private void SaveToCSV(double[] data, string path)
        {
            string dataCSV = string.Join("\n", data);
            System.IO.File.WriteAllText(path, dataCSV);
        }
        private double[] RampPattern(double Start, double Stop, double Step, int NumberOfSteps)
        {
            if (NumberOfSteps < 2)
            {
                throw new ArgumentException("Number of steps must be greater than or equal to 2.");
            }
            if (Stop < Start)
            {
                throw new ArgumentException("Stop value must be greater than or equal to start value");
            }
            double[] Ramp = new double[NumberOfSteps];
            double RampDelta = Step;
            double RampValue = Start;
            for (int i = 0; i < NumberOfSteps; i++)
            {
                Ramp[i] = RampValue;
                RampValue += RampDelta;

                if (Ramp[i] > StopFrequency)
                    Ramp[i] = StopFrequency;
            }
            return Ramp;
        }
        private ComplexDouble[] CarrierWave(int NumberOfSamples)
        {
            double[] ones = Enumerable.Repeat(1.0, NumberOfSamples).ToArray();
            double[] zeros = Enumerable.Repeat(0.0, NumberOfSamples).ToArray();
            return ComplexDouble.ComposeArrayPolar(ones, zeros);
        }
        private ComplexDouble[] Upconvert(ComplexDouble[] ComplexData, double SampleRate, double CarrierFrequency, bool reset)
        {
            if (reset)
            {
                MixerPhase = 0;
            }
            double dt = 1 / SampleRate;
            double MixerDeltaPhase = 2 * Math.PI * CarrierFrequency * dt;
            ComplexDouble[] UpconvertedData = new ComplexDouble[ComplexData.Length];
            for (int i = 0; i < ComplexData.Length; i++)
            {
                ComplexDouble Mixer = ComplexDouble.FromPolar(1, MixerPhase);
                UpconvertedData[i] = ComplexData[i].Multiply(Mixer);
                MixerPhase += MixerDeltaPhase;
            }
            return UpconvertedData;
        }
        /* This is a parallel implmentation of the upconvert function.  It may run faster in some situations.  However, 
         since it relies on multiplication operations, it may run slower than the serial implimentation. */
        private ComplexDouble[] ThreadSafeUpconvert(ComplexDouble[] ComplexData, double SampleRate, double CarrierFrequency, bool reset)
        {
            if (reset)
            {
                MixerPhase = 0;
            }
            // cache local copy of MixerPhase //
            double InitialPhase = MixerPhase;
            // get mixer deltas //
            double dt = 1 / SampleRate;
            double MixerDeltaPhase = 2 * Math.PI * CarrierFrequency * dt;
            // new array for returning data //
            ComplexDouble[] UpconvertedData = new ComplexDouble[ComplexData.Length];
            // perform parallel upconversion //
            Parallel.For(0, ComplexData.Length, i =>
            {
                // get phase of mixer depending on what iteration the thread is on //
                double ThreadMixerPhase = i * MixerDeltaPhase + InitialPhase;
                ComplexDouble Mixer = ComplexDouble.FromPolar(1, ThreadMixerPhase);
                UpconvertedData[i] = ComplexData[i].Multiply(Mixer);
                // thread that does last element is responsible for updating the mixer phase for next function call //
                if (i == ComplexData.Length - 1)
                {
                    MixerPhase = ThreadMixerPhase;
                }
            });
            return UpconvertedData;
        }
        private double[] DeltaPhase(ComplexDouble[] ComplexData)
        {
            if (ComplexData == null)
            {
                throw new ArgumentNullException("Complex data array must be initialized.");
            }
            ComplexDouble[] DataNeighbors = ComplexData.Skip(1).ToArray();
            double[] DeltaPhase = new double[DataNeighbors.Length];
            Parallel.For(0, DataNeighbors.Length, i =>
            {
                DeltaPhase[i] = ComplexData[i].ComplexConjugate.Multiply(DataNeighbors[i]).Phase;
            });
            return DeltaPhase;
        }
        // rfsg must be in peak power mode //
        private ComplexDouble[] DigitalGain(ComplexDouble[] UnscaledData, double dBGain)
        {
            ComplexDouble[] ScaledData = new ComplexDouble[UnscaledData.Length];
            double DigitalGain = Math.Sqrt(Math.Pow(10, dBGain / 10));
            ComplexDouble ComplexDigitalGain = ComplexDouble.FromPolar(DigitalGain, 0);
            Parallel.For(0, ScaledData.Length, i =>
            {
                ScaledData[i] = UnscaledData[i].Multiply(ComplexDigitalGain);
            });
            return ScaledData;
        }
        #endregion

        #region Power Ramp

        // Step 1: Initialize power sweeping test
        PowerRampingTest t = new PowerRampingTest();

        public void PowerRamp(string strWaveform, string strWaveformName,
                                double frequencyHz, double transientTimeS, int transientSteps, double dwellTimeS, int rampDownSteps,
                                double startPwrLevel, double stopPwrLevel)
        {
            //find modulation array no
            VST_WAVEFORM_MODE ModulationType;
            ModulationType = (VST_WAVEFORM_MODE)Enum.Parse(typeof(VST_WAVEFORM_MODE), strWaveformName.ToUpper());
            int arrayNo = (int)Enum.Parse(ModulationType.GetType(), ModulationType.ToString());         //to get the int value from System.Enum
            string I_Path = NF_VSTDriver.SignalType[arrayNo].SG_IPath;
            string Q_Path = NF_VSTDriver.SignalType[arrayNo].SG_QPath;
            double iqRate = NF_VSTDriver.SignalType[arrayNo].SG_IQRate;

            try
            {
                // Step 2: Build waveform for the power sweeeping test                   
                if (NF_VSTDriver.SignalType[arrayNo].signalMode == "CW")
                {
                    // Method 2 -- Build waveform based on CW signal
                    t.BuildCwSweepWaveforms(frequencyHz, iqRate,
                        transientTimeS, transientSteps, dwellTimeS, rampDownSteps,
                        startPwrLevel, stopPwrLevel);
                }
                else
                {
                    // Method 1 -- Build waveform based on LTE waveform from file  
                    StreamReader iReader = new StreamReader(File.OpenRead(I_Path));
                    StreamReader qReader = new StreamReader(File.OpenRead(Q_Path));
                    List<ComplexDouble> iqData = new List<ComplexDouble>();
                    while (!(iReader.EndOfStream || qReader.EndOfStream))
                    {
                        double i = double.Parse(iReader.ReadLine());
                        double q = double.Parse(qReader.ReadLine());
                        iqData.Add(new ComplexDouble(i, q));
                    }
                    t.BuildSweepWaveforms(iqData.ToArray(), frequencyHz, iqRate,
                        transientTimeS, transientSteps, dwellTimeS, rampDownSteps,
                        startPwrLevel, stopPwrLevel);
                }

                // Step 3: Call configure to commit configuration into the instruments
                t.Configure(rfsgSession);

                // Step 4: Initiate Generation 
                t.Initiate();

                t.Wait();
                t.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in PowerRamp():\n" + ex.GetType() + ": " + ex.Message);
            }
        }

        #endregion

        public void RfsaSelfCalibrateRange(long Steps_To_Omit, double Min_Frequency, double Max_Frequency, double Min_Reference_Level, double Max_Reference_Level)
        {
            // Get handle pointer from RFSA session based on .NET API library
            System.IntPtr Rfsaptr = rfsaSession.DangerousGetInstrumentHandle();

            // niRFSA is .NET wrapper class to call the non-exposed SelfCalibrateRange()
            // Pass the handle pointer to this wrapper class
            niRFSA rfsa2 = new niRFSA(Rfsaptr);
            int status = rfsa2.SelfCalibrateRange(Steps_To_Omit, Min_Frequency, Max_Frequency, Min_Reference_Level, Max_Reference_Level);

            rfsa2.Dispose();

            // When rfsa2 is disposed, the closing of instrument session will be called.
            // Thus the current RfsaHandle need to re-connect 
            rfsaSession = null;
            this.Initialize();
        }

        public void VST_SelfCalibration(double startFreq, double stopFreq)
        {
            RfsaSelfCalibrateRange(0, startFreq, stopFreq, -90, 0);
            rfsgSession.Calibration.Self.SelfCalibrateRange(RfsgSelfCalibrationSteps.OmitNone, startFreq, stopFreq, -30, 5);
        }

        //public void VSGInitiate(object o)
        //{
        //    rfsgSession.Initiate();
        //    DoneVSGinit.Set();
        //}

        public void VSGInitiate(object o)
        {
            Config_SG vsg = new Config_SG();
            vsg = (Config_SG)o;

            if (vsg.HotIteration == 0)
            {
             
                rfsgSession.Arb.Scripting.WriteScript(vsg.TxPAOnScript);
                rfsgSession.Arb.Scripting.SelectedScriptName = "powerServo";
                Eq.Site[0]._EqVST.Get_s_SignalType(vsg.Modulation , vsg.WaveFormName, out SG_IQRate);
                rfsgSession.Arb.IQRate = SG_IQRate;
                rfsgSession.RF.PowerLevel = vsg.SgPin;
                rfsgSession.RF.Frequency = vsg.TxPAOnFreq * 1e6; //added 

                rfsgSession.Initiate();

         //       rfsgSession.Initiate();
            }
            else
            {
                rfsgSession.RF.Frequency = vsg.TxPAOnFreq * 1e6; //added 
            }

            DoneVSGinit.Set();
        }

        //public double MeasureChanPower(bool byCalc = true, double CenterFrequency, double ReferenceChannelBW
        public double MeasureChanPower(double CenterFrequency, double ReferenceChannelBW, double ResolutionBW)
        {
            rfsaSession.Acquisition.IQ.Abort();

            rfsaSession.Configuration.AcquisitionType = RfsaAcquisitionType.Spectrum;

            rfsaSession.Configuration.Spectrum.ConfigureSpectrumFrequencyCenterSpan(CenterFrequency, ReferenceChannelBW);

            rfsaSession.Configuration.Spectrum.ResolutionBandwidth = ResolutionBW;

            //rfsaSession.Configuration.Triggers.ReferenceTrigger.Export.OutputTerminal = (niRFSAConstants.None);

            rfsaSession.Utility.Commit();

            int numSpectrumLines = 0;

            numSpectrumLines = rfsaSession.Configuration.Spectrum.NumberOfSpectralLines;

            double[] powerSpectrum = new double[numSpectrumLines];
            //niRFSA_spectrumInfo spectruminfo;
            powerSpectrum = rfsaSession.Acquisition.Spectrum.ReadPowerSpectrum(new PrecisionTimeSpan(10), out NationalInstruments.ModularInstruments.NIRfsa.RfsaSpectrumInfo spectruminfo);

            //for (int i = 0; i < powerSpectrum.Length; i++)
            //{
            //    double freq = spectruminfo.initialFrequency + spectruminfo.frequencyIncrement * i;
            //    Console.WriteLine(freq + "\t" + powerSpectrum[i]);
            //}

            double divine_bw = (rfsaSession.Configuration.Spectrum.Span) / (rfsaSession.Configuration.Spectrum.ResolutionBandwidth);

            double totalPower = 0;

            for (int i = 0; i < powerSpectrum.Length; i++)
            {
                totalPower += Math.Pow(10.0, (powerSpectrum[i]/10));
            }
            //totalPower =  Math.Log10(totalPower);

            totalPower = 10 * Math.Log10( divine_bw *((1.00000 / powerSpectrum.Length) * totalPower)); 

           // rfsaSession.Configuration.AcquisitionType = (RfsaAcquisitionType.IQ);
           //TriggerOut = TriggerLine.PxiTrig1;
            return totalPower;
        }

        public ComplexDouble[] MeasureIqTrace(bool Initiated)
        {
            int NumberOfSamples = (int)1E6; // CW IQ RATE = 1E6
            ComplexDouble[] Data = new ComplexDouble[NumberOfSamples];

            //niRFSA_wfmInfo wfmInfo;

            int error = 0;

            try
            {
                if (Initiated)
                {
                    rfsaSession.Acquisition.IQ.FetchIQSingleRecordComplex(0, NumberOfSamples, new PrecisionTimeSpan(10), out NationalInstruments.ModularInstruments.NIRfsa.RfsaWaveformInfo wfmInfo, out Data);
                }
                else
                {
                    Data = rfsaSession.Acquisition.IQ.ReadIQSingleRecordComplex(new PrecisionTimeSpan(10), out NationalInstruments.ModularInstruments.NIRfsa.RfsaWaveformInfo wfmInfo);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("RF trace measurement timed out.\n\n" + e.ToString());
            }

            rfsaSession.Acquisition.IQ.Abort();

            return Data;
        }

        public void Thread_Config_SG(object TestNum)
        {
            rfsgSession.Arb.IQRate = IQRate;
            SamplesPerStep = (int)Math.Ceiling(dwellT * IQRate);
            SweepSamples = SamplesPerStep * NumberOfSteps;

         //   ConfigureArbitraryWaveform();
            ConfigureScript(Convert.ToString(TestNum));

            rfsgSession.RF.Configure(CenterFrequency, RFSGPowerLevel);
 

            rfsgSession.Utility.Commit();
            rfsgSession.Utility.WaitUntilSettled(10);

            threadFlags[1].Set();
        }
        public void Thread_Config_SA(object SARefLevel)
        {
            SamplesPerStep = (int)Math.Ceiling(dwellT * IQRate);
            SweepSamples = SamplesPerStep * NumberOfSteps;




            rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);

            rfsaSession.Configuration.Triggers.AdvanceTrigger.Disable();

            rfsaSession.Configuration.Triggers.StartTrigger.Disable();




            rfsaSession.Configuration.IQ.CarrierFrequency = CenterFrequency;    //Seoul amplitude offset to NF
            rfsaSession.Configuration.IQ.IQRate = IQRate;


            rfsaSession.Configuration.IQ.NumberOfSamples = TotalNumber;
            rfsaSession.Configuration.IQ.NumberOfRecords = 1;


            rfsaSession.Configuration.Vertical.ReferenceLevel = Convert.ToDouble(SARefLevel); //= InherentReferenceLevel + ReferenceLevelAdjustment;
        //    rfsaSession.Utility.Commit();
            rfsaSession.Acquisition.IQ.Initiate();
            //  rfsaSession.Acquisition.IQ.Initiate();
            threadFlags[0].Set();

        }
        public void Thread_Ininite_SG(object i)
        {
            rfsgSession.Initiate();
            threadFlags[1].Set();
        }
        public void Thread_Ininite_SA(object i)
        {
            rfsaSession.Acquisition.IQ.Initiate();
            threadFlags[0].Set();
        }

        private bool _SA_StartTrigger;
        private bool _SA_AdvanceTrigger;
        private bool _SA_ReferenceTrigger;

        public bool SA_StartTrigger
        {
            get
            {
                return _SA_StartTrigger;
            }

            set
            {
                //if (value)
                //{

                //    rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
                //}
                //else
                //{
                //    rfsaSession.Configuration.Triggers.StartTrigger.Disable();
                //}

                _SA_StartTrigger = value;
            }
        }
        public bool SA_AdvanceTrigger
        {
            get
            {
                return _SA_AdvanceTrigger;
            }

            set
            {
                //if (value)
                //{
                //    rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;
                //}
                //else
                //{
                //    rfsaSession.Configuration.Triggers.AdvanceTrigger.Disable();
                //}

                _SA_AdvanceTrigger = value;
            }
        }
        public bool SA_ReferenceTrigger
        {
            get
            {
                return _SA_ReferenceTrigger;
            }

            set
            {
                //if (value)
                //{

                //    rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NF_VSTDriver.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NF_VSTDriver.RFSA_PreTriggerSamples);

                //}
                //else
                //{
                //    rfsaSession.Configuration.Triggers.ReferenceTrigger.Disable();
                //}

                _SA_ReferenceTrigger = value;
            }
        }

        public class Config_SG
        {
            public int Iteration;
            public int HotIteration;
            public string TxPAOnScript;
            public string Modulation;
            public string WaveFormName;
            public double SgPin;
            public double TxPAOnFreq;

            public Config_SG()
            {

            }

            public Config_SG(int Iteration, int HotIteration, string TxPAOnScript, string Modulation, string WaveFormName, double SgPin, double TxPAOnFreq)
            {
                this.Iteration = Iteration;
                this.HotIteration = HotIteration;
                this.TxPAOnScript = TxPAOnScript;
                this.Modulation = Modulation;
                this.WaveFormName = WaveFormName;
                this.SgPin = SgPin;
                this.TxPAOnFreq = TxPAOnFreq;
            }
        }
    }

    public static class NoiseTestFileUtilities
    {
        public static List<double> File_ReadData(int NumberOfSamples, string FilePath)
        {
            try
            {
                var Data = new List<double>();

                var SGdataReader = new StreamReader(File.OpenRead(FilePath));

                while (!SGdataReader.EndOfStream)
                {
                    Data.Add(Convert.ToDouble(SGdataReader.ReadLine()));
                }

                return Data;
            }

            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(@"Data file not found.");
            }
        }

        public static List<double> File_ReadData(string FilePath)
        {
            try
            {
                var Data = new List<double>();

                var SGdataReader = new StreamReader(File.OpenRead(FilePath));

                while (!SGdataReader.EndOfStream)
                {
                    Data.Add(Convert.ToDouble(SGdataReader.ReadLine()));
                }

                return Data;
            }

            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException(ex + @"Data file not found.");
            }
        }
    }

}
