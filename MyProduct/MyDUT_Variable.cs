using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyProduct
{
    public struct s_EqmtStatus
    {
        public bool MXG01;
        public bool MXG02;
        public bool MXA01;
        public bool MXA02;
        public bool SMU;
        public string SMU_CH;
        public bool DC;
        public string DC_CH;
        public bool Switch;
        public bool MIPI;
        public bool PM;
        public bool TuneFilter;
        public bool DC_1CH;
        public bool[] DCSupply;
        public bool PXI_VST;
        public bool handler;
        public bool MIPI_PPMU;
    }
    
    public struct s_TraceData
    {
        public string TestNumber;
        public bool Enable;
        public int TraceCount;
        public s_TraceNo[][] Multi_Trace;
        public bool SoakSweep;
    }
    public struct s_TraceNo
    {
        public string MXA_No;
        public int NoPoints;
        public double RBW_Hz;
        public double[] FreqMHz;
        public double[] Ampl;
        public string Result_Header;
        public double[] RxGain; //Yoonchun
        // new
        public double TargetPout;
        public string modulation;
        public string waveform;
    }
    public struct s_Result
    {
        public string TestNumber;
        public bool Enable;
        public s_mRslt[] Multi_Results;
        public int Misc;
    }
    public struct s_mRslt
    {
        public bool Enable;
        public string Result_Header;
        public double Result_Data;
    }
    public struct s_OffBias_AfterTest
    {
        public bool DC;
        public bool SMU;
    }
    public struct s_SNPFile
    {
        public string FileOutput_Path;
        public string FileOutput_FileName;
        public string FileOutput_Mode;
        public bool FileOutput_Enable;
        public int FileOutput_Count;
    }
    public struct s_SNPDatalog
    {
        public int Number;
        public bool Status;
        public string SNPFileName;
    }
    public struct s_StopOnFail
    {
        public bool Enable;
        public bool TestFail;
    }

    #region static variable
    public enum e_ResultTag
    {
        NF1_AMPL = 0,
        NF2_AMPL,
        NF1_FREQ,
        NF2_FREQ,
        HARMONIC_AMPL,
        HARMONIC_FREQ,
        PIN,
        POUT,
        PIN1,
        POUT1,
        PIN2,
        POUT2,
    }
    #endregion

    #region dynamic variable
    #endregion

    public static class TCF_Header
    {
        #region TCF Header
        public const string ConstTestNum = "Test Number";
        public const string ConstTestMode = "Test Mode";
        public const string ConstTestParam = "Test Parameter";
        public const string ConstParaName = "Parameter Name";
        public const string ConstUsePrev = "Use Previous";
        public const string ConstColdTrace = "Display_ColdTrace";

        //Single Freq Condition
        public const string ConstPout = "Pout";
        public const string ConstPin = "Pin";
        public const string ConstTXBand = "TX_Band";
        public const string ConstTXFreq = "TX_Freq";
        public const string ConstRXBand = "RX_Band";
        public const string ConstRXFreq = "RX_Freq";
        public const string ConstTunePwr_TX = "TunePwr_TX";

        //Sweep Freq Condition
        public const string ConstPout1 = "Pout1";
        public const string ConstPin1 = "Pin1";
        public const string ConstTX1Band = "TX1_Band";
        public const string ConstStartTXFreq1 = "Start_TXFreq1";
        public const string ConstStopTXFreq1 = "Stop_TXFreq1";
        public const string ConstStepTXFreq1 = "Step_TXFreq1";
        public const string ConstDwellTime1 = "Dwell_Time1";
        public const string ConstTunePwr_TX1 = "TunePwr_TX1";

        public const string ConstPout2 = "Pout2";
        public const string ConstPin2 = "Pin2";
        public const string ConstTX2Band = "TX2_Band";
        public const string ConstStartTXFreq2 = "Start_TXFreq2";
        public const string ConstStopTXFreq2 = "Stop_TXFreq2";
        public const string ConstStepTXFreq2 = "Step_TXFreq2";
        public const string ConstDwellTime2 = "Dwell_Time2";
        public const string ConstTunePwr_TX2 = "TunePwr_TX2";

        public const string ConstRX1Band = "RX1_Band";
        public const string ConstStartRXFreq1 = "Start_RXFreq1";
        public const string ConstStopRXFreq1 = "Stop_RXFreq1";
        public const string ConstStepRXFreq1 = "Step_RXFreq1";
        public const string ConstRX1SweepT = "RX1_SweepTime";
        public const string ConstSetRX1NDiag = "RX1_NDiag_Mode";
        public const string ConstRX2Band = "RX2_Band";
        public const string ConstStartRXFreq2 = "Start_RXFreq2";
        public const string ConstStopRXFreq2 = "Stop_RXFreq2";
        public const string ConstStepRXFreq2 = "Step_RXFreq2";
        public const string ConstRX2SweepT = "RX2_SweepTime";
        public const string ConstSetRX2NDiag = "RX2_NDiag_Mode";

        public const string PXI_MultiRBW = "PXI_MultiRBW";
        public const string PXI_NoOfSweep = "PXI_NoOfSweep";
        public const string ConstPoutTolerance = "Pout_Tolerance";
        public const string ConstPinTolerance = "Pin_Tolerance";
        public const string ConstPowerMode = "PowerMode";
        public const string ConstCalTag = "Calibration Tag";
        public const string ConstSwBand = "Switching Band";
        public const string ConstModulation = "Modulation";
        public const string ConstWaveformName = "WaveFormName";
        public const string ConstSetFullMod = "Full_Mod";

        //SMU Header
        public const string ConstSMUSetCh = "SMUSet_Channel";
        public const string ConstSMUMeasCh = "SMUMeasure_Channel";
        public const string ConstSMUVCh0 = "SMUV_CH0";
        public const string ConstSMUVCh1 = "SMUV_CH1";
        public const string ConstSMUVCh2 = "SMUV_CH2";
        public const string ConstSMUVCh3 = "SMUV_CH3";
        public const string ConstSMUVCh4 = "SMUV_CH4";
        public const string ConstSMUVCh5 = "SMUV_CH5";
        public const string ConstSMUVCh6 = "SMUV_CH6";
        public const string ConstSMUVCh7 = "SMUV_CH7";
        public const string ConstSMUVCh8 = "SMUV_CH8";

        public const string ConstSMUICh0Limit = "SMUI_CH0";
        public const string ConstSMUICh1Limit = "SMUI_CH1";
        public const string ConstSMUICh2Limit = "SMUI_CH2";
        public const string ConstSMUICh3Limit = "SMUI_CH3";
        public const string ConstSMUICh4Limit = "SMUI_CH4";
        public const string ConstSMUICh5Limit = "SMUI_CH5";
        public const string ConstSMUICh6Limit = "SMUI_CH6";
        public const string ConstSMUICh7Limit = "SMUI_CH7";
        public const string ConstSMUICh8Limit = "SMUI_CH8";

        //DC Header
        public const string ConstDCSetCh = "DCSet_Channel";
        public const string ConstDCMeasCh = "DCMeasure_Channel";
        public const string ConstDCPsSet = "DC_PS_Set";
        public const string ConstDCVCh1 = "DCV_CH1";
        public const string ConstDCVCh2 = "DCV_CH2";
        public const string ConstDCVCh3 = "DCV_CH3";
        public const string ConstDCVCh4 = "DCV_CH4";
        public const string ConstDCICh1Limit = "DCI_CH1";
        public const string ConstDCICh2Limit = "DCI_CH2";
        public const string ConstDCICh3Limit = "DCI_CH3";
        public const string ConstDCICh4Limit = "DCI_CH4";

        //MIPI Header
        public const string ConstMIPI_set1 = "MIPI_Set1";
        public const string ConstMIPI_set2 = "MIPI_Set2";
        public const string ConstMIPI_RegNo = "MIPI_RegCount";      //total register used
        public const string ConstMIPI_Reg0 = "MIPI_Reg_0";
        public const string ConstMIPI_Reg1 = "MIPI_Reg_1";
        public const string ConstMIPI_Reg2 = "MIPI_Reg_2";
        public const string ConstMIPI_Reg3 = "MIPI_Reg_3";
        public const string ConstMIPI_Reg4 = "MIPI_Reg_4";
        public const string ConstMIPI_Reg5 = "MIPI_Reg_5";
        public const string ConstMIPI_Reg6 = "MIPI_Reg_6";
        public const string ConstMIPI_Reg7 = "MIPI_Reg_7";
        public const string ConstMIPI_Reg8 = "MIPI_Reg_8";
        public const string ConstMIPI_Reg9 = "MIPI_Reg_9";
        public const string ConstMIPI_RegA = "MIPI_Reg_A";
        public const string ConstMIPI_RegB = "MIPI_Reg_B";
        public const string ConstMIPI_RegC = "MIPI_Reg_C";
        public const string ConstMIPI_RegD = "MIPI_Reg_D";
        public const string ConstMIPI_RegE = "MIPI_Reg_E";
        public const string ConstMIPI_RegF = "MIPI_Reg_F";

        //Equipment Setting
        public const string ConstSetSA1 = "SA1_Setting";
        public const string ConstSetSA2 = "SA2_Setting";
        public const string ConstSetSG1 = "SG1_Setting";
        public const string ConstSetSG2 = "SG2_Setting";
        public const string ConstSetSMU = "SMU_Setting";

        public const string ConstSA1att = "SA1_Input_Atten";
        public const string ConstSA2att = "SA2_Input_Atten";
        public const string ConstSG1MaxPwr = "SG1_MaxPwr";
        public const string ConstSG2MaxPwr = "SG2_MaxPwr";

        public const string ConstSG1_DefaultFreq = "SG1_DefaultFreq";        //variable to preset default or initial freq
        public const string ConstSG2_DefaultFreq = "SG2_DefaultFreq";        //variable to preset default or initial freq

        public const string ConstMultiplier_RXIQRate = "Multiplier_RXIQRate";       //variable for PXI RX IQ Rate multiplier , by default should be 1.25

        //Equipment Off State Flag
        public const string ConstOffSG1 = "SG1_Off";
        public const string ConstOffSG2 = "SG2_Off";
        public const string ConstOffSMU = "SMU_Off";
        public const string ConstOffDC = "DC_Off";

        //Result Header
        public const string ConstPara_Pout = "Para_Pout";
        public const string ConstPara_Pin = "Para_Pin";
        public const string ConstPara_Pout1 = "Para_Pout1";
        public const string ConstPara_Pin1 = "Para_Pin1";
        public const string ConstPara_Pout2 = "Para_Pout2";
        public const string ConstPara_Pin2 = "Para_Pin2";
        public const string ConstPara_NF1 = "Para_NF1";
        public const string ConstPara_NF2 = "Para_NF2";
        public const string ConstPara_MXATrace = "Para_MXA_Trace";
        public const string ConstPara_MXATraceFreq = "Para_MXA_TraceFreq";
        public const string ConstPara_Harmonic = "Para_Harmonic";
        public const string ConstPara_IMD = "Para_IMD";
        public const string ConstPara_MIPI = "Para_MIPI";
        public const string ConstPara_SMU = "Para_SMU";
        public const string ConstPara_DCSupply = "Para_DCSupply";
        public const string ConstPara_Switch = "Para_Switch";
        public const string ConstPara_TestTime = "Para_TestTime";

        //Port Infomation -Seoul, Ben
        public const string ConstPara_Tx_Port = "Tx_Port";
        public const string ConstPara_ANT_Port = "ANT_Port";
        public const string ConstPara_Rx_Port = "Rx_Port";

        //Delay Header and Other Setting
        public const string ConstTrig_Delay = "Trig_Delay";
        public const string ConstGeneric_Delay = "Generic_Delay";
        public const string ConstRdCurr_Delay = "RdCurr_Delay";
        public const string ConstRdPwr_Delay = "RdPwr_Delay";
        public const string ConstSetup_Delay = "Setup_Delay";
        public const string ConstStartSync_Delay = "StartSync_Delay";
        public const string ConstStopSync_Delay = "StopSync_Delay";
        public const string ConstEstimate_TestTime = "Estimate_TestTime";
        public const string ConstSearch_Method = "Search_Method";
        public const string ConstSearch_Value = "Search_Value";
        public const string ConstInterpolation = "Interpolation";
        public const string ConstAbs_Value = "Absolute Value";
        public const string ConstSave_MXATrace = "Save_MXATrace";

        //NF Setting -Seoul
        public const string ConstSwitching_Band_HotNF = "Switching Band_HotNF";
        public const string ConstNF_BW = "NF_BW";
        public const string ConstNF_REFLEVEL = "NF_REFLEVEL";
        public const string ConstNF_SWEEPTIME = "NF_SWEEPTIME";
        public const string ConstNF_AVERAGE = "NF_AVERAGE";
        public const string ConstNF_CalTag = "NF_CalTag";
        public const string ConstNF_SoakTime = "NF_SoakTime";
        public const string ConstNF_Cal_HL = "NF_Cal_HL";
        public const string ConstNF_Cal_LL = "NF_Cal_LL";

        //MPI Voltage and Current Set Header
        public const string ConstMIPISetCh = "MIPISet_Channel";
        public const string ConstMIPIMeasCh = "MIPIMeasure_Channel";
        public const string ConstMIPIVSclk = "V_SCLK";
        public const string ConstMIPIVSdata = "V_SDATA";
        public const string ConstMIPIVSvio = "V_SVIO";

        public const string ConstMIPIISclk = "I_SCLK";
        public const string ConstMIPIISdata = "I_SDATA";
        public const string ConstMIPIISvio = "I_SVIO";

        //Misc Header
        public const string ConstTxDac = "TxDac";
        public const string ConstRxDac = "RxDac";
        public const string ConstNote = "Note";

        public const string ConstFBarActiveDir = @"C:\\Avago.ATF.Common\\DataLog\\";
        #endregion
    }
    public static class DataFilePath
    {
        public const string CalPathRF = "CalFile_Path_RF";
        public const string CalPathNF = "CalFile_Path_NF";
        public const string LocSettingPath = "LocalSettingFile_Path";
        public const string EnaStateFileName = "ENA_StateFile_Path";
        public const string CheckDuplicateID = "PauseTestOnDuplicateModuleID";
        public const string StopOnContinueFail1A = "StopOnContinueFail_1A";
        public const string StopOnContinueFail2A = "StopOnContinueFail_2A";
        public const string Enable_GEHeader = "Enable_GEHeader";
        public const string BoardLossPath = "BoardLossFile_Path";
        public const string HSDIO_Model = "HSDIO_Model";
        public const string HSDIO_StrobePoint = "HSDIO_StrobePoint";
        public const string GuPartNo = "GuPartNo";
        public const string GuVerEnable = "GuVerEnable";
        public const string SwitchCycleLimit = "SwitchCycleLimit";
        public const string DpatEnable = "DpatEnable";
        public const string Delta2DIDCheckEnable = "Delta2DIDCheckEnable";
        public const string DeltaMfgIDCheckEnable = "DeltaMfgIDCheckEnable";
        public const string CalPath_PCB_STRIP_X = "PCB_STRIP_UNIT_COUNT_X";
        public const string CalPath_PCB_STRIP_Y = "PCB_STRIP_UNIT_COUNT_Y";
        public const string CalPath_PCB_PANEL_STRIP_X = "PCB_PANEL_STRIP_COUNT_X";
        public const string CalPath_PCB_PANEL_STRIP_Y = "PCB_PANEL_STRIP_COUNT_Y";
        public const string WebQueryValidation = "WebQueryValidation";
        public const string LOCAL_GUDB_Enable = "LOCAL_GUDB_Enable";
        public const string HandlerArmYieldDeltaEnable = "HandlerArmYieldDeltaEnable";
        public const string HandlerArmTestCount = "HandlerArmTestCount";
        public const string HandlerArmThreshold = "HandlerArmThreshold";
        public const string Sample_Version = "Sample_Version";
        public const string WebServerURL = "WebServerURL";
    }
    public class PCBUnitTrace
    {
        public double PCBUnitTrace_PcbLOT;
        public double PCBUnitTrace_PcbPanel;
        public double PCBUnitTrace_PcbStrip;
        public double PCBUnitTrace_PcbModuleID;
        public double PCBUnitTrace_PcbID; // PcbLOT + PcbPanel + PcbStrip
        public double PCBUnitTrace_PcbPanel_X; // Calculation Result
        public double PCBUnitTrace_PcbPanel_Y; // Calculation Result
        public double PCBUnitTrace_PcbStrip_X; // Calculation Result
        public double PCBUnitTrace_PcbStrip_Y; // Calculation Result
        public double PCBUnitTrace_PcbPanel_max_X; // Calculation Result
        public double PCBUnitTrace_PcbPanel_max_Y; // Calculation Result
        public double PCBUnitTrace_PcbStrip_max_X; // Calculation Result
        public double PCBUnitTrace_PcbStrip_max_Y; // Calculation Result
        public double PCBUnitTrace_PCBstrip_Edge;
        public double PCBUnitTrace_PCBpanel_Edge;
    }
    public static class TCF_Sheet
    {
        #region TCF

        public const int ConstPASheetNo = 2,
                  ConstCalSheetNo = 3,
                  ConstKeyWordSheetNo = 4,
                  ConstMipiRegSheetNo = 5,
                  ConstPwrBlastSheetNo = 6;
        public const int ConstPAIndexColumnNo = 1, ConstPATestParaColumnNo = 2;
        public const int ConstCalIndexColumnNo = 1, ConstCalParaColumnNo = 2;
        public const int ConstWaveFormColumnNo = 3;
        public const int ConstMipiKeyIndexColumnNo = 1, ConstMipiRegColumnNo = 2;
        public const int ConstPwrBlastIndexColumnNo = 1, ConstPwrBlastColumnNo = 2;

        public const string ConstPASheetName = "Test_Condition_PA",
            ConstCalSheetName = "CalPath",
            ConstKeyWordSheetName = "Waveform",
            ConstMipiRegSheetName = "MIPI",
            ConstPwrBlastSheetName = "PowerBlast";

        #endregion
    }
    public static class LocalSetting
    {
        #region Cal File
        public const string CalTag = "CalData1D";
        public const string BoardLossTag = "BoardLoss1D";
        #endregion

        #region Local Setting File
        public const string HeaderFilePath = "FilePath";
        public const string keyCalEnable = "Cal_Enable";
        public const string HeaderOCR = "OCR";
        public const string keyOcrEnable = "Enable";
        #endregion
    }
    public struct s_GE_Header
    {
        public bool b_Header;
        public string MeasType;
        public string Param;
        public string Band;
        public string Pmode;
        public string Modulation;
        public string Waveform;
        public string PType;
        public string Pwr;
        public string MeasInfo;
        public string Freq1;
        public string Freq2;
        public string Vcc;
        public string Vbat;
        public string Vdd;
        public string[] Dac;
        public string Note;

        // Add - Ben
        public string InfoTxPort;
        public string InfoANTPort;
        public string InfoRxPort;
    }
}
