using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.SG
{
    public interface iSiggen
    {
        void Close();
        void Initialize();
        void Reset();
        void EnableModulation(INSTR_OUTPUT _ONOFF);
        void EnableRF(INSTR_OUTPUT _ONOFF);
        void SetFreq(double _MHz);
        void SetAmplitude(float _dBm);
        void SetPowerMode(N5182_POWER_MODE _mode);
        void SetFreqMode(N5182_FREQUENCY_MODE _mode);
        void MOD_FORMAT_WITH_LOADING_CHECK(string strWaveform, string strWaveformName, bool WaveformInitalLoad);
        void SELECT_WAVEFORM(N5182A_WAVEFORM_MODE _MODE);
        void SET_LIST_TYPE(N5182_LIST_TYPE _mode);
        void SET_LIST_MODE(INSTR_MODE _mode);
        void SET_LIST_TRIG_SOURCE(N5182_TRIG_TYPE _mode);
        void SET_CONT_SWEEP(INSTR_OUTPUT _ONOFF);
        void SET_START_FREQUENCY(double _MHz);
        void SET_STOP_FREQUENCY(float _MHz);
        void SET_TRIG_TIMERPERIOD(double _ms);
        void SET_SWEEP_POINT(int _points);
        void SINGLE_SWEEP();
        void SET_SWEEP_PARAM(int _points, double _ms, double _StartFreqMHz, double _StopFreqMHz);
        bool OPERATION_COMPLETE();
        void SET_ROUTE_CONN_EVENT(N5182A_ROUTE_SUBSYS _mode);
        void SET_ROUTE_CONN_SOUT(N5182A_ROUTE_SUBSYS _mode);
        void SET_ROUTE_CONN_TOUT(N5182A_ROUTE_SUBSYS _mode);
        void SET_ALC_TRAN_REF(N5182A_ALC_TRAN_REF _mode);
        void QueryError_SG(out bool status);
    }
}
