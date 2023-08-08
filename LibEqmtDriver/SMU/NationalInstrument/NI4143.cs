using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using NationalInstruments.ModularInstruments.Interop;

namespace LibEqmtDriver.SMU
{
    public class NI4143 : iSmu
    {
        public nidcpower SMUsession;
        public string VisaAlias { get; set; }
        public string ChanNumber { get; set; }
        public string PinName { get; set; }
        public double priorVoltage { get; set; }
        public double priorCurrentLim { get; set; }

        public NI4143(string VisaAlias, string ChanNumber, string PinName, bool Reset, string OptionString)
        {
            try
            {
                this.SMUsession = new NationalInstruments.ModularInstruments.Interop.nidcpower(VisaAlias, ChanNumber, Reset, OptionString);

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
                SMUsession.SetInt32(nidcpowerProperties.TransientResponse, ChanNumber, nidcpowerConstants.Normal);
                SMUsession.Initiate();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "SMU Initialize");
            }
        }

        public void Close()
        {
            this.SMUsession.Dispose();
        }

        public void ForceVoltageOnlyVio(double voltsForce, double currentLimit)
        {

        }

        public void ForceVoltage(double voltsForce, double currentLimit, bool isTest = false)
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

                SMUsession.SetDouble(nidcpowerProperties.ApertureTime, ChanNumber, 0.0005);
                SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);

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
                MessageBox.Show(e.ToString(), "MeasureVoltage");

            }
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

        public void OutputEnable(bool state)
        {
            SMUsession.ConfigureOutputEnabled(ChanNumber, state);
        }

        public void CalSelfCalibrate()
        {

        }

        public double CheckDeviceTemperature()
        {
            double DeviceTemperature = 0.0f;
            SMUsession.ReadCurrentTemperature(out DeviceTemperature);

            return DeviceTemperature;
        }
    }

}
