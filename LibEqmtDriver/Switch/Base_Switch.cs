using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.SCU
{
    public partial class Base_Switch : iSwitch
    {
        public virtual string VisaAlias { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual string ChanNumber { get; set; }
        public virtual string PinName { get; set; }
        public virtual byte Site { get; set; }
        public virtual bool Simulated { get => ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE); }
        public virtual string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber};" : string.Empty; }
        public virtual string ModelNumber { get; }

        public virtual void Close() { }
        public virtual void Initialize(){ }
        public virtual void SetPath(string val){ }
        public virtual void SetPath(object state){ }
        public virtual void Reset(){ }
        public virtual int SPDT1CountValue(){ return 0; }
        public virtual int SPDT2CountValue(){ return 0; }
        public virtual int SPDT3CountValue(){ return 0; }
        public virtual int SPDT4CountValue(){ return 0; }
        public virtual int SP6T1_1CountValue(){ return 0; }
        public virtual int SP6T1_2CountValue(){ return 0; }
        public virtual int SP6T1_3CountValue(){ return 0; }
        public virtual int SP6T1_4CountValue(){ return 0; }
        public virtual int SP6T1_5CountValue(){ return 0; }
        public virtual int SP6T1_6CountValue(){ return 0; }
        public virtual int SP6T2_1CountValue(){ return 0; }
        public virtual int SP6T2_2CountValue(){ return 0; }
        public virtual int SP6T2_3CountValue(){ return 0; }
        public virtual int SP6T2_4CountValue(){ return 0; }
        public virtual int SP6T2_5CountValue(){ return 0; }
        public virtual int SP6T2_6CountValue(){ return 0; }
        public virtual void SaveRemoteMechSwStatusFile(){ }
        public virtual void SaveLocalMechSwStatusFile(){ }
        public virtual string GetInstrumentInfo(){ return ""; }

    }
}
