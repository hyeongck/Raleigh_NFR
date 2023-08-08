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
        public void Capture_MXA1_Trace(int traceNo, int testNum, string testParam, string rxBand, bool saveData)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            string resultHeader = testParam + "_RX" + rxBand + "_FREQ";

            //Read MXA Trace and store to temp file
            double startFreqHz = Eq.Site[0]._EqSA01.READ_STARTFREQ();
            double stopFreqHz = Eq.Site[0]._EqSA01.READ_STOPFREQ();
            int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA01.READ_SWEEP_POINTS());
            stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

            //temp trace array storage use for MAX , MIN etc calculation 
            MXATrace[TestCount].Enable = true;
            MXATrace[TestCount].TestNumber = Convert.ToString(testNum);
            MXATrace[TestCount].Multi_Trace[0][0].NoPoints = sweepPts;
            MXATrace[TestCount].Multi_Trace[0][0].FreqMHz = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][0].Ampl = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][0].Result_Header = resultHeader;
            MXATrace[TestCount].Multi_Trace[0][0].MXA_No = "MXA1_Trace" + traceNo;

            float[] arrSaTraceData = new float[sweepPts];
            arrSaTraceData = Eq.Site[0]._EqSA01.IEEEBlock_READ_MXATrace(traceNo);

            tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

            for (istep = 0; istep < sweepPts; istep++)
            {
                if (istep > 0)
                {
                    tmpFreqHz = tmpFreqHz + stepFreqHz;
                }

                MXATrace[TestCount].Multi_Trace[0][0].FreqMHz[istep] = tmpFreqHz / 1e6;     //convert to MHz
                MXATrace[TestCount].Multi_Trace[0][0].Ampl[istep] = Math.Round(Convert.ToDouble(arrSaTraceData[istep]), 3);
            }

            if (saveData)
            {
                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA1 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(MXATrace[TestCount].Multi_Trace[0][0].FreqMHz[istep]);
                    templine[1] = Convert.ToString(Math.Round(MXATrace[TestCount].Multi_Trace[0][0].Ampl[istep], 3));
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + testParam + "_MXA1_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Capture_MXA1_Trace(int traceNo, int testNum, string testParam, string rxBand, double prev_Rslt, bool saveData, double mkrNoiseOffset = 0)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            string resultHeader = testParam + "_RX" + rxBand + "_FREQ";

            //Read MXA Trace and store to temp file
            double startFreqHz = Eq.Site[0]._EqSA01.READ_STARTFREQ();
            double stopFreqHz = Eq.Site[0]._EqSA01.READ_STOPFREQ();
            int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA01.READ_SWEEP_POINTS());
            stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

            //temp trace array storage use for MAX , MIN etc calculation 
            MXATrace[TestCount].Enable = true;
            MXATrace[TestCount].TestNumber = Convert.ToString(testNum);
            MXATrace[TestCount].Multi_Trace[0][0].NoPoints = sweepPts;
            MXATrace[TestCount].Multi_Trace[0][0].FreqMHz = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][0].Ampl = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][0].Result_Header = resultHeader;
            MXATrace[TestCount].Multi_Trace[0][0].MXA_No = "MXA1_Trace" + traceNo;

            float[] arrSaTraceData = new float[sweepPts];
            arrSaTraceData = Eq.Site[0]._EqSA01.IEEEBlock_READ_MXATrace(traceNo);

            tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

            for (istep = 0; istep < sweepPts; istep++)
            {
                if (istep > 0)
                {
                    tmpFreqHz = tmpFreqHz + stepFreqHz;
                }

                MXATrace[TestCount].Multi_Trace[0][0].FreqMHz[istep] = tmpFreqHz / 1e6;     //convert to MHz
                MXATrace[TestCount].Multi_Trace[0][0].Ampl[istep] = Math.Round(Convert.ToDouble(arrSaTraceData[istep]) - prev_Rslt - mkrNoiseOffset, 3);     //prev_Rslt - usually data from DUT with internal LNA gain, other should be 0
            }

            if (saveData)
            {
                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA1 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(MXATrace[TestCount].Multi_Trace[0][0].FreqMHz[istep]);
                    templine[1] = Convert.ToString(Math.Round(MXATrace[TestCount].Multi_Trace[0][0].Ampl[istep], 3));       //raw data only from MXA without any prev_Rslt(usually data from DUT with internal LNA gain) embedded
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + testParam + "_MXA1_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Read_MXA1_Trace(int traceNum, int testNum, out double freqMHz, out double ampl, string searchMethod, string testParam)
        {
            freqMHz = -999;
            ampl = -999;
            int noPoints = 0;
            int traceNo = 0;            //MXA1 array location

            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    for (int i = 0; i < MXATrace.Length; i++)
                    {
                        if (MXATrace[i].TestNumber == Convert.ToString(testNum))
                        {
                            noPoints = MXATrace[i].Multi_Trace[0][traceNo].NoPoints;

                            for (int j = 0; j < noPoints; j++)
                            {
                                if (j == 0)
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[0];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[0];
                                }
                                if (ampl < MXATrace[i].Multi_Trace[0][traceNo].Ampl[j])
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[j];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[j];
                                }
                            }
                        }
                    }
                    break;

                case "MIN":
                    for (int i = 0; i < MXATrace.Length; i++)
                    {
                        if (MXATrace[i].TestNumber == Convert.ToString(testNum))
                        {
                            for (int j = 0; j < MXATrace[i].Multi_Trace[0][traceNo].NoPoints; j++)
                            {
                                if (j == 0)
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[0];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[0];
                                }
                                if (ampl > MXATrace[i].Multi_Trace[0][traceNo].Ampl[j])
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[j];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[j];
                                }
                            }
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Test Parameter : " + testParam + "(" + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    ampl = -999;
                    freqMHz = -999;
                    break;
            }
        }
        public void Capture_MXA2_Trace(int traceNo, int testNum, string testParam, string rxBand, bool saveData)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            string resultHeader = testParam + "_RX" + rxBand + "_FREQ";

            //Read MXA Trace and store to temp file
            double startFreqHz = Eq.Site[0]._EqSA02.READ_STARTFREQ();
            double stopFreqHz = Eq.Site[0]._EqSA02.READ_STOPFREQ();
            int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA02.READ_SWEEP_POINTS());
            stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

            //temp trace array storage use for MAX , MIN etc calculation 
            MXATrace[TestCount].Enable = true;
            MXATrace[TestCount].TestNumber = Convert.ToString(testNum);
            MXATrace[TestCount].Multi_Trace[0][1].NoPoints = sweepPts;
            MXATrace[TestCount].Multi_Trace[0][1].FreqMHz = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][1].Ampl = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][1].Result_Header = resultHeader;
            MXATrace[TestCount].Multi_Trace[0][1].MXA_No = "MXA2_Trace" + traceNo;

            float[] arrSaTraceData = new float[sweepPts];
            arrSaTraceData = Eq.Site[0]._EqSA02.IEEEBlock_READ_MXATrace(traceNo);

            tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

            for (istep = 0; istep < sweepPts; istep++)
            {
                if (istep > 0)
                {
                    tmpFreqHz = tmpFreqHz + stepFreqHz;
                }

                MXATrace[TestCount].Multi_Trace[0][1].FreqMHz[istep] = tmpFreqHz / 1e6;     //convert to MHz
                MXATrace[TestCount].Multi_Trace[0][1].Ampl[istep] = Math.Round(Convert.ToDouble(arrSaTraceData[istep]), 3);

            }

            if (saveData)
            {
                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA2 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(MXATrace[TestCount].Multi_Trace[0][1].FreqMHz[istep]);
                    templine[1] = Convert.ToString(Math.Round(MXATrace[TestCount].Multi_Trace[0][1].Ampl[istep], 3));
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + testParam + "_MXA2_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Capture_MXA2_Trace(int traceNo, int testNum, string testParam, string rxBand, double prev_Rslt, bool saveData, double mkrNoiseOffset = 0)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            string resultHeader = testParam + "_RX" + rxBand + "_FREQ";

            //Read MXA Trace and store to temp file
            double startFreqHz = Eq.Site[0]._EqSA02.READ_STARTFREQ();
            double stopFreqHz = Eq.Site[0]._EqSA02.READ_STOPFREQ();
            int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA02.READ_SWEEP_POINTS());
            stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

            //temp trace array storage use for MAX , MIN etc calculation 
            MXATrace[TestCount].Enable = true;
            MXATrace[TestCount].TestNumber = Convert.ToString(testNum);
            MXATrace[TestCount].Multi_Trace[0][1].NoPoints = sweepPts;
            MXATrace[TestCount].Multi_Trace[0][1].FreqMHz = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][1].Ampl = new double[sweepPts];
            MXATrace[TestCount].Multi_Trace[0][1].Result_Header = resultHeader;
            MXATrace[TestCount].Multi_Trace[0][1].MXA_No = "MXA2_Trace" + traceNo;

            float[] arrSaTraceData = new float[sweepPts];
            arrSaTraceData = Eq.Site[0]._EqSA02.IEEEBlock_READ_MXATrace(traceNo);

            tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

            for (istep = 0; istep < sweepPts; istep++)
            {
                if (istep > 0)
                {
                    tmpFreqHz = tmpFreqHz + stepFreqHz;
                }

                MXATrace[TestCount].Multi_Trace[0][1].FreqMHz[istep] = tmpFreqHz / 1e6;     //convert to MHz
                MXATrace[TestCount].Multi_Trace[0][1].Ampl[istep] = Math.Round(Convert.ToDouble(arrSaTraceData[istep]) - prev_Rslt - mkrNoiseOffset, 3);     //prev_Rslt - usually data from DUT with internal LNA gain, other should be 0
            }

            if (saveData)
            {
                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA2 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(MXATrace[TestCount].Multi_Trace[0][1].FreqMHz[istep]);
                    templine[1] = Convert.ToString(Math.Round(MXATrace[TestCount].Multi_Trace[0][1].Ampl[istep], 3));       //raw data only from MXA without any prev_Rslt(usually data from DUT with internal LNA gain) embedded
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + testParam + "_MXA2_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Read_MXA2_Trace(int traceNum, int testNum, out double freqMHz, out double ampl, string searchMethod, string testParam)
        {
            freqMHz = -999;
            ampl = -999;
            int noPoints = 0;
            int traceNo = 1;            //MXA2 array location

            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    for (int i = 0; i < MXATrace.Length; i++)
                    {
                        if (MXATrace[i].TestNumber == Convert.ToString(testNum))
                        {
                            noPoints = MXATrace[i].Multi_Trace[0][traceNo].NoPoints;

                            for (int j = 0; j < noPoints; j++)
                            {
                                if (j == 0)
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[0];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[0];
                                }
                                if (ampl < MXATrace[i].Multi_Trace[0][traceNo].Ampl[j])
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[j];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[j];
                                }
                            }
                        }
                    }
                    break;

                case "MIN":
                    for (int i = 0; i < MXATrace.Length; i++)
                    {
                        if (MXATrace[i].TestNumber == Convert.ToString(testNum))
                        {
                            for (int j = 0; j < MXATrace[i].Multi_Trace[0][traceNo].NoPoints; j++)
                            {
                                if (j == 0)
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[0];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[0];
                                }
                                if (ampl > MXATrace[i].Multi_Trace[0][traceNo].Ampl[j])
                                {
                                    ampl = MXATrace[i].Multi_Trace[0][traceNo].Ampl[j];
                                    freqMHz = MXATrace[i].Multi_Trace[0][traceNo].FreqMHz[j];
                                }
                            }
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Test Parameter : " + testParam + "(" + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    ampl = -999;
                    freqMHz = -999;
                    break;
            }
        }
        public void Read_MXA_MultiTrace(int MXA_No, int traceNum, string testUsePrev, double startFreqMHz, double stopFreqMHz, double stepFreqMHz, string searchMethod, string testParam, out double calcDataFreq, out double calcData)
        {
            int noPtsUser = 0;
            int[] testNumber;
            string[] resultArray;

            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            noPtsUser = (int)((stopFreqMHz - startFreqMHz) / stepFreqMHz) + 1;
            resultArray = testUsePrev.Split(',');
            s_TraceNo[] sortedMultiTrace = new s_TraceNo[resultArray.Length];
            testNumber = new int[resultArray.Length];

            #region Initialize Array and User Define Freq
            //initialize array & sorted the selected freq points
            for (int i = 0; i < resultArray.Length; i++)
            {
                sortedMultiTrace[i].Ampl = new double[noPtsUser];
                sortedMultiTrace[i].FreqMHz = new double[noPtsUser];

                testNumber[i] = Convert.ToInt16(resultArray[i]);        //example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6

                tmpFreqHz = startFreqMHz * 1e6;          //initialize 1st data to startFreq
                stepFreqHz = stepFreqMHz * 1e6;
                for (istep = 0; istep < noPtsUser; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }
                    sortedMultiTrace[i].FreqMHz[istep] = tmpFreqHz / 1e6;     //convert back to MHz
                    sortedMultiTrace[i].Ampl[istep] = -999999;              //initialize to default
                }
            }
            #endregion

            #region SORTED DATA POINT
            //sort the respective trace data to temp array location
            //really complex sorting , need to sort user define test freq (lower count) to actual of MXA trace test point (higher count) 
            //Example : 65 test freq and compared with 601 points of MXA trace (note:  both user define start & stop freq  must be in range of MXA trace start & stop freq)
            for (int i = 0; i < testNumber.Length; i++)     // "use previous" test number loop -> example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6
            {
                for (int count = 0; count < MXATrace.Length; count++)      //all MXA trace search loop
                {
                    if (MXATrace[count].TestNumber == Convert.ToString(testNumber[i]))        //select correct trace base on "use previous" test number
                    {
                        if (MXA_No == 1)    //select the correct trace either MXA#1 or MXA#2
                        {
                            for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
                            {
                                for (int j = 0; j < MXATrace[count].Multi_Trace[0][0].NoPoints; j++)
                                {
                                    if (sortedMultiTrace[i].FreqMHz[istep] == MXATrace[count].Multi_Trace[0][0].FreqMHz[j])        //find same freq and store amplitude to temp array
                                    {
                                        sortedMultiTrace[i].Ampl[istep] = MXATrace[count].Multi_Trace[0][0].Ampl[j];
                                    }
                                }
                            }
                        }
                        if (MXA_No == 2)    //select the correct trace either MXA#1 or MXA#2
                        {
                            for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
                            {
                                for (int j = 0; j < MXATrace[count].Multi_Trace[0][1].NoPoints; j++)
                                {
                                    if (sortedMultiTrace[i].FreqMHz[istep] == MXATrace[count].Multi_Trace[0][1].FreqMHz[j])        //find same freq and store amplitude to temp array
                                    {
                                        sortedMultiTrace[i].Ampl[istep] = MXATrace[count].Multi_Trace[0][1].Ampl[j];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Calculate Result
            //Calculate the result from the sorted data
            Result_MXATrace = new s_TraceNo();
            Result_MXATrace.Ampl = new double[noPtsUser];
            Result_MXATrace.FreqMHz = new double[noPtsUser];

            s_TraceNo resultMultiTrace = new s_TraceNo();
            resultMultiTrace.Ampl = new double[noPtsUser];
            resultMultiTrace.FreqMHz = new double[noPtsUser];

            calcData = -999;
            calcDataFreq = -999;
            Result_MXATrace.MXA_No = Convert.ToString(MXA_No);
            Result_MXATrace.NoPoints = noPtsUser;
            Result_MXATrace.Result_Header = testParam;

            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    for (istep = 0; istep < noPtsUser; istep++)     //get MAX data for every noPtsUser out of multitrace (from "use previous" setting)
                    {
                        for (int i = 0; i < sortedMultiTrace.Length; i++)
                        {
                            if (i == 0)
                            {
                                calcData = sortedMultiTrace[0].Ampl[istep];
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                            }
                            if (calcData < sortedMultiTrace[i].Ampl[istep])
                            {
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                                calcData = sortedMultiTrace[i].Ampl[istep];
                            }
                        }
                    }
                    for (istep = 0; istep < noPtsUser; istep++) //get MAX from the MAX of the multitrace
                    {
                        if (istep == 0)
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                        if (calcData < resultMultiTrace.Ampl[istep])
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                    }
                    break;

                case "MIN":
                    for (istep = 0; istep < noPtsUser; istep++)     //get MIN data for every noPtsUser out of multitrace (from "use previous" setting)
                    {
                        for (int i = 0; i < sortedMultiTrace.Length; i++)
                        {
                            if (i == 0)
                            {
                                calcData = sortedMultiTrace[0].Ampl[istep];
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                            }
                            if (calcData > sortedMultiTrace[i].Ampl[istep])
                            {
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                                calcData = sortedMultiTrace[i].Ampl[istep];
                            }
                        }
                    }
                    for (istep = 0; istep < noPtsUser; istep++) //get MIN from the MIN of the multitrace
                    {
                        if (istep == 0)
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                        if (calcData > resultMultiTrace.Ampl[istep])
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Test Parameter : " + testParam + "(" + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    break;
            }
            #endregion

            Result_MXATrace = resultMultiTrace;
        }
        public void Save_MXA1Trace(int traceNo, string TestParaName, bool saveData)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            if (saveData)
            {
                //Read MXA Trace and store to temp file
                double startFreqHz = Eq.Site[0]._EqSA01.READ_STARTFREQ();
                double stopFreqHz = Eq.Site[0]._EqSA01.READ_STOPFREQ();
                int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA01.READ_SWEEP_POINTS());
                stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

                double[] freqArray = new double[sweepPts];
                double[] amplitudeArray = new double[sweepPts];
                string[] sort_trace = new string[sweepPts];

                string tmpMxadata = Eq.Site[0]._EqSA01.READ_MXATrace(traceNo);
                sort_trace = tmpMxadata.Split(',');

                tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

                for (istep = 0; istep < sweepPts; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }

                    freqArray[istep] = tmpFreqHz / 1e6;       //convert to MHz
                    amplitudeArray[istep] = Convert.ToDouble(sort_trace[istep]);
                }

                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA1 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(freqArray[istep]);
                    templine[1] = Convert.ToString(Math.Round(amplitudeArray[istep], 3));
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + TestParaName + "_MXA1_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Save_MXA2Trace(int traceNo, string TestParaName, bool saveData)
        {
            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            if (saveData)
            {
                //Read MXA Trace and store to temp file
                double startFreqHz = Eq.Site[0]._EqSA02.READ_STARTFREQ();
                double stopFreqHz = Eq.Site[0]._EqSA02.READ_STOPFREQ();
                int sweepPts = Convert.ToInt16(Eq.Site[0]._EqSA02.READ_SWEEP_POINTS());
                stepFreqHz = (stopFreqHz - startFreqHz) / (sweepPts - 1);

                double[] freqArray = new double[sweepPts];
                double[] amplitudeArray = new double[sweepPts];
                string[] sort_trace = new string[sweepPts];

                string tmpMxadata = Eq.Site[0]._EqSA02.READ_MXATrace(traceNo);
                sort_trace = tmpMxadata.Split(',');

                tmpFreqHz = startFreqHz;          //initialize 1st data to startFreq

                for (istep = 0; istep < sweepPts; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }

                    freqArray[istep] = tmpFreqHz / 1e6;       //convert to MHz
                    amplitudeArray[istep] = Convert.ToDouble(sort_trace[istep]);
                }

                //Save all data to datalog 
                string[] templine = new string[2];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#MXA2 SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                templine[0] = "#MXA_FREQ (MHz)";
                templine[1] = "AMPLITUDE (dBm)";
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range
                for (istep = 0; istep < sweepPts; istep++)
                {
                    //Sorted the calibration result to array
                    templine[0] = Convert.ToString(freqArray[istep]);
                    templine[1] = Convert.ToString(Math.Round(amplitudeArray[istep], 3));
                    LocalTextList.Add(string.Join(",", templine));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + TestParaName + "_MXA2_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);
            }
        }
        public void Save_PXI_Trace(string TestParaName, string testUsePrev, bool saveData, int rbw_counter, double rbw_Hz)
        {
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            int istep = 0;
            int sweepPts = 0;
            int traceNo;
            string rbw_paramName;

            if (saveData)
            {
                rbw_paramName = Math.Abs(rbw_Hz / 1e6).ToString();
                sweepPts = PXITrace[TestCount].Multi_Trace[rbw_counter][0].NoPoints;
                traceNo = PXITrace[TestCount].TraceCount;

                //Save all data to datalog 
                string[] templine = new string[traceNo + 1];
                string[] templine2 = new string[traceNo + 1];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#PXI SWEEP DATALOG");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                LocalTextList.Add("#RBW Hz : " + rbw_Hz.ToString());
                templine[0] = "#VSA_FREQ (MHz)";

                for (int n = 0; n < traceNo; n++)
                {
                    templine[n + 1] = "dBm_RUN_" + (n + 1);
                }
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range 
                for (istep = 0; istep < sweepPts; istep++)
                {
                    for (int n = 0; n < traceNo; n++)
                    {
                        if (n == 0)
                        {
                            templine2[n] = Convert.ToString(PXITrace[TestCount].Multi_Trace[rbw_counter][n].FreqMHz[istep]);
                        }
                        templine2[n + 1] = Convert.ToString(PXITrace[TestCount].Multi_Trace[rbw_counter][n].Ampl[istep]);
                    }
                    LocalTextList.Add(string.Join(",", templine2));
                }



                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + TestParaName + "_" + rbw_paramName + "MHz_PXI_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);

            }

        }
        public void Save_PXI_NF_TraceRaw(string TestParaName, int testUsePrev, bool saveData, int rbw_counter, double rbw_Hz)
        {

            int istep = 0;
            int sweepPts = 0;
            int traceNo;
            string rbw_paramName;

            if (saveData)
            {
                rbw_paramName = Math.Abs(rbw_Hz / 1e6).ToString();

                //NoPoints and TraceCount are similar between PXITrace and PXITRaceRaw .. Only define in PXITrace
                sweepPts = PXITrace[testUsePrev].Multi_Trace[rbw_counter][0].NoPoints;
                traceNo = PXITrace[testUsePrev].TraceCount;

                //Save all data to datalog 
                string[] templine = new string[traceNo + 1];
                string[] templine2 = new string[traceNo + 1];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#PXI SWEEP DATALOG - RAW Data");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                LocalTextList.Add("#Measured Bandwidth Hz : " + rbw_Hz.ToString());
                templine[0] = "#VSA_FREQ (MHz)";

                for (int n = 0; n < traceNo; n++)
                {
                    templine[n + 1] = "dB_RUN_" + (n + 1);
                }
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range 
                for (istep = 0; istep < sweepPts; istep++)
                {
                    for (int n = 0; n < traceNo; n++)
                    {
                        if (n == 0)
                        {
                            templine2[n] = Convert.ToString(PXITraceRaw[testUsePrev].Multi_Trace[rbw_counter][n].FreqMHz[istep]);
                        }
                        templine2[n + 1] = Convert.ToString(PXITraceRaw[testUsePrev].Multi_Trace[rbw_counter][n].Ampl[istep]);
                    }
                    LocalTextList.Add(string.Join(",", templine2));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + TestParaName + "_" + rbw_paramName + "MHz_PXI_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);

            }

        }
        public void Save_PXI_TraceRaw(string TestParaName, string testUsePrev, bool saveData, int rbw_counter, double rbw_Hz)
        {
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            int istep = 0;
            int sweepPts = 0;
            int traceNo;
            string rbw_paramName;

            if (saveData)
            {
                rbw_paramName = Math.Abs(rbw_Hz / 1e6).ToString();

                //NoPoints and TraceCount are similar between PXITrace and PXITRaceRaw .. Only define in PXITrace
                sweepPts = PXITrace[TestCount].Multi_Trace[rbw_counter][0].NoPoints;
                traceNo = PXITrace[TestCount].TraceCount;

                //Save all data to datalog 
                string[] templine = new string[traceNo + 1];
                string[] templine2 = new string[traceNo + 1];
                ArrayList LocalTextList = new ArrayList();
                ArrayList tmpCalMsg = new ArrayList();

                //Calibration File Header
                LocalTextList.Add("#PXI SWEEP DATALOG - RAW Data");
                LocalTextList.Add("#Date : " + DateTime.Now.ToShortDateString());
                LocalTextList.Add("#Time : " + DateTime.Now.ToLongTimeString());
                LocalTextList.Add("#RBW Hz : " + rbw_Hz.ToString());
                templine[0] = "#VSA_FREQ (MHz)";

                for (int n = 0; n < traceNo; n++)
                {
                    templine[n + 1] = "dBm_RUN_" + (n + 1);
                }
                LocalTextList.Add(string.Join(",", templine));

                // Start looping until complete the freq range 
                for (istep = 0; istep < sweepPts; istep++)
                {
                    for (int n = 0; n < traceNo; n++)
                    {
                        if (n == 0)
                        {
                            templine2[n] = Convert.ToString(PXITraceRaw[TestCount].Multi_Trace[rbw_counter][n].FreqMHz[istep]);
                        }
                        templine2[n + 1] = Convert.ToString(PXITraceRaw[TestCount].Multi_Trace[rbw_counter][n].Ampl[istep]);
                    }
                    LocalTextList.Add(string.Join(",", templine2));
                }

                //Write cal data to csv file
                if (!Directory.Exists(SNPFile.FileOutput_Path))
                {
                    Directory.CreateDirectory(SNPFile.FileOutput_Path);
                }
                string tempPath = SNPFile.FileOutput_Path + SNPFile.FileOutput_FileName + "_" + TestParaName + "_" + rbw_paramName + "MHz_PXI_Unit" + tmpUnit_No.ToString() + ".csv";
                IO_TxtFile.CreateWrite_TextFile(tempPath, LocalTextList);

            }

        }
        public void Read_PXI_MultiTrace(string testUsePrev, double startFreqMHz, double stopFreqMHz, double stepFreqMHz, string searchMethod, string testParam, out double calcDataFreq, out double calcData, int rbw_counter, double rbw_Hz)
        {
            int noPtsUser = 0;
            int[] testNumber;
            int traceCount = 0;
            int startTraceNo = 0;
            int testUsePrev_ArrayNo = 0;

            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            bool excludeSoakSweep = true;

            noPtsUser = (int)((stopFreqMHz - startFreqMHz) / Math.Round(stepFreqMHz, 3)) + 1;

            //if excluded soak sweep trace , need to remove the array[0] from PXITrace[testnumber].Multi_Trace[0]
            for (int i = 0; i < PXITrace.Length; i++)
            {
                if (testUsePrev == PXITrace[i].TestNumber)
                {
                    testUsePrev_ArrayNo = i;
                    excludeSoakSweep = PXITrace[testUsePrev_ArrayNo].SoakSweep;
                }
            }

            if (excludeSoakSweep)
            {
                traceCount = PXITrace[testUsePrev_ArrayNo].TraceCount - 1;
                startTraceNo = 1;
            }
            else
            {
                traceCount = PXITrace[testUsePrev_ArrayNo].TraceCount;
                startTraceNo = 0;
            }

            #region Initialize Array and User Define Freq
            //initialize array & sorted the selected freq points
            s_TraceNo[] sortedMultiTrace = new s_TraceNo[traceCount];
            testNumber = new int[traceCount];

            for (int i = 0; i < traceCount; i++)
            {
                sortedMultiTrace[i].Ampl = new double[noPtsUser];
                sortedMultiTrace[i].FreqMHz = new double[noPtsUser];

                testNumber[i] = startTraceNo;           //example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6

                tmpFreqHz = startFreqMHz * 1e6;         //initialize 1st data to startFreq
                stepFreqHz = stepFreqMHz * 1e6;
                for (istep = 0; istep < noPtsUser; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }
                    sortedMultiTrace[i].FreqMHz[istep] = Math.Round((tmpFreqHz / 1e6), 3);    //convert back to MHz
                    sortedMultiTrace[i].Ampl[istep] = 99999;              //initialize to default
                }

                startTraceNo++;
            }
            #endregion

            #region SORTED DATA POINT
            //sort the respective trace data to temp array location
            //really complex sorting , need to sort user define test freq (lower count) to actual of MXA trace test point (higher count) 
            //Example : 65 test freq and compared with 601 points of MXA trace (note:  both user define start & stop freq  must be in range of MXA trace start & stop freq)
            for (int i = 0; i < testNumber.Length; i++)     // "use previous" test number loop -> example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6
            {
                for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
                {
                    if (rbw_Hz == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[i]].RBW_Hz)     //check stored RBW is same as pass in RBW , if different will not proceed
                    {
                        for (int j = 0; j < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[i]].NoPoints; j++)
                        {
                            if (sortedMultiTrace[i].FreqMHz[istep] == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[i]].FreqMHz[j])        //find same freq and store amplitude to temp array
                            {
                                sortedMultiTrace[i].Ampl[istep] = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[i]].Ampl[j];
                            }
                        }
                    }
                }
            }
            #endregion

            #region Calculate Result
            //Calculate the result from the sorted data
            Result_MXATrace = new s_TraceNo();
            Result_MXATrace.Ampl = new double[noPtsUser];
            Result_MXATrace.FreqMHz = new double[noPtsUser];

            s_TraceNo resultMultiTrace = new s_TraceNo();
            resultMultiTrace.Ampl = new double[noPtsUser];
            resultMultiTrace.FreqMHz = new double[noPtsUser];

            calcData = 999;
            calcDataFreq = -999;
            Result_MXATrace.MXA_No = "PXI";
            Result_MXATrace.NoPoints = noPtsUser;
            Result_MXATrace.Result_Header = testParam;

            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    for (istep = 0; istep < noPtsUser; istep++)     //get MAX data for every noPtsUser out of multitrace (from "use previous" setting)
                    {
                        for (int i = 0; i < sortedMultiTrace.Length; i++)
                        {
                            if (i == 0)
                            {
                                calcData = sortedMultiTrace[0].Ampl[istep];
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                            }
                            if (calcData < sortedMultiTrace[i].Ampl[istep])
                            {
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                                calcData = sortedMultiTrace[i].Ampl[istep];
                            }
                        }
                    }
                    for (istep = 0; istep < noPtsUser; istep++) //get MAX from the MAX of the multitrace
                    {
                        if (istep == 0)
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                        if (calcData < resultMultiTrace.Ampl[istep])
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                    }
                    break;

                case "MIN":
                    for (istep = 0; istep < noPtsUser; istep++)     //get MIN data for every noPtsUser out of multitrace (from "use previous" setting)
                    {
                        for (int i = 0; i < sortedMultiTrace.Length; i++)
                        {
                            if (i == 0)
                            {
                                calcData = sortedMultiTrace[0].Ampl[istep];
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                            }
                            if (calcData > sortedMultiTrace[i].Ampl[istep])
                            {
                                resultMultiTrace.Ampl[istep] = sortedMultiTrace[i].Ampl[istep];
                                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[i].FreqMHz[istep];
                                calcData = sortedMultiTrace[i].Ampl[istep];
                            }
                        }
                    }
                    for (istep = 0; istep < noPtsUser; istep++) //get MIN from the MIN of the multitrace
                    {
                        if (istep == 0)
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                        if (calcData > resultMultiTrace.Ampl[istep])
                        {
                            calcData = resultMultiTrace.Ampl[istep];
                            calcDataFreq = resultMultiTrace.FreqMHz[istep];
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Test Parameter : " + testParam + "(" + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    break;
            }
            #endregion

            Result_MXATrace = resultMultiTrace;

        }
        public void Read_PXI_SingleTrace(string testUsePrev, int treaceNo, double startFreqMHz, double stopFreqMHz, double stepFreqMHz, string searchMethod, string testParam, int rbw_counter, double rbw_Hz)
        {
            int noPtsUser = 0;
            int[] testNumber;
            int traceCount = 0;
            int startTraceNo = 0;
            int testUsePrev_ArrayNo = 0;

            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            bool excludeSoakSweep = true;

            noPtsUser = (int)((stopFreqMHz - startFreqMHz) / Math.Round(stepFreqMHz, 3)) + 1;

            //if excluded soak sweep trace , need to remove the array[0] from PXITrace[testnumber].Multi_Trace[0]
            for (int i = 0; i < PXITrace.Length; i++)
            {
                if (testUsePrev == PXITrace[i].TestNumber)
                {
                    testUsePrev_ArrayNo = i;
                    excludeSoakSweep = PXITrace[testUsePrev_ArrayNo].SoakSweep;
                    traceCount = PXITrace[testUsePrev_ArrayNo].TraceCount;
                }
            }

            #region Initialize Array and User Define Freq
            //initialize array & sorted the selected freq points
            s_TraceNo[] sortedMultiTrace = new s_TraceNo[traceCount];
            testNumber = new int[traceCount];

            for (int i = 0; i < traceCount; i++)
            {
                sortedMultiTrace[i].Ampl = new double[noPtsUser];
                sortedMultiTrace[i].FreqMHz = new double[noPtsUser];

                testNumber[i] = startTraceNo;           //example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6

                tmpFreqHz = startFreqMHz * 1e6;         //initialize 1st data to startFreq
                stepFreqHz = stepFreqMHz * 1e6;
                for (istep = 0; istep < noPtsUser; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }
                    sortedMultiTrace[i].FreqMHz[istep] = Math.Round((tmpFreqHz / 1e6), 3);    //convert back to MHz
                    sortedMultiTrace[i].Ampl[istep] = 99999;              //initialize to default
                }

                startTraceNo++;
            }
            #endregion

            #region SORTED DATA POINT
            //sort the respective trace data to temp array location
            //really complex sorting , need to sort user define test freq (lower count) to actual of MXA trace test point (higher count) 
            //Example : 65 test freq and compared with 601 points of MXA trace (note:  both user define start & stop freq  must be in range of MXA trace start & stop freq)
            for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
            {
                if (rbw_Hz == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].RBW_Hz)     //check stored RBW is same as pass in RBW , if different will not proceed
                {
                    for (int j = 0; j < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].NoPoints; j++)
                    {
                        if (sortedMultiTrace[treaceNo].FreqMHz[istep] == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j])        //find same freq and store amplitude to temp array
                        {
                            sortedMultiTrace[treaceNo].Ampl[istep] = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].Ampl[j];
                        }
                    }
                }
            }
            #endregion

            #region Calculate Result
            //Calculate the result from the sorted data
            Result_MXATrace = new s_TraceNo();
            Result_MXATrace.Ampl = new double[noPtsUser];
            Result_MXATrace.FreqMHz = new double[noPtsUser];

            s_TraceNo resultMultiTrace = new s_TraceNo();
            resultMultiTrace.Ampl = new double[noPtsUser];
            resultMultiTrace.FreqMHz = new double[noPtsUser];

            Result_MXATrace.MXA_No = "PXI";
            Result_MXATrace.NoPoints = noPtsUser;
            Result_MXATrace.Result_Header = testParam;

            for (istep = 0; istep < noPtsUser; istep++)
            {
                resultMultiTrace.Ampl[istep] = sortedMultiTrace[treaceNo].Ampl[istep];
                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[treaceNo].FreqMHz[istep];
            }
            #endregion

            Result_MXATrace = resultMultiTrace;
        }
        public void Read_PXI_SingleTrace_Interpolate(string testUsePrev, int treaceNo, double startFreqMHz, double stopFreqMHz, double stepFreqMHz, string searchMethod, string testParam, int rbw_counter, double rbw_Hz)
        {
            //routine to read the trace array and interpolate if SaveTrace data points not equal to SearchData
            int noPtsUser = 0;
            int[] testNumber;
            int traceCount = 0;
            int startTraceNo = 0;
            int testUsePrev_ArrayNo = 0;

            int istep;
            double tmpFreqHz = 0;
            double stepFreqHz = 0;

            bool excludeSoakSweep = true;

            noPtsUser = (int)((stopFreqMHz - startFreqMHz) / Math.Round(stepFreqMHz, 3)) + 1;

            //if excluded soak sweep trace , need to remove the array[0] from PXITrace[testnumber].Multi_Trace[0]
            for (int i = 0; i < PXITrace.Length; i++)
            {
                if (testUsePrev == PXITrace[i].TestNumber)
                {
                    testUsePrev_ArrayNo = i;
                    excludeSoakSweep = PXITrace[testUsePrev_ArrayNo].SoakSweep;
                    traceCount = PXITrace[testUsePrev_ArrayNo].TraceCount;
                }
            }

            #region Initialize Array and User Define Freq
            //initialize array & sorted the selected freq points
            s_TraceNo[] sortedMultiTrace = new s_TraceNo[traceCount];
            testNumber = new int[traceCount];

            for (int i = 0; i < traceCount; i++)
            {
                sortedMultiTrace[i].Ampl = new double[noPtsUser];
                sortedMultiTrace[i].FreqMHz = new double[noPtsUser];

                testNumber[i] = startTraceNo;           //example sort "use previous" - 3,4,6 -> array testNumber[i] where i(0) = 3 , i(1) = 4 , i(2) = 6

                tmpFreqHz = startFreqMHz * 1e6;         //initialize 1st data to startFreq
                stepFreqHz = stepFreqMHz * 1e6;
                for (istep = 0; istep < noPtsUser; istep++)
                {
                    if (istep > 0)
                    {
                        tmpFreqHz = tmpFreqHz + stepFreqHz;
                    }
                    sortedMultiTrace[i].FreqMHz[istep] = Math.Round((tmpFreqHz / 1e6), 3);    //convert back to MHz
                    sortedMultiTrace[i].Ampl[istep] = 99999;              //initialize to default
                }

                startTraceNo++;
            }
            #endregion

            #region SORTED DATA POINT
            //sort the respective trace data to temp array location
            //really complex sorting , need to sort user define test freq (lower count) to actual of MXA trace test point (higher count) 
            //Example : 65 test freq and compared with 601 points of MXA trace (note:  both user define start & stop freq  must be in range of MXA trace start & stop freq)

            if (rbw_Hz == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].RBW_Hz)     //check stored RBW is same as pass in RBW , if different will not proceed
            {
                if (PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].NoPoints < noPtsUser)
                {
                    //will do interpolate if MXA Trace point is smaller than user define test points
                    for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
                    {
                        for (int j = 0; j < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].NoPoints; j++)
                        {
                            if (sortedMultiTrace[treaceNo].FreqMHz[istep] == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j])        //find same freq and store amplitude to temp array
                            {
                                sortedMultiTrace[treaceNo].Ampl[istep] = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].Ampl[j];
                                break;
                            }
                            else        //interpolation 
                            {
                                if (j < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].NoPoints - 1)   //to ensure that no array overflow
                                {
                                    if ((sortedMultiTrace[treaceNo].FreqMHz[istep] > PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j]) && (sortedMultiTrace[treaceNo].FreqMHz[istep] < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j + 1]))
                                    {
                                        double g = sortedMultiTrace[treaceNo].FreqMHz[istep];
                                        double g1 = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j];
                                        double g2 = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j + 1];

                                        double d1 = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].Ampl[j];
                                        double d2 = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].Ampl[j + 1];

                                        //linear interpolation formula
                                        double d = d1 + (((g - g1) / (g2 - g1)) * (d2 - d1));
                                        sortedMultiTrace[treaceNo].Ampl[istep] = Math.Round(d, 3);
                                        break;
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    for (istep = 0; istep < noPtsUser; istep++)     //sorted user define freq point (lower count) against MXA trace no points (higher count)
                    {
                        for (int j = 0; j < PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].NoPoints; j++)
                        {
                            if (sortedMultiTrace[treaceNo].FreqMHz[istep] == PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].FreqMHz[j])        //find same freq and store amplitude to temp array
                            {
                                sortedMultiTrace[treaceNo].Ampl[istep] = PXITrace[testUsePrev_ArrayNo].Multi_Trace[rbw_counter][testNumber[treaceNo]].Ampl[j];
                                break;
                            }
                        }
                    }
                }
            }

            #endregion

            #region Calculate Result
            //Calculate the result from the sorted data
            Result_MXATrace = new s_TraceNo();
            Result_MXATrace.Ampl = new double[noPtsUser];
            Result_MXATrace.FreqMHz = new double[noPtsUser];

            s_TraceNo resultMultiTrace = new s_TraceNo();
            resultMultiTrace.Ampl = new double[noPtsUser];
            resultMultiTrace.FreqMHz = new double[noPtsUser];

            Result_MXATrace.MXA_No = "PXI";
            Result_MXATrace.NoPoints = noPtsUser;
            Result_MXATrace.Result_Header = testParam;

            for (istep = 0; istep < noPtsUser; istep++)
            {
                resultMultiTrace.Ampl[istep] = sortedMultiTrace[treaceNo].Ampl[istep];
                resultMultiTrace.FreqMHz[istep] = sortedMultiTrace[treaceNo].FreqMHz[istep];
            }
            #endregion

            Result_MXATrace.Ampl = resultMultiTrace.Ampl;
            Result_MXATrace.FreqMHz = resultMultiTrace.FreqMHz;
        }
    }
}
