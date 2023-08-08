using Avago.ATF.StandardLibrary;
using ClothoLibAlgo;
using ClothoSharedItems;
using LibEqmtDriver;
using MPAD_TestTimer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
//using TestLib;
using TestPlanCommon.CommonModel;
//using static TestPlanCommon.CommonModel.MultiSiteTestRunner;
using NationalInstruments;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TestPlanCommon.NFModel
{
    public class NFTestFactory
    {
        private NFTestConditionFactory testConFactory;
        private byte site;
        private List<string> AIDIPR;
        private ValidationDataObject m_validationDo;
        private string ClothoRootDir = "";
        public List<string> Pinsweep_Wvfm = new List<string>();
        //private CommonEquipmentInitializer EquipmentInitializer;
        public eTestType testType = eTestType.PATEST;
        public Dictionary<string, string> iTestRevIDs { get; private set; }
        public MyProduct.MyUtility My = new MyProduct.MyUtility();
        public static string VST;

        public ValidationDataObject ValDataObject
        {
            get { return m_validationDo; }
        }

        public NFTestFactory(List<string> _AIDPR)
        {
            m_validationDo = new ValidationDataObject();
            ClothoRootDir = GetTestPlanPath();
            AIDIPR = _AIDPR;
            iTestRevIDs = new Dictionary<string, string>();
            ClothoDataObject.Instance.RunOptionLocked = true;
        }

        public NFTestFactory(byte site, NFTestConditionFactory paramFactory)
        {
            this.site = site;
            testConFactory = paramFactory;
            m_validationDo = new ValidationDataObject();
            ClothoRootDir = GetTestPlanPath();
        }

        private string GetTestPlanPath()
        {
            string basePath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "");

            if (basePath == "")   // Lite Driver mode
            {
                string tcfPath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TCF_FULLPATH, "");

                int pos1 = tcfPath.IndexOf("TestPlans") + "TestPlans".Length + 1;
                int pos2 = tcfPath.IndexOf('\\', pos1);

                basePath = tcfPath.Remove(pos2);
            }

            return basePath + "\\";
        }
        public void PopulateAllPaTests(Dictionary<string, string>[] DicTestNF)
        {
            MyProduct.MyDUT.DicTestPA = DicTestNF;
            MyProduct.MyDUT.AllNFtest = new Dictionary<string, MyProduct.MyDUT.NFTestCondition>();

            LibEqmtDriver.NF_VST.NF_NiPXI_VST vst_Mod = new LibEqmtDriver.NF_VST.NF_NiPXI_VST();

            int T_Count = 0;

            string MeasCH = "";

            //  My.Results = new MyProduct.s_Result[DicTestNF.Length];

            MyProduct.MyDUT.Results = new MyProduct.s_Result[DicTestNF.Length];
            MyProduct.MyDUT.MXATrace = new MyProduct.s_TraceData[DicTestNF.Length];
            MyProduct.MyDUT.PXITrace = new MyProduct.s_TraceData[DicTestNF.Length];
            MyProduct.MyDUT.PXITraceRaw = new MyProduct.s_TraceData[DicTestNF.Length];
            MyProduct.MyDUT.Header = new Dictionary<string, string>();
            try

            {
                foreach (Dictionary<string, string> Data in DicTestNF)
                {
                    Stopwatch tTime = new Stopwatch();

                    tTime.Reset();
                    tTime.Start();

                    string testMode = Data["TEST MODE"].ToUpper();
                    string ge_HeaderStr = "";
                    bool b_SmuHeader = false;
                    string Band = "";

                    MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[1][];
                    MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace[0] = new MyProduct.s_TraceNo[2];

                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored
                    MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored

                    for (int i = 0; i < MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace.Length; i++)
                    {
                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                    }



                    MyProduct.MyDUT.NFTestCondition test = new MyProduct.MyDUT.NFTestCondition();
                    test._TestCount = T_Count;

                    test._TestNum = ReadTcfData(Data, TCF_Header.ConstTestNum.ToUpper());     //use as array number for data store
                    test._TestParam = ReadTcfData(Data, TCF_Header.ConstTestParam.ToUpper());
                    test._SwBand = ReadTcfData(Data, TCF_Header.ConstSwBand.ToUpper());
                    test._TestMode = ReadTcfData(Data, TCF_Header.ConstTestMode.ToUpper());
                    test._TestParam = ReadTcfData(Data, TCF_Header.ConstTestParam.ToUpper());
                    test._TestParaName = ReadTcfData(Data, TCF_Header.ConstParaName.ToUpper());
                    test._TX1Band = ReadTcfData(Data, TCF_Header.ConstTX1Band.ToUpper());
                    test._RX1Band = ReadTcfData(Data, TCF_Header.ConstRX1Band.ToUpper());
                    test._PowerMode = ReadTcfData(Data, TCF_Header.ConstPowerMode.ToUpper());
                    test._InfoTxPort = ReadTcfData(Data, TCF_Header.ConstPara_Tx_Port.ToUpper());
                    test._InfoANTPort = ReadTcfData(Data, TCF_Header.ConstPara_ANT_Port.ToUpper());
                    test._InfoRxPort = ReadTcfData(Data, TCF_Header.ConstPara_Rx_Port.ToUpper());
                    test._Disp_ColdTrace = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstColdTrace).ToUpper() == "V" ? true : false);
                    test._PXI_NoOfSweep = Convert.ToInt16(ReadTcfData(Data, TCF_Header.PXI_NoOfSweep.ToUpper()));

                    test._Test_Pin = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pin.ToUpper()) == "V" ? true : false);
                    test._Test_Pout = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pout.ToUpper()) == "V" ? true : false);
                    test._Test_Pin1 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pin1.ToUpper()) == "V" ? true : false);
                    test._Test_Pout1 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pout1.ToUpper()) == "V" ? true : false);
                    test._Test_Pin2 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pin2.ToUpper()) == "V" ? true : false);
                    test._Test_Pout2 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Pout2.ToUpper()) == "V" ? true : false);

                    test._Test_NF1 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_NF1).ToUpper() == "V" ? true : false);
                    test._Test_NF2 = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_NF2).ToUpper() == "V" ? true : false);

                    test._Test_MXATrace = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_MXATrace.ToUpper()) == "V" ? true : false);
                    test._Test_MXATraceFreq = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_MXATraceFreq.ToUpper()) == "V" ? true : false);
                    test._Test_Harmonic = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Harmonic.ToUpper()) == "V" ? true : false);
                    test._Test_IMD = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_IMD.ToUpper()) == "V" ? true : false);
                    test._Test_MIPI = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_MIPI.ToUpper()) == "V" ? true : false);
                    test._Test_SMU = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_SMU.ToUpper()) == "V" ? true : false);
                    test._Test_DCSupply = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_DCSupply.ToUpper()) == "V" ? true : false);
                    test._Test_Switch = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_Switch.ToUpper()) == "V" ? true : false);
                    test._Test_TestTime = Convert.ToBoolean(ReadTcfData(Data, TCF_Header.ConstPara_TestTime.ToUpper()) == "V" ? true : false);

                    test._Search_Method = ReadTcfData(Data, TCF_Header.ConstSearch_Method.ToUpper());

                    test._Setup_Delay = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstSetup_Delay));

                    test._SMUSetCh = ReadTcfData(Data, TCF_Header.ConstSMUSetCh);
                    test._SMUMeasCh = ReadTcfData(Data, TCF_Header.ConstSMUMeasCh);

                    if (test._SMUSetCh != "0") MeasCH = test._SMUSetCh;

                    test._SMUVCh = new float[9];
                    test._SMUILimitCh = new float[9];

                    bool _Test_Pin = test._Test_Pin;
                    bool _Test_Pout = test._Test_Pout; 
                    bool _Test_Pin1 = test._Test_Pin1;
                    bool _Test_Pout1 = test._Test_Pout1; 
                    bool _Test_Pin2 = test._Test_Pin2; 
                    bool _Test_Pout2 = test._Test_Pout2; 
                    bool _Test_SMU = test._Test_SMU;
                    bool _Test_NF1 = test._Test_NF1;
                    bool _Test_NF2 = test._Test_NF2;
                    bool _Test_Harmonic = test._Test_Harmonic;
                    bool _Test_MIPI = test._Test_MIPI;
                    bool _Test_DCSupply = test._Test_DCSupply;
                    bool _Test_Switch = test._Test_Switch;
                    bool b_TestBoard_temp = test.b_TestBoard_temp;
                    bool _Test_TestTime = test._Test_TestTime;


                    test._SMUVCh[0] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh0));
                    test._SMUVCh[1] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh1));
                    test._SMUVCh[2] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh2));
                    test._SMUVCh[3] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh3));
                    test._SMUVCh[4] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh4));
                    test._SMUVCh[5] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh5));
                    test._SMUVCh[6] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh6));
                    test._SMUVCh[7] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh7));
                    test._SMUVCh[8] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstSMUVCh8));


                    test._DCVCh = new float[5];
                    test._DCILimitCh = new float[5];

                    test._DCSetCh = ReadTcfData(Data, TCF_Header.ConstDCSetCh.ToUpper());
                    test._DCMeasCh = ReadTcfData(Data, TCF_Header.ConstDCMeasCh.ToUpper());

                    test._SetDC = test._DCSetCh.Split(',');
                    test._MeasDC = test._DCMeasCh.Split(',');

                    test._R_DC_ICh = new double[5];
                    test._R_DCLabel_ICh = new string[5];

                    test._DCVCh[1] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCVCh1.ToUpper()));
                    test._DCVCh[2] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCVCh2.ToUpper()));
                    test._DCVCh[3] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCVCh3.ToUpper()));
                    test._DCVCh[4] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCVCh4.ToUpper()));
                    test._DCILimitCh[1] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCICh1Limit.ToUpper()));
                    test._DCILimitCh[2] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCICh2Limit.ToUpper()));
                    test._DCILimitCh[3] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCICh3Limit.ToUpper()));
                    test._DCILimitCh[4] = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstDCICh4Limit.ToUpper()));

                    test._MeasDC = test._DCMeasCh.Split(',');

                    test.R_SMULabel_ICh = new string[9];
                    test.R_SMULabel_VCh = new string[9];
                    test.R_SMU_ICh = new double[9];

                    string CalSegmData = "";
                    string StrError = "";


                    switch (testMode)
                    {
                        #region DC
                        case "DC":

                            for (int i = 0; i < test._MeasDC.Length; i++)
                            {
                                int dcIChannel = Convert.ToInt16(test._MeasDC[i]);

                                // pass out the test result label for every measurement channel
                                string tempLabel = "DCI_CH" + test._MeasDC[i];
                                foreach (string key in NFTestConditionFactory.DicTestLabel.Keys)
                                {
                                    if (key == tempLabel)
                                    {
                                        test._R_DCLabel_ICh[dcIChannel] = NFTestConditionFactory.DicTestLabel[key].ToString();
                                        break;
                                    }
                                }
                            }
                            b_SmuHeader = false;

                            break;
                        #endregion

                        #region SWITCH
                        case "SWITCH":

                            Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);

                            test._str = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], TCF_Header.ConstSwBand, test._SwBand.ToUpper()).Split('@');




                            break;
                        #endregion

                        #region MIPI
                        case "MIPI":
                            switch (test._TestParam.ToUpper())
                            {
                                case "READMIPI_REG_CUSTOM":

                                    switch (test._Search_Method.ToUpper())
                                    {
                                        case "TEMP":
                                        case "TEMPERATURE":

                                            searchMIPIKey(test, NFTestConditionFactory.DicMipiKey);

                                            for (int i = 0; i < test._MeasDC.Length; i++)
                                            {
                                                int dcIChannel = Convert.ToInt16(test._MeasDC[i]);

                                                // pass out the test result label for every measurement channel
                                                string tempLabel = "SMUI_CH" + test._MeasDC[i];
                                                foreach (string key in NFTestConditionFactory.DicTestLabel.Keys)
                                                {
                                                    if (key == tempLabel)
                                                    {
                                                        test.R_SMULabel_ICh[dcIChannel] = NFTestConditionFactory.DicTestLabel[key].ToString();
                                                        break;
                                                    }
                                                }
                                            }


                                            break;


                                    }
                                    break;
                            }
                            string a = test._TestNum;

                            Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);
                            break;
                        #endregion

                        #region PIX_TRACE
                        case "PXI_TRACE":


                            test._Pout1 = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstPout1.ToUpper()));
                            test._Pin1 = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstPin1.ToUpper()));
                            test._NF_BW = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstNF_BW.ToUpper()));

                            //MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[1][];
                            //MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace[0] = new MyProduct.s_TraceNo[2];

                            //MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored
                            //MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored

                            //for (int i = 0; i < MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace.Length; i++)
                            //{
                            //    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                            //    MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                            //}



                            test._Modulation = ReadTcfData(Data, TCF_Header.ConstModulation.ToUpper());
                            test._WaveFormName = ReadTcfData(Data, TCF_Header.ConstWaveformName.ToUpper());
                            test._SwBand_HotNF = ReadTcfData(Data, TCF_Header.ConstSwitching_Band_HotNF.ToUpper());
                            test._Search_Method = ReadTcfData(Data, TCF_Header.ConstSearch_Method.ToUpper());
                            test._Note = ReadTcfData(Data, TCF_Header.ConstNote.ToUpper());

                            test._Txdac = ReadTcfData(Data, TCF_Header.ConstTxDac.ToUpper());
                            test._Rxdac = ReadTcfData(Data, TCF_Header.ConstRxDac.ToUpper());


                            test._StartRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartRXFreq1.ToUpper()));
                            test._StopTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStopTXFreq1.ToUpper()));
                            test._StopRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStopRXFreq1.ToUpper()));
                            test._StepRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStepRXFreq1.ToUpper()));

                            test._TXFreq = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstTXFreq.ToUpper()));
                            test._RXFreq = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstRXFreq.ToUpper()));
                            test._Pout = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstPout.ToUpper()));
                            test._Pin = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstPin.ToUpper()));
                            test._TXBand = ReadTcfData(Data, TCF_Header.ConstTXBand.ToUpper());
                            test._RXBand = ReadTcfData(Data, TCF_Header.ConstRXBand.ToUpper());
                            test._TunePwr_TX = ReadTcfData(Data, TCF_Header.ConstTunePwr_TX.ToUpper()) == "V" ? true : false;

                            test._StartTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartTXFreq1.ToUpper()));
                            test._StartRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartRXFreq1.ToUpper()));

                            test._RxFreq1NoOfPts = (Convert.ToInt32(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)))) + 1;

                            test._RXContactFreq = new double[test._RxFreq1NoOfPts];
                            test._RXPathLoss = new double[test._RxFreq1NoOfPts];
                            test._LNAInputLoss = new double[test._RxFreq1NoOfPts];
                            test._TXPAOnFreq = new double[test._RxFreq1NoOfPts];
                            test._In_BoardLoss = new double[test._RxFreq1NoOfPts];
                            test._Out_BoardLoss = new double[test._RxFreq1NoOfPts];
                            test._RXContactGain = new double[test._RxFreq1NoOfPts];

                            test._TestUsePrev = ReadTcfData(Data, TCF_Header.ConstUsePrev.ToUpper());
                            test._RXFreq = test._StartRXFreq1;

                            test._StepTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStepTXFreq1.ToUpper()));

              

                            switch (test._TestParam.ToUpper())
                            {
                                #region NF MAX MIN

                                case "NF_MAX_MIN":

                                    string H = "";

                                    test._CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand_HotNF.ToUpper());
                                    My.Decode_CalSegm_Setting(test._CalSegmData);

                                    string[] TestUsePrev_Array = test._TestUsePrev.Split(',');

                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace.Length; i++)
                                    {
                                        if (TestUsePrev_Array[0] == MyProduct.MyDUT.PXITrace[i].TestNumber)
                                        {
                                            test._ColdNF_TestCount = i;
                                        }

                                        if (TestUsePrev_Array[1] == MyProduct.MyDUT.PXITrace[i].TestNumber)
                                        {
                                            test._HotNF_TestCount = i;
                                        }
                                    }


                                    test._NumberOfRunsColdNF = MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].TraceCount;
                                    test._NumberOfRunsHotNF = MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].TraceCount;

                                    test._Nop_ColdNF = MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].NoPoints;
                                    test._Nop_HotNF = MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].Multi_Trace[0][0].NoPoints;

           
                                    test._RXPathLoss_Cold = new double[test._Nop_ColdNF];
                                    test._RXPathLoss_Hot = new double[test._Nop_HotNF];


                                    test._Cold_NF_new = new double[test._NumberOfRunsColdNF][];
                                    test._Cold_NoisePower_new = new double[test._NumberOfRunsColdNF][];
                                    test._Hot_NF_new = new double[test._NumberOfRunsHotNF][];
                                    test._Hot_NoisePower_new = new double[test._NumberOfRunsHotNF][];


                                    test._ResultMultiTrace_ColdNF = new MyProduct.s_TraceNo();
                                    test._ResultMultiTrace_ColdNF.Ampl = new double[test._Nop_ColdNF];
                                    test._ResultMultiTrace_ColdNF.FreqMHz = new double[test._Nop_ColdNF];

                                    test._ResultMultiTrace_HotNF = new MyProduct.s_TraceNo();
                                    test._ResultMultiTrace_HotNF.Ampl = new double[test._Nop_HotNF];
                                    test._ResultMultiTrace_HotNF.FreqMHz = new double[test._Nop_HotNF];

                                    test._ResultMultiTraceDelta = new MyProduct.s_TraceNo();
                                    test._ResultMultiTraceDelta.Ampl = new double[test._Nop_HotNF];
                                    test._ResultMultiTraceDelta.FreqMHz = new double[test._Nop_HotNF];

                                    for (int i = 0; i < test._NumberOfRunsColdNF; i++)
                                    {
                                        test._Cold_NF_new[i] = new double[test._Nop_ColdNF];
                                        test._Cold_NoisePower_new[i] = new double[test._Nop_ColdNF];
                                    }

                                    for (int i = 0; i < test._NumberOfRunsHotNF; i++)
                                    {
                                        test._Hot_NF_new[i] = new double[test._Nop_HotNF];
                                        test._Hot_NoisePower_new[i] = new double[test._Nop_HotNF];
                                    }


                                    // Cold NF RX path loss gathering
                                    for (int i = 0; i < test._Nop_ColdNF; i++)
                                    {
                                        test._RXFreq = Convert.ToSingle(MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i]);
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._RXPathLoss_Cold[i] = test._LossOutputPathRX1;
                                    }

                                    // Hot NF RX path loss gathering
                                    for (int i = 0; i < test._Nop_HotNF; i++)
                                    {
                                        test._RXFreq = Convert.ToSingle(MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].Multi_Trace[0][0].FreqMHz[i]);
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._RXPathLoss_Hot[i] = test._LossOutputPathRX1;
                                    }


               

                                    b_SmuHeader = true;
                                    // GE_TestParam = null;

                                    test.GE_Header = new MyProduct.s_GE_Header();

                                    Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);


                                    //Calculate no of tx freq + step

                                    int count = Convert.ToInt16(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)));

                                    double[] TXFreq_List;
                                    TXFreq_List = new double[count + 1];
                                    test._TXFreq = test._StartTXFreq1;

                                    if (test._StartTXFreq1 == test._StopTXFreq1)   //for NMAX method
                                    {
                                        test._SetRX1NDiag = false;
                                        test.GE_Header.Note = "_NOTE_NMAX";//re-assign ge header
                                    }
                                    else
                                    {
                                        test._SetRX1NDiag = true;
                                        test.GE_Header.Note = "_NOTE_NDIAG";    //re-assign ge header
                                    }

                                    for (int i = 0; i <= count; i++)
                                    {
                                        TXFreq_List[i] = Math.Round(test._TXFreq, 3);
                                        test._TXFreq = test._TXFreq + test._StepTXFreq1;

                                        if (!test._SetRX1NDiag)   //for NMAX method
                                        {
                                            test._TXFreq = test._StartTXFreq1;
                                        }

                                        if (test._TXFreq > test._StopTXFreq1) //For Last Freq match
                                        {
                                            test._TXFreq = test._StopTXFreq1;
                                        }
                                    }

                         

                                    if (test._Test_NF1)
                                    {
                                        for (int i = 0; i < test._Nop_ColdNF; i++)
                                        {
                                            List<double> listColdPower = new List<double>();
                                            for (int j = 0; j < test._NumberOfRunsColdNF; j++)
                                            {
                                                listColdPower.Add(test._Cold_NoisePower_new[j][i]);
                                            }

                                            if (test._Disp_ColdTrace)
                                            {
                                                test.GE_Header.Param = "_Power_Cold";      //re-assign ge header 
                                                test.GE_Header.Freq1 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 

                                                test.GE_Header.Waveform = "_x";
                                                test.GE_Header.Modulation = "_x";
                                                test.GE_Header.PType = "_x";
                                                test.GE_Header.Pwr = "_x";

                                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                                H = "Power_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);

                                                MyProduct.MyDUT.Header.Add("Power_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                                //   ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", listColdPower.Max());
                                                //    MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Threading_Class(MyProduct.MyDUT.Header[H], "dBm", listColdPower.Max()));
                                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dBm", 999));
                                            }
                                        }

                                        for (int i = 0; i < test._Nop_HotNF; i++)
                                        {
                                            List<double> listHotPower = new List<double>();
                                            for (int j = 0; j < test._NumberOfRunsHotNF; j++)
                                            {
                                                listHotPower.Add(test._Hot_NoisePower_new[j][i]);
                                            }

                                            test.GE_Header.Param = "_Power_Hot";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_Tx-" + TXFreq_List[i] + "MHz"; //re-assign ge header 
                                            test.GE_Header.Freq2 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 
                                      

                                           
                                            test.GE_Header.Waveform = "_" + MyProduct.MyDUT.DicWaveFormAlias[test._WaveFormName].ToString();
                                            test.GE_Header.Modulation = "_" + test._Modulation;
                                            test.GE_Header.PType = "_FixedPout";
                                            test.GE_Header.Pwr = "_" + test._Pout1 + "dBm";

                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                            MyProduct.MyDUT.Header.Add("Power_Hot" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                            H = "Power_Hot" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dBm", 999));

                                            //     ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", listHotPower.Max());
                                        }

                                        for (int i = 0; i < test._Nop_ColdNF; i++)
                                        {
                                            if (test._Disp_ColdTrace)
                                            {
                                                test.GE_Header.Param = "_NF_Cold";      //re-assign ge header 
                                                test.GE_Header.Freq1 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 

                                                test.GE_Header.Waveform = "_x";
                                                test.GE_Header.Modulation = "_x";
                                                test.GE_Header.PType = "_x";
                                                test.GE_Header.Pwr = "_x";

                                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                                MyProduct.MyDUT.Header.Add("NF_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                                H = "NF_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                                //        ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", ResultMultiTrace_ColdNF.Ampl[i]);
                                            }
                                        }

                                        if (test._Disp_ColdTrace)
                                        {
                                            test.GE_Header.Param = "_NF_Cold-Ampl-Max";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_x"; //re-assign ge header 
                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                            MyProduct.MyDUT.Header.Add("NF_Cold-Ampl-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                            H = "NF_Cold-Ampl-Max" + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            //     test.GE_Header.BuildResults(ref results, GE_TestParam, "dB", MaxColdNFAmpl);

                                            test.GE_Header.Param = "_NF_Cold-Freq-Max";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_x"; //re-assign ge header 
                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                            MyProduct.MyDUT.Header.Add("NF_Cold-Freq-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                            H = "NF_Cold-Freq-Max" + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            //         ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", MaxColdNFFreq);
                                        }

                                        for (int i = 0; i < test._Nop_HotNF; i++)
                                        {
                                            test.GE_Header.Param = "_NF_Hot";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_Tx-" + TXFreq_List[i] + "MHz"; //re-assign ge header 
                                            test.GE_Header.Freq2 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 


                                            test.GE_Header.Waveform = "_" + MyProduct.MyDUT.DicWaveFormAlias[test._WaveFormName].ToString();
                                            test.GE_Header.Modulation = "_" + test._Modulation;
                                            test.GE_Header.PType = "_FixedPout";
                                            test.GE_Header.Pwr = "_" + test._Pout1 + "dBm";

                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                            MyProduct.MyDUT.Header.Add("NF_Hot" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                            H = "NF_Hot" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            //      ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", ResultMultiTrace_HotNF.Ampl[i]);
                                        }

                                        test.GE_Header.Param = "_NF_Hot-Ampl-Max";      //re-assign ge header 
                                        test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 

                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                        MyProduct.MyDUT.Header.Add("NF_Hot-Ampl-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                        //      ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", MaxHotNFAmpl);

                                        H = "NF_Hot-Ampl-Max" + "_" + Convert.ToString(test._TestNum);
                                        MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));



                                        test.GE_Header.Param = "_NF_Hot-Freq-Max";      //re-assign ge header 
                                        test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 

                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                        MyProduct.MyDUT.Header.Add("NF_Hot-Freq-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                        //      ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", MaxHotNFFreq);
                                        H = "NF_Hot-Freq-Max" + "_" + Convert.ToString(test._TestNum);
                                        MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "MHz", 999));

                                        for (int istep = 0; istep < test._Nop_HotNF; istep++)
                                        {
                                            test.GE_Header.Param = "_NF_Rise";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_Tx-" + TXFreq_List[istep] + "MHz"; //re-assign ge header 
                                            test.GE_Header.Freq2 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._HotNF_TestCount].Multi_Trace[0][0].FreqMHz[istep] + "MHz"; //re-assign ge header 
                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                            MyProduct.MyDUT.Header.Add("NF_Rise" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);


                                            H = "NF_Rise" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            //           ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", ResultMultiTraceDelta.Ampl[istep]);
                                        }


                                        double[] maxOfmaxNFRiseAmpl = new double[test._NumberOfRunsHotNF];
                                        double[] maxOfmaxNFRiseFreq = new double[test._NumberOfRunsHotNF];

                                        for (int istep = 0; istep < test._NumberOfRunsHotNF; istep++)
                                        {
                                            //PXITrace[T_Count].Multi_Trace[0][istep].FreqMHz[Array.IndexOf(MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][istep].Ampl
                                            //       double maxNFRiseAmpl = MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][istep].Ampl.Max();
                                         //   double maxNFRiseFreq = MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][istep].FreqMHz[Array.IndexOf(MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][istep].Ampl, MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][istep].Ampl.Max())];

                                            //       maxOfmaxNFRiseAmpl[istep] = maxNFRiseAmpl;
                                         //   maxOfmaxNFRiseFreq[istep] = maxNFRiseFreq;

                                            test.GE_Header.Param = "_NF_Rise-Ampl-Max";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                            test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 
                                            test.GE_Header.MeasInfo = "_MAX" + (istep + 1); //re-assign ge header

                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                            MyProduct.MyDUT.Header.Add("NF_Rise-Ampl-Max" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                            //         ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", maxNFRiseAmpl);

                                            H = "NF_Rise-Ampl-Max" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));

                                            test.GE_Header.Param = "_NF_Rise-Freq-Max";      //re-assign ge header 
                                            test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                            test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 
                                            test.GE_Header.MeasInfo = "_MAX" + (istep + 1); //re-assign ge header

                                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                            MyProduct.MyDUT.Header.Add("NF_Rise-Freq-Max" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                            H = "NF_Rise-Freq-Max" + Convert.ToString(istep) + "_" + Convert.ToString(test._TestNum);
                                            MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "MHz", 999));
                                            //   ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", maxNFRiseFreq);
                                        }

                                        test.GE_Header.Param = "_NF_Rise-Ampl-Max";      //re-assign ge header 
                                        test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.MeasInfo = "_MAXALL"; //re-assign ge header

                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                        MyProduct.MyDUT.Header.Add("NF_Rise-Ampl-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                        H = "NF_Rise-Ampl-Max" + "_" + Convert.ToString(test._TestNum);
                                        MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));

                                        //      ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", maxOfmaxNFRiseAmpl.Max());

                                        test.GE_Header.Param = "_NF_Rise-Freq-Max";      //re-assign ge header 
                                        test.GE_Header.Freq1 = "_Tx-" + test._StartTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.Freq2 = "_Tx-" + test._StopTXFreq1 + "MHz"; //re-assign ge header 
                                        test.GE_Header.MeasInfo = "_MAXALL"; //re-assign ge header

                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);

                                        MyProduct.MyDUT.Header.Add("NF_Rise-Freq-Max" + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                        //       ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", maxOfmaxNFRiseFreq[Array.IndexOf(maxOfmaxNFRiseAmpl, maxOfmaxNFRiseAmpl.Max())]);

                                        H = "NF_Rise-Freq-Max" + "_" + Convert.ToString(test._TestNum);
                                        MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "MHz", 999));
                                    }

                                    break;

                                #endregion


                                #region NFG_TRACE_COLD
                                case "NFG_TRACE_COLD":



                                    test._ColdNF_TestCount = 0;

                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace.Length; i++)
                                    {
                                        if (test._TestUsePrev == MyProduct.MyDUT.PXITrace[i].TestNumber)
                                        {
                                            test._ColdNF_TestCount = i;
                                        }
                                    }

                                    test._Nop_ColdNF = MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].NoPoints;
                                    test._NumberOfRunsColdNF = MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].TraceCount;
                                    test._RXPathLoss_Cold = new double[test._Nop_ColdNF];

                                    test._Cold_NF_new = new double[test._NumberOfRunsColdNF][];
                                    test._Cold_NoisePower_new = new double[test._NumberOfRunsColdNF][];

                                    test._ResultMultiTrace_ColdNF = new MyProduct.s_TraceNo();
                                    test._ResultMultiTrace_ColdNF.Ampl = new double[test._Nop_ColdNF];
                                    test._ResultMultiTrace_ColdNF.FreqMHz = new double[test._Nop_ColdNF];
                                    test._ResultMultiTrace_ColdNF.RxGain = new double[test._Nop_ColdNF];

                                    test.Dic_ColdNF = new Dictionary<double, double>();

                                 //   MaxColdNFAmpl = 0;
                                 //   MaxColdNFFreq = 0;
                                 //   CalcData = 0;

                                    CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand_HotNF.ToUpper());
                                    My.Decode_CalSegm_Setting(CalSegmData);

                                    for (int i = 0; i < test._NumberOfRunsColdNF; i++)
                                    {
                                        test._Cold_NF_new[i] = new double[test._Nop_ColdNF];
                                        test._Cold_NoisePower_new[i] = new double[test._Nop_ColdNF];
                                    }

                                    // Cold NF RX path loss gathering
                                    for (int i = 0; i < test._Nop_ColdNF; i++)
                                    {
                                        test._RXFreq = Convert.ToSingle(MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i]);
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._RXPathLoss_Cold[i] = test._LossOutputPathRX1;
                                    }


                                    #region Golden Eagle Result Format
                                    //b_SmuHeader = true;
                                    //string GE_TestParam = null;
                                    //Rslt_GE_Header = new s_GE_Header();

                                    b_SmuHeader = true;

                                    Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);

                         
                                    count = Convert.ToInt16(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)));
                                    TXFreq_List = new double[count + 1];
                                    test._TXFreq = test._StartTXFreq1;

                                    for (int i = 0; i <= count; i++)
                                    {
                                        TXFreq_List[i] = test._TXFreq;
                                        test._TXFreq = test._TXFreq + test._StepTXFreq1;

                                        if (!test._SetRX1NDiag)   //for NMAX method
                                        {
                                            test._TXFreq = test._StartTXFreq1;
                                        }

                                        if (test._TXFreq > test._StopTXFreq1) //For Last Freq match
                                        {
                                            test._TXFreq = test._StopTXFreq1;
                                        }
                                    }

                                    if (_Test_NF1)
                                    {
                                        for (int i = 0; i < test._Nop_ColdNF; i++)
                                        {
                                            List<double> listColdPower = new List<double>();
                                            for (int j = 0; j < test._NumberOfRunsColdNF; j++)
                                            {
                                                listColdPower.Add(test._Cold_NoisePower_new[j][i]);
                                            }

                                            if (test._Disp_ColdTrace)
                                            {
                                                test.GE_Header.Param = "_Power_Cold";      //re-assign ge header 
                                                test.GE_Header.Freq1 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 
                                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                                // ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", listColdPower.Max());

                                                MyProduct.MyDUT.Header.Add("Power_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                                H = "Power_Cold" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dBm", 999));
                                            }
                                        }

                                        for (int i = 0; i < test._Nop_ColdNF; i++)
                                        {
                                            if (test._Disp_ColdTrace)
                                            {
                                                test.GE_Header.Param = "_Gain";      //re-assign ge header 
                                                test.GE_Header.Freq1 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 
                                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                                //  ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", ResultMultiTrace_ColdNF.RxGain[i]);

                                                MyProduct.MyDUT.Header.Add("Gain" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                                H = "Gain" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            }
                                        }

                                        for (int i = 0; i < test._Nop_ColdNF; i++)
                                        {
                                            if (test._Disp_ColdTrace)
                                            {
                                                test.GE_Header.Param = "_NF";      //re-assign ge header 
                                                test.GE_Header.Freq1 = "_Rx-" + MyProduct.MyDUT.PXITrace[test._ColdNF_TestCount].Multi_Trace[0][0].FreqMHz[i] + "MHz"; //re-assign ge header 
                                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                                //    ResultBuilder.BuildResults(ref results, GE_TestParam, "dB", ResultMultiTrace_ColdNF.Ampl[i]);

                                                MyProduct.MyDUT.Header.Add("NF" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                                H = "NF" + Convert.ToString(i) + "_" + Convert.ToString(test._TestNum);
                                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "dB", 999));
                                            }
                                        }
                                    }
                                    //Force test flag to false to ensure no repeated test data
                                    //because we add to string builder upfront for PXI due to data reported base on number of sweep

                                    _Test_Pin1 = false;
                                    _Test_Pout1 = false;
                                    _Test_SMU = false;
                                    _Test_NF1 = false;


                                    #endregion

                                    break;


                                    #endregion

                            }
                            break;
                        #endregion

                        #region NF
                        case "NF":


                            test._Pout1 = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstPout1.ToUpper()));
                            test._Pin1 = Convert.ToSingle(ReadTcfData(Data, TCF_Header.ConstPin1.ToUpper()));
                            test._NF_BW = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstNF_BW.ToUpper()));

                            //MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[1][];
                            //MyProduct.MyDUT.MXATrace[T_Count].Multi_Trace[0] = new MyProduct.s_TraceNo[2];

                            //MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored
                            //MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace = new MyProduct.s_TraceNo[10][];  //maximum of 10 RBW trace can be stored

                            //for (int i = 0; i < MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace.Length; i++)
                            //{
                            //    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                            //    MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[i] = new MyProduct.s_TraceNo[15]; //initialize to 15 for 15x PXI trace loop only
                            //}



                            test._Modulation = ReadTcfData(Data, TCF_Header.ConstModulation.ToUpper());
                            test._WaveFormName = ReadTcfData(Data, TCF_Header.ConstWaveformName.ToUpper());
                            test._SwBand_HotNF = ReadTcfData(Data, TCF_Header.ConstSwitching_Band_HotNF.ToUpper());
                            test._Search_Method = ReadTcfData(Data, TCF_Header.ConstSearch_Method.ToUpper());
                            test._Note = ReadTcfData(Data, TCF_Header.ConstNote.ToUpper());

                            test._Txdac = ReadTcfData(Data, TCF_Header.ConstTxDac.ToUpper());
                            test._Rxdac = ReadTcfData(Data, TCF_Header.ConstRxDac.ToUpper());


                            test._StartRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartRXFreq1.ToUpper()));
                            test._StopTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStopTXFreq1.ToUpper()));
                            test._StopRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStopRXFreq1.ToUpper()));
                            test._StepRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStepRXFreq1.ToUpper()));

                            test._TXFreq = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstTXFreq.ToUpper()));
                            test._RXFreq = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstRXFreq.ToUpper()));
                            test._Pout = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstPout.ToUpper()));
                            test._Pin = Convert.ToInt16(ReadTcfData(Data, TCF_Header.ConstPin.ToUpper()));
                            test._TXBand = ReadTcfData(Data, TCF_Header.ConstTXBand.ToUpper());
                            test._RXBand = ReadTcfData(Data, TCF_Header.ConstRXBand.ToUpper());
                            test._TunePwr_TX = ReadTcfData(Data, TCF_Header.ConstTunePwr_TX.ToUpper()) == "V" ? true : false;

                            test._StartTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartTXFreq1.ToUpper()));
                            test._StartRXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStartRXFreq1.ToUpper()));

                            test._RxFreq1NoOfPts = (Convert.ToInt32(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)))) + 1;

                            test._RXContactFreq = new double[test._RxFreq1NoOfPts];
                            test._RXPathLoss = new double[test._RxFreq1NoOfPts];
                            test._LNAInputLoss = new double[test._RxFreq1NoOfPts];
                            test._TXPAOnFreq = new double[test._RxFreq1NoOfPts];
                            test._In_BoardLoss = new double[test._RxFreq1NoOfPts];
                            test._Out_BoardLoss = new double[test._RxFreq1NoOfPts];
                            test._RXContactGain = new double[test._RxFreq1NoOfPts];

                            test._TestUsePrev = ReadTcfData(Data, TCF_Header.ConstUsePrev.ToUpper());
                            test._RXFreq = test._StartRXFreq1;

                            test._StepTXFreq1 = Convert.ToDouble(ReadTcfData(Data, TCF_Header.ConstStepTXFreq1.ToUpper()));

                            test._MipiCommand = ReadTcfData(Data, "MIPI COMMAND");

                            if (test._PoutTolerance <= 0)      //just to ensure that tolerance power cannot be 0dBm
                                test._PoutTolerance = 0.5;

                            if (test._PXI_NoOfSweep <= 0)                //check the number of sweep for pxi, set to default if user forget to keyin in excel
                                test._NumberOfRuns = 1;
                            else
                                test._NumberOfRuns = test._PXI_NoOfSweep;

                            switch (test._TestParam.ToUpper())
                            {
                                #region PXI NF COLD
                                case "PXI_NF_COLD":


                                    test._str = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], TCF_Header.ConstSwitching_Band_HotNF, test._SwBand_HotNF.ToUpper()).Split('@');


                                    test._CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand_HotNF.ToUpper());
                                    My.Decode_CalSegm_Setting(test._CalSegmData);

                                    test._count = Convert.ToInt16(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)));

                                    test._TestUsePrev_ArrayNo = 0;

                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace.Length; i++)
                                    {
                                        if (test._TestUsePrev == MyProduct.MyDUT.PXITrace[i].TestNumber)
                                        {
                                            test._TestUsePrev_ArrayNo = i;
                                        }
                                    }

                                    test._RxGainDic = new Dictionary<double, double>();

                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].FreqMHz.Length; i++)
                                    {
                                        test._RxGainDic.Add(Math.Round(MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].FreqMHz[i], 3), MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].Ampl[i]);
                                    }

                                    test._TXFreq = test._StartTXFreq1;
                                    test._RXFreq = test._StartRXFreq1;

                                    for (int i = 0; i <= test._count; i++)
                                    {
                                        test._TXPAOnFreq[i] = Math.Round(test._TXFreq, 3);
                                        test._RXContactFreq[i] = Math.Round(test._RXFreq, 3);

                                        if (test._RxGainDic.TryGetValue(Math.Round(test._RXFreq, 3), out test._RXContactGain[i])) { }
                                        else
                                        {
                                            MessageBox.Show("Need to check between RxGain & NF Frequency Range");
                                        }

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._RXPathLoss[i] = test._LossOutputPathRX1;//Seoul

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.ANTCalSegm, test._RXFreq, ref test._LossCouplerPath, ref StrError);
                                        test._LNAInputLoss[i] = test._LossCouplerPath;//Seoul

                                        test._TXFreq = Convert.ToSingle(Math.Round(test._TXFreq + test._StepTXFreq, 3));
                                        test._RXFreq = Convert.ToSingle(Math.Round(test._RXFreq + test._StepRXFreq1, 3));           //need to use round function because of C# float and double floating point bug/error

                                        if (test._RXFreq > test._StopRXFreq1)//For Last Freq match
                                        {
                                            test._TXFreq = test._StopTXFreq1;
                                            test._RXFreq = test._StopRXFreq1;
                                        }
                                    }


                                    for (int n = 0; n < test._NumberOfRuns; n++)
                                    {
                                        MyProduct.MyDUT.PXITrace[T_Count].Enable = true;
                                        MyProduct.MyDUT.PXITrace[T_Count].SoakSweep = true;
                                        MyProduct.MyDUT.PXITrace[T_Count].TestNumber = test._TestNum;
                                        MyProduct.MyDUT.PXITrace[T_Count].TraceCount = test._NumberOfRuns;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].NoPoints = test._RxFreq1NoOfPts;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RBW_Hz = test._NF_BW * 1e06;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].FreqMHz = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].Ampl = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].Result_Header = test._TestParaName;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].MXA_No = "PXI_NF_COLD_Trace";
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RxGain = new double[test._RxFreq1NoOfPts]; //Yoonchun

                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].FreqMHz = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].Ampl = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].RxGain = new double[test._RxFreq1NoOfPts]; //Yoonchun

                                        for (test.istep = 0; test.istep < test._RxFreq1NoOfPts; test.istep++)
                                        {
                                            MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].FreqMHz[test.istep] = Math.Round(test._RXContactFreq[test.istep], 3);
                                            MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RxGain[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);
                                            //  MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].Ampl[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);

                                            //Store Raw Trace Data to PXITraceRaw Array - Only actual data read from SA (not use in other than Save_PXI_TraceRaw function
                                            MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].FreqMHz[test.istep] = Math.Round(test._RXContactFreq[test.istep], 3);
                                            MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].RxGain[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);
                                            //MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].Ampl[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);
                                        }

                                    }

                                    _Test_Pin1 = false;
                                    _Test_Pout1 = false;
                                    _Test_SMU = false;

                                    //Force test flag to false to ensure no repeated test data
                                    //because we add to string builder upfront for PXI due to data reported base on number of sweep
                                    _Test_NF1 = false;

                     
                                    b_SmuHeader = true;

                                     Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);
                                     Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);


                                    break;

                                #endregion

                                #region PXI_NF_HOT
                                case "PXI_NF_HOT": //Seoul

                                    test._str = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], TCF_Header.ConstSwBand, test._SwBand.ToUpper()).Split('@');


                                    test._CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand.ToUpper());
                                    My.Decode_CalSegm_Setting(test._CalSegmData);

                                    vst_Mod.MOD_FORMAT_CHECK2(test._WaveFormName.ToString(), NFTestConditionFactory.DicWaveForm[test._WaveFormName].ToString(), NFTestConditionFactory.DicWaveFormMutate[test._WaveFormName].ToString(), true);

                                    test._RxFreq1NoOfPts = (Convert.ToInt32(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)))) + 1;
                                    test._modulationType = (LibEqmtDriver.NF_VST.VST_WAVEFORM_MODE)Enum.Parse(typeof(LibEqmtDriver.NF_VST.VST_WAVEFORM_MODE), test._WaveFormName.ToUpper());
                                    test._modArrayNo = (int)Enum.Parse(test._modulationType.GetType(), test._modulationType.ToString()); // to get the int value from System.Enum
                                    test._papr_dB = Math.Round(LibEqmtDriver.NF_VST.NF_VSTDriver.SignalType[test._modArrayNo].SG_papr_dB, 3);


                                    test._tbInputLoss = Convert.ToDouble(My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "TESTBOARD_LOSS", "INPUTLOSS"));
                                    test._tbOutputLoss = Convert.ToDouble(My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "TESTBOARD_LOSS", "OUTPUTLOSS"));

                                    test._TX_count = Convert.ToInt16((test._StopTXFreq1 - test._StartTXFreq1) / test._StepTXFreq1);

                                    test._TXFreq = test._StartTXFreq1;

                                    test._TXCenterFreq = (test._StartTXFreq1 + test._StopTXFreq1) / 2; //Seoul

                                    for (int i = 0; i <= test._TX_count; i++)
                                    {
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.TXCalSegm, test._TXFreq, ref test._LossInputPathSG1, ref StrError);
                                        test._tmpInputLoss = test._tmpInputLoss + (float)test._LossInputPathSG1;
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.ANTCalSegm, test._TXFreq, ref test._LossCouplerPath, ref StrError);
                                        test._tmpCouplerLoss = test._tmpCouplerLoss + (float)test._LossCouplerPath;
                                        test._TXFreq = test._TXFreq + test._StepTXFreq1;
                                    }

                                    test._tmpAveInputLoss = test._tmpInputLoss / (test._TX_count + 1);
                                    test._tmpAveCouplerLoss = test._tmpCouplerLoss / (test._TX_count + 1);
                                    test._totalInputLoss = test._tmpAveInputLoss - test._tbInputLoss;
                                    test._totalOutputLoss = Math.Abs(test._tmpAveCouplerLoss - test._tbOutputLoss);     //Need to remove -ve sign from cal factor for power sensor offset

                                    string TempWaveFormName = test._WaveFormName.Replace("_", "");
                                    string Script =
                                             "script powerServo\r\n"
                                           + "repeat forever\r\n"
                                           + "generate Signal" + TempWaveFormName + "\r\n"
                                           + "end repeat\r\n"
                                           + "end script";

                                    test._TxPAOnScript = Script;

                                    test._RX_count = (Convert.ToInt32(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)))) + 1;

                                    test._RXContactFreq = new double[test._RX_count];
                                    test._RXPathLoss = new double[test._RX_count];
                                    test._LNAInputLoss = new double[test._RX_count];
                                    test._TXPAOnFreq = new double[test._RX_count];
                                    test._In_BoardLoss = new double[test._RX_count];
                                    test._Out_BoardLoss = new double[test._RX_count];
                                    test._RXContactGain = new double[test._RX_count];


                                    test._HotNF_CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand_HotNF.ToUpper());
                                    My.Decode_CalSegm_Setting(test._HotNF_CalSegmData);
                                    test._TestUsePrev_ArrayNo = 0;
                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace.Length; i++)
                                    {
                                        if (test._TestUsePrev == MyProduct.MyDUT.PXITrace[i].TestNumber)
                                        {
                                            test._TestUsePrev_ArrayNo = i;
                                        }
                                    }

                                    test._RxGainDic = new Dictionary<double, double>();
                                    for (int i = 0; i < MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].FreqMHz.Length; i++)
                                    {
                                        test._RxGainDic.Add(Math.Round(MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].FreqMHz[i], 3), MyProduct.MyDUT.PXITrace[test._TestUsePrev_ArrayNo].Multi_Trace[0][0].Ampl[i]);
                                    }

                                    test._TXFreq = test._StartTXFreq1;
                                    test._RXFreq = test._StartRXFreq1;


                                    if ((test._StopTXFreq1 - test._StartTXFreq1) == (test._StopRXFreq1 - test._StartRXFreq1))
                                    {
                                        test._StepTXFreq = test._StepRXFreq1;
                                    }

                                    else
                                    {
                                        test._StepTXFreq = (test._StopTXFreq1 - test._StartTXFreq1) / (test._RX_count - 1);

                                        //Add - 27.01.2021
                                        if ((test._StepTXFreq != 0) && (test._StepTXFreq != test._StepTXFreq1))
                                        {
                                            test._StepTXFreq = test._StepTXFreq1;
                                        }
                                    }


                                    for (int i = 0; i <= test._RX_count - 1; i++)
                                    {
                                        test._TXPAOnFreq[i] = Math.Round(test._TXFreq, 3);
                                        test._RXContactFreq[i] = Math.Round(test._RXFreq, 3);

                                        if (test._RxGainDic.TryGetValue(Math.Round(test._RXFreq, 3), out test._RXContactGain[i]))
                                        {
                                        }
                                        else
                                        {
                                            MessageBox.Show("Need to check between RxGain & NF Frequency Range");
                                        }


                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._RXPathLoss[i] = test._LossOutputPathRX1;//Seoul

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.ANTCalSegm, test._RXFreq, ref test._LossCouplerPath, ref StrError);
                                        test._LNAInputLoss[i] = test._LossCouplerPath;//Seoul

                                        test._TXFreq = Convert.ToSingle(Math.Round(test._TXFreq + test._StepTXFreq, 3));
                                        test._RXFreq = Convert.ToSingle(Math.Round(test._RXFreq + test._StepRXFreq1, 3));           //need to use round function because of C# float and double floating point bug/error

                                        if (test._RXFreq > test._StopRXFreq1)//For Last Freq match
                                        {
                                            test._RXFreq = test._StopRXFreq1;
                                        }

                                        // Add - 27.01.2021, Bug fixed
                                        if (test._TXFreq > test._StopTXFreq1)
                                        {
                                            test._TXFreq = test._StopTXFreq1;
                                        }
                                    }

                                    #region Sort and Store Trace Data



                                    //Store multi trace from PXI to global array
                                    for (int n = 0; n < test._NumberOfRuns; n++)
                                    {
                                        //temp trace array storage use for MAX , MIN etc calculation 
                                        MyProduct.MyDUT.PXITrace[T_Count].Enable = true;
                                        MyProduct.MyDUT.PXITrace[T_Count].SoakSweep = true;
                                        MyProduct.MyDUT.PXITrace[T_Count].TestNumber = test._TestNum;
                                        MyProduct.MyDUT.PXITrace[T_Count].TraceCount = test._NumberOfRuns;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].NoPoints = test._RxFreq1NoOfPts;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RBW_Hz = test._NF_BW * 1e06;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].FreqMHz = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].Ampl = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].Result_Header = test._TestParaName;
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].MXA_No = "PXI_NF_HOT_Trace";
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RxGain = new double[test._RxFreq1NoOfPts]; //Yoonchun

                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].FreqMHz = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].Ampl = new double[test._RxFreq1NoOfPts];
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].RxGain = new double[test._RxFreq1NoOfPts]; //Yoonchun

                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].TargetPout = Math.Round(test._Pout1, 3);
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].modulation = test._Modulation;

                                        foreach (string key in NFTestConditionFactory.DicWaveFormAlias.Keys)
                                        {
                                            if (key == test._WaveFormName.ToUpper())
                                            {
                                                if (test._WaveFormName.ToUpper() != "CW")
                                                {
                                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].waveform = NFTestConditionFactory.DicWaveFormAlias[key].ToString();
                                                }
                                            }
                                        }



                                        for (int i = 0; i < test._RxFreq1NoOfPts; i++)
                                        {
                                            MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].FreqMHz[i] = Math.Round(test._RXContactFreq[i], 3);
                                            MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][n].RxGain[i] = Math.Round(test._RXContactGain[i], 3);

                                            MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].FreqMHz[i] = Math.Round(test._RXContactFreq[i], 3);
                                            MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[0][n].RxGain[i] = Math.Round(test._RXContactGain[i], 3);
                                        }
                                    }

                                    test.MeasSMU = test._SMUMeasCh.Split(',');
                                    if (_Test_SMU)
                                    {
                                        //DelayMs(test._RdCurr_Delay);
                                        float _NPLC = 0.1f; // float _NPLC = 1;  toh
                                        for (int i = 0; i < test.MeasSMU.Count(); i++)
                                        {
                                            int smuIChannel = Convert.ToInt16(test.MeasSMU[i]);
                                            if (test._SMUILimitCh[smuIChannel] > 0)
                                            {
                                                test.R_SMU_ICh[smuIChannel] = 0f;
                                            }

                                            // pass out the test result label for every measurement channel
                                            string tempLabel = "SMUI_CH" + test.MeasSMU[i];
                                            foreach (string key in NFTestConditionFactory.DicTestLabel.Keys)
                                            {
                                                if (key == tempLabel)
                                                {
                                                    test.R_SMULabel_ICh[smuIChannel] = NFTestConditionFactory.DicTestLabel[key].ToString();
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    #region build result

                                    //note - build header for Golden Eagle will use geneic function
                                    b_SmuHeader = true;

                                    //Force test flag to false to ensure no repeated test data
                                    //because we add to string builder upfront for PXI due to data reported base on number of sweep
                                    _Test_NF1 = false;


                                    for (int i = 0; i < test._RxFreq1NoOfPts; i++)
                                    {
                                        Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);
                                        test.GE_Header.Freq1 = "_Rx-" + test._RXContactFreq[i] + "MHz";
                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                        test.GE_TestParam = ge_HeaderStr;

                                        MyProduct.MyDUT.Header.Add(test.GE_Header.Freq1 + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);

                                    }
                                  
                                    test.GE_Header.Freq1 = "_Tx-" + Convert.ToString(test._StartTXFreq1) + "MHz";


                                    #endregion


                                    #endregion


                                    break;

                                #endregion

                                #region PXI_RXPATH_GAIN_NF 

                                case "PXI_RXPATH_GAIN_NF": //Seoul

                                    double testtime1 = tTime.ElapsedMilliseconds;

                                    double IQRate = 999;

                                    if (VST == "NI5646R") IQRate = 250e6;
                                    else IQRate = 120e6;


                                    Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);

                                    test._str = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], TCF_Header.ConstSwBand, test._SwBand.ToUpper()).Split('@');


                                    string[] Mipi = test._MipiCommand.Split(',');
                                    test._Mipi_Command = new List<Dictionary<string, string>>();
                               

                                    for (int i =0; i < Mipi.Length; i ++)
                                    {
                                        searchMIPIKey_MIPICommand(test._TestParam, Mipi[i], out test._CusMipiRegMap, out test._CusPMTrigMap, out test._CusSlaveAddr, out test._CusMipiPair, out test._CusMipiSite, out test._b_mipiTKey, test);
                                        Dictionary<string, string> Mipi_Dic = new Dictionary<string, string>();
                                        Mipi_Dic.Add("REGMAP", test._CusMipiRegMap);
                                        Mipi_Dic.Add("PMTRIGMAP", test._CusPMTrigMap);
                                        Mipi_Dic.Add("SLAVE", test._CusSlaveAddr);
                                        Mipi_Dic.Add("MIPIPAIR", test._CusMipiPair);
                                        Mipi_Dic.Add("MIPISITE", test._CusMipiSite);

                                        test._Mipi_Command.Add(Mipi_Dic);
                                    }

                                    test._FreqRamp = RampPattern(test._StartRXFreq1 * 1e6, test._StopRXFreq1 * 1e6, test._StepRXFreq1 * 1e6, test._RxFreq1NoOfPts);

                               
                                    int NumberOfSteps = test._RxFreq1NoOfPts;
                                    int SamplesPerStep = (int)Math.Ceiling(1e-6 * IQRate);
                                    int SweepSamples = SamplesPerStep * NumberOfSteps;
                                    int TotalSamples = 0;


                                    double CenterFrequency = (test._StartRXFreq1 + test._StopRXFreq1) / 2;
                                    //// create tone to upconvert //
                                    ComplexDouble[] Tone = CarrierWave(SamplesPerStep);
                                    //// initialize arbitrary waveform //
                                    ComplexDouble[] ArbitraryWaveform = new ComplexDouble[SweepSamples];
                                    // sythesize waveform //
                                    for (int i = 0; i < NumberOfSteps; i++)
                                    {
                                        // calculate baseband frequency //
                                        double FrequencyOffset = test._FreqRamp[i] - (CenterFrequency * 1e6) ;
                                        // upconvert waveform //
                                        ComplexDouble[] UpconvertedTone = Upconvert(Tone, IQRate, FrequencyOffset, i == 0);
                                        // copy into arbitrary waveform //
                                        Array.Copy(UpconvertedTone, 0, ArbitraryWaveform, i * SamplesPerStep, SamplesPerStep);
                                    }
                                    // put key at the end of the waveform //
                                    ComplexDouble[] Key = Upconvert(Tone, IQRate, 0, false); // keeps phase coherent between key and last frequency
                                    Key = DigitalGain(Key, -10); // hardcoded key drop of 10 dBm
                                    test._ArbitraryWaveform = ArbitraryWaveform.Concat(Key).Concat(Key).ToArray();
                                    test._TotalSamples = SweepSamples + Tone.Length;
                                    // write arb waveform to rfsg //

                                    double testtime2 = tTime.ElapsedMilliseconds;

                                    vst_Mod.MOD_FORMAT_CHECK2(test._WaveFormName.ToString(), NFTestConditionFactory.DicWaveForm[test._WaveFormName].ToString(), NFTestConditionFactory.DicWaveFormMutate[test._WaveFormName].ToString(), true);

                                    test._modulationType = (LibEqmtDriver.NF_VST.VST_WAVEFORM_MODE)Enum.Parse(typeof(LibEqmtDriver.NF_VST.VST_WAVEFORM_MODE), test._WaveFormName.ToUpper());
                                    test._modArrayNo = (int)Enum.Parse(test._modulationType.GetType(), test._modulationType.ToString()); // to get the int value from System.Enum
                                    test._papr_dB = Math.Round(LibEqmtDriver.NF_VST.NF_VSTDriver.SignalType[test._modArrayNo].SG_papr_dB, 3);


                                    test._CalSegmData = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NF_NONCA_CALTAG", test._SwBand.ToUpper());
                                    My.Decode_CalSegm_Setting(test._CalSegmData);

                                    test._count = Convert.ToInt16(Math.Ceiling((Math.Round(test._StopRXFreq1, 3) - Math.Round(test._StartRXFreq1, 3)) / Math.Round(test._StepRXFreq1, 3)));


                                    for (int i = 0; i <= test._count; i++)
                                    {
                                        test._RXContactFreq[i] = Math.Round(test._RXFreq, 3);

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.RX1CalSegm, test._RXFreq, ref test._LossOutputPathRX1, ref StrError);
                                        test._tmpRxLoss = Math.Round(test._tmpRxLoss + (float)test._LossOutputPathRX1, 3);   //need to use round function because of C# float and double floating point bug/error

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.CalTag, My.CalSegm_Setting.ANTCalSegm, test._RXFreq, ref test._LossCouplerPath, ref StrError);
                                        test._tmpCouplerLoss = Math.Round(test._tmpCouplerLoss + (float)test._LossCouplerPath, 3);   //need to use round function because of C# float and double floating point bug/error

                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.BoardLossTag, My.CalSegm_Setting.In_TBCalSegm, test._RXFreq, ref test._In_BoardLoss[i], ref StrError);
                                        ATFCrossDomainWrapper.Cal_GetCalData1DCombined(LocalSetting.BoardLossTag, My.CalSegm_Setting.Out_TBCalSegm, test._RXFreq, ref test._Out_BoardLoss[i], ref StrError);

                                        test._RXFreq = Convert.ToSingle(Math.Round(test._RXFreq + test._StepRXFreq1, 3));           //need to use round function because of C# float and double floating point bug/error

                                        if (test._RXFreq > test._StopRXFreq1)
                                            test._RXFreq = test._StopRXFreq1;

                                        test._RXPathLoss[i] = test._LossOutputPathRX1;      //Seoul
                                        test._LNAInputLoss[i] = test._LossCouplerPath;       //Seoul
                                    }

                                    test._tbInputLoss = test._In_BoardLoss.Average();
                                    test._tbOutputLoss = test._Out_BoardLoss.Average();

                                    test._tmpAveRxLoss = test._tmpRxLoss / (test._count + 1);
                                    test._tmpAveCouplerLoss = test._tmpCouplerLoss / (test._count + 1);
                                    test._totalInputLoss = test._tmpAveCouplerLoss - test._tbInputLoss;       //pathloss from SG to ANT Port inclusive fixed TB Loss
                                    test._totalOutputLoss = test._tmpAveRxLoss - test._tbOutputLoss;          //pathgain from RX Port to SA inclusive fixed TB Loss


                                    test._MXA_Config = My.ReadTextFile(NFTestConditionFactory.DicCalInfo[MyProduct.DataFilePath.LocSettingPath], "NFCA_MXA_Config", test._SwBand.ToUpper());
                                    test._MXA_Setting = My.Decode_MXA_Setting_ReturnValue(test._MXA_Config);

                                    test.SAReferenceLevel = My.MXA_Setting.RefLevel;
                                    test.vBW_Hz = My.MXA_Setting.VBW;



                                    int rbw_counter = 0;

                                    MyProduct.MyDUT.PXITrace[T_Count].Enable = true;
                                    MyProduct.MyDUT.PXITrace[T_Count].SoakSweep = false;
                                    MyProduct.MyDUT.PXITrace[T_Count].TestNumber = test._TestNum;
                                    MyProduct.MyDUT.PXITrace[T_Count].TraceCount = 1;
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].NoPoints = test._RxFreq1NoOfPts;
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].RBW_Hz = 1e6;
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].FreqMHz = new double[test._RxFreq1NoOfPts];
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].Ampl = new double[test._RxFreq1NoOfPts];
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].Result_Header = test._TestParaName;
                                    MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[rbw_counter][0].MXA_No = "PXI_RXCONTACT_Trace";

                                    MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[rbw_counter][0].FreqMHz = new double[test._RxFreq1NoOfPts];
                                    MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[rbw_counter][0].Ampl = new double[test._RxFreq1NoOfPts];

                                    for (test.istep = 0; test.istep < test._RxFreq1NoOfPts; test.istep++)
                                    {
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][0].FreqMHz[test.istep] = Math.Round(test._RXContactFreq[test.istep], 3);
                                        MyProduct.MyDUT.PXITrace[T_Count].Multi_Trace[0][0].Ampl[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);

                                        //Store Raw Trace Data to PXITraceRaw Array - Only actual data read from SA (not use in other than Save_PXI_TraceRaw function
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[rbw_counter][0].FreqMHz[test.istep] = Math.Round(test._RXContactFreq[test.istep], 3);
                                        MyProduct.MyDUT.PXITraceRaw[T_Count].Multi_Trace[rbw_counter][0].Ampl[test.istep] = Math.Round(test._RXContactGain[test.istep], 3);
                                    }

                                    for (int i = 0; i < test._RxFreq1NoOfPts; i++)
                                    {
                                        b_SmuHeader = true;

                                        Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);
                                        test.GE_Header.Freq1 = "_Rx-" + test._RXContactFreq[i] + "MHz";


                                        Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out ge_HeaderStr, b_SmuHeader);
                                        test.GE_TestParam = ge_HeaderStr;
                                        MyProduct.MyDUT.Header.Add(test.GE_Header.Freq1 + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                                    }

                                    if (test._TestParam.ToUpper() == "PXI_RXPATH_GAIN_NF")
                                    {
                                        test.GE_Header.Freq1 = "_Rx-" + Convert.ToString(Math.Round(test._StartRXFreq1, 6)) + "MHz";  // Start Freq
                                        test.GE_Header.Freq2 = "_Rx-" + Convert.ToString(Math.Round(test._StopRXFreq1, 6)) + "MHz";  // Stop Freq
                                    }


                                 //   Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);



                                    break;
                                    #endregion


                            }

                            break;

                        #endregion

                        //case "SKIP":

                        //    break;

                    }

                    //if (testMode != "SKIP")
                    //{

                        string GE_TestParam = null;
                        double R_NF1_Ampl = -999,
                                R_NF2_Ampl = -999,
                                R_NF1_Freq = -999,
                                R_NF2_Freq = -999,
                                R_H2_Ampl = -999,
                                R_H2_Freq = -999,
                                R_Pin = -999,
                                R_Pin1 = -999,
                                R_Pin2 = -999,
                                R_Pout = -999,
                                R_Pout1 = -999,
                                R_Pout2 = -999,
                                R_ITotal = -999,
                                R_MIPI = -999,
                                R_DCSupply = -999,
                                R_Switch = -999,
                                R_Temperature = -999,
                                R_RFCalStatus = -999;

                        #region add test result
                        if (_Test_Pin)
                        {

                            test.GE_Header.Param = "_Pin";


                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);

                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            //Rslt_GE_Header.Param = "_Pin";      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pin);

                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN].Result_Data = R_Pin;
                        }
                        if (_Test_Pout)
                        {
                            test.GE_Header.Param = "_Pout";

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);

                            //Rslt_GE_Header.Param = "_Pout";      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pin);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT].Result_Data = R_Pout;


                        }
                        if (_Test_Pin1)
                        {
                            test.GE_Header.Param = "_Pin";

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //Rslt_GE_Header.Param = "_Pin";      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pin1);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "1" + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN1].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN1].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN1].Result_Data = R_Pin1;



                        }
                        if (_Test_Pout1)
                        {
                            test.GE_Header.Param = "_Pout";

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //Rslt_GE_Header.Param = "_Pout";      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pout1);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "1" + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT1].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT1].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT1].Result_Data = R_Pout1;

                        }
                        if (_Test_Pin2)
                        {
                            test.GE_Header.Param = "_Pin";

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //Rslt_GE_Header.Param = "_Pin";      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pin2);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "2" + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN2].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN2].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.PIN2].Result_Data = R_Pin2;

                        }
                        if (_Test_Pout2)
                        {
                            test.GE_Header.Param = "_Pout";

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);

                            //Rslt_GE_Header.Param = "_Pout";      //re-assign ge header 
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_Pout2);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "2" + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT2].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT2].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.POUT2].Result_Data = R_Pout2;

                        }
                        if (_Test_NF1)
                        {

                            if (test._TestParam.ToUpper() == "PXI_RXPATH_GAIN_NF")
                            {
                                test.GE_Header.Freq1 = "_Rx-" + Convert.ToString(Math.Round(test._StartRXFreq1, 6)) + "MHz";  // Start Freq
                                test.GE_Header.Freq2 = "_Rx-" + Convert.ToString(Math.Round(test._StopRXFreq1, 6)) + "MHz";  // Stop Freq
                            }
                            test.GE_Header.Param = "_Gain_RX-Ampl";      //re-assign ge header 

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "dBm", R_NF1_Ampl);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_AMPL].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_AMPL].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_AMPL].Result_Data = R_NF1_Ampl;

                            test.GE_Header.Param = "_Gain_RX-Ampl-Freq";      //re-assign ge header

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", R_NF1_Freq);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);

                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_FREQ].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_FREQ].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF1_FREQ].Result_Data = R_NF1_Freq;

                        }
                        if (_Test_NF2)
                        {
                            test.GE_Header.Param = test.GE_Header.Param + "-Ampl";      //re-assign ge header 

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, Rslt_GE_Header.Param, "dBm", R_NF2_Ampl);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result

                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_AMPL].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_AMPL].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_AMPL].Result_Data = R_NF2_Ampl;

                            test.GE_Header.Param = test.GE_Header.Param + "-Freq";      //re-assign ge header

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", R_NF2_Freq);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_FREQ].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_FREQ].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.NF2_FREQ].Result_Data = R_NF2_Freq;

                        }
                        if (_Test_Harmonic)
                        {
                            test.GE_Header.Param = test.GE_Header.Param + "-Ampl";      //re-assign ge header 

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, Rslt_GE_Header.Param, "dBm", R_H2_Ampl);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            ////use as temp data storage for calculating MAX, MIN etc of multiple result
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_AMPL].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_AMPL].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_AMPL].Result_Data = R_H2_Ampl;


                            test.GE_Header.Param = test.GE_Header.Param + "-Freq";      //re-assign ge header

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "MHz", R_H2_Freq);
                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), ge_HeaderStr);
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_FREQ].Enable = true;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_FREQ].Result_Header = GE_TestParam;
                            //MyProduct.MyDUT.Results[T_Count].Multi_Results[(int)MyProduct.e_ResultTag.HARMONIC_FREQ].Result_Data = R_H2_Freq;


                        }
                        if (_Test_MIPI)
                        {
                            Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);

                            test.GE_Header.Note = "_NOTE_" + test._TestParaName + "_" + test._TestNum;      //re-assign ge header 

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            test.GE_TestParam = ge_HeaderStr;

                            MyProduct.MyDUT.Header.Add(test.GE_Header.Note + "_" + Convert.ToString(test._TestNum), GE_TestParam);

                            //Rslt_GE_Header.Note = "_NOTE_" + _TestParaName + "_" + _TestNum;      //re-assign ge header 
                            //Construct_GE_Header(TestPara, Rslt_GE_Header, DicTestLabel, MeasBand, out GE_TestParam, _Test_SMU);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "NA", R_MIPI);
                        }

                        if (_Test_SMU)
                        {
                            test.MeasSMU = test._SMUMeasCh.Split(',');
                            int ch_cnt = test.MeasSMU.Count();
                            string[] GE_TestParam_Array;

                            double[] smuMeas_I = new double[ch_cnt];
                            string[] tmp_GE_TestParam = new string[ch_cnt];

                            test.GE_Header.MeasType = "N";      //re-assign ge header 

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, test.R_SMU_ICh, out smuMeas_I, out GE_TestParam_Array);
                            //  Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);



                            for (int i = 0; i < test.MeasSMU.Count(); i++)
                            {
                                MyProduct.MyDUT.Header.Add(test.R_SMULabel_ICh[Convert.ToInt16(test.MeasSMU[i])].ToString() + "_" + Convert.ToString(test._TestNum), GE_TestParam_Array[i]);
                                //ResultBuilder.BuildResults(ref results, tmp_GE_TestParam[i], "A", smuMeas_I[i]);
                            }


                        }
                        if (_Test_DCSupply)
                        {

                            for (int i = 0; i < test._MeasDC.Count(); i++)
                            {

                                Decode_GE_Header(test, out test.GE_Header, NFTestConditionFactory.DicTestLabel, NFTestConditionFactory.DicMipiKey, NFTestConditionFactory.DicWaveFormAlias);


                                test.GE_Header.Note = "_NOTE_" + test._TestParaName + "_" + test._R_DCLabel_ICh[Convert.ToInt16(test._MeasDC[i])];      //re-assign ge header  

                                Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                                MyProduct.MyDUT.Header.Add(test._R_DCLabel_ICh[Convert.ToInt16(test._MeasDC[i])] + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                                //ResultBuilder.BuildResults(ref results, GE_TestParam, "A", test._R_DC_ICh[Convert.ToInt16(test._MeasDC[i])]);

                                string H = test._R_DCLabel_ICh[Convert.ToInt16(test._MeasDC[i])] + "_" + Convert.ToString(test._TestNum);
                                if (test._TestParam == "MULTI_DCSUPPLY")
                                {
                                    MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "A", 999));
                                }

                            }


                        }
                        if (_Test_Switch)
                        {

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "NA", R_Switch);
                        }
                        if (R_RFCalStatus == 1)
                        {

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "NA", R_RFCalStatus);
                        }
                        if (b_TestBoard_temp)
                        {

                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "C", R_Temperature);
                        }
                        if (_Test_TestTime)
                        {


                            test.GE_Header.MeasType = "M";      //re-assign ge header 
                            test.GE_Header.Param = "_TIME";      //re-assign ge header 
                            test.GE_Header.Note = "_NOTE_" + test._TestNum;      //re-assign ge header 


                            Construct_GE_Header(test, test.GE_Header, NFTestConditionFactory.DicTestLabel, Band, out GE_TestParam, b_SmuHeader);

                            MyProduct.MyDUT.Header.Add(test.GE_Header.Param + "_" + Convert.ToString(test._TestNum), GE_TestParam);
                            // MyProduct.MyDUT.Header.Add(GE_TestParam, GE_TestParam);
                            ////ResultBuilder.BuildResults(ref results, GE_TestParam, "mS", tTime.ElapsedMilliseconds);
                            //ResultBuilder.BuildResults(ref results, GE_TestParam, "mS", tTime.Elapsed.TotalMilliseconds);
                            if (test._TestParam == "MULTI_DCSUPPLY")
                            {
                                string H = test.GE_Header.Param + "_" + Convert.ToString(test._TestNum);

                                MyProduct.MyDUT.LResults.Add(MyProduct.MyDUT.Header[H], new MyProduct.MyDUT.Result_Class(MyProduct.MyDUT.Header[H], "mS", 999));
                            }
                        }
                        #endregion

                 //   }

                    MyProduct.MyDUT.AllNFtest.Add(test._TestNum, test);
                    T_Count++;


                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() , "GetCalfactor", MessageBoxButtons.OK, MessageBoxIcon.Error);


            }



        }

        public double[] RampPattern(double Start, double Stop, double Step, int NumberOfSteps)
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

                if (Ramp[i] > Stop)
                    Ramp[i] = Stop;
            }
            return Ramp;
        }
        public ComplexDouble[] CarrierWave(int NumberOfSamples)
        {
            double[] ones = Enumerable.Repeat(1.0, NumberOfSamples).ToArray();
            double[] zeros = Enumerable.Repeat(0.0, NumberOfSamples).ToArray();
            return ComplexDouble.ComposeArrayPolar(ones, zeros);
        }
        public double MixerPhase;
        public ComplexDouble[] Upconvert(ComplexDouble[] ComplexData, double SampleRate, double CarrierFrequency, bool reset)
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
        public ComplexDouble[] DigitalGain(ComplexDouble[] UnscaledData, double dBGain)
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
        public string ReadTcfData(Dictionary<string, string> TestPara, string strHeader)
        {
            string Temp = "";

            TestPara.TryGetValue(strHeader.ToUpper(), out Temp);
            return (Temp != null ? Temp : "");

        }


        public void Decode_GE_Header(MyProduct.MyDUT.NFTestCondition TestPara, out MyProduct.s_GE_Header GE_Header, Dictionary<string, string> DicTestLabel, Dictionary<string, string>[] DicTestMipi, Dictionary<string, string> DicWaveFormAlias)
        {
            #region initialize GE Header to default value

            string[] biasDataArr = null;

            #region Read Configuration from TCF
            string _TestMode = TestPara._TestMode;
            string _TestParam = TestPara._TestParam;
            string _TestParaName = TestPara._TestParaName;
            string _TX1Band = TestPara._TX1Band;

            if (_TX1Band == "0" || _TX1Band == null || _TX1Band == "")
            {
                _TX1Band = "x";
            }

            string _RX1Band = TestPara._RX1Band;

            if (_RX1Band == "0" || _RX1Band == null || _RX1Band == "")
            {
                _RX1Band = "x";
            }
            string _Pout1 = Convert.ToString(TestPara._Pout1);
            string _Pin1 = Convert.ToString(TestPara._Pin1);
            string _PowerMode = TestPara._PowerMode;
            if (_PowerMode == "0" || _PowerMode == null || _PowerMode == "")
            {
                _PowerMode = "x";
            }
            string _Modulation = TestPara._Modulation;
            if (_Modulation == "0" || _Modulation == null || _Modulation == "")
            {
                _Modulation = "x";
            }
            string _WaveFormName = TestPara._WaveFormName;

            if (_WaveFormName == "0" || _WaveFormName == null || _WaveFormName == "")
            {
                _WaveFormName = "x";
            }
            string _Search_Method = TestPara._Search_Method;
            string _NF_BW = Convert.ToString(TestPara._NF_BW);

            if (_NF_BW == "0" || _NF_BW == null || _NF_BW == "")
            {
                _NF_BW = "x";
            }
            string _Note = TestPara._Note;
            if (_Note == "0" || _Note == null || _Note == "")
            {
                _Note = "x";
            }
            string _TxDac = TestPara._Txdac;
            if (_TxDac == "0" || _TxDac == null || _TxDac == "")
            {
                _TxDac = "x";
            }
            string _RxDac = TestPara._Rxdac;
            if (_RxDac == "0" || _RxDac == null || _RxDac == "")
            {
                _RxDac = "x";
            }

            //Sweep TX1/RX1 Freq Condition
            string _StartTXFreq1 = Convert.ToString(TestPara._StartTXFreq1);
            string _StopTXFreq1 = Convert.ToString(TestPara._StopTXFreq1);
            string _StartRXFreq1 = Convert.ToString(TestPara._StartRXFreq1);
            string _StopRXFreq1 = Convert.ToString(TestPara._StopRXFreq1);

            // Add - Ben
            string _InfoTxPort = Convert.ToString(TestPara._InfoTxPort);
            string _InfoANTPort = Convert.ToString(TestPara._InfoANTPort);
            string _InfoRxPort = Convert.ToString(TestPara._InfoRxPort);

            #endregion

            GE_Header = new MyProduct.s_GE_Header();
            GE_Header.Dac = new string[3];      //Temp Initialize to 3 arrays

            GE_Header.b_Header = true;
            GE_Header.MeasType = "x";
            GE_Header.Param = "_x";
            GE_Header.Band = "_x";
            GE_Header.Pmode = "_x";
            GE_Header.Modulation = "_x";
            GE_Header.Waveform = "_x";
            GE_Header.PType = "_x";
            GE_Header.Pwr = "_x";
            GE_Header.MeasInfo = "_x";
            GE_Header.Freq1 = "_x";
            GE_Header.Freq2 = "_x";
            // Add - Ben
            GE_Header.InfoTxPort = "_x";
            GE_Header.InfoANTPort = "_x";
            GE_Header.InfoRxPort = "_x";
            GE_Header.Vcc = "_x";
            GE_Header.Vbat = "_x";
            GE_Header.Vdd = "_x";
            GE_Header.Note = "_NOTE_x";

            if (_InfoTxPort == "0")
                _InfoTxPort = "x";

            if (_InfoANTPort == "0")
                _InfoANTPort = "x";

            if (_InfoRxPort == "0")
                _InfoRxPort = "x";

            #region decode Dac Header

            int TxDacArr = 0;
            int RxDacArr = 0;
            string[] tmpArr;

            foreach (string key in DicTestLabel.Keys)
            {
                if (key == "TXDAC")
                {
                    tmpArr = DicTestLabel[key].ToString().Split('=');
                    TxDacArr = Convert.ToInt16(tmpArr[1]);
                }
                if (key == "RXDAC")
                {
                    tmpArr = DicTestLabel[key].ToString().Split('=');
                    RxDacArr = Convert.ToInt16(tmpArr[1]);

                    try
                    {
                        var _RxDic = _RxDac.Split('=');
                        if (_RxDic.Length > 1 && _RxDac.Split('=')[1].Split(';').Count() != RxDacArr)
                        {
                            RxDacArr = _RxDac.Split('=')[1].Split(';').Count();
                        }
                    }
                    catch (Exception Ex)
                    {
                        RxDacArr = Convert.ToInt16(tmpArr[1]);
                    }
                }
            }

            GE_Header.Dac = new string[TxDacArr + RxDacArr];

            for (int i = 0; i < GE_Header.Dac.Length; i++)
            {
                GE_Header.Dac[i] = "_x";    //Initialize to default
            }

            #region Search and return Data from Mipi custom spreadsheet
            bool b_mipiTKey = false;
            string[] tmpArr_addrs;
            string[] tmpBias_Data;
            int regNo = 0;

            if (_TxDac != "x" || _RxDac != "x")
            {
                if (_TxDac != "x")
                {
                    regNo = 0;       //start tx reg array always at 0
                    tmpArr = _TxDac.Split('=');
                    tmpArr_addrs = tmpArr[1].Split(';');

                    searchMIPIKey(_TestParam, tmpArr[0], out TestPara._CusMipiRegMap, out TestPara._CusPMTrigMap, out TestPara._CusSlaveAddr, out TestPara._CusMipiPair, out TestPara._CusMipiSite, out b_mipiTKey);

                    biasDataArr = TestPara._CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

                    for (int j = 0; j < TxDacArr; j++)
                    {
                        for (int i = 0; i < biasDataArr.Length; i++)
                        {
                            tmpBias_Data = biasDataArr[i].Split(':');
                            if ((Convert.ToInt32(tmpBias_Data[0], 16)) == (Convert.ToInt32(tmpArr_addrs[j], 16)))
                            {
                                GE_Header.Dac[regNo] = "_0x" + tmpBias_Data[1];
                                regNo++;
                                break;
                            }
                        }
                    }
                }
                if (_RxDac != "x")
                {
                    regNo = TxDacArr;       //start rx reg no array after TxDacArr
                    tmpArr = _RxDac.Split('=');
                    tmpArr_addrs = tmpArr[1].Split(';');

                    searchMIPIKey(_TestParam, tmpArr[0], out TestPara._CusMipiRegMap, out TestPara._CusPMTrigMap, out TestPara._CusSlaveAddr, out TestPara._CusMipiPair, out TestPara._CusMipiSite, out b_mipiTKey);

                    //searchMIPIKey(TestPara, DicTestMipi);

                    biasDataArr = TestPara._CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

                    for (int j = 0; j < RxDacArr; j++)
                    {
                        for (int i = 0; i < biasDataArr.Length; i++)
                        {
                            tmpBias_Data = biasDataArr[i].Split(':');
                            if ((Convert.ToInt32(tmpBias_Data[0], 16)) == (Convert.ToInt32(tmpArr_addrs[j], 16)))
                            {
                                GE_Header.Dac[regNo] = "_0x" + tmpBias_Data[1];
                                regNo++;
                                break;
                            }
                        }
                    }
                }
            }

            #endregion

            #region Read Waveform Alias
            foreach (string key in DicWaveFormAlias.Keys)
            {
                if (key == _WaveFormName.ToUpper())
                {
                    if (_WaveFormName.ToUpper() != "CW")
                    {
                        GE_Header.Waveform = "_" + DicWaveFormAlias[key].ToString();
                    }
                }
            }
            #endregion

            #endregion

            switch (_TestMode.ToUpper())
            {
                case "MXA_TRACE":
                    #region LXI Trace Calculation
                    switch (_TestParam.ToUpper())
                    {
                        case "CALC_MXA_TRACE":
                            break;
                        case "MERIT_FIGURE":
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "PXI_TRACE":
                    #region PXI Trace Calculation
                    GE_Header.MeasType = "N";
                    switch (_TestParam.ToUpper())
                    {
                        case "CALC_PXI_TRACE":
                            break;
                        case "MERIT_FIGURE":
                            break;
                        case "MAX_MIN":
                            break;
                        case "TRACE_MERIT_FIGURE":
                            break;
                        case "NF_MAX_MIN":
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "NF_MAX_MIN_COLD":
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "NF_MAX_MIN_MIPI":
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "NF_FETCH":
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            break;
                        case "NFG_TRACE_COLD":
                            GE_Header.MeasType = "NFG";
                            GE_Header.Param = "_NF";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.PType = "_FixedPin";
                            GE_Header.Pwr = "_" + _Pin1 + "dBm";
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "COMMON":
                    #region Common Math Function
                    switch (_TestParam.ToUpper())
                    {
                        case "MAX_MIN":
                            break;
                        case "AVERAGE":
                            break;
                        case "DELTA":
                            break;
                        case "SUM":
                            break;

                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "CALIBRATION":
                    #region System Calibration
                    switch (_TestParam.ToUpper())
                    {
                        case "RF_CAL":
                            break;
                        case "NF_CAL":
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "DC":
                    #region DC Setting
                    GE_Header.MeasType = "M";
                    switch (_TestParam.ToUpper())
                    {
                        case "PS4CH":
                        case "PS1CH":
                        case "MULTI_DCSUPPLY":
                            GE_Header.Note = "_NOTE_" + _Note;
                            break;
                        case "SMU":
                            break;
                        case "SMU_LEAKAGE":
                            GE_Header.Pmode = "_" + _PowerMode;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "MIPI":
                    #region MIPI
                    GE_Header.MeasType = "M";
                    switch (_TestParam.ToUpper())
                    {
                        case "SETMIPI":
                        case "SETMIPI_SMU":
                        case "SETMIPI_CUSTOM":
                        case "SETMIPI_CUSTOM_SMU":
                            GE_Header.Param = "_MIPI_NumBitErrors";
                            GE_Header.Band = "_" + _TX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.Note = "_NOTE_" + _Note;

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "READMIPI_REG_CUSTOM":
                            switch (_Search_Method.ToUpper())
                            {
                                case "TEMP":
                                case "TEMPERATURE":
                                    GE_Header.MeasType = "N";
                                    GE_Header.Param = "_MIPI_TEMP";
                                    GE_Header.Band = "_" + _TX1Band;
                                    GE_Header.Pmode = "_" + _PowerMode;
                                    GE_Header.Note = "_NOTE_" + _Note;
                                    break;
                            }
                            break;
                        case "READ_OTP_CUSTOM":
                            GE_Header.Param = "_MIPI_NumBitErrors";
                            GE_Header.Band = "_" + _TX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.Note = "_NOTE_" + _Note;
                            break;
                        case "BURN_OTP_JEDI":
                        case "BURN_OTP_JEDI2":
                            GE_Header.Param = "_MIPI_OTPBURN";
                            GE_Header.Note = "_NOTE_" + _Note;
                            break;
                        case "MIPI_LEAKAGE":
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "MIPI_OTP":
                    #region MIPI OTP
                    switch (_TestParam.ToUpper())
                    {
                        case "READ_OTP_SELECTIVE_BIT":
                            GE_Header.MeasType = "M";
                            GE_Header.Param = "_MIPI";
                            GE_Header.Note = "_NOTE_" + _TestParaName;

                            switch (_Search_Method.ToUpper())
                            {
                                //case "REV_ID":
                                //    GE_Header.Param = "_MIPI_ReadRxReg-21";
                                //    GE_Header.Note = "_NOTE_" + _Note;
                                //    break;
                                //case "CM_ID":
                                //    GE_Header.Param = "_MIPI_CM-ID";
                                //    GE_Header.Note = "_NOTE_" + _Note;
                                //    break;
                                case "UNIT_ID":
                                case "UNIT_ID_MANUAL_SET":
                                case "UNIT_2DID":
                                case "CUSTOM_HEADER":
                                case "READ_2DID_FROM_OTHER_OTP_BIT":
                                    GE_Header.b_Header = false;     //set to false - no required to display the GE Header Format . Use Parameter Header define in TCF only . Cause RF1 & RF2 using custom header for CMOS X-Y, MFG_ID, MODULE_ID etc ..
                                    GE_Header.Param = _TestParaName;
                                    break;
                            }
                            break;
                        case "BURN_OTP_SELECTIVE_BIT":
                            GE_Header.MeasType = "M";
                            GE_Header.Param = "_MIPI_OTPBURN";
                            GE_Header.Note = "_NOTE_" + _TestParaName;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "MIPI_VEC":
                    #region MIPI Vector
                    switch (_TestParam.ToUpper())
                    {
                        case "OTP_BURN_LNA":
                            GE_Header.MeasType = "M";
                            GE_Header.Param = "_MIPI_OTPBURN";
                            GE_Header.Note = "_NOTE_" + _TestParaName;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "SWITCH":
                    #region Switch Config
                    GE_Header.MeasType = "M";
                    switch (_TestParam.ToUpper())
                    {
                        case "SETSWITCH":
                            GE_Header.Note = "_NOTE_" + _Note;

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "NF":
                    #region NF Measurement
                    GE_Header.MeasType = "N";
                    switch (_TestParam.ToUpper())
                    {
                        case "NF_CA_NDIAG":
                            break;
                        case "NF_NONCA_NDIAG":
                            break;
                        case "NF_FIX_NMAX":
                            break;
                        case "RXPATH_CONTACT":
                            break;
                        case "NF_STEPSWEEP_NDIAG":
                            break;
                        case "PXI_NF_NONCA_NDIAG":
                            break;
                        case "PXI_NF_FIX_NMAX":
                            break;
                        case "PXI_RXPATH_CONTACT":
                            GE_Header.Param = "_Gain_RX";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.PType = "_FixedPin";
                            GE_Header.Pwr = "_" + _Pin1 + "dBm";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "PXI_FIXED_POWERBLAST":
                            //GE_Header.Param = "_NF_Hot";
                            GE_Header.Band = "_" + _TX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.PType = "_FixedPout";
                            GE_Header.Pwr = "_" + _Pout1 + "dBm";
                            GE_Header.Freq1 = "_Tx-" + _StartTXFreq1 + "MHz";
                            break;
                        case "PXI_RAMP_POWERBLAST":
                            //GE_Header.Param = "_NF_Hot";
                            GE_Header.Band = "_" + _TX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation; ;
                            GE_Header.PType = "_FixedPout";
                            GE_Header.Pwr = "_" + _Pout1 + "dBm";
                            GE_Header.Freq1 = "_Tx-" + _StartTXFreq1 + "MHz";
                            break;
                        case "PXI_RXPATH_GAIN":
                        case "PXI_RXPATH_GAIN_NF":
                            GE_Header.Param = "_Gain_RX";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.PType = "_FixedPin";
                            GE_Header.Pwr = "_" + _Pin1 + "dBm";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "PXI_NF_COLD":
                            GE_Header.Param = "_NF_Cold";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Rx-" + _StartRXFreq1 + "MHz";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "PXI_NF_HOT":
                            GE_Header.Param = "_NF_Hot";
                            GE_Header.Band = "_" + _TX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.Modulation = "_" + _Modulation;
                            GE_Header.PType = "_FixedPout";
                            GE_Header.Pwr = "_" + _Pout1 + "dBm";
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Tx-" + _StartTXFreq1 + "MHz";
                            GE_Header.Freq2 = "_Tx-" + _StopTXFreq1 + "MHz";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            if (_StartTXFreq1 == _StopTXFreq1)
                            {
                                GE_Header.Note = "_NOTE_NMAX_" + _RX1Band;
                            }
                            else
                            {
                                GE_Header.Note = "_NOTE_NDIAG_" + _RX1Band;
                            }
                            break;
                        case "PXI_NF_COLD_MIPI": // Ben, Add for MIPI NFR
                            GE_Header.Param = "_NF_Cold-MIPI"; // Need to modify
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Rx-" + _StartRXFreq1 + "MHz";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;
                        case "PXI_NF_MEAS":
                            GE_Header.MeasType = "NFG";
                            GE_Header.Param = "_NF";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.PType = "_FixedPin";
                            GE_Header.Pwr = "_" + _Pin1 + "dBm";
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Rx-" + _StartRXFreq1 + "MHz";
                            break;

                        case "PXI_NF_COLD_ALLINONE":
                            GE_Header.Param = "_NF_Cold-ALLINONE";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Rx-" + _StartRXFreq1 + "MHz";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;
                            break;

                        case "PXI_NF_COLD_MIPI_ALLINONE":
                            GE_Header.Param = "_NF_Cold-MIPI-ALLINONE";
                            GE_Header.Band = "_" + _RX1Band;
                            GE_Header.Pmode = "_" + _PowerMode;
                            GE_Header.MeasInfo = "_" + _NF_BW + "MBW";
                            GE_Header.Freq1 = "_Rx-" + _StartRXFreq1 + "MHz";

                            // Add - Ben
                            GE_Header.InfoTxPort = "_" + _InfoTxPort;
                            GE_Header.InfoANTPort = "_" + _InfoANTPort;
                            GE_Header.InfoRxPort = "_" + _InfoRxPort;

                            break;
                        default:
                            MessageBox.Show("NF Test Parameter : " + _TestParam.ToUpper() + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "MKR_NF":
                    #region LXI Marker Function
                    switch (_TestParam.ToUpper())
                    {
                        case "NF_CA_NDIAG":
                            break;
                        case "NF_NONCA_NDIAG":
                            break;
                        case "NF_FIX_NMAX":
                            break;
                        default:
                            MessageBox.Show("NF Test Parameter : " + _TestParam.ToUpper() + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                case "TESTBOARD":
                    #region Testboard ID & Temp
                    switch (_TestParam.ToUpper())
                    {
                        case "TEMPERATURE":
                            GE_Header.MeasType = "M";
                            GE_Header.Param = "_MIPI_TEMP";
                            GE_Header.Note = "_NOTE_" + _Note;
                            break;
                        default:
                            MessageBox.Show("Test Parameter : " + _TestParam + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                            break;
                    }
                    break;
                #endregion
                default:
                    MessageBox.Show("Test Mode " + _TestMode + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    break;
            }
            #endregion
        }

        public void Construct_GE_Header(MyProduct.MyDUT.NFTestCondition TestPara, MyProduct.s_GE_Header ge, Dictionary<string, string> DicTestLabel, string band, out string ge_HeaderStr, bool b_SmuHeader = false)
        {
            ge_HeaderStr = null;

            #region decode Dac Header

            string tmp_geHeader_DAC = null;

            for (int i = 0; i < ge.Dac.Length; i++)
            {
                tmp_geHeader_DAC = tmp_geHeader_DAC + ge.Dac[i];
            }

            #endregion

            #region decode SMU_Header
            string[] R_SMULabel_VCh;

         

            if (b_SmuHeader)
            {
                #region initialize data & result
                //Read TCF SMU Setting
                //float[] _SMUVCh;
                //_SMUVCh = new float[9];

                string _SMUSetCh = TestPara._SMUSetCh;
                string _SMUMeasCh = TestPara._SMUMeasCh;



                // int _SMUTotal = Convert.ToInt32(MyProduct.MyDUT.SearchLocalSettingDictionary("SmuSetting", "TOTAL_SMUCHANNEL"));
                int _SMUTotal = 5;
                #endregion

                #region construct SMU header
                //for (int i = 0; i < MeasSMU.Count(); i++)
                for (int i = 0; i < _SMUTotal; i++)
                {
                    // pass out the test result label for every measurement channel
                    //int smuVChannel = Convert.ToInt16(MeasSMU[i]);
                    //string tempLabel = "SMUV_CH" + MeasSMU[i];
                    string tempLabel = "SMUV_CH" + i;
                    foreach (string key in DicTestLabel.Keys)
                    {
                        if (key == tempLabel)
                        {
                            if (DicTestLabel[key].ToString().ToUpper() == "VCC")
                            {
                                ge.Vcc = "_" + TestPara._SMUVCh[i] + "V";
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VCC2") // Vcc_ET100

                            {
                                ge.Vcc = "_" + TestPara._SMUVCh[i] + "V";
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VBATT")
                            {
                                ge.Vbat = "_" + TestPara._SMUVCh[i] + "Vbatt";
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VDD")
                            {
                                ge.Vdd = "_" + TestPara._SMUVCh[i] + "Vdd";
                            }
                            break;
                        }
                    }
                }
                #endregion
            }

            #endregion

            //construct ge header string
            //ge_HeaderStr = ge.MeasType + ge.Param + ge.Band + ge.Pmode + ge.Modulation + ge.Waveform
            //    + ge.PType + ge.Pwr + ge.MeasInfo + ge.Freq1 + ge.Freq2 + ge.InfoTxPort + ge.InfoANTPort + ge.InfoRxPort
            //    + ge.Vcc + ge.Vbat + ge.Vdd
            //    + tmp_geHeader_DAC + ge.Note;

            ge_HeaderStr = ge.MeasType + ge.Param + ge.Band + ge.Pmode + ge.Modulation + ge.Waveform
           + ge.PType + ge.Pwr + ge.MeasInfo + ge.Freq1 + ge.Freq2 + ge.InfoTxPort + ge.InfoANTPort + ge.InfoRxPort
           + ge.Vcc + ge.Vbat + ge.Vdd
           + tmp_geHeader_DAC + ge.Note;

        }

        public void Construct_GE_Header(MyProduct.MyDUT.NFTestCondition TestPara, MyProduct.s_GE_Header ge, Dictionary<string, string> DicTestLabel, string band, double[] chSMU_I, out double[] smuRslt_I, out string[] ge_HeaderStr)
        {

            string tmp_geHeader_DAC = null;

            for (int i = 0; i < ge.Dac.Length; i++)
            {
                tmp_geHeader_DAC = tmp_geHeader_DAC + ge.Dac[i];
            }

            string[] R_SMULabel_VCh;
            smuRslt_I = new double[1]; //dummy data
            ge_HeaderStr = new string[1]; //dummy data

            bool _Test_SMU = TestPara._Test_SMU;
            bool _Test_NF1 = TestPara._Test_NF1;

            if (_Test_SMU)
            {
                #region initialize data & result
                //Read TCF SMU Setting
                float[] _SMUVCh;
                _SMUVCh = new float[9];

                string _SMUSetCh = TestPara._SMUSetCh;
                string _SMUMeasCh = TestPara._SMUMeasCh;

                _SMUVCh[0] = TestPara._SMUVCh[0];
                _SMUVCh[1] = TestPara._SMUVCh[1];
                _SMUVCh[2] = TestPara._SMUVCh[2];
                _SMUVCh[3] = TestPara._SMUVCh[3];
                _SMUVCh[4] = TestPara._SMUVCh[4];
                _SMUVCh[5] = TestPara._SMUVCh[5];
                _SMUVCh[6] = TestPara._SMUVCh[6];
                _SMUVCh[7] = TestPara._SMUVCh[7];
                _SMUVCh[8] = TestPara._SMUVCh[8];



                //to select which channel to set and measure - Format in TCF(DCSet_Channel) 1,4 -> means CH1 & CH4 to set/measure
                string[] MeasSMU = TestPara._SMUMeasCh.Split(',');
                R_SMULabel_VCh = new string[_SMUVCh.Length];
                int ch_cnt = MeasSMU.Length;
                smuRslt_I = new double[ch_cnt];
                ge_HeaderStr = new string[ch_cnt];
                #endregion

                #region construct SMU header
                for (int i = 0; i < MeasSMU.Count(); i++)
                {
                    // pass out the test result label for every measurement channel
                    int smuVChannel = Convert.ToInt16(MeasSMU[i]);
                    string tempLabel = "SMUV_CH" + MeasSMU[i];
                    foreach (string key in DicTestLabel.Keys.Where(t => t.Contains("SMUV_CH")))
                    {
                        if (key == tempLabel)
                        {
                            if (DicTestLabel[key].ToString().ToUpper() == "VCC") // Vcc_ET40
                            {
                                ge.Vcc = "_" + _SMUVCh[smuVChannel] + "V";
                                smuRslt_I[i] = chSMU_I[smuVChannel];
                                if (_Test_NF1)
                                {
                                    ge.Param = "_Icc";
                                }
                                else
                                {
                                    ge.Param = "_Icc_Q";
                                }
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VCC2") // Vcc_ET100
                            {
                                ge.Vcc = "_" + _SMUVCh[smuVChannel] + "V";
                                smuRslt_I[i] = chSMU_I[smuVChannel];
                                if (_Test_NF1)
                                {
                                    ge.Param = "_Icc2";
                                }
                                else
                                {
                                    ge.Param = "_Icc2_Q";
                                }
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VBATT")
                            {
                                ge.Vbat = "_" + _SMUVCh[smuVChannel] + "Vbatt";
                                smuRslt_I[i] = chSMU_I[smuVChannel];
                                if (_Test_NF1)
                                {
                                    ge.Param = "_Ibatt";
                                }
                                else
                                {
                                    ge.Param = "_Ibatt_Q";
                                }
                            }
                            if (DicTestLabel[key].ToString().ToUpper() == "VDD")
                            {
                                ge.Vdd = "_" + _SMUVCh[smuVChannel] + "Vdd";
                                smuRslt_I[i] = chSMU_I[smuVChannel];
                                if (_Test_NF1)
                                {
                                    ge.Param = "_Idd";
                                }
                                else
                                {
                                    ge.Param = "_Idd_Q";
                                }
                            }
                            break;
                        }
                    }

                    //construct ge header string - partial
                    ge_HeaderStr[i] = ge.MeasType + ge.Param + ge.Band + ge.Pmode + ge.Modulation + ge.Waveform
                        + ge.PType + ge.Pwr + ge.MeasInfo + ge.Freq1 + ge.Freq2;
                }
                #endregion
            }

            for (int i = 0; i < ge_HeaderStr.Length; i++)
            {
                //construct ge header string
                ge_HeaderStr[i] = ge_HeaderStr[i] + ge.InfoTxPort + ge.InfoANTPort + ge.InfoRxPort
                    + ge.Vcc + ge.Vbat + ge.Vdd
                    + tmp_geHeader_DAC + ge.Note;
            }

        }

        public void searchMIPIKey(MyProduct.MyDUT.NFTestCondition Cond, Dictionary<string, string>[] DicMipiKey)
        {
            //initialize variable - reset to default
            Cond._b_mipiTKey = false;
            Cond._CusMipiRegMap = null;
            Cond._CusPMTrigMap = null;
            Cond._CusSlaveAddr = null;
            Cond._CusMipiPair = null;
            Cond._CusMipiSite = null;

            foreach (Dictionary<string, string> currMipiReg in DicMipiKey)
            {
                string DicMipiTKey = "";

                currMipiReg.TryGetValue("MIPI KEY", out DicMipiTKey);

                DicMipiTKey = DicMipiTKey.ToUpper();

                if (Cond._SwBand.ToUpper() == DicMipiTKey)
                {
                    currMipiReg.TryGetValue("REGMAP", out Cond._CusMipiRegMap);
                    currMipiReg.TryGetValue("TRIG", out Cond._CusPMTrigMap);
                    currMipiReg.TryGetValue("SLAVEADDR", out Cond._CusSlaveAddr);
                    currMipiReg.TryGetValue("MIPI_PAIR", out Cond._CusMipiPair);
                    currMipiReg.TryGetValue("MIPI_SITE", out Cond._CusMipiSite);
                    Cond._b_mipiTKey = true;          //change flag if match
                }
            }

            if (!Cond._b_mipiTKey)        //if cannot find , show error
                MessageBox.Show("Failed to find MIPI KEY (" + Cond._SwBand.ToUpper() + ") in MIPI sheet \n\n", Cond._TestParam.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void searchMIPIKey(string testParam, string searchKey, out string CusMipiRegMap, out string CusPMTrigMap, out string CusSlaveAddr, out string CusMipiPair, out string CusMipiSite, out bool b_mipiTKey)
        {
            string DicMipiTKey = "";
            //initialize variable - reset to default
            b_mipiTKey = false;
            CusMipiRegMap = null;
            CusPMTrigMap = null;
            CusSlaveAddr = null;
            CusMipiPair = null;
            CusMipiSite = null;

            //Data from Mipi custom spreadsheet 
            foreach (Dictionary<string, string> currMipiReg in NFTestConditionFactory.DicMipiKey)
            {
                currMipiReg.TryGetValue("MIPI KEY", out DicMipiTKey);

                DicMipiTKey = DicMipiTKey.ToUpper();

                if (searchKey.ToUpper() == DicMipiTKey)
                {
                    currMipiReg.TryGetValue("REGMAP", out CusMipiRegMap);
                    currMipiReg.TryGetValue("TRIG", out CusPMTrigMap);
                    currMipiReg.TryGetValue("SLAVEADDR", out CusSlaveAddr);
                    currMipiReg.TryGetValue("MIPI_PAIR", out CusMipiPair);
                    currMipiReg.TryGetValue("MIPI_SITE", out CusMipiSite);
                    b_mipiTKey = true;          //change flag if match
                }
            }

            if (!b_mipiTKey)        //if cannot find , show error
                MessageBox.Show("Failed to find MIPI KEY (" + searchKey.ToUpper() + ") in MIPI sheet \n\n", testParam.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void searchMIPIKey_MIPICommand(string testParam, string searchKey, out string CusMipiRegMap, out string CusPMTrigMap, out string CusSlaveAddr, out string CusMipiPair, out string CusMipiSite, out bool b_mipiTKey, MyProduct.MyDUT.NFTestCondition test)
        {
            string DicMipiTKey = "";
            //initialize variable - reset to default
            b_mipiTKey = false;
            CusMipiRegMap = null;
            CusPMTrigMap = null;
            CusSlaveAddr = null;
            CusMipiPair = null;
            CusMipiSite = null;
            //    NFTestConditionFactory.DicTestPA
            //Data from Mipi custom spreadsheet 
            foreach (Dictionary<string, string> currMipiReg in NFTestConditionFactory.DicMipiKey)
            {
                currMipiReg.TryGetValue("MIPI KEY", out DicMipiTKey);

                DicMipiTKey = DicMipiTKey.ToUpper();

                if (searchKey.ToUpper() == DicMipiTKey)
                {
                    currMipiReg.TryGetValue("REGMAP", out CusMipiRegMap);
                    currMipiReg.TryGetValue("TRIG", out CusPMTrigMap);
                    currMipiReg.TryGetValue("SLAVEADDR", out CusSlaveAddr);
                    currMipiReg.TryGetValue("MIPI_PAIR", out CusMipiPair);
                    currMipiReg.TryGetValue("MIPI_SITE", out CusMipiSite);
                    b_mipiTKey = true;          //change flag if match
                }
            }
            if (!b_mipiTKey)        //if cannot find , show error
                MessageBox.Show("Failed to find MIPI KEY (" + searchKey.ToUpper() + ") in MIPI sheet \n\n", testParam.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
