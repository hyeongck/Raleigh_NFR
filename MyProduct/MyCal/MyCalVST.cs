using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using NationalInstruments.LabVIEW.Interop;
//using ni_NoiseFloor;
using System.Windows.Forms;
using ni_NoiseFloorWrapper;

namespace MyProduct.MyCal
{
    public class MyVSTCal
    {
        public NIRfsa rfsaSession;
        public NIRfsg _rfsgSession;
        string IOAddress;
        int NumberOfRecords;
        List<double> RFSA_IQCarrierFrequency = new List<double>();

        private string Address
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
        public MyVSTCal(string ioAddress)
        {
            Address = ioAddress;
        }
        MyVSTCal() { }

        public void initialize()
        {
            #region Initialize

            // Initialize the NIRfsa session
            rfsaSession = new NIRfsa(Address, true, false);

            // Initialize the NIRfsg session
            _rfsgSession = new NIRfsg(Address, true, false);

            #endregion
        }
        public void RFSAPreConfigure(double ReferenceLevel)
        {
            #region RFSAPreConfigure
            // Configure the reference clock source 
            rfsaSession.Configuration.ReferenceClock.Configure(NoiseTestConstants.ReferenceClockSource, 10e6);
            rfsaSession.Configuration.AcquisitionType = RfsaAcquisitionType.IQ;
            rfsaSession.Configuration.Vertical.ReferenceLevel = ReferenceLevel;
            rfsaSession.Configuration.Triggers.StartTrigger.DigitalEdge.Configure(NoiseTestConstants.RFSA_StartTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising);
            rfsaSession.Configuration.Triggers.ReferenceTrigger.DigitalEdge.Configure(NoiseTestConstants.RFSA_ReferenceTrigger_DigitalEdge_Source, RfsaTriggerEdge.Rising, NoiseTestConstants.RFSA_PreTriggerSamples);
            rfsaSession.Configuration.Triggers.AdvanceTrigger.DigitalEdge.Source = RfsaDigitalEdgeAdvanceTriggerSource.TimerEvent;
            rfsaSession.Configuration.BasicConfigurationList.TimerEventInterval = NoiseTestConstants.TimerEventInterval;
            rfsaSession.Utility.Commit();

            #endregion
        }
        public void RFSGPreConfigure(double refSiggenPwr)
        {
            #region RFSGPreConfigure

            // Configure the reference clock source 
            _rfsgSession.FrequencyReference.Configure(NoiseTestConstants.ReferenceClockSource, 10E6);
            _rfsgSession.Arb.GenerationMode = RfsgWaveformGenerationMode.Script;
            _rfsgSession.RF.PowerLevelType = RfsgRFPowerLevelType.PeakPower;
            // Configure the loop bandwidth 
            _rfsgSession.RF.Advanced.LoopBandwidth = NoiseTestConstants.loopBandwidth;

         
          //  _rfsgSession.Arb.PreFilterGain = a;
            _rfsgSession.Arb.PreFilterGain = NoiseTestConstants.ArbPreFilterGain;
            _rfsgSession.RF.PowerLevel = refSiggenPwr;

            var SG_Idata_LowCal = new List<double>();
            var SG_Qdata_LowCal = new List<double>();

            var SG_Idata_Idle = new List<double>();
            var SG_Qdata_Idle = new List<double>();

            for (int i = 0; i < 10000; i++)
            {
                SG_Idata_LowCal.Add(1);
                SG_Qdata_LowCal.Add(0);
            }

            for (int i = 0; i < 400; i++)
            {
                SG_Idata_Idle.Add(0);
                SG_Qdata_Idle.Add(0);
            }

            _rfsgSession.Arb.WriteWaveform("SignalLowCal", SG_Idata_LowCal.ToArray(), SG_Qdata_LowCal.ToArray());
            _rfsgSession.Arb.WriteWaveform("IdleLowCal", SG_Idata_Idle.ToArray(), SG_Qdata_Idle.ToArray());

            _rfsgSession.Utility.Commit();

            #endregion
        }
        public void VSTConfigure_DuringTest(double FreqStart, double FreqStop, double FreqStep, double rBW_Hz)
        {
            #region Variable
            double RX_FrequencyOffsetOut;
            #endregion

            #region RFSAConfigure_duringTest
            double constMultiplier_RX_IQRate = 1.25;
            double RX_IQRate = constMultiplier_RX_IQRate * rBW_Hz;

            //rfsaSession.Configuration.IQ.IQRate = 1.25e6;
            //rfsaSession.Configuration.IQ.NumberOfSamples = (int)(1.25e6 * NoiseTestConstants.TimePerRecord);

            rfsaSession.Configuration.IQ.IQRate = RX_IQRate;
            rfsaSession.Configuration.IQ.NumberOfSamples = (int)(RX_IQRate * NoiseTestConstants.TimePerRecord);

            FrequencyRamp(NoiseTestConstants.RX_FrequencyOffset, NoiseTestConstants.BandSelection, FreqStart, FreqStop, FreqStep, out RX_FrequencyOffsetOut, out NumberOfRecords, RFSA_IQCarrierFrequency);
            rfsaSession.Configuration.IQ.NumberOfRecords = NumberOfRecords;

            #region Local lambda functions
            
            // Create next rfsa configuration list step
            Action<double, double> CreateRfsaNextListStep = (double iqFreq, double downCF) =>
            {
                rfsaSession.Configuration.BasicConfigurationList.CreateStep(true);
                rfsaSession.Configuration.IQ.CarrierFrequency = iqFreq;
                rfsaSession.Acquisition.Advanced.DownconverterCenterFrequency = downCF;
            };

            // Evaluate if IQ frquency is within VST IBW range 
            Func<double, double, bool> IsWithinIbwRange = (double iqFreq, double centerFreq) =>
            {
                double maxIbw = rfsaSession.DeviceCharacteristics.MaxInstantaneousBandwidth;
                
                if (iqFreq < (centerFreq - (maxIbw / 2)))
                    return false;                
                else if (iqFreq > (centerFreq + maxIbw / 2))
                    return false;
                else
                    return true;
            };

            #endregion

            //***Configure Frequency Sweep List with Inband Retuning***
            double RFSA_LO_Offset_Cal = (FreqStart - 5e6); //To solve Downconverter frequency out of range issue; to maximize the in band tuning for 200MHz VST, Max RX range (Fstop - Fstart) = 98MHz 
            rfsaSession.Configuration.BasicConfigurationList.CreateConfigurationList("RFSA_List", NoiseTestConstants.RFSA_PropertyList, true);
            
            for (int i = 0; i < NumberOfRecords; i++)
            {
                double iqfreq = RFSA_IQCarrierFrequency[i] + RX_FrequencyOffsetOut;
                
                if (IsWithinIbwRange(iqfreq, RFSA_LO_Offset_Cal))
                    CreateRfsaNextListStep(iqfreq, RFSA_LO_Offset_Cal);
                else
                {
                    // Readjust downconverter frequency 
                    RFSA_LO_Offset_Cal = iqfreq - 5e6;
                    CreateRfsaNextListStep(iqfreq, RFSA_LO_Offset_Cal);
                }
            }

            try
            {
                rfsaSession.Utility.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in MyCalVST():\n" + ex.GetType() + ": " + ex.Message);
            }
            

            #endregion

            #region RFSGConfigure_duringTest

            //******RFSG Configuration******
            _rfsgSession.Arb.IQRate = 1e6;

            string Script =
              "script lowCal\r\n"
            + "generate IdleLowCal marker2(0)\r\n"
            + "repeat " + NumberOfRecords.ToString() + "\r\n"
            + "generate SignalLowCal marker1(0)\r\n"
            + "generate IdleLowcal marker0(0)\r\n"
            + "end repeat\r\n"
            + "end script";

            _rfsgSession.Arb.Scripting.WriteScript(Script);
            _rfsgSession.Arb.Scripting.SelectedScriptName = "lowCal";

            #region Local lambda functions

            // Create next rfsg configuration list step
            Action<double, double> CreateRfsgNextListStep = (double iqFreq, double upCF) =>
            {
                _rfsgSession.BasicConfigurationList.CreateStep(true);
                _rfsgSession.RF.Frequency = iqFreq;
                _rfsgSession.RF.Upconverter.CenterFrequency = upCF;
            };

            #endregion

            //***Configure Frequency Sweep List with Inband Retuning***
            RFSA_LO_Offset_Cal = (FreqStart - 5e6);  // Reset the LO offset 
            _rfsgSession.BasicConfigurationList.CreateConfigurationList("RFSG_List", NoiseTestConstants.RFSG_PropertyList, true);

            for (int i = 0; i < NumberOfRecords; i++)
            {
                double iqfreq = RFSA_IQCarrierFrequency[i] + RX_FrequencyOffsetOut;

                if (IsWithinIbwRange(iqfreq, RFSA_LO_Offset_Cal))
                    CreateRfsgNextListStep(iqfreq, RFSA_LO_Offset_Cal);
                else
                {
                    // Readjust upconverter frequency 
                    RFSA_LO_Offset_Cal = iqfreq - 5e6;
                    CreateRfsgNextListStep(iqfreq, RFSA_LO_Offset_Cal);
                }
            }
            
            //***Configure Trigger For List Mode Ping Pong***
            //// Export the marker event to the desired output terminal 
            _rfsgSession.DeviceEvents.MarkerEvents[2].ExportedOutputTerminal = NoiseTestConstants.RFSG_MarkerEvents_2_ExportedOutputTerminal;
            _rfsgSession.DeviceEvents.MarkerEvents[1].ExportedOutputTerminal = NoiseTestConstants.RFSG_MarkerEvents_1_ExportedOutputTerminal;

            // Configure the trigger type to advance steps in the list 
            _rfsgSession.Triggers.ConfigurationListStepTrigger.TriggerType = RfsgConfigurationListStepTriggerType.DigitalEdge;

            // Configure the trigger source to advance steps in the list 
            _rfsgSession.Triggers.ConfigurationListStepTrigger.DigitalEdge.Source = RfsgDigitalEdgeConfigurationListStepTriggerSource.Marker0Event;
            _rfsgSession.Triggers.ConfigurationListStepTrigger.ExportedOutputTerminal = NoiseTestConstants.RFSG_ConfigurationListStepTrigger_ExportedOutputTerminal;
            _rfsgSession.Utility.Commit();

            #endregion
        }
        public float[] measureLowNoiseCal(double vBW_Hz)
        {
            #region variable
            uint queue;
            var im = new List<float>();
            var re = new List<float>();
            ni_NoiseFloorWrapper.ni_NoiseFloorWrapper.Element[] complexData;
            //Element[] complexData;
            float[] traceResult;

            NationalInstruments.PrecisionTimeSpan timeout = new NationalInstruments.PrecisionTimeSpan(5.0);
            NationalInstruments.ComplexDouble[] data;
            Stopwatch tTime = new Stopwatch();
            #endregion

            #region Measurement
            //******Measurement******
            double Rx_IQrate = rfsaSession.Configuration.IQ.IQRate;
            double Rx_NumSample = rfsaSession.Configuration.IQ.NumberOfSamples;

            ni_NoiseFloorWrapper.ni_NoiseFloorWrapper.CreateQueue(out queue);
            //ni_NoiseFloor.ni_NoiseFloor.CreateQueue(out queue);
            double dt = 1 / Rx_IQrate;

            double TestTime1 = tTime.ElapsedMilliseconds;

            rfsaSession.Acquisition.IQ.Initiate();
            _rfsgSession.Initiate();

            for (int i = 0; i < NumberOfRecords; i++)
            {
                data = rfsaSession.Acquisition.IQ.FetchIQSingleRecordComplex<NationalInstruments.ComplexDouble>((long)i, (long)Rx_NumSample, timeout);

                complexData = new ni_NoiseFloorWrapper.ni_NoiseFloorWrapper.Element[(int)Rx_NumSample];
                //complexData = new Element[(int)Rx_NumSample];

                for (int j = 0; j < Rx_NumSample; j++)
                {
                    re.Add((float)data[j].Real);
                    im.Add((float)data[j].Imaginary);

                    complexData[j].re = data[j].Real;
                    complexData[j].im = data[j].Imaginary;
                }

                ni_NoiseFloorWrapper.ni_NoiseFloorWrapper.EnqueueData(queue, i, 0, dt, im.ToArray(), re.ToArray(), complexData);
                //ni_NoiseFloor.ni_NoiseFloor.EnqueueData(queue, i, 0, dt, im.ToArray(), re.ToArray(), complexData);
                re.Clear();
                im.Clear();
            }

            ni_NoiseFloorWrapper.ni_NoiseFloorWrapper.ProcessData(queue, RFSA_IQCarrierFrequency.ToArray(), Rx_IQrate, vBW_Hz, NumberOfRecords, out traceResult);
            //ni_NoiseFloor.ni_NoiseFloor.ProcessData(queue, RFSA_IQCarrierFrequency.ToArray(), Rx_IQrate, vBW_Hz, NumberOfRecords, out traceResult);
            
            //for debug purpose only - to save temp file
            //for (int i = 0; i < traceResult.Length; i++)
            //    File.AppendAllText(@"C:\Trace.xls", traceResult[i].ToString() + Environment.NewLine);

            float[] pathcalRslt = new float[NumberOfRecords];
            for (int i = 0; i < NumberOfRecords; i++)
            {
                pathcalRslt[i] = traceResult[i * (traceResult.Length / NumberOfRecords)];
            }

            //for debug purpose only - to save temp file
            //for (int i = 0; i < NumberOfRecords; i++)
            //    File.AppendAllText(@"C:\Users\AVG-NIVST\Desktop\NoiseTest_Avago\LowPowerCal\LowPowerCal.xls", temp[i].ToString() + Environment.NewLine);

            double TestTime5 = tTime.ElapsedMilliseconds;
            double measurementTime = TestTime5 - TestTime1;
            double avgTime = measurementTime / NoiseTestConstants.NumberOfRuns;

            //******Program End******
            rfsaSession.Acquisition.IQ.Abort();
            rfsaSession.Configuration.BasicConfigurationList.DeleteConfigurationList("RFSA_List");
            rfsaSession.Configuration.BasicConfigurationList.ActiveList = string.Empty;

            _rfsgSession.BasicConfigurationList.DeleteConfigurationList("RFSG_List");
            _rfsgSession.BasicConfigurationList.ActiveList = string.Empty;

            return pathcalRslt;         //return pathcal result

            #endregion
        }
        public void closeVST()
        {
            #region close

            rfsaSession.Close();
            rfsaSession = null;

            _rfsgSession.Close();
            _rfsgSession = null;

            #endregion
        }

        //Frequency Ramp function
        public static void FrequencyRamp(double RX_FrequencyOffset_In, string BandSelection, double FrequencyStart, double FrequencyEnd, double FrequencyStep, out double RX_FrequencyOffset_Out, out int Ramp_NumberOfSamples, List<double> FreqRamp)
        {
            double Ramp_Start = 0;
            double Ramp_End = 0;
            double Ramp_Step = 0;
            RX_FrequencyOffset_Out = 0;

            switch (BandSelection)
            {
                case "Custom":
                    {
                        RX_FrequencyOffset_Out = RX_FrequencyOffset_In;
                        Ramp_End = FrequencyEnd;
                        Ramp_Start = FrequencyStart;
                        Ramp_Step = FrequencyStep;
                        break;
                    }
            }

            Ramp_NumberOfSamples = (int)(1 + (Ramp_End - Ramp_Start) / (Ramp_Step));  //Note that if (Ramp_End - Ramp_Start) is not an integer multiple of Ramp_Step, then the (int) cast will truncate towards zero by default

            for (int i = 0; i < Ramp_NumberOfSamples; i++)
            {
                FreqRamp.Add(Ramp_Start + i * Ramp_Step);
            }
        }
    }
}
