using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LibEqmtDriver
{
    public class Eq
    {
        public static Eq[] Site;
        public static byte NumSites;
        //public List<Chassis.Chassis_base> Chassis;
        //public KeysightReferenceClock.iRFCLK EqClk;
        //public EqRF.iEqRF RF;
        //public Dictionary<string, EqDC.iEqDC> DC = new Dictionary<string, EqDC.iEqDC>();
        //public EqHSDIO.EqHSDIObase HSDIO;
        //public EqPM.iEqPM PM;
        //public EqSwitchMatrix.EqSwitchMatrixBase SwMatrix;
        //public EqSwitchMatrix.EqSwitchMatrixBase SwMatrixSplit;
        //public NA.NetworkAnalyzerAbstract EqNA;
        //public Eq_ENA.ENA_base EqENA;
        //public static EquipSA EqMXA;
        //public Eq_ENA.NetAn_Base EqNetAn;
        //public static EqHandler.iEqHandler Handler;
        //public static SplitTestPhase CurrentSplitTestPhase = SplitTestPhase.NoSplitTest;
        public static string InstrumentInfo = "";

        public string[] _SMUSetting;
        public string[] _VCCSetting;
        public Dictionary<string, LibEqmtDriver.SMU.iSmu> _PpmuResources = new Dictionary<string, LibEqmtDriver.SMU.iSmu>(); // for PPMU control @ MIPI Class
        public bool _isUseRFmxForTxMeasure = false;
        public int _totalDCSupply = 4;        //max DC Supply 1 Channel is 4 (equal 4 channel in tcf)

        public SCU.iSwitch _EqSwitch, _EqSwitchSplit;
        public SMU.iPowerSupply[] _EqSMU;
        public SMU.Drive_SMU _Eq_SMUDriver;
        public SMU.iSmu[] _EqPPMU;
        public DC_1CH.iDCSupply_1CH[] _Eq_DCSupply;
        public DC_1CH.iDCSupply_1CH _Eq_DC_1CH;
        public DC.iDCSupply _EqDC;
        public PS.iPowerSensor _EqPwrMeter;
        public SA.iSigAnalyzer _EqSA01, _EqSA02;
        public SG.iSiggen _EqSG01, _EqSG02;
        public SG.N5182A_WAVEFORM_MODE _ModulationType;
        public MIPI.iMiPiCtrl _EqMiPiCtrl;
        public NF_VST.NF_NiPXI_VST _EqVST;
        public NF_VST.NF_NI_RFmx _EqRFmx; //Seoul
        public TuneableFilter.iTuneFilterDriver _EqTuneFilter;

        public static void SetNumSites(byte numSites)
        {
            Site = new Eq[numSites];

            for (byte site = 0; site < numSites; site++)
            {
                Site[site] = new Eq();
            }

            NumSites = numSites;
        }
    }
}
