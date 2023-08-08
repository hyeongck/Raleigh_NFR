using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices; 

namespace LibEqmtDriver.SA
{
    public interface iSigAnalyzer
    {
        void Initialize(int EquipId);
        void Close();
        void Preset();
        void Select_Instrument(N9020A_INSTRUMENT_MODE _MODE);
        void Select_Triggering(N9020A_TRIGGERING_TYPE _TYPE);
        void Measure_Setup(N9020A_MEAS_TYPE _TYPE);
        void Enable_Display(N9020A_DISPLAY _ONOFF);
        void VBW_RATIO(double _ratio);
        void SPAN(double _freq_MHz);
        void MARKER_TURN_ON_NORMAL_POINT(int _markerNum, float _MarkerFreq_MHz);
        void TURN_ON_INTERNAL_PREAMP();
        void TURN_OFF_INTERNAL_PREAMP();
        void TURN_OFF_MARKER();
        double READ_MARKER(int _markerNum);
        void SWEEP_TIMES(int _sweeptime_ms);
        void SWEEP_POINTS(int _sweepPoints);
        void CONTINUOUS_MEASUREMENT_ON();
        void CONTINUOUS_MEASUREMENT_OFF();
        void RESOLUTION_BW(double _BW);
        double MEASURE_PEAK_POINT(int delayMs);
        double MEASURE_PEAK_FREQ(int delayMs);
        void VIDEO_BW(double _VBW_Hz);
        void TRIGGER_CONTINUOUS();
        void TRIGGER_SINGLE();
        void TRIGGER_IMM();
        void TRACE_AVERAGE(int _AVG);
        void AVERAGE_OFF();
        void AVERAGE_ON();
        void SET_TRACE_DETECTOR(string mode);
        void CLEAR_WRITE();
        void AMPLITUDE_REF_LEVEL_OFFSET(double _RefLvlOffset);
        void AMPLITUDE_REF_LEVEL(double _RefLvl);
        void AMPLITUDE_INPUT_ATTENUATION(double _Input_Attenuation);
        void AUTO_ATTENUATION(bool state);
        void ELEC_ATTENUATION(float _Input_Attenuation);
        void ELEC_ATTEN_ENABLE(bool _Input_Stat);
        void ALIGN_PARTIAL();
        void ALIGN_ONCE();
        void AUTOALIGN_ENABLE(bool _Input_Stat);
        void CAL();
        bool OPERATION_COMPLETE();
        void START_FREQ(string strFreq, string strUnit);
        void STOP_FREQ(string strFreq, string strUnit);
        void FREQ_CENT(string strSaFreq, string strUnit);
        string READ_MXATrace(int _traceNum);
        double READ_STARTFREQ();
        double READ_STOPFREQ();
        float READ_SWEEP_POINTS();
        float[] IEEEBlock_READ_MXATrace(int _traceNum);
        void SET_TRIG_DELAY(N9020A_TRIGGERING_TYPE _TYPE, string _delay_ms);
        void MARKER_NOISE(bool _state, int _markerNum, double _bandSpan_Hz = 1e6);
    }
}
