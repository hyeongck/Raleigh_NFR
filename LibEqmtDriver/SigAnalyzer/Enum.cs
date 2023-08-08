using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.SA
{
    public enum N9020A_INSTRUMENT_MODE
    {
        SpectrumAnalyzer,
        Basic,
        Wcdma,
        WIMAX,
        EDGE_GSM,
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
    public enum N9020A_ACP_METHOD
    {
        IBW,
        IBWRange,
        FAST,
        RBW,
    }
    public enum N9020A_FREQUENCY_LIST_MODE
    {
        STANDARD,
        SHORT,
        CUSTOM,
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
    public enum N9020A_GSM_AVERAGE
    {
        TXP,
        PVT,
        EPVT,
        EEVM,
        EORF
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
    public enum N9020A_DISPLAY
    {
        ON,
        OFF,
    }
    
}
