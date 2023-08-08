using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using Avago.ATF.Logger;
using Avago.ATF.LogService;

namespace MyProduct
{
    public struct S_MXA_Setting
    {
        public double RBW;
        public double VBW;
        public double Span;
        public double Attenuation;
        public double RefLevel;
        public int NoPoints;
        public int SweepT;
    }

    public struct S_CalSegm_Setting
    {
        public string TXCalPath;
        public string TXCalSegm;
        public string RX1CalPath;
        public string RX1CalSegm;
        public string RX2CalPath;
        public string RX2CalSegm;
        public string ANTCalPath;
        public string ANTCalSegm;
        public string TBCalPath;
        public string TBCalSegm;
        public string In_TBCalPath;
        public string In_TBCalSegm;
        public string Out_TBCalPath;
        public string Out_TBCalSegm;
    }

    public class MyUtility
    {
        public Dictionary<string, Dictionary<string, string>> DicLocalfile = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, Dictionary<double, double>> DicLocalCableLossFile, DicLocalBoardLossFile = new Dictionary<string, Dictionary<double, double>>();

        public S_MXA_Setting MXA_Setting;
        public S_CalSegm_Setting CalSegm_Setting;

        const string ConstExStartTxt = "#START";
        const string ConstExEndTxt = "#END";
        const string ConstExSkipTxt = "X";
        const string ConstExLabelTxt = "#LABEL";

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

        public void ReadWaveformFilePath (int SheetNo, int WaveFormColumnNo, ref Dictionary<string, string> DicWaveForm)
        {
            int CurrentRow = 2;
            DicWaveForm = new Dictionary<string, string>();
            string Waveform, WaveformFilePath;
            while(true)
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

        public string ReadTextFile(string dirpath, string groupName, string targetName)
        {
            string tempSingleString;
            try
            {
                string tempSingleString2 = takestringfromDic(groupName, targetName);

                if(tempSingleString2 != null )
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

        public void CalFileGeneration(string strTargetCalDataFile)
        {
            // Checking and creating a new calibration data file
            DateTime d1 = DateTime.Now;
            StreamWriter swCalDataFile;
            string tempTime = d1.ToString();
            FileInfo fCalDataFile = new FileInfo(strTargetCalDataFile);

            string fileName = string.Empty;
            string destFile = string.Empty;

            if (fCalDataFile.Exists)
            {
                DialogResult result = MessageBox.Show("The Cal file, " + strTargetCalDataFile + ", exists. Do you want to replace it?", "Debug Mode", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    String strTargetCalDataFile_Backup = @"C:\Avago.ATF.Common\Input\Calibration_Backup\" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + @"\";

                    if (!Directory.Exists(strTargetCalDataFile_Backup))
                    {
                        Directory.CreateDirectory(strTargetCalDataFile_Backup);
                    }

                    fileName = System.IO.Path.GetFileName(strTargetCalDataFile);
                    destFile = System.IO.Path.Combine(strTargetCalDataFile_Backup, fileName);
                    System.IO.File.Copy(strTargetCalDataFile, destFile);

                    fCalDataFile.Delete();
                    fCalDataFile = new FileInfo(strTargetCalDataFile);
                    swCalDataFile = fCalDataFile.CreateText();
                    swCalDataFile.WriteLine("1D-Combined" + "," + tempTime);
                    swCalDataFile.Close();
                }
            }
            else
            {
                fCalDataFile = new FileInfo(strTargetCalDataFile);
                swCalDataFile = fCalDataFile.CreateText();
                swCalDataFile.WriteLine("1D-Combined" + "," + tempTime);
                swCalDataFile.Close();
            }
            fCalDataFile = null;

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

        public void Decode_MXA_Setting(string MXA_Data)
        {
            try
            {
                if (MXA_Data == "") { throw new ArgumentNullException(); }

                string[] Tempdata1;
                string[] TempData2;

                MXA_Setting = new S_MXA_Setting();

                Tempdata1 = MXA_Data.Split(';');

                for (int i = 0; i < 7; i++)
                {
                    TempData2 = Tempdata1[i].Split('@');

                    switch (TempData2[0].ToUpper())
                    {
                        case "RBW":
                            MXA_Setting.RBW = Convert.ToDouble(TempData2[1]);
                            break;
                        case "VBW":
                            MXA_Setting.VBW = Convert.ToDouble(TempData2[1]);
                            break;
                        case "SPAN":
                            MXA_Setting.Span = Convert.ToDouble(TempData2[1]);
                            break;
                        case "ATTN":
                            MXA_Setting.Attenuation = Convert.ToDouble(TempData2[1]);
                            break;
                        case "REFLVL":
                            MXA_Setting.RefLevel = Convert.ToDouble(TempData2[1]);
                            break;
                        case "NOPOINTS":
                            MXA_Setting.NoPoints = Convert.ToInt32(TempData2[1]);
                            break;
                        case "SWEEPT":
                            MXA_Setting.SweepT = Convert.ToInt16(TempData2[1]);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void Decode_CalSegm_Setting(string CalSegm_Data)
        {
            try
            {
                if (CalSegm_Data == "") { throw new ArgumentNullException(); }

                string[] Tempdata1;
                string[] TempData2;

                CalSegm_Setting = new S_CalSegm_Setting();

                Tempdata1 = CalSegm_Data.Split(';');

                for (int i = 0; i < Tempdata1.Length; i++)
                {
                    TempData2 = Tempdata1[i].Split('@');

                    switch (TempData2[0].ToUpper())
                    {
                        case "TX":
                            CalSegm_Setting.TXCalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.TXCalSegm = TempData2[1].ToUpper();
                            break;
                        case "MXA1":
                            CalSegm_Setting.RX1CalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.RX1CalSegm = TempData2[1].ToUpper();
                            break;
                        case "MXA2":
                            CalSegm_Setting.RX2CalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.RX2CalSegm = TempData2[1].ToUpper();
                            break;
                        case "ANT":
                            CalSegm_Setting.ANTCalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.ANTCalSegm = TempData2[1].ToUpper();
                            break;
                        case "BOARD":
                            CalSegm_Setting.TBCalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.TBCalSegm = TempData2[1].ToUpper();
                            break;
                        case "LOSS_IN":
                            CalSegm_Setting.In_TBCalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.In_TBCalSegm = TempData2[1].ToUpper();
                            break;
                        case "LOSS_OUT":
                            CalSegm_Setting.Out_TBCalPath = TempData2[0].ToUpper();
                            CalSegm_Setting.Out_TBCalSegm = TempData2[1].ToUpper();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public S_MXA_Setting Decode_MXA_Setting_ReturnValue(string MXA_Data)
        {
            try
            {
                if (MXA_Data == "") { throw new ArgumentNullException(); }

                string[] Tempdata1;
                string[] TempData2;

                MXA_Setting = new S_MXA_Setting();

                Tempdata1 = MXA_Data.Split(';');

                for (int i = 0; i < 7; i++)
                {
                    TempData2 = Tempdata1[i].Split('@');

                    switch (TempData2[0].ToUpper())
                    {
                        case "RBW":
                            MXA_Setting.RBW = Convert.ToDouble(TempData2[1]);
                            break;
                        case "VBW":
                            MXA_Setting.VBW = Convert.ToDouble(TempData2[1]);
                            break;
                        case "SPAN":
                            MXA_Setting.Span = Convert.ToDouble(TempData2[1]);
                            break;
                        case "ATTN":
                            MXA_Setting.Attenuation = Convert.ToDouble(TempData2[1]);
                            break;
                        case "REFLVL":
                            MXA_Setting.RefLevel = Convert.ToDouble(TempData2[1]);
                            break;
                        case "NOPOINTS":
                            MXA_Setting.NoPoints = Convert.ToInt32(TempData2[1]);
                            break;
                        case "SWEEPT":
                            MXA_Setting.SweepT = Convert.ToInt16(TempData2[1]);
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                return MXA_Setting;
                throw ex;
            }
            return MXA_Setting;
        }

        public static Int64 ToDecimal(string bin)
        // Converts Binary string to Decimal integer
        {
            long l = Convert.ToInt64(bin, 2);
            Int64 i = (Int64)l;
            return i;
        }

        public static string ToBinary(Int64 Decimal)
        {
            // Declare a few variables we're going to need
            Int64 BinaryHolder;
            char[] BinaryArray;
            string BinaryResult = "";

            while (Decimal > 0)
            {
                BinaryHolder = Decimal % 2;
                BinaryResult += BinaryHolder;
                Decimal = Decimal / 2;
            }

            // The algoritm gives us the binary number in reverse order (mirrored)
            // We store it in an array so that we can reverse it back to normal
            BinaryArray = BinaryResult.ToCharArray();
            Array.Reverse(BinaryArray);
            BinaryResult = new string(BinaryArray);

            return BinaryResult;
        }

        public bool GenerateDicLocalFile(string dirpath)
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

                                DicLocalfile.Add(GroupName, new Dictionary<string, string>(DicLocalfileParamName));

                                continue;
                            }

                            if((line = reader.ReadLine()) == null)
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
        public string takestringfromDic(string groupName, string targetName)
        {
            string returnString = null;
            if (DicLocalfile.ContainsKey(groupName))
            {
                var chekc = DicLocalfile[groupName].Keys;
                if (DicLocalfile[groupName].ContainsKey(targetName))
                    returnString = DicLocalfile[groupName][targetName];                
            }
            return returnString;
        }
    }

}
