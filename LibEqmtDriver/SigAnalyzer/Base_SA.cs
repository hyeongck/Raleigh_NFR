using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClothoSharedItems;

namespace LibEqmtDriver.SA
{
    public partial class Base_SA : iSigAnalyzer
    {
        public virtual string VisaAlias { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual string ChanNumber { get; set; }
        public virtual string PinName { get; set; }
        public virtual byte Site { get; set; }
        public virtual bool Simulated { get => ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE); }
        public virtual string OptionString { get => Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber};" : string.Empty; }
        public virtual string ModelNumber { get; }

        public virtual void Initialize(int EquipId)
        {

        }
        public virtual void Close()
        {

        }
        public virtual void Preset()
        {

        }
        public virtual void Select_Instrument(N9020A_INSTRUMENT_MODE _MODE)
        {

        }
        public virtual void Select_Triggering(N9020A_TRIGGERING_TYPE _TYPE)
        {

        }
        public virtual void Measure_Setup(N9020A_MEAS_TYPE _TYPE)
        {

        }
        public virtual void Enable_Display(N9020A_DISPLAY _ONOFF)
        {

        }
        public virtual void VBW_RATIO(double _ratio)
        {

        }
        public virtual void SPAN(double _freq_MHz)
        {

        }
        public virtual void MARKER_TURN_ON_NORMAL_POINT(int _markerNum, float _MarkerFreq_MHz)
        {

        }
        public virtual void TURN_ON_INTERNAL_PREAMP()
        {

        }
        public virtual void TURN_OFF_INTERNAL_PREAMP()
        {

        }
        public virtual void TURN_OFF_MARKER()
        {

        }
        public virtual double READ_MARKER(int _markerNum)
        {
            return 0;
        }
        public virtual void SWEEP_TIMES(int _sweeptime_ms)
        {

        }
        public virtual void SWEEP_POINTS(int _sweepPoints)
        {

        }
        public virtual void CONTINUOUS_MEASUREMENT_ON()
        {

        }
        public virtual void CONTINUOUS_MEASUREMENT_OFF()
        {

        }
        public virtual void RESOLUTION_BW(double _BW)
        {

        }
        public virtual double MEASURE_PEAK_POINT(int delayMs)
        {
            return 0;
        }
        public virtual double MEASURE_PEAK_FREQ(int delayMs)
        {
            return 0;
        }
        public virtual void VIDEO_BW(double _VBW_Hz)
        {

        }
        public virtual void TRIGGER_CONTINUOUS(){ }
        public virtual void TRIGGER_SINGLE(){ }
        public virtual void TRIGGER_IMM(){ }
        public virtual void TRACE_AVERAGE(int _AVG){ }
        public virtual void AVERAGE_OFF(){ }
        public virtual void AVERAGE_ON(){ }
        public virtual void SET_TRACE_DETECTOR(string mode){ }
        public virtual void CLEAR_WRITE(){ }
        public virtual void AMPLITUDE_REF_LEVEL_OFFSET(double _RefLvlOffset){ }
        public virtual void AMPLITUDE_REF_LEVEL(double _RefLvl){ }
        public virtual void AMPLITUDE_INPUT_ATTENUATION(double _Input_Attenuation){ }
        public virtual void AUTO_ATTENUATION(bool state){ }
        public virtual void ELEC_ATTENUATION(float _Input_Attenuation){ }
        public virtual void ELEC_ATTEN_ENABLE(bool _Input_Stat){ }
        public virtual void ALIGN_PARTIAL(){ }
        public virtual void ALIGN_ONCE(){ }
        public virtual void AUTOALIGN_ENABLE(bool _Input_Stat){ }
        public virtual void CAL(){ }
        public virtual bool OPERATION_COMPLETE(){ return false; }
        public virtual void START_FREQ(string strFreq, string strUnit){ }
        public virtual void STOP_FREQ(string strFreq, string strUnit){ }
        public virtual void FREQ_CENT(string strSaFreq, string strUnit){ }
        public virtual string READ_MXATrace(int _traceNum){ return ""; }
        public virtual double READ_STARTFREQ(){ return 0; }
        public virtual double READ_STOPFREQ(){ return 0; }
        public virtual float READ_SWEEP_POINTS(){ return 0; }
        public virtual float[] IEEEBlock_READ_MXATrace(int _traceNum){ float[] fReturn = { 0, }; return fReturn; }
        public virtual void SET_TRIG_DELAY(N9020A_TRIGGERING_TYPE _TYPE, string _delay_ms){ }
        public virtual void MARKER_NOISE(bool _state, int _markerNum, double _bandSpan_Hz = 1e6){ }
    }
}
