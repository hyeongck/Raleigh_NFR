using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using Aemulus.Hardware.SMU;
using System.Diagnostics;

namespace LibEqmtDriver.SMU
{
    public class AePXISMU : Base_SMU, iPowerSupply
    {
        public override string VisaAlias { get; set; }
        public override string SerialNumber { get; set; }
        public override string ChanNumber { get; set; }
        public override string PinName { get; set; }
        public override byte Site { get; set; }
        public override string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber}" : string.Empty; }
        public override string ModelNumber { get; }

        public AePXISMU(string[] val)
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
        ~AePXISMU() { }

        public static Dictionary<string, iAeSmu> SmuResources = new Dictionary<string, iAeSmu>();

        public static iAeSmu getSMU(string VisaAlias, string ChanNumber, string PinName, bool Reset, string OptionString, string ModelNumber, bool Simulated)
        {
            iAeSmu smu;

            if (VisaAlias.Contains("430"))
            {
                ModelNumber = Simulated? "AM430e;" : string.Empty;
                smu = new AM430E(VisaAlias, ChanNumber, PinName, Reset, OptionString + ModelNumber);
            }
            else if (VisaAlias.Contains("471"))
            {
                ModelNumber = Simulated ? "AM471e;" : string.Empty;
                smu = new AM471E(VisaAlias, ChanNumber, PinName, Reset, OptionString + ModelNumber);
            }
            else
            {
                throw new Exception("Visa Alias \"" + VisaAlias + "\" is not in a recognized format.\nValid SMU Visa Aliases must include one of the following:\n"
                    + "\n\"430\""
                    + "\n\"471\""
                    + "\n\nFor example, Visa Alias \"SMU_AM471E_P1\" will be recognized as an AEMULUS AM471E module.");
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
            SmuResources[strSelection].OutputEnable(true, Channel);
        }

        public override void DcOff(string strSelection, ePSupply_Channel Channel)
        {
            //SmuResources[strSelection].ForceVoltage(0.0, 1e-6);      //force voltage to 0V and very small current (cannot be zero)
            SmuResources[strSelection].OutputEnable(false, Channel);
        }

        public override void SetNPLC(string strSelection, ePSupply_Channel Channel, float val)
        {
            Stopwatch tTime = new Stopwatch();

            tTime.Reset();
            tTime.Start();

      

            SmuResources[strSelection].SetNPLC(Channel, val);

            double a = tTime.ElapsedMilliseconds;

        }

        public override void SetVolt(string strSelection, ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange)
        {
            SmuResources[strSelection].SetVoltage(Channel, Volt, iLimit, VRange);
        }

        public override float MeasI(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange)
        {
            float imeas = -999;
            imeas = SmuResources[strSelection].MeasureCurrent(Channel, IRange);
            return imeas;
        }
        // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 
        public override float MeasITraceMode(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int ReadDelay)
        {
            // need to modify
            float imeas = -999;
            imeas = SmuResources[strSelection].MeasureCurrent(Channel, IRange);
            return imeas;
        }

        public override float MeasV(string strSelection, ePSupply_Channel Channel, ePSupply_VRange VRange)
        {
            float vmeas = -999;
            vmeas = SmuResources[strSelection].MeasureVoltage(Channel, VRange);
            return vmeas;
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

    public interface iAeSmu
    {
        string VisaAlias { get; set; }
        string ChanNumber { get; set; }
        string PinName { get; set; }

        void Close();
        void OutputEnable(bool state, ePSupply_Channel Channel);
        void SetNPLC(ePSupply_Channel Channel, float val);
        void SetVoltage(ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange);
        float MeasureCurrent(ePSupply_Channel Channel, ePSupply_IRange IRange);
        float MeasureVoltage(ePSupply_Channel Channel, ePSupply_VRange VRange);
        void CalSelfCalibrate();
        double CheckDeviceTemperature();
    }
}


