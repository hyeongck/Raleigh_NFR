using ClothoLibAlgo;
using Ivi.Driver;
using NationalInstruments.ModularInstruments.NIDigital;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace LibEqmtDriver.MIPI
{
    public partial class EqHSDIO
    {
        public partial class NI6570 : EqHSDIObase
        {
            /*
             * Notes:  Requires the following References added to project (set Copy Local = false):
             *   - NationalInstruments.ModularInstruments.NIDigital.Fx40
             *   - Ivi.Driver
             */

            // The Instrument Session, int bus_no = 1
            public NIDigital DIGI;

            public string SerialNumber
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

            #region Private Variables

            private string allRffeChans, allRffeChanswoTrig, allEEPROMChans, allTEMPSENSEChans, allEEPROM_UNIOChans, allRffeChanswoVio;

            private DigitalPinSet
                allEEPROMPins, EEPROMVccPin, EEPROMsckPin,
                allRffePins, allRffePinswoTrig, allRffePinswoVio, sdata1Pin, sclk1Pin, sdata2Pin, sclk2Pin, trigPin,
                allTEMPSENSEPins, TEMPSENSEsckPin, TEMPSENSEVccPin,
                allEEPROM_UNIOPins, EEPROM_UNIOVpupPin, EEPROM_UNIOSIOPin, EEPROM_UNIOVpup2Pin, EEPROM_UNIOSIO2Pin;

            private string[] allDutPins = new string[] { };
            private List<string> loadedPatternFiles; // used to store previously loaded patterns so we don't try and double load.  Double Loading will cause an error, so always check this list to see if pattern was previously loaded.
            private List<string> loadedPatternNames; // used to check if a pattern of that name was loaded without execution error. (message popup) make TCF debug easier
            private double MIPIClockRate;  // MIPI NRZ Clock Rate (2 x Vector Rate)
            private double EEPROMClockRate;  // EEPROM NRZ Clock Rate (2 x Vector Rate)
            private double UNIORate;
            private bool eepromReadWriteEnabled = false;
            private double StrobePoint;
            private bool forceDigiPatRegeneration = true; //false;  // Set this to true if you want to re-generate all .digipat files from the .vec files, even if the .vec files haven't changed.
            private int NumBitErrors; // Stores the number of bit errors from the most recently executed pattern.
            private Dictionary<string, uint> captureCounters = new Dictionary<string, uint>(); // This dictionary stores the # of captures each .vec contains (for .vec files that are converted to capture format)
            private string fileDir; // This is the path used to store intermediate digipatsrc, digipat, and other files.
            private TrigConfig triggerConfig = TrigConfig.PXI_Backplane;  // No Triggering by default
            private PXI_Trig pxiTrigger = PXI_Trig.PXI_Trig2;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG7 shouldn't interfere.
            private uint regWriteTriggerCycleDelay = 0;

            private bool debug = false; // Turns on additional console messages if true

            private string I2CVCCChanName; // = "VCC";
            private string I2CSDAChanName; // = "SDA";
            private string I2CSCKChanName; // = "SCK";
            private string TEMPSENSEI2CVCCChanName; // = "TSVCC";

            private string UNIOVPUPChanName = "UNIO_VPUP";
            private string UNIOSIOChanName = "UNIO_SIO";
            private string UNIOVPUP2ChanName = "UNIO2_VPUP";
            private string UNIOSIO2ChanName = "UNIO2_SIO";

            //Temp Sense State
            private bool TempSenseStateOn = false;

            private double TempSenseRaw = 0;

            private DigitalTimeSet tsNRZ;

            // keng shan Added
            private Dictionary<string, uint[]> SourceWaveform = new Dictionary<string, uint[]>();

            private CustomMipiLevel m_DeviceMipiLevel;
            private object lockObject = new object();

            //  Reuse the channel names varibles from the other vectors to avoid hardcoding names Ken Hilla
            // if you need to add more channels define them in the custom testplan (Digital_Definitions_Part_Specific) then modify the Initialize function here to set them up

            //private const string Sclk1ChanName = "Sclk_TX", Sdata1ChanName = "Sdata_TX", Vio1ChanName = "Vio_TX",
            //    Sclk2ChanName = "Sclk_RX", Sdata2ChanName = "Sdata_RX", Vio2ChanName = "Vio_RX", TrigChanName = "Trig";

            public class CustomMipiLevel
            {
                public double vih, vil, voh, vol, vtt;

                public CustomMipiLevel(double vih, double vil, double voh, double vol, double vtt)
                {
                    this.vih = vih;
                    this.vil = vil;
                    this.voh = voh;
                    this.vol = vol;
                    this.vtt = vtt;
                }
            }

            #endregion Private Variables

            /// <summary>
            /// Initialize the NI 6570 Instrument:
            ///   - Open Instrument session
            ///   - Reset Instrument and Unload All Patterns from Instrument Memory
            ///   - Configure Pin -> Channel Mapping
            ///   - Configure Timing for: MIPI (6556 style NRZ) & MIPI_SCLK_RZ (6570 style RZ)
            ///   - Configure 6570 in Digital mode by default (instead of PPMU mode)
            /// </summary>
            /// <param name="visaAlias">The VISA Alias of the instrument, typically NI6570.</param>
            public override bool Initialize()
            {
                Dictionary<string, string> ifRFFEisNIDC = new Dictionary<string, string>();
                ifRFFEisNIDC.Add("SDATA1", "1");
                ifRFFEisNIDC.Add("SCLK1", "0");
                ifRFFEisNIDC.Add("VIO1", "2");
                ifRFFEisNIDC.Add("SDATA2", "5");
                ifRFFEisNIDC.Add("SCLK2", "4");
                ifRFFEisNIDC.Add("VIO2", "3");

                ////Tx
                //TrigOffRz = Digital_Mipi_Trig["TrigOffRz".ToUpper()];
                //TrigOnRz = Digital_Mipi_Trig["TrigOnRz".ToUpper()];
                //TrigMaskOnRz = Digital_Mipi_Trig["TrigMaskOnRz".ToUpper()];

                ////Rx
                //TrigOffRzRx = Digital_Mipi_Trig["TrigOffRzRx".ToUpper()];
                //TrigOnRzRx = Digital_Mipi_Trig["TrigOnRzRx".ToUpper()];
                //TrigMaskOnRzRx = Digital_Mipi_Trig["TrigMaskOnRzRx".ToUpper()];

                // Clock Rate & Cable Delay
                MIPIClockRate = EqHSDIO.MIPIClockRate; // 52e6; // This is the Non-Return to Zero rate, Actual Vector rate is 1/2 of this.
                EEPROMClockRate = 2e5; // This is the Non-Return to Zero rate, Actual Vector rate is 1/2 of this.
                UNIORate = 1e6;        // This is the Non-Return to Zero rate, UNI/O has no clock.
                                       //TempSenseClockRate = 2e5;

                // Set these values based on calling ((HSDIO.NI6570)HSDIO.Instrument).shmoo("QC_Test");
                // Ideally, try to set UserDelay = 0 if possible and only modify StrobePoint.
                StrobePoint = EqHSDIO.StrobePoint; //Maximator compare delay for demo board using flying lead probe         // 77e-9;
                regWriteTriggerCycleDelay = 0;

                // Trigger Configuration;  This applies to the RegWrite command and will send out a hardware trigger
                // on the specified triggers (Digital Pin, PXI Backplane, or Both) at the end of the Register Write operation.
                triggerConfig = TrigConfig.Digital_Pin;
                pxiTrigger = PXI_Trig.PXI_Trig2;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG6 shouldn't interfere.

                #region Initialize Private Variables

                fileDir = Path.GetTempPath() + "NI.Temp\\NI6570";
                Directory.CreateDirectory(fileDir);

                #endregion Initialize Private Variables

                #region Initialize Instrument

                // Initialize private variables
                loadedPatternFiles = new List<string> { };
                loadedPatternNames = new List<string> { };

                string OptionString = (Eq.SharedConfiguration.RunOptions & RunOption.SIMULATE) == RunOption.SIMULATE ? "Simulate= 1,DriverSetup=Model:6570" : "";
                DIGI = new NIDigital(this.VisaAlias, false, true, OptionString);
                DIGI.Utility.ResetDevice();

                #endregion Initialize Instrument

                Eq.InstrumentInfo += GetInstrumentInfo();

                #region NI Pin Map Configuration

                // Make sure you add all needed pins here so that they get auto-added to all NI-6570 digipat files.  If they aren't in allDutPins or allSystemPins, you can't use them.

                //Sclk1ChanName = Get_Digital_Definition("SCLK1_VEC_NAME");
                //Sdata1ChanName = Get_Digital_Definition("SDATA1_VEC_NAME");
                //Vio1ChanName = Get_Digital_Definition("VIO1_VEC_NAME");

                //if (Num_Mipi_Bus == 2)
                //{
                //    Sclk2ChanName = Get_Digital_Definition("SCLK2_VEC_NAME");
                //    Sdata2ChanName = Get_Digital_Definition("SDATA2_VEC_NAME");
                //    Vio2ChanName = Get_Digital_Definition("VIO2_VEC_NAME");
                //}

                //ShieldChanName = Get_Digital_Definition("SHIELD_VEC_NAME");
                TrigChanName = Get_Digital_Definition("TRIG_VEC_NAME");
                I2CVCCChanName = Get_Digital_Definition("I2C_VCC_VEC_NAME");
                I2CSCKChanName = Get_Digital_Definition("I2C_SCK_VEC_NAME");
                I2CSDAChanName = Get_Digital_Definition("I2C_DAC_VEC_NAME");
                TEMPSENSEI2CVCCChanName = Get_Digital_Definition("TEMPSENSE_I2C_VCC_VEC_NAME");

                UNIOVPUPChanName = Get_Digital_Definition("UNIO_VPUP_VEC_NAME");
                UNIOSIOChanName = Get_Digital_Definition("UNIO_SIO_VEC_NAME");

                UNIOVPUP2ChanName = Get_Digital_Definition("UNIO2_VPUP_VEC_NAME");
                UNIOSIO2ChanName = Get_Digital_Definition("UNIO2_SIO_VEC_NAME");

                // Map extra pins that are not included in the TCF as of 10/07/2015
                //PinNamesAndChans[ShieldChanName] = Get_Digital_Definition("SHIELD_CHANNEL"); //"8";
                PinNamesAndChans[TrigChanName] = Get_Digital_Definition("TRIG_CHANNEL");
                PinNamesAndChans[I2CVCCChanName] = Get_Digital_Definition("I2C_VCC_CHANNEL");
                PinNamesAndChans[I2CSCKChanName] = Get_Digital_Definition("I2C_SCK_CHANNEL");
                PinNamesAndChans[I2CSDAChanName] = Get_Digital_Definition("I2C_SDA_CHANNEL");
                PinNamesAndChans[TEMPSENSEI2CVCCChanName] = Get_Digital_Definition("TEMPSENSE_I2C_VCC_CHANNEL");

                // UNIO
                PinNamesAndChans[UNIOVPUPChanName] = Get_Digital_Definition("UNIO_VPUP_CHANNEL");
                PinNamesAndChans[UNIOSIOChanName] = Get_Digital_Definition("UNIO_SIO_CHANNEL");

                PinNamesAndChans[UNIOVPUP2ChanName] = Get_Digital_Definition("UNIO2_VPUP_CHANNEL");
                PinNamesAndChans[UNIOSIO2ChanName] = Get_Digital_Definition("UNIO2_SIO_CHANNEL");

                allRffeChans = string.Join(",",
                    (Num_Mipi_Bus == 2) ?
                        new string[] { Sclk1ChanName.ToUpper(), Sdata1ChanName.ToUpper(), Vio1ChanName.ToUpper(), Sclk2ChanName.ToUpper(), Sdata2ChanName.ToUpper(), Vio2ChanName.ToUpper(), TrigChanName.ToUpper() } :
                        new string[] { Sclk1ChanName.ToUpper(), Sdata1ChanName.ToUpper(), Vio1ChanName.ToUpper(), TrigChanName.ToUpper() }
                    );

                allRffeChanswoTrig = string.Join(",",
                    (Num_Mipi_Bus == 2) ?
                        new string[] { Sclk1ChanName.ToUpper(), Sdata1ChanName.ToUpper(), Vio1ChanName.ToUpper(), Sclk2ChanName.ToUpper(), Sdata2ChanName.ToUpper(), Vio2ChanName.ToUpper() } :
                        new string[] { Sclk1ChanName.ToUpper(), Sdata1ChanName.ToUpper(), Vio1ChanName.ToUpper() }
                    );

                allRffeChanswoVio = string.Join(",",
                    new string[] { Sclk1ChanName.ToUpper(), Sdata1ChanName.ToUpper(), Sclk2ChanName.ToUpper(), Sdata2ChanName.ToUpper(), /*Vio2ChanName.ToUpper(),*/ TrigChanName.ToUpper() });

                allEEPROMChans = string.Join(",", new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), I2CVCCChanName.ToUpper() });
                allTEMPSENSEChans = string.Join(",", new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), TEMPSENSEI2CVCCChanName.ToUpper() });
                allEEPROM_UNIOChans = string.Join(",", new string[] { UNIOVPUPChanName.ToUpper(), UNIOSIOChanName.ToUpper(), UNIOVPUP2ChanName.ToUpper(), UNIOSIO2ChanName.ToUpper() });

                this.allDutPins =
                    (Num_Mipi_Bus == 2) ?
                    new string[] { Sclk1ChanName, Sdata1ChanName, Vio1ChanName, Sclk2ChanName, Sdata2ChanName, Vio2ChanName, TrigChanName, I2CSCKChanName, I2CSDAChanName, I2CVCCChanName, TEMPSENSEI2CVCCChanName, UNIOVPUPChanName, UNIOSIOChanName, UNIOVPUP2ChanName, UNIOSIO2ChanName } :
                    new string[] { Sclk1ChanName, Sdata1ChanName, Vio1ChanName, TrigChanName, I2CSCKChanName, I2CSDAChanName, I2CVCCChanName, TEMPSENSEI2CVCCChanName, UNIOVPUPChanName, UNIOSIOChanName, UNIOVPUP2ChanName, UNIOSIO2ChanName };

                // Map all pins that are defined in the TCF as well as any other "extra" pins such as Trigger, and EEPROM
                // Create combined Pin List
                string[] allPinsUpperCase = new string[allDutPins.Length];
                int i = 0;
                foreach (string pin in allDutPins)
                    allPinsUpperCase[i++] = pin.ToUpper();

                // Configure 6570 Pin Map with all pins
                DIGI.PinAndChannelMap.CreatePinMap(allPinsUpperCase, null);
                DIGI.PinAndChannelMap.CreateChannelMap(1);
                foreach (string pin in allDutPins)
                {
                    var isThere = Eq.Site[0].DC.Where(t => t.Key.Equals(pin, StringComparison.OrdinalIgnoreCase));
                    if (isThere.Count() > 0 &&
                         isThere.First().Value is EqDC.nidcbase &&
                         ifRFFEisNIDC.ContainsKey(isThere.First().Key.ToUpper())) PinNamesAndChans[pin] = ifRFFEisNIDC[isThere.First().Key.ToUpper()];
                    DIGI.PinAndChannelMap.MapPinToChannel(pin, 0, PinNamesAndChans[pin]);
                }

                DIGI.PinAndChannelMap.EndChannelMap();

                // Get DigitalPinSets
                allRffePins = DIGI.PinAndChannelMap.GetPinSet(allRffeChans);
                allRffePinswoVio = DIGI.PinAndChannelMap.GetPinSet(allRffeChanswoVio); // Pinot added (Pcon)
                allRffePinswoTrig = DIGI.PinAndChannelMap.GetPinSet(allRffeChanswoTrig);
                allEEPROMPins = DIGI.PinAndChannelMap.GetPinSet(allEEPROMChans);
                EEPROMsckPin = DIGI.PinAndChannelMap.GetPinSet(I2CSCKChanName.ToUpper());
                EEPROMVccPin = DIGI.PinAndChannelMap.GetPinSet(I2CVCCChanName.ToUpper());

                allEEPROM_UNIOPins = DIGI.PinAndChannelMap.GetPinSet(allEEPROM_UNIOChans);
                EEPROM_UNIOVpupPin = DIGI.PinAndChannelMap.GetPinSet(UNIOVPUPChanName.ToUpper());
                EEPROM_UNIOSIOPin = DIGI.PinAndChannelMap.GetPinSet(UNIOSIOChanName.ToUpper());

                EEPROM_UNIOVpup2Pin = DIGI.PinAndChannelMap.GetPinSet(UNIOVPUP2ChanName.ToUpper());
                EEPROM_UNIOSIO2Pin = DIGI.PinAndChannelMap.GetPinSet(UNIOSIO2ChanName.ToUpper());

                sclk1Pin = DIGI.PinAndChannelMap.GetPinSet(Sclk1ChanName.ToUpper());
                sdata1Pin = DIGI.PinAndChannelMap.GetPinSet(Sdata1ChanName.ToUpper());

                if (Num_Mipi_Bus == 2)
                {
                    sclk2Pin = DIGI.PinAndChannelMap.GetPinSet(Sclk2ChanName.ToUpper());
                    sdata2Pin = DIGI.PinAndChannelMap.GetPinSet(Sdata2ChanName.ToUpper());
                }

                trigPin = DIGI.PinAndChannelMap.GetPinSet(TrigChanName.ToUpper());
                allTEMPSENSEPins = DIGI.PinAndChannelMap.GetPinSet(allTEMPSENSEChans);
                TEMPSENSEsckPin = DIGI.PinAndChannelMap.GetPinSet(I2CSCKChanName.ToUpper());
                TEMPSENSEVccPin = DIGI.PinAndChannelMap.GetPinSet(TEMPSENSEI2CVCCChanName.ToUpper());

                #endregion NI Pin Map Configuration

                #region MIPI Level Configuration

                double vih = Convert.ToDouble(Get_Digital_Definition("VIH"));
                double vil = Convert.ToDouble(Get_Digital_Definition("VIL"));
                double voh = Convert.ToDouble(Get_Digital_Definition("VOH"));
                double vol = Convert.ToDouble(Get_Digital_Definition("VOL"));
                //double voh = .9;  KH experiment to improve temp sense read
                //double vol = 0.8;
                double vtt = 3.0;
                m_DeviceMipiLevel = new CustomMipiLevel(vih, vil, voh, vol, vtt);

                allRffePins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                trigPin.DigitalLevels.ConfigureVoltageLevels(0.0, 5.0, 0.5, 2.5, 5.0); // Set VST Trigger Channel to 5V logic.  VST's PFI0 VIH is 2.0V, absolute max is 5.5V

                #endregion MIPI Level Configuration

                #region EEPROM Level Configuration

                vih = 5.0;
                vil = 0.0;
                voh = 2.4;
                vol = 1.0;
                vtt = 5.0;
                allEEPROMPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);

                #endregion EEPROM Level Configuration

                #region EEPROM UNI/O Level Configuration

                // UNIO
                vih = 2.7;
                vil = 0;
                voh = 2;
                vol = 0.5;
                vtt = 2.7;
                allEEPROM_UNIOPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);

                #endregion EEPROM UNI/O Level Configuration

                #region TEMPSENSE Level Configuration

                vih = 3.0;
                vil = 0.0;
                voh = 1.5;
                vol = 1.0;
                vtt = 3.0;
                allTEMPSENSEPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                TEMPSENSEVccPin.DigitalLevels.Vcom = vtt;

                #endregion TEMPSENSE Level Configuration

                #region Timing Variable Declarations

                // Variables
                double period_dbl;
                Ivi.Driver.PrecisionTimeSpan period;
                Ivi.Driver.PrecisionTimeSpan driveOn, driveOn_half;
                Ivi.Driver.PrecisionTimeSpan driveData, driveData_half;
                Ivi.Driver.PrecisionTimeSpan driveReturn, driveReturn_half;
                Ivi.Driver.PrecisionTimeSpan driveOff, driveOff_half;
                Ivi.Driver.PrecisionTimeSpan compareStrobe;
                Ivi.Driver.PrecisionTimeSpan clockRisingEdgeDelay;
                Ivi.Driver.PrecisionTimeSpan clockFallingEdgeDelay;

                #endregion Timing Variable Declarations

                #region MIPI Timing Configuration

                #region Timing configuration for Return/Non-Return to Zero format Patterns.

                period_dbl = 1.0 / MIPIClockRate;

                CreateTimeSet(timeset: Timeset.MIPI_RZ, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 1, clockRisingEdgeDelayRaw: period_dbl / 8); ///RZ
                CreateTimeSet(timeset: Timeset.MIPI, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 1, driveOffAmp: 1, compareStrobeAmp: 1, clockRisingEdgeDelayRaw: 0, driveallpin: DriveFormat.NonReturn, driveclkpin: DriveFormat.NonReturn); ///NRZ (eg: 6556 style).

                period_dbl = 1.0 / (MIPIClockRate / 2);
                CreateTimeSet(timeset: Timeset.MIPI_RZ_HALF, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 2, clockRisingEdgeDelayRaw: period_dbl / 8); ///RZ
                CreateTimeSet(timeset: Timeset.MIPI_HALF, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 1, driveOffAmp: 1, compareStrobeAmp: 2, clockRisingEdgeDelayRaw: 0, driveallpin: DriveFormat.NonReturn, driveclkpin: DriveFormat.NonReturn); ///NRZ

                period_dbl = 1.0 / (MIPIClockRate / 4);
                CreateTimeSet(timeset: Timeset.MIPI_RZ_QUAD, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 4, clockRisingEdgeDelayRaw: period_dbl / 8);

                period_dbl = 1.0 / 10e6;
                CreateTimeSet(timeset: Timeset.MIPI_RZ_10MHZ, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: MIPIClockRate / 10e6, clockRisingEdgeDelayRaw: period_dbl / 8);

                #endregion Timing configuration for Return/Non-Return to Zero format Patterns.

                #endregion MIPI Timing Configuration

                #region MIPI RFONOFFTime Timing Configuration

                #region Timing configuration for Return to Zero format Patterns.                                                            ////////////////Thaison, Frank

                // All RegRead / RegWrite functions use the RZ format for SCLK

                // Vector Rate is Clock Toggle Rate.
                // Compute timing values, shift all clocks out by 2 x periods so we can adjust the strobe "backwards" if needed.
                period_dbl = 1.0 / MIPIClockRate;
                //period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                //driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                //driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                //driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.5 * period_dbl);
                //driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(3.0 * period_dbl);
                //compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(StrobePoint);

                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                //CM WONG: Jedi is 0.5, PC3 is 0
                //driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.5 * period_dbl);
                //driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.5 * period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.5 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(StrobePoint);

                driveOn_half = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0 * period_dbl);
                driveData_half = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0 * period_dbl);
                driveReturn_half = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1 * period_dbl);
                driveOff_half = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);

                clockRisingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);   // This is the amount of time after SDATA is set to high or low before SCLK is set high.
                                                                                      // By setting this > 0, this will slightly delay the SCLK rising edge which can help ensure
                                                                                      // SDATA is settled before clocking in the value at the DUT.
                                                                                      // Note: This does not shift the Falling Edge of SCLK.  This means that adjusting this value will
                                                                                      //  reduce the overall duty cycle of SCLK.  You must adjuct clockFallingEdgeDelay by the same amount
                                                                                      //  if you would like to maintain a 50% duty cycle.
                clockFallingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);

                // Create Timeset
                var tsRZ = DIGI.Timing.CreateTimeSet(Timeset.MIPI_RFONOFF.ToString("g"));
                DigitalTimeSet tsRZ_Half = DIGI.Timing.CreateTimeSet(Timeset.MIPI_HALF_RFONOFF.ToString("g"));
                tsRZ.ConfigurePeriod(period);
                tsRZ_Half.ConfigurePeriod(period * 2);

                // Vio, Sdata, Trig
                tsRZ.ConfigureDriveEdges(allRffePins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsRZ.ConfigureCompareEdgesStrobe(allRffePins, compareStrobe);
                // Sclk
                tsRZ.ConfigureDriveEdges(sclk1Pin, DriveFormat.ReturnToLow, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsRZ.ConfigureCompareEdgesStrobe(sclk1Pin, compareStrobe);
                tsRZ.ConfigureDriveEdges(sclk2Pin, DriveFormat.ReturnToLow, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsRZ.ConfigureCompareEdgesStrobe(sclk2Pin, compareStrobe);

                // Vio, Sdata, Trig - Half Clk (Read)
                tsRZ_Half.ConfigureDriveEdges(allRffePins, DriveFormat.NonReturn, driveOn_half, driveData_half, driveReturn_half, driveOff_half);
                tsRZ_Half.ConfigureCompareEdgesStrobe(allRffePins, compareStrobe);
                // Sclk - Half Clk (Read)
                tsRZ_Half.ConfigureDriveEdges(sclk1Pin, DriveFormat.ReturnToLow, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn_half + clockFallingEdgeDelay, driveOff_half + clockFallingEdgeDelay);
                tsRZ_Half.ConfigureCompareEdgesStrobe(sclk1Pin, compareStrobe);
                tsRZ_Half.ConfigureDriveEdges(sclk2Pin, DriveFormat.ReturnToLow, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn_half + clockFallingEdgeDelay, driveOff_half + clockFallingEdgeDelay);
                tsRZ_Half.ConfigureCompareEdgesStrobe(sclk2Pin, compareStrobe);

                #endregion Timing configuration for Return to Zero format Patterns.                                                            ////////////////Thaison, Frank

                #region Timing configuration for Non Return to Zero format Patterns (eg: 6556 style).

                // Standard .vec files use the Non Return to Zero Format

                //Actual Vector Rate is still 1/2 Clock Toggle Rate.
                // Compute timing values, shift all clocks out by 2 x periods so we can adjust the strobe "backwards" if needed.
                period_dbl = 1.0 / MIPIClockRate;
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.5 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.5 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.0 * period_dbl);
                //compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(StrobePoint);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.8 * period_dbl);

                clockRisingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0); //period / 8;  // This is the amount of time after SDATA is set to high or low before SCLK is set high.
                                                                                    // By setting this > 0, this will slightly delay the SCLK rising edge which can help ensure
                                                                                    // SDATA is settled before clocking in the value at the DUT.
                                                                                    // Note: This does not shift the Falling Edge of SCLK.  This means that adjusting this value will
                                                                                    //  reduce the overall duty cycle of SCLK.  You must adjuct clockFallingEdgeDelay by the same amount
                                                                                    //  if you would like to maintain a 50% duty cycle.
                clockFallingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);

                // Create Timeset
                tsNRZ = DIGI.Timing.CreateTimeSet(Timeset.MIPI_SCLK_NRZ_RFONOFF.ToString("g"));
                tsNRZ.ConfigurePeriod(period);

                // Vio, Sdata, Trig
                tsNRZ.ConfigureDriveEdges(allRffePins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsNRZ.ConfigureCompareEdgesStrobe(allRffePins, compareStrobe);
                // Sclk
                tsNRZ.ConfigureDriveEdges(sclk1Pin, DriveFormat.NonReturn, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsNRZ.ConfigureCompareEdgesStrobe(sclk1Pin, compareStrobe);
                tsNRZ.ConfigureDriveEdges(sclk2Pin, DriveFormat.NonReturn, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsNRZ.ConfigureCompareEdgesStrobe(sclk2Pin, compareStrobe);

                #endregion Timing configuration for Non Return to Zero format Patterns (eg: 6556 style).

                #endregion MIPI RFONOFFTime Timing Configuration

                #region EEPROM Timing Configuration

                // Current EEPROM Implementation uses the Non Return to Zero Clock Format
                //Actual Vector Rate is 1/2 Clock Toggle Rate.
                period_dbl = 1.0 / EEPROMClockRate;
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.9 * period_dbl);

                // Create EEPROM NRZ Timeset
                DigitalTimeSet tsEEPROMNRZ = DIGI.Timing.CreateTimeSet(Timeset.EEPROM.ToString("g"));
                tsEEPROMNRZ.ConfigurePeriod(period);

                tsEEPROMNRZ.ConfigureDriveEdges(allEEPROMPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsEEPROMNRZ.ConfigureCompareEdgesStrobe(allEEPROMPins, compareStrobe);

                // Shift all EEPROM SCK edges by 1/4 Period so SDA is stable before clock rising edge
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.15 * period_dbl);
                // Set EEPROM SCK timing
                tsEEPROMNRZ.ConfigureDriveEdges(EEPROMsckPin, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);

                #endregion EEPROM Timing Configuration

                #region EEPROM UNI/O Timing Configuration

                // UNIO
                period_dbl = 1.0 / UNIORate;
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.98 * period_dbl);

                // UNIO EEPROM NRZ Timeset
                DigitalTimeSet tsEEPROMUNIONRZ = DIGI.Timing.CreateTimeSet(Timeset.UNIO_EEPROM.ToString("g"));
                tsEEPROMUNIONRZ.ConfigurePeriod(period);

                tsEEPROMUNIONRZ.ConfigureDriveEdges(allEEPROM_UNIOPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsEEPROMUNIONRZ.ConfigureCompareEdgesStrobe(allEEPROM_UNIOPins, compareStrobe);

                #endregion EEPROM UNI/O Timing Configuration

                #region TEMPSENSE Timing Configuration

                // Current TEMPSENSE Implementation uses the Non Return to Zero Clock Format
                //Actual Vector Rate is 1/2 Clock Toggle Rate.
                period_dbl = 1.0 / EEPROMClockRate;
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.0);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.9 * period_dbl);

                // Create TEMPSENSE NRZ Timeset
                DigitalTimeSet tsTEMPSENSENRZ = DIGI.Timing.CreateTimeSet(Timeset.TEMPSENSE.ToString("g"));
                tsTEMPSENSENRZ.ConfigurePeriod(period);

                tsTEMPSENSENRZ.ConfigureDriveEdges(allTEMPSENSEPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsTEMPSENSENRZ.ConfigureCompareEdgesStrobe(allTEMPSENSEPins, compareStrobe);

                // Shift all TEMPSENSE SCK edges by 1/4 Period so SDA is stable before clock rising edge //MM added 0.1 to everything
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.15 * period_dbl);
                // Set TEMPSENSE SCK timing
                tsTEMPSENSENRZ.ConfigureDriveEdges(TEMPSENSEsckPin, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);

                #endregion TEMPSENSE Timing Configuration

                #region Configure 6570 for Digital Mode with HighZ Termination

                if (!isVioTxPpmu)
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }
                else
                {
                    allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                    allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }

                allEEPROMPins.SelectedFunction = SelectedFunction.Digital;
                allEEPROMPins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                #endregion Configure 6570 for Digital Mode with HighZ Termination

                usingMIPI = true;

                //this.LoadVector_RFOnOffTest();
                //LoadVector_RFOnOffTestRx(bool isNRZ = false); //Rx Trigger

                //this.LoadVector_RFOnOffSwitchTest(bool isNRZ = false);
                //LoadVector_RFOnOffSwitchTest_WithPreMipi(); //Tx Trigger: RFOnOff + SwitchingTime
                //LoadVector_RFOnOffSwitchTest_With3TxPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime, 3TXPreMipi, For TX Band-To-Band

                //LoadVector_RFOnOffSwitchTestRx();   //Rx Trigger: RFOnOff + SwitchingTime
                //LoadVector_RFOnOffSwitchTestRx_WithPreMipi();   //Rx Trigger: RFOnOff + SwitchingTime, 1RXPreMipi, for LNA Output Switching
                //LoadVector_RFOnOffSwitchTestRx_With1Tx2RxPreMipi();    //Rx Trigger: RFOnOff + SwitchingTime, 1TXPreMipi, 2RXPreMipi, For LNA switching time (same output) G0 only

                //LoadVector_RFOnOffSwitchTest2();     //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2
                //LoadVector_RFOnOffSwitchTest2_WithPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, For CPL
                //LoadVector_RFOnOffSwitchTest2_With1Tx2RxPreMipi();    //Tx Trigger: RFOnOff + SwitchingTime + SwitchingTime2, 1TXPreMipi, 2RXPreMipi, For TERM:TX:RX:TX

                //LoadVector_RFOnOffSwitchTest2Rx();  //Rx Trigger: RFOnOff + SwitchingTime + SwitchingTime2

                // Configure History RAM streaming Parameters
                DIGI.HistoryRam.CyclesToAcquire = HistoryRamCycle.Failed;
                DIGI.Trigger.HistoryRamTrigger.TriggerType = HistoryRamTriggerType.CycleNumber;
                DIGI.Trigger.HistoryRamTrigger.CycleNumber.Number = 0;
                DIGI.Trigger.HistoryRamTrigger.PretriggerSamples = 0;
                DIGI.HistoryRam.MaximumSamplesToAcquirePerSite = -1;
                DIGI.HistoryRam.NumberOfSamplesIsFinite = !true;
                DIGI.HistoryRam.BufferSizePerSite = 100000;

                try
                {
                    PrecisionTimeSpan[] offsets;
                    string TDRInformation = Eq.SharedConfiguration.TDRRootDir + "\\TDRInformation.tdr";
                    List<PrecisionTimeSpan> precisionTimeSpans = new List<PrecisionTimeSpan>();
                    DIGI.Timing.TdrEndpointTermination = TdrEndpointTermination.TdrToOpenCircuit;

                    if (File.Exists(TDRInformation))
                    {
                        string line;

                        using (StreamReader stream = new StreamReader(TDRInformation))
                        {
                            while ((line = stream.ReadLine()) != null)
                            {
                                var _precison = line.Split(',');
                                double SecondsTotal = Convert.ToDouble(_precison.First());
                                double SecondsFractional = Convert.ToDouble(_precison.Last());
                                precisionTimeSpans.Add(new PrecisionTimeSpan(SecondsTotal, SecondsFractional));
                            }
                        }

                        offsets = precisionTimeSpans.ToArray();
                    }
                    else
                    {
                        offsets = allRffePinswoTrig.Tdr(false);

                        offsets[0] = PrecisionTimeSpan.FromMilliseconds(0);
                        offsets[1] = PrecisionTimeSpan.FromMilliseconds(0);
                        offsets[2] = PrecisionTimeSpan.FromMilliseconds(0);

                        using (StreamWriter stream = new StreamWriter(TDRInformation, false))
                        {
                            foreach (var t in offsets)
                            {
                                stream.WriteLine(string.Format("{0},{1}", t.SecondsTotal, t.SecondsFractional));
                            }
                        }
                    }

                    allRffePinswoTrig.ApplyTdrOffsets(offsets);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Please remove sample and reload program to adjust TDR.\n\n{0}\n{1}", ex.ToString(), ex.InnerException), "TDR Exception");
                }

                return usingMIPI;
            }

            /// <summary>
            /// Close the NI 6570 session when shutting down the application
            /// and ensure all patterns are unloaded and all channels are disconnected.
            /// </summary>
            public override void Close()
            {
                allRffePins.SelectedFunction = SelectedFunction.Disconnect;
                allEEPROMPins.SelectedFunction = SelectedFunction.Disconnect;
                allTEMPSENSEPins.SelectedFunction = SelectedFunction.Disconnect;
                allEEPROM_UNIOPins.SelectedFunction = SelectedFunction.Disconnect;
                DIGI.Dispose();
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="timeset"></param>
            /// <param name="period_dbl"></param>
            /// <param name="driveOnAmp">Drive On — Specifies the delay from the beginning of the vector period for turning on the pin driver. This option applies only when the previous vector left the pin in a non-drive pin state (L, H, X, V, M, E). </param>
            /// <param name="driveDataAmp">Drive Data — Specifies the delay from the beginning of the vector period until the pattern data is driven to the pattern value.</param>
            /// <param name="driveReturnAmp">Drive Return — Specifies the delay from the beginning of the vector period until the pin changes from the pattern data to the return value, as specified by the format. Because not all formats use this column, the document disables editing this field when the drive format does not support it.</param>
            /// <param name="driveOffAmp">Drive Off — Specifies the delay from the beginning of the vector period to turn off the pin driver when the next vector period uses a non-drive symbol (L, H, X, V, M, E). </param>
            /// <param name="compareStrobeAmp">Compare Strobe — Specifies the time when the comparison happens within a vector period. </param>
            /// <param name="clockRisingEdgeDelayRaw"></param>
            /// <param name="driveallpin"></param>
            /// <param name="driveclkpin"></param>
            private void CreateTimeSet(Timeset timeset, double period_dbl, double driveOnAmp, double driveDataAmp, double driveReturnAmp, double driveOffAmp, double compareStrobeAmp, double clockRisingEdgeDelayRaw, DriveFormat driveallpin = DriveFormat.NonReturn, DriveFormat driveclkpin = DriveFormat.ReturnToLow)
            {
                // All RegRead / RegWrite functions use the RZ format for SCLK
                // Vector Rate is Clock Toggle Rate.
                // Compute timing values, shift all clocks out by 2 x periods so we can adjust the strobe "backwards" if needed.
                var period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                var driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(driveOnAmp * period_dbl); //0.5
                var driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(driveDataAmp * period_dbl); //0.5
                var driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(driveReturnAmp * period_dbl); // NR — Non-return. Setting Drive Format to NR disables the Drive Return cell in that row and clears that cell if a value was present.
                var driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(driveOffAmp * period_dbl);
                var compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(compareStrobeAmp * StrobePoint);

                var clockRisingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(clockRisingEdgeDelayRaw);  // This is the amount of time after SDATA is set to high or low before SCLK is set high.
                                                                                                               // By setting this > 0, this will slightly delay the SCLK rising edge which can help ensure
                                                                                                               // SDATA is settled before clocking in the value at the DUT.
                                                                                                               // Note: This does not shift the Falling Edge of SCLK.  This means that adjusting this value will
                                                                                                               //  reduce the overall duty cycle of SCLK.  You must adjuct clockFallingEdgeDelay by the same amount
                                                                                                               //  if you would like to maintain a 50% duty cycle.
                var clockFallingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);
                var _ZeroTimeSpan = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);

                // Create Timeset
                DigitalTimeSet ts = DIGI.Timing.CreateTimeSet(timeset.ToString("g"));
                ts.ConfigurePeriod(period);

                // Vio, Sdata, Trig
                //var edgeMultiplier = ts.GetEdgeMultiplier(allRffePins);
                ts.ConfigureEdgeMultiplier(allRffePins, 1);
                ts.ConfigureDriveEdges(pinSet: allRffePins, format: driveallpin, driveOnEdge: driveOn, driveDataEdge: driveData, driveReturnEdge: driveallpin == DriveFormat.NonReturn ? _ZeroTimeSpan : driveReturn, driveOffEdge: driveOff, driveData2Edge: _ZeroTimeSpan, driveReturn2Edge: _ZeroTimeSpan);
                ts.ConfigureCompareEdgesStrobe(pinSet: allRffePins, compareEdge: compareStrobe, compare2Edge: _ZeroTimeSpan);

                // Sclk
                ts.ConfigureDriveEdges(pinSet: sclk1Pin, format: driveclkpin, driveOnEdge: driveOn + clockRisingEdgeDelay, driveDataEdge: driveData + clockRisingEdgeDelay, driveReturnEdge: driveclkpin == DriveFormat.NonReturn ? _ZeroTimeSpan : driveReturn + clockFallingEdgeDelay, driveOffEdge: driveOff + clockFallingEdgeDelay, driveData2Edge: _ZeroTimeSpan, driveReturn2Edge: _ZeroTimeSpan);
                ts.ConfigureCompareEdgesStrobe(pinSet: sclk1Pin, compareEdge: compareStrobe, compare2Edge: _ZeroTimeSpan);

                if (Num_Mipi_Bus == 2)
                {
                    ts.ConfigureDriveEdges(pinSet: sclk2Pin, format: driveclkpin, driveOnEdge: driveOn + clockRisingEdgeDelay, driveDataEdge: driveData + clockRisingEdgeDelay, driveReturnEdge: driveclkpin == DriveFormat.NonReturn ? _ZeroTimeSpan : driveReturn + clockFallingEdgeDelay, driveOffEdge: driveOff + clockFallingEdgeDelay, driveData2Edge: _ZeroTimeSpan, driveReturn2Edge: _ZeroTimeSpan);
                    ts.ConfigureCompareEdgesStrobe(pinSet: sclk2Pin, compareEdge: compareStrobe, compare2Edge: _ZeroTimeSpan);
                }
            }

            public override bool ReInitializeVIO(double violevel)
            {
                var _DeviceMipiLevel = new CustomMipiLevel(violevel, m_DeviceMipiLevel.vil, m_DeviceMipiLevel.voh, m_DeviceMipiLevel.vol, m_DeviceMipiLevel.vtt);
                m_DeviceMipiLevel = _DeviceMipiLevel;

                allRffePins.DigitalLevels.ConfigureVoltageLevels(m_DeviceMipiLevel.vil, m_DeviceMipiLevel.vih, m_DeviceMipiLevel.vol, m_DeviceMipiLevel.voh, m_DeviceMipiLevel.vtt);
                trigPin.DigitalLevels.ConfigureVoltageLevels(0.0, 5.0, 0.5, 2.5, 5.0); // Set VST Trigger Channel to 5V logic.  VST's PFI0 VIH is 2.0V, absolute max is 5.5V

                return true;
            }

            public override string GetInstrumentInfo()
            {
                return "HSDIO=" + DIGI.Identity.InstrumentModel + " r" + DIGI.Identity.InstrumentFirmwareRevision + "*" + SerialNumber + ";";
            }

            public override void shmoo(string REG_address)
            {
                double originalStrobePoint = this.StrobePoint;
                //double originalUserDelay = this.UserDelay;

                //double maxdelay = 25e-9;
                double maxstrobe = 175e-9; // (1.0 / ClockRate) * 8.0;
                                           //double delaystep = 1e-9;
                double strobestep = 1e-9;
                //Console.WriteLine("X-Axis: UserDelay 0nS to " + (Math.Round(maxdelay / 1e-9)).ToString() + "nS");
                Console.WriteLine("Y-Axis: StrobePoint 0nS to " + (Math.Round(maxstrobe / 1e-9)).ToString() + "nS");

                //Console.WindowHeight = Math.Min((int)(maxstrobe / strobestep) + 10, Console.LargestWindowHeight);
                //Console.WindowWidth = Math.Min((int)(maxdelay / delaystep + 2) * 5 + 5, Console.LargestWindowWidth);
                DigitalTimeSet tsNRZ = DIGI.Timing.GetTimeSet(Timeset.MIPI.ToString("g"));
                for (double compareStrobe = 0; compareStrobe < maxstrobe; compareStrobe += strobestep)
                {
                    tsNRZ.ConfigureCompareEdgesStrobe(sdata1Pin, Ivi.Driver.PrecisionTimeSpan.FromSeconds(compareStrobe));
                    tsNRZ.ConfigureCompareEdgesStrobe(sdata2Pin, Ivi.Driver.PrecisionTimeSpan.FromSeconds(compareStrobe));
                    DIGI.PatternControl.Commit();
                    Console.Write(Math.Round(compareStrobe / 1e-9).ToString().PadLeft(2, ' '));
                    //for (double delay = 0; delay < maxdelay; delay += delaystep)
                    string regValue = "";
                    //double delay = 0;
                    {
                        //DIGI.ConfigureUserDelay(HSDIO.SdataChanName.ToUpper(), delay);
                        regValue = this.RegRead(REG_address);
                        this.SendVector("FUNCTIONAL_RX");
                        // int errors = this.GetNumExecErrors("FUNCTIONAL_RX");

                        long[] Fails = sdata2Pin.GetFailCount();
                        long errors = Fails[0];

                        //Console.WriteLine((errors > 0 ? "FAIL: " : "PASS: ") + nameInMemory + " CableDelay: " + delay.ToString() + " -- Bit Errors: " + errors.ToString());
                        Console.BackgroundColor = (errors > 0 ? ConsoleColor.Red : ConsoleColor.Green);
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        string errstr = "";
                        if (errors >= 1000000)
                        {
                            errstr = (errors / 1000000).ToString("D");
                            errstr = errstr.PadLeft(3, ' ') + "M ";
                        }
                        else if (errors >= 1000)
                        {
                            errstr = (errors / 1000).ToString("D");
                            errstr = errstr.PadLeft(3, ' ') + "K ";
                        }
                        else
                        {
                            errstr = errors.ToString("D");
                            errstr = errstr.PadLeft(4, ' ') + " ";
                        }
                        Console.Write((errors > 0 ? errstr : "     "));
                        Console.ResetColor();
                    }
                    Console.Write(regValue + "\n");
                }
                /*Console.Write("  ");
                for (double delay = 0; delay < maxdelay; delay += 1e-9)
                {
                    string str = (delay / 1e-9).ToString();
                    Console.Write(str.PadLeft(4,' ') + " ");
                }
                Console.Write("\n");*/

                //this.UserDelay = originalUserDelay;
                this.StrobePoint = originalStrobePoint;

                //DIGI.ConfigureUserDelay(HSDIO.SdataChanName.ToUpper(), originalUserDelay);
                tsNRZ.ConfigureCompareEdgesStrobe(sdata1Pin, Ivi.Driver.PrecisionTimeSpan.FromSeconds(originalStrobePoint));
            }

            /// <summary>
            /// Send the pattern requested by nameInMemory.
            /// If requesting the PID pattern, generate the signal and store the result for later processing by InterpretPID
            /// If requesting the TempSense pattern, generate the signal and store the result for later processing by InterpretTempSense
            /// </summary>
            /// <param name="nameInMemory">The requested pattern to generate</param>
            /// <returns>True if the pattern generated without bit errors</returns>
            public override bool SendVector(string nameInMemory)
            {
                if (!usingMIPI || nameInMemory == null || nameInMemory == "") return true;
                bool vioSpecific = false;

                if (Enum.TryParse(nameInMemory.ToUpper(), out PPMUVioOverrideString vioEnum))
                {
                    vioSpecific = true;
                }
                else
                {
                    nameInMemory = nameInMemory.Replace("_", "");

                    if (!loadedPatternNames.Contains(nameInMemory.ToLower())) // kh for Merlin debug
                    {
                        MessageBox.Show("vector " + nameInMemory + "Not loaded. Check TCF Conditions or HSDIO init", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return true;
                    }
                }

                try
                {
                    if (!isVioTxPpmu) //Pinot added (pcon)
                    {
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        if (vioSpecific)
                        {
                            string pin = "";

                            if (nameInMemory.Contains("TX")) pin = "Vio1";
                            else if (nameInMemory.Contains("RX")) pin = "Vio2";

                            bool isDCUnit;
                            double SettingCompliance;

                            if (string.IsNullOrWhiteSpace(pin))
                            {
                                foreach (var s in new string[] { "Vio1", "Vio2" })
                                {
                                    pin = s;
                                    isDCUnit = Eq.Site[Site].DC.ContainsKey(pin) && Eq.Site[Site].DC[pin] is EqDC.nidcbase;
                                    SettingCompliance = isDCUnit ? 0.15 : 0.032;

                                    switch (vioEnum)
                                    {
                                        case PPMUVioOverrideString.RESET:

                                            #region W/A reset

#if false
                                            if (isDCUnit)
                                            {
                                            int WAresetMethod = 1;

                                            switch (WAresetMethod)
                                            {
                                                case 1:
                                                    /// Check real voltage
                                                    double volts;
                                                    myDC.ForceVoltage(m_DeviceMipiLevel.vil, SettingCompliance);
                                                    myDC.SetupVoltageMeasure(1);
                                                    while (true)
                                                    {
                                                        volts = myDC.MeasureVoltage(1);
                                                        if (volts < 0.2) break;
                                                    }

                                                    Thread.Sleep(1);
                                                    myDC.ForceVoltage(m_DeviceMipiLevel.vih, SettingCompliance);

                                                    while (true)
                                                    {
                                                        volts = myDC.MeasureVoltage(1);
                                                        if (volts > m_DeviceMipiLevel.vih * 0.9) break;
                                                    }
                                                    Thread.Sleep(1);
                                                    break;

                                                case 2:
                                                    /// Set current soruce mode
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Control.Abort();
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Measurement.MeasureWhen = NationalInstruments.ModularInstruments.NIDCPower.DCPowerMeasurementWhen.AutomaticallyAfterSourceComplete;
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Source.Output.Function = NationalInstruments.ModularInstruments.NIDCPower.DCPowerSourceOutputFunction.DCCurrent;
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Source.Current.CurrentLevel = -1;
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Source.Current.VoltageLimit = 0.1;
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Control.Initiate();
                                                    Thread.Sleep(1);

                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Control.Abort();
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Source.Output.Function = NationalInstruments.ModularInstruments.NIDCPower.DCPowerSourceOutputFunction.DCVoltage;
                                                    myDC.SMUsession._nidcSession.Outputs[myDC.SMUsession.ChannelName].Control.Initiate();
                                                    break;
                                            }
                                    }
#endif

                                            #endregion W/A reset

                                            Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vil, SettingCompliance);
                                            if (isDCUnit) Thread.Sleep(1);
                                            Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vih, SettingCompliance);

                                            break;

                                        case PPMUVioOverrideString.VIOOFF:
                                            Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vil, SettingCompliance);
                                            if (isDCUnit) Thread.Sleep(1);
                                            break;

                                        case PPMUVioOverrideString.VIOON:
                                            Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vih, SettingCompliance);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                isDCUnit = Eq.Site[Site].DC.ContainsKey(pin) && Eq.Site[Site].DC[pin] is EqDC.nidcbase;
                                SettingCompliance = isDCUnit ? 0.15 : 0.032;

                                switch (vioEnum)
                                {
                                    case PPMUVioOverrideString.RESET_TX:
                                    case PPMUVioOverrideString.RESET_RX:
                                        Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vil, SettingCompliance);
                                        if (isDCUnit) Thread.Sleep(1);
                                        Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vih, SettingCompliance);
                                        break;

                                    case PPMUVioOverrideString.VIOOFF_TX:
                                    case PPMUVioOverrideString.VIOOFF_RX:
                                        Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vil, SettingCompliance);
                                        if (isDCUnit) Thread.Sleep(1);
                                        break;

                                    case PPMUVioOverrideString.VIOON_TX:
                                    case PPMUVioOverrideString.VIOON_RX:
                                        Eq.Site[Site].DC[pin]?.ForceVoltage(m_DeviceMipiLevel.vih, SettingCompliance);
                                        break;
                                }
                            }

                            return true;
                        }
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    TEMPSENSEVccPin.DigitalLevels.TerminationMode = TerminationMode.ActiveLoad;

                    // Select pattern to burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory.ToLower();

                    // Send the normal pattern file and store the number of bit errors from the SDATA pin
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));
                    //  Thread.Sleep(500);

                    // Get PassFail Results
                    bool[] passFail = DIGI.PatternControl.GetSitePassFail("");

                    if (((Eq.SharedConfiguration.RunOptions & RunOption.QCFAIL_LOG) == RunOption.QCFAIL_LOG) && passFail.Any(t => t == false))
                    {
                        Eq.Site[Site].HSDIO.QCFail_Log(nameInMemory.ToLower());
                    }

                    Int64[] failureCount;
                    string CurrentSlaveAddress;
                    if (dutSlaveAddress.Length % 2 == 1)
                        CurrentSlaveAddress = dutSlaveAddress.PadLeft(dutSlaveAddress.Length + 1, '0');
                    else
                        CurrentSlaveAddress = dutSlaveAddress;

                    int Current_MIPI_Bus = dutSlavePairIndex;// Convert.ToUInt16(Get_Digital_Definition("SLAVE_ADDR_" + CurrentSlaveAddress));

                    if (TRXQC)
                    {
                        failureCount = sdata1Pin.GetFailCount();
                        AddOrReplaceTRxQCDic(nameInMemory + "_TX", (int)failureCount[0]);
                        failureCount = sdata2Pin.GetFailCount();
                        AddOrReplaceTRxQCDic(nameInMemory + "_RX", (int)failureCount[0]);
                    }
                    else
                    {
                        if (EqHSDIO.dutSlavePairIndex == 2)
                            failureCount = sdata2Pin.GetFailCount();
                        else
                            failureCount = sdata1Pin.GetFailCount();

                        NumBitErrors = (int)failureCount[0];
                    }

                    if (debug) Console.WriteLine("SendVector " + nameInMemory + " Bit Errors: " + NumBitErrors.ToString());

                    return passFail[0];
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to generate vector for " + nameInMemory + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            public override void QCFail_Log(string label)
            {
                // Create data structures for streaming. We will maintain a list of each chunk of samples and merge them at the end, as this is faster.
                List<DigitalHistoryRamCycleInformation[]> historyRamCycleInformation = new List<DigitalHistoryRamCycleInformation[]>();
                List<long[]> historyRamScanCycleResults = new List<long[]>();
                long fetchedSamplesCount = 0;

                bool isDone = false;
                long newSamples = 0;
                while (!isDone || newSamples > 0)
                {
                    isDone = DIGI.PatternControl.IsDone;
                    long totalSamplesCount = DIGI.HistoryRam.GetSampleCount("site0");
                    newSamples = totalSamplesCount - fetchedSamplesCount;

                    // Fetch chunk of new samples and add it to the list
                    historyRamCycleInformation.Add(DIGI.HistoryRam.FetchCycleInformation("site0", "", fetchedSamplesCount, newSamples));
                    //historyRamScanCycleResults.Add(session.HistoryRam.FetchScanCycleNumbers(SitesToFetch, fetchedSamplesCount, newSamples));

                    fetchedSamplesCount += newSamples;
                }

                // Collapse the blocks of History RAM samples into a single array
                DigitalHistoryRamCycleInformation[] historyRamResults = new DigitalHistoryRamCycleInformation[fetchedSamplesCount];
                // Write the History RAM header to the HistoryRAMResults.csv file.

                string[] patternPins = DIGI.PatternControl.GetPatternPinSetString(label).Split(',');
                IEnumerable<string> actualHeaders = patternPins.Select(s => s.Trim() + " actual");
                string actualHeadersString = string.Join(",", actualHeaders);
                IEnumerable<string> expectedHeaders = patternPins.Select(s => s.Trim() + " expected");
                string expectedHeadersString = string.Join(",", expectedHeaders);

                string header = "TimeSet,Pattern,Vector,Cycle," + actualHeadersString + "," + expectedHeadersString;

                QC_Fail_list.Add(header);

                long[] historyRamScanResults = new long[fetchedSamplesCount];
                int currentPosition = 0;
                for (int blockIndex = 0; blockIndex < historyRamCycleInformation.Count; blockIndex++)
                {
                    historyRamCycleInformation[blockIndex].CopyTo(historyRamResults, currentPosition);
                    //historyRamScanCycleResults[blockIndex].CopyTo(historyRamScanResults, currentPosition);
                    currentPosition += historyRamCycleInformation[blockIndex].Length;
                }

                foreach (DigitalHistoryRamCycleInformation cycleInformation in historyRamResults)
                {
                    string cycleInfoString = string.Format("{0},{1},{2},{3}", cycleInformation.TimeSetName, cycleInformation.PatternName,
                        cycleInformation.VectorNumber.ToString(), cycleInformation.CycleNumber.ToString());

                    foreach (PinState[] actualPinStates in cycleInformation.ActualPinStates)
                    {
                        foreach (PinState actualPinState in actualPinStates)
                        {
                            cycleInfoString = string.Format("{0},{1}", cycleInfoString, actualPinState);
                        }
                    }

                    foreach (PinState[] expectedPinStates in cycleInformation.ExpectedPinStates)
                    {
                        foreach (PinState expectedPinState in expectedPinStates)
                        {
                            switch (expectedPinState)
                            {
                                case PinState._0:
                                    cycleInfoString = string.Format("{0},{1}", cycleInfoString, 0);
                                    break;

                                case PinState._1:
                                    cycleInfoString = string.Format("{0},{1}", cycleInfoString, 1);
                                    break;

                                case PinState.L:
                                case PinState.H:
                                case PinState.X:
                                case PinState.M:
                                case PinState.V:
                                case PinState.D:
                                    cycleInfoString = string.Format("{0},{1}", cycleInfoString, expectedPinState);
                                    break;

                                case PinState.NotAPinState:
                                default:
                                    break;
                            }
                        }
                    }
                    QC_Fail_list.Add(cycleInfoString);
                }
            }

            /// <summary>
            /// Loop through each pattern name in the MipiWaveformNames list and execute them
            /// </summary>
            /// <param name="firstTest">Unused</param>
            /// <param name="MipiWaveformNames">The list of pattern names to execute</param>
            public override void SendNextVectors(bool firstTest, List<string> MipiWaveformNames)
            {
                try
                {
                    if (MipiWaveformNames == null) return;

                    foreach (string nameInMemory in MipiWaveformNames)
                    {
                        SendVector(nameInMemory);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            public override void SendMipiCommands(object Infor)
            {
                try
                {
                    Config Cf = Infor as Config;

                    if (Cf.MipiCommands == null) return;

                    SW_MIPI = new Stopwatch();
                    SW_MIPI.Start();

                    SendMipiCommands(Cf.MipiCommands, Cf._eMipiTestType);

                    ThreadMipi.Set();
                    SW_MIPI.Stop();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "HSDIO MIPI Write Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            public override void SendMipiCommands(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands, eMipiTestType _eMipiTestType = eMipiTestType.Write, int TryCount = 1)
            {
                try
                {
                    if (MipiCommands == null) return;

                    switch (_eMipiTestType)
                    {
                        case eMipiTestType.Read:
                        case eMipiTestType.WriteRead:
                            break;

                        case eMipiTestType.Write:
                            if (isShareBus) RegWriteMultiplePair(MipiCommands);
                            else RegWriteMultiple(MipiCommands);
                            break;

                        case eMipiTestType.Timing:
                            if (isShareBus) TimingRegWriteMultiplePair(MipiCommands);
                            else TimingRegWriteMultiple(MipiCommands);//, BeforeDelay, AfterDelay  );
                            break;

                        case eMipiTestType.OTPburn:
                            OTPburn(MipiCommands, TryCount);
                            break;
                    }
                    //PAtesting.Stop();
                    //long newtime = PAtesting.ElapsedMilliseconds;
                    //PAtesting.Reset();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "HSDIO MIPI Write Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            //keng shan ADDED
            public override bool SendRFOnOffTestVector(bool RxMode, string[] SwTimeCustomArry)
            {
                if (!usingMIPI) return true;

                try
                {
                    DIGI.PatternControl.WriteSequencerFlag("seqflag3", false);
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent3", "PXI_Trig3");
                    DIGI.PatternControl.Commit();

                    //Lib_Var.TestCondition.preOnOffOperation = false;
                    RxMode = false;
                    bool isRFONOFFSWITCHTest = false;
                    uint[] SWregON = new uint[] { };
                    uint[] SWregOFF = new uint[] { };
                    string PinName = "";
                    //string SWonString = "";
                    //string SWoffString = "";

                    string[] temp = null;
                    string[] tempSwTimeCustomArry = new string[4];
                    int k = 0;

                    string SrcRFONOFFSwitch = "SrcRFONOFFSwitch";
                    string RFOnOffSwitchTest = "RFOnOffSwitchTest";
                    string RFOnOffTest = "RFOnOffTest";

                    PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                    #region Sorting RF onoff vectors for SWregON and SWregOFF

                    if (SwTimeCustomArry[0] != "")
                    {
                        switch (SwTimeCustomArry.Length)
                        {
                            case 2:
                                isRFONOFFSWITCHTest = true;
                                break;

                            case 4:
                                isRFONOFFSWITCHTest = true;
                                SrcRFONOFFSwitch = "SrcRFONOFFSwitch2";
                                RFOnOffSwitchTest = "RFOnOffSwitchTest2";
                                break;

                            default:
                                throw new Exception("The SWCustomArray definition is not correct. Total of ':' is " + tempSwTimeCustomArry.Length);
                        }
                    }

                    #endregion Sorting RF onoff vectors for SWregON and SWregOFF

                    #region Check SWTIMECUSTOM for multiple MIPI configuration

                    int iCnt = 0;
                    int loop = 0;
                    foreach (char a in SwTimeCustomArry[0])
                    {
                        //Has to Detect either TX or RX Mipi base on Slave address
                        //"E" for TX,  "C" for RX. Cannot determine from "TXRX" column in tcf because
                        //certain switching test has switch matrix switching base on RX but testing
                        //TX mipi switches
                        if (loop == 0) { RxMode = (a == 'E') ? false : true; }

                        if (a == ',')
                        {
                            iCnt++;
                        }

                        loop++;
                    }

                    #endregion Check SWTIMECUSTOM for multiple MIPI configuration

                    if (iCnt > 0)
                    {
                        #region Pre-MIPI Configuration

                        switch (iCnt)
                        {
                            case 1:

                                #region ONE PreMIPI

                                //    // Reassigned "swTimeCustomArry" for testcases that requires additional mipi command
                                //    // prior the MIPI to switch RF switches ON and OFF. (Currently used in PC3 MBOut only)
                                if (SwTimeCustomArry.Length == 2)
                                {
                                    temp = null;
                                    tempSwTimeCustomArry = new string[4];
                                    k = 0;

                                    //preOnOffOperation = true;
                                    RFOnOffSwitchTest = "RFOnOffSwitchTestPreMipi";
                                    SrcRFONOFFSwitch = "SrcRFONOFFSwitchPreMipi";

                                    for (int i = 0; i < SwTimeCustomArry.Length; i++)
                                    {
                                        temp = SwTimeCustomArry[i].Split(',');

                                        for (int j = 0; j < temp.Length; j++)
                                        {
                                            tempSwTimeCustomArry[k] = temp[j];
                                            k++;
                                        }
                                    }
                                    Array.Resize(ref SwTimeCustomArry, tempSwTimeCustomArry.Length);
                                    SwTimeCustomArry = tempSwTimeCustomArry;
                                }
                                else if (SwTimeCustomArry.Length == 4)
                                {
                                    temp = null;
                                    tempSwTimeCustomArry = new string[8];
                                    k = 0;

                                    //preOnOffOperation = true;
                                    RFOnOffSwitchTest = "RFOnOffSwitchTest2PreMipi";
                                    SrcRFONOFFSwitch = "SrcRFONOFFSwitch2PreMipi";

                                    for (int i = 0; i < SwTimeCustomArry.Length; i++)
                                    {
                                        temp = SwTimeCustomArry[i].Split(',');

                                        for (int j = 0; j < temp.Length; j++)
                                        {
                                            tempSwTimeCustomArry[k] = temp[j];
                                            k++;
                                        }
                                    }
                                    Array.Resize(ref SwTimeCustomArry, tempSwTimeCustomArry.Length);
                                    SwTimeCustomArry = tempSwTimeCustomArry;
                                }

                                #endregion ONE PreMIPI

                                break;

                            case 3:

                                #region 3Tx PreMIPI conditions

                                temp = null;
                                tempSwTimeCustomArry = new string[8];
                                k = 0;

                                //preOnOffOperation = true;
                                RFOnOffSwitchTest = "RFOnOffSwitchTest3TxPreMipi";
                                SrcRFONOFFSwitch = "SrcRFONOFFSwitch3TxPreMipi";

                                for (int i = 0; i < SwTimeCustomArry.Length; i++)
                                {
                                    temp = SwTimeCustomArry[i].Split(',');

                                    for (int j = 0; j < temp.Length; j++)
                                    {
                                        tempSwTimeCustomArry[k] = temp[j];
                                        k++;
                                    }
                                }
                                Array.Resize(ref SwTimeCustomArry, tempSwTimeCustomArry.Length);
                                SwTimeCustomArry = tempSwTimeCustomArry;

                                #endregion 3Tx PreMIPI conditions

                                break;

                            case 4:
                                if (SwTimeCustomArry.Length == 2)   //Not loading on the Vector yet!!
                                {
                                    #region 1Tx & 2Rx PreMIPI condtions (2 Steps)

                                    temp = null;
                                    tempSwTimeCustomArry = new string[10];
                                    k = 0;

                                    //preOnOffOperation = true;
                                    RFOnOffSwitchTest = "RFOnOffSwitchTest1Tx2RxPreMipi";
                                    SrcRFONOFFSwitch = "SrcRFONOFFSwitch1Tx2RxPreMipi";

                                    for (int i = 0; i < SwTimeCustomArry.Length; i++)
                                    {
                                        temp = SwTimeCustomArry[i].Split(',');

                                        for (int j = 0; j < temp.Length; j++)
                                        {
                                            tempSwTimeCustomArry[k] = temp[j];
                                            k++;
                                        }
                                    }
                                    Array.Resize(ref SwTimeCustomArry, tempSwTimeCustomArry.Length);
                                    SwTimeCustomArry = tempSwTimeCustomArry;

                                    #endregion 1Tx & 2Rx PreMIPI condtions (2 Steps)
                                }
                                else if (SwTimeCustomArry.Length == 4)
                                {
                                    #region 1Tx & 2Rx PreMIPI condtions (4 Steps)

                                    temp = null;
                                    tempSwTimeCustomArry = new string[20];
                                    k = 0;

                                    //preOnOffOperation = true;
                                    RFOnOffSwitchTest = "RFOnOffSwitchTest21Tx2RxPreMipi";
                                    SrcRFONOFFSwitch = "SrcRFONOFFSwitch21Tx2RxPreMipi";

                                    for (int i = 0; i < SwTimeCustomArry.Length; i++)
                                    {
                                        temp = SwTimeCustomArry[i].Split(',');

                                        for (int j = 0; j < temp.Length; j++)
                                        {
                                            tempSwTimeCustomArry[k] = temp[j];
                                            k++;
                                        }
                                    }
                                    Array.Resize(ref SwTimeCustomArry, tempSwTimeCustomArry.Length);
                                    SwTimeCustomArry = tempSwTimeCustomArry;

                                    #endregion 1Tx & 2Rx PreMIPI condtions (4 Steps)
                                }
                                else
                                {
                                    throw new Exception("The SWCustomArray definition is not correct. Total of ':' is " + tempSwTimeCustomArry.Length);
                                }

                                break;

                            default:
                                throw new Exception("The PreMipi Configuration is undefined. Total of ',' is " + iCnt);
                        }

                        #endregion Pre-MIPI Configuration

                        if (RxMode)
                        {
                            PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                            SrcRFONOFFSwitch = SrcRFONOFFSwitch + "Rx";
                            RFOnOffSwitchTest = RFOnOffSwitchTest + "Rx";
                            RFOnOffTest = RFOnOffTest + "Rx";
                        }
                    }
                    else
                    {
                        if (RxMode)
                        {
                            PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                            SrcRFONOFFSwitch = SrcRFONOFFSwitch + "Rx";
                            RFOnOffSwitchTest = RFOnOffSwitchTest + "Rx";
                            RFOnOffTest = RFOnOffTest + "Rx";
                        }
                    }

                    #region Burst pattern

                    try
                    {
                        if (!isVioTxPpmu)
                        {
                            // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                            allRffePins.SelectedFunction = SelectedFunction.Digital;
                            allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }
                        else
                        {
                            allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                            allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }

                        //  & check for Pass/Fail
                        bool[] passFail = new bool[] { };

                        if (isRFONOFFSWITCHTest)
                        {
                            // Set vectors array to source waveform data
                            uint[] dataArray = new uint[512];
                            int totalLenth = 0;

                            for (int i = 0; i < SwTimeCustomArry.Length; i++)
                            {
                                Array.Copy(SourceWaveform[SwTimeCustomArry[i]], 0, dataArray, totalLenth, SourceWaveform[SwTimeCustomArry[i]].Length);
                                totalLenth += SourceWaveform[SwTimeCustomArry[i]].Length;
                            }

                            // Configure to source data, register address is up to 8 bits
                            DIGI.SourceWaveforms.CreateSerial(PinName, SrcRFONOFFSwitch, SourceDataMapping.Broadcast, 1, BitOrder.MostSignificantBitFirst);
                            DIGI.SourceWaveforms.WriteBroadcast(SrcRFONOFFSwitch, dataArray);

                            // Burst Pattern RFONOFFSwitchTest
                            DIGI.PatternControl.ConfigurePatternBurstSites("site0");

                            DIGI.PatternControl.StartLabel = RFOnOffSwitchTest;
                        }
                        else
                        {
                            // Burst Pattern RFONOFFTest
                            DIGI.PatternControl.ConfigurePatternBurstSites("site0");

                            DIGI.PatternControl.StartLabel = RFOnOffTest;
                        }

                        // Do not need to call "WaitUntilDone()" as it will block the software triggerring(SeqFlag3)
                        DIGI.PatternControl.Initiate();

                        return true;
                    }
                    catch (Exception e)
                    {
                        DIGI.PatternControl.Abort();
                        MessageBox.Show("Failed to send RFONOFFTest Vector.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    #endregion Burst pattern
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to generate vector for " + SwTimeCustomArry[0] + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DIGI.PatternControl.Abort();
                    //cm wong: moved up: MessageBox.Show("Failed to generate vector for " + nameInMemory + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            //keng shan ADDED
            private bool AddtoDic_SourceWaveformArry(string ArryName, string _UsId, string _RegAdd, string _RegData) // only support Register write, not Ext.
            {
                bool isAddsuccese = true;
                string ArryString = null;
                char[] arr;
                string USID = null;
                string RegAdd = null;
                string RegData = null;
                string Command_Parity_bit = null;
                string Data_Parity_bit = null;

                int Command_Parity_Count = 1;
                int Data_Parity_Count = 0;
                string RegWrite = "0;1;0;";
                string BusPark = "0";

                USID = Convert.ToString(Convert.ToInt32(_UsId, 16), 2).PadLeft(4, '0'); arr = USID.ToCharArray(); USID = ""; foreach (char Value in arr) { USID += Convert.ToString(Value); USID += ";"; if (Value == '1') Command_Parity_Count++; }
                RegAdd = Convert.ToString(Convert.ToInt32(_RegAdd, 16), 2).PadLeft(5, '0'); arr = RegAdd.ToCharArray(); RegAdd = ""; foreach (char Value in arr) { RegAdd += Convert.ToString(Value); RegAdd += ";"; if (Value == '1') Command_Parity_Count++; }
                RegData = Convert.ToString(Convert.ToInt32(_RegData, 16), 2).PadLeft(8, '0'); arr = RegData.ToCharArray(); RegData = ""; foreach (char Value in arr) { RegData += Convert.ToString(Value); RegData += ";"; if (Value == '1') Data_Parity_Count++; }

                Command_Parity_bit = (Command_Parity_Count % 2) == 0 ? "1;" : "0;";
                Data_Parity_bit = (Data_Parity_Count % 2) == 0 ? "1;" : "0;";

                ArryString = USID + RegWrite + RegAdd + Command_Parity_bit + RegData + Data_Parity_bit + BusPark;
                uint[] uintArry = ArryString.Split(';').Select(uint.Parse).ToArray();

                if (uintArry.Length == 23)
                    SourceWaveform[ArryName] = uintArry;
                else
                    isAddsuccese = false;

                return isAddsuccese;
            }

            //kengs shan Added
            public override void SetSourceWaveformArry(string customMIPIlist)
            {
                try
                {
                    //string strCustomMIPI;
                    string strUSID = "", strRegAdd, strData;
                    bool isAddsucess = true;

                    //List<string> listCustomString = new List<string>();
                    List<string> listCustomDict = new List<string>();

                    //for (int i = 0; i < customMIPIlist.Count; i++)
                    //{
                    //    customMIPIlist[i].TryGetValue("SWTIMECUSTOM", out strCustomMIPI);
                    //    if (strCustomMIPI != "") listCustomString.Add(strCustomMIPI);
                    //}

                    //for (int j = 0; j < listCustomString.Count; j++)
                    //{
                    string[] strArrList = customMIPIlist.Split(':');

                    for (int k = 0; k < strArrList.Length; k++)
                    {
                        string[] strArrListSplit = strArrList[k].Split(',');

                        for (int m = 0; m < strArrListSplit.Length; m++)
                        {
                            if (!listCustomDict.Contains(strArrListSplit[m]))
                            {
                                var charCustom = strArrListSplit[m].ToCharArray();

                                //if (charCustom[0] == 'T')
                                //    strUSID = "E";
                                //else if (charCustom[0] == 'R')
                                //    strUSID = "C";
                                strUSID = charCustom[0].ToString(); //slave address setting

                                strRegAdd = "0" + charCustom[1].ToString();
                                strData = charCustom[2].ToString() + charCustom[3].ToString();

                                if (isAddsucess)
                                {
                                    isAddsucess = AddtoDic_SourceWaveformArry(strArrListSplit[m], strUSID, strRegAdd, strData);
                                    if (isAddsucess) listCustomDict.Add(strArrListSplit[m]);
                                }
                            }
                        }
                    }
                    //}

                    if (!isAddsucess) MessageBox.Show("Please check your Usid, Reg Add Or Data", "SorceWaveform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("NI6570: SetSourceWaveformArry error: " + ex.Message);
                }

                /*
                //bool isAddsucess = true;

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T000", "E", "00", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T00D", "E", "00", "0D");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T01D", "E", "00", "1D");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T01D", "E", "00", "3D");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T055", "E", "00", "55");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T009", "E", "00", "09");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T1B4", "E", "01", "B4");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T100", "E", "01", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T1C0", "E", "01", "C0");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T204", "E", "02", "04");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T202", "E", "02", "02");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T20D", "E", "02", "0D");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T207", "E", "02", "07");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T206", "E", "02", "06");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T2D0", "E", "02", "D0");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T240", "E", "02", "40");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T300", "E", "03", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T308", "E", "03", "08");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T320", "E", "03", "20");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T3A0", "E", "03", "A0");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T380", "E", "03", "80");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T400", "E", "04", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T401", "E", "04", "01");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("T402", "E", "04", "02");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R100", "C", "01", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R101", "C", "01", "01");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R104", "C", "01", "04");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R106", "C", "01", "06");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R107", "C", "01", "07");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R200", "C", "02", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R201", "C", "02", "01");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R300", "C", "03", "00");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R86F", "C", "08", "6F");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R85A", "C", "08", "5A");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R84C", "C", "08", "4C");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R84E", "C", "08", "4E");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R800", "C", "08", "00");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R847", "C", "08", "47");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("R848", "C", "08", "48");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("RC00", "C", "0C", "00");

                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("B1G0", "C", "08", "47");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("B1G1", "C", "08", "4E");    //Steven: 4C
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("B1G3", "C", "08", "5A");
                if (isAddsucess) isAddsucess = AddtoDic_SourceWaveformArry("B1G5", "C", "08", "6F");

                if (!isAddsucess) MessageBox.Show("Please check your Usid, Reg Add Or Data", "SorceWaveform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            */
            }

            /// <summary>
            /// Not currently supported.  Suport will be added in future driver release.
            /// </summary>
            /// <param name="namesInMemory"></param>
            /// <param name="finalizeScript"></param>
            public override void AddVectorsToScript(List<string> namesInMemory, bool finalizeScript)
            {
            }

            /// <summary>
            /// Returns the number of bit errors from the most recently executed pattern.
            /// </summary>
            /// <param name="nameInMemory">Not Used</param>
            /// <returns>Number of bit errors</returns>
            public override int GetNumExecErrors(string nameInMemory)
            {
                //Int64[] failureCount = sdata1Pin.GetFailCount();
                //return (int)failureCount[0];
                return NumBitErrors;
            }

            /// <summary>
            /// Dynamic Multiple Register Write function.  This uses NI 6570 source memory to dynamically change
            /// the register address and write values in the pattern.
            /// This supports extended register write.
            /// </summary>
            /// <param name="registerAddress_hex">The register address to write (hex)</param>
            /// <param name="data_hex">The data to write into the specified register in Hex.  Note:  Maximum # of bytes to write is 16.</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public override void RegWriteMultiple(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)
            {
                int numOfWrites = 0;
                // Source buffer must contain 512 elements, even if sourcing less
                uint[] dataArray = new uint[512];

                try
                {
                    if ((MipiCommands == null) || (MipiCommands.Count == 0)) return;

                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", "");

                    // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                    DIGI.PatternControl.Commit();

                    // Build source data from ClsMIPIFrame
                    bool EnableMaskedWrite = IsMaskedWrite(MipiCommands);

                    dataArray = GeneraterwaveformArry(ref numOfWrites, MipiCommands, EnableMaskedWrite);

                    string PatternName = "MultipleExtendedRegisterWritewithreg";

                    if (EnableMaskedWrite) PatternName = PatternName.Replace("Write", "MaskedWrite");

                    // Configure 6570 to source data calculated above
                    //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "SrcMultipleExtendedRegisterWrite" + numOfWrites.ToString() , SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    //DIGI.SourceWaveforms.WriteBroadcast("SrcMultipleExtendedRegisterWrite" + numOfWrites.ToString() , dataArray);
                    //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "SrcMultipleExtendedRegisterWritewithreg", SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    //DIGI.SourceWaveforms.WriteBroadcast("SrcMultipleExtendedRegisterWritewithreg", dataArray);

                    DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "Src" + PatternName, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + PatternName, dataArray);

                    DIGI.PatternControl.WriteSequencerRegister("reg0", numOfWrites);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    //DIGI.PatternControl.StartLabel = "MultipleExtendedRegisterWritewithreg";
                    DIGI.PatternControl.StartLabel = PatternName;
                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Write Multiple MIPI Registers: MultipleExtendedRegisterWritewithreg.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            public override void RegWriteMultiplePair(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)
            {
                for (int PairIndex = 1; PairIndex < 3; PairIndex++)
                {
                    int numOfWrites = 0;
                    // Source buffer must contain 512 elements, even if sourcing less
                    uint[] dataArray = new uint[512];

                    try
                    {
                        if ((MipiCommands == null) || (MipiCommands.Count == 0)) return;

                        bool Check = false;
                        foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                        {
                            if (PairIndex != command.Pair) continue;

                            if (!command.Duplication)
                            {
                                Check = true;
                                break;
                            }
                        }
                        if (Check)
                        {
                            if (!isVioTxPpmu)
                            {
                                // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                                allRffePins.SelectedFunction = SelectedFunction.Digital;
                                allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                            }
                            else
                            {
                                allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                                allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                            }

                            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", "");

                            // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                            DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                            DIGI.PatternControl.Commit();

                            // Build source data from ClsMIPIFrame
                            bool EnableMaskedWrite = IsMaskedWrite(MipiCommands);

                            dataArray = GeneraterwaveformArry(ref numOfWrites, MipiCommands, EnableMaskedWrite, PairIndex);
                            string PatternName = "MultipleExtendedRegisterWritewithreg";

                            if (EnableMaskedWrite) PatternName = PatternName.Replace("Write", "MaskedWrite");

                            string PinName;
                            string waveformNameinMemory = "";

                            if (PairIndex == 1) PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                            else PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                            if (isShareBus) waveformNameinMemory = PatternName + "Pair" + PairIndex;
                            else waveformNameinMemory = PatternName;

                            // Configure 6570 to source data calculated above
                            //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "SrcMultipleExtendedRegisterWrite" + numOfWrites.ToString() , SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                            //DIGI.SourceWaveforms.WriteBroadcast("SrcMultipleExtendedRegisterWrite" + numOfWrites.ToString() , dataArray);
                            //DIGI.SourceWaveforms.CreateSerial(Sdata2ChanName.ToUpper(), "Src" + PatternName, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                            DIGI.SourceWaveforms.CreateSerial(PinName, "Src" + waveformNameinMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                            //DIGI.SourceWaveforms.WriteBroadcast("Src" + PatternName, dataArray);
                            DIGI.SourceWaveforms.WriteBroadcast("Src" + waveformNameinMemory, dataArray);

                            DIGI.PatternControl.WriteSequencerRegister("reg0", numOfWrites);

                            // Choose Pattern to Burst
                            // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                            DIGI.PatternControl.StartLabel = PatternName + "Pair" + PairIndex;

                            // Burst Pattern
                            DIGI.PatternControl.Initiate();

                            // Wait for Pattern Burst to complete
                            DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));
                        }
                    }
                    catch (Exception e)
                    {
                        DIGI.PatternControl.Abort();
                        MessageBox.Show("Failed to Write Multiple MIPI Registers: MultipleExtendedRegisterWritewithreg.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            private bool OTPburn(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands, int TryCount = 1)
            {
                bool isTx = (dutSlavePairIndex == 2) ? false : true; //20190311

                try
                {
                    //bool isTx = (dutSlaveAddress.ToUpper().Contains(Get_Digital_Definition("MIPI2_SLAVE_ADDR")) ? false : true);

                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
                    // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                    DIGI.PatternControl.Commit();

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };

                    if (isTx)
                    {
                        #region Tx OTP

                        switch (Get_Digital_Definition("TX_FABSUPPLIER"))
                        {
                            #region TSMC IP

                            case "TSMC_B12":
                            case "TSMC_B10":
                            case "TSMC_013UM_B12":
                            case "TSMC_065UM_B12":
                                List<MipiSyntaxParser.ClsMIPIFrame> _EfuseProgramMipiCmd = CreatorDataArry(MipiCommands, isTx);
                                _SendOTPGeneratedVectors(_EfuseProgramMipiCmd, isTx, TryCount);
                                break;

                                #endregion TSMC IP
                        }

                        #endregion Tx OTP
                    }
                    else
                    {
                        #region Rx OTP

                        List<MipiSyntaxParser.ClsMIPIFrame> _EfuseProgramMipiCmd;
                        switch (Get_Digital_Definition("RX_FABSUPPLIER"))
                        {
                            #region GF, Merge TSMC 2021.May

                            case "GF_B8":
                            case "GF_B16":
                            case "TSMC_B8":
                            case "TSMC_B16":
                                _EfuseProgramMipiCmd = CreatorDataArry(MipiCommands, isTx);
                                if (_EfuseProgramMipiCmd.Count() > 0)
                                    _SendOTPGeneratedVectors(_EfuseProgramMipiCmd, isTx, TryCount);
                                break;

                                #endregion GF, Merge TSMC 2021.May

#if false

                                #region TSMC

                            case "TSMC_B8":
                            case "TSMC_B16":
                                int numOfWrites, dataArrayIndex;
                                numOfWrites = 0;
                                dataArrayIndex = 0;

                                for (int Byte = 0; Byte < 8; Byte++)
                                {
                                    for (int bit = 0; bit < 8; bit++)
                                    {
                                        foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                                        {
                                            dutSlaveAddress = command.SlaveAddress_hex;
                                            registerAddress_hex = command.Register_hex;
                                            data_hex = command.Data_hex;

                                            int bitVal = (int)Math.Pow(2, bit);
                                            int data_int = Convert.ToInt32(Convert.ToInt32(data_hex, 16));
                                            int Byte_int = Convert.ToInt32(command.Register_hex, 16);
                                            Byte_int = Byte_int > 7 ? (Byte_int - 8) : Byte_int; //  (Byte_int - 8) for second memory. 8 is 1st memory num of byte

                                            // Build extended write command data, setting read byte count and register address.
                                            // Note, write byte count is 0 indexed.
                                            uint cmdBytesWithParity;

                                            cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE, dutSlaveAddress);
                                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                                            dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                                            dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                                            dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                                            // Convert Hex Data string to bytes and add to data Array
                                            if ((Byte == Byte_int) && (bitVal & data_int) == bitVal)
                                            {
                                                dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32("40", 16));
                                                dataArrayIndex += 4;
                                                numOfWrites++;
                                                break;
                                            }
                                            else
                                            {
                                                dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32("00", 16));
                                                dataArrayIndex += 4;
                                                numOfWrites++;
                                            }
                                        }

                                        //foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                                        //{
                                        //    dutSlaveAddress = command.SlaveAddress_hex;
                                        //    registerAddress_hex = command.Register_hex;
                                        //    data_hex = command.Data_hex;

                                        //    int bitVal = (int)Math.Pow(2, bit);
                                        //    int data_int = Convert.ToInt32(Convert.ToInt32(data_hex, 16));
                                        //    int Byte_int = Convert.ToInt32(command.Register_hex, 16);
                                        //    Byte_int = Byte_int > 7 ? (Byte_int - 8) : Byte_int; //  (Byte_int - 8) for second memory. 8 is 1st memory num of byte

                                        //    if ((Byte == Byte_int) && (bitVal & data_int) == bitVal)
                                        //    {
                                        //        dataArray[Byte * 8 + bit] = calculateParity(Convert.ToUInt32("40", 16));
                                        //        break;
                                        //    }
                                        //    else
                                        //    {
                                        //        dataArray[Byte * 8 + bit] = calculateParity(Convert.ToUInt32("00", 16));
                                        //    }
                                        //}
                                    }
                                }

                                //Last bit Indecater
                                if (dataArray[63] != 1) dataArray[63] = calculateParity(Convert.ToUInt32("4F", 16));
                                else dataArray[63] = calculateParity(Convert.ToUInt32("0F", 16));

                                DIGI.SourceWaveforms.CreateSerial(Sdata2ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                                DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                                // Choose Pattern to Burst
                                // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                                DIGI.PatternControl.StartLabel = nameInMemory;

                                // Burst Pattern
                                DIGI.PatternControl.Initiate();

                                // Wait for Pattern Burst to complete
                                DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 100));
                                break;

                                #endregion TSMC

#endif
                        }

                        #endregion Rx OTP
                    }

                    return true;
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show($"Failed to {(isTx ? "Tx" : "Rx")}OTPBurnTemplate. \n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            private void _SendOTPGeneratedVectors(List<MipiSyntaxParser.ClsMIPIFrame> cmdList, bool isTx, int TryCount)
            {
                string nameInMemory = isTx ? "TxOTPBurnTemplate" : (TryCount > 1 ? "RxOTPBurnTemplateRetry" : "RxOTPBurnTemplate");
                string data_hex = "";
                string registerAddress_hex = "";
                uint[] dataArray = new uint[512 * 8]; // Source buffer must contain 512 elements, even if sourcing less

                if (cmdList.Count > 0)
                {
                    int numOfWrites, dataArrayIndex;
                    numOfWrites = 0;
                    dataArrayIndex = 0;

                    foreach (MipiSyntaxParser.ClsMIPIFrame command in cmdList)
                    {
                        dutSlaveAddress = command.SlaveAddress_hex;
                        registerAddress_hex = command.Register_hex;
                        data_hex = command.Data_hex;

                        // Build extended write command data, setting read byte count and register address.
                        // Note, write byte count is 0 indexed.
                        uint cmdBytesWithParity;

                        cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE, dutSlaveAddress);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                        // Convert Hex Data string to bytes and add to data Array
                        dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                        dataArrayIndex += 4; // set for next command
                        numOfWrites++;
                    }

                    DIGI.SourceWaveforms.CreateSerial(isTx ? Sdata1ChanName.ToUpper() : Sdata2ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                    DIGI.PatternControl.WriteSequencerRegister("reg0", numOfWrites);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory;

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 100));
                }
            }

            private List<MipiSyntaxParser.ClsMIPIFrame> CreatorDataArry(List<MipiSyntaxParser.ClsMIPIFrame> _mipicmd, bool isTx)
            {
                List<MipiSyntaxParser.ClsMIPIFrame> listmipicmd = new List<EqLib.MipiSyntaxParser.ClsMIPIFrame>();
                string data_hex;
                int indexcmd = 0;

                if (isTx)
                {
                    string RegOTPSigCtrl = Eq.Site[Site].HSDIO.Get_Digital_Definition("REG_TX_OTP_SIG_CTRL");
                    string RegOTPAddrCtrl = Eq.Site[Site].HSDIO.Get_Digital_Definition("REG_TX_OTP_ADDR_CTRL", "F1");

                    switch (Eq.Site[Site].HSDIO.Get_Digital_Definition("TX_FABSUPPLIER"))
                    {
                        case "TSMC_B10":
                        case "TSMC_B12":
                            foreach (MipiSyntaxParser.ClsMIPIFrame command in _mipicmd)
                            {
                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int bitVal = (int)Math.Pow(2, bit);

                                    if ((bitVal & Convert.ToInt32(command.Data_hex, 16)) == bitVal)
                                    {
                                        uint OffburnDataDec = Convert.ToUInt32((Convert.ToInt32(command.Register_hex, 16) << 3) + bit);
                                        uint OnburnDataDec = (1 << 7) + OffburnDataDec;
                                        data_hex = OnburnDataDec.ToString("X"); // Convert captured data to hex string and return

                                        if (data_hex.Length % 2 == 1) data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                                        listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex,
                                        RegOTPSigCtrl,
                                        false,
                                        data_hex,
                                        command.Pair));
                                    }
                                }
                            }
                            break;

                        case "TSMC_013UM_B12":
                            foreach (MipiSyntaxParser.ClsMIPIFrame command in _mipicmd)
                            {
                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int bitVal = (int)Math.Pow(2, bit);

                                    if ((bitVal & Convert.ToInt32(command.Data_hex, 16)) == bitVal)
                                    {
                                        uint OffburnDataDec = Convert.ToUInt32((Convert.ToInt32(command.Register_hex, 16) << 3) + bit);
                                        uint OnburnDataDec = (1 << 7) + OffburnDataDec;
                                        data_hex = OffburnDataDec.ToString("X"); // Convert captured data to hex string and return

                                        if (data_hex.Length % 2 == 1) data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                                        listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex,
                                        RegOTPAddrCtrl,
                                        false,
                                        data_hex,
                                        command.Pair));

                                        listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex,
                                        RegOTPSigCtrl,
                                        false,
                                        "50",
                                        command.Pair));

                                        listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex,
                                        RegOTPSigCtrl,
                                        false,
                                        "40",
                                        command.Pair));
                                    }
                                }
                            }
                            break;

                        case "TSMC_065UM_B12":
                            bool isFirst = true;

                            foreach (MipiSyntaxParser.ClsMIPIFrame command in _mipicmd)
                            {
                                bool is2ndMemory = Convert.ToInt32(command.Register_hex, 16) > 7;

                                indexcmd = 0;
                                dutSlaveAddress = command.SlaveAddress_hex;
                                data_hex = command.Data_hex;

                                if (isFirst)
                                {
                                    for (int Byte = 0; Byte < (is2ndMemory ? 4 : 8); Byte++)
                                    {
                                        for (int bit = 0; bit < 8; bit++)
                                        {
                                            listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(dutSlaveAddress, RegOTPSigCtrl, false, is2ndMemory ? "D0" : "E0", command.Pair));
                                            listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(dutSlaveAddress, RegOTPSigCtrl, false, is2ndMemory ? "D2" : "E1", command.Pair));
                                            listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(dutSlaveAddress, RegOTPSigCtrl, false, is2ndMemory ? "D2" : "E1", command.Pair));
                                            listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(dutSlaveAddress, RegOTPSigCtrl, false, is2ndMemory ? "D0" : "E0", command.Pair));
                                        }
                                    }
                                    isFirst = false;
                                }

                                for (int Byte = 0; Byte < (is2ndMemory ? 4 : 8); Byte++)
                                {
                                    for (int bit = 0; bit < 8; bit++)
                                    {
                                        int offset = indexcmd * 4;

                                        indexcmd++;
                                        int bitVal = (int)Math.Pow(2, bit);
                                        int data_int = Convert.ToInt32(Convert.ToInt32(data_hex, 16));

                                        if (data_int == 0) break;

                                        int Byte_int = Convert.ToInt32(command.Register_hex, 16);
                                        Byte_int = Byte_int > 7 ? (Byte_int - 8) : Byte_int; //  (Byte_int - 8) for second memory. 8 is 1st memory num of byte

                                        if ((Byte == Byte_int) && (bitVal & data_int) == bitVal)
                                        {
                                            listmipicmd[offset + 0].Data_hex = is2ndMemory ? "D8" : "E4";
                                            listmipicmd[offset + 1].Data_hex = is2ndMemory ? "DA" : "E5";
                                            listmipicmd[offset + 2].Data_hex = is2ndMemory ? "D2" : "E1";
                                            listmipicmd[offset + 3].Data_hex = is2ndMemory ? "D0" : "E0";
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    switch (Eq.Site[Site].HSDIO.Get_Digital_Definition("RX_FABSUPPLIER"))
                    {
                        case "GF_B16":
                        case "GF_B8":

                            #region GF_B8/16

                            string CmdPgmOn = Get_Digital_Definition("RX_FABSUPPLIER").Contains("B8") ? "A0" : "A1"; // A0 : No Ip selector

                            foreach (MipiSyntaxParser.ClsMIPIFrame command in _mipicmd)
                            {
                                indexcmd++;

                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int bitVal = (int)Math.Pow(2, bit);

                                    if ((bitVal & Convert.ToInt32(command.Data_hex, 16)) == bitVal)
                                    {
                                        if (listmipicmd.Count == 0)
                                        {
                                            string _Data_hex;

                                            if (Convert.ToInt32(command.Register_hex, 16) < 8)
                                                _Data_hex = CmdPgmOn; //A1
                                            else
                                                _Data_hex = "A2"; //A2

                                            listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex, "F0", false, _Data_hex, command.Pair));
                                        }

                                        int uRegister_hex = (Convert.ToInt32(command.Register_hex, 16) < 8 ?
                                                                Convert.ToInt32(command.Register_hex, 16) :
                                                                Convert.ToInt32(command.Register_hex, 16) - 8);

                                        uint OffburnDataDec = Convert.ToUInt32((uRegister_hex << 3) + bit);
                                        uint OnburnDataDec = (1 << 7) + OffburnDataDec;

                                        data_hex = OnburnDataDec.ToString("X"); // Convert captured data to hex string and return
                                        if (data_hex.Length % 2 == 1) data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                                        listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex, "F2", false, data_hex, command.Pair));
                                    }
                                }

                                if (indexcmd == _mipicmd.Count)
                                {
                                    listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex, "F2", false, "00", command.Pair));
                                    listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex, "F0", false, "78", command.Pair));
                                }
                            }

                            #endregion GF_B8/16

                            break;

                        case "TSMC_B16":
                        case "TSMC_B8":

                            #region TSMC_B8/16

                            string registerAddress_hex;
                            string sig_ctrl = Eq.Site[Site].HSDIO.Get_Digital_Definition("REG_RX_OTP_SIG_CTRL", "F0");

                            foreach (MipiSyntaxParser.ClsMIPIFrame command in _mipicmd)
                            {
                                indexcmd = 0;
                                int prevIndex = indexcmd;

                                dutSlaveAddress = command.SlaveAddress_hex;
                                registerAddress_hex = command.Register_hex;
                                data_hex = command.Data_hex;

                                for (int Byte = 0; Byte < 8; Byte++)
                                {
                                    for (int bit = 0; bit < 8; bit++)
                                    {
                                        prevIndex = indexcmd;

                                        indexcmd++;

                                        int bitVal = (int)Math.Pow(2, bit);
                                        int data_int = Convert.ToInt32(Convert.ToInt32(data_hex, 16));

                                        if (data_int == 0) break;

                                        int Byte_int = Convert.ToInt32(command.Register_hex, 16);
                                        Byte_int = Byte_int > 7 ? (Byte_int - 8) : Byte_int; //  (Byte_int - 8) for second memory. 8 is 1st memory num of byte

                                        if ((Byte == Byte_int) && (bitVal & data_int) == bitVal)
                                        {
                                            if (listmipicmd.ElementAtOrDefault(prevIndex) != null)
                                            {
                                                listmipicmd[prevIndex].Data_hex = indexcmd == 64 ? "4F" : "40";
                                            }
                                            else
                                            {
                                                listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(dutSlaveAddress, sig_ctrl, false, indexcmd == 64 ? "4F" : "40", command.Pair));
                                            }
                                        }
                                        else
                                        {
                                            if (listmipicmd.ElementAtOrDefault(prevIndex) == null)
                                            {
                                                listmipicmd.Add(new EqLib.MipiSyntaxParser.ClsMIPIFrame(command.SlaveAddress_hex, sig_ctrl, false, indexcmd == 64 ? "0F" : "00", command.Pair));
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion TSMC_B8/16

                            break;
                    }
                }

                return listmipicmd;
            }

            //Seoul
            private void TimingRegWriteMultiple(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)//, int _nBeforeCmd, double _BeforeDelay, double _AfterDelay)
            {
                string data_hex = "";
                string registerAddress_hex = "";
                int numOfWrites = 0;
                // Source buffer must contain 512 elements, even if sourcing less
                uint[] dataArray = new uint[512];
                int dataArrayIndex = 0;

                string PinName;

                if (dutSlavePairIndex == 1) PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                else PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                {
                    dutSlaveAddress = command.SlaveAddress_hex;
                    registerAddress_hex = command.Register_hex;
                    data_hex = command.Data_hex;

                    // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                    if (data_hex.Length % 2 == 1)
                        data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                    // Build extended write command data, setting read byte count and register address.
                    // Note, write byte count is 0 indexed.
                    uint cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE, dutSlaveAddress);
                    // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                    dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                    dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                    dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                    // Convert Hex Data string to bytes and add to data Array
                    dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                    dataArrayIndex = dataArrayIndex + 4; // set for next command
                    numOfWrites++;
                }

                try
                {
                    if ((MipiCommands == null) || (MipiCommands.Count == 0)) return;

                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", pxiTrigger.ToString("g"));

                    // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                    DIGI.PatternControl.Commit();

                    // Configure 6570 to source data calculated above
                    //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "SrcTimingExtendedRegisterWriteReg", SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.CreateSerial(PinName, "SrcTimingExtendedRegisterWriteRegPair" + Convert.ToString(dutSlavePairIndex), SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("SrcTimingExtendedRegisterWriteReg", dataArray);

                    //Vaild values for register inculde  Reg0-15
                    //Numeric is the data 16bits ( 0 - 65535 )

                    int singleMipiCmdFrmbits = 38;
                    int TriggerLength = 102;

                    int _nAftercCmd = MipiCommands.Count - nBeforeCmd - 1;
                    int _nBits_Before = (int)(((BeforeDelay * 1e-6) - (1 / MIPIClockRate) * singleMipiCmdFrmbits) * MIPIClockRate);
                    int _nBits_After = (int)(((AfterDelay * 1e-6) - ((1 / MIPIClockRate) * (TriggerLength + (singleMipiCmdFrmbits * _nAftercCmd)))) * MIPIClockRate);

                    if (_nBits_Before < 0 || _nBits_After < 0 || _nAftercCmd < 0) throw new Exception("Please check Timing Stript \nCan not set to the delay time due to Number of command");

                    DIGI.PatternControl.WriteSequencerRegister("reg0", nBeforeCmd);
                    DIGI.PatternControl.WriteSequencerRegister("reg1", _nBits_Before); // 58 bits for Fixed Vector
                    DIGI.PatternControl.WriteSequencerRegister("reg2", _nBits_After); // 102 bits for Fixed Vector + (56 * n) bits for Command
                    DIGI.PatternControl.WriteSequencerRegister("reg3", _nAftercCmd); // -1 for Trig Cmd

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = "TimingExtendedRegisterWriteReg";

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Write Multiple MIPI Registers: TimingExtendedRegisterWriteReg" + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void TimingRegWriteMultiplePair(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)//, int _nBeforeCmd, double _BeforeDelay, double _AfterDelay)
            {
                for (int PairIndex = 1; PairIndex < 3; PairIndex++)
                {
                    string data_hex = "";
                    string registerAddress_hex = "";
                    int numOfWrites = 0;
                    // Source buffer must contain 512 elements, even if sourcing less
                    uint[] dataArray = new uint[512];
                    int dataArrayIndex = 0;

                    foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                    {
                        if (PairIndex != command.Pair) continue;

                        dutSlaveAddress = command.SlaveAddress_hex;
                        registerAddress_hex = command.Register_hex;
                        data_hex = command.Data_hex;

                        // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                        if (data_hex.Length % 2 == 1)
                            data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                        // Build extended write command data, setting read byte count and register address.
                        // Note, write byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE, dutSlaveAddress);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                        // Convert Hex Data string to bytes and add to data Array
                        dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                        dataArrayIndex = dataArrayIndex + 4; // set for next command
                        numOfWrites++;
                    }

                    try
                    {
                        if ((MipiCommands == null) || (MipiCommands.Count == 0)) return;
                        if (numOfWrites == 0) continue;

                        if (!isVioTxPpmu)
                        {
                            // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                            allRffePins.SelectedFunction = SelectedFunction.Digital;
                            allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }
                        else
                        {
                            allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                            allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }

                        // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                        DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", pxiTrigger.ToString("g"));

                        // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                        DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                        DIGI.PatternControl.Commit();

                        string PatternName = "TimingExtendedRegisterWriteReg1";

                        //if (EnableMaskedWrite) PatternName = PatternName.Replace("Write", "MaskedWrite");

                        string PinName;
                        string waveformNameinMemory = "";

                        if (PairIndex == 1) PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                        else PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                        if (isShareBus) waveformNameinMemory = PatternName + "Pair" + PairIndex;
                        else waveformNameinMemory = PatternName;

                        // Configure 6570 to source data calculated above
                        //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "SrcTimingExtendedRegisterWriteReg", SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                        //DIGI.SourceWaveforms.CreateSerial(PinName, "SrcTimingExtendedRegisterWriteRegPair" + Convert.ToString(dutSlavePairIndex), SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                        //DIGI.SourceWaveforms.WriteBroadcast("SrcTimingExtendedRegisterWriteReg", dataArray);

                        DIGI.SourceWaveforms.CreateSerial(PinName, "Src" + waveformNameinMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                        //DIGI.SourceWaveforms.WriteBroadcast("Src" + PatternName, dataArray);
                        DIGI.SourceWaveforms.WriteBroadcast("Src" + waveformNameinMemory, dataArray);

                        //Vaild values for register inculde  Reg0-15
                        //Numeric is the data 16bits ( 0 - 65535 )

                        int singleMipiCmdFrmbits = 38;
                        int TriggerLength = 102;

                        int _nAftercCmd = MipiCommands.Count - nBeforeCmd - 1;
                        int _nBits_Before = (int)(((BeforeDelay * 1e-6) - (1 / MIPIClockRate) * singleMipiCmdFrmbits) * MIPIClockRate);
                        int _nBits_After = (int)(((AfterDelay * 1e-6) - ((1 / MIPIClockRate) * (TriggerLength + (singleMipiCmdFrmbits * _nAftercCmd)))) * MIPIClockRate);

                        if (_nBits_Before < 0 || _nBits_After < 0 || _nAftercCmd < 0) throw new Exception("Please check Timing Stript \nCan not set to the delay time due to Number of command");

                        DIGI.PatternControl.WriteSequencerRegister("reg0", nBeforeCmd);
                        DIGI.PatternControl.WriteSequencerRegister("reg1", _nBits_Before); // 58 bits for Fixed Vector
                        DIGI.PatternControl.WriteSequencerRegister("reg2", _nBits_After); // 102 bits for Fixed Vector + (56 * n) bits for Command
                        DIGI.PatternControl.WriteSequencerRegister("reg3", _nAftercCmd); // -1 for Trig Cmd

                        // Choose Pattern to Burst
                        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                        //DIGI.PatternControl.StartLabel = "TimingExtendedRegisterWriteReg";
                        DIGI.PatternControl.StartLabel = waveformNameinMemory; // PatternName + "Pair" + PairIndex;

                        // Burst Pattern
                        DIGI.PatternControl.Initiate();

                        // Wait for Pattern Burst to complete
                        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));
                    }
                    catch (Exception e)
                    {
                        DIGI.PatternControl.Abort();
                        MessageBox.Show("Failed to Write Multiple MIPI Registers: TimingExtendedRegisterWriteReg" + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            public override bool Burn(string data_hex, bool invertData = false, int efuseDataByteNum = 0, string efuseCtlAddress = "C0")
            {
                Stopwatch myWatch1 = new Stopwatch();

                int data_dec = Convert.ToInt32(data_hex, 16);

                if (data_dec > 255)
                {
                    MessageBox.Show("Error: Cannot burn decimal values greater than 255", "BurnOTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // burn the data

                myWatch1.Restart();
                double[] OTPTiem = new double[8];

                for (int bit = 0; bit < 8; bit++)
                {
                    int bitVal = (int)Math.Pow(2, bit);

                    if ((bitVal & data_dec) == (invertData ? 0 : bitVal))
                    {
                        //for (int programMode = 1; programMode >= 0; programMode--)
                        //{
                        //    int burnDataDec = (programMode << 7) + (efuseDataByteNum << 3) + bit;
                        //    //HSDIO.Instrument.RegWrite(HSDIO.dutSlaveAddress, efuseCtlAddress, burnDataDec.ToString("X"), false, true);
                        //    //HSDIO.Instrument.RegWrite(efuseCtlAddress, burnDataDec.ToString("X"), false);

                        //    HSDIO.Instrument.RegWrite("3",efuseCtlAddress, burnDataDec.ToString("X"));
                        //    OTPTiem[bit] = myWatch1.Elapsed.TotalMilliseconds;
                        //}

                        int burnDataDec = (1 << 7) + (efuseDataByteNum << 3) + bit;
                        SendVectorOTP(burnDataDec.ToString("X"), "00");
                    }
                }

                return true;
            }

            public override bool SendVectorOTP(string TargetData, string CurrentData = "00", bool isEfuseBurn = false)
            {
                try
                {
                    //bool isTx = (dutSlaveAddress.ToUpper().Contains(Get_Digital_Definition("MIPI2_SLAVE_ADDR")) ? false : true);
                    bool isTx = (dutSlavePairIndex == 2) ? false : true; //20190311

                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
                    // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);

                    DIGI.PatternControl.Commit();

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };

                    string nameInMemory = (isTx ? "TxOTPBurnTemplate" : "RxOTPBurnTemplate");

                    // Source buffer must contain 512 elements, even if sourcing less
                    uint[] dataArray = new uint[512];

                    if (isTx)
                    {
                        #region DataArry calculate TX

                        //char[] arr;
                        //string Data = "";
                        //string DataFrameParity = "";
                        //int Parity_Count = 0;

                        //string ExData = TargetData;

                        //Data = Convert.ToString(Convert.ToInt32(ExData, 16), 2).PadLeft(8, '0'); arr = Data.ToCharArray(); Data = ""; foreach (char Value in arr) { Data += Convert.ToString(Value); }
                        //DataFrameParity = Convert.ToString(Convert.ToInt32(ExData, 16), 2).PadLeft(8, '0'); foreach (char Parse_Command in DataFrameParity) { if (Parse_Command == '1') Parity_Count++; }

                        //int reWrite = (Convert.ToInt32(TargetData, 16) << 1) + ((Parity_Count % 2) == 0 ? 1 : 0);

                        //for (int bit = 0; bit < 2; bit++)
                        //{
                        //    dataArray[bit] = (bit == 0 ? (uint)(reWrite) :
                        //        (
                        //            (uint)((reWrite - (Parity_Count % 2 == 0 ? 1 : 0)) - (1 << 8))
                        //        )

                        //    );
                        //}

                        string RegOTPctrl = Eq.Site[Site].HSDIO.Get_Digital_Definition("REG_TX_OTP_SIG_CTRL");

                        string data_hex = "";
                        int dataArrayIndex = 0;
                        uint OffburnDataDec = Convert.ToUInt32((Convert.ToInt32(TargetData, 16) & 0x7F));//Convert.ToUInt32((Convert.ToInt32(registerAddress_hex, 16) << 3) + bit);
                        uint OnburnDataDec = Convert.ToUInt32(TargetData, 16);// ; ; (1 << 7) + OffburnDataDec;

                        data_hex = OnburnDataDec.ToString("X"); // Convert captured data to hex string and return
                                                                //data_hex = TargetData;
                                                                // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                        if (data_hex.Length % 2 == 1)
                            data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                        uint cmdBytesWithParity;

                        cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(RegOTPctrl, 16)); // address 9 bits

                        // Convert Hex Data string to bytes and add to data Array
                        dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                        DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);

                        #endregion DataArry calculate TX
                    }
                    else
                    {
                        #region DataArry calculate RX

                        string Efuse = "00";
                        if (isEfuseBurn) Efuse = "80";

                        int reWrite = ((Convert.ToInt32(TargetData, 16) ^ Convert.ToInt32(CurrentData, 16)) & Convert.ToInt32(TargetData, 16));
                        int inEfuse = (Convert.ToInt32(Efuse, 16));//reWrite += Convert.ToInt32(Efuse, 16) << 8;
                                                                   //int inEfuseForBandGab = (Convert.ToInt32("04", 16));//reWrite += Convert.ToInt3

                        bool isLastIndicator = false;

                        for (int Add = 0; Add < 8; Add++)
                        {
                            if (Add == 0)
                            {
                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int bitVal = (int)Math.Pow(2, bit);
                                    if ((bitVal & reWrite) == bitVal) dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 158 : 128);
                                    else dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 31 : 1);
                                }
                            }
                            //else if (Add == 3) //BandGap OTP...
                            //{
                            //    for (int bit = 0; bit < 8; bit++)
                            //    {
                            //        int bitVal = (int)Math.Pow(2, bit);
                            //        if ((bitVal & inEfuseForBandGab) == bitVal) dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 158 : 128);
                            //        else dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 31 : 1);
                            //    }
                            //}
                            else if (Add == 7)
                            {
                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int bitVal = (int)Math.Pow(2, bit);
                                    if (bit == 7) isLastIndicator = true;
                                    if ((bitVal & inEfuse) == bitVal) dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 158 : 128);
                                    else dataArray[Add * 8 + bit] = (uint)(isLastIndicator ? 31 : 1);
                                }
                            }
                            else
                                for (int bit = 0; bit < 8; bit++) dataArray[Add * 8 + bit] = 1;
                        }
                        DIGI.SourceWaveforms.CreateSerial(Sdata2ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);

                        #endregion DataArry calculate RX
                    }

                    // Configure 6570 to source data calculated above

                    DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory;

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 100));

                    //// Get PassFail Results for site 0
                    //Int64[] failureCount = sdataPin.GetFailCount();
                    //NumExecErrors = (int)failureCount[0];
                    //if (debug) Console.WriteLine("RegWrite " + nameInMemory + " Bit Errors: " + NumExecErrors.ToString());
                    return true;
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to RxOTPBurnTemplate. \n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            /// <summary>
            /// Dynamic Register Write function.  This uses NI 6570 source memory to dynamically change
            /// the register address and write values in the pattern.
            /// This supports extended and non-extended register write.
            /// </summary>
            /// <param name="registerAddress_hex">The register address to write (hex)</param>
            /// <param name="data_hex">The data to write into the specified register in Hex.  Note:  Maximum # of bytes to write is 16.</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public override void RegWrite(string registerAddress_hex, string data_hex, string slave_address, bool sendTrigger)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    if (sendTrigger)
                    {
                        triggerConfig = TrigConfig.PXI_Backplane;
                        // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_trigger, clear_trigger, and pulse_trigger opcodes
                        if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
                        {
                            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", pxiTrigger.ToString("g"));
                        }
                        else
                        {
                            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", "");
                        }

                        // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
                        if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.PXI_Backplane)
                        {
                            DIGI.PatternControl.WriteSequencerFlag("seqflag0", true);
                        }
                        else
                        {
                            // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                            DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
                        }

                        if (triggerConfig == TrigConfig.None)
                        {
                            throw new Exception("sendTrigger=True requested, but NI 6570 is not configured for Triggering.  Please update the NI6570 Constructor triggerConfig to use TrigConfig.Digital_Pin, TrigConfig.PXI_Backplane, or TrigConfig.Both.");
                        }
                    }
                    else
                    {
                        // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                        DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", "");

                        // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                        DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
                    }

                    DIGI.PatternControl.Commit();

                    // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                    if (data_hex.Length % 2 == 1)
                        data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };
                    bool extendedWrite = Convert.ToInt32(registerAddress_hex, 16) > 31;    // any register address > 5 bits requires extended read
                    uint writeByteCount = extendedWrite ? (uint)(data_hex.Length / 2) : 1;
                    string nameInMemory = extendedWrite ? "ExtendedRegisterWrite" + writeByteCount.ToString() : "RegisterWrite";

                    // Source buffer must contain 512 elements, even if sourcing less
                    uint[] dataArray = new uint[512];
                    if (!extendedWrite)
                    {
                        // Build non-exteded write command
                        uint cmdBytesWithParity = generateRFFECommand(registerAddress_hex, Command.REGISTERWRITE, slave_address);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[2] = calculateParity(Convert.ToUInt32(data_hex, 16)); // final 9 bits
                    }
                    else
                    {
                        // Build extended read command data, setting read byte count and register address.
                        // Note, write byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(Convert.ToString(writeByteCount - 1, 16), Command.EXTENDEDREGISTERWRITE, slave_address);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits
                                                                                                   // Convert Hex Data string to bytes and add to data Array
                        for (int i = 0; i < writeByteCount * 2; i += 2)
                            dataArray[3 + (i / 2)] = (uint)(calculateParity(Convert.ToByte(data_hex.Substring(i, 2), 16)));
                    }

                    string PinName;

                    if (dutSlavePairIndex == 1) PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                    else PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    if (isShareBus) nameInMemory += "Pair" + dutSlavePairIndex;
                    DIGI.PatternControl.StartLabel = nameInMemory;

                    // Configure 6570 to source data calculated above
                    //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.CreateSerial(PinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                    // Get PassFail Results for site 0
                    Int64[] failureCount = sdata1Pin.GetFailCount();
                    NumBitErrors = (int)failureCount[0];
                    if (debug) Console.WriteLine("RegWrite " + registerAddress_hex + " Bit Errors: " + NumBitErrors.ToString());
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent0", "");
                    DIGI.PatternControl.Commit();
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Write Register for Address " + registerAddress_hex + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            /// <summary>
            /// Dynamic Register Read function.  This uses NI 6570 source memory to dynamically change
            /// the register address and uses NI 6570 capture memory to receive the values from the DUT.
            /// This supports extended and non-extended register read.
            /// </summary>
            /// <param name="registerAddress_hex">The register address to read (hex)</param>
            /// <param name="slave_address">The slave address to read (hex)</param>
            /// <returns>The value of the specified register in Hex</returns>
            public override string RegRead(string registerAddress_hex, string slave_address = null)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    if (!isVioTxPpmu)
                    {
                        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePinswoVio.SelectedFunction = SelectedFunction.Digital;
                        allRffePinswoVio.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    string[] registerAddress_hex_Arry = registerAddress_hex.Split(':');

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };
                    bool extendedRead;
                    bool Ismultiple = extendedRead = (registerAddress_hex_Arry.Count() > 1 ? true : false);
                    uint readByteCount = 1;

                    if (!Ismultiple) extendedRead = Convert.ToInt32(registerAddress_hex, 16) > 31;    // any register address > 5 bits requires extended read

                    string nameInMemory = extendedRead ? "ExtendedRegisterRead" + readByteCount.ToString() : "RegisterRead";

                    if (Ismultiple) nameInMemory = "Multiple" + nameInMemory;

                    int dataArrayIndex = 0;
                    int numOfWrites = 0;
                    uint[] dataArray = new uint[512];
                    string CurrentSlaveAddress = "";
                    // Source buffer must contain 512 elements, even if sourcing less

                    foreach (string current_Hex in registerAddress_hex_Arry)
                    {
                        if (!extendedRead)
                        {
                            // Build non-extended read command data
                            uint cmdBytesWithParity = generateRFFECommand(current_Hex, Command.REGISTERREAD, slave_address);
                            // Split data into array of data, all must be same # of bits (16) which must be specified when calling CreateSerial
                            dataArray[0] = cmdBytesWithParity;
                            numOfWrites++;
                        }
                        else
                        {
                            // Build extended read command data, setting read byte count and register address.
                            // Note, read byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(Convert.ToString(readByteCount - 1, 16), Command.EXTENDEDREGISTERREAD, slave_address);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[dataArrayIndex] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            dataArray[dataArrayIndex + 2] = (uint)(calculateParity(Convert.ToUInt16(current_Hex, 16)));  // Final 9 bits to contains the address (for extended read) + parity.

                            dataArrayIndex = dataArrayIndex + 3; // set for next command
                            numOfWrites++;
                        }
                    }

                    string PinName;
                    string waveformNameinMemory = "";

                    if (dutSlavePairIndex == 1) PinName = Sdata1ChanName.ToUpper(); //Sdata1ChanName.ToUpper();
                    else PinName = Sdata2ChanName.ToUpper(); //Sdata1ChanName.ToUpper();

                    if (isShareBus) waveformNameinMemory = nameInMemory + "Pair" + dutSlavePairIndex;
                    else waveformNameinMemory = nameInMemory;

                    // Configure to source data
                    //DIGI.SourceWaveforms.CreateParallel(Sdata1ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast);
                    //DIGI.SourceWaveforms.CreateSerial(Sdata1ChanName.ToUpper(), "Src" + nameInMemory, SourceDataMapping.Broadcast, (uint)(extendedRead ? 9 : 16), BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.CreateSerial(PinName, "Src" + waveformNameinMemory, SourceDataMapping.Broadcast, (uint)(extendedRead ? 9 : 16), BitOrder.MostSignificantBitFirst);
                    //DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + waveformNameinMemory, dataArray);

                    // Configure to capture 8 bits (Ignore Parity)
                    //DIGI.CaptureWaveforms.CreateSerial(SdataChanName.ToUpper(), "Cap" + nameInMemory, readByteCount * 9, BitOrder.MostSignificantBitFirst);

                    // Get Num MIPI Bus and Current Slave address
                    // int Num_MIPI_Bus = Convert.ToUInt16(Get_Digital_Definition("NUM_MIPI_BUS"));

                    slave_address = slave_address ?? dutSlaveAddress;
                    if (slave_address.Length % 2 == 1)
                        CurrentSlaveAddress = slave_address.PadLeft(slave_address.Length + 1, '0');
                    else
                        CurrentSlaveAddress = slave_address;

                    int Current_MIPI_Bus = dutSlavePairIndex;//  Convert.ToUInt16(Get_Digital_Definition("SLAVE_ADDR_" + CurrentSlaveAddress));

                    if (EqHSDIO.Num_Mipi_Bus == 1)
                        DIGI.CaptureWaveforms.CreateParallel(Sdata1ChanName.ToUpper(), "Cap" + nameInMemory);
                    else  // To Do: use Num_MIPI_Bus to automatically add the correct channels to PinSet paramenter in CreateParallel KH
                        DIGI.CaptureWaveforms.CreateParallel(Sdata1ChanName.ToUpper() + "," + Sdata2ChanName.ToUpper(), "Cap" + nameInMemory);

                    DIGI.PatternControl.WriteSequencerRegister("reg0", numOfWrites);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    string _StringPair = dutSlavePairIndex == 2 ? "RX_EFUSE_BYTE" : "TX_EFUSE_BYTE";
                    bool IsOtpReg = Get_Specific_Key(registerAddress_hex, _StringPair).Count() == 0 ? false : true;

                    string Labelname = nameInMemory;
                    if (IsOtpReg && (base.ReadTimeset == ReadTimeset.HALF)) Labelname = "OTP" + nameInMemory;
                    if (isShareBus) Labelname += "Pair" + dutSlavePairIndex;

                    DIGI.PatternControl.StartLabel = Labelname;

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                    // Get PassFail Results for site 0
                    passFail = DIGI.PatternControl.GetSitePassFail("");
                    Int64[] failureCount = sdata1Pin.GetFailCount();
                    NumBitErrors = (int)failureCount[0];
                    if (debug) Console.WriteLine("RegRead " + registerAddress_hex + " Bit Errors: " + NumBitErrors.ToString());

                    // Retrieve captured waveform
                    uint[][] capData = new uint[][] { };
                    //uint[][] capData2 = new uint[][] { };

                    //DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, 1, TimeSpan.FromSeconds(3), ref capData);
                    DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, 9 * numOfWrites, TimeSpan.FromSeconds(3), ref capData);

                    //// Remove the parity bit   // this is for serial capture KH
                    //capData[0][0] = (capData[0][0] >> 1) & 0xFF;

                    string returnval = "";
                    int RegisterData = 0;

                    for (int j = 0; j < numOfWrites; j++)
                    {
                        RegisterData = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            int _ArryIndex = j * 9 + i;
                            if ((capData[0][_ArryIndex] & 1 << (EqHSDIO.Num_Mipi_Bus - Current_MIPI_Bus)) != 0)  // MIPI bus data is represented by bit position. MIPI1 is bit 1: MIPI2 would be at Bit0. This is just masking and recording the correct bit (Bus) in data returned
                                RegisterData |= 1 << (7 - i);
                        }

                        if (returnval == "")
                            returnval = RegisterData.ToString("X"); // Convert captured data to hex string and return
                        else
                            returnval = returnval + ":" + RegisterData.ToString("X"); // Convert captured data to hex string and return
                    }

                    //string returnval = RegisterData.ToString();

                    if (debug) Console.WriteLine("ReadReg " + registerAddress_hex + ": " + returnval);

                    //Ivi.Driver.PrecisionTimeSpan compareStrobe;
                    //compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(.000001);

                    //tsNRZ.ConfigureCompareEdgesStrobe(allRffePins, compareStrobe);
                    //tsNRZ.ConfigureCompareEdgesStrobe(sclk1Pin, compareStrobe);

                    return returnval;
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Read Register for Address " + registerAddress_hex + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }
            }

            /// <summary>
            /// EEPROM Write not currently implemented
            /// </summary>
            /// <param name="dataWrite"></param>
            public override void EepromWrite(string dataWrite)
            {
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(dataWrite + '\0');

                if (byteArray.Length > 256)
                {
                    MessageBox.Show("Exceeded maximum data length of 255 characters,\nEEPROM will not be written.", "EEPROM", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                allEEPROMPins.SelectedFunction = SelectedFunction.Digital;
                allEEPROMPins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                for (int tryWrite = 0; tryWrite < 5; tryWrite++)
                {
                    for (ushort reg = 0; reg < byteArray.Length; reg++)
                    {
                        // Burst pattern & check for Pass/Fail
                        bool[] passFail = new bool[] { };

                        // Set EEPROM register write address and data
                        uint[] dataArray = new uint[512];
                        dataArray[0] = (byte)reg;
                        dataArray[1] = byteArray[reg];

                        // Configure to source data, register address is up to 8 bits
                        DIGI.SourceWaveforms.CreateSerial(I2CSDAChanName.ToUpper(), "SrcEEPROMWrite", SourceDataMapping.Broadcast, 8, BitOrder.MostSignificantBitFirst);
                        DIGI.SourceWaveforms.WriteBroadcast("SrcEEPROMWrite", dataArray);

                        // Burst Pattern
                        passFail = DIGI.PatternControl.BurstPattern("", "EEPROMWrite", true, new TimeSpan(0, 0, 10));
                    }

                    if (EepromRead() == dataWrite)
                    {
                        MessageBox.Show("Writing & readback successful:\n\n    " + dataWrite, "EEPROM");
                        return;
                    }
                }

                MessageBox.Show("Writing NOT successful!", "EEPROM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            /// <summary>
            /// EEPROM Read
            /// </summary>
            /// <returns></returns>
            public override string EepromRead()
            {
                try
                {
                    if (!eepromReadWriteEnabled)
                    {
                        //eepromReadWriteEnabled = this.SendVector("EEPROMEraseWriteEnable".ToLower());
                    }

                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allEEPROMPins.SelectedFunction = SelectedFunction.Digital;
                    allEEPROMPins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    string returnval = "";
                    for (ushort reg = 0; reg < 256; reg++)
                    {
                        // Burst pattern & check for Pass/Fail
                        bool[] passFail = new bool[] { };

                        // Set EEPROM register read address
                        uint[] dataArray = new uint[512];
                        dataArray[0] = reg;

                        // Configure to source data, register address is up to 8 bits
                        DIGI.SourceWaveforms.CreateSerial(I2CSDAChanName.ToUpper(), "SrcEEPROMRead", SourceDataMapping.Broadcast, 8, BitOrder.MostSignificantBitFirst);
                        DIGI.SourceWaveforms.WriteBroadcast("SrcEEPROMRead", dataArray);

                        // Configure to capture 8 bits
                        DIGI.CaptureWaveforms.CreateSerial(I2CSDAChanName.ToUpper(), "CapEEPROMRead", 8, BitOrder.MostSignificantBitFirst);

                        // Burst Pattern
                        passFail = DIGI.PatternControl.BurstPattern("", "EEPROMRead", true, new TimeSpan(0, 0, 10));

                        // Retreive captured waveform
                        uint[][] capData = new uint[][] { };
                        DIGI.CaptureWaveforms.Fetch("", "CapEEPROMRead", 1, new TimeSpan(0, 0, 0, 0, 100), ref capData);

                        // Convert captured data to hex string and return
                        if (capData[0][0] != 0)
                        {
                            returnval += (char)capData[0][0];
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (debug) Console.WriteLine("EEPROMReadReg: " + returnval);
                    return returnval;
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Read EEPROM Register.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }
            }

            private bool IsMaskedWrite(List<MipiSyntaxParser.ClsMIPIFrame> MipiCommands)
            {
                bool isMaskedWrite = false;

                foreach (MipiSyntaxParser.ClsMIPIFrame command in MipiCommands)
                {
                    if (command.IsMaskedWrite)
                    {
                        isMaskedWrite = true;
                        break;
                    }
                }

                return isMaskedWrite;
            }

            private uint[] GeneraterwaveformArry(ref int _numOfWrites, List<MipiSyntaxParser.ClsMIPIFrame> _MipiCommands, bool _EnableMaskedWrite, int _PairIndex = 0)
            {
                string data_hex = "";
                string registerAddress_hex = "";
                int numOfWrites = 0;
                // Source buffer must contain 512 elements, even if sourcing less
                uint[] dataArray = new uint[512];
                int dataArrayIndex = 0;
                int IndexStep = _EnableMaskedWrite ? 5 : 4;

                foreach (MipiSyntaxParser.ClsMIPIFrame command in _MipiCommands)
                {
                    if (_PairIndex != command.Pair && _PairIndex != 0) continue;

                    dutSlaveAddress = command.SlaveAddress_hex;
                    registerAddress_hex = command.Register_hex;
                    data_hex = command.Data_hex;

                    // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                    if (data_hex.Length % 2 == 1)
                        data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                    // Build extended write command data, setting read byte count and register address.
                    // Note, write byte count is 0 indexed.
                    uint cmdBytesWithParity;
                    bool extendedWrite = Convert.ToInt32(registerAddress_hex, 16) > 31;    // any register address > 5 bits requires extended read
                    int _CheckRegWriteValue = Convert.ToInt32(registerAddress_hex, 16);

                    if (extendedWrite == false && Eq.SharedConfiguration.DigitalOption.EnableWrite0 && (_CheckRegWriteValue == 0))
                    {
                        // Build non-exteded write command
                        cmdBytesWithParity = generateRFFECommand(data_hex, Command.REGWRITE0, dutSlaveAddress);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // final 9 bits

                        dataArrayIndex = dataArrayIndex + IndexStep; // set for next command
                        numOfWrites++;
                    }
                    else if (extendedWrite == false && Eq.SharedConfiguration.DigitalOption.EnableRegWrite && Eq.SharedConfiguration.DigitalOption.RegWriteFrames.Any(v => v == _CheckRegWriteValue)) // (!extendedWrite)
                    {
                        // Build non-exteded write command
                        cmdBytesWithParity = generateRFFECommand(registerAddress_hex, Command.REGISTERWRITE, dutSlaveAddress);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                        dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(data_hex, 16)); // final 9 bits

                        dataArrayIndex = dataArrayIndex + IndexStep; // set for next command
                        numOfWrites++;
                    }
                    else
                    {
                        if (!command.IsMaskedWrite)
                        {
                            cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.EXTENDEDREGISTERWRITE, dutSlaveAddress);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                            // Convert Hex Data string to bytes and add to data Array
                            dataArray[dataArrayIndex + 3] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                            dataArrayIndex = dataArrayIndex + IndexStep; // set for next command
                            numOfWrites++;
                        }
                        else
                        {
                            cmdBytesWithParity = generateRFFECommand(Convert.ToString(0, 16), Command.MASKEDREGISTERWRITE, dutSlaveAddress);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[dataArrayIndex + 0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[dataArrayIndex + 1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            dataArray[dataArrayIndex + 2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits

                            // Convert Hex Data string to bytes and add to data Array
                            dataArray[dataArrayIndex + 3] = 0x01; //calculateParity(0xff - Convert.ToUInt32(data_hex, 16)); // data mask 9 bits
                            dataArray[dataArrayIndex + 4] = calculateParity(Convert.ToUInt32(data_hex, 16)); // data 9 bits

                            dataArrayIndex = dataArrayIndex + 5; // set for next command
                            numOfWrites++;
                        }
                    }
                }
                _numOfWrites = numOfWrites;

                return dataArray;
            }

            #region Avago SJC Specific Helper Functions

            /// <summary>
            /// NI Internal Function:  Generate the requested RFFE command
            /// </summary>
            /// <param name="registerAddress_hex_or_ByteCount_or_Write0Data">For non-extended read / write, this is the register address.  For extended read / write, this is the number of bytes to read.</param>
            /// <param name="instruction">EXTENDEDREGISTERWRITE, EXTENDEDREGISTERREAD, REGISTERWRITE, or REGISTERREAD</param>
            /// <returns>The RFFE Command + Parity</returns>
            private uint generateRFFECommand(string registerAddress_hex_or_ByteCount_or_Write0Data, Command instruction, string _dutSlaveAddress = null)
            {
                int slaveAddress = (Convert.ToByte(_dutSlaveAddress ?? dutSlaveAddress, 16)) << 8;
                int commandFrame = 1 << 14;
                Byte regAddress = Convert.ToByte(registerAddress_hex_or_ByteCount_or_Write0Data, 16);

                Byte maxRange = 0, modifiedAddress = 0;

                switch (instruction)
                {
                    case Command.EXTENDEDREGISTERWRITE:
                        maxRange = 0x0F;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x00);
                        break;

                    case Command.EXTENDEDREGISTERREAD:
                        maxRange = 0x0F;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x20);
                        break;

                    case Command.REGISTERWRITE:
                        maxRange = 0x1F;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x40);
                        break;

                    case Command.REGISTERREAD:
                        maxRange = 0x1F;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x60);
                        break;

                    case Command.MASKEDREGISTERWRITE:
                        maxRange = 0x19;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x19);
                        break;

                    case Command.REGWRITE0:
                        maxRange = 0x7F;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x80);
                        break;

                    default:
                        maxRange = 0x0F;
                        modifiedAddress = regAddress;
                        break;
                }

                if (regAddress != (maxRange & regAddress))
                    throw new Exception("Address out of range for requested command");

                // combine command frame, slave address, and modifiedAddress which contains the command and register address
                uint cmd = calculateParity((uint)(slaveAddress | modifiedAddress));
                cmd = (uint)(commandFrame) | cmd;
                return cmd;
            }

            /// <summary>
            /// NI Internal Function: Computes and appends parity
            /// </summary>
            /// <param name="cmdWithoutParity"></param>
            /// <returns></returns>
            private uint calculateParity(uint cmdWithoutParity)
            {
                int x = (int)cmdWithoutParity;
                x ^= x >> 16;
                x ^= x >> 8;
                x ^= x >> 4;
                x &= 0x0F;
                bool parity = ((0x6996 >> x) & 1) != 0;
                return (uint)(cmdWithoutParity << 1 | Convert.ToByte(!parity));
            }

            #endregion Avago SJC Specific Helper Functions

            #region Avago SJC Specific Enums

            /// <summary>
            /// Used to specify which timeset is used for a specified pattern.
            /// Get the string representation using Timeset.MIPI.ToString("g");
            /// </summary>
            public enum Timeset
            {
                MIPI,
                MIPI_HALF,
                MIPI_SCLK_NRZ,
                MIPI_RZ,
                MIPI_RZ_HALF,
                MIPI_RZ_QUAD,
                MIPI_RZ_10MHZ,
                EEPROM,
                UNIO_EEPROM,
                TEMPSENSE,                               ////////migration
                MIPI_RFONOFF,
                MIPI_HALF_RFONOFF,
                MIPI_SCLK_NRZ_RFONOFF,
            };

            public enum TrigConfig
            {
                PXI_Backplane,
                Digital_Pin,
                Both,
                None
            }

            public enum PXI_Trig
            {
                PXI_Trig0,
                PXI_Trig1,
                PXI_Trig2,
                PXI_Trig3,
                PXI_Trig4,
                PXI_Trig5,
                PXI_Trig6,
                PXI_Trig7
            }

            /// <summary>
            /// NI Internal Enum:  Used to select which command for which to generate and RFFE packet
            /// </summary>
            private enum Command
            {
                EXTENDEDREGISTERWRITE,
                EXTENDEDREGISTERREAD,
                REGISTERWRITE,
                REGISTERREAD,
                MASKEDREGISTERWRITE,
                REGWRITE0
            };

            private System.Version version = new System.Version(1, 0, 1215, 1);

            #endregion Avago SJC Specific Enums

            public override void SendTRIGVectors()
            {
                try
                {
                    DIGI.PatternControl.WriteSequencerFlag("seqflag3", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }

            private TriggerLine _triggerOut;

            public override TriggerLine TriggerOut
            {
                get
                {
                    return _triggerOut;
                }
                set
                {
                    string niTrigLine = TranslateNiTriggerLine(value);
                    //DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 5));
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "PatternOpcodeEvent3", niTrigLine);
                    DIGI.PatternControl.Commit();
                    _triggerOut = value;
                }
            }
        }
    }
}