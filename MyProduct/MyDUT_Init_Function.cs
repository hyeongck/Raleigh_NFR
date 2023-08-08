using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Avago.ATF.StandardLibrary;
using Ivi.Visa.Interop;
using LibEqmtDriver;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
//using ni_NoiseFloor;
using NationalInstruments.RFmx.InstrMX;
using NationalInstruments.RFmx.SpecAnMX;
using MPAD_TestTimer;
using TCPHandlerProtocol;
using ni_NoiseFloorWrapper;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using System.Threading.Tasks;
using ClothoSharedItems;
using Avago.ATF.Shares;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MyProduct
{
    public partial class MyDUT : IDisposable
    {
        private void ReadPaTCF()
        {
            //myUtility.ReadTCF(ConstPASheetNo, ConstPAIndexColumnNo, ConstPATestParaColumnNo, ref DicTestPA);
            //myUtility.ReadTCF(TCF_Sheet.ConstPASheetNo, TCF_Sheet.ConstPAIndexColumnNo, TCF_Sheet.ConstPATestParaColumnNo, ref DicTestPA, ref DicTestLabel);
            myUtility.ReadTCF(TCF_Sheet.ConstPASheetName, TCF_Sheet.ConstPAIndexColumnNo, TCF_Sheet.ConstPATestParaColumnNo, ref DicTestPA, ref DicTestLabel);
        }
        private void ReadCalTCF()
        {
            //myUtility.ReadCalSheet(TCF_Sheet.ConstCalSheetNo, TCF_Sheet.ConstCalIndexColumnNo, TCF_Sheet.ConstCalParaColumnNo, ref DicCalInfo);
            myUtility.ReadCalSheet(TCF_Sheet.ConstCalSheetName, TCF_Sheet.ConstCalIndexColumnNo, TCF_Sheet.ConstCalParaColumnNo, ref DicCalInfo);
        }
        private void ReadWafeForm()
        {
            //myUtility.ReadWaveformFilePath(TCF_Sheet.ConstKeyWordSheetNo, TCF_Sheet.ConstWaveFormColumnNo, ref DicWaveForm);  //remark and replace by additional dic for mutateWaveform (Shaz - 12/05/2016)
            //myUtility.ReadWaveformFilePath(TCF_Sheet.ConstKeyWordSheetNo, TCF_Sheet.ConstWaveFormColumnNo, ref DicWaveForm, ref DicWaveFormMutate);
            //myUtility.ReadWaveformFilePath(TCF_Sheet.ConstKeyWordSheetName, TCF_Sheet.ConstWaveFormColumnNo, ref DicWaveForm, ref DicWaveFormMutate);
            myUtility.ReadWaveformFilePath(TCF_Sheet.ConstKeyWordSheetName, TCF_Sheet.ConstWaveFormColumnNo, ref DicWaveForm, ref DicWaveFormMutate, ref DicWaveFormAlias);
        }
        private void ReadMipiReg()
        {
            //myUtility.ReadMipiReg(TCF_Sheet.ConstMipiRegSheetNo, TCF_Sheet.ConstMipiKeyIndexColumnNo, TCF_Sheet.ConstMipiRegColumnNo, ref DicMipiKey);
            myUtility.ReadMipiReg(TCF_Sheet.ConstMipiRegSheetName, TCF_Sheet.ConstMipiKeyIndexColumnNo, TCF_Sheet.ConstMipiRegColumnNo, ref DicMipiKey);
        }
        private void ReadPwrBlast()
        {
            //myUtility.ReadPwrBlast(TCF_Sheet.ConstPwrBlastSheetNo, TCF_Sheet.ConstPwrBlastIndexColumnNo, TCF_Sheet.ConstPwrBlastColumnNo, ref DicPwrBlast);
            myUtility.ReadPwrBlast(TCF_Sheet.ConstPwrBlastSheetName, TCF_Sheet.ConstPwrBlastIndexColumnNo, TCF_Sheet.ConstPwrBlastColumnNo, ref DicPwrBlast);
        }
        private void LoadTCFandSettingFiles(ref StringBuilder sb)
        {
            #region Load TCF

            //ReadWafeForm();

            //ManualResetEvent[] DoneEvents = new ManualResetEvent[5];
            //DoneEvents[0] = new ManualResetEvent(false);
            //DoneEvents[1] = new ManualResetEvent(false);
            //DoneEvents[2] = new ManualResetEvent(false);
            //DoneEvents[3] = new ManualResetEvent(false);
            //DoneEvents[4] = new ManualResetEvent(false);

            //ThreadWithDelegate ThLoadPaTCF = new ThreadWithDelegate(DoneEvents[0]);
            //ThLoadPaTCF.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadPaTCF);
            //ThreadPool.QueueUserWorkItem(ThLoadPaTCF.ThreadPoolCallback, 0);

            //ThreadWithDelegate ThLoadWaveForm = new ThreadWithDelegate(DoneEvents[1]);
            //ThLoadWaveForm.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadWafeForm);
            //ThreadPool.QueueUserWorkItem(ThLoadWaveForm.ThreadPoolCallback, 0);

            //ThreadWithDelegate ThLoadCalTCF = new ThreadWithDelegate(DoneEvents[2]);
            //ThLoadCalTCF.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadCalTCF);
            //ThreadPool.QueueUserWorkItem(ThLoadCalTCF.ThreadPoolCallback, 0);

            //ThreadWithDelegate ThLoadMipiReg = new ThreadWithDelegate(DoneEvents[3]);
            //ThLoadMipiReg.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadMipiReg);
            //ThreadPool.QueueUserWorkItem(ThLoadMipiReg.ThreadPoolCallback, 0);

            //ThreadWithDelegate ThLoadPwrBlast = new ThreadWithDelegate(DoneEvents[4]);
            //ThLoadPwrBlast.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadPwrBlast);
            //ThreadPool.QueueUserWorkItem(ThLoadPwrBlast.ThreadPoolCallback, 0);

            //WaitHandle.WaitAll(DoneEvents);

            ReadCalTCF();

            #region Retrieve Cal Sheet Info

            CalFilePath = Convert.ToString(DicCalInfo[DataFilePath.CalPathRF]);
            LocSetFilePath = Convert.ToString(DicCalInfo[DataFilePath.LocSettingPath]);
            StopOnContinueFail1A = Convert.ToInt32(DicCalInfo[DataFilePath.StopOnContinueFail1A]);
            StopOnContinueFail2A = Convert.ToInt32(DicCalInfo[DataFilePath.StopOnContinueFail2A]);

            //Ivan - Arm Reject
            HandlerArmYieldDeltaEnable = Convert.ToBoolean(DicCalInfo[DataFilePath.HandlerArmYieldDeltaEnable]);
            HandlerArmTestCount = Convert.ToInt32(DicCalInfo[DataFilePath.HandlerArmTestCount]);
            HandlerArmThreshold = Convert.ToInt32(DicCalInfo[DataFilePath.HandlerArmThreshold]);

            //Ivan - Dpat
            DpatEnable = Convert.ToBoolean(DicCalInfo[DataFilePath.DpatEnable]);

            //Ivan - Delta2DID
            Delta2DIDCheckEnable = Convert.ToBoolean(DicCalInfo[DataFilePath.Delta2DIDCheckEnable]);

            //Ivan - DeltaMfgID
            DeltaMfgIDCheckEnable = Convert.ToBoolean(DicCalInfo[DataFilePath.DeltaMfgIDCheckEnable]);

            //Ivan - webQueryValidation
            WebQueryValidation = Convert.ToBoolean(DicCalInfo[DataFilePath.WebQueryValidation]);

            //Ivan - LOCAL_GUDB_
            LOCAL_GUDB_Enable = Convert.ToBoolean(DicCalInfo[DataFilePath.LOCAL_GUDB_Enable]);
            ClothoDataObject.Instance.LOCAL_GUDB_Enable = LOCAL_GUDB_Enable;

            string tmpBdLossFile = GetTestPlanPath() + @"BOARDLOSS\";
            try
            {
                BoardlossFilePath = tmpBdLossFile + Convert.ToString(DicCalInfo[DataFilePath.BoardLossPath]);
            }
            catch
            {
                //do noting
            }

            #region GU Cal
            try
            {
                GuVerEnable = Convert.ToBoolean(DicCalInfo[DataFilePath.GuVerEnable]);
            }
            catch
            {
                //do nothing
            }


            //#if (!DEBUG)
            //if (GuVerEnable)
            //{
            //    ProductTag = DicCalInfo[DataFilePath.GuPartNo].ToUpper().TrimEnd(' ');
            //    if (ProductTag == string.Empty | !ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, "").ToUpper().Contains(ProductTag))
            //    {
            //        InitSuccess = false;
            //        MessageBox.Show("TCF Main sheet GuPartNo " + ProductTag + " not match with test package load " +
            //            ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, "").ToUpper(), "Read Main Sheet Error", MessageBoxButtons.OK);
            //    }
            //}
            //#else
            if (GuVerEnable)
            {
                ProductTag = DicCalInfo[DataFilePath.GuPartNo].ToUpper().TrimEnd(' ');
            }
            //#endif


            #endregion GU Cal

            #endregion Retrieve Cal Sheet Info

            #endregion Load TCF

            #region Load Cal Data

            try
            {
                ATFCrossDomainWrapper.Cal_SwitchInterpolationFlag(true);
                ATFCrossDomainWrapper.Cal_LoadCalData(LocalSetting.CalTag, CalFilePath);
                ATFCrossDomainWrapper.Cal_LoadCalData(LocalSetting.BoardLossTag, BoardlossFilePath);

                // Get Data from File & Make a Dictionary
                myUtility.LoadCalFreqListandGenerateDic(CalFilePath, "1D-Combined", ref myUtility.DicLocalCableLossFile);
                myUtility.LoadCalFreqListandGenerateDic(BoardlossFilePath, "1D-Combined", ref myUtility.DicLocalBoardLossFile);
            }
            catch (Exception ex)
            {
                if (DicTestPA[0].ContainsValue("Calibration"))
                {
                    //Do Nothing
                }
                else
                {
                    // show error
                    sb.AppendFormat("Fail to Load 1D Cal Data from {0}: {1}\n", CalFilePath, ex.Message);

                    MessageBox.Show(ex.Message);
                    logger.Log(LogLevel.Error, LogSource.eTestPlan, ex.Message);
                }

                return;
            }
            #endregion

            #region Load Local Setting File

            myUtility.GenerateDicLocalFile(LocSetFilePath);
            //DicLocalSettingFile = myUtility.DicLocalfile;

            //Read & Set DC & SMU biasing status - OFF/ON for every DUT after complete test
            BiasStatus.DC = Convert.ToBoolean(SearchLocalSettingDictionary("OFF_AfterTest", "DC"));
            BiasStatus.SMU = Convert.ToBoolean(SearchLocalSettingDictionary("OFF_AfterTest", "SMU"));

            //Read Stop On Failure status mode - True (program will stop testing if failure happen) , false (proceed per normal)
            StopOnFail.TestFail = false;      //init to default 
            StopOnFail.Enable = Convert.ToBoolean(SearchLocalSettingDictionary("STOP_ON_FAIL", "ENABLE"));

            #endregion

            #region Check TCF, Local Setting File & Cal Data

            #region Check TCF
            // Check Tx Freq, Rx Freq, Switch Path (Gain Rx Path, Tx pout Path, ANT NF Path, ANT Tx Path)

            #endregion

            #region Check Local Setting File

            #endregion

            #region Check Cal Data

            #endregion

            #endregion
        }
        // Move out
        public void InitInstr(ref StringBuilder sb, Dictionary<string, string>[] NFDicTestPA, Dictionary<string, string> NFDicWaveForm, Dictionary<string, string> NFDicWaveFormMutate)
        {
            #region Instrument Init

            Task taskInstrInit = new Task(() => { InstrInit(LocSetFilePath, NFDicTestPA, NFDicWaveForm, NFDicWaveFormMutate); });
            taskInstrInit.Start();
            taskInstrInit.Wait();

            //InstrInit(LocSetFilePath);
            #endregion
        }
        private void UnInit()
        {
            //var processes = from p in System.Diagnostics.Process.GetProcessesByName("EXCEL") select p;

            //foreach (var process in processes)
            //{
            //    // All those background un-release process will be closed
            //    if (process.MainWindowTitle == "")
            //        process.Kill();
            //}

            InstrUnInit();
        }
        private void InstrInit(string LocSetFilePath, Dictionary<string, string>[] DicTestPA, Dictionary<string, string> DicWaveForm, Dictionary<string, string> DicWaveFormMutate)
        {


            try
            {
                #region Tuneable Filter
                string Filtermodel = SearchLocalSettingDictionary("Model", "FILTER");
                string Filteraddr = SearchLocalSettingDictionary("Address", "FILTER");

                switch (Filtermodel.ToUpper())
                {
                    case "KNL":
                        Eq.Site[0]._EqTuneFilter = new LibEqmtDriver.TuneableFilter.cKnL_D5BT(Filteraddr);
                        EqmtStatus.TuneFilter = true;
                        MessageForInstrument("Filter - KNL", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.TuneFilter = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment FILTER Model : " + Filtermodel.ToUpper(), "Pls ignore if Equipment Switch not require.");
                        EqmtStatus.TuneFilter = false;
                        break;

                }
                #endregion

                #region Switch Init
                string SWmodel = SearchLocalSettingDictionary("Model", "Switch");
                string SWaddr = SearchLocalSettingDictionary("Address", "Switch");

                switch (SWmodel.ToUpper())
                {
                    case "3499A":
                        Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.Agilent3499(SWaddr);
                        Eq.Site[0]._EqSwitch.Initialize();
                        Eq.Site[0]._EqSwitch.SetPath(myUtility.ReadTextFile(DicCalInfo[DataFilePath.LocSettingPath], TCF_Header.ConstSwBand, "INIT"));
                        EqmtStatus.Switch = true;
                        MessageForInstrument("Switch - 3499A", true);
                        break;
                    case "AEM_WOLFER":
                        Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.AeWofer(8);
                        Eq.Site[0]._EqSwitch.Initialize();
                        EqmtStatus.Switch = true;
                        MessageForInstrument("Switch - AEM_WOLFER", true);
                        break;
                    case "SW_NI6509":
                        if (SWaddr.IndexOf(",") == -1)
                        {
                            Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(SWaddr);
                            EqmtStatus.Switch = true;
                            bMultiSW = false;
                        }
                        else
                        {
                            string[] SW = SWaddr.Split(',');

                            Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(SW[0], 0);
                            Eq.Site[0]._EqSwitchSplit = new LibEqmtDriver.SCU.NI6509(SW[1], 1);
                            EqmtStatus.Switch = true;
                            bMultiSW = true;
                        }

                        try
                        {
                            switchLifeCycle = Convert.ToInt32(DicCalInfo[DataFilePath.SwitchCycleLimit]);
                        }
                        catch { }

                        StringBuilder errMessage = new StringBuilder("Switch Life Cycle Exceeded! Replaced Switch Immediately." + Environment.NewLine +
                            "Refer to Clotho Logger for detail information." + Environment.NewLine);
                        bool exceedCnt = false;
                        bool exceedThres = false;
                        int triggerCnt = switchLifeCycle + triggerThreshold;

                        if (Eq.Site[0]._EqSwitch.SPDT1CountValue() > switchLifeCycle)
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT1_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if (Eq.Site[0]._EqSwitch.SPDT1CountValue() > triggerCnt) { exceedThres = true; }
                        }
                        if (Eq.Site[0]._EqSwitch.SPDT2CountValue() > switchLifeCycle)
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT2_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if (Eq.Site[0]._EqSwitch.SPDT2CountValue() > triggerCnt) { exceedThres = true; }
                        }
                        if (Eq.Site[0]._EqSwitch.SPDT3CountValue() > switchLifeCycle)
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT3_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if (Eq.Site[0]._EqSwitch.SPDT3CountValue() > triggerCnt) { exceedThres = true; }
                        }
                        if (Eq.Site[0]._EqSwitch.SPDT4CountValue() > switchLifeCycle)
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT4_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if (Eq.Site[0]._EqSwitch.SPDT4CountValue() > triggerCnt) { exceedThres = true; }
                        }
                        if ((Eq.Site[0]._EqSwitch.SP6T1_1CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_2CountValue() > switchLifeCycle) ||
                            (Eq.Site[0]._EqSwitch.SP6T1_3CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_4CountValue() > switchLifeCycle) ||
                            (Eq.Site[0]._EqSwitch.SP6T1_5CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_6CountValue() > switchLifeCycle))
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SP6T1_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if ((Eq.Site[0]._EqSwitch.SP6T1_1CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_2CountValue() > triggerCnt) ||
                            (Eq.Site[0]._EqSwitch.SP6T1_3CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_4CountValue() > triggerCnt) ||
                            (Eq.Site[0]._EqSwitch.SP6T1_5CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_6CountValue() > triggerCnt)) { exceedThres = true; }
                        }
                        if ((Eq.Site[0]._EqSwitch.SP6T2_1CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_2CountValue() > switchLifeCycle) ||
                            (Eq.Site[0]._EqSwitch.SP6T2_3CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_4CountValue() > switchLifeCycle) ||
                            (Eq.Site[0]._EqSwitch.SP6T2_5CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_6CountValue() > switchLifeCycle))
                        {
                            exceedCnt = true;
                            logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SP6T2_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                            if ((Eq.Site[0]._EqSwitch.SP6T2_1CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_2CountValue() > triggerCnt) ||
                            (Eq.Site[0]._EqSwitch.SP6T2_3CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_4CountValue() > triggerCnt) ||
                            (Eq.Site[0]._EqSwitch.SP6T2_5CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_6CountValue() > triggerCnt)) { exceedThres = true; }
                        }

                        if (exceedCnt)
                        {
                            FrmSwitchCount frm;

                            if (exceedThres)
                            {
                                errMessage.Append(String.Format(Environment.NewLine + "Switch Trigger Threshold - {0} - Exceeded! Test will be aborted!", triggerCnt));
                                frm = new FrmSwitchCount(errMessage.ToString());
                                frm.ShowDialog();
                                throw new Exception("Exceeded Switch Trigger Threshold!");
                            }

                            frm = new FrmSwitchCount(errMessage.ToString());
                            frm.ShowDialog();
                        }

                        //Ivan
                        InstrumentInfo_SwitchBox += Eq.Site[0]._EqSwitch.GetInstrumentInfo();
                        MessageForInstrument("Switch - NI6509", true);
                        break;
                    case "SW_NI6509,ZTM":
                        string[] SW_withZTM = SWaddr.Split(',');

                        Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(SW_withZTM[0], 0);
                        Eq.Site[0]._EqSwitchSplit = new LibEqmtDriver.SCU.ZTM(SW_withZTM[1]);

                        EqmtStatus.Switch = true;
                        bMultiSW = true;

                        MessageForInstrument("Switch - NI6509 & ZTM", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.Switch = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment SWITCH Model : " + SWmodel.ToUpper(), "Pls ignore if Equipment Switch not require.");
                        EqmtStatus.Switch = false;
                        break;
                }
                #endregion

                #region SMU Init

                string SMUmodel = SearchLocalSettingDictionary("Model", "SMU");
                string SMUaddr = SearchLocalSettingDictionary("Address", "SMU");

                // Ivan - Create alias name for card info
                Alias_Vcc1 = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH0");
                Alias_Vcc2 = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH1");
                Alias_Vbatt = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH2");
                Alias_Vlna = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH3");

                // Ben - add for SMU_Leakage_Test, 15-10-2020
                s_strSMU = SMUmodel;

                int SMUtotalCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "TOTAL_SMUCHANNEL"));
                int VCCSMUCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "VCC_SMUCHANNEL"));

                // SMU Setting : CH0 - Vcc, CH1 - Vbatt, CH2 - Vdd or CH0 - Vcc1, CH1 - Vcc2, CH2 - Vbatt, CH3 - Vdd
                Eq.Site[0]._SMUSetting = new string[SMUtotalCH];
                Eq.Site[0]._VCCSetting = new string[VCCSMUCH];

                for (int smuCH = 0; smuCH < SMUtotalCH; smuCH++)
                {
                    Eq.Site[0]._SMUSetting[smuCH] = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH" + smuCH);
                }

                for (int vccCH = 0; vccCH < VCCSMUCH; vccCH++)
                {
                    Eq.Site[0]._VCCSetting[vccCH] = SearchLocalSettingDictionary("SmuSetting", "SMUV_VCC" + vccCH);

                    if (Eq.Site[0]._VCCSetting[vccCH] != Eq.Site[0]._SMUSetting[vccCH])
                    {
                        Helper.AutoClosingMessageBox.Show(string.Format("Please check VCC_SMUChannel!\n SMU Ch : {0} VCC_SMU Ch : {1}", Eq.Site[0]._SMUSetting[vccCH], Eq.Site[0]._VCCSetting[vccCH]), "Error");
                        MPAD_TestTimer.LoggingManager.Instance.LogError("Please check VCC_SMUChannel!\n SMU Ch : " + Eq.Site[0]._SMUSetting[vccCH] + " VCC_SMU Ch : " + Eq.Site[0]._VCCSetting[vccCH]);
                    }
                }

                Eq.Site[0]._EqSMU = new LibEqmtDriver.SMU.iPowerSupply[1];
                Eq.Site[0]._Eq_SMUDriver = new LibEqmtDriver.SMU.Drive_SMU();

                switch (SMUmodel.ToUpper())
                {
                    case "AM1340":
                        Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.Aemulus1340(0);
                        Eq.Site[0]._Eq_SMUDriver.Initialize(Eq.Site[0]._EqSMU);
                        EqmtStatus.SMU = true;
                        EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                        MessageForInstrument("SMU - AM1340", true);
                        break;
                    case "AEPXI":
                        Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.AePXISMU(Eq.Site[0]._SMUSetting);
                        EqmtStatus.SMU = true;
                        EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                        MessageForInstrument("SMU - AEPXI", true);
                        break;
                    case "NIPXI":
                        Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.NiPXISMU(Eq.Site[0]._SMUSetting);
                        EqmtStatus.SMU = true;
                        EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                        MessageForInstrument("SMU - NIPXI", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.SMU = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment SMU Model : " + SMUmodel.ToUpper(), "Pls ignore if Equipment SMU not require.");
                        EqmtStatus.SMU = false;
                        break;
                }
                #endregion

                #region Multiple 1CH DC Supply
                //This initilaization will also work with a single 4 Channel Power Supply like N6700B
                //For example N6700B, all address will be same. Software will create 4 instance for each channel

                Eq.Site[0]._Eq_DCSupply = new LibEqmtDriver.DC_1CH.iDCSupply_1CH[Eq.Site[0]._totalDCSupply];
                EqmtStatus.DCSupply = new bool[Eq.Site[0]._totalDCSupply];

                for (int i = 0; i < Eq.Site[0]._totalDCSupply; i++)
                {
                    string DCSupplymodel = SearchLocalSettingDictionary("Model", "DCSUPPLY0" + (i + 1));
                    string DCSupplyaddr = SearchLocalSettingDictionary("Address", "DCSUPPLY0" + (i + 1));

                    switch (DCSupplymodel.ToUpper())
                    {
                        case "E3633A":
                        case "E3644A":
                            Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.E3633A(DCSupplyaddr);
                            Eq.Site[0]._Eq_DCSupply[i].Init();
                            EqmtStatus.DCSupply[i] = true;
                            MessageForInstrument("DC-1CH - E3633A OR E3644A", true);
                            break;
                        case "N6700B":
                            Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.N6700B(DCSupplyaddr);
                            Eq.Site[0]._Eq_DCSupply[i].Init();
                            EqmtStatus.DCSupply[i] = true;
                            MessageForInstrument("DC-1CH - N6700B", true);
                            break;
                        case "NI4154":
                            Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.NI4154(DCSupplyaddr);
                            Eq.Site[0]._Eq_DCSupply[i].Init();
                            EqmtStatus.DCSupply[i] = true;
                            MessageForInstrument("DC-1CH - NI4154", true);
                            break;
                        case "NONE":
                        case "NA":
                            EqmtStatus.DCSupply[i] = false;
                            // Do Nothing , equipment not present
                            break;
                        default:
                            MessageBox.Show("Equipment DC Supply Model(DCSUPPLY0" + (i + 1) + ") : " + DCSupplymodel.ToUpper(), "Pls ignore if Equipment DC not require.");
                            EqmtStatus.DCSupply[i] = false;
                            break;
                    }
                }
                #endregion

                #region DC 1-Channel Init
                string DCmodel_1CH = SearchLocalSettingDictionary("Model", "PWRSUPPLY_1CH");
                string DCaddr_1CH = SearchLocalSettingDictionary("Address", "PWRSUPPLY_1CH");

                switch (DCmodel_1CH.ToUpper())
                {
                    case "E3633A":
                    case "E3644A":
                        Eq.Site[0]._Eq_DC_1CH = new LibEqmtDriver.DC_1CH.E3633A(DCaddr_1CH);
                        Eq.Site[0]._Eq_DC_1CH.Init();
                        EqmtStatus.DC_1CH = true;
                        MessageForInstrument("DC-1CH - E3633A OR E3644A", true);
                        break;
                    case "N6700B":
                        Eq.Site[0]._Eq_DC_1CH = new LibEqmtDriver.DC_1CH.N6700B(DCaddr_1CH);
                        Eq.Site[0]._Eq_DC_1CH.Init();
                        EqmtStatus.DC_1CH = true;
                        MessageForInstrument("DC-1CH - N6700B", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.DC_1CH = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment DC Supply 1-Channel Model : " + DCmodel_1CH.ToUpper(), "Pls ignore if Equipment DC not require.");
                        EqmtStatus.DC_1CH = false;
                        break;
                }
                #endregion

                #region DC Init

                string DCmodel = SearchLocalSettingDictionary("Model", "PWRSUPPLY");
                string DCaddr = SearchLocalSettingDictionary("Address", "PWRSUPPLY");

                switch (DCmodel.ToUpper())
                {
                    case "N6700B":
                        Eq.Site[0]._EqDC = new LibEqmtDriver.DC.N6700B(DCaddr);
                        Eq.Site[0]._EqDC.Init();
                        EqmtStatus.DC = true;
                        MessageForInstrument("DC - N6700B", true);
                        break;
                    case "PS662xA":
                        Eq.Site[0]._EqDC = new LibEqmtDriver.DC.PS662xA(DCaddr);
                        Eq.Site[0]._EqDC.Init();
                        EqmtStatus.DC = true;
                        MessageForInstrument("DC - PS662xA", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.DC = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment DC Supply Model : " + DCmodel.ToUpper(), "Pls ignore if Equipment DC not require.");
                        EqmtStatus.DC = false;
                        break;
                }
                #endregion

                #region SA Init
                string SA01model = SearchLocalSettingDictionary("Model", "MXA01");
                string SA01addr = SearchLocalSettingDictionary("Address", "MXA01");
                string SA02model = SearchLocalSettingDictionary("Model", "MXA02");
                string SA02addr = SearchLocalSettingDictionary("Address", "MXA02");
                bool cal_MXA = false;
                bool status = false;

                switch (SA01model.ToUpper())
                {
                    case "N9020A":
                        //MXA Alignment Calibration
                        string cnt_str = Interaction.InputBox("Do you want to perform MXA#01 alignment cal?\n" + "If so, please enter \"Yes\".", "MXA#01 ALIGNMENT", "No", 200, 200);
                        switch (cnt_str.ToUpper())
                        {
                            case "NO":
                                break;
                            case "YES":
                                cal_MXA = true;
                                break;
                            case "CANCEL":
                                break;
                            default:
                                break;
                        }

                        Eq.Site[0]._EqSA01 = new LibEqmtDriver.SA.N9020A(SA01addr);
                        Eq.Site[0]._EqSA01.Preset();
                        if (cal_MXA)
                        {
                            Eq.Site[0]._EqSA01.CAL();
                        }
                        EqmtStatus.MXA01 = true;
                        MessageForInstrument("SA01 - N9020A", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXA01 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXA Model : " + SA01model.ToUpper(), "Pls ignore if Equipment MXA not required.");
                        EqmtStatus.MXA01 = false;
                        break;
                }

                switch (SA02model.ToUpper())
                {
                    case "N9020A":
                        //MXA Alignment Calibration
                        string cnt_str = Interaction.InputBox("Do you want to perform MXA#02 alignment cal?\n" + "If so, please enter \"Yes\".", "MXA#02 ALIGNMENT", "No", 200, 200);
                        switch (cnt_str.ToUpper())
                        {
                            case "NO":
                                break;
                            case "YES":
                                cal_MXA = true;
                                break;
                            case "CANCEL":
                                break;
                            default:
                                break;
                        }

                        Eq.Site[0]._EqSA02 = new LibEqmtDriver.SA.N9020A(SA02addr);
                        Eq.Site[0]._EqSA02.Preset();
                        if (cal_MXA)
                        {
                            Eq.Site[0]._EqSA02.CAL();
                        }
                        EqmtStatus.MXA02 = true;
                        MessageForInstrument("SA02 - N9020A", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXA02 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXA02 Model : " + SA02model.ToUpper(), "Pls ignore if Equipment MXA02 not required.");
                        EqmtStatus.MXA02 = false;
                        break;
                }

                if (cal_MXA)
                {
                    DelayMs(60000);        //delay to wait for alignment to complete
                }
                else
                {
                    DelayMs(1000);
                }

                switch (SA01model.ToUpper())
                {
                    case "N9020A":
                        //MXA Display Enable/Disable
                        string cnt_str = Interaction.InputBox("Do you want to enable MXA#01 Display?\n" + "If so, please enter \"Yes\".", "Penang NPI", "No", 200, 200);
                        switch (cnt_str.ToUpper())
                        {
                            case "YES":
                                MXA_DisplayEnable = false;
                                break;
                            case "NO":
                                MXA_DisplayEnable = true;
                                break;
                            default:
                                MXA_DisplayEnable = true;
                                break;
                        }

                        status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                        Eq.Site[0]._EqSA01.AUTOALIGN_ENABLE(false);
                        Eq.Site[0]._EqSA01.Initialize(3);
                        if (MXA_DisplayEnable)
                        {
                            Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                        }
                        else
                        {
                            Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                        }
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXA01 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXA Model : " + SA01model.ToUpper(), "Pls ignore if Equipment MXA not required.");
                        EqmtStatus.MXA01 = false;
                        break;
                }

                switch (SA02model.ToUpper())
                {
                    case "N9020A":
                        //MXA Display Enable/Disable
                        string cnt_str = Interaction.InputBox("Do you want to enable MXA#02 Display?\n" + "If so, please enter \"Yes\".", "Penang NPI", "No", 200, 200);
                        switch (cnt_str.ToUpper())
                        {
                            case "YES":
                                MXA_DisplayEnable = false;
                                break;
                            case "NO":
                                MXA_DisplayEnable = true;
                                break;
                            default:
                                MXA_DisplayEnable = true;
                                break;
                        }

                        status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                        Eq.Site[0]._EqSA02.AUTOALIGN_ENABLE(false);
                        Eq.Site[0]._EqSA02.Initialize(3);
                        if (MXA_DisplayEnable)
                        {
                            Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                        }
                        else
                        {
                            Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                        }
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXA02 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXA02 Model : " + SA02model.ToUpper(), "Pls ignore if Equipment MXA02 not required.");
                        EqmtStatus.MXA02 = false;
                        break;
                }

                #endregion

                #region SG Init
                string SG01model = SearchLocalSettingDictionary("Model", "MXG01");
                string SG01addr = SearchLocalSettingDictionary("Address", "MXG01");
                string SG02model = SearchLocalSettingDictionary("Model", "MXG02");
                string SG02addr = SearchLocalSettingDictionary("Address", "MXG02");

                switch (SG01model.ToUpper())
                {
                    case "N5182A":
                        Eq.Site[0]._EqSG01 = new LibEqmtDriver.SG.N5182A(SG01addr);
                        Eq.Site[0]._EqSG01.Reset();
                        foreach (string key in DicWaveForm.Keys)
                        {
                            Eq.Site[0]._EqSG01.MOD_FORMAT_WITH_LOADING_CHECK(key.ToString(), DicWaveForm[key].ToString(), true);
                            DelayMs(500);
                            status = Eq.Site[0]._EqSG01.OPERATION_COMPLETE();
                            Eq.Site[0]._EqSG01.QueryError_SG(out InitInstrStatus);
                            if (!InitInstrStatus)
                            {
                                MessageBox.Show("Test Program Will Abort .. Please Fixed The Issue", "Equipment MXG01 Model : " + SG01model.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                        }
                        Eq.Site[0]._EqSG01.Initialize();
                        Eq.Site[0]._EqSG01.SetAmplitude(-110);
                        Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        Eq.Site[0]._EqSG01.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        EqmtStatus.MXG01 = true;
                        MessageForInstrument("SG01 - N5182A", true);
                        break;
                    case "E8257D":
                        Eq.Site[0]._EqSG01 = new LibEqmtDriver.SG.E8257D(SG01addr);
                        Eq.Site[0]._EqSG01.Initialize();
                        Eq.Site[0]._EqSG01.SetAmplitude(-40);
                        Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        Eq.Site[0]._EqSG01.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        EqmtStatus.MXG01 = true;
                        MessageForInstrument("SG01 - E8257D", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXG01 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXG01 Model : " + SG01model.ToUpper(), "Pls ignore if Equipment MXA01 not required.");
                        EqmtStatus.MXG01 = false;
                        break;
                }

                switch (SG02model.ToUpper())
                {
                    case "N5182A":
                        Eq.Site[0]._EqSG02 = new LibEqmtDriver.SG.N5182A(SG02addr);
                        Eq.Site[0]._EqSG01.Reset();
                        foreach (string key in DicWaveForm.Keys)
                        {
                            Eq.Site[0]._EqSG02.MOD_FORMAT_WITH_LOADING_CHECK(key.ToString(), DicWaveForm[key].ToString(), true);
                            DelayMs(500);
                            status = Eq.Site[0]._EqSG02.OPERATION_COMPLETE();
                            Eq.Site[0]._EqSG02.QueryError_SG(out InitInstrStatus);
                            if (!InitInstrStatus)
                            {
                                MessageBox.Show("Test Program Will Abort .. Please Fixed The Issue", "Equipment MXG02 Model : " + SG02model.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                        }
                        Eq.Site[0]._EqSG02.Initialize();
                        Eq.Site[0]._EqSG02.SetAmplitude(-110);
                        Eq.Site[0]._EqSG02.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        Eq.Site[0]._EqSG02.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        EqmtStatus.MXG02 = true;
                        MessageForInstrument("SG02 - N5182A", true);
                        break;
                    case "E8257D":
                        Eq.Site[0]._EqSG02 = new LibEqmtDriver.SG.E8257D(SG01addr);
                        Eq.Site[0]._EqSG02.Initialize();
                        Eq.Site[0]._EqSG02.SetAmplitude(-40);
                        Eq.Site[0]._EqSG02.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        Eq.Site[0]._EqSG02.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                        EqmtStatus.MXG02 = true;
                        MessageForInstrument("SG02 - E8257D", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MXG02 = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MXG02 Model : " + SG02model.ToUpper(), "Pls ignore if Equipment MXG02 not required.");
                        EqmtStatus.MXG02 = false;
                        break;
                }
                #endregion

                #region VST Init
                string VSTmodel = SearchLocalSettingDictionary("Model", "PXI_VST");
                string VSTaddr = SearchLocalSettingDictionary("Address", "PXI_VST");

                //Ivan - Create alias name for MIPI Card Info
                Alias_VST = SearchLocalSettingDictionary("Model", "PXI_VST");
                Alias_VST = "VST";

                switch (VSTmodel.ToUpper())
                {
                    case "NI5644R":
                    case "PXIE-5644R":
                        Eq.Site[0]._EqVST = new LibEqmtDriver.NF_VST.NF_NiPXI_VST(VSTaddr);
                        Eq.Site[0]._EqRFmx = new LibEqmtDriver.NF_VST.NF_NI_RFmx(VSTaddr);
                        Eq.Site[0]._EqVST.Initialize_NI5644R();
                        Eq.Site[0]._EqVST.IQRate = 120e6;

                        foreach (string key in DicWaveForm.Keys)
                        {
                            Eq.Site[0]._EqVST.MOD_FORMAT_CHECK(key.ToString(), DicWaveForm[key].ToString(), DicWaveFormMutate[key].ToString(), true);
                        }
                        Eq.Site[0]._EqVST.PreConfigureVST();

                        foreach (Dictionary<string, string> currTestCond in DicTestPA)
                        {
                            string tmpTestNo = myUtility.ReadTcfData(currTestCond, TCF_Header.ConstTestNum);

                            Cs = AllNFtest[tmpTestNo];

                            if (Cs._TestParam == "PXI_RXPATH_GAIN_NF")
                            {
                                Eq.Site[0]._EqVST.writeWaveForm(currTestCond["TEST NUMBER"], Cs._ArbitraryWaveform);
                            }

                        }
                        EqmtStatus.PXI_VST = true;
                        MessageForInstrument("VST - 5644R", true);
                        break;
                    case "NI5646R":
                    case "PXIE-5646R":
                        Eq.Site[0]._EqVST = new LibEqmtDriver.NF_VST.NF_NiPXI_VST(VSTaddr);
                        Eq.Site[0]._EqRFmx = new LibEqmtDriver.NF_VST.NF_NI_RFmx(VSTaddr);
                        Eq.Site[0]._EqVST.Initialize();
                        Eq.Site[0]._EqVST.IQRate = 250E6;

                        foreach (string key in DicWaveForm.Keys)
                        {
                            Eq.Site[0]._EqVST.MOD_FORMAT_CHECK(key.ToString(), DicWaveForm[key].ToString(), DicWaveFormMutate[key].ToString(), true);
                        }

                        Eq.Site[0]._EqVST.PreConfigureVST();

                        foreach (Dictionary<string, string> currTestCond in DicTestPA)
                        {
                            string tmpTestNo = myUtility.ReadTcfData(currTestCond, TCF_Header.ConstTestNum);

                            Cs = AllNFtest[tmpTestNo];

                            if (Cs._TestParam == "PXI_RXPATH_GAIN_NF")
                            {
                                Eq.Site[0]._EqVST.writeWaveForm(currTestCond["TEST NUMBER"], Cs._ArbitraryWaveform);
                            }

                        }

                   
                        EqmtStatus.PXI_VST = true;
                        MessageForInstrument("VST - 5646R", true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.PXI_VST = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment PXI VST Model : " + VSTmodel.ToUpper(), "Pls ignore if Equipment PXI_VST not required");
                        EqmtStatus.TuneFilter = false;
                        break;
                }

                #region RFMX NF Initial Setting -Seoul & VST Calibration
                if (EqmtStatus.PXI_VST)
                {
                    Eq.Site[0]._EqRFmx.InitList(DicTestPA.Count());

                    for (int i = 0; i < DicTestPA.Count(); i++)
                    {
                        string parameterName;
                        DicTestPA[i].TryGetValue("TEST PARAMETER", out parameterName);

                        if (parameterName.ToUpper() == "NF_CAL" || parameterName.ToUpper() == "PXI_NF_COLD" || parameterName.ToUpper() == "PXI_NF_HOT" || parameterName.ToUpper() == "PXI_NF_MEAS" || parameterName.ToUpper() == "PXI_NF_COLD_MIPI"
                            || parameterName.ToUpper() == "PXI_NF_COLD_ALLINONE" || parameterName.ToUpper() == "PXI_NF_COLD_MIPI_ALLINONE") // Ben, add "PXI_NF_COLD_MIPI"
                        {
                            double nF_BW = Convert.ToDouble(DicTestPA[i]["NF_BW"]);
                            double nF_REFLEVEL = Convert.ToDouble(DicTestPA[i]["NF_REFLEVEL"]);
                            double nF_SWEEPTIME = Convert.ToDouble(DicTestPA[i]["NF_SWEEPTIME"]);
                            int nF_AVERAGE = Convert.ToInt32(DicTestPA[i]["NF_AVERAGE"]);
                            string calSetID = DicTestPA[i]["NF_CALTAG"];

                            double[] dutInputLoss, dutOutputLoss, DUTAntForTxMeasureLoss, freqList;

                            NFvariables(DicTestPA[i], parameterName, out DUTAntForTxMeasureLoss, out dutInputLoss, out dutOutputLoss, out freqList);
                            Eq.Site[0]._EqRFmx.cRFmxNF.ListConfigureSpecNFColdSource(i, nF_BW, nF_SWEEPTIME, nF_AVERAGE, nF_REFLEVEL, parameterName.ToUpper(), dutInputLoss, dutOutputLoss, freqList, calSetID);

                            if (parameterName.ToUpper() == "PXI_NF_HOT" && SearchLocalSettingDictionary("Model", "PWRMETER").ToUpper() == "NONE")
                            {
                                double _StartTxFreq = Convert.ToDouble(DicTestPA[i]["START_TXFREQ1"]);
                                double _StopTxFreq = Convert.ToDouble(DicTestPA[i]["STOP_TXFREQ1"]);
                                double _TargetTxPout = Convert.ToDouble(DicTestPA[i]["POUT1"]);
                                string _modulation = Convert.ToString(DicTestPA[i]["MODULATION"]);
                                string _Waveform = Convert.ToString(DicTestPA[i]["WAVEFORMNAME"]);
                                double channelBW = 0;

                                Eq.Site[0]._EqVST.Get_SignalBandwidth_fromModulation(_modulation, _Waveform, out channelBW);
                                Eq.Site[0]._EqRFmx.cRFmxChp.ConfigureSpec(i, ((_StartTxFreq + _StopTxFreq) / 2), _TargetTxPout + DUTAntForTxMeasureLoss.Average(), channelBW, 600, 0.001);
                            }
                        }
                    }

                    if (!ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE))
                    {
                        //VST Calibration
                        #region VST Calibration

                        bool bSelfcal_Flag = false;
                        double dSelfCalLast_SA_temperature = 0.0f;
                        double dSelfCalLast_SG_temperature = 0.0f;
                        double dSA_temperature = 0.0f;
                        double dSG_temperature = 0.0f;

                        dSA_temperature = Eq.Site[0]._EqVST.rfsaSession.DeviceCharacteristics.GetDeviceTemperature();
                        dSG_temperature = Eq.Site[0]._EqVST.rfsgSession.DeviceCharacteristics.DeviceTemperature;

                        CheckVSTTemperature(ref dSelfCalLast_SA_temperature, ref dSelfCalLast_SG_temperature, ref dSA_temperature, ref dSG_temperature, ref bSelfcal_Flag);

                        #endregion VST Calibration

                        #region Close VSG/VSA session if this is RF_Calibration case -Seoul
                    }

                    for (int i = 0; i < DicTestPA.Count(); i++)
                    {
                        string testMode, testParameter;
                        DicTestPA[i].TryGetValue("TEST MODE", out testMode);
                        DicTestPA[i].TryGetValue("TEST PARAMETER", out testParameter);

                        if (testMode.ToUpper() == "CALIBRATION")
                        {
                            if (testParameter.ToUpper() == "RF_CAL")
                                Eq.Site[0]._EqVST.Close_VST();

                            //VST Calibration : Set True
                            bTestCalibration = true;
                        }
                    }
                    #endregion Close VSG/VSA session if this is RF_Calibration case -Seoul
                }
                #endregion RFMX NF Initial Setting -Seoul & VST Calibration

                #endregion

                #region Power Sensor Init
                string PMmodel = SearchLocalSettingDictionary("Model", "PWRMETER");
                string PMaddr = SearchLocalSettingDictionary("Address", "PWRMETER");

                switch (PMmodel.ToUpper())
                {
                    case "E4416A":
                    case "E4417A":
                        Eq.Site[0]._EqPwrMeter = new LibEqmtDriver.PS.E4417A(PMaddr);
                        Eq.Site[0]._EqPwrMeter.Initialize(1);
                        EqmtStatus.PM = true;
                        MessageForInstrument("Power Sensor - E4416A or E4417A", true);
                        break;
                    case "NRPZ11":
                    case "NRPZ21":
                    case "NRP8S":
                        Eq.Site[0]._EqPwrMeter = new LibEqmtDriver.PS.RSNRPZ11("");
                        Eq.Site[0]._EqPwrMeter.Initialize(1);
                        Eq.Site[0]._EqPwrMeter.SetFreq(1, 1500, PowerSensorMeasuringType);
                        DelayMs(200);
                        dummyData = Eq.Site[0]._EqPwrMeter.MeasPwr(1);
                        EqmtStatus.PM = true;
                        MessageForInstrument(string.Format("Power Sensor - {0}", PMmodel.ToUpper()), true);
                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.PM = false;
                        Eq.Site[0]._isUseRFmxForTxMeasure = true;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment POWERSENSOR Model : " + PMmodel.ToUpper(), "Pls ignore if Equipment Power Sensor not require.");
                        Eq.Site[0]._isUseRFmxForTxMeasure = true;
                        EqmtStatus.PM = false;
                        break;
                }
                #endregion

                #region MIPI Init

                string MIPImodel = SearchLocalSettingDictionary("Model", "MIPI_Card");
                string MIPIaddr = SearchLocalSettingDictionary("Address", "MIPI_Card");
                string AemulusPxi_FileName = SearchLocalSettingDictionary("Address", "APXI_FileName");

                #region MIPI Configure - DUT MIPI
                try
                {
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Clock_Speed"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiNFRClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_NFR_Clock_Speed"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiSyncWriteRead = Convert.ToBoolean(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Clock_Sync_Write_Read"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPBurnClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_OTP_Burn_Clock_Speed"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPReadClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_OTP_Read_Clock_Speed"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_VIO_Voltage"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIH"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIL"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOH"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOL"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VTT"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiType = SearchLocalSettingDictionary("MIPI_Config", "MIPI_Type").ToUpper();
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.StrobePoint = Convert.ToDouble(DicCalInfo[DataFilePath.HSDIO_StrobePoint]);
                }
                catch (Exception ex)
                {
                    Helper.AutoClosingMessageBox.Show(string.Format("Pleaseh Check - MIPI Config: {0}", ex), "MIPI Configure Error");
                    MPAD_TestTimer.LoggingManager.Instance.LogError("[Fail] Pleaseh Check - MIPI Config : " + ex);

                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiClockSpeed = 26e6;
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiNFRClockSpeed = 51.2e6;
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiSyncWriteRead = true;
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPBurnClockSpeed = 26e6;
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPReadClockSpeed = 26e6;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = 1.2;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = 1.2;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = 0.6;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = 0.8;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = 0.0;
                    //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = 3.0;
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_VIO_Voltage"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIH"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIL"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOH"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOL"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VTT"));
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiType = "RZ";
                    LibEqmtDriver.MIPI.Lib_Var.DUTMipi.StrobePoint = 0.9;
                }

                LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = true;

                EqmtStatus.MIPI = true;
                #endregion

                #region MIPI Pin Config
                //use for MIPI pin initialization
                string mipiPairCount = "";
                LibEqmtDriver.MIPI.s_MIPI_PAIR[] tmp_mipiPair;
                mipiPairCount = SearchLocalSettingDictionary("MIPI_PIN_CFG", "Mipi_Pair_Count");
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
                        tmp_mipiPair[i].SCLK = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SCLK_" + i);
                        tmp_mipiPair[i].SDATA = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SDATA_" + i);
                        tmp_mipiPair[i].SVIO = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SVIO_" + i);
                    }
                }

                #endregion

                switch (MIPImodel.ToUpper())
                {

                    case "DM280E":
                        try
                        {
                            LibEqmtDriver.MIPI.Lib_Var.myDM280Address = MIPIaddr;
                            LibEqmtDriver.MIPI.Lib_Var.DM280_CH0 = 0;
                            LibEqmtDriver.MIPI.Lib_Var.DM280_CH1 = 1;
                            LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                            //PM Trigger 
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                            //Read Function
                            string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                            LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                            //Init
                            Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM280e();
                            Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                            EqmtStatus.MIPI = true;
                            MessageForInstrument("HSDIO - DM280E", true);
                        }
                        catch (Exception ex)
                        {
                            //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                            //EqmtStatus.MIPI = false;
                            MessageBox.Show("DM280E MIPI cards not detected, please check!", ex.ToString());
                            return;
                        }
                        break;
                    case "DM482E":
                        try
                        {
                            LibEqmtDriver.MIPI.Lib_Var.myDM482Address = MIPIaddr;
                            LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                            //PM Trigger 
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                            //Read Function
                            string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                            LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                            //Load MIPI Vector File to Memory
                            string vectorBasePath = GetTestPlanPath() + @"RFFE_vectors\";
                            LibEqmtDriver.MIPI.Lib_Var.VectorPATH = vectorBasePath;

                            //Init
                            string AemulusePxi_Path = "C:\\Aemulus\\common\\map_file\\";
                            AemulusePxi_Path += AemulusPxi_FileName;
                            LibEqmtDriver.MIPI.Lib_Var.HW_Profile = AemulusePxi_Path;
                            //Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM482e();
                            Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                            EqmtStatus.MIPI = true;
                            MessageForInstrument("HSDIO - DM482E", true);
                        }
                        catch (Exception ex)
                        {
                            //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                            //EqmtStatus.MIPI = false;
                            MessageBox.Show("DM482E MIPI cards not detected, please check!", ex.ToString());
                            return;
                        }

                        break;
                    case "DM482E_VEC":
                        try
                        {
                            LibEqmtDriver.MIPI.Lib_Var.myDM482Address = MIPIaddr;
                            LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                            //PM Trigger 
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                            //Read Function
                            string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                            LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                            //Init
                            string AemulusePxi_Path = "C:\\Aemulus\\common\\map_file\\";
                            AemulusePxi_Path += AemulusPxi_FileName;
                            LibEqmtDriver.MIPI.Lib_Var.HW_Profile = AemulusePxi_Path;
                            Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM482e_Vec();
                            Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                            EqmtStatus.MIPI = true;
                            MessageForInstrument("HSDIO - DM482E_VEC", true);
                        }
                        catch (Exception ex)
                        {
                            //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                            //EqmtStatus.MIPI = false;

                            MessageBox.Show("DM482E MIPI (Vector Config) cards not detected, please check!", ex.ToString());
                            return;
                        }

                        break;
                    case "NI6570":
                        try
                        {
                            LibEqmtDriver.MIPI.Lib_Var.myNI6570Address = MIPIaddr;
                            LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                            //PM Trigger 
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                            LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                            //Read Function
                            string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                            LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                            //Init
                            Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.NI_PXIe6570(tmp_mipiPair, Eq.Site[0]._PpmuResources);

                            EqmtStatus.MIPI = true;
                            MessageForInstrument("HSDIO - NI6570", true);
                        }
                        catch (Exception ex)
                        {
                            //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                            //EqmtStatus.MIPI = false;
                            MessageBox.Show("NI6570 MIPI cards not detected, please check!", ex.ToString());
                            return;
                        }

                        break;
                    case "NONE":
                    case "NA":
                        EqmtStatus.MIPI = false;
                        // Do Nothing , equipment not present
                        break;
                    default:
                        MessageBox.Show("Equipment MIPI Model : " + MIPImodel.ToUpper(), "Pls ignore if Equipment MIPI not require.");
                        EqmtStatus.MIPI = false;
                        break;
                }
                #endregion

                #region MIPI-PPMU Init

                int PPMUtotalCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "TOTAL_PPMUCHANNEL"));

                if (PPMUtotalCH > 0)
                {
                    string[] PPMUSetting = new string[PPMUtotalCH];
                    for (int ppmuVIO = 0; ppmuVIO < PPMUtotalCH; ppmuVIO++)
                    {
                        PPMUSetting[ppmuVIO] = SearchLocalSettingDictionary("SmuSetting", "PPMUV_CH" + ppmuVIO);
                    }
                    LibEqmtDriver.MIPI.Lib_Var.isVioPpmu = PPMUtotalCH > 0;

                    Eq.Site[0]._EqPPMU = new LibEqmtDriver.SMU.iSmu[PPMUtotalCH];

                    for (int i = 0; i < PPMUtotalCH; i++)
                    {
                        switch (MIPImodel.ToUpper())
                        {
                            case "NI6570":
                                Eq.Site[0]._EqPPMU[i] = new LibEqmtDriver.SMU.NI_PXIe6570_PPMU(PPMUSetting[i], Eq.Site[0]._EqMiPiCtrl);
                                EqmtStatus.MIPI_PPMU = true;
                                break;
                            case "DM482E":
                                Eq.Site[0]._EqPPMU[i] = new LibEqmtDriver.SMU.AemulusDM482ePPMU(PPMUSetting[i], Eq.Site[0]._EqMiPiCtrl);
                                EqmtStatus.MIPI_PPMU = true;
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.MIPI_PPMU = false;
                                break;
                            default:
                                MessageBox.Show("Equipment MIPI PPMU Model : " + MIPImodel.ToUpper(), "Pls ignore if Equipment MIPI PPMU not require.");
                                EqmtStatus.MIPI_PPMU = false;
                                break;
                        }
                        if (Eq.Site[0]._EqPPMU[i] != null)
                            Eq.Site[0]._PpmuResources.Add(Eq.Site[0]._EqPPMU[i].PinName, Eq.Site[0]._EqPPMU[i]);
                    }
                }
                else
                {
                    LibEqmtDriver.MIPI.Lib_Var.isVioPpmu = false;
                }
                #endregion

                #region Handler Init

                strHandlerType = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_Type, "");

                if (strHandlerType.CIvContainsAnyOf("HANDLERSIM", "HANDLERSIM002", "MULTISITEHANDLERSIM") || string.IsNullOrEmpty(strHandlerType))
                {
                    Handler_Info = "FALSE";
                }
                else
                {
                    //Handler_Info = "TRUE";
                    string HandlerSN = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_SN, "");

                    if (HandlerSN.CIvContainsAnyOf("HT", "IM"))
                    {
                        HandlerAddress = ATFRTE.Instance.HandlerAddress; //For Inari Handler address [Chee On].
                    }
                    else if (HandlerSN.Contains("NS"))
                    {
                        HandlerAddress = ATFRTE.Instance.HandlerAddress;
                    }
                    else
                    {
                        ///Get handlerAddress from ClothoRoot/System/Configuration/ATFConfig.xml > UserSection
                        HandlerAddress = ClothoDataObject.Instance.ATFConfiguration.UserSection.GetValue("HandlerAddress");
                    }
                }

                Handler_Info = myUtility.DicLocalfile["HANDLER_INFO"]["ENABLE"];
                if (Handler_Info == "TRUE")
                {
                    try
                    {
                        using (Ping QuickPingTester = new Ping())
                        {
                            if (!int.TryParse(HandlerAddress, out int _handlerAddress))
                                _handlerAddress = 1;

                            PingReply PingTest = QuickPingTester.Send(IPAddress.Parse("192.168.0.10" + _handlerAddress.ToString()), 3);

                            if (PingTest.Status == IPStatus.Success)
                            {
                                if (handler != null)
                                    handler.Disconnect();

                                if (HandlerAddress != null)
                                {
                                    handler = new HontechHandler(_handlerAddress);
                                    handler.Connect();
                                    HandlerForce hli = handler.ContactForceQuery();
                                    //Thread.Sleep(200);
                                    DelayMs(200);
                                    hli = handler.ContactForceQuery();

                                    if (hli.ArmNo == 0 && hli.PlungerForce == 0 && hli.SiteNo == 0)
                                    {
                                        Flag_HandlerInfor = false;
                                    }
                                    else Flag_HandlerInfor = true;
                                }
                            }
                            else
                            {
                                Flag_HandlerInfor = false;
                            }
                        }
                    }
                    catch
                    {
                        Flag_HandlerInfor = false;
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                MPAD_TestTimer.LoggingManager.Instance.LogError(String.Format("[Fail] Initialize the instruments\nMessage : {0}\nStack : {1}", ex.Message, ex.StackTrace));
            }
        }
        private void InstrInitThread(string EquipType, string EquipModel, string EquipAddr)
        {

            try
            {
                switch (EquipType.ToUpper())
                {
                    case "FILTER":
                        #region Tuneable Filter
                        switch (EquipModel.ToUpper())
                        {
                            case "KNL":
                                Eq.Site[0]._EqTuneFilter = new LibEqmtDriver.TuneableFilter.cKnL_D5BT(EquipAddr);
                                EqmtStatus.TuneFilter = true;
                                MessageForInstrument("Filter - KNL", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.TuneFilter = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment FILTER Model : " + EquipAddr.ToUpper(), "Pls ignore if Equipment Switch not require.");
                                EqmtStatus.TuneFilter = false;
                                break;
                        }
                        #endregion
                        break;
                    case "SWITCH":
                        #region Switch Init
                        switch (EquipModel.ToUpper())
                        {
                            case "3499A":
                                Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.Agilent3499(EquipAddr);
                                Eq.Site[0]._EqSwitch.Initialize();
                                Eq.Site[0]._EqSwitch.SetPath(myUtility.ReadTextFile(DicCalInfo[DataFilePath.LocSettingPath], TCF_Header.ConstSwBand, "INIT"));
                                EqmtStatus.Switch = true;
                                MessageForInstrument("Switch - 3499A", true);
                                break;
                            case "AEM_WOLFER":
                                Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.AeWofer(8);
                                Eq.Site[0]._EqSwitch.Initialize();
                                EqmtStatus.Switch = true;
                                MessageForInstrument("Switch - AEM_WOLFER", true);
                                break;
                            case "SW_NI6509":
                                if (EquipAddr.IndexOf(",") == -1)
                                {
                                    Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(EquipAddr);
                                    EqmtStatus.Switch = true;
                                    bMultiSW = false;
                                }
                                else
                                {
                                    string[] SW = EquipAddr.Split(',');

                                    Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(SW[0], 0);
                                    Eq.Site[0]._EqSwitchSplit = new LibEqmtDriver.SCU.NI6509(SW[1], 1);
                                    EqmtStatus.Switch = true;
                                    bMultiSW = true;
                                }

                                try
                                {
                                    switchLifeCycle = Convert.ToInt32(DicCalInfo[DataFilePath.SwitchCycleLimit]);
                                }
                                catch { }

                                StringBuilder errMessage = new StringBuilder("Switch Life Cycle Exceeded! Replaced Switch Immediately." + Environment.NewLine +
                                    "Refer to Clotho Logger for detail information." + Environment.NewLine);
                                bool exceedCnt = false;
                                bool exceedThres = false;
                                int triggerCnt = switchLifeCycle + triggerThreshold;

                                if (Eq.Site[0]._EqSwitch.SPDT1CountValue() > switchLifeCycle)
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT1_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if (Eq.Site[0]._EqSwitch.SPDT1CountValue() > triggerCnt) { exceedThres = true; }
                                }
                                if (Eq.Site[0]._EqSwitch.SPDT2CountValue() > switchLifeCycle)
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT2_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if (Eq.Site[0]._EqSwitch.SPDT2CountValue() > triggerCnt) { exceedThres = true; }
                                }
                                if (Eq.Site[0]._EqSwitch.SPDT3CountValue() > switchLifeCycle)
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT3_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if (Eq.Site[0]._EqSwitch.SPDT3CountValue() > triggerCnt) { exceedThres = true; }
                                }
                                if (Eq.Site[0]._EqSwitch.SPDT4CountValue() > switchLifeCycle)
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SPDT4_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if (Eq.Site[0]._EqSwitch.SPDT4CountValue() > triggerCnt) { exceedThres = true; }
                                }
                                if ((Eq.Site[0]._EqSwitch.SP6T1_1CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_2CountValue() > switchLifeCycle) ||
                                    (Eq.Site[0]._EqSwitch.SP6T1_3CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_4CountValue() > switchLifeCycle) ||
                                    (Eq.Site[0]._EqSwitch.SP6T1_5CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T1_6CountValue() > switchLifeCycle))
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SP6T1_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if ((Eq.Site[0]._EqSwitch.SP6T1_1CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_2CountValue() > triggerCnt) ||
                                    (Eq.Site[0]._EqSwitch.SP6T1_3CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_4CountValue() > triggerCnt) ||
                                    (Eq.Site[0]._EqSwitch.SP6T1_5CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T1_6CountValue() > triggerCnt)) { exceedThres = true; }
                                }
                                if ((Eq.Site[0]._EqSwitch.SP6T2_1CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_2CountValue() > switchLifeCycle) ||
                                    (Eq.Site[0]._EqSwitch.SP6T2_3CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_4CountValue() > switchLifeCycle) ||
                                    (Eq.Site[0]._EqSwitch.SP6T2_5CountValue() > switchLifeCycle) || (Eq.Site[0]._EqSwitch.SP6T2_6CountValue() > switchLifeCycle))
                                {
                                    exceedCnt = true;
                                    logger.Log(LogLevel.Error, LogSource.eHandler, string.Format("SP6T2_Switch cycle > {0}, replace Mechanical switch immediately", switchLifeCycle));
                                    if ((Eq.Site[0]._EqSwitch.SP6T2_1CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_2CountValue() > triggerCnt) ||
                                    (Eq.Site[0]._EqSwitch.SP6T2_3CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_4CountValue() > triggerCnt) ||
                                    (Eq.Site[0]._EqSwitch.SP6T2_5CountValue() > triggerCnt) || (Eq.Site[0]._EqSwitch.SP6T2_6CountValue() > triggerCnt)) { exceedThres = true; }
                                }

                                if (exceedCnt)
                                {
                                    FrmSwitchCount frm;

                                    if (exceedThres)
                                    {
                                        errMessage.Append(String.Format(Environment.NewLine + "Switch Trigger Threshold - {0} - Exceeded! Test will be aborted!", triggerCnt));
                                        frm = new FrmSwitchCount(errMessage.ToString());
                                        frm.ShowDialog();
                                        throw new Exception("Exceeded Switch Trigger Threshold!");
                                    }

                                    frm = new FrmSwitchCount(errMessage.ToString());
                                    frm.ShowDialog();
                                }

                                //Ivan
                                InstrumentInfo_SwitchBox += Eq.Site[0]._EqSwitch.GetInstrumentInfo();
                                MessageForInstrument("Switch - NI6509", true);
                                break;
                            case "SW_NI6509,ZTM":
                                string[] SW_withZTM = EquipAddr.Split(',');

                                Eq.Site[0]._EqSwitch = new LibEqmtDriver.SCU.NI6509(SW_withZTM[0], 0);
                                Eq.Site[0]._EqSwitchSplit = new LibEqmtDriver.SCU.ZTM(SW_withZTM[1]);

                                EqmtStatus.Switch = true;
                                bMultiSW = true;

                                MessageForInstrument("Switch - NI6509 & ZTM", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.Switch = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment SWITCH Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment Switch not require.");
                                EqmtStatus.Switch = false;
                                break;
                        }
                        #endregion
                        break;
                    case "SMU":
                        #region SMU Init

                        // Ivan - Create alias name for card info
                        Alias_Vcc1 = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH0");
                        Alias_Vcc2 = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH1");
                        Alias_Vbatt = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH2");
                        Alias_Vlna = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH3");

                        // Ben - add for SMU_Leakage_Test, 15-10-2020
                        s_strSMU = EquipModel;

                        int SMUtotalCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "TOTAL_SMUCHANNEL"));
                        int VCCSMUCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "VCC_SMUCHANNEL"));

                        // SMU Setting : CH0 - Vcc, CH1 - Vbatt, CH2 - Vdd or CH0 - Vcc1, CH1 - Vcc2, CH2 - Vbatt, CH3 - Vdd
                        Eq.Site[0]._SMUSetting = new string[SMUtotalCH];
                        Eq.Site[0]._VCCSetting = new string[VCCSMUCH];

                        for (int smuCH = 0; smuCH < SMUtotalCH; smuCH++)
                        {
                            Eq.Site[0]._SMUSetting[smuCH] = SearchLocalSettingDictionary("SmuSetting", "SMUV_CH" + smuCH);
                        }

                        for (int vccCH = 0; vccCH < VCCSMUCH; vccCH++)
                        {
                            Eq.Site[0]._VCCSetting[vccCH] = SearchLocalSettingDictionary("SmuSetting", "SMUV_VCC" + vccCH);

                            if (Eq.Site[0]._VCCSetting[vccCH] != Eq.Site[0]._SMUSetting[vccCH])
                            {
                                Helper.AutoClosingMessageBox.Show(string.Format("Please check VCC_SMUChannel!\n SMU Ch : {0} VCC_SMU Ch : {1}", Eq.Site[0]._SMUSetting[vccCH], Eq.Site[0]._VCCSetting[vccCH]), "Error");
                                MPAD_TestTimer.LoggingManager.Instance.LogError("Please check VCC_SMUChannel!\n SMU Ch : " + Eq.Site[0]._SMUSetting[vccCH] + " VCC_SMU Ch : " + Eq.Site[0]._VCCSetting[vccCH]);
                            }
                        }

                        Eq.Site[0]._EqSMU = new LibEqmtDriver.SMU.iPowerSupply[1];
                        Eq.Site[0]._Eq_SMUDriver = new LibEqmtDriver.SMU.Drive_SMU();

                        switch (EquipModel.ToUpper())
                        {
                            case "AM1340":
                                Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.Aemulus1340(0);
                                Eq.Site[0]._Eq_SMUDriver.Initialize(Eq.Site[0]._EqSMU);
                                EqmtStatus.SMU = true;
                                EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                                MessageForInstrument("SMU - AM1340", true);
                                break;
                            case "AEPXI":
                                Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.AePXISMU(Eq.Site[0]._SMUSetting);
                                EqmtStatus.SMU = true;
                                EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                                MessageForInstrument("SMU - AEPXI", true);
                                break;
                            case "NIPXI":
                                Eq.Site[0]._EqSMU[0] = new LibEqmtDriver.SMU.NiPXISMU(Eq.Site[0]._SMUSetting);
                                EqmtStatus.SMU = true;
                                EqmtStatus.SMU_CH = "0";        //Initialize variable default SMU_CH to SMU Channel0
                                MessageForInstrument("SMU - NIPXI", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.SMU = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment SMU Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment SMU not require.");
                                EqmtStatus.SMU = false;
                                break;
                        }
                        #endregion
                        break;
                    case "DCSUPPLY":
                        #region Multiple 1CH DC Supply
                        //This initilaization will also work with a single 4 Channel Power Supply like N6700B
                        //For example N6700B, all address will be same. Software will create 4 instance for each channel
                        if (Eq.Site[0]._Eq_DCSupply == null)
                        {
                            Eq.Site[0]._Eq_DCSupply = new LibEqmtDriver.DC_1CH.iDCSupply_1CH[Eq.Site[0]._totalDCSupply];
                            EqmtStatus.DCSupply = new bool[Eq.Site[0]._totalDCSupply];

                            for (int i = 0; i < Eq.Site[0]._totalDCSupply; i++)
                            {
                                string DCSupplymodel = SearchLocalSettingDictionary("Model", "DCSUPPLY0" + (i + 1));
                                string DCSupplyaddr = SearchLocalSettingDictionary("Address", "DCSUPPLY0" + (i + 1));

                                switch (DCSupplymodel.ToUpper())
                                {
                                    case "E3633A":
                                    case "E3644A":
                                        Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.E3633A(DCSupplyaddr);
                                        Eq.Site[0]._Eq_DCSupply[i].Init();
                                        EqmtStatus.DCSupply[i] = true;
                                        MessageForInstrument("DC-1CH - E3633A OR E3644A", true);
                                        break;
                                    case "N6700B":
                                        Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.N6700B(DCSupplyaddr);
                                        Eq.Site[0]._Eq_DCSupply[i].Init();
                                        EqmtStatus.DCSupply[i] = true;
                                        MessageForInstrument("DC-1CH - N6700B", true);
                                        break;
                                    case "NI4154":
                                        Eq.Site[0]._Eq_DCSupply[i] = new LibEqmtDriver.DC_1CH.NI4154(DCSupplyaddr);
                                        Eq.Site[0]._Eq_DCSupply[i].Init();
                                        EqmtStatus.DCSupply[i] = true;
                                        MessageForInstrument("DC-1CH - NI4154", true);
                                        break;
                                    case "NONE":
                                    case "NA":
                                        EqmtStatus.DCSupply[i] = false;
                                        // Do Nothing , equipment not present
                                        break;
                                    default:
                                        MessageBox.Show("Equipment DC Supply Model(DCSUPPLY0" + (i + 1) + ") : " + DCSupplymodel.ToUpper(), "Pls ignore if Equipment DC not require.");
                                        EqmtStatus.DCSupply[i] = false;
                                        break;
                                }
                            }
                        }
                        #endregion
                        break;
                    case "PWRSUPPLY_1CH":
                        #region DC 1-Channel Init
                        switch (EquipModel.ToUpper())
                        {
                            case "E3633A":
                            case "E3644A":
                                Eq.Site[0]._Eq_DC_1CH = new LibEqmtDriver.DC_1CH.E3633A(EquipAddr);
                                Eq.Site[0]._Eq_DC_1CH.Init();
                                EqmtStatus.DC_1CH = true;
                                MessageForInstrument("DC-1CH - E3633A OR E3644A", true);
                                break;
                            case "N6700B":
                                Eq.Site[0]._Eq_DC_1CH = new LibEqmtDriver.DC_1CH.N6700B(EquipAddr);
                                Eq.Site[0]._Eq_DC_1CH.Init();
                                EqmtStatus.DC_1CH = true;
                                MessageForInstrument("DC-1CH - N6700B", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.DC_1CH = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment DC Supply 1-Channel Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment DC not require.");
                                EqmtStatus.DC_1CH = false;
                                break;
                        }
                        #endregion
                        break;
                    case "PWRSUPPLY":
                        #region DC Init
                        switch (EquipModel.ToUpper())
                        {
                            case "N6700B":
                                Eq.Site[0]._EqDC = new LibEqmtDriver.DC.N6700B(EquipAddr);
                                Eq.Site[0]._EqDC.Init();
                                EqmtStatus.DC = true;
                                MessageForInstrument("DC - N6700B", true);
                                break;
                            case "PS662xA":
                                Eq.Site[0]._EqDC = new LibEqmtDriver.DC.PS662xA(EquipAddr);
                                Eq.Site[0]._EqDC.Init();
                                EqmtStatus.DC = true;
                                MessageForInstrument("DC - PS662xA", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.DC = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment DC Supply Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment DC not require.");
                                EqmtStatus.DC = false;
                                break;
                        }
                        #endregion
                        break;
                    case "MXA":
                        #region SA Init

                        if (Eq.Site[0]._EqSA01 == null && Eq.Site[0]._EqSA02 == null)
                        {
                            string SA01model = SearchLocalSettingDictionary("Model", "MXA01");
                            string SA01addr = SearchLocalSettingDictionary("Address", "MXA01");
                            string SA02model = SearchLocalSettingDictionary("Model", "MXA02");
                            string SA02addr = SearchLocalSettingDictionary("Address", "MXA02");
                            bool cal_MXA = false;
                            bool status = false;

                            switch (SA01model.ToUpper())
                            {
                                case "N9020A":
                                    //MXA Alignment Calibration
                                    string cnt_str = Interaction.InputBox("Do you want to perform MXA#01 alignment cal?\n" + "If so, please enter \"Yes\".", "MXA#01 ALIGNMENT", "No", 200, 200);
                                    switch (cnt_str.ToUpper())
                                    {
                                        case "NO":
                                            break;
                                        case "YES":
                                            cal_MXA = true;
                                            break;
                                        case "CANCEL":
                                            break;
                                        default:
                                            break;
                                    }

                                    Eq.Site[0]._EqSA01 = new LibEqmtDriver.SA.N9020A(SA01addr);
                                    Eq.Site[0]._EqSA01.Preset();
                                    if (cal_MXA)
                                    {
                                        Eq.Site[0]._EqSA01.CAL();
                                    }
                                    EqmtStatus.MXA01 = true;
                                    MessageForInstrument("SA01 - N9020A", true);
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXA01 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXA Model : " + SA01model.ToUpper(), "Pls ignore if Equipment MXA not required.");
                                    EqmtStatus.MXA01 = false;
                                    break;
                            }

                            switch (SA02model.ToUpper())
                            {
                                case "N9020A":
                                    //MXA Alignment Calibration
                                    string cnt_str = Interaction.InputBox("Do you want to perform MXA#02 alignment cal?\n" + "If so, please enter \"Yes\".", "MXA#02 ALIGNMENT", "No", 200, 200);
                                    switch (cnt_str.ToUpper())
                                    {
                                        case "NO":
                                            break;
                                        case "YES":
                                            cal_MXA = true;
                                            break;
                                        case "CANCEL":
                                            break;
                                        default:
                                            break;
                                    }

                                    Eq.Site[0]._EqSA02 = new LibEqmtDriver.SA.N9020A(SA02addr);
                                    Eq.Site[0]._EqSA02.Preset();
                                    if (cal_MXA)
                                    {
                                        Eq.Site[0]._EqSA02.CAL();
                                    }
                                    EqmtStatus.MXA02 = true;
                                    MessageForInstrument("SA02 - N9020A", true);
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXA02 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXA02 Model : " + SA02model.ToUpper(), "Pls ignore if Equipment MXA02 not required.");
                                    EqmtStatus.MXA02 = false;
                                    break;
                            }

                            if (cal_MXA)
                            {
                                DelayMs(60000);        //delay to wait for alignment to complete
                            }
                            else
                            {
                                DelayMs(1000);
                            }

                            switch (SA01model.ToUpper())
                            {
                                case "N9020A":
                                    //MXA Display Enable/Disable
                                    string cnt_str = Interaction.InputBox("Do you want to enable MXA#01 Display?\n" + "If so, please enter \"Yes\".", "Penang NPI", "No", 200, 200);
                                    switch (cnt_str.ToUpper())
                                    {
                                        case "YES":
                                            MXA_DisplayEnable = false;
                                            break;
                                        case "NO":
                                            MXA_DisplayEnable = true;
                                            break;
                                        default:
                                            MXA_DisplayEnable = true;
                                            break;
                                    }

                                    status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                                    Eq.Site[0]._EqSA01.AUTOALIGN_ENABLE(false);
                                    Eq.Site[0]._EqSA01.Initialize(3);
                                    if (MXA_DisplayEnable)
                                    {
                                        Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                                    }
                                    else
                                    {
                                        Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    }
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXA01 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXA Model : " + SA01model.ToUpper(), "Pls ignore if Equipment MXA not required.");
                                    EqmtStatus.MXA01 = false;
                                    break;
                            }

                            switch (SA02model.ToUpper())
                            {
                                case "N9020A":
                                    //MXA Display Enable/Disable
                                    string cnt_str = Interaction.InputBox("Do you want to enable MXA#02 Display?\n" + "If so, please enter \"Yes\".", "Penang NPI", "No", 200, 200);
                                    switch (cnt_str.ToUpper())
                                    {
                                        case "YES":
                                            MXA_DisplayEnable = false;
                                            break;
                                        case "NO":
                                            MXA_DisplayEnable = true;
                                            break;
                                        default:
                                            MXA_DisplayEnable = true;
                                            break;
                                    }

                                    status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                                    Eq.Site[0]._EqSA02.AUTOALIGN_ENABLE(false);
                                    Eq.Site[0]._EqSA02.Initialize(3);
                                    if (MXA_DisplayEnable)
                                    {
                                        Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                                    }
                                    else
                                    {
                                        Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    }
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXA02 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXA02 Model : " + SA02model.ToUpper(), "Pls ignore if Equipment MXA02 not required.");
                                    EqmtStatus.MXA02 = false;
                                    break;
                            }
                        }

                        #endregion
                        break;
                    case "MXG":
                        if (Eq.Site[0]._EqSA01 == null && Eq.Site[0]._EqSA02 == null)
                        {
                            #region SG Init
                            string SG01model = SearchLocalSettingDictionary("Model", "MXG01");
                            string SG01addr = SearchLocalSettingDictionary("Address", "MXG01");
                            string SG02model = SearchLocalSettingDictionary("Model", "MXG02");
                            string SG02addr = SearchLocalSettingDictionary("Address", "MXG02");
                            bool status = false;

                            switch (SG01model.ToUpper())
                            {
                                case "N5182A":
                                    Eq.Site[0]._EqSG01 = new LibEqmtDriver.SG.N5182A(SG01addr);
                                    Eq.Site[0]._EqSG01.Reset();
                                    foreach (string key in DicWaveForm.Keys)
                                    {
                                        Eq.Site[0]._EqSG01.MOD_FORMAT_WITH_LOADING_CHECK(key.ToString(), DicWaveForm[key].ToString(), true);
                                        DelayMs(500);
                                        status = Eq.Site[0]._EqSG01.OPERATION_COMPLETE();
                                        Eq.Site[0]._EqSG01.QueryError_SG(out InitInstrStatus);
                                        if (!InitInstrStatus)
                                        {
                                            MessageBox.Show("Test Program Will Abort .. Please Fixed The Issue", "Equipment MXG01 Model : " + SG01model.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            break;
                                        }
                                    }
                                    Eq.Site[0]._EqSG01.Initialize();
                                    Eq.Site[0]._EqSG01.SetAmplitude(-110);
                                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    Eq.Site[0]._EqSG01.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    EqmtStatus.MXG01 = true;
                                    MessageForInstrument("SG01 - N5182A", true);
                                    break;
                                case "E8257D":
                                    Eq.Site[0]._EqSG01 = new LibEqmtDriver.SG.E8257D(SG01addr);
                                    Eq.Site[0]._EqSG01.Initialize();
                                    Eq.Site[0]._EqSG01.SetAmplitude(-40);
                                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    Eq.Site[0]._EqSG01.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    EqmtStatus.MXG01 = true;
                                    MessageForInstrument("SG01 - E8257D", true);
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXG01 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXG01 Model : " + SG01model.ToUpper(), "Pls ignore if Equipment MXA01 not required.");
                                    EqmtStatus.MXG01 = false;
                                    break;
                            }

                            switch (SG02model.ToUpper())
                            {
                                case "N5182A":
                                    Eq.Site[0]._EqSG02 = new LibEqmtDriver.SG.N5182A(SG02addr);
                                    Eq.Site[0]._EqSG01.Reset();
                                    foreach (string key in DicWaveForm.Keys)
                                    {
                                        Eq.Site[0]._EqSG02.MOD_FORMAT_WITH_LOADING_CHECK(key.ToString(), DicWaveForm[key].ToString(), true);
                                        DelayMs(500);
                                        status = Eq.Site[0]._EqSG02.OPERATION_COMPLETE();
                                        Eq.Site[0]._EqSG02.QueryError_SG(out InitInstrStatus);
                                        if (!InitInstrStatus)
                                        {
                                            MessageBox.Show("Test Program Will Abort .. Please Fixed The Issue", "Equipment MXG02 Model : " + SG02model.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            break;
                                        }
                                    }
                                    Eq.Site[0]._EqSG02.Initialize();
                                    Eq.Site[0]._EqSG02.SetAmplitude(-110);
                                    Eq.Site[0]._EqSG02.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    Eq.Site[0]._EqSG02.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    EqmtStatus.MXG02 = true;
                                    MessageForInstrument("SG02 - N5182A", true);
                                    break;
                                case "E8257D":
                                    Eq.Site[0]._EqSG02 = new LibEqmtDriver.SG.E8257D(SG01addr);
                                    Eq.Site[0]._EqSG02.Initialize();
                                    Eq.Site[0]._EqSG02.SetAmplitude(-40);
                                    Eq.Site[0]._EqSG02.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    Eq.Site[0]._EqSG02.EnableModulation(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                                    EqmtStatus.MXG02 = true;
                                    MessageForInstrument("SG02 - E8257D", true);
                                    break;
                                case "NONE":
                                case "NA":
                                    EqmtStatus.MXG02 = false;
                                    // Do Nothing , equipment not present
                                    break;
                                default:
                                    MessageBox.Show("Equipment MXG02 Model : " + SG02model.ToUpper(), "Pls ignore if Equipment MXG02 not required.");
                                    EqmtStatus.MXG02 = false;
                                    break;
                            }
                            #endregion
                        }
                        break;
                    case "PXI_VST":
                        #region VST Init

                        //Ivan - Create alias name for MIPI Card Info
                        Alias_VST = SearchLocalSettingDictionary("Model", "PXI_VST");
                        Alias_VST = "VST";

                        switch (EquipModel.ToUpper())
                        {
                            case "NI5644R":
                            case "PXIE-5644R":
                                Eq.Site[0]._EqVST = new LibEqmtDriver.NF_VST.NF_NiPXI_VST(EquipAddr);
                                Eq.Site[0]._EqRFmx = new LibEqmtDriver.NF_VST.NF_NI_RFmx(EquipAddr);
                                Eq.Site[0]._EqVST.Initialize_NI5644R();
                                Eq.Site[0]._EqVST.IQRate = 120e6;

                                foreach (string key in DicWaveForm.Keys)
                                {
                                    Eq.Site[0]._EqVST.MOD_FORMAT_CHECK(key.ToString(), DicWaveForm[key].ToString(), DicWaveFormMutate[key].ToString(), true);
                                }
                                Eq.Site[0]._EqVST.PreConfigureVST();
                                EqmtStatus.PXI_VST = true;
                                MessageForInstrument("VST - 5644R", true);
                                break;
                            case "NI5646R":
                            case "PXIE-5646R":
                                Eq.Site[0]._EqVST = new LibEqmtDriver.NF_VST.NF_NiPXI_VST(EquipAddr);
                                Eq.Site[0]._EqRFmx = new LibEqmtDriver.NF_VST.NF_NI_RFmx(EquipAddr);
                                Eq.Site[0]._EqVST.Initialize();
                                Eq.Site[0]._EqVST.IQRate = 250E6;

                                foreach (string key in DicWaveForm.Keys)
                                {
                                    Eq.Site[0]._EqVST.MOD_FORMAT_CHECK(key.ToString(), DicWaveForm[key].ToString(), DicWaveFormMutate[key].ToString(), true);
                                }
                                Eq.Site[0]._EqVST.PreConfigureVST();
                                EqmtStatus.PXI_VST = true;
                                MessageForInstrument("VST - 5646R", true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.PXI_VST = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment PXI VST Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment PXI_VST not required");
                                EqmtStatus.TuneFilter = false;
                                break;
                        }

                        #region RFMX NF Initial Setting -Seoul & VST Calibration
                        if (EqmtStatus.PXI_VST)
                        {
                            Eq.Site[0]._EqRFmx.InitList(DicTestPA.Count());

                            for (int i = 0; i < DicTestPA.Count(); i++)
                            {
                                string parameterName;
                                DicTestPA[i].TryGetValue("TEST PARAMETER", out parameterName);

                                if (parameterName.ToUpper() == "NF_CAL" || parameterName.ToUpper() == "PXI_NF_COLD" || parameterName.ToUpper() == "PXI_NF_HOT" || parameterName.ToUpper() == "PXI_NF_MEAS" || parameterName.ToUpper() == "PXI_NF_COLD_MIPI"
                                    || parameterName.ToUpper() == "PXI_NF_COLD_ALLINONE" || parameterName.ToUpper() == "PXI_NF_COLD_MIPI_ALLINONE") // Ben, add "PXI_NF_COLD_MIPI"
                                {
                                    double nF_BW = Convert.ToDouble(DicTestPA[i]["NF_BW"]);
                                    double nF_REFLEVEL = Convert.ToDouble(DicTestPA[i]["NF_REFLEVEL"]);
                                    double nF_SWEEPTIME = Convert.ToDouble(DicTestPA[i]["NF_SWEEPTIME"]);
                                    int nF_AVERAGE = Convert.ToInt32(DicTestPA[i]["NF_AVERAGE"]);
                                    string calSetID = DicTestPA[i]["NF_CALTAG"];

                                    double[] dutInputLoss, dutOutputLoss, DUTAntForTxMeasureLoss, freqList;

                                    NFvariables(DicTestPA[i], parameterName, out DUTAntForTxMeasureLoss, out dutInputLoss, out dutOutputLoss, out freqList);
                                    Eq.Site[0]._EqRFmx.cRFmxNF.ListConfigureSpecNFColdSource(i, nF_BW, nF_SWEEPTIME, nF_AVERAGE, nF_REFLEVEL, parameterName.ToUpper(), dutInputLoss, dutOutputLoss, freqList, calSetID);

                                    if (parameterName.ToUpper() == "PXI_NF_HOT" && SearchLocalSettingDictionary("Model", "PWRMETER").ToUpper() == "NONE")
                                    {
                                        double _StartTxFreq = Convert.ToDouble(DicTestPA[i]["START_TXFREQ1"]);
                                        double _StopTxFreq = Convert.ToDouble(DicTestPA[i]["STOP_TXFREQ1"]);
                                        double _TargetTxPout = Convert.ToDouble(DicTestPA[i]["POUT1"]);
                                        string _modulation = Convert.ToString(DicTestPA[i]["MODULATION"]);
                                        string _Waveform = Convert.ToString(DicTestPA[i]["WAVEFORMNAME"]);
                                        double channelBW = 0;

                                        Eq.Site[0]._EqVST.Get_SignalBandwidth_fromModulation(_modulation, _Waveform, out channelBW);
                                        Eq.Site[0]._EqRFmx.cRFmxChp.ConfigureSpec(i, ((_StartTxFreq + _StopTxFreq) / 2), _TargetTxPout + DUTAntForTxMeasureLoss.Average(), channelBW, 600, 0.001);
                                    }
                                }
                            }

                            if (!ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE))
                            {
                                //VST Calibration
                                #region VST Calibration

                                bool bSelfcal_Flag = false;
                                double dSelfCalLast_SA_temperature = 0.0f;
                                double dSelfCalLast_SG_temperature = 0.0f;
                                double dSA_temperature = 0.0f;
                                double dSG_temperature = 0.0f;

                                dSA_temperature = Eq.Site[0]._EqVST.rfsaSession.DeviceCharacteristics.GetDeviceTemperature();
                                dSG_temperature = Eq.Site[0]._EqVST.rfsgSession.DeviceCharacteristics.DeviceTemperature;

                                CheckVSTTemperature(ref dSelfCalLast_SA_temperature, ref dSelfCalLast_SG_temperature, ref dSA_temperature, ref dSG_temperature, ref bSelfcal_Flag);

                                #endregion VST Calibration

                                #region Close VSG/VSA session if this is RF_Calibration case -Seoul
                            }

                            for (int i = 0; i < DicTestPA.Count(); i++)
                            {
                                string testMode, testParameter;
                                DicTestPA[i].TryGetValue("TEST MODE", out testMode);
                                DicTestPA[i].TryGetValue("TEST PARAMETER", out testParameter);

                                if (testMode.ToUpper() == "CALIBRATION")
                                {
                                    if (testParameter.ToUpper() == "RF_CAL")
                                        Eq.Site[0]._EqVST.Close_VST();

                                    //VST Calibration : Set True
                                    bTestCalibration = true;
                                }
                            }
                            #endregion Close VSG/VSA session if this is RF_Calibration case -Seoul
                        }
                        #endregion RFMX NF Initial Setting -Seoul & VST Calibration

                        #endregion
                        break;
                    case "PWRMETER":
                        #region Power Sensor Init
                        switch (EquipModel.ToUpper())
                        {
                            case "E4416A":
                            case "E4417A":
                                Eq.Site[0]._EqPwrMeter = new LibEqmtDriver.PS.E4417A(EquipAddr);
                                Eq.Site[0]._EqPwrMeter.Initialize(1);
                                EqmtStatus.PM = true;
                                MessageForInstrument("Power Sensor - E4416A or E4417A", true);
                                break;
                            case "NRPZ11":
                            case "NRPZ21":
                            case "NRP8S":
                                Eq.Site[0]._EqPwrMeter = new LibEqmtDriver.PS.RSNRPZ11("");
                                Eq.Site[0]._EqPwrMeter.Initialize(1);
                                Eq.Site[0]._EqPwrMeter.SetFreq(1, 1500, PowerSensorMeasuringType);
                                DelayMs(200);
                                dummyData = Eq.Site[0]._EqPwrMeter.MeasPwr(1);
                                EqmtStatus.PM = true;
                                MessageForInstrument(string.Format("Power Sensor - {0}", EquipModel.ToUpper()), true);
                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.PM = false;
                                Eq.Site[0]._isUseRFmxForTxMeasure = true;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment POWERSENSOR Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment Power Sensor not require.");
                                Eq.Site[0]._isUseRFmxForTxMeasure = true;
                                EqmtStatus.PM = false;
                                break;
                        }
                        #endregion
                        break;
                    case "MIPI":
                        #region MIPI Init
                        string AemulusPxi_FileName = SearchLocalSettingDictionary("Address", "APXI_FileName");

                        #region MIPI Configure - DUT MIPI
                        try
                        {
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Clock_Speed"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiNFRClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_NFR_Clock_Speed"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiSyncWriteRead = Convert.ToBoolean(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Clock_Sync_Write_Read"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPBurnClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_OTP_Burn_Clock_Speed"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPReadClockSpeed = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_OTP_Read_Clock_Speed"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_VIO_Voltage"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIH"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIL"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOH"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOL"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VTT"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiType = SearchLocalSettingDictionary("MIPI_Config", "MIPI_Type").ToUpper();
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.StrobePoint = Convert.ToDouble(DicCalInfo[DataFilePath.HSDIO_StrobePoint]);
                        }
                        catch (Exception ex)
                        {
                            Helper.AutoClosingMessageBox.Show(string.Format("Pleaseh Check - MIPI Config: {0}", ex), "MIPI Configure Error");
                            MPAD_TestTimer.LoggingManager.Instance.LogError("[Fail] Pleaseh Check - MIPI Config : " + ex);

                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiClockSpeed = 26e6;
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiNFRClockSpeed = 51.2e6;
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiSyncWriteRead = true;
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPBurnClockSpeed = 26e6;
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiOTPReadClockSpeed = 26e6;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = 1.2;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = 1.2;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = 0.6;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = 0.8;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = 0.0;
                            //LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = 3.0;
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIOTargetVoltage = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_VIO_Voltage"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIH"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VIL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VIL"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOH = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOH"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VOL = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VOL"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.VTT = Convert.ToDouble(SearchLocalSettingDictionary("MIPI_Config", "MIPI_Level_VTT"));
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.MipiType = "RZ";
                            LibEqmtDriver.MIPI.Lib_Var.DUTMipi.StrobePoint = 0.9;
                        }

                        LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = true;

                        EqmtStatus.MIPI = true;
                        #endregion

                        #region MIPI Pin Config
                        //use for MIPI pin initialization
                        string mipiPairCount = "";
                        LibEqmtDriver.MIPI.s_MIPI_PAIR[] tmp_mipiPair;
                        mipiPairCount = SearchLocalSettingDictionary("MIPI_PIN_CFG", "Mipi_Pair_Count");
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
                                tmp_mipiPair[i].SCLK = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SCLK_" + i);
                                tmp_mipiPair[i].SDATA = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SDATA_" + i);
                                tmp_mipiPair[i].SVIO = SearchLocalSettingDictionary("MIPI_PIN_CFG", "SVIO_" + i);
                            }
                        }

                        #endregion

                        switch (EquipModel.ToUpper())
                        {

                            case "DM280E":
                                try
                                {
                                    LibEqmtDriver.MIPI.Lib_Var.myDM280Address = EquipAddr;
                                    LibEqmtDriver.MIPI.Lib_Var.DM280_CH0 = 0;
                                    LibEqmtDriver.MIPI.Lib_Var.DM280_CH1 = 1;
                                    LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                                    //PM Trigger 
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                                    //Read Function
                                    string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                                    LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                                    //Init
                                    Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM280e();
                                    Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                                    EqmtStatus.MIPI = true;
                                    MessageForInstrument("HSDIO - DM280E", true);
                                }
                                catch (Exception ex)
                                {
                                    //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                                    //EqmtStatus.MIPI = false;
                                    MessageBox.Show("DM280E MIPI cards not detected, please check!", ex.ToString());
                                    return;
                                }
                                break;
                            case "DM482E":
                                try
                                {
                                    LibEqmtDriver.MIPI.Lib_Var.myDM482Address = EquipAddr;
                                    LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                                    //PM Trigger 
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                                    //Read Function
                                    string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                                    LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                                    //Load MIPI Vector File to Memory
                                    string vectorBasePath = GetTestPlanPath() + @"RFFE_vectors\";
                                    LibEqmtDriver.MIPI.Lib_Var.VectorPATH = vectorBasePath;

                                    //Init
                                    string AemulusePxi_Path = "C:\\Aemulus\\common\\map_file\\";
                                    AemulusePxi_Path += AemulusPxi_FileName;
                                    LibEqmtDriver.MIPI.Lib_Var.HW_Profile = AemulusePxi_Path;
                                    Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM482e();
                                    Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                                    EqmtStatus.MIPI = true;
                                    MessageForInstrument("HSDIO - DM482E", true);
                                }
                                catch (Exception ex)
                                {
                                    //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                                    //EqmtStatus.MIPI = false;
                                    MessageBox.Show("DM482E MIPI cards not detected, please check!", ex.ToString());
                                    return;
                                }

                                break;
                            case "DM482E_VEC":
                                try
                                {
                                    LibEqmtDriver.MIPI.Lib_Var.myDM482Address = EquipAddr;
                                    LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                                    //PM Trigger 
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                                    //Read Function
                                    string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                                    LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                                    //Init
                                    string AemulusePxi_Path = "C:\\Aemulus\\common\\map_file\\";
                                    AemulusePxi_Path += AemulusPxi_FileName;
                                    LibEqmtDriver.MIPI.Lib_Var.HW_Profile = AemulusePxi_Path;
                                    Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.Aemulus_DM482e_Vec();
                                    Eq.Site[0]._EqMiPiCtrl.Init(tmp_mipiPair);

                                    EqmtStatus.MIPI = true;
                                    MessageForInstrument("HSDIO - DM482E_VEC", true);
                                }
                                catch (Exception ex)
                                {
                                    //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                                    //EqmtStatus.MIPI = false;

                                    MessageBox.Show("DM482E MIPI (Vector Config) cards not detected, please check!", ex.ToString());
                                    return;
                                }

                                break;
                            case "NI6570":
                                try
                                {
                                    LibEqmtDriver.MIPI.Lib_Var.myNI6570Address = EquipAddr;
                                    LibEqmtDriver.MIPI.Lib_Var.SlaveAddress = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Slave_Address"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.ChannelUsed = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "Channel_Used"));
                                    //PM Trigger 
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig"), 16);
                                    LibEqmtDriver.MIPI.Lib_Var.PMTrig_Data = Convert.ToInt32(SearchLocalSettingDictionary("MIPI_Config", "PM_Trig_Data"), 16);
                                    //Read Function
                                    string read = SearchLocalSettingDictionary("MIPI_Config", "Read_Function");
                                    LibEqmtDriver.MIPI.Lib_Var.ReadFunction = (read.ToUpper() == "TRUE" ? true : false);

                                    //Init
                                    Eq.Site[0]._EqMiPiCtrl = new LibEqmtDriver.MIPI.NI_PXIe6570(tmp_mipiPair, Eq.Site[0]._PpmuResources);

                                    EqmtStatus.MIPI = true;
                                    MessageForInstrument("HSDIO - NI6570", true);
                                }
                                catch (Exception ex)
                                {
                                    //LibEqmtDriver.MIPI.Lib_Var.MIPI_Enable = false;
                                    //EqmtStatus.MIPI = false;
                                    MessageBox.Show("NI6570 MIPI cards not detected, please check!", ex.ToString());
                                    return;
                                }

                                break;
                            case "NONE":
                            case "NA":
                                EqmtStatus.MIPI = false;
                                // Do Nothing , equipment not present
                                break;
                            default:
                                MessageBox.Show("Equipment MIPI Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment MIPI not require.");
                                EqmtStatus.MIPI = false;
                                break;
                        }
                        #endregion

                        #region MIPI-PPMU Init

                        int PPMUtotalCH = Convert.ToInt32(SearchLocalSettingDictionary("SmuSetting", "TOTAL_PPMUCHANNEL"));

                        if (PPMUtotalCH > 0)
                        {
                            string[] PPMUSetting = new string[PPMUtotalCH];
                            for (int ppmuVIO = 0; ppmuVIO < PPMUtotalCH; ppmuVIO++)
                            {
                                PPMUSetting[ppmuVIO] = SearchLocalSettingDictionary("SmuSetting", "PPMUV_CH" + ppmuVIO);
                            }
                            LibEqmtDriver.MIPI.Lib_Var.isVioPpmu = PPMUtotalCH > 0;

                            Eq.Site[0]._EqPPMU = new LibEqmtDriver.SMU.iSmu[PPMUtotalCH];

                            for (int i = 0; i < PPMUtotalCH; i++)
                            {
                                switch (EquipModel.ToUpper())
                                {
                                    case "NI6570":
                                        Eq.Site[0]._EqPPMU[i] = new LibEqmtDriver.SMU.NI_PXIe6570_PPMU(PPMUSetting[i], Eq.Site[0]._EqMiPiCtrl);
                                        EqmtStatus.MIPI_PPMU = true;
                                        break;
                                    case "DM482E":
                                        Eq.Site[0]._EqPPMU[i] = new LibEqmtDriver.SMU.AemulusDM482ePPMU(PPMUSetting[i], Eq.Site[0]._EqMiPiCtrl);
                                        EqmtStatus.MIPI_PPMU = true;
                                        break;
                                    case "NONE":
                                    case "NA":
                                        EqmtStatus.MIPI_PPMU = false;
                                        break;
                                    default:
                                        MessageBox.Show("Equipment MIPI PPMU Model : " + EquipModel.ToUpper(), "Pls ignore if Equipment MIPI PPMU not require.");
                                        EqmtStatus.MIPI_PPMU = false;
                                        break;
                                }
                                if (Eq.Site[0]._EqPPMU[i] != null)
                                    Eq.Site[0]._PpmuResources.Add(Eq.Site[0]._EqPPMU[i].PinName, Eq.Site[0]._EqPPMU[i]);
                            }
                        }
                        else
                        {
                            LibEqmtDriver.MIPI.Lib_Var.isVioPpmu = false;
                        }
                        #endregion
                        break;
                    case "ENABLE":
                        #region Handler Init

                        strHandlerType = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_Type, "");

                        if (strHandlerType.CIvContainsAnyOf("HANDLERSIM", "HANDLERSIM002", "MULTISITEHANDLERSIM") || string.IsNullOrEmpty(strHandlerType))
                        {
                            Handler_Info = "FALSE";
                        }
                        else
                        {
                            //Handler_Info = "TRUE";
                            string HandlerSN = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_SN, "");

                            if (HandlerSN.CIvContainsAnyOf("HT", "IM"))
                            {
                                HandlerAddress = ATFRTE.Instance.HandlerAddress; //For Inari Handler address [Chee On].
                            }
                            else if (HandlerSN.Contains("NS"))
                            {
                                HandlerAddress = ATFRTE.Instance.HandlerAddress;
                            }
                            else
                            {
                                ///Get handlerAddress from ClothoRoot/System/Configuration/ATFConfig.xml > UserSection
                                HandlerAddress = ClothoDataObject.Instance.ATFConfiguration.UserSection.GetValue("HandlerAddress");
                            }
                        }

                        Handler_Info = EquipAddr;
                        if (Handler_Info == "TRUE")
                        {
                            try
                            {
                                using (Ping QuickPingTester = new Ping())
                                {
                                    if (!int.TryParse(HandlerAddress, out int _handlerAddress))
                                        _handlerAddress = 1;

                                    PingReply PingTest = QuickPingTester.Send(IPAddress.Parse("192.168.0.10" + _handlerAddress.ToString()), 3);

                                    if (PingTest.Status == IPStatus.Success)
                                    {
                                        if (handler != null)
                                            handler.Disconnect();

                                        if (HandlerAddress != null)
                                        {
                                            handler = new HontechHandler(_handlerAddress);
                                            handler.Connect();
                                            HandlerForce hli = handler.ContactForceQuery();
                                            //Thread.Sleep(200);
                                            DelayMs(200);
                                            hli = handler.ContactForceQuery();

                                            if (hli.ArmNo == 0 && hli.PlungerForce == 0 && hli.SiteNo == 0)
                                            {
                                                Flag_HandlerInfor = false;
                                            }
                                            else Flag_HandlerInfor = true;
                                        }
                                    }
                                    else
                                    {
                                        Flag_HandlerInfor = false;
                                    }
                                }
                            }
                            catch
                            {
                                Flag_HandlerInfor = false;
                            }
                        }

                        #endregion
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                MPAD_TestTimer.LoggingManager.Instance.LogError(String.Format("[Fail] Initialize the instruments\nMessage : {0}\nStack : {1}", ex.Message, ex.StackTrace));
            }
        }
        public void InstrUnInit()
        {
            try
            {
                if (EqmtStatus.MXG01)
                {
                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                    Eq.Site[0]._EqSG01.Close();
                    Eq.Site[0]._EqSG01 = null;
                    MessageForInstrument("SG01", false);
                }
                if (EqmtStatus.MXG02)
                {
                    Eq.Site[0]._EqSG02.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                    Eq.Site[0]._EqSG02.Close();
                    Eq.Site[0]._EqSG02 = null;
                    MessageForInstrument("SG02", false);
                }
                if (EqmtStatus.DC)
                {
                    //Eq.Site[0]._EqDC.Init();
                    Eq.Site[0]._EqDC.Close();
                    Eq.Site[0]._EqDC = null;
                    MessageForInstrument("DC", false);
                }
                if (EqmtStatus.DC_1CH)
                {
                    //Eq.Site[0]._Eq_DC_1CH.Init();
                    Eq.Site[0]._Eq_DC_1CH.Close();
                    Eq.Site[0]._Eq_DC_1CH = null;
                    MessageForInstrument("DC 1ch", false);
                }

                for (int i = 0; i < Eq.Site[0]._totalDCSupply; i++)
                {
                    if (EqmtStatus.DCSupply[i])
                    {
                        //Eq.Site[0]._Eq_DCSupply[i].Init();
                        Eq.Site[0]._Eq_DCSupply[i].Close();
                        Eq.Site[0]._Eq_DCSupply[i] = null;
                        MessageForInstrument("DC Supply" + i, false);
                    }
                }
                Eq.Site[0]._Eq_DCSupply = null;

                if (EqmtStatus.Switch)
                {
                    if (bMultiSW)
                    {
                        Eq.Site[0]._EqSwitch.SaveRemoteMechSwStatusFile();
                        Eq.Site[0]._EqSwitch.Close();
                        Eq.Site[0]._EqSwitch = null;
                        MessageForInstrument("Switch01", false);

                        Eq.Site[0]._EqSwitchSplit.SaveRemoteMechSwStatusFile();
                        Eq.Site[0]._EqSwitchSplit.Close();
                        Eq.Site[0]._EqSwitchSplit = null;
                        MessageForInstrument("Switch02", false);
                    }
                    else
                    {
                        Eq.Site[0]._EqSwitch.SaveRemoteMechSwStatusFile();
                        Eq.Site[0]._EqSwitch.Close();
                        Eq.Site[0]._EqSwitch = null;
                        MessageForInstrument("Switch01", false);
                    }
                }
                if (EqmtStatus.SMU)
                {
                    Eq.Site[0]._Eq_SMUDriver.DcOff(Eq.Site[0]._SMUSetting, Eq.Site[0]._EqSMU);
                    Eq.Site[0]._Eq_SMUDriver.Close(Eq.Site[0]._EqSMU);
                    Eq.Site[0]._EqSMU = null;
                    Eq.Site[0]._Eq_SMUDriver = null;
                    Eq.Site[0]._SMUSetting = null;
                    Eq.Site[0]._VCCSetting = null;
                    MessageForInstrument("SMU", false);
                }

                if (EqmtStatus.MXA01)
                {
                    Eq.Site[0]._EqSA01.Close();
                    Eq.Site[0]._EqSA01 = null;
                    MessageForInstrument("SA01", false);
                }
                if (EqmtStatus.MXA02)
                {
                    Eq.Site[0]._EqSA02.Close();
                    Eq.Site[0]._EqSA02 = null;
                    MessageForInstrument("SA02", false);
                }
                if (EqmtStatus.MIPI)
                {
                    Eq.Site[0]._EqMiPiCtrl.TurnOff_VIO(0);      //mipi pair 0 - DIO 0, DIO 1 and DIO 2 - For DUT TX
                    Eq.Site[0]._EqMiPiCtrl.TurnOff_VIO(1);      //mipi pair 1 - DIO 3, DIO 4 and DIO 5 - For ref Unit on Test Board / DUT RX
                    Eq.Site[0]._EqMiPiCtrl.TurnOff_VIO(2);      //mipi pair 2 - DIO 6, DIO 7 and DIO 8 - For future use
                    Eq.Site[0]._EqMiPiCtrl.TurnOff_VIO(3);      //mipi pair 3 - DIO 9, DIO 10 and DIO 11 - For future use
                    Eq.Site[0]._EqMiPiCtrl = null;
                    Eq.Site[0]._EqPPMU = null;
                    Eq.Site[0]._PpmuResources = null;
                    MessageForInstrument("MIPI", false);
                }
                if (EqmtStatus.PXI_VST)
                {
                    Eq.Site[0]._EqVST.Close_VST();
                    Eq.Site[0]._EqRFmx.CloseSession();
                    Eq.Site[0]._EqVST = null;
                    Eq.Site[0]._EqRFmx = null;
                    MessageForInstrument("VST", false);
                }

                if (EqmtStatus.PM)
                {
                    Eq.Site[0]._EqPwrMeter.Close();
                    Eq.Site[0]._EqPwrMeter = null;
                    MessageForInstrument("Power Sensor", false);
                }
            }
            catch (Exception ex)
            {
                MPAD_TestTimer.LoggingManager.Instance.LogError(String.Format("[Fail] Unitialize the instruments - Error : {0}", ex.Message));
            }

        }
        public void MessageForInstrument(string Inst, bool isInit)
        {
            if (isInit)
                MPAD_TestTimer.LoggingManager.Instance.LogInfo(string.Format("[Success] Initialize the instrument : {0}", Inst));
            else
                MPAD_TestTimer.LoggingManager.Instance.LogInfo(string.Format("[Success] Uninitialize the instrument : {0}", Inst));
        }
    }
}
