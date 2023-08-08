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
        private void RF_Calibration(int Trig_Delay, int Generic_Delay, int RdCurr_Delay, int RdPwr_Delay, int Setup_Delay)
        {
            System.Collections.ArrayList tempArray;
            List<string> CalGroup = new List<string>();

            string tempString;
            string FileSetting = Convert.ToString(DicCalInfo[DataFilePath.LocSettingPath]);
            string LocSetFilePath = Convert.ToString(DicCalInfo[DataFilePath.LocSettingPath]);

            string VSTmodel = myUtility.ReadTextFile(LocSetFilePath, "Model", "PXI_VST");
            string VSTaddr = myUtility.ReadTextFile(LocSetFilePath, "Address", "PXI_VST");

            int ArrayCount, CalGroupCount, i = 0, FreqListCountRF = 0, FreqListCountNF = 0;
            bool blnUseSourceCalFactor = false;
            double power = -999;
            tempArray = myUtility.ReadCalProcedure(Convert.ToString(DicCalInfo[DataFilePath.LocSettingPath]));
            ArrayCount = tempArray.Count;
            double startFreq, stopFreq;
            int markerNo;
            string tmpCalHeader;

            for (i = 0; i < ArrayCount; i++)
            {
                if (tempArray[i].ToString().Contains("[Cal"))
                {
                    tempString = tempArray[i].ToString().Replace('[', '.').Replace(']', '.');
                    CalGroup.Add(tempString.Split('.')[1]);
                }
            }
            CalGroupCount = CalGroup.Count();

            myUtility.CalFileGeneration(Convert.ToString(DicCalInfo[DataFilePath.CalPathRF]));

            for (i = 0; i < CalGroupCount; i++)
            {

                if (myUtility.ReadTextFile(FileSetting, CalGroup[i], "Skip").ToUpper() == "FALSE")
                {
                    FileInfo fCalDataFile = new FileInfo(Convert.ToString(DicCalInfo[DataFilePath.CalPathRF]));

                    StreamWriter swCalDataFile = fCalDataFile.AppendText();

                    string tempFreq = string.Empty, tempCalResult = string.Empty, tempMkrCalResult = string.Empty;
                    string Source1_Model = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Source1_Model").ToUpper();
                    string Source2_Model = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Source2_Model").ToUpper();
                    string PowerLevel = myUtility.ReadTextFile(FileSetting, CalGroup[i], "PowerLevel").ToUpper();
                    string Modulation = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Modulation_Format").ToUpper();
                    int Measure_Channel = Convert.ToInt16(myUtility.ReadTextFile(FileSetting, CalGroup[i], "Measure_Channel"));
                    string Target_CalSegment = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Target_CalSegment").ToUpper();
                    string Source_CalFactor = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Source_CalFactor").ToUpper();
                    double CalLimitLow = Convert.ToDouble(myUtility.ReadTextFile(FileSetting, CalGroup[i], "CalLimitLow"));
                    double CalLimitHigh = Convert.ToDouble(myUtility.ReadTextFile(FileSetting, CalGroup[i], "CalLimitHigh"));
                    string CalType = myUtility.ReadTextFile(FileSetting, CalGroup[i], "Type").ToUpper();
                    string CalFreqList = myUtility.ReadTextFile(FileSetting, CalGroup[i], "CalFreqList");
                    //string mkrNoise_RBW = myUtility.ReadTextFile(FileSetting, CalGroup[i], "MKRNoise_RBW");
                    string sa_config = myUtility.ReadTextFile(FileSetting, CalGroup[i], "SA_CONFIG").ToUpper();
                    double CalOffset = Convert.ToDouble(myUtility.ReadTextFile(FileSetting, CalGroup[i], "CalOffset"));
                    string switchPath = myUtility.ReadTextFile(FileSetting, CalGroup[i], "SwitchControl").ToUpper();

                    DialogResult calSkip = MessageBox.Show(myUtility.ReadTextFile(FileSetting, CalGroup[i], "MessageBox"), CalType + " (" + Target_CalSegment + ") -> Calibration Data - Press OK to proceed , CANCEL to skip", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                    if (calSkip == DialogResult.Cancel)
                    {
                        CalType = "SKIP_CAL";
                    }

                    switch (switchPath)
                    {
                        case "NONE":
                        case "NA":
                            //Do nothing , switching not enable
                            break;
                        default:
                            if (bMultiSW)
                            {
                                string[] str = switchPath.Split('@');

                                Eq.Site[0]._EqSwitch.SetPath(str[0]);
                                Eq.Site[0]._EqSwitchSplit.SetPath(str[1]);
                            }
                            else
                            {
                                Eq.Site[0]._EqSwitch.SetPath(switchPath);

                            }
                            break;
                    }

                    double[] noiseMKR_RBW;
                    double[] FreqListNF = new double[2000], FreqListMKR = new double[2000];
                    string[] FreqListRF = new string[2000], SourceCalFactor = new string[2000];
                    myUtility.LoadCalFreqList(myUtility.ReadTextFile(FileSetting, "FilePath", CalFreqList), ref FreqListRF, ref FreqListCountRF);
                    myUtility.LoadSourceData(Convert.ToString(DicCalInfo[DataFilePath.CalPathRF]), Source_CalFactor, FreqListRF, ref SourceCalFactor, ref blnUseSourceCalFactor, ref swCalDataFile);

                    //variable for display result
                    string[] dispFreq;
                    string[] dispCal;
                    string dispResult = "";
                    int iNewline = 0;
                    bool calStatus = false;
                    bool status = false;
                    bool callimitStatus = true;
                    string tmpMsgTxt = "";
                    double calRslt = -999;
                    string[] tempdataMkr;

                    switch (CalType)
                    {
                        case "RF_LOPWR_NFCAL":
                            #region RF Lo Noise Power Calibration
                            //Calibration using MXA

                            myUtility.Decode_MXA_Setting(sa_config);
                            startFreq = Convert.ToDouble(FreqListRF[0]);
                            stopFreq = Convert.ToDouble(FreqListRF[FreqListCountRF - 1]);
                            markerNo = 1;

                            switch (Measure_Channel)
                            {
                                case 1:
                                    Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    Eq.Site[0]._EqSA01.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                                    DelayMs(1500);

                                    Eq.Site[0]._EqSA01.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                                    Eq.Site[0]._EqSA01.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                                    Eq.Site[0]._EqSA01.VIDEO_BW(myUtility.MXA_Setting.VBW);
                                    Eq.Site[0]._EqSA01.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                                    Eq.Site[0]._EqSA01.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                                    Eq.Site[0]._EqSA01.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                                    Eq.Site[0]._EqSA01.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                                    Eq.Site[0]._EqSA01.START_FREQ(startFreq.ToString(), "MHz");
                                    Eq.Site[0]._EqSA01.STOP_FREQ(stopFreq.ToString(), "MHz");
                                    Eq.Site[0]._EqSA01.TRIGGER_CONTINUOUS();
                                    break;
                                case 2:
                                    Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    Eq.Site[0]._EqSA02.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                                    DelayMs(1500);

                                    Eq.Site[0]._EqSA02.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                                    Eq.Site[0]._EqSA02.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                                    Eq.Site[0]._EqSA02.VIDEO_BW(myUtility.MXA_Setting.VBW);
                                    Eq.Site[0]._EqSA02.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                                    Eq.Site[0]._EqSA02.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                                    Eq.Site[0]._EqSA02.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                                    Eq.Site[0]._EqSA02.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                                    Eq.Site[0]._EqSA02.START_FREQ(startFreq.ToString(), "MHz");
                                    Eq.Site[0]._EqSA02.STOP_FREQ(stopFreq.ToString(), "MHz");
                                    Eq.Site[0]._EqSA02.TRIGGER_CONTINUOUS();
                                    break;
                                default:
                                    MessageBox.Show("Wrong MXA Equipment selection : " + Measure_Channel + " , Only MXA 1 or 2 allow!!!");
                                    break;
                            }

                            DelayMs(1000);

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];

                                    Eq.Site[0]._EqSG01.SetFreq(Convert.ToDouble(FreqListRF[iCount]));
                                    Eq.Site[0]._EqSG01.SetAmplitude((float)Convert.ToDouble(PowerLevel));
                                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.ON);
                                    DelayMs(200);

                                    switch (Measure_Channel)
                                    {
                                        case 1:
                                            Eq.Site[0]._EqSA01.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                                            DelayMs(myUtility.MXA_Setting.SweepT);
                                            status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                                            power = Eq.Site[0]._EqSA01.READ_MARKER(markerNo) - Convert.ToDouble(PowerLevel) + CalOffset;
                                            break;
                                        case 2:
                                            Eq.Site[0]._EqSA02.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                                            DelayMs(myUtility.MXA_Setting.SweepT);
                                            status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                                            power = Eq.Site[0]._EqSA02.READ_MARKER(markerNo) - Convert.ToDouble(PowerLevel) + CalOffset;
                                            break;
                                        default:
                                            break;
                                    }

                                    //compare individual result with cal spec limit & set cal status flag
                                    if ((power < CalLimitLow) || (power > CalLimitHigh))
                                    {
                                        callimitStatus = false;
                                    }

                                    tempCalResult += "," + Math.Round(power, 3);
                                }

                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }

                            break;
                        #endregion

                        case "RF_LOPWR_CAL":
                            #region RF Lo Power Calibration
                            //Calibration using MXA

                            myUtility.Decode_MXA_Setting(sa_config);

                            switch (Measure_Channel)
                            {
                                case 1:
                                    Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    Eq.Site[0]._EqSA01.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                                    DelayMs(1500);

                                    Eq.Site[0]._EqSA01.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                                    Eq.Site[0]._EqSA01.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                                    Eq.Site[0]._EqSA01.VIDEO_BW(myUtility.MXA_Setting.VBW);
                                    Eq.Site[0]._EqSA01.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                                    Eq.Site[0]._EqSA01.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                                    Eq.Site[0]._EqSA01.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                                    Eq.Site[0]._EqSA01.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                                    Eq.Site[0]._EqSA01.TRIGGER_CONTINUOUS();
                                    break;
                                case 2:
                                    Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                                    Eq.Site[0]._EqSA02.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                                    DelayMs(1500);

                                    Eq.Site[0]._EqSA02.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                                    Eq.Site[0]._EqSA02.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                                    Eq.Site[0]._EqSA02.VIDEO_BW(myUtility.MXA_Setting.VBW);
                                    Eq.Site[0]._EqSA02.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                                    Eq.Site[0]._EqSA02.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                                    Eq.Site[0]._EqSA02.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                                    Eq.Site[0]._EqSA02.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                                    Eq.Site[0]._EqSA02.TRIGGER_CONTINUOUS();
                                    break;
                                default:
                                    MessageBox.Show("Wrong MXA Equipment selection : " + Measure_Channel + " , Only MXA 1 or 2 allow!!!");
                                    break;
                            }

                            DelayMs(1000);

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];

                                    Eq.Site[0]._EqSG01.SetFreq(Convert.ToDouble(FreqListRF[iCount]));
                                    Eq.Site[0]._EqSG01.SetAmplitude((float)Convert.ToDouble(PowerLevel));
                                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.ON);
                                    DelayMs(200);

                                    switch (Measure_Channel)
                                    {
                                        case 1:
                                            Eq.Site[0]._EqSA01.FREQ_CENT(FreqListRF[iCount].ToString(), "MHz");
                                            DelayMs(500);
                                            power = Eq.Site[0]._EqSA01.MEASURE_PEAK_POINT(10) - Convert.ToDouble(PowerLevel) + CalOffset;
                                            break;
                                        case 2:
                                            Eq.Site[0]._EqSA02.FREQ_CENT(FreqListRF[iCount].ToString(), "MHz");
                                            DelayMs(500);
                                            power = Eq.Site[0]._EqSA02.MEASURE_PEAK_POINT(10) - Convert.ToDouble(PowerLevel) + CalOffset;
                                            break;
                                        default:
                                            break;
                                    }

                                    //compare individual result with cal spec limit & set cal status flag
                                    if ((power < CalLimitLow) || (power > CalLimitHigh))
                                    {
                                        callimitStatus = false;
                                    }
                                    tempCalResult += "," + Math.Round(power, 3);
                                }

                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }

                            break;
                        #endregion

                        case "RF_HIPWR_CAL":
                            #region RF High Power Calibration
                            // calibration using Power Meter
                            tempFreq = string.Empty;
                            tempCalResult = string.Empty;

                            Eq.Site[0]._EqPwrMeter.SetOffset(1, CalOffset);

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];

                                    Eq.Site[0]._EqSG01.SetFreq(Convert.ToDouble(FreqListRF[iCount]));
                                    Eq.Site[0]._EqSG01.SetAmplitude((float)Convert.ToDouble(PowerLevel));
                                    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.ON);
                                    DelayMs(Setup_Delay);

                                    Eq.Site[0]._EqPwrMeter.SetFreq(1, Convert.ToDouble(FreqListRF[iCount]), PowerSensorCalType);
                                    DelayMs(RdPwr_Delay);
                                    power = Eq.Site[0]._EqPwrMeter.MeasPwr(1) - Convert.ToDouble(PowerLevel);

                                    tempCalResult += "," + Math.Round(power, 3);

                                    //compare individual result with cal spec limit & set cal status flag
                                    if (power < CalLimitLow || power > CalLimitHigh)
                                    {
                                        callimitStatus = false;
                                    }
                                }

                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);
                            Eq.Site[0]._EqPwrMeter.SetOffset(1, 0); //reset power sensor offset to default : 0

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }

                            break;
                        #endregion

                        case "PXI_RF_LOPWR_NFCAL":
                            #region PXI RF Lo Noise Power Cal
                            //Note : using VST-NI5646R
                            //using start,stop and step freq while LXI base using freq list

                            myUtility.Decode_MXA_Setting(sa_config);
                            startFreq = Convert.ToDouble(FreqListRF[0]) * 1e6;
                            stopFreq = Convert.ToDouble(FreqListRF[FreqListCountRF - 1]) * 1e6;
                            double stepFreq = (stopFreq - startFreq) / (FreqListCountRF - 1);
                            float[] tempData = new float[FreqListCountRF];
                            calStatus = false;

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                MyCal.MyVSTCal EqVSTCal;
                                EqVSTCal = new MyCal.MyVSTCal(VSTaddr);
                                EqVSTCal.initialize();

                                EqVSTCal.RFSAPreConfigure(myUtility.MXA_Setting.RefLevel);
                                EqVSTCal.RFSGPreConfigure(Convert.ToDouble(PowerLevel));

                                EqVSTCal.VSTConfigure_DuringTest(startFreq, stopFreq, stepFreq, myUtility.MXA_Setting.RBW);
                                tempData = EqVSTCal.measureLowNoiseCal(myUtility.MXA_Setting.VBW);
                                EqVSTCal.closeVST();

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];
                                    calRslt = Math.Round(Convert.ToDouble(tempData[iCount]) - Convert.ToDouble(PowerLevel) + CalOffset, 3);
                                    tempCalResult += "," + calRslt;

                                    //compare individual result with cal spec limit & set cal status flag
                                    if ((calRslt < CalLimitLow) || (calRslt > CalLimitHigh))
                                    {
                                        callimitStatus = false;
                                    }
                                }
                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }

                            #endregion
                            break;

                        case "PXI_RF_LOPWR_CAL":
                            #region PXI RF Low Power Calibration (Point Measurement) // 20.06.01, Power Calibration for RX Path (SA Point Measurement per Freq)

                            myUtility.Decode_MXA_Setting(sa_config);

                            // Initiate Start Freq, Stop Freq, Step Freq, Temp Data for Pout Data, Calibration Status
                            //NationalInstruments.PrecisionTimeSpan timeout = new NationalInstruments.PrecisionTimeSpan(10.0); // 10ms 안에 Data gathering (10ms가 넘으면 error 발생)
                            NationalInstruments.PrecisionTimeSpan timeout = new NationalInstruments.PrecisionTimeSpan(10.0);

                            startFreq = Convert.ToDouble(FreqListRF[0]) * 1e6;
                            stopFreq = Convert.ToDouble(FreqListRF[FreqListCountRF - 1]) * 1e6;
                            stepFreq = (stopFreq - startFreq) / (FreqListCountRF - 1);
                            tempData = new float[FreqListCountRF];
                            double[] SAtempData;
                            calStatus = false;
                            bool initVST_SG = false;

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                MyCal.MyVSTCal EqVSTCal;
                                EqVSTCal = new MyCal.MyVSTCal(VSTaddr);
                                EqVSTCal.initialize();

                                // Set SA, SG Configuration
                                #region SA Configuration Setting

                                // Reference Clock Setting
                                EqVSTCal.rfsaSession.Configuration.ReferenceClock.Configure("PXI_CLK", 10e6);

                                // Acquisiton Type Setting
                                EqVSTCal.rfsaSession.Configuration.AcquisitionType = RfsaAcquisitionType.Spectrum;
                                EqVSTCal.rfsaSession.Configuration.Spectrum.PowerSpectrumUnits = RfsaPowerSpectrumUnits.dBm;

                                // Trigger Setting
                                //EqVSTCal.rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure("PXI_Trig2", RfsaTriggerEdge.Rising);
                                //EqVSTCal.rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure("PXI_Trig1", RfsaTriggerEdge.Rising, 0);
                                //EqVSTCal.rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;

                                // NumberOfAvg Setting
                                EqVSTCal.rfsaSession.Configuration.Spectrum.AveragingMode = RfsaSpectrumAveragingMode.Rms;
                                EqVSTCal.rfsaSession.Configuration.Spectrum.NumberOfAverages = myUtility.MXA_Setting.NoPoints;

                                // Reference Level & Attenuation
                                EqVSTCal.rfsaSession.Configuration.Vertical.ReferenceLevel = myUtility.MXA_Setting.RefLevel;
                                //EqVSTCal.rfsaSession.Configuration.Vertical.Advanced.RFAttenuation = myUtility.MXA_Setting.Attenuation;

                                // Frequency Span
                                EqVSTCal.rfsaSession.Configuration.Spectrum.Span = myUtility.MXA_Setting.Span;

                                // Resolution BandWidth
                                EqVSTCal.rfsaSession.Configuration.Spectrum.ResolutionBandwidth = myUtility.MXA_Setting.RBW;
                                EqVSTCal.rfsaSession.Configuration.Spectrum.ResolutionBandwidthType = RfsaResolutionBandwidthType.Rbw6dB;

                                EqVSTCal.rfsaSession.Configuration.Spectrum.FftWindowType = RfsaFftWindowType.FlatTop;

                                // Commit 
                                EqVSTCal.rfsaSession.Utility.Commit();

                                #endregion

                                #region SG Configuration Setting
                                // Configure the reference clock source 
                                EqVSTCal._rfsgSession.FrequencyReference.Configure("PXI_CLK", 10E6);
                                EqVSTCal._rfsgSession.Arb.GenerationMode = RfsgWaveformGenerationMode.ContinuousWave;
                                EqVSTCal._rfsgSession.RF.PowerLevelType = RfsgRFPowerLevelType.PeakPower;
                                // Configure the loop bandwidth 
                                EqVSTCal._rfsgSession.RF.Advanced.LoopBandwidth = RfsgLoopBandwidth.High;

                                EqVSTCal._rfsgSession.Arb.PreFilterGain = -3;
                                EqVSTCal._rfsgSession.RF.PowerLevel = Convert.ToDouble(PowerLevel);

                                EqVSTCal._rfsgSession.Utility.Commit();
                                #endregion

                                EqVSTCal._rfsgSession.Initiate(); //Ivan-For retry cal

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];

                                    #region SA Freq Sweep Setting
                                    EqVSTCal.rfsaSession.Configuration.Spectrum.CenterFrequency = Convert.ToDouble(FreqListRF[iCount]) * 1e6;

                                    #endregion
                                    DelayMs(Setup_Delay);

                                    #region SG Freq, PowerLevel Setting
                                    EqVSTCal._rfsgSession.RF.Frequency = Convert.ToDouble(FreqListRF[iCount]) * 1e6;
                                    EqVSTCal._rfsgSession.RF.PowerLevel = Convert.ToDouble(PowerLevel);
                                    #endregion

                                    //Ivan-Disable for retry cal
                                    //if ((iCount == 0) && (initVST_SG == false))   //Turn RF ON for 1st time only - continuos mode
                                    //{
                                    //    EqVSTCal._rfsgSession.Initiate();
                                    //    initVST_SG = true;
                                    //}

                                    // Mesure Power
                                    DelayMs(RdPwr_Delay);
                                    SAtempData = EqVSTCal.rfsaSession.Acquisition.Spectrum.ReadPowerSpectrum(timeout);
                                    power = SAtempData.Max();

                                    calRslt = Math.Round(power - Convert.ToDouble(PowerLevel) + CalOffset, 3);
                                    tempCalResult += "," + calRslt;

                                    //compare individual result with cal spec limit & set cal status flag
                                    if ((calRslt < CalLimitLow) || (calRslt > CalLimitHigh))
                                    {
                                        callimitStatus = false;
                                    }
                                }

                                EqVSTCal.closeVST();

                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }
                            #endregion
                            break;

                        case "PXI_RF_HIPWR_CAL":
                            #region PXI RF High Power Calibration
                            // calibration using Power Meter and VST NI5646R

                            tempFreq = string.Empty;
                            tempCalResult = string.Empty;
                            bool initVSG = false;

                            Eq.Site[0]._EqPwrMeter.SetOffset(1, CalOffset);

                            #region MXG setup
                            MyCal.MyVSTCal EqVSGCal;
                            EqVSGCal = new MyCal.MyVSTCal(VSTaddr);
                            EqVSGCal.initialize();
                            EqVSGCal.RFSGPreConfigure(Convert.ToDouble(PowerLevel));

                            //generate modulated signal
                            string Script =
                                     "script powerServo\r\n"
                                   + "repeat forever\r\n"
                                   + "generate Signal" + "LowCal" + "\r\n"
                                   + "end repeat\r\n"
                                   + "end script";
                            EqVSGCal._rfsgSession.Arb.Scripting.WriteScript(Script);
                            EqVSGCal._rfsgSession.Arb.Scripting.SelectedScriptName = "powerServo";
                            #endregion

                            do
                            {
                                //Initialize Variable
                                tempFreq = string.Empty;
                                tempCalResult = string.Empty;

                                //variable for display result
                                dispFreq = new string[FreqListCountRF];
                                dispCal = new string[FreqListCountRF];
                                dispResult = "";
                                iNewline = 0;
                                calStatus = false;
                                callimitStatus = true;

                                for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                                {
                                    tempFreq += "," + FreqListRF[iCount];

                                    EqVSGCal._rfsgSession.RF.Frequency = Convert.ToDouble(FreqListRF[iCount]) * 1e6;
                                    EqVSGCal._rfsgSession.RF.PowerLevel = Convert.ToDouble(PowerLevel);

                                    if ((iCount == 0) && (initVSG == false))            //Turn RF ON for 1st time only - continuos mode
                                    {
                                        EqVSGCal._rfsgSession.Initiate();
                                        initVSG = true;
                                    }

                                    DelayMs(Setup_Delay);

                                    Eq.Site[0]._EqPwrMeter.SetFreq(1, Convert.ToDouble(FreqListRF[iCount]), PowerSensorCalType);
                                    DelayMs(RdPwr_Delay);
                                    power = Eq.Site[0]._EqPwrMeter.MeasPwr(1) - Convert.ToDouble(PowerLevel);

                                    tempCalResult += "," + Math.Round(power, 3);

                                    //compare individual result with cal spec limit & set cal status flag
                                    if (power < CalLimitLow || power > CalLimitHigh)
                                    {
                                        callimitStatus = false;
                                    }
                                }

                                //Display calibration result 
                                dispFreq = tempFreq.Split(',');
                                dispCal = tempCalResult.Split(',');
                                dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                                iNewline = 0;

                                for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                                {
                                    dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                                    iNewline++;

                                    if (iNewline == 4)
                                    {
                                        dispResult += "\r\n";
                                        iNewline = 0;
                                    }
                                }

                                if (callimitStatus)
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (chkStatus == DialogResult.Yes)
                                    {
                                        calStatus = true;
                                    }
                                }
                                else
                                {
                                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                                    DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                                    if (chkStatus == DialogResult.Cancel)
                                    {
                                        calStatus = true;
                                    }
                                }

                            } while (!calStatus);

                            EqVSGCal._rfsgSession.Abort();         //stop power servo script
                            EqVSGCal.closeVST();
                            Eq.Site[0]._EqPwrMeter.SetOffset(1, 0); //reset power sensor offset to default : 0

                            //Write to file if CalLimitStatus is True
                            if (callimitStatus)
                            {
                                swCalDataFile.WriteLine("");
                                swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                                swCalDataFile.WriteLine(tempCalResult);
                            }

                            #endregion
                            break;

                        //case "RF_LOPWR_NFCAL_NOISEMKR":
                        //    #region RF Lo Noise Power Calibration with Noise Marker use for noise floor normalization
                        //    //Calibration using MXA

                        //    myUtility.Decode_MXA_Setting(sa_config);
                        //    startFreq = Convert.ToDouble(FreqListRF[0]);
                        //    stopFreq = Convert.ToDouble(FreqListRF[FreqListCountRF - 1]);
                        //    markerNo = 1;
                        //    tempdataMkr = mkrNoise_RBW.Split(';');

                        //    #region MXA/MXG Setting
                        //    switch (Measure_Channel)
                        //    {
                        //        case 1:
                        //            Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                        //            Eq.Site[0]._EqSA01.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                        //            DelayMs(1500);

                        //            Eq.Site[0]._EqSA01.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                        //            Eq.Site[0]._EqSA01.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                        //            Eq.Site[0]._EqSA01.VIDEO_BW(myUtility.MXA_Setting.VBW);
                        //            Eq.Site[0]._EqSA01.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                        //            Eq.Site[0]._EqSA01.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                        //            Eq.Site[0]._EqSA01.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                        //            Eq.Site[0]._EqSA01.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                        //            Eq.Site[0]._EqSA01.START_FREQ(startFreq.ToString(), "MHz");
                        //            Eq.Site[0]._EqSA01.STOP_FREQ(stopFreq.ToString(), "MHz");
                        //            Eq.Site[0]._EqSA01.TRIGGER_CONTINUOUS();
                        //            break;
                        //        case 2:
                        //            Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.ON);
                        //            Eq.Site[0]._EqSA02.Measure_Setup(LibEqmtDriver.SA.N9020A_MEAS_TYPE.SweptSA);
                        //            DelayMs(1500);

                        //            Eq.Site[0]._EqSA02.SPAN(myUtility.MXA_Setting.Span / 1e6);        //Convert Hz To MHz
                        //            Eq.Site[0]._EqSA02.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                        //            Eq.Site[0]._EqSA02.VIDEO_BW(myUtility.MXA_Setting.VBW);
                        //            Eq.Site[0]._EqSA02.AMPLITUDE_REF_LEVEL(myUtility.MXA_Setting.RefLevel);
                        //            Eq.Site[0]._EqSA02.SWEEP_TIMES(myUtility.MXA_Setting.SweepT);
                        //            Eq.Site[0]._EqSA02.SWEEP_POINTS(myUtility.MXA_Setting.NoPoints);
                        //            Eq.Site[0]._EqSA02.AMPLITUDE_INPUT_ATTENUATION(myUtility.MXA_Setting.Attenuation);
                        //            Eq.Site[0]._EqSA02.START_FREQ(startFreq.ToString(), "MHz");
                        //            Eq.Site[0]._EqSA02.STOP_FREQ(stopFreq.ToString(), "MHz");
                        //            Eq.Site[0]._EqSA02.TRIGGER_CONTINUOUS();
                        //            break;
                        //        default:
                        //            MessageBox.Show("Wrong MXA Equipment selection : " + Measure_Channel + " , Only MXA 1 or 2 allow!!!");
                        //            break;
                        //    }

                        //    DelayMs(1000);
                        //    #endregion

                        //    #region NOISE MARKER CAL

                        //    noiseMKR_RBW = new double[tempdataMkr.Length];
                        //    for (int count = 0; count < tempdataMkr.Length; count++)
                        //    {
                        //        //Initialize Variable
                        //        tempFreq = string.Empty;
                        //        tempCalResult = string.Empty;
                        //        FreqListNF = new double[FreqListCountRF];
                        //        FreqListMKR = new double[FreqListCountRF];

                        //        //variable for display result
                        //        dispFreq = new string[FreqListCountRF];
                        //        dispCal = new string[FreqListCountRF];
                        //        dispResult = "";
                        //        iNewline = 0;
                        //        calStatus = false;
                        //        callimitStatus = true;
                        //        tmpCalHeader = "";

                        //        noiseMKR_RBW[count] = Convert.ToDouble(tempdataMkr[count]);
                        //        tmpCalHeader = Target_CalSegment + "_MKRNoise_" + noiseMKR_RBW[count] / 1e6 + "MHz";

                        //        #region Maker Noise Measurement

                        //        switch (Measure_Channel)
                        //        {
                        //            case 1:
                        //                Eq.Site[0]._EqSA01.RESOLUTION_BW(noiseMKR_RBW[count]);
                        //                Eq.Site[0]._EqSA01.MARKER_NOISE(true, markerNo, noiseMKR_RBW[count]);
                        //                break;
                        //            case 2:
                        //                Eq.Site[0]._EqSA02.RESOLUTION_BW(noiseMKR_RBW[count]);
                        //                Eq.Site[0]._EqSA02.MARKER_NOISE(true, markerNo, noiseMKR_RBW[count]);
                        //                break;
                        //            default:
                        //                break;
                        //        }

                        //        DelayMs(1000);

                        //        for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                        //        {
                        //            tempFreq += "," + FreqListRF[iCount];

                        //            switch (Measure_Channel)
                        //            {
                        //                case 1:
                        //                    Eq.Site[0]._EqSA01.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                        //                    FreqListMKR[iCount] = Math.Round(Eq.Site[0]._EqSA01.READ_MARKER(markerNo), 3);
                        //                    break;
                        //                case 2:
                        //                    Eq.Site[0]._EqSA02.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                        //                    FreqListMKR[iCount] = Math.Round(Eq.Site[0]._EqSA02.READ_MARKER(markerNo), 3);
                        //                    break;
                        //                default:
                        //                    break;
                        //            }
                        //        }
                        //        #endregion

                        //        #region Maker Noise Off Measurement
                        //        switch (Measure_Channel)
                        //        {
                        //            case 1:
                        //                Eq.Site[0]._EqSA01.MARKER_NOISE(false, markerNo);
                        //                break;
                        //            case 2:
                        //                Eq.Site[0]._EqSA02.MARKER_NOISE(false, markerNo);
                        //                break;
                        //            default:
                        //                break;
                        //        }

                        //        DelayMs(1000);

                        //        for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                        //        {
                        //            switch (Measure_Channel)
                        //            {
                        //                case 1:
                        //                    Eq.Site[0]._EqSA01.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                        //                    FreqListNF[iCount] = Math.Round(Eq.Site[0]._EqSA01.READ_MARKER(markerNo), 3);
                        //                    break;
                        //                case 2:
                        //                    Eq.Site[0]._EqSA02.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                        //                    FreqListNF[iCount] = Math.Round(Eq.Site[0]._EqSA02.READ_MARKER(markerNo), 3);
                        //                    break;
                        //                default:
                        //                    break;
                        //            }
                        //        }
                        //        #endregion

                        //        #region Calc Normalization offset
                        //        double dB_Hz = 10 * Math.Log10(noiseMKR_RBW[count]);      //convert RBW to dB/Hz
                        //        for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                        //        {
                        //            //offset = (Normalize Noise xMHz_RBW to dB/Hz) - Noise Marker at dB/Hz
                        //            power = (FreqListNF[iCount] - dB_Hz) - FreqListMKR[iCount];
                        //            tempCalResult += "," + Math.Round(power, 3);
                        //        }

                        //        //Display calibration result 
                        //        dispFreq = tempFreq.Split(',');
                        //        dispCal = tempCalResult.Split(',');
                        //        dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                        //        iNewline = 0;

                        //        for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                        //        {
                        //            dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                        //            iNewline++;

                        //            if (iNewline == 4)
                        //            {
                        //                dispResult += "\r\n";
                        //                iNewline = 0;
                        //            }
                        //        }

                        //        tmpMsgTxt = "\r\r\n *** MARKER NOISE Calibration Done *** " + "\n\r\r Press OK to Continue";
                        //        MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS - " + tmpCalHeader + " ***", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        //        //Write to file if CalLimitStatus is True
                        //        swCalDataFile.WriteLine("");
                        //        swCalDataFile.WriteLine(tmpCalHeader + tempFreq);
                        //        swCalDataFile.WriteLine(tempCalResult);

                        //        #endregion
                        //    }

                        //    #endregion

                        //    #region RX Pathgain/loss Cal
                        //    switch (Measure_Channel)
                        //    {
                        //        case 1:
                        //            Eq.Site[0]._EqSA01.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                        //            break;
                        //        case 2:
                        //            Eq.Site[0]._EqSA02.RESOLUTION_BW(myUtility.MXA_Setting.RBW);
                        //            break;
                        //        default:
                        //            break;
                        //    }
                        //    DelayMs(1000);

                        //    do
                        //    {
                        //        //Initialize Variable
                        //        tempFreq = string.Empty;
                        //        tempCalResult = string.Empty;

                        //        //variable for display result
                        //        dispFreq = new string[FreqListCountRF];
                        //        dispCal = new string[FreqListCountRF];
                        //        dispResult = "";
                        //        iNewline = 0;
                        //        calStatus = false;
                        //        callimitStatus = true;

                        //        for (int iCount = 0; iCount < FreqListCountRF; iCount++)
                        //        {
                        //            tempFreq += "," + FreqListRF[iCount];

                        //            Eq.Site[0]._EqSG01.SetFreq(Convert.ToDouble(FreqListRF[iCount]));
                        //            Eq.Site[0]._EqSG01.SetAmplitude((float)Convert.ToDouble(PowerLevel));
                        //            Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.ON);
                        //            DelayMs(200);

                        //            switch (Measure_Channel)
                        //            {
                        //                case 1:
                        //                    Eq.Site[0]._EqSA01.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA01.OPERATION_COMPLETE();
                        //                    power = Eq.Site[0]._EqSA01.READ_MARKER(markerNo) - Convert.ToDouble(PowerLevel) + CalOffset;
                        //                    break;
                        //                case 2:
                        //                    Eq.Site[0]._EqSA02.MARKER_TURN_ON_NORMAL_POINT(markerNo, (float)Convert.ToDouble(FreqListRF[iCount]));
                        //                    DelayMs(myUtility.MXA_Setting.SweepT);
                        //                    status = Eq.Site[0]._EqSA02.OPERATION_COMPLETE();
                        //                    power = Eq.Site[0]._EqSA02.READ_MARKER(markerNo) - Convert.ToDouble(PowerLevel) + CalOffset;
                        //                    break;
                        //                default:
                        //                    break;
                        //            }

                        //            //compare individual result with cal spec limit & set cal status flag
                        //            if ((power < CalLimitLow) || (power > CalLimitHigh))
                        //            {
                        //                callimitStatus = false;
                        //            }

                        //            tempCalResult += "," + Math.Round(power, 3);
                        //        }

                        //        //Display calibration result 
                        //        dispFreq = tempFreq.Split(',');
                        //        dispCal = tempCalResult.Split(',');
                        //        dispResult = "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "Freq,CalVal" + "   " + "\r\n";
                        //        iNewline = 0;

                        //        for (int iCount = 1; iCount < FreqListCountRF + 1; iCount++)
                        //        {
                        //            dispResult += dispFreq[iCount] + "," + dispCal[iCount] + "   ";
                        //            iNewline++;

                        //            if (iNewline == 4)
                        //            {
                        //                dispResult += "\r\n";
                        //                iNewline = 0;
                        //            }
                        //        }

                        //        if (callimitStatus)
                        //        {
                        //            tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                        //            DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        //            if (chkStatus == DialogResult.Yes)
                        //            {
                        //                calStatus = true;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + CalLimitHigh + " , LSL: " + CalLimitLow + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                        //            DialogResult chkStatus = MessageBox.Show(dispResult + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                        //            if (chkStatus == DialogResult.Cancel)
                        //            {
                        //                calStatus = true;
                        //            }
                        //        }

                        //    } while (!calStatus);

                        //    Eq.Site[0]._EqSG01.EnableRF(LibEqmtDriver.SG.INSTR_OUTPUT.OFF);

                        //    //Write to file if CalLimitStatus is True
                        //    if (callimitStatus)
                        //    {
                        //        swCalDataFile.WriteLine("");
                        //        swCalDataFile.WriteLine(Target_CalSegment + tempFreq);
                        //        swCalDataFile.WriteLine(tempCalResult);
                        //    }

                        //    #endregion

                        //    #endregion
                        //    break;

                        case "SKIP_CAL":
                            //do nothing , skip calibration process
                            break;
                    }

                    swCalDataFile.Close();
                }

            }

            if (MXA_DisplayEnable)
            {
                if (EqmtStatus.MXA01)
                {
                    Eq.Site[0]._EqSA01.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                }
                if (EqmtStatus.MXA02)
                {
                    Eq.Site[0]._EqSA02.Enable_Display(LibEqmtDriver.SA.N9020A_DISPLAY.OFF);
                }
            }

            MessageBox.Show("The PA calibration is finished.");
            //ATFCrossDomainWrapper.Cal_LoadCalData(LocalSetting.CalTag, Convert.ToString(DicCalInfo[CalPathRF]));
        }
        private void NF_Calibration(int Iteration, string NF_CalTag, double[] Freq, double[] DutInputLoss, double[] DutOutputLoss, double NF_Cal_HL, double NF_Cal_LL, double NF_BW)
        {

            double[] nf_Freq;
            double[] nf_Analyzer;
            double[] nf_ColdSourcePower;
            double maxVal, minVal;
            int maxIndex, minIndex;
            string tmpMsgTxt = "";
            bool calStatus = false;

            StreamWriter resultFile = new StreamWriter(calDir + NF_CalTag + ".csv");
            StringBuilder resultBuilder;
            StringBuilder dispMsgBuilder;

            do
            {
                resultBuilder = new StringBuilder();
                dispMsgBuilder = new StringBuilder();

                Eq.Site[0]._EqRFmx.cRFmxNF.CalibratioSpeNFCouldSource(Iteration, NF_CalTag, Freq, DutInputLoss, DutOutputLoss, NF_BW);

                nf_Freq = Eq.Site[0]._EqRFmx.cRFmxNF.frequencyListOut;
                nf_Analyzer = Eq.Site[0]._EqRFmx.cRFmxNF.analyserNoiseFigure;
                nf_ColdSourcePower = Eq.Site[0]._EqRFmx.cRFmxNF.coldSourcePower;

                maxVal = nf_Analyzer.Max();
                minVal = nf_Analyzer.Min();

                maxIndex = Array.IndexOf(nf_Analyzer, maxVal);
                minIndex = Array.IndexOf(nf_Analyzer, minVal);

                dispMsgBuilder.AppendLine("Analyzer NF Cal Result");
                dispMsgBuilder.AppendLine("Max Value: " + Math.Round(maxVal, 3) + "dB at Freq " + nf_Freq[maxIndex] / 1e6 + "Mhz");
                dispMsgBuilder.AppendLine("Min Value: " + Math.Round(minVal, 3) + "dB at Freq " + nf_Freq[minIndex] / 1e6 + "Mhz");

                resultBuilder.AppendLine("Freq,AnalyzerNoiseFigure,ColdSourcePower");
                for (int i = 0; i < nf_Analyzer.Length; i++)
                {
                    resultBuilder.AppendLine(nf_Freq[i] + "," + nf_Analyzer[i] + "," + nf_ColdSourcePower[i]);
                }

                if ((minVal > NF_Cal_LL) && (maxVal < NF_Cal_HL))
                {
                    if (ClothoDataObject.Instance.EnableOnlySeoulUser)
                    {
                        calStatus = true;
                    }
                    else
                    {
                        tmpMsgTxt = "\r\r\n *** Calibration Data PASS *** " + "\n\r\r Press YES to Save and Continue, NO to Redo Calibration";
                        DialogResult chkStatus = MessageBox.Show(dispMsgBuilder + tmpMsgTxt, "*** Calibration Data PASS ***", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (chkStatus == DialogResult.Yes)
                        {
                            calStatus = true;
                        }
                    }
                }

                else
                {
                    tmpMsgTxt = "\r\r\n *** Calibration Data FAIL *** " + "\r\n Calibration Data Fail Spec -> USL: " + NF_Cal_HL + " , LSL: " + NF_Cal_LL + "\n\r\r Press RETRY to redo Calibration , CANCEL to stop calibration";
                    DialogResult chkStatus = MessageBox.Show(dispMsgBuilder + tmpMsgTxt, "!!! Calibration Data FAIL !!!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);

                    if (chkStatus == DialogResult.Cancel)
                    {
                        calStatus = true;
                    }
                }

            } while (!calStatus);

            resultFile.Write(resultBuilder);
            resultFile.Close();

        }
    }
}
