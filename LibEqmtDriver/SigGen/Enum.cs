using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.SG
{
    public enum N5182_LIST_TYPE
    {
        //set sweep type either LIST or STEP mode
        STEP,
        LIST
    }
    public enum N5182_TRIG_TYPE
    {
        //immediate triggering or Free Run
        IMM,
        //This choice enables triggering through front panel interaction by pressing the Trigger hardkey
        KEY,
        //This choice enables GPIB triggering using the *TRG or GET command
        BUS,
        //This choice enables the triggering of a sweep event by an externally applied signal at the TRIG IN connector
        EXT,
        //This choice enables the sweep trigger timer
        TIM
    }
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
        /// <summary>
        /// setting the frequency in the SWEEP mode
        /// </summary>
        SWEEP,
    }
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
        /// <summary>
        /// setting the frequency in the SWEEP mode
        /// </summary>
        SWEEP,
    }
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

    public enum INSTR_MODE
    {
        AUTO,   // Sets mode for the list sweep - either MANUAL or AUTO
        MAN
    }
    public enum N5182A_WAVEFORM_MODE
    {
        NONE,
        CW,
        PULSE,
        DREP,
        TDSCDMA_TS1,
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
        HSUPA_ST2,
        HSUPA_ST3,
        HSUPA_ST4,
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
        LTETD5M8RB,
        LTETD5M8RB17S,
        LTETD10M12RB,
        LTETD10M1RB49S,
        LTETD10M12RB38S,
        LTETD10M50RB,
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
        LTE16QAM15M75RB,
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

    public enum N5182A_ROUTE_SUBSYS
    {
        MRK1,
        MRK2,
        MRK3,
        MRK4,
        SweepOut,
        SourSettle,
        PulseVideo,
        PulseSync,
        SweepRun
    }

    public enum N5182A_ALC_TRAN_REF
    {
        RMS,
        Mod,
        NBMod
    }
}
