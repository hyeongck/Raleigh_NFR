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
        public double CalcDelta(string testUsePrev, int rsltTag, bool abs)
        {
            double calcData = -999;
            double data_1 = -999;
            double data_2 = -999;
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            for (int j = 0; j < Results.Length; j++)
            {
                if (resultArray[0] == Results[j].TestNumber)
                {
                    data_1 = Results[j].Multi_Results[rsltTag].Result_Data;
                }
                if (resultArray[1] == Results[j].TestNumber)
                {
                    data_2 = Results[j].Multi_Results[rsltTag].Result_Data;
                }
            }

            if (abs)
            {
                calcData = Math.Abs(data_1 - data_2);
            }
            else
            {
                calcData = data_1 - data_2;
            }

            return calcData;
        }
        public double CalcSum(string testUsePrev, int rsltTag)
        {
            double calcData = -999;
            double data_1 = -999;
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            for (int i = 0; i < resultArray.Length; i++)
            {
                for (int j = 0; j < Results.Length; j++)
                {
                    if (resultArray[i] == Results[j].TestNumber)
                    {
                        if (i == 0)     //set start-up data before calculating
                        {
                            calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                        }
                        else
                        {
                            data_1 = Results[j].Multi_Results[rsltTag].Result_Data;
                            calcData = calcData + data_1;       //sum data
                        }
                    }
                }
            }

            return calcData;
        }
        public double CalcAverage(string testUsePrev, int rsltTag)
        {
            double calcData = -999;
            double data_1 = -999;
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            for (int i = 0; i < resultArray.Length; i++)
            {
                for (int j = 0; j < Results.Length; j++)
                {
                    if (resultArray[i] == Results[j].TestNumber)
                    {
                        if (i == 0)     //set start-up data before calculating
                        {
                            calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                        }
                        else
                        {
                            data_1 = Results[j].Multi_Results[rsltTag].Result_Data;
                            calcData = calcData + data_1;       //sum data
                        }
                    }
                }
            }

            calcData = calcData / resultArray.Length;       //calculate average

            return calcData;
        } 
        public double ReportRslt(string testUsePrev, int rsltTag)
        {
            //Note : testUsePrev must only be round number , no other character allowed
            double calcData = -999;
            for (int j = 0; j < Results.Length; j++)
            {
                if (testUsePrev == Results[j].TestNumber)
                {
                    calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                }
            }
            return calcData;
        }
        private double[] CalculatePowerRamp(double minPower, double maxPower, int sampleCount)
        {
            double[] ramp = new double[sampleCount];

            double step = (maxPower - minPower) / (sampleCount - 1);

            for (int i = 0; i < sampleCount; i++)
            {
                ramp[i] = minPower + i * step;
            }

            return ramp;
        }
        public void SearchMAXMIN(string testParam, string testUsePrev, string searchMethod, int rsltTag, out double calcData, out int rsltArrayNo)
        {
            calcData = -999;
            rsltArrayNo = 0;
            string[] resultArray;
            resultArray = testUsePrev.Split(',');

            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    for (int i = 0; i < resultArray.Length; i++)
                    {
                        for (int j = 0; j < Results.Length; j++)
                        {
                            if (resultArray[i] == Results[j].TestNumber)
                            {
                                if (i == 0)     //set start-up data before calculating
                                {
                                    calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                                    rsltArrayNo = j;    //pass out the arryno - to be use by NF MAX or MIN search
                                }
                                if (calcData < Results[j].Multi_Results[rsltTag].Result_Data)
                                {
                                    calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                                    rsltArrayNo = j;    //pass out the arryno - to be use by NF MAX or MIN search
                                }
                                break;      //get out of j loop 
                            }
                        }
                    }
                    break;

                case "MIN":
                    for (int i = 0; i < resultArray.Length; i++)
                    {
                        for (int j = 0; j < Results.Length; j++)
                        {
                            if (resultArray[i] == Results[j].TestNumber)
                            {
                                if (i == 0)     //set start-up data before calculating
                                {
                                    calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                                    rsltArrayNo = j;    //pass out the arryno - to be use by NF MAX or MIN search
                                }
                                if (calcData > Results[j].Multi_Results[rsltTag].Result_Data)
                                {
                                    calcData = Results[j].Multi_Results[rsltTag].Result_Data;
                                    rsltArrayNo = j;    //pass out the arryno - to be use by NF MAX or MIN search
                                }
                                break;      //get out of j loop
                            }
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Test Parameter : " + testParam + "(" + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    calcData = -999;
                    rsltArrayNo = 0;
                    break;
            }
        }
        //Get the pathloss data
        private void GetCalData_Array(out double[] rtnPathloss, out double[] rtnPathlossFreq, string calTag, string calSegm, double startFreq, double stopFreq, double stepFreq, string searchMethod, double optSearchValue = 1710)
        {
            double searchFreq = -999;
            double lossOutput = 999;
            string strError = null;
            double[] tmpPathloss;
            double[] tmpPathlossFreq;

            //Get pathloss base on start and stop freq
            int count = Convert.ToInt16((stopFreq - startFreq) / stepFreq) + 1;
            searchFreq = Math.Round(startFreq, 3);          //need to use round function because of C# float and double floating point bug/error

            //initialize array
            tmpPathloss = new double[count];
            tmpPathlossFreq = new double[count];
            rtnPathloss = new double[count];
            rtnPathlossFreq = new double[count];

            for (int i = 0; i < count; i++)
            {
                ATFCrossDomainWrapper.Cal_GetCalData1DCombined(calTag, calSegm, searchFreq, ref lossOutput, ref strError);
                tmpPathloss[i] = lossOutput;
                tmpPathlossFreq[i] = searchFreq;
                searchFreq = Math.Round(searchFreq + stepFreq, 3);      //need to use round function because of C# float and double floating point bug/error
            }

            //Sort out test result
            switch (searchMethod.ToUpper())
            {
                case "ALL":
                case "RANGE":
                    rtnPathloss = tmpPathloss;
                    rtnPathlossFreq = tmpPathlossFreq;
                    break;

                case "MAX":
                    rtnPathloss = new double[1];
                    rtnPathlossFreq = new double[1];
                    rtnPathloss[0] = tmpPathloss.Max();
                    rtnPathlossFreq[0] = tmpPathlossFreq[Array.IndexOf(tmpPathloss, rtnPathloss[0])];
                    break;

                case "MIN":
                    rtnPathloss = new double[1];
                    rtnPathlossFreq = new double[1];
                    rtnPathloss[0] = tmpPathloss.Min();
                    rtnPathlossFreq[0] = tmpPathlossFreq[Array.IndexOf(tmpPathloss, rtnPathloss[0])];
                    break;

                case "AVE":
                case "AVERAGE":
                    rtnPathloss = new double[1];
                    rtnPathlossFreq = new double[1];
                    rtnPathloss[0] = tmpPathloss.Average();
                    rtnPathlossFreq[0] = tmpPathlossFreq[0];          //return default freq i.e Start Freq
                    break;

                case "USER":
                    rtnPathloss = new double[1];
                    rtnPathlossFreq = new double[1];

                    //Note : this case required user to define freq that is within Start or Stop Freq and also same in step size
                    if ((optSearchValue >= startFreq) && (optSearchValue <= stopFreq))
                    {
                        try
                        {
                            rtnPathloss[0] = tmpPathloss[Array.IndexOf(tmpPathlossFreq, optSearchValue)];     //return contact power from same array number(of index number associated with 'USER' Freq)
                            rtnPathlossFreq[0] = optSearchValue;
                        }
                        catch       //if _Search_Value not in tmpPathlossFreq list , will return error . Eg. User Define 1840.5 but Freq List , 1839, 1840, 1841 - > program will fail because 1840.5 is not Exactly same in freq list
                        {
                            rtnPathloss[0] = 99999;
                            rtnPathlossFreq[0] = optSearchValue;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Function: GetCalData_Array" + "(SEARCH METHOD : " + searchMethod + ", USER DEFINE : " + optSearchValue + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    }
                    break;

                default:
                    MessageBox.Show("Function: GetCalData_Array" + "(SEARCH METHOD : " + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    break;
            }
        }
        private void GetCalData(out double rtnPathloss, out double rtnPathlossFreq, string calTag, string calSegm, double startFreq, double stopFreq, double stepFreq, string searchMethod, double optSearchValue = 1710)
        {
            double searchFreq = -999;
            double lossOutput = 999;
            string strError = null;
            double[] tmpPathloss;
            double[] tmpPathlossFreq;

            //Get pathloss base on start and stop freq
            int count = Convert.ToInt16((stopFreq - startFreq) / stepFreq) + 1;
            searchFreq = Math.Round(startFreq, 3);         //need to use round function because of C# float and double floating point bug/error

            //initialize array
            tmpPathloss = new double[count];
            tmpPathlossFreq = new double[count];
            rtnPathloss = 999;
            rtnPathlossFreq = -999;

            for (int i = 0; i < count; i++)
            {
                ATFCrossDomainWrapper.Cal_GetCalData1DCombined(calTag, calSegm, searchFreq, ref lossOutput, ref strError);
                tmpPathloss[i] = lossOutput;
                tmpPathlossFreq[i] = searchFreq;
                searchFreq = Math.Round(searchFreq + stepFreq, 3);      //need to use round function because of C# float and double floating point bug/error
            }

            //Sort out test result
            switch (searchMethod.ToUpper())
            {
                case "MAX":
                    rtnPathloss = tmpPathloss.Max();
                    rtnPathlossFreq = tmpPathlossFreq[Array.IndexOf(tmpPathloss, rtnPathloss)];
                    break;

                case "MIN":
                    rtnPathloss = tmpPathloss.Min();
                    rtnPathlossFreq = tmpPathlossFreq[Array.IndexOf(tmpPathloss, rtnPathloss)];
                    break;

                case "AVE":
                case "AVERAGE":
                    rtnPathloss = tmpPathloss.Average();
                    rtnPathlossFreq = tmpPathlossFreq[0];          //return default freq i.e Start Freq
                    break;

                case "USER":
                    //Note : this case required user to define freq that is within Start or Stop Freq and also same in step size
                    if ((optSearchValue >= startFreq) && (optSearchValue <= stopFreq))
                    {
                        try
                        {
                            rtnPathloss = tmpPathloss[Array.IndexOf(tmpPathlossFreq, optSearchValue)];     //return contact power from same array number(of index number associated with 'USER' Freq)
                            rtnPathlossFreq = optSearchValue;
                        }
                        catch       //if _Search_Value not in tmpPathlossFreq list , will return error . Eg. User Define 1840.5 but Freq List , 1839, 1840, 1841 - > program will fail because 1840.5 is not Exactly same in freq list
                        {
                            rtnPathloss = 999;
                            rtnPathlossFreq = optSearchValue;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Function: GetCalData" + "(SEARCH METHOD : " + searchMethod + ", USER DEFINE : " + optSearchValue + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    }
                    break;

                default:
                    MessageBox.Show("Function: GetCalData" + "(SEARCH METHOD : " + searchMethod + ") not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                    break;
            }
        }
    }
}
