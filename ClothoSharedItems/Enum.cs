using System;

namespace ClothoSharedItems
{
    [Flags]
    public enum MathSyntax { NONE, SUM = 1 << 0, DELTA = 1 << 1, MULTIPLY = 1 << 2, DIVIDE = 1 << 3, POW = 1 << 4 }

    public enum eSimulated { ACTIVE, SIMULATED }

    public enum eTestType { PATEST, DCOTPTEST }

    public enum eTRx { TX, RX }

    [Flags]
    public enum OptionTCPIP
    {
        AddNewLineToRequest = 0x01,
        DeleteLastCharFromResponse = 0x02
    }

    [Flags]
    public enum DevType
    {
        NONE = 0, GPIB = 0x1, SERIAL = 0x2, TCPIP_INSTR = 0x4, QDIAG = 0x10, SHELL = 0x100, TCPIP_SOCKET = 0x1000
    }

    public enum ChamberType { Temptronic = 0, Espec = 1 }

    public enum ChamberHeadONOFF { UP = 0, DOWN }

    public enum ONOFF { OFF = 0, ON }

    public enum AUTOOFF { OFF = 0, AUTO }

    public enum AUTOMANUAL { AUTO, MANUAL }

    public enum TriggerLine
    {
        None, PxiTrig0, PxiTrig1, PxiTrig2, PxiTrig3, PxiTrig4, PxiTrig5, PxiTrig6, PxiTrig7, FrontPanel0, FrontPanel1, FrontPanel2, FrontPanel3
    }

    public enum NetworkType { LTE, NR };

    public enum eMipiTestType
    {
        Write,
        Read,
        WriteRead,
        Timing,
        OTPburn,
        OTPread,
    }

    public enum SplitTestPhase
    {
        NoSplitTest, PhaseA, PhaseB
    }

    public enum Operation
    {
        VSGtoTRX1,
        VSGtoTRX2,
        VSGtoTRX3,
        VSGtoTRX4,
        VSGtoTRX5,
        VSGtoANT1,
        VSGtoANT2,
        VSGtoANT_UAT,
        VSAtoANT1,
        VSAtoANT2,
        VSAtoRX1,
        VSAtoRX2,
        VSAtoRX3,
        VSAtoRX4,
        VSAtoRX5,

        VSAtoN3,
        VSAtoN4,
        VSAtoN5,
        VSAtoN6,

        ANTtoTERM,
        MeasureH2_ANT1,
        MeasureH2_ANT2,
        MeasureH2_ANT3,
        MeasureH2_ANT_UAT,

        MeasureH3_ANT1,
        MeasureH3_ANT2,
        MeasureH3_ANT3,
        MeasureH3_ANT_UAT,
        MeasureH3_PS,
        MeasureH3_VSA,

        MeasureH4_ANT1,
        MeasureH4_ANT2,
        MeasureH4_ANT3,
        MeasureH4_ANT_UAT,
        MeasureH4_PS,
        MeasureH4_VSA,

        MeasureH5_ANT1,
        MeasureH5_ANT2,
        MeasureH5_ANT3,
        MeasureH5_ANT_UAT,
        MeasureH5_PS,
        MeasureH5_VSA,

        MeasureH6_ANT1,
        MeasureH6_ANT2,
        MeasureH6_ANT3,
        MeasureH6_ANT_UAT,
        MeasureH6_PS,
        MeasureH6_VSA,

        MeasureH7_ANT1,
        MeasureH7_ANT2,
        MeasureH7_ANT3,
        MeasureH7_ANT_UAT,
        MeasureH7_PS,
        MeasureH7_VSA,

        MeasureH8_ANT1,
        MeasureH8_ANT2,
        MeasureH8_ANT3,
        MeasureH8_ANT_UAT,
        MeasureH8_PS,
        MeasureH8_VSA,

        MeasureH9_ANT1,
        MeasureH9_ANT2,
        MeasureH9_ANT3,
        MeasureH9_ANT_UAT,
        MeasureH9_PS,
        MeasureH9_VSA,

        MeasureH10_ANT1,
        MeasureH10_ANT2,
        MeasureH10_ANT3,
        MeasureH10_ANT_UAT,
        MeasureH10_PS,
        MeasureH10_VSA,

        MeasureCpl,
        ENAtoRFIN,
        ENAtoRFOUT,
        ENAtoRFOUT_ANT1,
        ENAtoRFOUT_ANT2,
        ENAtoTX_Diff_Plus,
        ENAtoTX_Diff_Neg,
        ENAtoTX,
        ENAto2GTX,
        ENAtoRX,
        ENAtoRX2,
        ENAtoTRX2,
        ENAtoTRX3,
        ENAtoDCS_PCS_RX,
        ENAtoDRX,
        ENAtoCPL,
        P_VSTtoVST_1,
        P_VSTtoVST_2,
        P_FBARtoFBAR_1,
        P_FBARtoFBAR_2,

        // Burhan - Added for SPAR Calibration

        // Add for O2M
        SGCplrToSGIn,

        SAOutToSAIn,
        PwrSensorToPwrSensorIn_From_O2,

        // Used for Joker..
        VSGtoTX,

        VSGtoTX1,
        VSGtoTX2,
        VSGtoTXFor_HMU,
        VSGtoTX2For_HMU,
        VSGtoANT,
        VSGtoANT3,
        VSAtoANT,
        VSAtoANT3,
        HMUtoVSAtoANT,
        VSAtoANT_UAT,
        VSAtoRX,
        TxLeakage,
        MeasureH2,

        NStoAnt,
        NS1toAnt1,
        NS2toAnt2,
        NS3toAnt3,

        N1toTx,
        N2toTx,
        N2toAnt,
        N3toAnt,
        N4toAnt,
        N5toAnt,
        N3toRx,
        N4toRx,
        N5toRx,
        N6toRx,
        N7toRx,
        N8toRx,
        N9toRx,

        NONE,

        VSGtoLNA_IN,
        VSAtoTRX,
        VSAtoTRX1,
        VSAtoTRX2,
        VSAtoTRX3,
        VSAtoTRX4,
        VSAtoTRXAUx,
        VSGtoTRXAUx,

        // Used for Joker..
        NStoAnt1,

        NStoAnt2,
        NStoAnt3,
    }

    public enum eNIDCPOWERTYPE
    {
        DEFAULT = 0b0001,
        HIGHPOWER = 0b0010
    }

    public enum CalibrationInstrument
    {
        MAGICBOX,
        PS,
        MXA,
        NA,
    }

    #region Supplier

    public enum FreqUnit
    {
        Hz = 1,
        KHz = 1000,
        MHz = 1000000,
    }

    public enum Current
    {
        A = 1,
        mA = 1000,
        uA = 1000000,
        nA = 1000000000,
    }

    /// <summary>
    /// Define Instrument Output
    /// </summary>
    public enum INSTR_OUTPUT
    {
        /// <summary>
        /// On Instrument Output
        /// </summary>
        ON,

        /// <summary>
        /// Off Intrument Output
        /// </summary>
        OFF
    }

    public enum N9020A_MEAS_TYPE
    {
        SweptSA,
        ChanPower,
        ACP,
        BTxPow,
        GPowVTM,
        GPHaseFreq,
        GOutRFSpec,
        GTxSpur,
        EPowVTM,
        EEVM,
        EOutRFSpec,
        ETxSpur,
        MonitorSpec,
    }

    public enum N9020A_AVE_TYPE
    {
        RMS,
        LOGARITHM,
        SCALAR,
        TXPRMS,
        TXPLOGARITHM,
        PVTRMS,
        PVTLOGARITHM,
        EPVTRMS,
        EPVTLOGRITHM,
        EORFRMS,
        EORFLOGRITHM,
    }

    public enum N9020A_TRACE_AVE_TYPE
    {
        AVER,
        NEG,
        NORM,
        POS,
        SAMP,
        QPE,
        EAV,
        EPOS,
        MPOS,
    }

    public enum N9020A_TRIGGERING_TYPE
    {
        RF_FreeRun,
        RF_Ext1,
        RF_Ext2,
        RF_RFBurst,
        RF_Video,
        TXP_FreeRun,
        TXP_Ext1,
        TXP_Ext2,
        TXP_RFBurst,
        TXP_Video,
        PVT_FreeRun,
        PVT_Ext1,
        PVT_Ext2,
        PVT_RFBurst,
        PVT_Video,
        EPVT_FreeRun,
        EPVT_Ext1,
        EPVT_Ext2,
        EPVT_RFBurst,
        EPVT_Video,
        EORFS_FreeRun,
        EORFS_Ext1,
        EORFS_Ext2,
        EORFS_RFBurst,
        EORFS_Video,
        EEVM_FreeRun,
        EEVM_Ext1,
        EEVM_Ext2,
        EEVM_RFBurst,
        EEVM_Video,
    }

    public enum N9020A_ACP_METHOD
    {
        IBW,
        IBWRange,
        FAST,
        RBW,
    }

    public enum N9020A_INSTRUMENT_MODE
    {
        SpectrumAnalyzer,
        Basic,
        Wcdma,
        WIMAX,
        EDGE_GSM,
    }

    public enum N9020A_RAD_STD
    {
        NONE,
        IS95A,
        JSTD,
        IS97D,
        GSM,
        W3GPP,
        C2000MC1,
        C20001X,
        Nadc,
        Pdc,
        BLUEtooth,
        TETRa,
        WL802DOT11A,
        WL802DOT11B,
        WL802DOT11G,
        HIPERLAN2,
        DVBTLSN,
        DVBTGPN,
        DVBTIPN,
        FCC15,
        SDMBSE,
        UWBINDOOR,
    }

    public enum N9020A_RADIO_STD_BAND
    {
        EGSM,
        PGSM,
        RGSM,
        DCS1800,
        PCS1900,
        GSM450,
        GSM480,
        GSM700,
        GSM850,
    }

    public enum N9020A_AUTO_COUPLE
    {
        ALL,
        NONE,
    }

    public enum N9020A_STANDARD_DEVICE
    {
        /// <summary>
        /// Base station transmitter
        /// </summary>
        BTS,

        /// <summary>
        /// Mobile station transmitter
        /// </summary>
        MS,
    }

    public enum N9020A_FREQUENCY_LIST_MODE
    {
        STANDARD,
        SHORT,
        CUSTOM,
    }

    public enum N9020A_DISPLAY
    {
        ON,
        OFF,
    }

    public enum N9020A_GSM_AVERAGE
    {
        TXP,
        PVT,
        EPVT,
        EEVM,
        EORF
    }

    /// <summary>
    /// Agilent MXG Power Mode setting
    /// </summary>
    public enum N5182_POWER_MODE
    {
        /// <summary>
        /// This choice stops a power sweep, allowing the signal generator to operate at a
        /// fixed power level.
        /// </summary>
        FIX,

        /// <summary>
        /// This choice selects the swept power mode. If sweep triggering is set to immediate
        /// along with continuous sweep mode, executing the command starts the LIST or
        /// STEP power sweep.
        /// </summary>
        LIST,
    }

    /// <summary>
    /// Agilent MXG Frequency Mode setting
    /// </summary>
    public enum N5182_FREQUENCY_MODE
    {
        /// <summary>
        /// setting the frequency in the FIXed mode
        /// </summary>
        FIX,

        /// <summary>
        /// This choice selects the swept frequency mode. If sweep triggering is set to
        /// immediate along with continuous sweep mode, executing the command starts the
        /// LIST or STEP frequency sweep.
        /// </summary>
        LIST,

        /// <summary>
        /// setting the frequency in the CW mode
        /// </summary>
        CW,
    }

    public enum N5182A_WAVEFORM_MODE
    {
        NONE,
        CDMA2K,
        CDMA2K_RC1,
        GSM850,
        GSM900,
        GSM1800,
        GSM1900,
        GSM850A,
        GSM900A,
        GSM1800A,
        GSM1900A,
        HSDPA,
        HSUPA_TC3,
        IS95A,
        IS98,
        WCDMA,
        WCDMA_UL,
        WCDMA_GTC1,
        WCDMA_GTC3,
        WCDMA_GTC4,
        WCDMA_GTC1_NEW,
        EDGE900,
        EDGE1800,
        EDGE1900,
        EDGE850A,
        EDGE900A,
        EDGE1800A,
        EDGE1900A,
        LTE10M1RB,
        LTE10M1RB49S,
        LTE10M12RB19S,
        LTE10M12RB,
        LTE10M12RB38S,
        LTE10M20RB,
        LTE10M50RB,
        LTE20M18RB82S_MCS6,
        LTE10M48RB,
        LTE15M18RB57S,
        LTE15M75RB,
        LTE5M25RB,
        LTE5M8RB,
        LTE5M8RB17S,
        LTE5M25RB38S,
        LTE20M100RB,
        LTE20M18RB,
        LTE20M48RB,
        LTE10M12RB_MCS6,
        LTE10M12RB38S_MCS6,
        LTE5M25RB_MCS5,
        LTE5M25RB_MCS6,
        LTE5M8RB17S_MCS6,
        LTE16QAM5M8RB17S,
        LTE5M1RB,
        LTE1P4M5RB_MCS5,
        LTE1P4M5RB1S_MCS5,
        LTE3M4RB_MCS5,
        LTE3M4RB11S_MCS5,
        LTE5M8RB_MCS5,
        LTE5M8RB_MCS6,
        LTE5M8RB17S_MCS5,
        LTE10M50RB_MCS6,
        LTE15M16RB_MCS5,
        LTE15M16RB59S_MCS5,
        LTE15M75RB_MCS5,
        LTE16QAM10M50RB,
        LTE16QAM5M8RB,
        LTE20M18RB_MCS6,
        LTE20M100RB_MCS2,
        LTE16QAM5M25RB,
        HSPAPLUS_MPR0,
        HSPAPLUS_MPR2,
        GMSK900,
        GMSK800,
        GMSK850,
        EDGE800,
        EDGE850,
        GMSK1700,
        GMSK1900,
        GMSK_TS01,
        EDGE_TS01,
        EVDO_4096,
        EVDO_B
    }

    public enum Keith_Channel
    {
        a,
        b
    }

    public enum Keith_VRange
    {
        //260xB range
        _Auto,

        _260x_100mV,
        _260x_1V,
        _260x_6V,
        _260x_40V,

        //261xB range
        _261x_200mV,

        _261x_2V,
        _261x_20V,
        _261x_200V,
    }

    public enum Keith_IRange
    {
        //all model range
        _Auto,

        _all_100nA,
        _all_1uA,
        _all_10uA,
        _all_100uA,
        _all_1mA,
        _all_10mA,
        _all_100mA,
        _all_1A,

        //260x range
        _260x_3A,

        //261x range
        _261x_1_5A,

        _261x_10A,
    }

    #endregion Supplier
}