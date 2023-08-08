using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NationalInstruments.ModularInstruments.Interop;

namespace LibEqmtDriver.DC_1CH
{
    public class NI4154 : Base_DC_1CH,iDCSupply_1CH
    {
        public nidcpower SMUsession;
        public string VisaAlias { get; set; }
        public string ChanNumber { get; set; }
        public string PinName { get; set; }
        public double priorVoltage { get; set; }
        public double priorCurrentLim { get; set; }
        public override string ModelNumber { get => "4154"; }

        public NI4154(string val)
        {
            //Note : val data must be in this format Px_CHy_NIxxxx (eg P1_CH1_NI4143) , will decode the NI SMU aliasname (eg. NI4143_P1)
            //Where Px = part SMU aliasname
            //Where CHx = which SMU channel to set (eg NI4143 has 4x CH)
            //Where NIxxxx = which SMU model

            string tempVal = "";

            string visaAlias;
            string chNum;
            string pinName;

            string[] arSelected = new string[4];
            tempVal = val;
            arSelected = tempVal.Split('_');

            visaAlias = arSelected[3];
            pinName = tempVal;
            chNum = arSelected[1].Substring(2, 1);

            getSMU(visaAlias, chNum, pinName, true);
        }
        ~NI4154() { }

        #region iDCSupply_1CH Members

        public override void Init()
        {
            Reset();
        }

        public override void Close()
        {
            if (this.SMUsession != null)
            {
                this.SMUsession.Abort();
                this.SMUsession.Dispose();
            }      
        }

        public override void DcOn(int Channel)
        {
            try
            {
                OutputEnable(true);
            }
            catch (Exception ex)
            {
                throw new Exception("NI4154: DcOn -> " + ex.Message);
            }
        }
        public override void DcOff(int Channel)
        {
            try
            {
                OutputEnable(false);
            }
            catch (Exception ex)
            {
                throw new Exception("NI4154: DcOff -> " + ex.Message);
            }
        }

        public override void SetVolt(int Channel, double Volt, double iLimit)
        {
            Volt = Math.Round(Volt, 5); //Seoul
            iLimit = Math.Round(iLimit, 10); //Seoul
            ForceVoltage(Volt, iLimit);
        }

        public override float MeasI(int Channel)
        {
            try
            {
                double CurrentRslt = 0;

                SetupCurrentMeasure(false, false);
                MeasureCurrent(3, false, ref CurrentRslt);

                return Convert.ToSingle(CurrentRslt);
            }
            catch (Exception ex)
            {
                throw new Exception("NI4154:: MeasI -> " + ex.Message);
            }
        }

        public override float MeasV(int Channel)
        {
            try
            {
                double VoltageRslt = 0;

                SetupVoltageMeasure();
                MeasureVoltage(3,ref VoltageRslt);

                return Convert.ToSingle(VoltageRslt);
            }
            catch (Exception ex)
            {
                throw new Exception("NI4154:: MeasV -> " + ex.Message);
            }
        }

        #endregion

        public bool getSMU(string VisaAlias, string ChanNumber, string PinName, bool Reset)
        {
            try
            {
                this.SMUsession = new NationalInstruments.ModularInstruments.Interop.nidcpower(VisaAlias, ChanNumber, Reset, OptionString);

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

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "SMU Initialize");
                return false;
            }
        }

        public void Reset()
        {
            SMUsession.reset();

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
                MessageBox.Show(e.ToString(), "ForceVoltage4154");
            }
        }

        public void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
        {
            try
            {
                SMUsession.Abort();

                double measureTimeLength = 0;

                measureTimeLength = 0.0005;
                SMUsession.SetInt32(nidcpowerProperties.MeasureWhen, nidcpowerConstants.OnDemand);
                SMUsession.SetInt32(nidcpowerProperties.MeasureRecordLength, 1);

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

        public void OutputEnable(bool state)
        {
            SMUsession.ConfigureOutputEnabled(ChanNumber, state);
        }
    }
}
