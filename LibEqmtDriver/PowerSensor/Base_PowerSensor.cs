using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.PS
{
    public partial class Base_PowerSensor : iPowerSensor
    {
        public virtual string VisaAlias { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual string ChanNumber { get; set; }
        public virtual string PinName { get; set; }
        public virtual byte Site { get; set; }
        public virtual bool Simulated { get => ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE); }
        public virtual string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber};" : string.Empty; }
        public virtual string ModelNumber { get; }

        public virtual void Initialize(int ch)
        {

        }
        public virtual void Reset()
        {

        }
        public virtual void Close()
        {

        }

        public virtual void SetOffset(int ch, double val)
        {

        }
        public virtual void EnableOffset(int ch, bool status)
        {

        }
        public virtual void SetFreq(int ch, double val, int measuretype) // meausr type : 0 = Calibration Type, 1 = DUT Measuring Type
        {

        }
        public virtual float MeasPwr(int ch)
        {
            return 0;
        }

    }
}
