//#define PWRRAMPING_TRIGGER  // Define this if want to export the marker event at the beginning of transient to a terminal for debugging purpose.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments;
using NationalInstruments.ModularInstruments.NIRfsg;
using System.Threading;
//using LabVIEWFilters;
using LabVIEWFiltersWrapper;

namespace LibEqmtDriver.NF_VST
{

    class PowerRampingTest
    {
        /// <summary>
        /// Timeout in miliseconds when calling Wait(). Default is 1000ms. 
        /// If the timeout is less than waveform time length, timeout will auto-adjust to waveform time length + 1000ms.
        /// </summary>
        public int waitTimeoutMS = 1000;

        #region private data
        ComplexDouble[] _rampUpIqData;
        ComplexDouble[] _rampDownIqData;
        double[] _rampUpPowerLevels;
        double[] _rampDownPowerLevels;
        double _peakPowerToSG;
        int _totalWfmCount;


        NIRfsg _rfsgSession;
        double _frequency;
        double _iqRate;
        double _transientTime;
        int _transientSteps;
        double _dwellTime;
        int _rampDownSteps;
        double _papr;
        bool _enableTransient;
        #endregion

        /// <summary>
        /// Initialize a waveform of PC3 Power Sweeping for NF Test based on CW signal
        /// </summary>
        /// <param name="frequency">Center frequency of carrier</param>
        /// <param name="iqRate">IQ rate</param>
        /// <param name="transientTime">Transient time in seconds to ramp up power to start level . 
        /// Disable transient by setting it zero</param>
        /// <param name="transientSteps">Number of steps to ramp up if any</param>
        /// <param name="dwellTime">Dwell time in seconds of each power level when ramping down</param>
        /// <param name="rampDownSteps">Number of steps to ramp down</param>
        /// <param name="startPwrLevel">Starting power level of power sweeping (dBm)</param>
        /// <param name="stopPwrLevel">Stopping power level of power sweeping (dBm)</param>
        public void BuildCwSweepWaveforms(double frequency, double iqRate,
                              double transientTime, int transientSteps, double dwellTime, int rampDownSteps,
                              double startPwrLevel, double stopPwrLevel)
        {
            if (!CheckRampResolutionsValidity(
                transientTime, transientSteps, dwellTime, rampDownSteps, startPwrLevel, stopPwrLevel))
            {
                throw new Exception("Error in Ramp Resolutions. "
                    + "Make sure that dwell times > 0; transient time >= 0; ramping steps > 1; startPwrLevel >= stopPwrLevel");
            }

            _frequency = frequency;
            _iqRate = iqRate;
            _transientTime = transientTime;
            _transientSteps = transientSteps;
            _dwellTime = dwellTime;
            _rampDownSteps = rampDownSteps;


            // Generate IQ data for CW signal            
            ComplexDouble[] iqData = new ComplexDouble[Convert.ToInt32(Convert.ToDouble(_dwellTime) * iqRate)];
            for (int i = 0; i < iqData.Length; i++)
            {
                iqData[i] = new ComplexDouble(1, 0);
            }

            CalcSweepPwrAndWfm(iqData, startPwrLevel, stopPwrLevel);
        }

        /// <summary>
        /// Build a waveform of PC3 Power Sweeping for NF Test based on IQ waveform given by user.
        /// </summary>
        /// /// <param name="iqWfm">IQ waveform provided by user</param>
        /// <param name="frequency">Center frequency of carrier</param>
        /// <param name="iqRate">IQ rate</param>
        /// <param name="transientTime">Transient time in seconds to ramp up power to start level . 
        /// Disable transient by setting it zero</param>        
        /// <param name="transientSteps">Number of steps to ramp up if any</param>
        /// <param name="dwellTime">Dwell time in seconds of each power level when ramping down</param>
        /// <param name="rampDownSteps">Number of steps to ramp down</param>
        /// <param name="startPwrLevel">Starting power level of power sweeping (dBm)</param>
        /// <param name="stopPwrLevel">Stopping power level of power sweeping (dBm)</param>
        public void BuildSweepWaveforms(
                              ComplexDouble[] iqWfm, double frequency, double iqRate,
                              double transientTime, int transientSteps, double dwellTime, int rampDownSteps,
                              double startPwrLevel, double stopPwrLevel)
        {
            if (!CheckRampResolutionsValidity(
                transientTime, transientSteps, dwellTime, rampDownSteps, startPwrLevel, stopPwrLevel))
            {
                throw new Exception("Error in Ramp Resolutions. "
                    + "Make sure that dwell times > 0; transient time >= 0; ramping steps > 1; startPwrLevel >= stopPwrLevel");
            }

            _frequency = frequency;
            _iqRate = iqRate;
            _transientTime = transientTime;
            _transientSteps = transientSteps;
            _dwellTime = dwellTime;
            _rampDownSteps = rampDownSteps;


            // Generate IQ data for CW signal            
            CalcSweepPwrAndWfm(iqWfm, startPwrLevel, stopPwrLevel);
        }

        // Read previous config for reverting configuration
        private RfsgRFPowerLevelType _previousPwrLevelType;
        private RfsgWaveformGenerationMode _previousGenerationMode;


        /// <summary>
        /// Configure the rfsg for power ramping test. Please build the sweep waveforms (BuildSweepWaveforms() or BuildCwSweepWaveforms()) first 
        /// before calling this function.
        /// </summary>
        public void Configure(NIRfsg rfsgHandle)
        {
            if (_totalWfmCount > 0)  // Configure if waveform is configured
            {
                _rfsgSession = rfsgHandle;

                // Read previous config for reverting configuration
                _previousPwrLevelType = _rfsgSession.RF.PowerLevelType;
                _previousGenerationMode = _rfsgSession.Arb.GenerationMode;

                _rfsgSession.RF.PowerLevelType = RfsgRFPowerLevelType.PeakPower;
                _rfsgSession.Arb.GenerationMode = RfsgWaveformGenerationMode.Script;
                //rfsgSession.Configuration.Triggers.StartTrigger.Disable();
                //rfsgSession.Configuration.Triggers.AdvanceTrigger.Disable();
                

                _rfsgSession.RF.Configure(_frequency, _peakPowerToSG);
                _rfsgSession.Arb.IQRate = _iqRate;

                WriteScriptForRampUpAndDown();                
                _rfsgSession.Utility.Commit();
            }
            else
                throw new Exception("Sweeping waveforms has not been defined.");
        }

        /// <summary>
        /// Poll to check whether the waveform generation is finished.
        /// </summary>
        public void Wait()
        {
            { // Auto increase wait timeout if it is less than waveform time length
                var ttlTime = ((_totalWfmCount - 1) * _dwellTime + _transientTime) / 1000;
                if (waitTimeoutMS < ttlTime)
                {
                    waitTimeoutMS = Convert.ToInt32(ttlTime) + 1000;  // Add extra 1000ms to waveform time length
                }
            }

            var loopleft = this.waitTimeoutMS / 10;

            while (loopleft > 0 & _rfsgSession.CheckGenerationStatus() != RfsgGenerationStatus.Complete)
            {
                Thread.Sleep(100);  // Sleep 10 ms
                loopleft--;
            }

            // Revert previous settings            
            _rfsgSession.RF.PowerLevelType = _previousPwrLevelType;
            _rfsgSession.Arb.GenerationMode = _previousGenerationMode;
        }

        public void Initiate()
        {
            _rfsgSession.Initiate();
        }

        public void Abort()
        {
            _rfsgSession.Abort();
        }


        #region private methods
        bool CheckRampResolutionsValidity(double transientTime, int transientSteps, double dwellTime, int rampDownSteps,
                              double startPwrLevel, double stopPwrLevel)
        {
            if (startPwrLevel < stopPwrLevel)
                return false;

            if (transientTime > 0)
            {
                if (transientSteps > 1 & dwellTime > 0 & rampDownSteps > 1)
                    return true;
            }
            else if (transientTime == 0)
            {
                if (dwellTime > 0 & rampDownSteps > 1)
                    return true;
            }


            return false;
        }


        /// <summary>
        /// Calculate power and waveform for PC3 Power Ramping for NF test.
        /// </summary>
        /// <param name="iqdata"></param>
        /// <param name="startPwrLvl"></param>
        /// <param name="stopPwrLvl"></param>
        void CalcSweepPwrAndWfm(ComplexDouble[] iqdata, double startPwrLvl, double stopPwrLvl)
        {
            var rmpUpSubset = GetNormIQSubset(iqdata, _iqRate, _transientTime / _transientSteps, false);
            _enableTransient = rmpUpSubset.Length > 0;

            if (_enableTransient)
            {
                CalcSweepLevelByStartStop(stopPwrLvl, startPwrLvl, _transientSteps, rmpUpSubset,
                        out _rampUpPowerLevels, out _rampUpIqData);                
            }


            var rmpDownSubset = GetNormIQSubset(iqdata, _iqRate, _dwellTime, true);
            CalcSweepLevelByStartStop(startPwrLvl, stopPwrLvl, _rampDownSteps, rmpDownSubset,
                        out _rampDownPowerLevels, out _rampDownIqData);

            _peakPowerToSG = _papr + _rampDownPowerLevels[0];
            _totalWfmCount = Convert.ToInt32(_enableTransient) + _rampDownPowerLevels.Length;
        }

        /// <summary>
        /// Get subset of normalized IQ data in specified length.
        /// </summary>
        /// <param name="iqData"></param>
        /// <param name="iqRate"></param>
        /// <param name="wfmLengthInSecond"></param>
        /// <returns></returns>
        ComplexDouble[] GetNormIQSubset(ComplexDouble[] iqData, double iqRate, double wfmLengthInSecond, bool calcPAPR)
        {
            int availableSamples = iqData.Length;
            int subsetSize, remainder, quotient;

            if (availableSamples == 1)
            {
                iqData = new ComplexDouble[2] { iqData[0], iqData[0] };
                availableSamples = 2;
            }

            #region Calculate subset size and subset repeat count (quotient and remainder)
            if (wfmLengthInSecond > 0)
            {
                double requestedSamples = iqRate * wfmLengthInSecond;
                if (availableSamples < requestedSamples)
                {
                    subsetSize = availableSamples;
                    remainder = Convert.ToInt32(requestedSamples % Convert.ToDouble(availableSamples));
                    quotient = Convert.ToInt32(requestedSamples / Convert.ToDouble(availableSamples));
                }
                else
                {
                    subsetSize = Convert.ToInt32(requestedSamples);
                    remainder = 0;
                    quotient = 1;
                }
            }
            else
            {
                if (wfmLengthInSecond == 0)
                {
                    subsetSize = 0; remainder = 0; quotient = 0;
                }
                else
                {
                    subsetSize = availableSamples;
                    remainder = 0;
                    quotient = 1;
                }
            }

            // Make sure resulted samples are even number (by subtract it with 1 if it is odd)
            subsetSize = subsetSize - Convert.ToInt32(Math.IEEERemainder(subsetSize, 2));
            remainder = remainder - Convert.ToInt32(Math.IEEERemainder(remainder, 2));

            #endregion



            var subsetWfm = iqData.Take(subsetSize);

            for (int i = 0; i < quotient - 1; i++)
            {
                subsetWfm = subsetWfm.Concat(iqData.Take(subsetSize));
            }

            if (remainder >= 1)
            {
                subsetWfm = subsetWfm.Concat(iqData.Take(remainder));
            }

            if (subsetSize > 0)
            {
                #region Calculate PAPR and Normalize IQ
                double vmax = ComplexDouble.GetMagnitudes(subsetWfm.ToArray()).Max();


                if (calcPAPR)
                {
                    Filters.PAPR(subsetWfm.Select(iq => iq.Real).ToArray(),
                                 subsetWfm.Select(iq => iq.Imaginary).ToArray(),
                                 out _papr);
                }

                subsetWfm = ComplexDouble.ComposeArrayPolar(
                    ComplexDouble.GetMagnitudes(subsetWfm.ToArray()).Select(v => v / vmax).ToArray(),
                    ComplexDouble.GetPhases(subsetWfm.ToArray()));
                #endregion
            }



            return subsetWfm.ToArray();

        }

        /// <summary>
        /// Calculate levels of power sweeping based on start and stop power level
        /// </summary>
        /// <param name="startPwrLvl"></param>
        /// <param name="stopPwrLvl"></param>
        /// <param name="numOfSteps"></param>
        /// <param name="iqData"></param>
        /// <param name="sweepPwrLevels"></param>
        /// <param name="sweepingIqData"></param>
        void CalcSweepLevelByStartStop(double startPwrLvl, double stopPwrLvl, int numOfSteps, ComplexDouble[] iqData,
                                        out double[] sweepPwrLevels, out ComplexDouble[] sweepingIqData)
        {
            double sweepRange;
            sweepPwrLevels = new double[numOfSteps];
            sweepingIqData = new ComplexDouble[numOfSteps * iqData.Length];

            if (startPwrLvl < stopPwrLvl)      // Ramp up
            {
                sweepRange = stopPwrLvl - startPwrLvl;
                sweepRange *= -1;

                for (int i = 0; i < numOfSteps; i++)
                {
                    double pdiff_dB = sweepRange * (1 - ((double)i / (double)(numOfSteps - 1)));
                    sweepPwrLevels[i] = stopPwrLvl + pdiff_dB;
                    iqData.Select(
                        iq => iq.Multiply(new ComplexDouble(Math.Pow(10, (pdiff_dB / 20)), 0))).
                        ToArray().
                        CopyTo(sweepingIqData, i * iqData.Length);
                }
            }
            else                              // Ramp down
            {
                sweepRange = startPwrLvl - stopPwrLvl;
                sweepRange *= -1;

                for (int i = 0; i < numOfSteps; i++)
                {
                    double pdiff_dB = sweepRange * (double)i / (double)(numOfSteps - 1);
                    sweepPwrLevels[i] = stopPwrLvl - sweepRange + pdiff_dB;
                    iqData.Select(
                        iq => iq.Multiply(new ComplexDouble(Math.Pow(10, (pdiff_dB / 20)), 0))).
                        ToArray().
                        CopyTo(sweepingIqData, i * iqData.Length);
                }
            }
        }

        void WriteScriptForRampUpAndDown()
        {
            #region Build script and write waveforms
            StringBuilder scriptBuilder = new StringBuilder();
            scriptBuilder.AppendFormat("script PC3PwrRamping\r\n");
            
            if (_enableTransient)
            {
                try
                {
                    _rfsgSession.Arb.WriteWaveform("Transient", this._rampUpIqData);
                }
                catch
                {
                    // If fail to write waveform, then clear it before write the waveform
                    _rfsgSession.Arb.ClearWaveform("Transient");
                    _rfsgSession.Arb.WriteWaveform("Transient", this._rampUpIqData);
                }                
                
#if PWRRAMPING_TRIGGER
                scriptBuilder.Append("  generate Transient marker0(0)\r\n");
#else
                scriptBuilder.Append("  generate Transient\r\n");
#endif
            }

            try
            {
                _rfsgSession.Arb.WriteWaveform("Ramp", _rampDownIqData);
            }
            catch
            {
                // If fail to write waveform, then clear it before write the waveform
                _rfsgSession.Arb.ClearWaveform("Ramp");
                _rfsgSession.Arb.WriteWaveform("Ramp", _rampDownIqData);
            }            

            scriptBuilder.AppendFormat("  generate Ramp\r\n");
            

            scriptBuilder.Append("end script");
            #endregion

            // Write script 
            _rfsgSession.Arb.Scripting.WriteScript(scriptBuilder.ToString());

#if PWRRAMPING_TRIGGER
            // Export the marker event to the desired output terminal       
            _rfsgSession.DeviceEvents.MarkerEvents[0].ExportedOutputTerminal
                = RfsgMarkerEventExportedOutputTerminal.FromString("PXI_Trig2");
#endif
            _rfsgSession.Arb.Scripting.SelectedScriptName = "PC3PwrRamping";
        }

        #endregion

    }


}
