using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.DC_1CH
{
    public partial class Base_DC_1CH : iDCSupply_1CH
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
        public virtual void DcOff(int Channel)
        {

        }
        public virtual void DcOn(int Channel)
        {

        }
        public virtual void SetVolt(int Channel, double Volt, double iLimit)
        {

        }
        public virtual float MeasI(int Channel)
        {
            return 0;
        }
        public virtual float MeasV(int Channel)
        {
            return 0;
        }
    }

}
