using System;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;

namespace LibEqmtDriver.NF_VST
{
    public class NF_VSTDriver
    {
        //***Non-Driver Specific***
        public const string ReferenceClockSource = "PXI_CLK";
        public const double IdleTime = 100e-6; //s  .5e-3
        public const double Leading_Script_Idle_Length = 100e-6; // 25e-6 #NJKMOD
        public const double RefTrig_Marker_Delay = 15e-6; //s Delay with respect to the beginning of modulated waveform.
        public const float PI = (float)Math.PI;

        //***RFSG***        
        //CDMA2K
        //public const double SG_IQRate_CDMA = 4.9152e6;
        //public const string SG_IPath_CDMA = @"C:\Avago.ATF.Common\Input\Waveform\CDMA2K_RC1\I_Data.txt";
        //public const string SG_QPath_CDMA = @"C:\Avago.ATF.Common\Input\Waveform\CDMA2K_RC1\Q_Data.txt";
        ////LTE 10M50RB
        //public const double SG_IQRate_LTE = 15.36e6;
        //public const string SG_IPath_LTE = @"C:\Avago.ATF.Common\Input\Waveform\LTE10M50RB\I_LTE_QPSK_10M50RB_091215.txt";
        //public const string SG_QPath_LTE = @"C:\Avago.ATF.Common\Input\Waveform\LTE10M50RB\Q_LTE_QPSK_10M50RB_091215.txt";

        public const string RFSG_MarkerEvents_2_ExportedOutputTerminal = "PXI_Trig2";
        public const string RFSG_MarkerEvents_1_ExportedOutputTerminal = "PXI_Trig1";
        public const string RFSG_ConfigurationListStepTrigger_ExportedOutputTerminal = "PXI_Trig0";
        public const double ArbPreFilterGain = -3;
        public const RfsgLoopBandwidth loopBandwidth = RfsgLoopBandwidth.High;
        public static readonly RfsgConfigurationListProperties[] RFSG_PropertyList = new RfsgConfigurationListProperties[] { RfsgConfigurationListProperties.Frequency, (RfsgConfigurationListProperties)1154098 }; //1154098 corresponds to Property ID for Upconverter Center Frequency
        public const Boolean UseWaveformFile = true;

        //***RFSA***        

        //public const double RX_IQRate_CDMA = 1e6;//TODO: Remove?
        //public const double RX_IQRate_LTE = 8e6;//TODO: Remove?
        public const double RFSA_LO_Offset = 60e6;
        public const string RFSA_StartTrigger_DigitalEdge_Source = "PXI_Trig2";
        public const string RFSA_ReferenceTrigger_DigitalEdge_Source = "PXI_Trig1";
        public const long RFSA_PreTriggerSamples = 0;
        public const double TimerEventInterval = 5e-5;
        public static readonly RfsaConfigurationListProperties[] RFSA_PropertyList = new RfsaConfigurationListProperties[] { RfsaConfigurationListProperties.IQCarrierFrequency, RfsaConfigurationListProperties.DownconverterCenterFrequency };

        public static s_SignalType[] SignalType;
        public const string SG_Path = @"C:\Avago.ATF.Common\Input\Waveform\";
    }

    public enum VST_WAVEFORM_MODE
    {
        CW,
        CDMA2K,
        CDMA2KRC1,
        CDMA2KCUSTOM,
        GSM850,
        GSM900,
        GSM1800,
        GSM1900,
        GSM850A,
        GSM900A,
        GSM1800A,
        GSM1900A,
        IS95A,
        IS98,
        WCDMA,
        WCDMAUL,
        WCDMAGTC1,
        WCDMACUSTOM,
        LTETD5M8RB,
        LTETD10M12RB,
        LTETD10M50RB,
        LTE10M1RB,
        LTE10M12RB,
        LTE10M20RB,
        LTE10M50RB,
        LTE10M48RB,
        LTE10MCUSTOM,
        LTE15M75RB,
        LTE15MCUSTOM,
        LTE5M25RB,
        LTE5M8RB,
        LTE5MCUSTOM,
        LTE20M100RB,
        LTE20M18RB,
        LTE20M48RB,
        LTE20MCUSTOM,
        NCFUQSC15B5M25R0S3X,
        LFUQ10M50R0SM6REF,
        LFUQ5M25R0SM5REF,
        LFUQ_10M25R25SM5_NTS3X_IV8_CFR4P2
    }

    public struct s_SignalType
    {
        public bool status;
        public double SG_IQRate;
        public int signalLength;
        public string signalMode;
        public string SG_IPath;
        public string SG_QPath;
        public double SG_papr_dB;
        public double SignalBandwidth;
    }

    public struct S_MutSignal_Setting
    {
        public bool enable;
        public double total_time_sec;
        public double mod_time_sec;
        public double mod_offset_sec;
        public double freq_offset_hz;
        public double f_off_delay_sec;
    }

    public struct S_MultiRBW_Data
    {
        public double RBW_Hz;
        public double[,] rsltTrace;
        public double[] rsltMaxHoldTrace;
        public double[,] multiTraceData;
    }
}
