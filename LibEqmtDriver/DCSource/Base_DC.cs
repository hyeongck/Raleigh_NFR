using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.DC
{
    public partial class Base_DC : iDCSupply
    {
        public virtual string VisaAlias { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual string ChanNumber { get; set; }
        public virtual string PinName { get; set; }
        public virtual byte Site { get; set; }
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
