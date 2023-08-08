using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.SG
{
    public partial class Base_SG : iSiggen
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
        public virtual void Reset(){ }
        public virtual void EnableModulation(INSTR_OUTPUT _ONOFF){ }
        public virtual void EnableRF(INSTR_OUTPUT _ONOFF){ }
        public virtual void SetFreq(double _MHz){ }
        public virtual void SetAmplitude(float _dBm){ }
        public virtual void SetPowerMode(N5182_POWER_MODE _mode){ }
        public virtual void SetFreqMode(N5182_FREQUENCY_MODE _mode){ }
        public virtual void MOD_FORMAT_WITH_LOADING_CHECK(string strWaveform, string strWaveformName, bool WaveformInitalLoad){ }
        public virtual void SELECT_WAVEFORM(N5182A_WAVEFORM_MODE _MODE){ }
        public virtual void SET_LIST_TYPE(N5182_LIST_TYPE _mode){ }
        public virtual void SET_LIST_MODE(INSTR_MODE _mode){ }
        public virtual void SET_LIST_TRIG_SOURCE(N5182_TRIG_TYPE _mode){ }
        public virtual void SET_CONT_SWEEP(INSTR_OUTPUT _ONOFF){ }
        public virtual void SET_START_FREQUENCY(double _MHz){ }
        public virtual void SET_STOP_FREQUENCY(float _MHz){ }
        public virtual void SET_TRIG_TIMERPERIOD(double _ms){ }
        public virtual void SET_SWEEP_POINT(int _points){ }
        public virtual void SINGLE_SWEEP(){ }
        public virtual void SET_SWEEP_PARAM(int _points, double _ms, double _StartFreqMHz, double _StopFreqMHz){ }
        public virtual bool OPERATION_COMPLETE(){ return false; }
        public virtual void SET_ROUTE_CONN_EVENT(N5182A_ROUTE_SUBSYS _mode){ }
        public virtual void SET_ROUTE_CONN_SOUT(N5182A_ROUTE_SUBSYS _mode){ }
        public virtual void SET_ROUTE_CONN_TOUT(N5182A_ROUTE_SUBSYS _mode){ }
        public virtual void SET_ALC_TRAN_REF(N5182A_ALC_TRAN_REF _mode){ }
        public virtual void QueryError_SG(out bool status){ status = false; }
    }
}
