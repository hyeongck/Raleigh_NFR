using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ivi.Visa.Interop;
using System.Threading;

namespace EquipmentDrivers.PM
{
    #region "Enumeration Declaration"
    #endregion
    #region "Structure"
    #endregion

    public class cPM4417A
    {
        public static string ClassName = "PM4417A Class";
        private string IOAddress;
        private FormattedIO488 ioPS = new FormattedIO488();
        private static cGeneral.cGeneral common = new cGeneral.cGeneral();

        #region "Class Initialization"
        public cCommonFunction BasicCommand; // Basic Command for General Equipment (Must be Initialized)
        public cSystem System;
        public cSource Source;
        public cTrigger Trigger;
        public cInitiate Initiate;
        public cSense Sense;

        public void Init(FormattedIO488 IOInit)
        {
            BasicCommand = new cCommonFunction(IOInit);
            System = new cSystem(IOInit);
            Source = new cSource(IOInit);
            Trigger = new cTrigger(IOInit);
            Initiate = new cInitiate(IOInit);
            Sense = new cSense(IOInit);
        }
        #endregion

        #region "Conversion Functions"
        #endregion
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
                return ioPS;
            }
            set
            {
                ioPS = parseIO;
                Init(parseIO);
            }
        }
        public void OpenIO()
        {
            if (IOAddress.Length > 3)
            {
                try
                {
                    ResourceManager mgr = new ResourceManager();
                    ioPS.IO = (IMessage)mgr.Open(IOAddress, AccessMode.NO_LOCK, 2000, "");
                }
                catch (SystemException ex)
                {
                    common.DisplayError(ClassName, "Initialize IO Error", ex.Message);
                    ioPS.IO = null;
                    return;
                }
                Init(ioPS);
            }
        }
        public void CloseIO()
        {
            ioPS.IO.Close();
        }
        /// <summary>
        /// Driver Revision control
        /// </summary>
        /// <returns>Driver's Version</returns>
        public string Version()
        {
            string VersionStr;
            //VersionStr = "X.XX"       //Date (DD/MM/YYYY)  Person          Reason for Change?
            //                          //-----------------  ----------- ----------------------------------------------------------------------------------
            VersionStr = "0.01a";        //  13/02/2012       KKL             VISA Driver for MXG N5182A 

            //                          //-----------------  ----------- ----------------------------------------------------------------------------------
            return (ClassName + " Version = v" + VersionStr);
        }

        /// <summary>
        /// Initializing all Parameters
        /// </summary>
        /// <param name="IOInit"></param>
        /// 

        #region "Class Functional Codes"
        public class cSystem : cCommonFunction
        {
            public cSystem(FormattedIO488 parse)
                : base(parse)
            {
            }
            public void Preset()
            {
                SendCommand("SYST:PRES DEF");
                Thread.Sleep(2500);
                System.Operation_Complete();
            }
        }
        public class cSense : cCommonFunction
        {
            public cSense(FormattedIO488 parse)
                : base(parse)
            {
            }
            public void Set_Freq(int chNo, double freqHz)
            {
                SendCommand("SENS" + chNo + ":FREQ " + freqHz);
            }
            public void Set_Average_Point(int Point)
            {
                SendCommand("SENS:AVER:COUN " + Point);
            }
            public void Set_TimeGated_Period(double Time)
            {
                SendCommand("SENS:SWE:TIME " + Time);
            }
            public void Set_TimeGated_StartPoint(double Time)
            {
                SendCommand("SENS:SWE:OFFS:TIME " + Time);
            }
            public void Set_Offset(int chNo, double offsetdB)
            {
                SendCommand("CALC" + chNo + ":GAIN " + offsetdB);
            }
            public void OffsetEnable(int chNo, bool enable)
            {
                switch (enable)
                {
                    case true:
                        SendCommand("CALC" + chNo + ":GAIN:STAT ON");
                        break;
                    case false:
                        SendCommand("CALC" + chNo + ":GAIN:STAT OFF");
                        break;
                }
            }
        }
        public class cTrigger : cCommonFunction
        {
            public cTrigger(FormattedIO488 parse)
                : base(parse)
            {
            }
            public void Trig_Immediate()
            {
                SendCommand("TRIG:IMM");
            }
            public void Set_TrigSource(string mode)
            {
                switch (mode.ToUpper())
                {
                    case "IMM":
                        SendCommand("TRIG:SOUR IMM");
                        break;
                    case "BUS":
                        SendCommand("TRIG:SOUR BUS");
                        break;
                    case "EXT":
                        SendCommand("TRIG:SOUR EXT");
                        break;
                    case "HOLD":
                        SendCommand("TRIG:SOUR HOLD");
                        break;
                    default:
                        SendCommand("TRIG:SOUR IMM");
                        break;
                }
            }
            public void Delay_Auto(bool enable)
            {
                switch (enable)
                {
                    case true:
                        SendCommand("TRIG:DEL ON");
                        break;
                    case false:
                        SendCommand("TRIG:DEL OFF");
                        break;
                }
            }

        }
        public class cSource : cCommonFunction
        {
            public cSource(FormattedIO488 parse)
                : base(parse)
            {
            }
            public string Fetch(int chNo)
            {
                return (ReadCommand("FETC" + chNo + "?"));
            }
            public string Measure(int chNo)
            {
                return (ReadCommand("MEAS" + chNo + "?"));
            }
            public string Read(int chNo)
            {
                return (ReadCommand("READ" + chNo + "?"));
            }
        }
        public class cInitiate : cCommonFunction
        {
            public cInitiate(FormattedIO488 parse) : base(parse) { }
            public void Immediate()
            {
                SendCommand("INIT:IMM");
            }
            public void Init()
            {
                SendCommand("INIT");
                System.Operation_Complete();
            }
            public void Continuous(bool enable)
            {
                switch (enable)
                {
                    case true:
                        SendCommand("INIT:CONT ON");
                        break;
                    case false:
                        SendCommand("INIT:CONT OFF");
                        break;
                }
            }
        }
        #endregion
    }
}
