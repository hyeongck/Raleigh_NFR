using System;
using Ivi.Visa.Interop;
using System.Windows.Forms;

namespace LibEqmtDriver.SMU
{
    public class KeithleySMU : Base_SMU, iPowerSupply 
    {
        public override string VisaAlias { get; set; }
        public override string SerialNumber { get; set; }
        public override string ChanNumber { get; set; }
        public override string PinName { get; set; }
        public override byte Site { get; set; }
        public override string OptionString { get => Simulated ? $"Simulate=true, DriverSetup= Model=;" : string.Empty; }
        public override string ModelNumber { get; }


        public static string ClassName = "Keithly SMU Class";
        private FormattedIO488 myVisaKeithley = new FormattedIO488();
        public string IOAddress;

        /// <summary>
        /// Parsing Equpment Address
        /// </summary>
        public string Address
        {
            get
            {
                return IOAddress;
            }
            set
            {
                IOAddress = value;
            }
        }
        public FormattedIO488 parseIO
        {
            get
            {
                return myVisaKeithley;
            }
            set
            {
                myVisaKeithley = parseIO;
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    myVisaKeithley.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, OptionString);
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Class Name: " + ClassName + "\nParameters: OpenIO" + "\n\nErrorDesciption: \n"
                        + ex, "Error found in Class " + ClassName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    myVisaKeithley.IO = null;
                    return;
                }
            }
        }
        public KeithleySMU(string ioAddress)
        {
            Address = ioAddress;
            OpenIO();
        }
        ~KeithleySMU() { }

        #region iPowerSupply Members

        public override void Close()
        {
            if (myVisaKeithley.IO != null)
            {
                myVisaKeithley.IO.Close();
            }
        }

        public override void Init()
        {
            RESET();
        }

        public override void DcOn(string strSelection, ePSupply_Channel Channel)
        {
            try 
            {
                myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".source.output = smu" + Channel.ToString() + ".OUTPUT_ON");
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DcOn -> " + ex.Message);
            }
        }

        public override void DcOff(string strSelection, ePSupply_Channel Channel)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".source.output = smu" + Channel.ToString() + ".OUTPUT_OFF");
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DcOff -> " + ex.Message);
            }
        }

        public override void SetNPLC(string strSelection, ePSupply_Channel Channel, float val)
        {
            try
            {
                if ((val < 0.001) | (val > 25))
                {
                    throw new Exception("KeithleySMU: SetNPLC -> must in range: 0.001 < NPLC < 25");
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".measure.nplc = " + val.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: SetNPLC -> " + ex.Message);
            }
        }

        public override void SetVolt(string strSelection, ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".source.func = smu" + Channel.ToString() + ".OUTPUT_DCVOLTS");

                VSourceAutoRange((ePSupply_Channel)Channel, true);

                myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".source.limiti = " + iLimit.ToString());
                myVisaKeithley.IO.WriteString("smu" + Channel.ToString() + ".source.levelv = " + Volt.ToString());

            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: SetVolt -> " + ex.Message);
            }
        }

        public override float MeasI(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange)
        {
            try
            {
                myVisaKeithley.IO.WriteString("print(smu" + Channel.ToString() + ".measure.i())");
                return (float)Convert.ToDouble(myVisaKeithley.ReadString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: MeasI -> " + ex.Message);
            }
        }

        // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 
        public override float MeasITraceMode(string strSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int ReadDelay)
        {
			// need to modify
            try
            {
                myVisaKeithley.IO.WriteString("print(smu" + Channel.ToString() + ".measure.i())");
                return (float)Convert.ToDouble(myVisaKeithley.ReadString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: MeasI -> " + ex.Message);
            }
        }

        public override float MeasV(string strSelection, ePSupply_Channel Channel, ePSupply_VRange VRange)
        {
            try
            {
                myVisaKeithley.IO.WriteString("print(smu" + Channel.ToString() + ".measure.v())");
                return (float)Convert.ToDouble(myVisaKeithley.ReadString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: MeasV -> " + ex.Message);
            }
        }
        public override void CalSelfCalibrate(string strSelection, ePSupply_Channel Channel)
        {

        }
        public override double CheckDeviceTemperature(string strSelection, ePSupply_Channel Channel)
        {
            return 0;
        }

        #endregion iPowerSupply Members


        private void RESET()
        {
            try
            {

                myVisaKeithley.IO.WriteString("reset()" );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: RESET -> " + ex.Message);
            }
        }
        private void SentCmd(string strCmd)
        {
            try
            {
                myVisaKeithley.IO.WriteString(strCmd );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: SentCmd -> " + ex.Message);
            }
        }

        private void SetOutput(ePSupply_Channel val, bool _ON)
        {
            try
            {
                if (_ON)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.output = smu" + val.ToString() + ".OUTPUT_ON" );
                    
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.output = smu" + val.ToString() + ".OUTPUT_OFF" );
                }
            }
            catch(Exception ex)
            {
                throw new Exception("KeithleySMU: SetOutput -> " + ex.Message);
            }

        }
        private void VMeasAutoRange(ePSupply_Channel val, bool _on_off)
        {
            try
            {
                if (_on_off)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".measure.autorangeI = smu" + val.ToString() + "AUTORANGE_ON");
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".measure.autorangeI = smu" + val.ToString() + "AUTORANGE_OFF");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: VMeasAutoRange -> " + ex.Message);
            }
        }
        private void IMeasAutoRange(ePSupply_Channel val, bool _on_off)
        {
            try
            {
                if (_on_off)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".measure.autorangei = smu" + val.ToString() + ".AUTORANGE_ON");
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".measure.autorangei = smu" + val.ToString() + ".AUTORANGE_OFF");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: IMeasAutoRange -> " + ex.Message);
            }
        }
        private void VSourceAutoRange(ePSupply_Channel val, bool _on_off)
        {
            try
            {
                if (_on_off)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.autorangev = smu" + val.ToString() + ".AUTORANGE_ON");
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.autorangev = smu" + val.ToString() + ".AUTORANGE_OFF");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: VSourceAutoRange -> " + ex.Message);
            }
        }
        private void ISourceAutoRange(ePSupply_Channel val, bool _on_off)
        {
            try
            {
                if (_on_off)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.autorangeI = smu" + val.ToString() + "AUTORANGE_ON");
                }
                else
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.autorangeI = smu" + val.ToString() + "AUTORANGE_OFF");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: ISourceAutoRange -> " + ex.Message);
            }
        }
        private void VSourceSet(ePSupply_Channel val, double dblVoltage, double dblClampI, ePSupply_VRange _range)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.func = smu" + val.ToString() + ".OUTPUT_DCVOLTS" );
                if (_range != ePSupply_VRange._Auto)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.rangev = " + VRange_String(_range).ToString());
                }
                else
                {
                    VSourceAutoRange(val, true);
                }
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.limiti = " + dblClampI.ToString() );
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.levelv = " + dblVoltage.ToString() );
                
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: VSourceSet -> " + ex.Message);
            }
        }
        private void ISourceSet(ePSupply_Channel val, double dblAmps, double dblClampV, ePSupply_IRange _range)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.func = smu" + val.ToString() + ".OUTPUT_DCAMPS" );
                if (_range != ePSupply_IRange._Auto)
                {
                    myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.rangei = " + IRange_String(_range).ToString());
                }
                else
                {
                    ISourceAutoRange(val, true);
                }
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.limitv = " + dblClampV.ToString() );
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.leveli = " + dblAmps.ToString() );
                
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: ISourceSet -> " + ex.Message);
            }
        }
        private void VChangeLevel(ePSupply_Channel val, double dblVoltage)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.levelv = " + dblVoltage.ToString() );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: VChangeLevel -> " + ex.Message);
            }
        }
        private void IChangeLevel(ePSupply_Channel val, double dblAmps)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.leveli = " + dblAmps.ToString() );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: IChangeLevel -> " + ex.Message);
            }
        }
        private void ILimit(ePSupply_Channel val, double dblAmps)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.limiti = " + dblAmps.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: ILimit -> " + ex.Message);
            }
        }
        private void VLimit(ePSupply_Channel val, double dblVoltage)
        {
            try
            {
                myVisaKeithley.IO.WriteString("smu" + val.ToString() + ".source.limitv = " + dblVoltage.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: VLimit -> " + ex.Message);
            }
        }
       
        private void DisplayClear()
        {
            try
            {
                myVisaKeithley.IO.WriteString("display.clear()" );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DisplayClear -> " + ex.Message);
            }
        }
        private void DisplayVolt(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("display.smu" + val.ToString() + ".measure.func = display.MEASURE_DCVOLTS" );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DisplayVolt -> " + ex.Message);
            }

        }
        private void DisplayAmps(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("display.smu" + val.ToString() + ".measure.func = display.MEASURE_DCAMPS");
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DisplayAmps -> " + ex.Message);
            }

        }
        private void DisplayOhms(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("display.smu" + val.ToString() + ".measure.func = display.MEASURE_OHMS" );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DisplayOhms -> " + ex.Message);
            }

        }
        private void DisplayWatt(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("display.smu" + val.ToString() + ".measure.func = display.MEASURE_WATTS" );
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: DisplayWatt -> " + ex.Message);
            }

        }

        private double MeasWatt(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("print(smu" + val.ToString() + ".measure.p())" );
                return Convert.ToDouble(myVisaKeithley.ReadString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: MeasWatt -> " + ex.Message);
            }
        }
        private double MeasOhms(ePSupply_Channel val)
        {
            try
            {
                myVisaKeithley.IO.WriteString("print(smu" + val.ToString() + ".measure.r())" );
                return Convert.ToDouble(myVisaKeithley.ReadString());
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: MeasOhms -> " + ex.Message);
            }
        }
        private string ReadString(string strCmd)
        {
            try
            {
                myVisaKeithley.IO.WriteString("print(" + strCmd + ")" );
                return myVisaKeithley.ReadString();
            }
            catch (Exception ex)
            {
                throw new Exception("KeithleySMU: SentCmd -> " + ex.Message);
            }
        }

        private double VRange_String(ePSupply_VRange val)
        {
            if (val == ePSupply_VRange.Keith260x_100mV) return 100e-3;
            if (val == ePSupply_VRange.Keith260x_1V) return 1;
            if (val == ePSupply_VRange.Keith260x_40V) return 40;
            if (val == ePSupply_VRange.Keith260x_6V) return 6;
            if (val == ePSupply_VRange.Keith261x_200mV) return 200e-3;
            if (val == ePSupply_VRange.Keith261x_200V) return 200;
            if (val == ePSupply_VRange.Keith261x_20V) return 20;
            if (val == ePSupply_VRange.Keith261x_2V) return 2;
            if (val == ePSupply_VRange._Auto) return 999;
            else return 0;
        }
        private double IRange_String(ePSupply_IRange val)
        {
            if (val == ePSupply_IRange._260x_3A) return 3;
            if (val == ePSupply_IRange._261x_1_5A) return 1.5;
            if (val == ePSupply_IRange._261x_10A) return 10;
            if (val == ePSupply_IRange._all_100mA) return 100e-3;
            if (val == ePSupply_IRange._all_100nA) return 100e-9;
            if (val == ePSupply_IRange._all_100uA) return 100e-6;
            if (val == ePSupply_IRange._all_10mA) return 10e-3;
            if (val == ePSupply_IRange._all_10uA) return 10e-6;
            if (val == ePSupply_IRange._all_1A) return 1;
            if (val == ePSupply_IRange._all_1mA) return 1e-3;
            if (val == ePSupply_IRange._all_1uA) return 1e-6;
            if (val == ePSupply_IRange._Auto) return 999;
            else return 0;
        }

    }
}
