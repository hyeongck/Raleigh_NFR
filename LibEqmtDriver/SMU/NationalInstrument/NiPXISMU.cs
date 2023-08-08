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
    public class NiPXISMU : Base_SMU, iPowerSupply
    {
        public override string VisaAlias { get; set; }
        public override string SerialNumber { get; set; }
        public override string ChanNumber { get; set; }
        public override string PinName { get; set; }
        public override byte Site { get; set; }
        public override string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber}" : string.Empty; }
        public override string ModelNumber { get; }

        public NiPXISMU(string[] val)
        {
            //Note : val data must be in this format Px_CHy_NIxxxx (eg P1_CH1_NI4143) , will decode the NI SMU aliasname (eg. NI4143_P1)
            //Where Px = part SMU aliasname
            //Where CHx = which SMU channel to set (eg NI4143 has 4x CH)
            //Where NIxxxx = which SMU model

            SmuResources.Clear();

            string tempVal = "";

            for (int i = 0; i < val.Length; i++)
            {
                string[] arSelected = new string[4];
                tempVal = val[i];
                arSelected = tempVal.Split('_');

                this.VisaAlias = arSelected[3];
                this.PinName = tempVal;
                this.ChanNumber = arSelected[1].Substring(2, 1);

                getSMU(VisaAlias, ChanNumber, PinName, true, OptionString, ModelNumber, Simulated);
            }
        }
        ~NiPXISMU() { }

        public static Dictionary<string, iSmu> SmuResources = new Dictionary<string, iSmu>();

        public static iSmu getSMU(string VisaAlias, string ChanNumber, string PinName, bool Reset, string OptionString, string ModelNumber, bool Simulated)
        {
            iSmu smu;

            if (VisaAlias.Contains("4154"))
            {
                ModelNumber = Simulated ? "4154;" : string.Empty;
                smu = new NI4154(VisaAlias, ChanNumber, PinName, Reset, OptionString + ModelNumber);
            }
            else if (VisaAlias.Contains("4139"))
            {
                ModelNumber = Simulated ? "4139;" : string.Empty;
                smu = new NI4139(VisaAlias, ChanNumber, PinName, Reset, OptionString + ModelNumber);
            }
            else if (VisaAlias.Contains("4143") || VisaAlias.Contains("4141"))
            {
                
                ModelNumber = Simulated ? (VisaAlias.Contains("4143")?"4143;":"4139;"): string.Empty;
                smu = new NI4143(VisaAlias, ChanNumber, PinName, Reset, OptionString + ModelNumber);
            }
            else
            {
                throw new Exception("Visa Alias \"" + VisaAlias + "\" is not in a recognized format.\nValid SMU Visa Aliases must include one of the following:\n"
                    + "\n\"4154\""
                    + "\n\"4139\""
                    + "\n\"4143\""
                    + "\n\"4141\""
                    + "\n\nFor example, Visa Alias \"SMU_NI4143_02\" will be recognized as an NI 4143 module.");
            }

            SmuResources.Add(PinName, smu);

            return smu;
        }

        #region iPowerSupply Members

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override void DcOn(string strSelection, ePSupply_Channel Channel)
        {
            SmuResources[strSelection].OutputEnable(true);
        }

        public override void DcOff(string strSelection, ePSupply_Channel Channel)
        {
            //SmuResources[strSelection].ForceVoltage(0.0, 1e-6);      //force voltage to 0V and very small current (cannot be zero)
            SmuResources[strSelection].OutputEnable(false);
        }

        public override void SetNPLC(string strSelection, ePSupply_Channel Channel, float val)
        {

        }

        public override void SetVolt(string strSelection, ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange)
        {
            Volt = Math.Round(Volt, 5); //Seoul
            //iLimit = Math.Round(iLimit, 5); //Seoul
            iLimit = Math.Round(iLimit, 10); //Seoul
            SmuResources[strSelection].ForceVoltage(Volt, iLimit);
        }

        public override float MeasI(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange)
        {
            double imeas = -999;
            SmuResources[strSelection].SetupCurrentMeasure(false, false);

            // Ben - Change Average Count 1 to 3
            //SmuResources[strSelection].MeasureCurrent(1, false, ref imeas);
            SmuResources[strSelection].MeasureCurrent(3, false, ref imeas);
            return Convert.ToSingle(imeas);
        }
        // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 
        public override float MeasITraceMode(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int ReadDelay)
        {
            int CurrentAvg = 16;
            int mDelay = ReadDelay;
            double mt = mDelay / 1000.0 + (double)CurrentAvg * 0.001;

            SmuResources[strSelection].SetupCurrentTraceMeasurement(mt, 500e-6, false);
            double[] CurrentTraceRawData = SmuResources[strSelection].MeasureCurrentTrace();

            int skipPoints = (int)(CurrentTraceRawData.Length * (double)mDelay / 1000.0 /
                                    ((double)mDelay / 1000.0 + (double)CurrentAvg * 0.001));
            skipPoints = Math.Min(skipPoints, CurrentTraceRawData.Length - 1);

            double result = CurrentTraceRawData.Skip(skipPoints).Average();

            return Convert.ToSingle(result);
        }
        public override float MeasV(string strSelection, ePSupply_Channel Channel, ePSupply_VRange VRange)
        {
            throw new NotImplementedException();
        }
        public override void CalSelfCalibrate(string strSelection, ePSupply_Channel Channel)
        {
            SmuResources[strSelection].CalSelfCalibrate();
        }

        public override double CheckDeviceTemperature(string strSelection, ePSupply_Channel Channel)
        {
            return SmuResources[strSelection].CheckDeviceTemperature();
        }

        #endregion iPowerSupply Members
    }

    public interface iSmu
    {
        string VisaAlias { get; set; }
        string ChanNumber { get; set; }
        string PinName { get; set; }
        double priorVoltage { get; set; }
        double priorCurrentLim { get; set; }

        void Close();
        void ForceVoltage(double voltsForce, double currentLimit, bool isTest = false);
        void ForceVoltageOnlyVio(double voltsForce, double currentLimit); // Ben - Add for setting VIO from digital to PPMU (Original code from RF1 - iEqDC interface functions)
        void SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA);
        void MeasureCurrent(int NumAverages, bool triggered, ref double Result);
        void SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false);
        double[] MeasureCurrentTrace();
        void SetupVoltageMeasure();
        void MeasureVoltage(int NumAverages, ref double Result);
        void SetupContinuity(double currentForce);
        void MeasureContinuity(int avgs, ref double result);
        void OutputEnable(bool state);
        void CalSelfCalibrate();
        double CheckDeviceTemperature();

    }
}
