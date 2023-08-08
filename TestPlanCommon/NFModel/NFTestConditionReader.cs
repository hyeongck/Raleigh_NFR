using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

using Avago.ATF.StandardLibrary;
using MPAD_TestTimer;

using ClothoLibAlgo;
using ClothoSharedItems;
using LibEqmtDriver;
using TestPlanCommon.CommonModel;

namespace TestPlanCommon.NFModel
{
    public class NFTestConditionReader : TestConditionReaderBase
    {
        public NFTestConditionFactory cNFTestCondition;
        public NFStructure cNFStructure;

        public Dictionary<string, string> TCF_Setting;
        public Dictionary<string, Dictionary<string, string>> DicLocalSettingfile;
        public Dictionary<string, Dictionary<double, double>> DicLocalCableLossFile, DicLocalBoardLossFile;

        const string ConstExStartTxt = "#START";
        const string ConstExEndTxt = "#END";
        const string ConstExSkipTxt = "X";
        const string ConstExLabelTxt = "#LABEL";

        public NFTestConditionReader()
        {
            cNFTestCondition = new NFTestConditionFactory();
            cNFStructure = new NFStructure();
            TCF_Setting = new Dictionary<string, string>();
            DicLocalSettingfile = new Dictionary<string, Dictionary<string, string>>();
            DicLocalCableLossFile = new Dictionary<string, Dictionary<double, double>>();
            DicLocalBoardLossFile = new Dictionary<string, Dictionary<double, double>>();
        }
        ~ NFTestConditionReader()
        {

        }

        public void ReadTCFAllSheet()
        {
            try
            {
                ManualResetEvent[] DoneEvents = new ManualResetEvent[5];

                for (int i = 0; i < DoneEvents.Length; i++)
                {
                    DoneEvents[i] = new ManualResetEvent(false);
                }

                ThreadWithDelegate ThLoadPaTCF = new ThreadWithDelegate(DoneEvents[0]);
                ThLoadPaTCF.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadPaTCF);
                ThreadPool.QueueUserWorkItem(ThLoadPaTCF.ThreadPoolCallback, 0);

                ThreadWithDelegate ThLoadWaveForm = new ThreadWithDelegate(DoneEvents[1]);
                ThLoadWaveForm.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadWafeForm);
                ThreadPool.QueueUserWorkItem(ThLoadWaveForm.ThreadPoolCallback, 0);

                ThreadWithDelegate ThLoadCalTCF = new ThreadWithDelegate(DoneEvents[2]);
                ThLoadCalTCF.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadCalSheet);
                ThreadPool.QueueUserWorkItem(ThLoadCalTCF.ThreadPoolCallback, 0);

                ThreadWithDelegate ThLoadMipiReg = new ThreadWithDelegate(DoneEvents[3]);
                ThLoadMipiReg.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadMipiReg);
                ThreadPool.QueueUserWorkItem(ThLoadMipiReg.ThreadPoolCallback, 0);

                ThreadWithDelegate ThLoadPwrBlast = new ThreadWithDelegate(DoneEvents[4]);
                ThLoadPwrBlast.WorkExternal = new ThreadWithDelegate.DoWorkExternal(ReadPwrBlast);
                ThreadPool.QueueUserWorkItem(ThLoadPwrBlast.ThreadPoolCallback, 0);

                WaitHandle.WaitAll(DoneEvents);

                NFTestFactory myTestFactory = new NFTestFactory(null);

                myTestFactory.PopulateAllPaTests(NFTestConditionFactory.DicTestPA);

                ClothoDataObject.Instance.TCF_Setting = TCF_Setting;

                LoggingManager.Instance.LogInfo(string.Format("Succeed to reading NF RISE Test Plan!"));
            }
            catch (Exception Ex)
            {
                LoggingManager.Instance.LogInfo(string.Format("Failed to reading NF RISE Test Plan! \n {0}", Ex.ToString()));
            }
        }

        public void ReadSubFiles()
        {
            try
            {
                string LocSetFilePath = Convert.ToString(NFTestConditionFactory.DicCalInfo[DataFilePathLib.LocSettingPath]);
                string CalFilePath = Convert.ToString(NFTestConditionFactory.DicCalInfo[DataFilePathLib.CalPathRF]);
                string BoardlossFilePath = GetTestPlanPath() + @"BOARDLOSS\" + Convert.ToString(NFTestConditionFactory.DicCalInfo[DataFilePathLib.BoardLossPath]);

                NFTestConditionFactory.CalList = ReadCalProcedure(LocSetFilePath);
                NFTestConditionFactory.DicCalInfo[DataFilePathLib.BoardLossPath] = BoardlossFilePath;

                GenerateDicLocalFile(LocSetFilePath, ref DicLocalSettingfile); // Read Local Setting File
                LoadCalFreqListandGenerateDic(CalFilePath, "1D-Combined", ref DicLocalCableLossFile); // Read Cable Calibration File
                LoadCalFreqListandGenerateDic(BoardlossFilePath, "1D-Combined", ref DicLocalBoardLossFile); // Read Board Loss File

                LoggingManager.Instance.LogInfo(string.Format("Succeed to reading calibration files!"));
            }
            catch (Exception Ex)
            {
                LoggingManager.Instance.LogInfo(string.Format("Failed to reading calibration files! \n {0}", Ex.ToString()));
            }

        }

        public void CheckTestPlan()
        {
            #region Check TCF, Local Setting File & Cal Data

            #region Check TCF - Check Tx Freq, Rx Freq, Switch Path (Gain Rx Path, Tx pout Path, ANT NF Path, ANT Tx Path

            #endregion

            #region Check Local Setting File

            #endregion

            #region Check Cal Data

            #endregion

            #endregion
        }

        public class ThreadWithDelegate
        {
            public delegate void DoWorkExternal();
            public DoWorkExternal WorkExternal;
            private ManualResetEvent _doneEvent;

            public ThreadWithDelegate(ManualResetEvent doneEvent)
            {
                _doneEvent = doneEvent;
            }

            // Wrapper method for use with thread pool.
            public void ThreadPoolCallback(Object threadContext)
            {
                WorkExternal();
                _doneEvent.Set();
            }
        }
        private void ReadPaTCF()
        {
            ReadTCF(TCF_Sheet.ConstPASheetName, TCF_Sheet.ConstPAIndexColumnNo, TCF_Sheet.ConstPATestParaColumnNo, ref NFTestConditionFactory.DicTestPA, ref NFTestConditionFactory.DicTestLabel);
            MyProduct.MyDUT.DicTestLabel = NFTestConditionFactory.DicTestLabel;
        }
        private void ReadCalSheet()
        {
            ReadCalSheet(TCF_Sheet.ConstCalSheetName, TCF_Sheet.ConstCalIndexColumnNo, TCF_Sheet.ConstCalParaColumnNo, ref NFTestConditionFactory.DicCalInfo);
          //  MyProduct.MyDUT.DicCalInfo = NFTestConditionFactory.DicCalInfo;
        }
        private void ReadWafeForm()
        {
            ReadWaveformFilePath(TCF_Sheet.ConstKeyWordSheetName, TCF_Sheet.ConstWaveFormColumnNo, ref NFTestConditionFactory.DicWaveForm, ref NFTestConditionFactory.DicWaveFormMutate, ref NFTestConditionFactory.DicWaveFormAlias);
            MyProduct.MyDUT.DicWaveFormAlias = NFTestConditionFactory.DicWaveFormAlias;
            MyProduct.MyDUT.DicWaveForm = NFTestConditionFactory.DicWaveForm;
            MyProduct.MyDUT.DicWaveFormMutate = NFTestConditionFactory.DicWaveFormMutate;
        }
        private void ReadMipiReg()
        {
            ReadMipiReg(TCF_Sheet.ConstMipiRegSheetName, TCF_Sheet.ConstMipiKeyIndexColumnNo, TCF_Sheet.ConstMipiRegColumnNo, ref NFTestConditionFactory.DicMipiKey);
            MyProduct.MyDUT.DicMipiKey = NFTestConditionFactory.DicMipiKey;
        }
        private void ReadPwrBlast()
        {
            ReadPwrBlast(TCF_Sheet.ConstPwrBlastSheetName, TCF_Sheet.ConstPwrBlastIndexColumnNo, TCF_Sheet.ConstPwrBlastColumnNo, ref NFTestConditionFactory.DicPwrBlast);
            MyProduct.MyDUT.DicPwrBlast = NFTestConditionFactory.DicPwrBlast;
        }



        public void ReadCalSheet(int SheetNo, int IndexColumnNo, int CalParaColumnNo, ref Dictionary<string, string> DicCalInfo)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 1;
            bool StarCalcuteCalNo = false;

            DicCalInfo = new Dictionary<string, string>();

            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo);
                }
                catch (Exception)
                {

                    strExInput = "";
                }

                try
                {
                    strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, CalParaColumnNo);
                }
                catch (Exception)
                {

                    strExTestItems = "";
                }


                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteCalNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    DicCalInfo.Add(strExInput, strExTestItems);
                    TCF_Setting.Add(strExInput, strExTestItems);
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    StarCalcuteCalNo = true;
                }
                intRow++;
            }
        }
        public void ReadCalSheet(string SheetName, int IndexColumnNo, int CalParaColumnNo, ref Dictionary<string, string> DicCalInfo)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 1;
            bool StarCalcuteCalNo = false;

            DicCalInfo = new Dictionary<string, string>();

            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, IndexColumnNo);
                }
                catch (Exception)
                {

                    strExInput = "";
                }

                try
                {
                    strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, CalParaColumnNo);
                }
                catch (Exception)
                {

                    strExTestItems = "";
                }


                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteCalNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    DicCalInfo.Add(strExInput, strExTestItems);
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    StarCalcuteCalNo = true;
                }

                TCF_Setting.Add(strExInput, strExTestItems);
                intRow++;
            }
        }

        public void ReadTCF(int SheetNo, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 1, intTotalTestNo = 0;
            int TestStartRow = 0, intTotaltColumnNo = 0;
            int intTestCount = 0;
            bool StarCalcuteTestNo = false;

            #region Calculate Test Parameter
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo);
                }
                catch (Exception)
                {

                    strExInput = "";
                }


                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {

                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (true)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }

                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 2; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion
        }
        public void ReadTCF(int SheetNo, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest, ref Dictionary<string, string> DicLabel)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 1, intTotalTestNo = 0;
            int TestStartRow = 0, intTotaltColumnNo = 0;
            int intTestCount = 0;
            int TestLabelRow = 0;
            bool StarCalcuteTestNo = false;

            #region Calculate Test Parameter
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }

                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                else if (strExInput.ToUpper() == ConstExLabelTxt)
                {
                    TestLabelRow = intRow;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (true)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 2; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion

            #region Parameter Label Generation

            DicLabel = new Dictionary<string, string>();

            string currentLabelCondName = "";
            string currentLabelCondValue = "";

            for (int i = 2; i <= intTotaltColumnNo; i++)
            {
                try
                {
                    currentLabelCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, i).ToUpper();
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    currentLabelCondName = "";
                }
                try
                {
                    currentLabelCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestLabelRow, i);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    currentLabelCondValue = "";
                }

                if (currentLabelCondValue == "")
                    currentLabelCondValue = "NA";
                if (currentLabelCondName.Trim() != "")
                    DicLabel.Add(currentLabelCondName, currentLabelCondValue);
            }



            #endregion
        }
        public void ReadTCF(string SheetName, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest, ref Dictionary<string, string> DicLabel)
        {
            int intTotalTestNo = 0, intTestCount = 0;
            int intTotalRowNo = 0, intTotaltColumnNo = 0, intTotalParamNo = 0;
            int TestStartRow = 0, TestLabelRow = 0;
            bool StarCalcuteTestNo = false;

            Tuple<bool, string, string[,]> TestConditionContents = ATFCrossDomainWrapper.Excel_Get_IputRangeByValue(SheetName, 1, 1, 2500, 250);
            string[,] TestInput = TestConditionContents.Item3;

            #region Calculate Test Parameter - Row and Column No
            for (int iRow = 0; iRow < TestInput.GetUpperBound(0); iRow++)
            {
                if (TestInput[iRow, 0].ToUpper().StartsWith("#LABEL"))
                {
                    TestLabelRow = iRow;
                }
                if (TestInput[iRow, 0].ToUpper().StartsWith("#END"))
                {
                    break;
                }
                if (StarCalcuteTestNo && TestInput[iRow, 0].ToUpper() != "X")
                {
                    intTotalTestNo++;
                }
                if (TestInput[iRow, 0].ToUpper().StartsWith("#START"))
                {
                    StarCalcuteTestNo = true;
                    TestStartRow = iRow + 1;        //start counting after "#START" row

                    for (int iCol = 0; iCol < TestInput.GetUpperBound(1); iCol++)
                    {
                        if (TestInput[iRow, iCol].ToUpper().StartsWith("#END"))
                        {
                            break;
                        }
                        if (TestInput[iRow, iCol].Trim() == "")
                        {
                            intTotaltColumnNo++;
                            continue;
                        }
                        if (TestInput[iRow, iCol].Trim() != "")
                        {
                            intTotalParamNo++;
                            intTotaltColumnNo++;
                        }
                    }
                }

                intTotalRowNo++;
            }
            #endregion

            #region Test Dictionary Generation
            DicTest = new Dictionary<string, string>[intTotalTestNo];

            for (int iRow = TestStartRow; iRow < intTotalRowNo; iRow++)
            {
                string excludeTest = "";
                excludeTest = TestInput[iRow, 0].ToUpper();     //find "X" in 1st column

                if (excludeTest != "X")
                {
                    DicTest[intTestCount] = new Dictionary<string, string>();

                    for (int iCol = 1; iCol < intTotaltColumnNo; iCol++)
                    {
                        string currentTestCondName = "";
                        string currentTestCondValue = "";

                        currentTestCondName = TestInput[TestStartRow - 1, iCol].ToUpper();
                        currentTestCondValue = TestInput[iRow, iCol].ToUpper();

                        if (currentTestCondName.Trim() != "")
                        {
                            if (currentTestCondValue == "")
                            {
                                currentTestCondValue = "0";
                            }

                            DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            //DicTest[intTestCount].Add("1", "1");
                            //DicTest[intTestCount].Add("22", "22");
                        }
                    }

                    intTestCount++;
                    if (intTestCount == intTotalTestNo)     //break out from for loop
                    {
                        break;
                    }

                }
            }
            #endregion

            #region Parameter Label Generation
            DicLabel = new Dictionary<string, string>();

            for (int iCol = 1; iCol < intTotaltColumnNo; iCol++)
            {
                string currentLabelCondName = "";
                string currentLabelCondValue = "";

                currentLabelCondName = TestInput[TestStartRow - 1, iCol].ToUpper();
                currentLabelCondValue = TestInput[TestLabelRow, iCol].Trim();

                if (currentLabelCondName.Trim() != "")
                {
                    if (currentLabelCondValue == "")
                        currentLabelCondValue = "NA";

                    DicLabel.Add(currentLabelCondName, currentLabelCondValue);
                }
            }
            #endregion
        }

        public void ReadWaveformFilePath(int SheetNo, int WaveFormColumnNo, ref Dictionary<string, string> DicWaveForm)
        {
            int CurrentRow = 2;
            DicWaveForm = new Dictionary<string, string>();
            string Waveform, WaveformFilePath;
            while (true)
            {
                try
                {
                    Waveform = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, CurrentRow, WaveFormColumnNo).Trim().ToUpper();
                }
                catch (Exception)
                {

                    Waveform = "";
                }

                try
                {
                    WaveformFilePath = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, CurrentRow, WaveFormColumnNo + 1).Trim().ToUpper();
                }
                catch (Exception)
                {

                    WaveformFilePath = "";
                }

                if (Waveform.ToUpper() == ConstExEndTxt)
                    break;
                else
                {
                    DicWaveForm.Add(Waveform, WaveformFilePath);
                    CurrentRow++;
                }
            }
        }
        public void ReadWaveformFilePath(int SheetNo, int WaveFormColumnNo, ref Dictionary<string, string> DicWaveForm, ref Dictionary<string, string> DicWaveFormMutate)
        {
            int CurrentRow = 2;
            DicWaveForm = new Dictionary<string, string>();
            DicWaveFormMutate = new Dictionary<string, string>();
            string Waveform, WaveformFilePath, WaveformMutateCond;

            while (true)
            {
                try
                {
                    Waveform = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, CurrentRow, WaveFormColumnNo).Trim().ToUpper();
                }
                catch (Exception)
                {
                    Waveform = "";
                }

                try
                {
                    WaveformFilePath = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, CurrentRow, WaveFormColumnNo + 1).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformFilePath = "";
                }

                try
                {
                    WaveformMutateCond = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, CurrentRow, WaveFormColumnNo + 2).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformMutateCond = "";
                }

                if (Waveform.ToUpper() == ConstExEndTxt)
                    break;
                else
                {
                    DicWaveForm.Add(Waveform, WaveformFilePath);
                    DicWaveFormMutate.Add(Waveform, WaveformMutateCond);
                    CurrentRow++;
                }
            }
        }
        public void ReadWaveformFilePath(string SheetName, int WaveFormColumnNo, ref Dictionary<string, string> DicWaveForm, ref Dictionary<string, string> DicWaveFormMutate)
        {
            int CurrentRow = 2;
            DicWaveForm = new Dictionary<string, string>();
            DicWaveFormMutate = new Dictionary<string, string>();
            string Waveform, WaveformFilePath, WaveformMutateCond;

            while (true)
            {
                try
                {
                    Waveform = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo).Trim().ToUpper();
                }
                catch (Exception)
                {
                    Waveform = "";
                }

                try
                {
                    WaveformFilePath = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo + 1).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformFilePath = "";
                }

                try
                {
                    WaveformMutateCond = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo + 2).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformMutateCond = "";
                }

                if (Waveform.ToUpper() == ConstExEndTxt)
                    break;
                else
                {
                    DicWaveForm.Add(Waveform, WaveformFilePath);
                    DicWaveFormMutate.Add(Waveform, WaveformMutateCond);
                    CurrentRow++;
                }
            }
        }
        public void ReadWaveformFilePath(string SheetName, int WaveFormColumnNo, ref Dictionary<string, string> DicWaveForm, ref Dictionary<string, string> DicWaveFormMutate, ref Dictionary<string, string> DicWaveFormAlias)
        {
            int CurrentRow = 2;
            DicWaveForm = new Dictionary<string, string>();
            DicWaveFormMutate = new Dictionary<string, string>();
            DicWaveFormAlias = new Dictionary<string, string>();
            string Waveform, WaveformFilePath, WaveformMutateCond, WaveformAlias;

            while (true)
            {
                try
                {
                    Waveform = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo).Trim().ToUpper();
                }
                catch (Exception)
                {
                    Waveform = "";
                }

                try
                {
                    WaveformFilePath = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo + 1).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformFilePath = "";
                }

                try
                {
                    WaveformMutateCond = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo + 2).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformMutateCond = "";
                }

                try
                {
                    WaveformAlias = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, CurrentRow, WaveFormColumnNo + 3).Trim().ToUpper();
                }
                catch (Exception)
                {
                    WaveformAlias = "";
                }

                if (Waveform.ToUpper() == ConstExEndTxt)
                    break;
                else
                {
                    DicWaveForm.Add(Waveform, WaveformFilePath);
                    DicWaveFormMutate.Add(Waveform, WaveformMutateCond);
                    DicWaveFormAlias.Add(Waveform, WaveformAlias);
                    CurrentRow++;
                }
            }
        }

        public void ReadMipiReg(int SheetNo, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 2, intTotalTestNo = 0;
            int TestStartRow = 1, intTotaltColumnNo = 0;
            int intTestCount = 0;
            int TestLabelRow = 0;
            bool StarCalcuteTestNo = true;

            #region Calculate Test Parameter
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }

                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                else if (strExInput.ToUpper() == ConstExLabelTxt)
                {
                    TestLabelRow = intRow;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (true)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 1; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion
        }
        public void ReadMipiReg(string SheetName, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 2, intTotalTestNo = 0;
            int TestStartRow = 1, intTotaltColumnNo = 0;
            int intTestCount = 0;
            int TestLabelRow = 0;
            bool StarCalcuteTestNo = true;

            #region Calculate Test Parameter
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, IndexColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }

                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                else if (strExInput.ToUpper() == ConstExLabelTxt)
                {
                    TestLabelRow = intRow;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (true)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (true)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 1; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion
        }

        public void ReadPwrBlast(int SheetNo, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 2, intTotalTestNo = 0;
            int TestStartRow = 1, intTotaltColumnNo = 0;
            int intTestCount = 0;
            int TestLabelRow = 0;
            bool StarCalcuteTestNo = true;
            bool b_chkExcel = true;
            bool b_1stRow = true;

            #region Calculate Test Parameter
            while (b_chkExcel)
            {
                try
                {
                    //Check 1st row and 1st  column against PWRBLAST spreadsheet , if non-exist -> will skip this spreadsheet
                    //assumption that -> if no spreadsheet , no PXI_RAMP_POWERBLAST test method required 
                    //else clotho will be in infinite loop if spreadsheet non exist
                    if (b_1stRow)
                    {
                        string strChkExcel = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, 1, IndexColumnNo);
                        if (strChkExcel.ToUpper() != "TEST SELECTION")
                        {
                            b_chkExcel = false;
                        }
                        b_1stRow = false;
                    }

                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }

                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                else if (strExInput.ToUpper() == ConstExLabelTxt)
                {
                    TestLabelRow = intRow;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (b_chkExcel)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (b_chkExcel)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 1; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetNo, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion
        }
        public void ReadPwrBlast(string SheetName, int IndexColumnNo, int TestParaColumnNo, ref Dictionary<string, string>[] DicTest)
        {
            string strExInput = "";
            string strExTestItems = "";
            int intRow = 2, intTotalTestNo = 0;
            int TestStartRow = 1, intTotaltColumnNo = 0;
            int intTestCount = 0;
            int TestLabelRow = 0;
            bool StarCalcuteTestNo = true;
            bool b_chkExcel = true;
            bool b_1stRow = true;

            #region Calculate Test Parameter
            while (b_chkExcel)
            {
                try
                {
                    //Check 1st row and 1st  column against PWRBLAST spreadsheet , if non-exist -> will skip this spreadsheet
                    //assumption that -> if no spreadsheet , no PXI_RAMP_POWERBLAST test method required 
                    //else clotho will be in infinite loop if spreadsheet non exist
                    if (b_1stRow)
                    {
                        string strChkExcel = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, 1, IndexColumnNo);
                        if (strChkExcel.ToUpper() != "TEST SELECTION")
                        {
                            b_chkExcel = false;
                        }
                        b_1stRow = false;
                    }

                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, IndexColumnNo);
                }
                catch (Exception)
                {
                    if (b_1stRow)
                    {
                        b_chkExcel = false;
                    }
                    else
                    {
                        //meant is blank space - need to force it because of clotho ver 2.2.3 above
                        strExInput = "";
                    }
                }

                if (strExInput.ToUpper() == ConstExEndTxt)
                {
                    break;
                }
                else if (StarCalcuteTestNo && (strExInput.Trim().ToUpper() != ConstExSkipTxt))
                {
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, TestParaColumnNo);
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }

                    if (strExTestItems.Trim() != "")
                    {
                        intTotalTestNo++;
                    }
                }
                else if (strExInput.ToUpper() == ConstExStartTxt)
                {
                    TestStartRow = intRow;
                    StarCalcuteTestNo = true;
                }
                else if (strExInput.ToUpper() == ConstExLabelTxt)
                {
                    TestLabelRow = intRow;
                }
                intRow++;
            }
            #endregion

            #region Calculate Excel Column
            while (b_chkExcel)
            {
                try
                {
                    strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, TestStartRow, intTotaltColumnNo);
                }
                catch (Exception)
                {
                    //meant is blank space - need to force it because of clotho ver 2.2.3 above
                    strExInput = "";
                }


                if (strExInput.Trim().ToUpper() == ConstExEndTxt)
                {
                    intTotaltColumnNo--;
                    break;
                }
                else
                    intTotaltColumnNo++;
            }
            #endregion

            #region Test Dictionary Generation
            try
            {
                intRow = TestStartRow + 1;
                DicTest = new Dictionary<string, string>[intTotalTestNo];

                while (b_chkExcel)
                {
                    try
                    {
                        strExInput = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, IndexColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExInput = "";
                    }
                    try
                    {
                        strExTestItems = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, TestParaColumnNo).ToUpper();
                    }
                    catch (Exception)
                    {

                        strExTestItems = "";
                    }


                    if (strExInput.ToUpper() == ConstExEndTxt)
                        break;
                    else if (strExInput.Trim().ToUpper() == ConstExSkipTxt)
                    {
                        intRow++;
                        continue;
                    }
                    else
                    {
                        if (strExTestItems.Trim() != "")
                        {
                            DicTest[intTestCount] = new Dictionary<string, string>();

                            string currentTestCondName = "";
                            string currentTestCondValue = "";

                            for (int i = 1; i <= intTotaltColumnNo; i++)
                            {
                                try
                                {
                                    currentTestCondName = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, TestStartRow, i).ToUpper();
                                }
                                catch (Exception)
                                {

                                    currentTestCondName = "";
                                }

                                try
                                {
                                    currentTestCondValue = ATFCrossDomainWrapper.Excel_Get_Input(SheetName, intRow, i);
                                }
                                catch (Exception)
                                {

                                    currentTestCondValue = "";
                                }

                                if (currentTestCondValue == "")
                                    currentTestCondValue = "0";
                                if (currentTestCondName.Trim() != "")
                                    DicTest[intTestCount].Add(currentTestCondName, currentTestCondValue);
                            }
                            intTestCount++;
                        }
                    }
                    intRow++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            #endregion
        }

        public string ReadTcfData(Dictionary<string, string> TestPara, string strHeader)
        {
            string Temp = "";

            TestPara.TryGetValue(strHeader.ToUpper(), out Temp);
            return (Temp != null ? Temp : "");

        }

        public string ReadTextFile(Dictionary<string, Dictionary<string, string>> DicVariable, string dirpath, string groupName, string targetName)
        {
            string tempSingleString;
            try
            {
                string tempSingleString2 = takestringfromDic(DicVariable, groupName, targetName);

                if (tempSingleString2 != null)
                {
                    return tempSingleString2;
                }

                if (!File.Exists(@dirpath))
                {
                    throw new FileNotFoundException("{0} does not exist."
                        , @dirpath);
                }
                else
                {
                    using (StreamReader reader = File.OpenText(@dirpath))
                    {
                        string line = "";
                        string[] templine;
                        tempSingleString = "";

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "[" + groupName + "]")
                            {
                                char[] temp = { };
                                line = reader.ReadLine();
                                while (line != null && line != "")
                                {
                                    templine = line.ToString().Split(new char[] { '=' });
                                    temp = line.ToCharArray();
                                    if (temp[0] == '[' && temp[temp.Length - 1] == ']')
                                        break;
                                    if (templine[0].TrimEnd() == targetName)
                                    {
                                        tempSingleString = templine[templine.Length - 1].ToString().TrimStart();
                                        break;
                                    }
                                    line = reader.ReadLine();
                                }
                                break;
                            }
                        }

                        reader.Close();
                    }
                }

#if (debug)
                if(tempSingleString2 != tempSingleString)
                {

                }
#endif
                if (tempSingleString == "") MessageBox.Show(string.Format("There is no {0}.", targetName));

                return tempSingleString;
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(dirpath + " " + groupName + " " +
                    targetName + " Cannot Read from the file!");
            }
        }

        public ArrayList ReadCalProcedure(string dirpath)
        {
            ArrayList tempString = new ArrayList();
            try
            {
                if (!File.Exists(@dirpath))
                {
                    throw new FileNotFoundException("{0} does not exist."
                        , @dirpath);
                }
                else
                {
                    using (StreamReader reader = File.OpenText(@dirpath))
                    {
                        string line = "";
                        tempString.Clear();

                        while ((line = reader.ReadLine()) != null)
                        {
                            tempString.Add(line.ToString());
                        }
                        reader.Close();
                    }
                }
                return tempString;
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(dirpath + " Cannot Read from the file!");
            }
        }

        public void LoadCalFreqList(string strCalFreqList, ref string[] arrCalFreqList, ref int varNumOfCalFreqList)
        {
            // Loading the calibration freq list
            string tempStr;
            FileInfo fCalFreqList = new FileInfo(strCalFreqList);
            StreamReader srCalFreqList = new StreamReader(fCalFreqList.ToString());

            varNumOfCalFreqList = 0;
            while ((tempStr = srCalFreqList.ReadLine()) != null)
            {
                arrCalFreqList[varNumOfCalFreqList] = tempStr.Trim();    //tempStr.Trim();
                varNumOfCalFreqList++;
            }
            srCalFreqList.Close();
        }
        public void LoadCalFreqListandGenerateDic(string Filepath, string CalTag, ref Dictionary<string, Dictionary<double, double>> DicCalList)
        {
            FileInfo fFilePath = new FileInfo(Filepath);
            StreamReader srFilePath = new StreamReader(fFilePath.ToString());
            DicCalList = new Dictionary<string, Dictionary<double, double>>();
            Dictionary<double, double> DicLocalCalInfo = new Dictionary<double, double>();
            string tempStr, strCalFreqTag = "";
            List<double> CalFreqList = new List<double>();
            bool bAddDataToDic = false;

            while ((tempStr = srFilePath.ReadLine()) != null)
            {
                tempStr.Trim();

                string[] value = tempStr.Split(',');
                var CSVarray = Array.FindAll(value, s => !s.Equals(""));

                // Find Cal Freq Tag
                if (CSVarray.Length > 0)
                {
                    Regex regex = new Regex("[a-zA-Z]");
                    if (regex.IsMatch(CSVarray[0]) && (!CSVarray[0].Contains(CalTag)))
                    {
                        strCalFreqTag = CSVarray[0];

                        // Copy Freq List Data to List except Cal Freq Tag
                        for (int i = 1; i < CSVarray.Length; i++)
                        {
                            CalFreqList.Add(Convert.ToDouble(CSVarray[i]));
                        }

                        bAddDataToDic = true;
                        continue;
                    }
                }

                if (bAddDataToDic)
                {
                    if (CalFreqList.Count == CSVarray.Length)
                    {
                        for (int i = 0; i < CalFreqList.Count; i++)
                        {
                            DicLocalCalInfo.Add(CalFreqList[i], Convert.ToDouble(CSVarray[i]));
                        }
                        DicCalList.Add(strCalFreqTag, new Dictionary<double, double>(DicLocalCalInfo));
                    }

                    // Clear pre-data
                    DicLocalCalInfo.Clear();
                    CalFreqList.Clear();
                    bAddDataToDic = false;
                }

            }
            srFilePath.Close();
        }
        public void LoadMeasEquipCalFactor(string strMeasEquipCalFactor, ref bool varCalDataAvailableMeas)
        {
            // Loading the calibration data for the measurement equipment
            if (strMeasEquipCalFactor.ToUpper().Trim() == "NONE")
                varCalDataAvailableMeas = false;
            else
                varCalDataAvailableMeas = true;
        }
        private void Assign_Cal_File_Combined(string _strTargetCalDataFile, ref StreamWriter swCalDataFile)
        {
            // Checking and creating a new calibration data file
            FileInfo fCalDataFile = new FileInfo(_strTargetCalDataFile);
            swCalDataFile = fCalDataFile.AppendText();

        }
        public void LoadSourceData(string _strTargetCalDataFile, string strSourceEquipCalFactor, string[] arrFreqList, ref string[] arrCalDataSource, ref bool varCalDataAvailableSource, ref StreamWriter swCalDataFile)
        {
            string errInformation = "";
            float cal_factor = 0f;
            int varNumOfCalFreqList = 0;

            // Loading the calibration data for the source equipment
            if (strSourceEquipCalFactor.ToUpper().Trim() == "NONE")
                varCalDataAvailableSource = false;
            else
            {
                varCalDataAvailableSource = true;
                varNumOfCalFreqList = 0;
                try
                {
                    swCalDataFile.Close();
                }
                catch { }

                ATFCrossDomainWrapper.Cal_LoadCalData("CalData1D_", _strTargetCalDataFile);

                try
                {
                    Assign_Cal_File_Combined(_strTargetCalDataFile, ref swCalDataFile);
                }
                catch { }

                ATFCrossDomainWrapper.Cal_GetCalData1DCombined("CalData1D_", strSourceEquipCalFactor, Convert.ToSingle(arrFreqList[varNumOfCalFreqList]), ref cal_factor, ref errInformation);
                while (arrFreqList[varNumOfCalFreqList] != null)
                {
                    ATFCrossDomainWrapper.Cal_GetCalData1DCombined("CalData1D_", strSourceEquipCalFactor, Convert.ToSingle(arrFreqList[varNumOfCalFreqList]), ref cal_factor, ref errInformation);
                    arrCalDataSource[varNumOfCalFreqList] = cal_factor.ToString(); ;
                    varNumOfCalFreqList++;
                }
                try
                {
                    ATFCrossDomainWrapper.Cal_ResetAll();
                }
                catch { }

            }
        }

        public bool GenerateDicLocalFile(string dirpath, ref Dictionary<string, Dictionary<string, string>> Dic)
        {
            try
            {
                if (!File.Exists(@dirpath))
                {
                    throw new FileNotFoundException("{0} does not exist."
                        , @dirpath);
                }
                else
                {
                    using (StreamReader reader = File.OpenText(@dirpath))
                    {
                        string line = "";
                        string[] templine;
                        bool IsKeepRead = true;
                        while (IsKeepRead)
                        {

                            if (line.Contains("["))
                            {
                                Dictionary<string, string> DicLocalfileParamName = new Dictionary<string, string>();
                                string GroupName = line.Trim('[', ']');

                                char[] temp = { };
                                line = reader.ReadLine();
                                while (line != null && line != "")
                                {
                                    //DicLocalfileParamName.Add("","");


                                    templine = line.ToString().Split(new char[] { '=' });
                                    temp = line.ToCharArray();
                                    if (temp[0] == '[' && temp[temp.Length - 1] == ']')
                                        break;
                                    if (temp[0] == '\'')
                                    {
                                        line = reader.ReadLine();
                                        continue;
                                    }

                                    if (templine.Length == 2)
                                    {


                                        string v1 = templine[0].ToString().Trim(' ');
                                        string v2 = templine[1].ToString().TrimStart();
                                        try
                                        {
                                            DicLocalfileParamName.Add(v1, v2);
                                            line = reader.ReadLine();
                                        }
                                        catch
                                        {
                                            line = reader.ReadLine();
                                        }

                                        //DicLocalfile
                                        continue;
                                    }
                                    line = reader.ReadLine();
                                }

                                Dic.Add(GroupName, new Dictionary<string, string>(DicLocalfileParamName));

                                continue;
                            }

                            if ((line = reader.ReadLine()) == null)
                            {
                                IsKeepRead = false;
                            }
                        }

                        reader.Close();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(dirpath + " Cannot Read from the file!");
            }

            return false;
        }
        public string takestringfromDic(Dictionary<string, Dictionary<string, string>> DicVariable, string groupName, string targetName)
        {
            string returnString = null;
            if (DicVariable.ContainsKey(groupName))
            {
                var chekc = DicVariable[groupName].Keys;
                if (DicVariable[groupName].ContainsKey(targetName))
                    returnString = DicVariable[groupName][targetName];
            }
            return returnString;
        }


        public static string GetTestPlanPath()
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

    }
}
