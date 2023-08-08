using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using Avago.ATF.Outlier;
using ClothoSharedItems;

#region Custom Reference Section
using MyProduct;
using System.Net;
using LibEqmtDriver;

using ProductionLib2;
using ProductionControl_x86;
using MPAD_TestTimer;
using AvagoGU;
using Aemulus_PXIe_Modules_Info;
using NI_PXIe_Modules_Info;
using TestPlanCommon.CommonModel;
using TestPlanCommon.NFModel;
using System.Text.RegularExpressions;

#endregion Custom Reference Section

namespace TestPlan_BansheeFull
{
    public class BansheeNF : TestPlanBase, IATFTest
    {
        Random rand = new Random(DateTime.Now.Millisecond);

        // Used to simulate MFGID and OTP_MODULE_ID random
        private static long[] mfgid_options_to_use = new long[3] { 1234L, 1257L, 2301L };
        private static long _dutCount = 0L;
        //private Dictionary<long, long> DicOTPModuleID;
        private bool PauseOnDuplicateID = false;

        MyDUT myDUT;
        TestPlanStateModel m_modelTpState;
        //ProductionTestPlan m_modelTpProd;
        ClothoDataObject m_doClotho1;

        #region  SNP (Datalog) variable
        IPHostEntry ipEntry = null;
        DateTime DT = new DateTime();

        bool InitSNP;
        int failcount = 0;
        int count_1A = 0;
        int count_2A = 0;
        bool previousCount = true;
        bool currentCount;
        int passcount = 0;

        string
        tPVersion = "",
        ProductTag = "",
        lotId = "",
        SublotId = "",
        WaferId = "",
        OpId = "",
        HandlerSN = "",
        newPath = "",
        FileName = "",
        TesterHostName = "",
        TesterIP = "",
        activeDir = @"C:\\Avago.ATF.Common\\DataLog\\";

        //Temp string for current Lot and SubLot ID - to solve Inari issue when using Tally Generator without unload testplan
        //This will cause the datalog for current lot been copied to previous lot folder
        string previous_LotSubLotID = "",
            current_LotSubLotID = "",
            tempWaferId = "",
            tempOpId = "",
            tempHandlerSN = "";

        #endregion

        //GUI ENTRY Variable flag
        bool GUI_Enable = false;
        string InstrumentInfo = "";

        string MFG_LotID = "123456";
        string InitProTag = "";
        bool FirstTest;
        bool programLoadSuccess = true;
        bool Flag1 = true; // Vcc1
        bool Flag2 = true; // Vcc2
        bool Flag3 = true; // VBatt
        bool Flag4 = true; // Vdd
        bool _GuVerEnable = false;
        private string DicMipiTKey;
        Dictionary<string, string>[] DicMipiKey;

        public BansheeNF()
        {
        }

        public override string DoATFInit(string args)
        {
            //Debugger.Break();

            base.DoATFInit(args);
            pjtTagNum = new string[] { "8250", "8260" };

            Eq.SetNumSites(1);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Enter DoATFInit: {0}\nDo Minimum HW Init:\n{1}\n", args, ATFInitializer.DoMinimumHWInit());

            ResultBuilder.DuplicatedModuleID = new bool[1];
            ResultBuilder.DuplicatedModuleIDCtr = new int[1];
            ResultBuilder.DuplicatedModuleIDCtr[0] = 0;

            //test time logger
            m_modelTpState = new TestPlanStateModel();
            //m_modelTpProd = new ProductionTestPlan();

            //Log.LogInfoTestPlan("New Test Plan V2.");
            StopWatchManager.Instance.IsActivated = true;
            PromptManager.Instance.IsAutoAnswer = true;
            StopWatchManager.Instance.Start();

            ValidationDataObject vdo;

            #region Custom Init Coding Section
            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom Init Coding here --------------- //

            //ChoonChin - Lock product tag field right after init button is pressed
            InitProTag = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, "");
            //Clotho.EnableClothoTextBoxes(false);
            FirstTest = true;

            //Check boolean status of GUI ENTRY

            //m_NFTpProd.ShowInputGui(pjtTagNum, SkipProdGUIFlag, myDUT);

            vdo = Validate(null);
            if (!vdo.IsValidated) return vdo.ErrorMessage;

            #region initialize Equipment
            myDUT = new MyDUT(ref sb);
            myDUT.tmpUnit_No = 0;


            NFTestFactory.VST = myDUT.SearchLocalSettingDictionary("Model", "PXI_VST");
     
            NFTestConditionReader Reader = new NFTestConditionReader();
            Reader.ReadTCFAllSheet();

            //myDUT.mfgLotID = MFG_LotID;
            //myDUT.deviceID = "XXXX-1234-Y";

            #region Read Testboard and Socket ID for Aemulus only

            string LocSetFilePath = Convert.ToString(myDUT.DicCalInfo[DataFilePath.LocSettingPath]);

            string MIPImodel = myDUT.SearchLocalSettingDictionary("Model", "MIPI_Card");
            string MIPIaddr = myDUT.SearchLocalSettingDictionary("Address", "MIPI_Card");
            string AemulusPxi_FileName = myDUT.SearchLocalSettingDictionary("Address", "APXI_FileName");


            if (MIPImodel == "DM482E")
            {

                #region MIPI Pin Config
                //use for MIPI pin initialization
                string mipiPairCount = "";
                LibEqmtDriver.MIPI.s_MIPI_PAIR[] tmp_mipiPair;
                mipiPairCount = myDUT.SearchLocalSettingDictionary("MIPI_PIN_CFG", "Mipi_Pair_Count");
                if (mipiPairCount == "")
                {
                    // Not define in config file - set to default of 2 mipi pair only
                    tmp_mipiPair = new LibEqmtDriver.MIPI.s_MIPI_PAIR[2];

                    tmp_mipiPair[0].PAIRNO = 0;
                    tmp_mipiPair[0].SCLK = "0";
                    tmp_mipiPair[0].SDATA = "1";
                    tmp_mipiPair[0].SVIO = "2";

                    tmp_mipiPair[1].PAIRNO = 1;
                    tmp_mipiPair[1].SCLK = "4";
                    tmp_mipiPair[1].SDATA = "5";
                    tmp_mipiPair[1].SVIO = "3";

                }
                else
                {
                    tmp_mipiPair = new LibEqmtDriver.MIPI.s_MIPI_PAIR[Convert.ToInt32(mipiPairCount)];

                    for (int i = 0; i < tmp_mipiPair.Length; i++)
                    {
                        tmp_mipiPair[i].PAIRNO = i;
                        tmp_mipiPair[i].SCLK = myDUT.SearchLocalSettingDictionary("MIPI_PIN_CFG", "SCLK_" + i);
                        tmp_mipiPair[i].SDATA = myDUT.SearchLocalSettingDictionary("MIPI_PIN_CFG", "SDATA_" + i);
                        tmp_mipiPair[i].SVIO = myDUT.SearchLocalSettingDictionary("MIPI_PIN_CFG", "SVIO_" + i);
                    }
                }

                #endregion

                //LibEqmtDriver.MIPI.Lib_Var.myDM482Address = MIPIaddr;
                //LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(myDUT.SearchLocalSettingDictionary("MIPI_Config","Slave_Address"));
                //LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(myDUT.SearchLocalSettingDictionary("MIPI_Config","Channel_Used"));
                LibEqmtDriver.MIPI.Lib_Var.myDM482Address = MIPIaddr;
                string SlaveAddress = myDUT.SearchLocalSettingDictionary("MIPI_Config", "Slave_Address");
                string ChannelUsed = myDUT.SearchLocalSettingDictionary("MIPI_Config", "Channel_Used");

                //Init
                string AemulusePxi_Path = "C:\\Aemulus\\common\\map_file\\";
                AemulusePxi_Path += AemulusPxi_FileName;
                LibEqmtDriver.MIPI.Lib_Var.HW_Profile = AemulusePxi_Path;
                Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM482e();
                Eq.Site[0]._EqMiPiCtrl.Init_ID(tmp_mipiPair);
                Eq.Site[0]._EqMiPiCtrl.ReadLoadboardsocketID(out string loadboardID, out string socketID);
                string error = "";

                //if (loadboardID == "NaN")
                //{
                //    programLoadSuccess = false;
                //    MessageBox.Show("LoadboardID was not read successfully.\nPlease resolve errors and reload program.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    error = "LoadboardID or ContactorID was not loaded successfully.\nPlease resolve errors and reload program";
                //    return sb.ToString();
                //}

                //if (socketID == "NaN")
                //{
                //    programLoadSuccess = false;
                //    MessageBox.Show("ContactorID was not read successfully.\nPlease resolve errors and reload program.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    error = "LoadboardID or ContactorID was not loaded successfully.\nPlease resolve errors and reload program";
                //    return sb.ToString();
                //}

                //if (loadboardID.Contains("LB-RF1-823"))
                //{
                //    programLoadSuccess = false;
                //    MessageBox.Show("Using non-RF2 loadboard, please swap loadboard to remove this notification!\nPlease resolve errors and reload program.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    error = "Using non-RF2 loadboard, please swap loadboard to remove this notification!\nPlease resolve errors and reload program";
                //    return sb.ToString();
                //}

                //if (!loadboardID.Contains("LB-8233-305") || !socketID.Contains("LN-8233-GXX"))
                //{
                //    programLoadSuccess = false;
                //    MessageBox.Show("Using wrong format NFR loadboard ID, please swap loadboard to remove this notification!\nPlease resolve errors and reload program.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    error = "Using wrong format NFR loadboard ID, please swap loadboard to remove this notification!\nPlease resolve errors and reload program";
                //    return sb.ToString();
                //}

            }
            #endregion
            //Check boolean status of GUI ENTRY

            m_NFTpProd.ShowInputGui(pjtTagNum, SkipProdGUIFlag, myDUT);

            //Ivan - Create alias name for MIPI Card Info
         //   Alias_VST = SearchLocalSettingDictionary("Model", "PXI_VST");

          //  Alias_VST = "VST";
            myDUT.InitInstr(ref sb, NFTestConditionFactory.DicTestPA, NFTestConditionFactory.DicWaveForm, NFTestConditionFactory.DicWaveFormMutate);

       
            //force Clotho to exit if Instrument Init detect failure
            if (!myDUT.InitInstrStatus)
            {
                return TestPlanRunConstants.RunFailureFlag;
            }

            // Grab NI Instrument info
            var hw = new HardwareProperty();
            StringBuilder VST = new StringBuilder();
            StringBuilder Vcc1 = new StringBuilder();
            StringBuilder Vcc2 = new StringBuilder();
            StringBuilder Vbatt = new StringBuilder();
            StringBuilder Vlna = new StringBuilder();
            StringBuilder HSDIO = new StringBuilder();
            Flag1 = true;
            Flag2 = true;
            Flag3 = true;
            Flag4 = true;

            for (int i = 0; i < hw.Count; i++)
            {
                if (hw.ProductName[i].Contains("5644R") || hw.ProductName[i].Contains("5646R"))
                {
                    string Alias_VST = "VST";
                    VST.AppendFormat("{0} = {1}*{2}", Alias_VST, hw.ProductName[i], hw.SerialNumber[i]);
                }
                else
                if (hw.ProductName[i].Contains("6570"))
                {
                    string Alias_HSDIO = "HSDIO";
                    HSDIO.AppendFormat("{0} = {1}*{2}", Alias_HSDIO, hw.ProductName[i], hw.SerialNumber[i]);
                }
                else
                if (hw.ProductName[i].Contains("4139") & MyDUT.Alias_Vcc1.Contains("P1_Ch0_NI4139_NI4139P1") && Flag1 == true)
                {
                    string Alias_Vcc1 = "VCC1-ET40";
                    Vcc1.AppendFormat("{0} = {1}*{2}", Alias_Vcc1, hw.ProductName[i], hw.SerialNumber[i]);
                    Flag1 = false;
                }
                else
                if (hw.ProductName[i].Contains("4139") & MyDUT.Alias_Vcc2.Contains("P1_Ch0_NI4139_NI4139P2") && Flag2 == true)
                {
                    string Alias_Vcc2 = "VCC2-ET100";
                    Vcc1.AppendFormat("{0} = {1}*{2}", Alias_Vcc2, hw.ProductName[i], hw.SerialNumber[i]);
                    Flag2 = false;
                }
                else
                if (hw.ProductName[i].Contains("4143") & MyDUT.Alias_Vbatt.Contains("P1_Ch0_NI4143_NI4143P3") && Flag3 == true)
                {
                    string Alias_Vbatt = "VBATT";
                    Vbatt.AppendFormat("{0} = {1}*{2}", Alias_Vbatt, hw.ProductName[i], hw.SerialNumber[i]);
                    Flag3 = false;
                }
                else
                if (hw.ProductName[i].Contains("4139") & MyDUT.Alias_Vlna.Contains("P1_Ch1_NI4139_NI4139P3") && Flag4 == true)
                {
                    string Alias_Vlna = "VLNA";
                    Vlna.AppendFormat("{0} = {1}*{2}", Alias_Vlna, hw.ProductName[i], hw.SerialNumber[i]);
                    Flag4 = false;
                }
                else
                if (hw.ProductName[i].Contains("4143") & MyDUT.Alias_Vbatt.Contains("P1_Ch0_NI4143_NI4143P1") & MyDUT.Alias_Vlna.Contains("P1_Ch1_NI4143_NI4143P1"))
                {
                    string Alias_Vbatt = "VBATT";
                    Vbatt.AppendFormat("{0} = {1}*{2}", Alias_Vbatt, hw.ProductName[i], hw.SerialNumber[i]);
                    string Alias_Vlna = "VLNA";
                    Vlna.AppendFormat("{0} = {1}*{2}", Alias_Vlna, hw.ProductName[i], hw.SerialNumber[i]);
                }
            }

            //Grab Aemulus card into and store in summary files
            var aemHW = new AemHardwareProperty();
            Flag1 = true;
            Flag2 = true;
            Flag3 = true;
            Flag4 = true;
            for (int i = 0; i < aemHW.Count; i++)
            {
                if (aemHW.ProductName[i].Contains("471") && MyDUT.Alias_Vcc1.Contains("P1_Ch0_AM471E_AM471E01") && Flag1 == true)
                {
                    string Alias_Vcc1 = "VCC1-ET40";
                    Vcc1.AppendFormat("{0} = {1}*{2}", Alias_Vcc1, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                    Flag1 = false;
                }
                else
                if (aemHW.ProductName[i].Contains("471") & MyDUT.Alias_Vcc2.Contains("P1_Ch0_AM471E_AM471E02") && Flag2 == true)
                {
                    string Alias_Vcc2 = "VCC2-ET100";
                    Vbatt.AppendFormat("{0} = {1}*{2}", Alias_Vcc2, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                    Flag2 = false;
                }
                else
                if (aemHW.ProductName[i].Contains("471") & MyDUT.Alias_Vbatt.Contains("P1_Ch0_AM471E_AM471E03") && Flag3 == true)
                {
                    string Alias_Vbatt = "VBATT";
                    Vbatt.AppendFormat("{0} = {1}*{2}", Alias_Vbatt, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                    Flag3 = false;
                }
                else
                if (aemHW.ProductName[i].Contains("471") & MyDUT.Alias_Vlna.Contains("P1_Ch0_AM471E_AM471E04") && Flag4 == true)
                {
                    string Alias_Vlna = "VLNA";
                    Vlna.AppendFormat("{0} = {1}*{2}", Alias_Vlna, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                    Flag4 = false;
                }
                else
                if (aemHW.ProductName[i].Contains("430") & MyDUT.Alias_Vbatt.Contains("P1_Ch0_AM430E_AM430E01") & MyDUT.Alias_Vlna.Contains("P1_Ch1_AM430E_AM430E01"))
                {
                    string Alias_Vbatt = "VBATT";
                    Vbatt.AppendFormat("{0} = {1}*{2}", Alias_Vbatt, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                    string Alias_Vlna = "VLNA";
                    Vlna.AppendFormat("{0} = {1}*{2}", Alias_Vlna, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                }
                else
                if (aemHW.ProductName[i].Contains("DM"))
                {
                    String Alias_HSDIO = "HSDIO";
                    HSDIO.AppendFormat("{0} = {1}*{2}", Alias_HSDIO, aemHW.ProductName[i], aemHW.SerialNumber[i]);
                }
            }

            string InstrumentInfo_VST = VST.ToString();
            string InstrumentInfo_SwitchBox = MyDUT.InstrumentInfo_SwitchBox;
            string InstrumentInfo_Vcc1_ET40 = Vcc1.ToString();
            string InstrumentInfo_Vcc2_ET100 = Vcc2.ToString();
            string InstrumentInfo_Vbatt = Vbatt.ToString();
            string InstrumentInfo_Vlna = Vlna.ToString();
            string InstrumentInfo_HSDIO = HSDIO.ToString();

            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_INSTRUMENT_INFO, InstrumentInfo_VST + ";" + InstrumentInfo_SwitchBox + ";" + InstrumentInfo_Vcc1_ET40 + ";" + InstrumentInfo_Vcc2_ET100 + ";" + InstrumentInfo_Vbatt + ";" + InstrumentInfo_Vlna + ";" + InstrumentInfo_HSDIO + ";");

            myDUT.mfgLotID = m_NFTpProd.prodInputForm.MfgLotID;
            //myDUT.deviceID = m_NFTpProd.prodInputForm.DeviceID;

            //Ivan - Dpat
            bool isSuccess1 = SetDPatOutlier(MyDUT.DpatEnable);

            count_1A = (MyDUT.StopOnContinueFail1A) * -1;
            count_2A = (MyDUT.StopOnContinueFail2A) * -1;
            #endregion initialize Equipment

            // Loading CF and TL
            ResultBuilder.LoadingCFandTL();

            #region GU Cal
            _GuVerEnable = myDUT.GuVerEnable;

            if (_GuVerEnable)
            {
                try
                {
                    GU.DoInit_afterCustomCode(true, false, myDUT.ProductTag, @"C:\Avago.ATF.Common\Results");
                    ResultBuilder.GuCalFactorsDict = GU.GuCalFactorsDict[GU.GuCalFactorsDict.Keys.First()];
                }
                catch (Exception EX)
                {
                    MPAD_TestTimer.LoggingManager.Instance.LogError(String.Format("[Fail] Fail to proceed a Golden Unit calibration \nMessage : {0}\nStack : {1}", EX.Message, EX.StackTrace));
                }
            }
            else
            {
                if (ResultBuilder.corrFileExists)
                {
                    try
                    {
                        GU.ReadGuCorrelationFile();
                        ResultBuilder.GuCalFactorsDict = GU.GuCalFactorsDict[GU.GuCalFactorsDict.Keys.First()];
                        GU.runningGU = false;
                    }
                    catch (Exception EX)
                    {
                        MPAD_TestTimer.LoggingManager.Instance.LogError(String.Format("[Fail] Fail to read a correlation file \nMessage : {0}\nStack : {1}", EX.Message, EX.StackTrace));
                    }
                }
            }
            #endregion

            // ----------- END of Custom Init Coding --------------- //
            //////////////////////////////////////////////////////////////////////////////////
            #endregion Custom Init Coding Section

            //test time logger
            StopWatchManager.Instance.Stop();

            m_doClotho.Initialize();

            string TestTimeDir = "C:\\Avago.ATF.Common.x64\\Production\\TestTime\\";
            StopWatchManager.Instance.SaveToFile(TestTimeDir + "DoATFInit_TestTimes.txt", "DoATFInit_Times");
            StopWatchManager.Instance.Reset();

            //m_modelTpProd.TestTimeLogController.Initialize(new TestPlanStateModel(),
            //m_doClotho.ConfigXmlPath, myDUT.NFDic.DicTestPA);

            m_NFTpProd.LockClotho();
            vdo = Validate(null);


            foreach (Dictionary<string, string> currTestCond in NFTestConditionFactory.DicTestPA)
            {
                string tmpTestNo = currTestCond["TEST NUMBER"];
                //string _TestMode = myUtility.ReadTcfData(currTestCond, TCF_Header.ConstTestMode);

                MyProduct.MyDUT.NFTestCondition Cs = new MyProduct.MyDUT.NFTestCondition();

                Cs = MyProduct.MyDUT.AllNFtest[tmpTestNo];

                if (currTestCond["TEST PARAMETER"] == "MULTI_DCSUPPLY")
                {

                    Cs._SetDC = Cs._DCSetCh.Split(',');
                    Cs._MeasDC = Cs._DCMeasCh.Split(',');

                    //if (FirstDut_DCSupply)
                    //{
                    for (int i = 0; i < Cs._SetDC.Length; i++)
                    {
                        int dcVChannel = Convert.ToInt16(Cs._SetDC[i]);
                        Eq.Site[0]._Eq_DCSupply[i].SetVolt((dcVChannel), Cs._DCVCh[dcVChannel], Cs._DCILimitCh[dcVChannel]);
                        Eq.Site[0]._Eq_DCSupply[i].DcOn(dcVChannel);
                    }


                    MyProduct.MyDUT.FirstDut_DCSupply = true;

                    for (int i = 0; i < Cs._MeasDC.Length; i++)
                    {
                        int dcIChannel = Convert.ToInt16(Cs._MeasDC[i]);

                        if (Cs._DCILimitCh[dcIChannel] > 0)
                        {

                            Thread.Sleep(100);
                            Cs._R_DC_ICh[dcIChannel] = Eq.Site[0]._Eq_DCSupply[i].MeasI(dcIChannel);

                        }
                    }

                    MyProduct.MyDUT.FirstDut = false;
                }

                break;
            }


            Thread.Sleep(1000);

            ATFReturnResult result = new ATFReturnResult();

            myDUT.Dummy_RunTest(ref result);

            ChkPauseOnDuplicatedID();

            if (ClothoDataObject.Instance.EnableOnlySeoulUser) Helper.UnsubscribeSystemEvents();

            return sb.ToString();
        }


        public override string DoATFUnInit(string args)
        {
            //Debugger.Break();

            base.DoATFUnInit(args);

            #region Custom UnInit Coding Section
            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom UnInit Coding here --------------- //
            myDUT.Dispose();

            for (int i = 0; i < Eq.Site.Length; i++)
            {
                Eq.Site[i] = null;
            }

            #region GU Cal
            if (_GuVerEnable)
                GU.forceReload = false;
            #endregion

            #region Equipment CalInfo

            if (File.Exists(@"C:\Program Files\PXIe Calibration Log\PXIe Calibration Log.exe"))
                System.Diagnostics.Process.Start(@"C:\Program Files\PXIe Calibration Log\PXIe Calibration Log.exe");

            #endregion;

            //if (PauseOnDuplicateID) DicOTPModuleID.Clear();

            // ----------- END of Custom UnInit Coding --------------- //
            //////////////////////////////////////////////////////////////////////////////////
            #endregion Custom UnInit Coding Section

            return args;
        }


        public override string DoATFLot(string args)
        {
            Debugger.Break();

            return base.DoATFLot(args);
        }

        public override ATFReturnResult DoATFTest(string args)
        {
            //Debugger.Break();
            StopWatchManager.Instance.Start();
            StopWatchManager.Instance.Start("TIME_DoATFTest");

            string err = "";
            StringBuilder sb = new StringBuilder();
            ATFReturnResult result = new ATFReturnResult();

            // ----------- Example for Argument Parsing --------------- //
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (!ArgParser.parseArgString(args, ref dict))
            {
                err = "Invalid Argument String" + args;
                MessageBox.Show(err, "Exit Test Plan Run", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new ATFReturnResult(err);

            }

            int simHW;
            try
            {
                simHW = ArgParser.getIntItem(ArgParser.TagSimMode, dict);
            }
            catch (Exception ex)
            {
                err = ex.Message;
                MessageBox.Show(err, "Exit Test Plan Run", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new ATFReturnResult(err);
            }
            // ----------- END of Argument Parsing Example --------------- //

            #region Custom Test Coding Section
            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom Test Coding here --------------- //
            // Example for build TestPlan Result (Single Site)

            if (FirstTest == true)
            {
                ResultBuilder.M_Previous_OTP_MODULE_ID_2DID = -1;  //DH DuplicateID
                ResultBuilder.M_OTP_MODULE_ID_MIPI = -1;
                ResultBuilder.M_Previous_OTP_CheckAll = -1;
                
                //string[] ResultFileName = ATFCrossDomainWrapper.GetClothoCurrentResultFileFullPath().Split('_');

                //if (GUI_Enable == true)
                //{
                //    if (ResultFileName[0] != InitProTag)
                //    {
                //        programLoadSuccess = false;
                //        MessageBox.Show("Product Tag accidentally changed to: " + ResultFileName[0] + "\nPlease re-load program!");
                //        err = "Product Tag accidentally changed to: " + ResultFileName[0];
                //        return new ATFReturnResult(err); ;
                //    }
                //}
            }
            ResultBuilder.M_OTP_CheckAll = 0;

            Regex regSublot1st = new Regex(@"^1[A-Z]$");
            Regex regSublot2nd = new Regex(@"^2[A-Z]$");

            if (regSublot1st.IsMatch(SublotId))
            {
                if (passcount == count_1A)
                {
                    failcount = 0;
                    ATFLogControl.Instance.Log(LogLevel.Error, LogSource.eHandler, "Unit under testing Continue Failures more than " + count_1A + " times!!!");
                    MessageBox.Show("Unit under testing Continue Failures more than " + (count_1A * -1) + " times!!!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    programLoadSuccess = false;
                    //return new ATFReturnResult(TestPlanRunConstants.RunSkipFlag);
                }
            }
            else if (regSublot2nd.IsMatch(SublotId))
            {
                if (passcount == count_2A)
                {
                    failcount = 0;
                    ATFLogControl.Instance.Log(LogLevel.Error, LogSource.eHandler, "Unit under testing Continue Failures more than " + count_2A + " times!!!");
                    MessageBox.Show("Unit under testing Continue Failures more than " + (count_2A * -1) + " times!!!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    programLoadSuccess = false;
                    //return new ATFReturnResult(TestPlanRunConstants.RunSkipFlag);
                }
            }

            if (!programLoadSuccess)
            {
                MessageBox.Show("Program was not loaded successfully.\nPlease resolve errors and reload program.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                err = "Program was not loaded successfully.\nPlease resolve errors and reload program";
                return new ATFReturnResult(err);
            }

            #region GU CAL
            if (_GuVerEnable)
                GU.DoTest_beforeCustomCode();
            #endregion

            #region Retrieve lot ID# (for Datalog)
            //Retrieve lot ID#
            tPVersion = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TP_VER, "");
            ProductTag = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, "").ToUpper();
            lotId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "").ToUpper();
            SublotId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_SUB_LOT_ID, "").ToUpper();
            WaferId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_WAFER_ID, "");
            OpId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_OP_ID, "");
            HandlerSN = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_SN, "");
            TesterHostName = System.Net.Dns.GetHostName();
            ipEntry = System.Net.Dns.GetHostEntry(TesterHostName);
            //TesterIP = ipEntry.AddressList[0].ToString().Replace(".", ""); //Always default to the 1st network card. This is because for Result FileName , clotho always take the 1st nework id return by system
            TesterIP = NetworkHelper.GetStaticIPAddress().Replace(".", "");      //Use Clotho method , original code has issue with IPv6 - 12/03/2015 Shaz

            if (myDUT.tmpUnit_No == 0)      //do this for the 1st unit only
            {
                DT = DateTime.Now;

                if (ProductTag != "" && lotId != "")
                {
                    //// SnP file Dir generation            
                    try
                    {
                        lotId = lotId.Replace("\t", "");
                        newPath = System.IO.Path.Combine(activeDir, ProductTag + "_" + lotId + "_" + SublotId + "_" + TesterIP + "\\");
                    }

                    catch (Exception ex)
                    {
                    }
                    //System.IO.Directory.CreateDirectory(newPath);
                    //FileName = System.IO.Path.Combine(activeDir, ProductTag + "_" + lotId + "_" + SublotId + "_" + TesterIP + "\\" + lotId + ".txt");
                }
                else
                {
                    string tempname = "DebugMode_" + DT.ToString("yyyyMMdd" + "_" + "HHmmss");
                    newPath = System.IO.Path.Combine(activeDir, tempname + "\\");
                    //System.IO.Directory.CreateDirectory(newPath);
                    ProductTag = "Debug";
                    //FileName = System.IO.Path.Combine(activeDir, tempname + "\\" + "DebugMode" + ".txt");
                }

                //Parse information to LibFbar
                myDUT.SNPFile.FileOutput_Path = newPath;
                myDUT.SNPFile.FileOutput_FileName = ProductTag;
                InitSNP = true;

                // Added variable to solve issue with datalog when Inari operator using 
                //Tally Generator to close lot instead of unload test plan
                //WaferId,OpId and HandlerSN are null when 2nd Lot started - make assumption that this 3 param are similar 1st Lot
                tempWaferId = WaferId;
                tempOpId = OpId;
                tempHandlerSN = HandlerSN;
                previous_LotSubLotID = current_LotSubLotID;
            }
            #endregion

#if (!DEBUG)
        myDUT.tmpUnit_No = Convert.ToInt32(ATFCrossDomainWrapper.GetClothoCurrentSN());
#else
            myDUT.tmpUnit_No++;      // Need to enable this during debug mode
#endif


            #region GU CAL
            if (_GuVerEnable)
            {
                for (GU.runLoop = 1; GU.runLoop <= GU.numRunLoops; GU.runLoop++)
                {

                    DoNFTest(ref result);

                    GU.DoTest_afterCustomCode(ref result);
                }
            }

            else
            {
                DoNFTest(ref result);
            }
            #endregion

            #region Test Time Dashboard Logger

            if (FirstTest != true)
            {
                StopWatchManager.Instance.Stop("TIME_DoATFTest");
                //m_modelTpProd.TestTimeLogController.Save();
                StopWatchManager.Instance.Stop();
                //StopWatchManager.Instance.SaveToFile(@"C:\TEMP\m1.csv", "runtest", ',');
                StopWatchManager.Instance.Clear();
            }
            else
            {
                StopWatchManager.Instance.Clear();  //need to clear 1st unit test time data to ensure that datalogger used data from 2nd passing unit
            }

            #endregion Test Time Dashboard Logger

            //long mfg_id = mfgid_options_to_use[DateTime.Now.Millisecond % 3];
            //long otp_module_id = _dutCount + DateTime.Now.Millisecond % 4;


            // Start from V3.1.1, Clotho FORBID return OTP values through test plan result. Instead you MUST call Set API to dispatch <long, long> OTP value to Clotho

            // Following two lines will trigger "Failure and Block Further Test" 
            //  ATFResultBuilder.AddResult(ref ret, ATFOTPConstants.TAG_MFG_ID, "", mfg_id);
            // ATFResultBuilder.AddResult(ref ret, ATFOTPConstants.TAG_MODULE_ID, "", otp_module_id); 

            // Following 1 line is the correct way
            //ATFCrossDomainWrapper.SetMfgIDAndModuleIDBySite(1, mfgid_options_to_use[DateTime.Now.Millisecond % 3], _dutCount);

            // ----------- END of Custom Test Coding --------------- //
            //////////////////////////////////////////////////////////////////////////////////
            #endregion Custom Test Coding Section

            if (ClothoDataObject.Instance.EnableOnlySeoulUser && ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.GenPackageNFR))
            {
                MyProduct.ResultBuilder.results = result;
                var sList = MyProduct.ResultBuilder.results.Data.Select(s => s.Name).ToList();
                ClothoDataObject.Instance.PackageHelper.GeneratePackage(sList, ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TCF_VER, ""));
            }

            #region check continue fail units count

            //coding....
            currentCount = (MyProduct.ResultBuilder.FailedTests[0].Count > 0);

            if (currentCount == previousCount)
            {
                failcount++;
            }
            else
            {
                failcount = 0;
                previousCount = currentCount;
                failcount++;
            }

            if (MyProduct.ResultBuilder.FailedTests[0].Count > 0)
            {
                passcount = failcount * -1;
                ATFResultBuilder.AddResult(ref result, "M_CONTINUOUS-PASSFAIL-COUNT", "NA", Convert.ToDouble(passcount));
            }
            else
            {
                ATFResultBuilder.AddResult(ref result, "M_CONTINUOUS-PASSFAIL-COUNT", "NA", Convert.ToDouble(failcount));
            }

            #endregion check continue fail units count

            ChkDuplicatedDUT();

            FirstTest = false;      //clear first test flag
            ResultBuilder.M_Previous_OTP_MODULE_ID_MIPI = ResultBuilder.M_OTP_MODULE_ID_MIPI;
            //ResultBuilder.M_Previous_OTP_MODULE_ID_2DID = ResultBuilder.M_OTP_MODULE_ID_2DID;
            ResultBuilder.M_Previous_OTP_CheckAll = ResultBuilder.M_OTP_CheckAll;

            return result;

        }
        private void LockClothoInputUI()
        {
            Clotho.EnableClothoTextBoxes(false);
            Thread.Sleep(5);
            Clotho.EnableClothoTextBoxes(false);
            Thread.Sleep(10);
            Clotho.EnableClothoTextBoxes(false);
            Thread.Sleep(15);
            Clotho.EnableClothoTextBoxes(false);
            Thread.Sleep(20);
            Clotho.EnableClothoTextBoxes(false);
        }

        public void DoNFTest(ref ATFReturnResult result)
        {
            ATFResultBuilder.Reset();
            //ResultBuilder.DuplicatedModuleID[0] = false;

            try
            {
                myDUT.b_GE_Header = Convert.ToBoolean(myDUT.DicCalInfo[DataFilePath.Enable_GEHeader]);
            }
            catch
            {
                myDUT.b_GE_Header = false;
            }

            //if (!myDUT.b_GE_Header)
            //{
            //    ATFResultBuilder.AddResult(ref result, "MFG_LOTID", "NA", Convert.ToDouble(myDUT.mfgLotID));
            //}
            //else
            //{
            //    ATFResultBuilder.AddResult(ref result, "M_MIPI_OTPBURN_x_x_x_x_x_x_x_x_x_x_x_x_x_x_x_NOTE_SCANNED", "NA", Convert.ToDouble(myDUT.mfgLotID));
            //}

            if (programLoadSuccess == true)
            {
                try
                {
                    myDUT.RunTest(ref result);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public void ChkPauseOnDuplicatedID()
        {

            try
            {
                PauseOnDuplicateID = Convert.ToBoolean(myDUT.DicCalInfo[DataFilePath.CheckDuplicateID]);
            }
            catch
            {
                PauseOnDuplicateID = true;
            }
        }

        public bool ChkDuplicatedDUT()
        {
            #region Duplicate Module ID
            if (PauseOnDuplicateID && !AvagoGU.GU.runningGU)
            {
                //if (FirstTest ||
                //    ((ResultBuilder.M_Previous_OTP_MODULE_ID != ResultBuilder.M_OTP_MODULE_ID)
                //    || ((ClothoDataObject.Instance.EnableOnlySeoulUser
                //    && Math.Round((double)ResultBuilder.M_OTP_MODULE_ID, 0) == 0))))

                bool bDuplicatedModuleIdChk = false;
                if (ClothoDataObject.Instance.EnableOnlySeoulUser)
                {
                    if (ResultBuilder.M_Previous_OTP_MODULE_ID_MIPI != ResultBuilder.M_OTP_MODULE_ID_MIPI) bDuplicatedModuleIdChk = true;
                }
                else
                {
                    //if (ResultBuilder.M_Previous_OTP_MODULE_ID_2DID != ResultBuilder.M_OTP_MODULE_ID_2DID || ResultBuilder.M_Previous_OTP_CheckAll != ResultBuilder.M_OTP_CheckAll)  bDuplicatedModuleIdChk = true;
                    if (ResultBuilder.M_Previous_OTP_MODULE_ID_MIPI != ResultBuilder.M_OTP_MODULE_ID_MIPI || ResultBuilder.M_Previous_OTP_CheckAll != ResultBuilder.M_OTP_CheckAll) bDuplicatedModuleIdChk = true;
                }

                if (FirstTest || 
                    (bDuplicatedModuleIdChk || (ClothoDataObject.Instance.EnableOnlySeoulUser && Math.Round((double)ResultBuilder.M_OTP_MODULE_ID_MIPI, 0) == 0)))
                {
                    //DicOTPModuleID.Add(ResultBuilder.M_OTP_MODULE_ID, 1);
                    ResultBuilder.DuplicatedModuleID[0] = false;
                }
                else
                {
                    ResultBuilder.DuplicatedModuleID[0] = true;
                }

                ResultBuilder.M_Previous_OTP_MODULE_ID_MIPI = ResultBuilder.M_OTP_MODULE_ID_MIPI;
                //ResultBuilder.M_Previous_OTP_MODULE_ID_2DID = ResultBuilder.M_OTP_MODULE_ID_2DID;
                ResultBuilder.M_Previous_OTP_CheckAll = ResultBuilder.M_OTP_CheckAll;

                if (ResultBuilder.DuplicatedModuleID[0] == true)
                {
                    ProductionLib2.InspectSocketMessage formDoubleUnit = new ProductionLib2.InspectSocketMessage();

                    if (ResultBuilder.DuplicatedModuleIDCtr[0] < 2 || ClothoDataObject.Instance.EnableOnlySeoulUser)
                    {
                        formDoubleUnit.txtErrMsg.Text = "!!! Duplicated Module ID detected !!!";
                        formDoubleUnit.txtRectifyMsg.Text = "Please inspect test socket and then rectify the problem to resume test";

                        ResultBuilder.DuplicatedModuleIDCtr[0]++;     //increase counter if duplicate ID detected

                        // MessageBox.Show("Duplicated Module ID detected, test aborted \n\n" +
                        //"Please inspect test socket and then rectify the problem to resume test", "!! SUSPECTED DUPLICATE UNIT !!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        ATFLogControl.Instance.Log(LogLevel.Warn, LogSource.eHandler, "Duplicated Module ID detected" + Environment.NewLine + "Please inspect test socket and then rectify the problem to resume test");

                        DialogResult rslt = formDoubleUnit.ShowDialog();

                        //return new ATFReturnResult(TestPlanRunConstants.RunFailureFlag);
                        return true;
                    }
                    else
                    {
                        formDoubleUnit.txtErrMsg.Text = "Duplicated Module ID detected for > 3 times , test aborted";
                        formDoubleUnit.txtRectifyMsg.Text = "Please inspect test socket .. Please UnInit and Reload test plan to continue";

                        ATFLogControl.Instance.Log(LogLevel.Error, LogSource.eHandler, "Duplicated Module ID detected for > 3 times, reload test plan to continue");

                        DialogResult rslt = formDoubleUnit.ShowDialog();

                        programLoadSuccess = false;

                        throw new Exception("Exceeded 3 times of duplicated Module ID");

                        //result = new ATFReturnResult();        //force all result to be blank  because of failure to address duplicate ID issue
                        //return result;
                    }
                }
                else
                {
                    ResultBuilder.DuplicatedModuleIDCtr[0] = 0;   //reset back to 0 if no continuous duplucate ID detected
                    return false;
                }
            }
            else return false;


            #endregion Duplicate Module ID      
        }
        //Ivan - DPat
        public bool SetDPatOutlier(bool DPAT_Flag)
        {
            bool isOlfFileAvailable = false; //From Clotho
                                             //bool RunDPAT = false;
                                             //isOlfFileAvailable = ATFRTE.Instance.SetOutlierCheckFlag(DPAT_Flag);
                                             //LoggingManager.Instance.LogInfo("DPAT is set to " + DPAT_Flag.ToString());
                                             //LoggingManager.Instance.LogInfo("OLF file availability is " + isOlfFileAvailable.ToString());

            string lookForProcess = "RF1";
            Tuple<bool, int> setOutlierCheckFlagRespond = ATFRTE.Instance.SetOutlierCheckFlag(DPAT_Flag, lookForProcess);
            isOlfFileAvailable = setOutlierCheckFlagRespond.Item1;
            //MessageBox.Show(isOlfFileAvailable.ToString());
            LoggingManager.Instance.LogInfo("DPAT is set to " + DPAT_Flag.ToString());
            LoggingManager.Instance.LogInfo("OLF file availability is " + isOlfFileAvailable.ToString());

            if (isOlfFileAvailable || !DPAT_Flag) //no issue
            {
                LoggingManager.Instance.LogInfo("DPAT test program loading without issue.");
                return true;
            }
            else
            {
                DialogResult result = MessageBox.Show("Outlier Spec Un-Available. Suggest to Abort Loading.\nSelect 'Yes' to Abort program.\nSelect 'No' to continue program loading without DPAT.", "Outlier Spec UnAvailable Warn!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    // this is default                                 
                    // Opt to Abort 
                    MessageBox.Show("Please click 'OK' and unload program...", "Test program stop loading");
                    return false;
                }
                else if (result == DialogResult.No)
                {
                    int loop = 0;
                    while (loop < 3)
                    {

                        DpatBox DpatBypassBox = new DpatBox();
                        DialogResult BoxRslt = DpatBypassBox.ShowDialog();

                        if (BoxRslt == DialogResult.OK)
                        {
                            MyDUT.DpatEnable = false;
                            ATFLogControl.Instance.Log(LogLevel.Warn, LogSource.eTestPlan, "Outlier Spec Un-Available. Operator select to continue by ignoring Outlier Check");
                            return true;
                        }
                        else if (BoxRslt == DialogResult.Retry)
                        {
                            loop++;
                        }

                        else if (BoxRslt == DialogResult.Cancel)
                        {
                            //programLoadSuccess = false;
                            MessageBox.Show("Please click 'OK' and unload program...", "Test program stop loading");
                            return false;
                            //return TestPlanRunConstants.RunFailureFlag + ": Outlier Spec Un-Available";
                        }
                    }

                    if (loop >= 3)
                    {
                        //programLoadSuccess = false;
                        MessageBox.Show("Too many tries, please click ok to stop testing!", "Test program stop loading");
                        return false;
                        //return TestPlanRunConstants.RunFailureFlag + ": Outlier Spec Un-Available, too many password incorrect tries.";
                    }
                }
            }
            return false;
        }

    }
}
