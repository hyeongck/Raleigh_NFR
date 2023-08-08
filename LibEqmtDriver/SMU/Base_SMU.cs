using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.SMU
{
    public partial class Base_SMU : iPowerSupply
    {
        public virtual string VisaAlias { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual string ChanNumber { get; set; }
        public virtual string PinName { get; set; }
        public virtual byte Site { get; set; }
        public virtual double priorVoltage { get; set; }
        public virtual double priorCurrentLim { get; set; }
        public virtual double priorApertureTime { get; set; }
        public virtual string priorCH { get; set; }
        public virtual double Temp { get; set; }
        public virtual int NumTraceSamples { get; set; }
        public virtual TriggerLine _trigLine { get; set; }
        public virtual bool MultiChannelDevice { get; }
        public virtual double MaxSampleRate { get; }
        public virtual double MaxCurrentLimit { get; }
        public virtual double MinCurrentLimit { get; }
        public virtual bool Simulated { get => ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE); }
        public virtual string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber};" : string.Empty; }
        public virtual string ModelNumber { get; }

        public virtual void Init()
        {

        }
        public virtual void Close()
        {

        }
        public virtual void DcOn(string StrSelection, ePSupply_Channel Channel)
        {

        }
        public virtual void DcOff(string StrSelection, ePSupply_Channel Channel)
        {

        }
        public virtual void SetNPLC(string StrSelection, ePSupply_Channel Channel, float val)
        {

        }
        public virtual void SetVolt(string StrSelection, ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange)
        {

        }
        public virtual float MeasI(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange)
        {
            return 0.0f;
        }
        public virtual float MeasI(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int i)
        {
            return 0.0f;
        }
        // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 
        public virtual float MeasITraceMode(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int ReadDelay)
        {
            return 0.0f;
        }
        public virtual float MeasV(string StrSelection, ePSupply_Channel Channel, ePSupply_VRange VRange)
        {
            return 0.0f;
        }
        // Self Calibration : Only NI-4139 works
        public virtual void CalSelfCalibrate(string strSelection, ePSupply_Channel Channel)
        {

        }
        public virtual double CheckDeviceTemperature(string strSelection, ePSupply_Channel Channel)
        {
            return 0.0;
        }
    }
}
