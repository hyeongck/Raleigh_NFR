using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using Aemulus.Hardware.SMU;

namespace LibEqmtDriver.SMU
{
    public class AM471E : iAeSmu
    {
        public PxiSmu smu;
        public string VisaAlias { get; set; }
        public string ChanNumber { get; set; }
        public string PinName { get; set; }

        public AM471E(string VisaAlias, string ChanNumber, string PinName, bool Reset, string OptionString)
        {
            try
            {
                this.VisaAlias = VisaAlias;
                this.ChanNumber = ChanNumber;
                this.PinName = PinName;

                smu = new PxiSmu(VisaAlias, "0", 0xf, OptionString);

                int ret = 0;
                ret += smu.Reset();
                ret += smu.ConfigurePowerLineFrequency(60);
                ret += smu.ConfigureOutputTransient("0", 1);
                ret += smu.ConfigureSamplingTime("0", 0.1, 1);
                ret += smu.ConfigureSense("0", PxiSmuConstants.SenseRemote);
                ret += smu.ConfigureOutputFunction("0", PxiSmuConstants.DVCI);
                ret += smu.ConfigureCurrentLimit("0".ToString(), 0, 100e-3);
                ret += smu.ConfigureVoltageLevelAndRange("0", 0, 1);
                //ret += smu.ConfigureVoltageLevel("0", 0);
                ret += smu.ConfigureOutputSwitch("0", 1);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "SMU Initialize");
            }
        }

        public void Close()
        {
            smu.Dispose();
        }
        public void OutputEnable(bool state, ePSupply_Channel Channel)
        {
            //if (state)
            //    smu.ConfigureOutputSwitch(((int)Channel).ToString(), 1);
            //else
            //    smu.ConfigureOutputSwitch(((int)Channel).ToString(), 0);      
        }
        public void SetNPLC(ePSupply_Channel Channel, float val)
        {
            smu.ConfigureSamplingTime(((int)Channel).ToString(), val, 1);
        }
        public void SetVoltage(ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange)
        {
            double _VRange = 0;
            switch (VRange)
            {
                case ePSupply_VRange._1V:
                    _VRange = 1; break;
                case ePSupply_VRange._10V:
                    _VRange = 10; break;
                case ePSupply_VRange._Auto:
                    _VRange = 10; break;
            }
            smu.ConfigureOutputFunction(((int)Channel).ToString(), PxiSmuConstants.DVCI);
            smu.ConfigureCurrentLimit(((int)Channel).ToString(), 0, iLimit);
            smu.ConfigureVoltageLevelAndRange(((int)Channel).ToString(), Volt, _VRange);
            //smu.ConfigureVoltageLevel(((int)Channel).ToString(), Volt);
        }
        public float MeasureCurrent(ePSupply_Channel Channel, ePSupply_IRange IRange)
        {
            //we don't need to set range for measure
            double[] current = new double[4];
            int ret = 0;
            ret += smu.Measure(((int)Channel).ToString(), PxiSmuConstants.MeasureCurrent, current);
            return (float)current[0];
        }
        public float MeasureVoltage(ePSupply_Channel Channel, ePSupply_VRange VRange)
        {
            double[] volt = new double[4];
            int ret = 0;
            ret += smu.Measure(((int)Channel).ToString(), PxiSmuConstants.MeasureVoltage, volt);
            return (float)volt[0];
        }
        public void CalSelfCalibrate()
        {

        }

        public double CheckDeviceTemperature()
        {
            double DeviceTemperature = 0.0f;
            smu.ReadCurrentTemperature(out DeviceTemperature);
            return DeviceTemperature;
        }
    }
}
