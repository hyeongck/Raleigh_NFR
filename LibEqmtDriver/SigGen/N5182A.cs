using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Threading;
using System.Windows.Forms;

namespace LibEqmtDriver.SG
{
    public class N5182A : Base_SG, iSiggen
    {
        public static string ClassName = "N5182A Siggen Class";
        private FormattedIO488 myVisaSg = new FormattedIO488();
        public string IOAddress;
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get => "N5182A"; }

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
        public N5182A(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~N5182A() { }

        #region iSeggen Memebers
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
                throw new Exception("EquipN5182A: Initialization -> " + ex.Message);
            }

        }
        public override void Reset()
        {
            try
            {
                myVisaSg.WriteString("SYSTEM:PRESET", true);
                Thread.Sleep(1000);
                myVisaSg.WriteString("*CLS; *RST", true);
            }
            catch (Exception ex)
            {
                throw new Exception("EquipN5182A: RESET -> " + ex.Message);
            }
        }
        public override void EnableRF(INSTR_OUTPUT _ONOFF)
        {
            myVisaSg.WriteString("OUTP:STATE " + _ONOFF, true);

        }
        public override void EnableModulation(INSTR_OUTPUT _ONOFF)
        {
            myVisaSg.WriteString("OUTP:MOD " + _ONOFF, true);
        }
        public override void SetAmplitude(float _dBm)
        {
            myVisaSg.WriteString(":POW " + _dBm.ToString(), true);
        }
        public override void SetFreq(double _MHz)
        {
            myVisaSg.WriteString("FREQ " + _MHz.ToString() + "MHz", true);
        }
        public override void SetPowerMode(N5182_POWER_MODE _mode)
        {
            myVisaSg.WriteString(":SOUR:POW:MODE " + _mode.ToString(), true);
        }
        public override void SetFreqMode(N5182_FREQUENCY_MODE _mode)
        {
            myVisaSg.WriteString(":SOUR:FREQ:MODE " + _mode.ToString(), true);
        }
        public override void MOD_FORMAT_WITH_LOADING_CHECK(string strWaveform, string strWaveformName, bool WaveformInitalLoad)
        {
            while (true)
            {
                if (WaveformInitalLoad)
                {
                    myVisaSg.WriteString(":MEM:COPY \"" + strWaveformName + "@NVWFM\",\"" + strWaveformName + "@WFM1\"", true);
                }

                myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:" + strWaveformName + "\"", true);
                myVisaSg.WriteString(":RAD:ARB ON;:OUTP:MOD ON", true);                  
                break;
            }
        }
        public override void SELECT_WAVEFORM(N5182A_WAVEFORM_MODE _MODE)
        {
            switch (_MODE)
            {
                case N5182A_WAVEFORM_MODE.CDMA2K:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:CDMA2K-WFM1\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.CDMA2K_RC1:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:CDMA2K_RC1_20100316\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM850:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK850\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM1800:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1800\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM1900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM850A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GSM850A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM900A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK900A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM1800A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1800A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GSM1900A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1900A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.HSDPA:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:HSDPA_UL\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.HSUPA_TC3:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_UPLINK_HSUPA_TC3\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.HSUPA_ST2:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_HSUPA_ST2\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.HSUPA_ST3:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_HSUPA_ST3\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.HSUPA_ST4:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_HSUPA_ST4\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.IS95A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:IS95A_RE-WFM1\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.IS98:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:IS98_WFM\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_1DPCH_WFM\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA_UL:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_UL\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA_GTC1:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_GTC1_20100208A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA_GTC3:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_GTC3_20100726A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA_GTC4:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_UPLINK_GTC4\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.WCDMA_GTC1_NEW:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:WCDMA_GTC1_NEW_20101111\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE850:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE850\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE1800:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE1800\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE1900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE1900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE850A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE850A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE900A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE900A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE1800A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE1800A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE1900A:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE1900A\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.LTETD5M8RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTETU_QPSK_5M8RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTETD5M8RB17S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTETU_QPSK_5M8RB17S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTETD10M12RB38S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTETU_QPSK_10M12RB38S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTETD10M12RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTETU_QPSK_10M12RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTETD10M1RB49S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTUQ_10M1R49S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.LTE5M8RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_5M8RB_20091202\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M8RB17S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_QPSK_5M8RB17S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M25RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_5M25RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M1RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_QPSK_10M1RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M1RB49S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_QPSK10M1RB49S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M12RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_10M12RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M12RB19S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK10M12RB19S_1220\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M12RB38S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_10M12RB38S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M48RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_10M48RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M50RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_10M50RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M20RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_QPSK_10M20RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE15M75RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_15M75RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE15M18RB57S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK15M18RB57S_1025\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M100RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_20M100RB091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M18RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_20M18RB_100408\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M48RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_QPSK_20M48RB_091215\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M12RB_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_10M12RB_ST0_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE10M12RB38S_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_10M12RB_ST38_M6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M25RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_5M25RB_ST0_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M8RB17S_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_5M8RB_ST17_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M8RB17S_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_5M8RB_ST17_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE16QAM5M8RB17S:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_16QAM_5M8RB17S\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M25RB_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_5M25RB_ST0_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M1RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_QPSK_5M1RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.LTE10M50RB_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_10M50RB_ST0_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M18RB_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_20M18RB0S_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M18RB82S_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_20M18RB82S_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE20M100RB_MCS2:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTEFUQ_20M100RB0S_MCS2\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE15M16RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_15M16RB0S_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE15M16RB59S_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFUQ_15M16RB59S_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE15M75RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTEFUQ_15M75RB0S_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M8RB_MCS6:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTEFUQ_5M8RB_ST0_MCS6\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE5M8RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTEFUQ_5M8RB_ST0_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.LTE1P4M5RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_1P4M5RB_ST0_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE1P4M5RB1S_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_1P4M5RB_ST1_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE3M4RB_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_3M4RB_ST0_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE3M4RB11S_MCS5:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTEFU_3M4RB_ST11_MCS5\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.LTE16QAM5M25RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:LTE_16QAM_5M25RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE16QAM10M50RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTE_16QAM_10M50RB_0213\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE16QAM15M75RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTE_16QAM_15M75RB_0213\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.LTE16QAM5M8RB:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"LTE_16QAM_5M8RB\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.GMSK900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GMSK800:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK800\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GMSK850:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK850\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE800:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE800\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GMSK1700:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1700\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GMSK1900:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GMSK1900\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.GMSK_TS01:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:GSM_TIMESLOT01_20100107\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EDGE_TS01:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:EDGE_TS01_20100107\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EVDO_4096:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:1XEVDO_REVA_TR4096_0816\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.EVDO_B:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:1XEVDO_REVB_5MHZSEP_001\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;

                case N5182A_WAVEFORM_MODE.TDSCDMA_TS1:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:TDSCDMA_TS1_1P28MHZ\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.DREP:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:DREP\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.PULSE:
                    myVisaSg.WriteString(":RAD:ARB:WAV \"WFM1:PULSE\"", true);
                    myVisaSg.WriteString(":OUTP:MOD ON", true);
                    break;
                case N5182A_WAVEFORM_MODE.NONE:
                    myVisaSg.WriteString(":OUTP:MOD OFF", true);
                    break;
                case N5182A_WAVEFORM_MODE.CW:
                    myVisaSg.WriteString(":OUTP:MOD OFF", true);
                    break;
                default: throw new Exception("Not such a waveform!");
            }
        }
        public override void SET_LIST_TYPE(N5182_LIST_TYPE _mode)
        {
            myVisaSg.WriteString(":LIST:TYPE " + _mode.ToString(), true);
        }
        public override void SET_LIST_MODE(INSTR_MODE _mode)
        {
            myVisaSg.WriteString(":LIST:MODE " + _mode.ToString(), true);
        }
        public override void SET_LIST_TRIG_SOURCE(N5182_TRIG_TYPE _mode)
        {
            myVisaSg.WriteString(":LIST:TRIG:SOUR " + _mode.ToString(), true);
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
            myVisaSg.WriteString("TRIG:SEQ:TIM " + _ms.ToString() + "ms", true);
        }
        public override void SET_SWEEP_POINT(int _points)
        {
            myVisaSg.WriteString("SWE:POIN " + _points.ToString(), true);
        }
        public override void SINGLE_SWEEP()
        {
            myVisaSg.WriteString("SOUR:TSWEEP", true);
        }
        public override void SET_SWEEP_PARAM(int _points, double _ms, double _startFreqMHz, double _stopFreqMHz)
        {
            myVisaSg.WriteString("FREQ:START " + _startFreqMHz.ToString() + "MHz", true);
            myVisaSg.WriteString("FREQ:STOP " + _stopFreqMHz.ToString() + "MHz", true);
            myVisaSg.WriteString("SWE:POIN " + _points.ToString(), true);
            myVisaSg.WriteString("TRIG:SEQ:TIM " + _ms.ToString() + "ms", true);
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
                throw new Exception("N5182A: OPERATION_COMPLETE -> " + ex.Message);
            }
        }
        public override void SET_ROUTE_CONN_EVENT(N5182A_ROUTE_SUBSYS _MODE)
        {
            switch (_MODE)
            {
                case N5182A_ROUTE_SUBSYS.MRK1:
                    myVisaSg.WriteString(":ROUT:CONN:EVENT1 M1", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK2:
                    myVisaSg.WriteString(":ROUT:CONN:EVENT1 M2", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK3:
                    myVisaSg.WriteString(":ROUT:CONN:EVENT1 M3", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK4:
                    myVisaSg.WriteString(":ROUT:CONN:EVENT1 M4", true);
                    break;
                default: throw new Exception("Not such a Route Connector Sub System!");
            }
        }
        public override void SET_ROUTE_CONN_TOUT(N5182A_ROUTE_SUBSYS _MODE)
        {
            switch (_MODE)
            {
                case N5182A_ROUTE_SUBSYS.SweepOut:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT SWE", true);
                    break;
                case N5182A_ROUTE_SUBSYS.SourSettle:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT SETT", true);
                    break;
                case N5182A_ROUTE_SUBSYS.PulseVideo:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT PVID", true);
                    break;
                case N5182A_ROUTE_SUBSYS.PulseSync:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT PSYN", true);
                    break;
                case N5182A_ROUTE_SUBSYS.SweepRun:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT SRUN", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK1:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT M1", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK2:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT M2", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK3:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT M3", true);
                    break;
                case N5182A_ROUTE_SUBSYS.MRK4:
                    myVisaSg.WriteString(":ROUT:CONN:TOUT M4", true);
                    break;
                default: throw new Exception("Not such a Route Connector Sub System!");
            }
        }
        public override void SET_ROUTE_CONN_SOUT(N5182A_ROUTE_SUBSYS _MODE)
        {
            switch (_MODE)
            {
                case N5182A_ROUTE_SUBSYS.SweepOut:
                    myVisaSg.WriteString(":ROUT:CONN:SOUT SWE", true);
                    break;
                case N5182A_ROUTE_SUBSYS.SourSettle:
                    myVisaSg.WriteString(":ROUT:CONN:SOUT SETT", true);
                    break;
                case N5182A_ROUTE_SUBSYS.PulseVideo:
                    myVisaSg.WriteString(":ROUT:CONN:SOUT PVID", true);
                    break;
                case N5182A_ROUTE_SUBSYS.PulseSync:
                    myVisaSg.WriteString(":ROUT:CONN:SOUT PSYN", true);
                    break;
                case N5182A_ROUTE_SUBSYS.SweepRun:
                    myVisaSg.WriteString(":ROUT:CONN:SOUT SRUN", true);
                    break;
                default: throw new Exception("Not such a Route Connector Sub System!");
            }
        }
        public override void SET_ALC_TRAN_REF(N5182A_ALC_TRAN_REF _MODE)
        {
            switch (_MODE)
            {
                case N5182A_ALC_TRAN_REF.RMS:
                    myVisaSg.WriteString(":SOUR:POW:ALC:TRAN:REF RMS", true);
                    break;
                case N5182A_ALC_TRAN_REF.Mod:
                    myVisaSg.WriteString(":SOUR:POW:ALC:TRAN:REF MOD", true);
                    break;
                case N5182A_ALC_TRAN_REF.NBMod:
                    myVisaSg.WriteString(":SOUR:POW:ALC:TRAN:REF NBM", true);
                    break;
                default: throw new Exception("Not such setting in Ampl:ALC:TRAN:RER Sub System!");
            }
        }
        public override void QueryError_SG(out bool status)
        {
            status = false;
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
                    status = false;
                    MessageBox.Show(TempErrMsg, "N5182A - Query Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    status = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("EquipN5182A: QUERY_ERROR --> " + ex.Message);
            }
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
                throw new Exception("EquipN5182A: QUERY_ERROR --> " + ex.Message);
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
