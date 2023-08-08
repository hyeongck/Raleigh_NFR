using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using NationalInstruments.LabVIEW.Interop;
//using ni_NoiseFloor;
using ni_NoiseFloorWrapper;

namespace MyProduct.MyCal
{
    public static class NoiseTestConstants
    {
        //***Non-Driver Specific***
        public const string ReferenceClockSource = "PXI_CLK";
        public const int NumberOfRuns = 10;
        public const string BandSelection = "Custom";
        public const double RX_FrequencyOffset = 0;

        //***RFSG***        
        public const double GenerationLength = 100e-6; //s   2.2e-3                             
        public const double IdleTime = 100e-6; //s  .5e-3

        public const string RFSG_MarkerEvents_2_ExportedOutputTerminal = "PXI_Trig2";
        public const string RFSG_MarkerEvents_1_ExportedOutputTerminal = "PXI_Trig1";
        public const string RFSG_ConfigurationListStepTrigger_ExportedOutputTerminal = "PXI_Trig0";
        public const double ArbPreFilterGain = -3; 
       // public const double ArbPreFilterGain = 0;
        public const RfsgLoopBandwidth loopBandwidth = RfsgLoopBandwidth.High;
        public static readonly RfsgConfigurationListProperties[] RFSG_PropertyList = new RfsgConfigurationListProperties[] { RfsgConfigurationListProperties.Frequency, (RfsgConfigurationListProperties)1154098 }; //1154098 corresponds to Property ID for Upconverter Center Frequency
        public const Boolean UseWaveformFile = true;

        //***RFSA***        
        public const double TimePerRecord = 300e-6; //2e-3

        public const double RFSA_LO_Offset = 60e6;
        public const string RFSA_StartTrigger_DigitalEdge_Source = "PXI_Trig2";
        public const string RFSA_ReferenceTrigger_DigitalEdge_Source = "PXI_Trig1";
        public const long RFSA_PreTriggerSamples = 0;
        public const double TimerEventInterval = 5e-5;

        public static readonly RfsaConfigurationListProperties[] RFSA_PropertyList = new RfsaConfigurationListProperties[] { RfsaConfigurationListProperties.IQCarrierFrequency, RfsaConfigurationListProperties.DownconverterCenterFrequency };
    }
}
