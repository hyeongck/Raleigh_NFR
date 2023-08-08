using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using NationalInstruments.ModularInstruments.Interop;


namespace InstrLib
{
    public static class Smu
    {
        public static Dictionary<iSmu, ManualResetEvent> isChanInitiated = new Dictionary<iSmu, ManualResetEvent>();
        public static Dictionary<iSmu, ManualResetEvent> isChanMeasured = new Dictionary<iSmu, ManualResetEvent>();

        public static iSmu getSMU(string VisaAlias, string ChanNumber, string PinName, bool Reset)
        {
            iSmu smu;

            if (VisaAlias.Contains("4154"))
            {
                smu = new NI4154(VisaAlias, ChanNumber, PinName, Reset);
            }
            else if (VisaAlias.Contains("4139"))
            {
                smu = new NI4139(VisaAlias, ChanNumber, PinName, Reset);
            }
            else if (VisaAlias.Contains("4143") || VisaAlias.Contains("4141"))
            {
                smu = new NI4143(VisaAlias, ChanNumber, PinName, Reset);
            }
            else if (VisaAlias.ToUpper().Contains("6556"))
            {
                smu = new NI6556Pmu(VisaAlias, ChanNumber, PinName);
            }
            else if (VisaAlias.ToUpper().Contains("6570")) //*//CHANGE
            {
                smu = new NI6570Pmu(VisaAlias, ChanNumber, PinName);
            }
            else if (VisaAlias.ToUpper().Contains("9195"))
            {
                smu = new Ks9195Pmu(VisaAlias, ChanNumber, PinName);
            }
            else
            {
                throw new Exception("Visa Alias \"" + VisaAlias + "\" is not in a recognized format.\nValid SMU Visa Aliases must include one of the following:\n"
                    + "\n\"4154\""
                    + "\n\"4139\""
                    + "\n\"4143\""
                    + "\n\"4141\""
                    + "\n\"6556\""
                    + "\n\"6570\"" //*//CHANGE
                    + "\n\"9195\""
                    + "\n\nFor example, Visa Alias \"SMU_NI4143_02\" will be recognized as an NI 4143 module.");
            }

            isChanInitiated.Add(smu, new ManualResetEvent(false));
            isChanMeasured.Add(smu, new ManualResetEvent(false));

            return smu;
        }

        public class NI4154 : iSmu
        {
            public nidcpower SMUsession;
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            public NI4154(string VisaAlias, string ChanNumber, string PinName, bool Reset)
            {
                try
                {
                    this.SMUsession = new NationalInstruments.ModularInstruments.Interop.nidcpower(VisaAlias, ChanNumber, Reset, "");

                    this.VisaAlias = VisaAlias;
                    this.ChanNumber = ChanNumber;
                    this.PinName = PinName;

                    string model = SMUsession.GetString(NationalInstruments.ModularInstruments.Interop.nidcpowerProperties.InstrumentModel);

                    SMUsession.SetDouble(nidcpowerProperties.SourceDelay, ChanNumber, 0.00003);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, false);
                    SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
                    SMUsession.ConfigureSense(ChanNumber, nidcpowerConstants.Remote);
                    SMUsession.SetInt32(nidcpowerProperties.CurrentLimitAutorange, ChanNumber, nidcpowerConstants.On);
                    SMUsession.SetInt32(nidcpowerProperties.VoltageLevelAutorange, ChanNumber, nidcpowerConstants.On);
                    SMUsession.ConfigureVoltageLevel(ChanNumber, 0);
                    SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, 0.01);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, true);
                    SMUsession.Initiate();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SMU Initialize");
                }
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                int error = -1;
                int chanInt = Convert.ToInt16(ChanNumber);

                try
                {
                    if (currentLimit != priorCurrentLim)
                    {
                        // ConfigureCurrentLimit appears to also set the range automatically, because auto-range is on
                        error = SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, currentLimit);
                        priorCurrentLim = currentLimit;

                        //double readRange = SMUsession.GetDouble(nidcpowerProperties.CurrentLimit);
                    }

                    if (voltsForce != priorVoltage)
                    {
                        error = SMUsession.ConfigureVoltageLevel(ChanNumber, voltsForce);
                        priorVoltage = voltsForce;
                    }


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ForceVoltage4154");
                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {
                try
                {
                    SMUsession.Abort();

                    double measureTimeLength = 0;

                    if (UseIQsettings)
                    {
                        measureTimeLength = IQ.ActiveWaveform.FinalServoMeasTime;
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                        SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);   // "MeasureRecordLength" doesn't wait to re-trigger, just measures all immediately after 1st trigger. So, we have to set it to 1.
                    }
                    else
                    {
                        measureTimeLength = 0.00005;                     
                        
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);
                    }

                    double dcSampleRate = 200e3;   // this is fixed for NI hardware
                    int SamplesToAvg = (int)(dcSampleRate * measureTimeLength);

                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, SamplesToAvg);

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentMeasure4154");

                }
            }

            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volt = new double[NumAverages];
                    double[] curr = new double[NumAverages];
                    ushort[] measComp = new ushort[NumAverages];
                    int actCount = 0;
                    double[] voltSingle = new double[1];
                    double[] currSingle = new double[1];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        if (triggered)
                        {
                            error = SMUsession.FetchMultiple(ChanNumber, 1, 1, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger

                            volt[avg] = voltSingle[0];
                            curr[avg] = currSingle[0];
                        }
                        else
                        {
                            error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureCurrent, out curr[avg]);
                        }
                    }

                    Result = curr.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent4154");

                }
            }

            int NumTraceSamples;
            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    double dcSampleRate = 200e3;   // this is fixed for NI hardware
                    NumTraceSamples = (int)(dcSampleRate * measureTimeLength);

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);

                    if (triggered)
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                    }
                    else
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.SoftwareEdge);
                    }

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, NumTraceSamples);
                    SMUsession.SetDouble(nidcpowerProperties.SourceDelay, ChanNumber, 0.001);

                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, 1);

                    SMUsession.Initiate();

                    SMUsession.WaitForEvent(nidcpowerConstants.SourceCompleteEvent, 0.02);

                    if (!triggered) SMUsession.SendSoftwareEdgeTrigger(nidcpowerConstants.MeasureTrigger);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentTraceMeasurement4154");

                }
            }

            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }

            public double[] MeasureCurrentTrace()
            {
                int error = -1;

                try
                {
                    ushort[] measComp = new ushort[NumTraceSamples];
                    int actCount = 0;
                    double[] voltSingle = new double[NumTraceSamples];
                    double[] currSingle = new double[NumTraceSamples];

                    error = SMUsession.FetchMultiple(ChanNumber, 1, NumTraceSamples, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger

                    return currSingle;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrentTrace4154");
                    return new double[4];
                }
            }

            public void SetupVoltageMeasure()
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    double measureTimeLength = 0.001;

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

                    double dcSampleRate = 200e3;   // this is fixed for NI hardware
                    int SamplesToAvg = (int)(dcSampleRate * measureTimeLength);

                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, SamplesToAvg);

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupVoltageMeasure4154");

                }
            }
            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volts = new double[NumAverages];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureVoltage, out volts[avg]);
                    }

                    Result = volts.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureVoltage4154");

                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {

            }

            public void PostLeakageTest()
            {

            }

            public void SetupContinuity(double currentForce)
            {
                throw new NotImplementedException();
            }

            public void MeasureContinuity(int avgs, ref double result)
            {
                throw new NotImplementedException();
            }

            public void CloseSession()
            {
                SMUsession.Abort();
                SMUsession.reset();
                SMUsession.Dispose();
            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                dblTemp = 0;
            }

            public void DeviceSelfCal()
            {
              
            }

        }

        public class NI4143 : iSmu
        {
            public nidcpower SMUsession;
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            public NI4143(string VisaAlias, string ChanNumber, string PinName, bool Reset)
            {
                try
                {
                    this.SMUsession = new NationalInstruments.ModularInstruments.Interop.nidcpower(VisaAlias, ChanNumber, Reset, "");

                    this.VisaAlias = VisaAlias;
                    this.ChanNumber = ChanNumber;
                    this.PinName = PinName;

                    string model = SMUsession.GetString(NationalInstruments.ModularInstruments.Interop.nidcpowerProperties.InstrumentModel);

                    SMUsession.SetDouble(nidcpowerProperties.SourceDelay, ChanNumber, 0);
                    SMUsession.SetDouble(nidcpowerProperties.PowerLineFrequency, ChanNumber, nidcpowerConstants._60Hertz);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, false);
                    SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
                    SMUsession.ConfigureSense(ChanNumber, nidcpowerConstants.Remote);
                    SMUsession.SetInt32(nidcpowerProperties.CurrentLimitAutorange, ChanNumber, nidcpowerConstants.On);
                    SMUsession.SetInt32(nidcpowerProperties.VoltageLevelAutorange, ChanNumber, nidcpowerConstants.On);
                    SMUsession.ConfigureVoltageLevel(ChanNumber, 0);
                    SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, 0.001);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, true);
                    SMUsession.SetDouble(nidcpowerProperties.VoltageCompensationFrequency, 500);
                    SMUsession.SetDouble(nidcpowerProperties.VoltageGainBandwidth, 20000);
                    SMUsession.SetDouble(nidcpowerProperties.VoltagePoleZeroRatio, 8);
                    SMUsession.Initiate();

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SMU Initialize");
                }
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                int error = -1;
                int chanInt = Convert.ToInt16(ChanNumber);

                try
                {
                    if (currentLimit != priorCurrentLim)
                    {
                        // ConfigureCurrentLimit appears to also set the range automatically, because auto-range is on
                        error = SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, currentLimit);
                        priorCurrentLim = currentLimit;

                        //double readRange = SMUsession.GetDouble(nidcpowerProperties.CurrentLimit);
                    }
                    if (voltsForce != priorVoltage)
                    {
                        error = SMUsession.ConfigureVoltageLevel(ChanNumber, voltsForce);
                        priorVoltage = voltsForce;
                    }


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ConfigureCurrentLimit");
                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

                    if (UseIQsettings)
                    {
                        SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, IQ.ActiveWaveform.FinalServoMeasTime);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                    }
                    else
                    {
                        SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, 0.0005);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                    }

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentMeasure4143");

                }
            }
            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volt = new double[NumAverages];
                    double[] curr = new double[NumAverages];
                    ushort[] measComp = new ushort[NumAverages];
                    int actCount = 0;
                    double[] voltSingle = new double[1];
                    double[] currSingle = new double[1];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        if (triggered)
                        {
                            error = SMUsession.FetchMultiple(ChanNumber, 1, 1, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger

                            volt[avg] = voltSingle[0];
                            curr[avg] = currSingle[0];
                        }
                        else
                        {
                            error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureCurrent, out curr[avg]);
                        }
                    }

                    Result = curr.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show("PinName: " + PinName + "\n\nVisaAlias: " + VisaAlias + "\n\nChannel: " + ChanNumber + "\n\n" + e.ToString(), "MeasureCurrent");
                }
            }

            int NumTraceSamples;
            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                try
                {
                    SMUsession.Abort();

                    SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, aperture);
                    aperture = SMUsession.GetDouble(nidcpowerProperties.ApertureTime);

                    NumTraceSamples = (int)(measureTimeLength / aperture);

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);

                    if (triggered)
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                    }
                    else
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.SoftwareEdge);
                    }

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, NumTraceSamples);
                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, 1);

                    SMUsession.Initiate();

                    if (!triggered) SMUsession.SendSoftwareEdgeTrigger(nidcpowerConstants.MeasureTrigger);

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentTraceMeasurement4143");

                }
            }

            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }

            public double[] MeasureCurrentTrace()
            {
                int error = -1;

                try
                {
                    ushort[] measComp = new ushort[NumTraceSamples];
                    int actCount = 0;
                    double[] voltSingle = new double[NumTraceSamples];
                    double[] currSingle = new double[NumTraceSamples];

                    error = SMUsession.FetchMultiple(ChanNumber, 1, NumTraceSamples, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger

                    return currSingle;
                }
                catch (Exception e)
                {
                    MessageBox.Show("PinName: " + PinName + "\n\nVisaAlias: " + VisaAlias + "\n\nChannel: " + ChanNumber + "\n\n" +  e.ToString(), "MeasureCurrentTrace");
                    return new double[4];
                }
            }

            public void SetupVoltageMeasure()
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    double measureTimeLength = 0.001;

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

                    double dcSampleRate = 200e3;   // this is fixed for NI hardware
                    int SamplesToAvg = (int)(dcSampleRate * measureTimeLength);

                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, SamplesToAvg);

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupVoltageMeasure4154");

                }
            }
            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volts = new double[NumAverages];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureVoltage, out volts[avg]);
                    }

                    Result = volts.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureVoltage");

                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {
                SMUsession.Abort();
                SMUsession.SetInt32(nidcpowerProperties.TransientResponse, nidcpowerConstants.Custom);
                SMUsession.Initiate();
            }
            public void PostLeakageTest()
            {
                SMUsession.Abort();
                SMUsession.SetInt32(nidcpowerProperties.TransientResponse, nidcpowerConstants.Normal);
                SMUsession.Initiate();
            }

            public void SetupContinuity(double currentForce)
            {
                SMUsession.Abort();
                SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcCurrent);
                SMUsession.ConfigureCurrentLevel(ChanNumber, currentForce);
                SMUsession.ConfigureCurrentLevelRange(ChanNumber, Math.Abs(currentForce));

                SetupCurrentMeasure(false, false);
            }

            public void MeasureContinuity(int avgs, ref double result)
            {
                MeasureVoltage(avgs, ref result);

                SMUsession.Abort();
                SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
            }

            public void CloseSession()
            {

            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                dblTemp = 0;
            }

            public void DeviceSelfCal()
            {

            }
        }

        public class NI4139 : iSmu
        {
            public nidcpower SMUsession;
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            public NI4139(string VisaAlias, string ChanNumber, string PinName, bool Reset)
            {
                try
                {
                    this.SMUsession = new NationalInstruments.ModularInstruments.Interop.nidcpower(VisaAlias, ChanNumber, Reset, "");

                    this.VisaAlias = VisaAlias;
                    this.ChanNumber = ChanNumber;
                    this.PinName = PinName;

                    string model = SMUsession.GetString(NationalInstruments.ModularInstruments.Interop.nidcpowerProperties.InstrumentModel);

                    SMUsession.SetDouble(nidcpowerProperties.SourceDelay, ChanNumber, 0);
                    SMUsession.SetDouble(nidcpowerProperties.PowerLineFrequency, ChanNumber, nidcpowerConstants._50Hertz);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, false);
                    SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
                    SMUsession.ConfigureSense(ChanNumber, nidcpowerConstants.Remote);
                    SMUsession.SetInt32(nidcpowerProperties.CurrentLimitAutorange, ChanNumber, nidcpowerConstants.On);
                    SMUsession.SetInt32(nidcpowerProperties.VoltageLevelAutorange, ChanNumber, nidcpowerConstants.Off);
                    SMUsession.ConfigureVoltageLevel(ChanNumber, 0);
                    SMUsession.ConfigureVoltageLevelRange(ChanNumber, 6);
                    SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, 0.001);
                    SMUsession.ConfigureOutputEnabled(ChanNumber, true);
                    SMUsession.SetDouble(nidcpowerProperties.VoltageCompensationFrequency, 500);
                    SMUsession.SetDouble(nidcpowerProperties.VoltageGainBandwidth, 20000);
                    SMUsession.SetDouble(nidcpowerProperties.VoltagePoleZeroRatio, 8);
                    SMUsession.Initiate();

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SMU Initialize");
                }
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                int error = -1;
                int chanInt = Convert.ToInt16(ChanNumber);

                try
                {
                    if (currentLimit != priorCurrentLim)
                    {
                        // ConfigureCurrentLimit appears to also set the range automatically, because auto-range is on
                        error = SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, currentLimit);
                        priorCurrentLim = currentLimit;

                        //double readRange = SMUsession.GetDouble(nidcpowerProperties.CurrentLimit);
                    }
                    if (voltsForce != priorVoltage)
                    {
                        error = SMUsession.ConfigureVoltageLevel(ChanNumber, voltsForce);
                        priorVoltage = voltsForce;
                    }


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ConfigureCurrentLimit");
                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

                    if (UseIQsettings)
                    {
                        SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, IQ.ActiveWaveform.FinalServoMeasTime);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                    }
                    else
                    {
                        if(PinName =="Vrx")
                            SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, 0.01);
                        else
                            SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, 0.0005);
                        SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                    }

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentMeasure4139");

                }
            }
            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volt = new double[NumAverages];
                    double[] curr = new double[NumAverages];
                    ushort[] measComp = new ushort[NumAverages];
                    int actCount = 0;
                    double[] voltSingle = new double[1];
                    double[] currSingle = new double[1];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        if (triggered)
                        {
                            error = SMUsession.FetchMultiple(ChanNumber, 1, 1, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger
                            
                            volt[avg] = voltSingle[0];
                            curr[avg] = currSingle[0];
                        }
                        else
                        {
                            error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureCurrent, out curr[avg]);
                        }
                    }

                    Result = curr.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show("PinName: " + PinName + "\n\nVisaAlias: " + VisaAlias + "\n\nChannel: " + ChanNumber + "\n\n" + e.ToString(), "MeasureCurrent");

                }
            }

            int NumTraceSamples;
            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                try
                {
                    SMUsession.Abort();

                    SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, aperture);
                    aperture = SMUsession.GetDouble(nidcpowerProperties.ApertureTime);

                    NumTraceSamples = (int)(measureTimeLength / aperture);

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);

                    if (triggered)
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig1");
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig0");
                    }
                    else
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.SoftwareEdge);
                    }

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, NumTraceSamples);
                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, 1);

                    SMUsession.Initiate();

                    if (!triggered) SMUsession.SendSoftwareEdgeTrigger(nidcpowerConstants.MeasureTrigger);

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentTraceMeasurement4139");

                }
            }
            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                try
                {
                    SMUsession.Abort();

                    SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, aperture);
                    aperture = SMUsession.GetDouble(nidcpowerProperties.ApertureTime);

                    NumTraceSamples = (int)(measureTimeLength / aperture);

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnMeasureTrigger);

                    if (triggered)
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.DigitalEdge);
                        //if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig2"); //NI6556
                        //else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig2");
                        if (triggerFromSA) SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig3");   //NI6570
                        else SMUsession.SetString(nidcpowerProperties.DigitalEdgeMeasureTriggerInputTerminal, "PXI_Trig3");
                    }
                    else
                    {
                        SMUsession.SetInt32(nidcpowerProperties.MeasureTriggerType, nidcpowerConstants.SoftwareEdge);
                    }

                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, NumTraceSamples);
                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, 1);

                    SMUsession.Initiate();

                    if (!triggered) SMUsession.SendSoftwareEdgeTrigger(nidcpowerConstants.MeasureTrigger);

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupCurrentTraceMeasurement4139");

                }
            }
            public double[] MeasureCurrentTrace()
            {
                int error = -1;

                try
                {
                    ushort[] measComp = new ushort[NumTraceSamples];
                    int actCount = 0;
                    double[] voltSingle = new double[NumTraceSamples];
                    double[] currSingle = new double[NumTraceSamples];

                    error = SMUsession.FetchMultiple(ChanNumber, 1, NumTraceSamples, voltSingle, currSingle, measComp, out actCount);  // "Count" doesn't wait to re-trigger, just measures all immediately after 1st trigger

                    return currSingle;
                }
                catch (Exception e)
                {
                    MessageBox.Show("PinName: " + PinName + "\n\nVisaAlias: " + VisaAlias + "\n\nChannel: " + ChanNumber + "\n\n" + e.ToString(), "MeasureCurrentTrace");
                    return new double[4];
                }
            }

            public void SetupVoltageMeasure()
            {
                int error = -1;

                try
                {
                    SMUsession.Abort();

                    double measureTimeLength = 0.001;

                    SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                    SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

                    double dcSampleRate = 200e3;   // this is fixed for NI hardware
                    int SamplesToAvg = (int)(dcSampleRate * measureTimeLength);

                    SMUsession.SetInt32(nidcpowerProperties.SamplesToAverage, SamplesToAvg);

                    SMUsession.Initiate();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "SetupVoltageMeasure4154");

                }
            }
            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                int error = -1;

                try
                {
                    double[] volts = new double[NumAverages];

                    for (int avg = 0; avg < NumAverages; avg++)
                    {
                        error = SMUsession.Measure(ChanNumber, nidcpowerConstants.MeasureVoltage, out volts[avg]);
                    }

                    Result = volts.Average();


                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureVoltage4154");

                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {
                SMUsession.Abort();
                SMUsession.SetInt32(nidcpowerProperties.TransientResponse, nidcpowerConstants.Custom);
                SMUsession.Initiate();
            }
            public void PostLeakageTest()
            {
                SMUsession.Abort();
                SMUsession.SetInt32(nidcpowerProperties.TransientResponse, nidcpowerConstants.Normal);
                SMUsession.Initiate();
            }

            public void SetupContinuity(double currentForce)
            {
                SMUsession.Abort();
                SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcCurrent);
                SMUsession.ConfigureCurrentLevel(ChanNumber, currentForce);
                SMUsession.ConfigureCurrentLevelRange(ChanNumber, Math.Abs(currentForce));

                SetupCurrentMeasure(false, false);
            }

            public void MeasureContinuity(int avgs, ref double result)
            {
                MeasureVoltage(avgs, ref result);

                SMUsession.Abort();
                SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
            }

            public void CloseSession()
            {
                SMUsession.Abort();
                SMUsession.reset();
                SMUsession.Dispose();
            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                SMUsession.ReadCurrentTemperature(out dblTemp);
            }

            public void DeviceSelfCal()
            {
                SMUsession.CalSelfCalibrate(ChanNumber);

                SMUsession.SetDouble(nidcpowerProperties.SourceDelay, ChanNumber, 0);
                SMUsession.SetDouble(nidcpowerProperties.PowerLineFrequency, ChanNumber, nidcpowerConstants._50Hertz);
                SMUsession.ConfigureOutputEnabled(ChanNumber, false);
                SMUsession.ConfigureOutputFunction(ChanNumber, nidcpowerConstants.DcVoltage);
                SMUsession.ConfigureSense(ChanNumber, nidcpowerConstants.Remote);
                SMUsession.SetInt32(nidcpowerProperties.CurrentLimitAutorange, ChanNumber, nidcpowerConstants.On);
                SMUsession.SetInt32(nidcpowerProperties.VoltageLevelAutorange, ChanNumber, nidcpowerConstants.On);
                SMUsession.ConfigureVoltageLevel(ChanNumber, 0);
                SMUsession.ConfigureCurrentLimit(ChanNumber, nidcpowerConstants.CurrentRegulate, 0.001);
                SMUsession.ConfigureOutputEnabled(ChanNumber, true);
                //SMUsession.Initiate();
            }
        }


        public class NI6556Pmu : iSmu
        {
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            public NI6556Pmu(string VisaAlias, string ChanNumber, string PinName)
            {
                this.VisaAlias = VisaAlias;
                this.ChanNumber = ChanNumber;
                this.PinName = PinName;
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                try
                {
                    HSDIO.NI.GenSession.STPMU_SourceVoltage(ChanNumber, voltsForce, niHSDIOConstants.StpmuLocalSense, currentLimit);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ForceVoltage");
                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {

            }
            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];
                    int i = 0;

                    HSDIO.NI.GenSession.STPMU_MeasureCurrent(ChanNumber, 0.0001 * (double)NumAverages, meas, out i);
                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent");
                }
            }

            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }
            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }

            public double[] MeasureCurrentTrace()
            {
                throw new NotImplementedException();
            }

            public void SetupVoltageMeasure()
            {
            }
            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];
                    int i = 0;

                    HSDIO.NI.GenSession.STPMU_MeasureVoltage(ChanNumber, 0.0001 * (double)NumAverages, niHSDIOConstants.StpmuLocalSense, meas, out i);
                   
                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent");
                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {
            }
            public void PostLeakageTest()
            {
                if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    HSDIO.NI.GenSession.STPMU_DisablePMU(ChanNumber, niHSDIOConstants.StpmuReturnToPrevious);
                }
            }

            public void SetupContinuity(double currentForce)
            {
                HSDIO.NI.GenSession.STPMU_SourceCurrent(ChanNumber, currentForce, Math.Abs(currentForce), -2, 1);
            }
            public void MeasureContinuity(int avgs, ref double result)
            {
                MeasureVoltage(avgs, ref result);
                if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    HSDIO.NI.GenSession.STPMU_DisablePMU(ChanNumber, niHSDIOConstants.StpmuReturnToPrevious);
                }
            }

            public void CloseSession()
            {
                
            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                dblTemp = 0;
            }

            public void DeviceSelfCal()
            {

            }
        }

        //*//CHANGE
        public class NI6570Pmu : iSmu
        {
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            private double measureTimeLength;

            public NI6570Pmu(string VisaAlias, string ChanNumber, string PinName)
            {
                this.VisaAlias = VisaAlias;
                this.ChanNumber = ChanNumber;
                this.PinName = PinName.ToUpper();
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                try
                {
                    // Configure 6570 for PPMU measurements, Output Voltage, Measure Current
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
                    pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCVoltage;
                    // Force Voltage Configure
                    pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;

                    // Using the requested current limit to decide the current level range from the values supported for private release of 6570
                    double range = currentLimit;
                    if (Math.Abs(range) < 2e-6) { range = 2e-6; } // +-2uA
                    else if (Math.Abs(range) < 32e-6) { range = 32e-6; } // +-32uA
                    else if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
                    else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
                    else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

                    pin.Ppmu.DCCurrent.CurrentLevelRange = range;
                    pin.Ppmu.DCVoltage.CurrentLimitRange = range;
                    // Perform Voltage Force
                    pin.Ppmu.Source();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ForceVoltage");
                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {
            }

            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];

                    // Measure Current
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    //CM Edited: Added aperture time configuration
                    pin.Ppmu.ConfigureApertureTime(0.0001 * (double)NumAverages, NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);
                    
                    meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Current);
                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent");
                }
            }

            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                this.measureTimeLength = measureTimeLength;
            }
            
            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }

            public double[] MeasureCurrentTrace()
            {
                try
                {
                    List<double> measurements = new List<double>();

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    while (sw.ElapsedMilliseconds <= measureTimeLength * 1000 || measurements.Count() < 1)
                    {
                        double result = 0;
                        MeasureCurrent(1, false, ref result);
                        measurements.Add(result);
                    }

                    return measurements.ToArray();
                }
                catch (Exception e)
                {
                    return new double[16];
                }

            }

            public void SetupVoltageMeasure()
            {
                // Configure for PPMU Measurements
                NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
            }

            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];
                    // Configure Number of Averages by setting the Apperture Time
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.Ppmu.ConfigureApertureTime(0.0020 * (double)(NumAverages), NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);
                   
                    // Measure Voltage
                    meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Voltage);

                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent");
                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {
                NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
            }

            public void PostLeakageTest()
            {
                //if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Digital;
                }
            }

            public void SetupContinuity(double currentForce)
            {
                // Configure for PPMU measurements, Output Current, Measure Voltage
                SetupVoltageMeasure();
                NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCCurrent;
               
                // Using the requested current to decide the current level range from the values supported for private release of 6570
                double range = currentForce;
                if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
                else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
                else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

                // Set the current level range and voltage limits
                pin.Ppmu.DCCurrent.CurrentLevelRange = Math.Abs(range);
                pin.Ppmu.DCCurrent.VoltageLimitHigh = 1;    //Original: 5
                pin.Ppmu.DCCurrent.VoltageLimitLow = -2;
                // Configure Current Level and begin Sourcing
                pin.Ppmu.DCCurrent.CurrentLevel = currentForce;
                pin.Ppmu.Source();
            }

            public void MeasureContinuity(int avgs, ref double result)
            {
                MeasureVoltage(avgs, ref result);

                //if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Digital;
                }
            }
        
            public void CloseSession()
            {

            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                dblTemp = 0;
            }

            public void DeviceSelfCal()
            {

            }
        }
        
        public class Ks9195Pmu : iSmu
        {
            public string VisaAlias { get; set; }
            public string ChanNumber { get; set; }
            public string PinName { get; set; }
            public double priorVoltage { get; set; }
            public double priorCurrentLim { get; set; }

            private double currentForce;
            private bool ppmuIsActive;
            private double measureTimeLength;

            public Ks9195Pmu(string VisaAlias, string ChanNumber, string PinName)
            {
                this.VisaAlias = VisaAlias;
                this.ChanNumber = ChanNumber;
                this.PinName = PinName;
            }

            public void ForceVoltage(double voltsForce, double currentLimit)
            {
                try
                {
                    ActivatePPMU(true);

                    ////HSDIO.Keysight.driver.PpmuSites.Item[PinName].ForceVoltageMeasureCurrent(voltsForce, currentLimit);
                    //HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].Force(Keysight.KtMDsr.Interop.KtMDsrPpmuSiteForceModeEnum.KtMDsrPpmuSiteForceModeVoltage, voltsForce);

                    priorVoltage = voltsForce;
                    priorCurrentLim = currentLimit;
                }
                catch (Exception e)
                {

                }
            }

            public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
            {
                ActivatePPMU(true);
            }

            public void MeasureCurrent(int NumAverages, bool triggered, ref double Result)
            {
                try
                {
                    //double[] results = HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].ForceVoltageMeasureCurrent(priorVoltage, priorCurrentLim);
                    //Result = results[0];
                }
                catch (Exception e)
                {

                }
            }

            public void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                ActivatePPMU(true);
                this.measureTimeLength = measureTimeLength;
            }

            public void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
            {
                throw new NotImplementedException();
            }
            
            public double[] MeasureCurrentTrace()
            {
                try
                {
                    List<double> measurements = new List<double>();

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    while (sw.ElapsedMilliseconds <= measureTimeLength * 1000 || measurements.Count() < 1)
                    {
                        //double[] results = HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].ForceVoltageMeasureCurrent(priorVoltage, priorCurrentLim);
                        //measurements.Add(results[0]);
                    }

                    return measurements.ToArray();
                }
                catch (Exception e)
                {
                    return new double[16];
                }
            }

            public void SetupVoltageMeasure()
            {
                ActivatePPMU(true);
            }

            public void MeasureVoltage(int NumAverages, ref double Result)
            {
                try
                {
                   //double[] results = HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].ForceCurrentMeasureVoltage(currentForce);

                   // if (double.IsInfinity(results[0]))
                   // {
                   //     results[0] = -999;
                   // }
                    
                   // Result = results[0];
                }
                catch (Exception e)
                {

                }
            }

            public void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings)
            {
            }

            public void PostLeakageTest()
            {
                if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    ActivatePPMU(false);
                }
            }

            public void SetupContinuity(double currentForce)
            {
                ActivatePPMU(true);
                //HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].Force(Keysight.KtMDsr.Interop.KtMDsrPpmuSiteForceModeEnum.KtMDsrPpmuSiteForceModeCurrent, currentForce);
                this.currentForce = currentForce;
            }

            public void MeasureContinuity(int avgs, ref double result)
            {
                MeasureVoltage(avgs, ref result);

                if (HSDIO.IsMipiChannel(PinName))  // return MIPI channel to digital state
                {
                    ActivatePPMU(false);
                }
            }

            private void ActivatePPMU(bool activate)
            {
                if (activate != ppmuIsActive)
                {
                    if (activate)
                    {
                        //HSDIO.Keysight.driver.PpmuSites.Activate(HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber);  // or Inactivate?
                        //HSDIO.Keysight.driver.PpmuSites.Item[HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber].AverageMode = Keysight.KtMDsr.Interop.KtMDsrPpmuSiteAverageModeEnum.KtMDsrPpmuSiteAverageModeAverage64;  // do this here, since we can't do this during initialization
                    }
                    else
                        //HSDIO.Keysight.driver.PpmuSites.InactivateAndDisable(HSDIO.Keysight.ppmuSiteAliasPrefix + ChanNumber);  // or Inactivate?

                    ppmuIsActive = activate;
                }
            }

            public void CloseSession()
            {

            }

            public void GetDeviceTemperature(out double dblTemp)
            {
                dblTemp = 0;
            }

            public void DeviceSelfCal()
            {

            }
        }

        public interface iSmu
        {
            string VisaAlias { get; set; }
            string ChanNumber { get; set; }
            string PinName { get; set; }
            double priorVoltage { get; set; }
            double priorCurrentLim { get; set; }

            void ForceVoltage(double voltsForce, double currentLimit);
            void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA);
            void MeasureCurrent(int NumAverages, bool triggered, ref double Result);
            void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false);
            void SetupCurrentTraceMeasurement_RFONOFFTIME(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false);

            double[] MeasureCurrentTrace();
            void SetupVoltageMeasure();
            void MeasureVoltage(int NumAverages, ref double Result);
            void PreLeakageTest(RotiCanaiLib.TestParams.smuSettings settings);
            void PostLeakageTest();
            void SetupContinuity(double currentForce);
            void MeasureContinuity(int avgs, ref double result);
            void CloseSession();

            void GetDeviceTemperature(out double temperature);
            void DeviceSelfCal();

        }
    
    }
}
