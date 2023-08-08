using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using NationalInstruments.ModularInstruments.Interop;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;

namespace LibEqmtDriver.SMU
{
    public class AemulusDM482ePPMU : Base_SMU, iSmu 
    {
        public override string VisaAlias { get; set; }
        public override string SerialNumber { get; set; }
        public override string ChanNumber { get; set; }
        public override string PinName { get; set; }
        public override byte Site { get; set; }
        public override double priorVoltage { get; set; }
        public override double priorCurrentLim { get; set; }
        public override double priorApertureTime { get; set; }

        public LibEqmtDriver.MIPI.iMiPiCtrl iMiPiCtrl = null;

        // Declare Aemulus Class
        Aemulus.Hardware.DM DM;

        public AemulusDM482ePPMU(string val, LibEqmtDriver.MIPI.iMiPiCtrl EqMiPiCtrl = null)
        {
            string tempVal = "";

            string visaAlias;
            string chNum;
            string pinName;

            string[] arSelected = new string[4];
            tempVal = val;
            arSelected = tempVal.Split('_');

            visaAlias = arSelected[3];
            pinName = arSelected[2];
            chNum = arSelected[1].Substring(2, 1);

            #region Get class from MIPI (Aemulus DM482e)
            iMiPiCtrl = EqMiPiCtrl;

            MIPI.Aemulus_DM482e aemulus_DM482E = iMiPiCtrl as MIPI.Aemulus_DM482e;
            DM = aemulus_DM482E.myDM;
            #endregion

            getPPMU(visaAlias, chNum, pinName, Site);

        }
        ~AemulusDM482ePPMU() { }

        public void getPPMU(string VisaAlias, string ChanNumber, string PinName, byte Site)
        {
            this.VisaAlias = VisaAlias;
            this.ChanNumber = ChanNumber;
            this.PinName = PinName.ToUpper();
            this.Site = Site;
        }      

        #region iSmu Interface

        // Ben - Add for setting VIO from digital to PPMU (Original code from RF1 - iEqDC interface functions) 
        void iSmu.Close()
        {

        }
        void iSmu.ForceVoltage(double voltsForce, double currentLimit, bool isTest = false)
        {

        }
        void iSmu.ForceVoltageOnlyVio(double voltsForce, double currentLimit)
        {

        }
        void iSmu.SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
        {

        }
        void iSmu.SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA = false)
        {

        }
        void iSmu.MeasureCurrent(int NumAverages, bool triggered, ref double Result)
        {

            return;
        }
        double[] iSmu.MeasureCurrentTrace()
        {
            double[] data = { 0 };

            return data;
        }
        void iSmu.SetupVoltageMeasure()
        {

        }
        void iSmu.MeasureVoltage(int NumAverages, ref double Result)
        {
            
        }
        void iSmu.SetupContinuity(double currentForce)
        {

        }
        void iSmu.MeasureContinuity(int avgs, ref double result)
        {

        }
        void iSmu.OutputEnable(bool state)
        {

        }
        void iSmu.CalSelfCalibrate()
        {

        }
        double iSmu.CheckDeviceTemperature()
        {
            return 0;
        }

        #endregion iSmu Interface
    }

    public class NI_PXIe6570_PPMU : Base_SMU, iSmu 
    {
        public override string VisaAlias { get; set; }
        public override string SerialNumber
        {
            get
            {
                ModularInstrumentsSystem Modules = new ModularInstrumentsSystem();
                foreach (DeviceInfo ModulesInfo in Modules.DeviceCollection)
                {
                    if (ModulesInfo.Name == VisaAlias)
                    {
                        return ModulesInfo.SerialNumber;
                    }
                }
                return "NA";
            }
        }
        public override string ChanNumber { get; set; }
        public override string PinName { get; set; }
        public override byte Site { get; set; }
        public override double priorVoltage { get; set; }
        public override double priorCurrentLim { get; set; }
        public override double priorApertureTime { get; set; }
        private double measureTimeLength;

        public LibEqmtDriver.MIPI.iMiPiCtrl iMiPiCtrl = null;

        public NI_PXIe6570_PPMU(string val, LibEqmtDriver.MIPI.iMiPiCtrl EqMiPiCtrl = null)
        {
            string tempVal = "";

            string visaAlias;
            string chNum;
            string pinName;

            string[] arSelected = new string[4];
            tempVal = val;
            arSelected = tempVal.Split('_');

            visaAlias = arSelected[3];
            pinName = arSelected[2];
            chNum = arSelected[1].Substring(2, 1);

            iMiPiCtrl = EqMiPiCtrl;

            getPPMU(visaAlias, chNum, pinName, Site);
        }        
        
        ~NI_PXIe6570_PPMU() { }

        public void getPPMU(string VisaAlias, string ChanNumber, string PinName,byte Site)
        {
            this.VisaAlias = VisaAlias;
            this.ChanNumber = ChanNumber;
            this.PinName = PinName.ToUpper();
            this.Site = Site;
        }

        private NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet _pin;
        private NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin
        {
            get
            {
                if (_pin == null)
                {
                    MIPI.NI_PXIe6570 hsdio = iMiPiCtrl as MIPI.NI_PXIe6570;
                    _pin = hsdio.myMipiCtrl.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                }
                return _pin;
            }
        }

        #region iSmu Interface

        void iSmu.Close()
        {

        }
        // Ben - Add for setting VIO from digital to PPMU (Original code from RF1 - iEqDC interface functions)       
        void iSmu.ForceVoltage(double voltsForce, double currentLimit, bool isTest = false)
        {
            try
            {
                // Configure 6570 for PPMU measurements, Output Voltage, Measure Current
                if (currentLimit != priorCurrentLim || voltsForce != priorVoltage || isTest)
                {
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
                    pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCVoltage;

                    //  Using the requested current limit to decide the current level range from the values supported for private release of 6570
                    double range = currentLimit;
                    if (Math.Abs(range) <= 2e-6) { range = 2e-6; } // +-2uA
                    else if (Math.Abs(range) <= 32e-6) { range = 32e-6; } // +-32uA
                    else if (Math.Abs(range) <= 128e-6) { range = 128e-6; } // +-128uA
                    else if (Math.Abs(range) <= 2e-3) { range = 2e-3; } // +-2mA
                    else if (Math.Abs(range) <= 32e-3) { range = 32e-3; } // +-32mA}
                    else
                    {
                        range = 32e-3;
                        MessageBox.Show("Max allowed current range is 32mA! forced set 32mA.");
                    }

                    pin.Ppmu.DCCurrent.CurrentLevelRange = isTest ? range : 32e-3;
                    pin.Ppmu.DCVoltage.CurrentLimitRange = isTest ? range : 32e-3;
                    pin.Ppmu.Source();

                    //   Force Voltage Configure
                    if (priorVoltage == 0 && voltsForce >= 1 && !isTest) // To prevent the damage of the ESD Circuit by the fast Vio rising - DH
                    {
                        pin.Ppmu.DCVoltage.VoltageLevel = 0;
                        pin.Ppmu.Source();
                        pin.Ppmu.DCVoltage.VoltageLevel = 0.3;
                        pin.Ppmu.Source();
                        pin.Ppmu.DCVoltage.VoltageLevel = 0.6;
                        pin.Ppmu.Source();
                        pin.Ppmu.DCVoltage.VoltageLevel = 1;
                        pin.Ppmu.Source();
                        pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;
                        pin.Ppmu.Source();
                    }
                    else
                    {
                        pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;
                        pin.Ppmu.Source();
                    }

                }

                priorCurrentLim = currentLimit;
                priorVoltage = voltsForce;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "ForceVoltage");
            }
        }
        void iSmu.ForceVoltageOnlyVio(double voltsForce, double currentLimit)
        {
            try
            {
                // Configure 6570 for PPMU measurements, Output Voltage, Measure Current

                pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
                pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCVoltage;
                //   Force Voltage Configure
                pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;

                //  Using the requested current limit to decide the current level range from the values supported for private release of 6570
                double range = currentLimit;
                if (Math.Abs(range) <= 2e-6) { range = 2e-6; } // +-2uA
                else if (Math.Abs(range) <= 32e-6) { range = 32e-6; } // +-32uA
                else if (Math.Abs(range) <= 128e-6) { range = 128e-6; } // +-128uA
                else if (Math.Abs(range) <= 2e-3) { range = 2e-3; } // +-2mA
                else if (Math.Abs(range) <= 32e-3) { range = 32e-3; } // +-32mA}
                else
                {
                    range = 32e-3;
                    MessageBox.Show("Max allowed current range is 32mA! forced set 32mA.");
                }

                pin.Ppmu.DCCurrent.CurrentLevelRange = range;
                pin.Ppmu.DCVoltage.CurrentLimitRange = range;
                //       Perform Voltage Force
                pin.Ppmu.Source();

                priorCurrentLim = currentLimit;
                priorVoltage = voltsForce;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "ForceVoltage");
            }
        }
        
        void iSmu.SetupCurrentMeasure(bool UseIQsettings, bool triggerFromSA)
        {

        }

        void iSmu.MeasureCurrent(int NumAverages, bool triggered, ref double Result)
        {
            Result = MeasureCurrent(NumAverages, true);
            return;
        }
        double MeasureCurrent(int NumAverages, bool resetCompliance = true)
        {
            try
            {
                double[] meas = new double[32];

                // Measure Current
                meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Current);
                return meas[0];
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "MeasureCurrent");
                return 0;
            }
            finally
            {
                if (resetCompliance)
                {
                    try
                    {
                        pin.Ppmu.DCCurrent.CurrentLevelRange = 32e-3;
                        pin.Ppmu.DCVoltage.CurrentLimitRange = 32e-3;
                        pin.Ppmu.Source();
                    }
                    catch { }
                }
            }
        }
        void iSmu.SetupCurrentTraceMeasurement(double measureTimeLength, double aperture, bool triggered, bool triggerFromSA)
        {
            this.measureTimeLength = measureTimeLength;
        }
        double[] iSmu.MeasureCurrentTrace()
        {
            try
            {
                List<double> measurements = new List<double>();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                double result = 0;

                while (sw.ElapsedMilliseconds <= measureTimeLength * 1000 || measurements.Count() < 1)
                {
                    result = MeasureCurrent(1, false);
                    measurements.Add(result);
                }

                return measurements.ToArray();
            }
            catch (Exception)
            {
                return new double[16];
            }
            finally
            {
                try
                {
                    pin.Ppmu.DCCurrent.CurrentLevelRange = 32e-3;
                    pin.Ppmu.DCVoltage.CurrentLimitRange = 32e-3;
                    pin.Ppmu.Source();
                }
                catch { }
            }
        }
        
        void iSmu.SetupVoltageMeasure()
        {
            // Configure for PPMU Measurements
            pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
        }
        void iSmu.MeasureVoltage(int NumAverages, ref double Result)
        {
            try
            {
                double[] meas = new double[32];
                // Configure Number of Averages by setting the Apperture Time
                pin.Ppmu.ConfigureApertureTime(0.0020 * (double)(NumAverages), NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);
                // Measure Voltage
                meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Voltage);

                Result = meas[0];
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "MeasureCurrent");
                return;
            }
        }
        void iSmu.SetupContinuity(double currentForce)
        {

        }
        void iSmu.MeasureContinuity(int avgs, ref double result)
        {

        }
        void iSmu.OutputEnable(bool state)
        {

        }
        void iSmu.CalSelfCalibrate()
        {

        }
        double iSmu.CheckDeviceTemperature()
        {
            return 0;
        }
        #endregion iSmu Interface
    }
}