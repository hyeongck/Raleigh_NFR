using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using NationalInstruments.ModularInstruments.NIDigital;

namespace LibEqmtDriver.MIPI
{
    public struct s_assignMIPIpin
    {
        public string SclkPinName;
        public int SclkChanDIO;
        public string SdataPinName;
        public int SdataChanDIO;
        public string VioPinName;
        public int VioChanDIO;
    }

    public class NI_PXIe6570 : iMiPiCtrl
    {
        int slaveaddr, pairNo;
        int ret = 0;
        bool[] dataArray_Bool;
        string moduleAlias = Lib_Var.ºmyNI6570Address;

        // Initialize NI 6570 session; generate and load vector files of MIPI commands
        HSDIO.NI6570 myMipiCtrl;

        public s_assignMIPIpin[] copyMipiPinNames;
        
        LibEqmtDriver.Utility.HiPerfTimer HiTimer = new LibEqmtDriver.Utility.HiPerfTimer();

        public NI_PXIe6570(s_MIPI_PAIR[] mipiPairCfg)
        {
            copyMipiPinNames = new s_assignMIPIpin[mipiPairCfg.Length];

            for (int i = 0; i < mipiPairCfg.Length; i++)
            {
                copyMipiPinNames[i].SclkPinName = "SCLKP" + mipiPairCfg[i].PAIRNO;
                copyMipiPinNames[i].SdataPinName = "SDATAP" + mipiPairCfg[i].PAIRNO;
                copyMipiPinNames[i].VioPinName = "VIOP" + mipiPairCfg[i].PAIRNO;

                copyMipiPinNames[i].SclkChanDIO = Int32.Parse(mipiPairCfg[i].SCLK);
                copyMipiPinNames[i].SdataChanDIO = Int32.Parse(mipiPairCfg[i].SDATA);
                copyMipiPinNames[i].VioChanDIO = Int32.Parse(mipiPairCfg[i].SVIO);
            }

            myMipiCtrl = new HSDIO.NI6570(moduleAlias, true, copyMipiPinNames);
        }

        #region iMipiCtrl interface
        void iMiPiCtrl.Init(s_MIPI_PAIR[] mipiPairCfg)
        {
            //not use
        }
        void iMiPiCtrl.TurnOn_VIO(int pair)
        {
            if (Lib_Var.b_setNIVIO)
            {
                myMipiCtrl.MipiReset();
                Lib_Var.b_setNIVIO = false;
            }
        }
        void iMiPiCtrl.TurnOff_VIO(int pair)
        {
            myMipiCtrl.MipiHiZ();
        }
        void iMiPiCtrl.SendAndReadMIPICodes(out bool ReadSuccessful, int Mipi_Reg)
        {
            //This function is for fixed MIPI Pair and Slave address
            pairNo = 0;                             //default using MIPI pair no 0 (fixed - hardcoded)
            slaveaddr = Lib_Var.ºSlaveAddress;      //default setting from config file

            ReadSuccessful = Register_Change(Mipi_Reg);
        }
        void iMiPiCtrl.SendAndReadMIPICodesRev2(out bool ReadSuccessful, int Mipi_Reg, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            ReadSuccessful = Register_Change(Mipi_Reg);
        }
        void iMiPiCtrl.SendAndReadMIPICodesCustom(out bool ReadSuccessful, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            ReadSuccessful = Register_Change_Custom(MipiRegMap, TrigRegMap);
        }
        void iMiPiCtrl.ReadMIPICodesCustom(out int Result, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            string tmpRslt = "";
            Result = 0;
            string[] tmpData = MipiRegMap.Split(':');

            ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));
            ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));       //need 2nd read before NI return correctly
            Result = int.Parse(tmpRslt, System.Globalization.NumberStyles.HexNumber);               //convert HEX to INT
        }
        void iMiPiCtrl.WriteMIPICodesCustom(string MipiRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            WriteRegister_Single(MipiRegMap);
        }
        void iMiPiCtrl.WriteOTPRegister(string efuseCtlReg_hex, string data_hex, int pair, int slvaddr, bool invertData = false)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller
            string mipiRegMap = efuseCtlReg_hex + ":" + data_hex;   //construct mipiRegMap to this format only example "C0:9E" - where C0 is efuseCtrl Address , 9E is efuseData to burn

            WriteRegister_Single(mipiRegMap);
        }
        void iMiPiCtrl.SetMeasureMIPIcurrent(int delayMs, int pair, int slvaddr, s_MIPI_DCSet[] setDC_Mipi, string[] measDC_MipiCh, out s_MIPI_DCMeas[] measDC_Mipi)
        {
            //Initialize variable
            s_MIPI_DCMeas[] tmpMeasDC_Mipi = new s_MIPI_DCMeas[measDC_MipiCh.Length];

            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            //DIO Ch alias name -> must be same as we define earlier during init instrument
            string tmpMipiPinNames = null;

            #region PPMU function - Force and Measure V/I
            #region Force PPMU
            for (int i = 0; i < setDC_Mipi.Length; i++)
            {
                switch (setDC_Mipi[i].ChNo)
                {
                    case 0:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].SclkPinName;
                        break;
                    case 1:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].SdataPinName;
                        break;
                    case 2:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].VioPinName;
                        break;
                }

                myMipiCtrl.ForceVoltage(tmpMipiPinNames, setDC_Mipi[i].VChSet, setDC_Mipi[i].IChSet);  // Force voltage on two pins
            }
            #endregion

            HiTimer.wait(Convert.ToDouble(delayMs));

            #region Measure PPMU
            for (int i = 0; i < measDC_MipiCh.Length; i++)
            {
                switch (Convert.ToInt16(measDC_MipiCh[i]))
                {
                    case 0:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].SclkPinName;
                        break;
                    case 1:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].SdataPinName;
                        break;
                    case 2:
                        tmpMipiPinNames = copyMipiPinNames[pairNo].VioPinName;
                        break;
                }

                double meas = 0;
                myMipiCtrl.MeasureCurrent(tmpMipiPinNames, 10, ref meas);  // Measurement always return result of one pin,
                tmpMeasDC_Mipi[i].IChMeas = (float)meas;
                tmpMeasDC_Mipi[i].MipiPinNames = tmpMipiPinNames;
            }
            #endregion
            #endregion

            //return result
            measDC_Mipi = tmpMeasDC_Mipi;
        }

        #endregion

        #region Init function
        public void INITIALIZATION()
        {
            //not use
        }
        #endregion

        #region small apps
        private int decodetohexvalue(int raw)
        {
            int result = 0;
            int[] tempdata = new int[2];
            tempdata[0] = (raw & 0xff00) >> 8;
            tempdata[1] = raw & 0xff;

            //raw to hex table 
            result = ((rawtohex(tempdata[0])) << 4) | rawtohex(tempdata[1]);

            return result;
        }
        private int rawtohex(int rawbyte)
        {
            int result = 0;
            switch (rawbyte)
            {
                case 0x00:
                    result = 0x00;
                    break;
                case 0x01:
                    result = 0x01;
                    break;
                case 0x04:
                    result = 0x02;
                    break;
                case 0x05:
                    result = 0x03;
                    break;
                case 0x10:
                    result = 0x04;
                    break;
                case 0x11:
                    result = 0x05;
                    break;
                case 0x14:
                    result = 0x06;
                    break;
                case 0x15:
                    result = 0x07;
                    break;
                case 0x40:
                    result = 0x08;
                    break;
                case 0x41:
                    result = 0x09;
                    break;
                case 0x44:
                    result = 0x0A;
                    break;
                case 0x45:
                    result = 0x0B;
                    break;
                case 0x50:
                    result = 0x0C;
                    break;
                case 0x51:
                    result = 0x0D;
                    break;
                case 0x54:
                    result = 0x0E;
                    break;
                case 0x55:
                    result = 0x0F;
                    break;
                default:
                    result = -1;
                    break;
            }
            return result;
        }
        public static string ToHex(int value)
        {
            return String.Format("0x{0:X}", value);
        }
        public static int FromHex(string value)
        {
            // strip the leading 0x
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(2);
            }
            return Int32.Parse(value, System.Globalization.NumberStyles.HexNumber);
        }
        #endregion

        #region test mipi
        public void TRIG()
        {
            //Mipi_Write(pairNo, slaveaddr, 0x1c, 0x03);
            myMipiCtrl.RegWrite(pairNo, ToHex(slaveaddr), ToHex(Lib_Var.ºPMTrig), ToHex(Lib_Var.ºPMTrig_Data));
        }
        public bool Register_Change(int Mipi_Reg)
        {
            int limit = 0;
            int[] MIPI_arr = new int[Mipi_Reg];
            bool readSuccessful = false;
            bool[] T_ReadSuccessful = new bool[Mipi_Reg];
            string[] regX_value = new string[Mipi_Reg];
            string[] MIPI_RegCond = new string[Mipi_Reg];
            int i;
            int reg_Cnt;
            int PassRd, FailRd;
            string result = "";

            //Initialize variable
            i = 0; reg_Cnt = 0;
            for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
            {
                switch (reg_Cnt)
                {
                    case 0:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg0;
                        break;
                    case 1:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg1;
                        break;
                    case 2:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg2;
                        break;
                    case 3:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg3;
                        break;
                    case 4:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg4;
                        break;
                    case 5:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg5;
                        break;
                    case 6:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg6;
                        break;
                    case 7:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg7;
                        break;
                    case 8:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg8;
                        break;
                    case 9:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_Reg9;
                        break;
                    case 10:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegA;
                        break;
                    case 11:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegB;
                        break;
                    case 12:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegC;
                        break;
                    case 13:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegD;
                        break;
                    case 14:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegE;
                        break;
                    case 15:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.ºMIPI_RegF;
                        break;
                    default:
                        MessageBox.Show("Total Register Number : " + Mipi_Reg + " not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                        break;
                }
            }

            while (true)
            {
                reg_Cnt = 0; PassRd = 0; FailRd = 0; //reset read success counter

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    if (MIPI_RegCond[reg_Cnt].ToUpper() != "X")
                        myMipiCtrl.RegWrite(pairNo, ToHex(slaveaddr), MIPI_RegCond[reg_Cnt], ToHex(reg_Cnt));
                }

                TRIG();

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    T_ReadSuccessful[reg_Cnt] = true;
                    regX_value[reg_Cnt] = "";

                    if (MIPI_RegCond[reg_Cnt].ToUpper() != "X")
                    {
                        regX_value[reg_Cnt] = myMipiCtrl.RegRead(pairNo, ToHex(slaveaddr), ToHex(reg_Cnt));
                    }
                    else
                    {
                        regX_value[reg_Cnt] = MIPI_RegCond[reg_Cnt];
                    }

                    if (MIPI_RegCond[reg_Cnt] != regX_value[reg_Cnt] && LibEqmtDriver.MIPI.Lib_Var.ºReadFunction == true)
                        T_ReadSuccessful[reg_Cnt] = false;
                    else
                        T_ReadSuccessful[reg_Cnt] = true;
                }

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    if (T_ReadSuccessful[reg_Cnt] == true)
                        PassRd++;
                    else
                        FailRd++;
                }

                if (PassRd == (Mipi_Reg))
                {
                    readSuccessful = true;
                    break;
                }
                else
                    readSuccessful = false;

                limit = limit + 1;


                if (limit > 10) break;
            }
            return readSuccessful;
        }
        public bool Register_Change_Custom(string _cmd, string _cmdTrig)
        {
            //_cmd must be in this format -> "01:01 02:00 05:00 06:40 07:00 08:00 09:00 1C:01 1C:02 1C:03 1C:04 1C:05 1C:06 1C:07"
            // ext_reg use when your register address is above 1F (5 bit)
            bool dummyRslt;
            bool result;
            int limit = 0;

            // check data length , if more that 1 data use WriteReadRegister_Multi , else use WriteRegister_Single
            bool multiData = false;
            string[] biasDataArr = _cmd.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            if (biasDataArr.Length > 1)
                multiData = true;
            bool multiDataTrig = false;
            string[] trigDataArr = _cmd.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            if (trigDataArr.Length > 1)
                multiDataTrig = true;

            while (true)
            {
                result = false;
                if (!multiData)
                {
                    WriteRegister_Single(_cmd);
                    ReadRegister_Rev2(_cmd, out result);
                }
                else
                {
                    ReadWrite_Register(_cmd, out result);
                }

                if (_cmdTrig.ToUpper() != "NONE")
                {
                    if (!multiDataTrig)
                    {
                        WriteRegister_Single(_cmdTrig);      //write PM Trigger
                    }
                    else
                    {
                        ReadWrite_Register(_cmdTrig, out dummyRslt);
                    }
                }

                if (result)
                    break;      //exit loop when result = true

                limit = limit + 1;
                if (limit > 10) break;      //allow 10 try before exit
            }
            return result;
        }
        public void ReadRegister_Rev2(string _cmd, out bool readSuccessful)
        {
            int reg_Cnt;
            int PassRd, FailRd;

            //Initialize variable
            reg_Cnt = 0; PassRd = 0; FailRd = 0;
            string result = "";
            readSuccessful = false;

            string biasData = _cmd;
            string[] biasDataArr = biasData.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            bool[] T_ReadSuccessful = new bool[biasDataArr.Length];
            string[] regX_value = new string[biasDataArr.Length];

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                T_ReadSuccessful[i] = true;
                regX_value[i] = "";

                string[] tmpData = biasDataArr[i].Split(':');
                string mipi_RegCond = tmpData[1];

                ReadRegister_Single(ref result, Convert.ToInt32(tmpData[0], 16));
                regX_value[i] = result;

                if (mipi_RegCond != regX_value[i] && LibEqmtDriver.MIPI.Lib_Var.ºReadFunction == true)
                    T_ReadSuccessful[i] = false;
                else
                    T_ReadSuccessful[i] = true;
            }

            for (reg_Cnt = 0; reg_Cnt < biasDataArr.Length; reg_Cnt++)
            {
                if (T_ReadSuccessful[reg_Cnt] == true)
                    PassRd++;
                else
                    FailRd++;
            }

            if (PassRd == (biasDataArr.Length))
                readSuccessful = true;
            else
                readSuccessful = false;
        }
        public void ReadWrite_Register(string _cmd, out bool readSuccessful)
        {
            string returnData = "";
            string givenData = "";
            char[] delimiters = { ' ' };
            readSuccessful = false;
            string biasData = _cmd;

            WriteReadRegister_Multi(biasData, out returnData);

            // Verify returnData with givenData
            string[] regdata = biasData.Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries);
            givenData = string.Join(",", (from r in regdata select r.Substring(3, 2)).ToArray());

            if (returnData == givenData)
                readSuccessful = true;
            else
                readSuccessful = false;
        }
        public string ReadRegister_Single(ref string x, int RegAddr)
        {
            string regX_value = null;
            string tempresult = "";

            //read
            regX_value = myMipiCtrl.RegRead(pairNo, ToHex(slaveaddr), ToHex(RegAddr));

            // F -> 0F
            //Note : x return always 2 digit >>> eg 0A , 01, 00, 15 etc ...
            if (FromHex(regX_value) <= 15)
            {
                tempresult = "0" + regX_value;
            }
            else
            {
                tempresult = regX_value;
            }

            x = tempresult;
            return x;
        }
        public void WriteRegister_Single(string _cmd)
        {
            string biasData = _cmd;
            string[] biasDataArr = biasData.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            for (int i = 0; i < biasDataArr.Length; i++)
            {
                string[] tmpData = biasDataArr[i].Split(':');
                myMipiCtrl.RegWrite(pairNo, ToHex(slaveaddr), tmpData[0], tmpData[1], false);
            }
        }
        public void WriteReadRegister_Multi(string _cmd, out string dataOut)
        {
            dataOut = myMipiCtrl.RegWriteReadAny(pairNo, ToHex(slaveaddr), _cmd, false);
        }
        #endregion
    }

    public static class HSDIO
    {
        public static Dictionary<string, string> PinNamesAndChans = new Dictionary<string, string>();

        public static bool usingMIPI = false;
        public const string Reset = "RESET", HiZ = "HiZ", RegIO = "regIO";
        public const string TrigPinName = "TRIG";

        public struct MipiPinNames
        {
            public string SclkPinName;
            public int SclkChanDIO;

            public string SdataPinName;
            public int SdataChanDIO;

            public string VioPinName;
            public int VioChanDIO;
        };

        //disable - Shaz (note : use config file to pass in the pin setting)
        // To add pair, write one more line of MipiPinNames Definition here. Please make sure all the pin names are unique 
        // To define pin and channel mapping, go to region "Start Index of PhysicalPinsDefinition" 
        //public static MipiPinNames[] allMipiPinNames =  {
        //        new MipiPinNames { SclkPinName = "SCLKP0", SdataPinName= "SDATAP0", VioPinName = "VIOP0", SclkChanDIO = 0, SdataChanDIO = 1, VioChanDIO = 2},
        //        new MipiPinNames { SclkPinName = "SCLKP1", SdataPinName = "SDATAP1", VioPinName = "VIOP1", SclkChanDIO = 6, SdataChanDIO = 4, VioChanDIO = 8} };
        ////new MipiPinNames { SclkPinName = "SCLKP0", SdataPinName= "SDATAP0", VioPinName = "VIOP0", SclkChanDIO = 10, SdataChanDIO = 11, VioChanDIO = 12},
        ////new MipiPinNames { SclkPinName = "SCLKP1", SdataPinName = "SDATAP1", VioPinName = "VIOP1", SclkChanDIO = 13, SdataChanDIO = 14, VioChanDIO = 15} };

        public static MipiPinNames[] allMipiPinNames;   //add by Shaz

        public static bool useScript = false;
        public static Dictionary<string, bool> datalogResults = new Dictionary<string, bool>();
        public static iHsdioInstrument Instrument;
        public static string tmpVisaAlias;
        public static Dictionary<string, string> ConfigRegisterSettings = new Dictionary<string, string>();

        public interface iHsdioInstrument
        {
            bool LoadVector(List<string> fullPaths, string nameInMemory, bool datalogResults);
            bool LoadVector_MipiHiZ();
            bool LoadVector_MipiReset();
            bool LoadVector_MipiRegIO();


            void ConfigureSetting(bool script, string TestMode);
            void SendTRIGVectors();

            int GetNumExecErrors(string nameInMemory);
            void RegWrite(int pair, string slaveAddress_hex, string registerAddress_hex, string data_hex, bool sendTrigger = false);
            string RegRead(int pair, string slaveAddress_hex, string registerAddress_hex);
            void Close();
        }

        public class NI6570 : iHsdioInstrument
        {
            /*
             * Notes:  Requires the following References added to project (set Copy Local = false):
             *   - NationalInstruments.ModularInstruments.NIDigital.Fx40
             *   - Ivi.Driver
             */

            // The Instrument Session
            public static NIDigital DIGI;

            #region Private Variables
            private string allRffeChans;
            private DigitalPinSet allRffePins, sdataPins, sclkPins, vioPins, trigPin;
            private string[] allDutPins = new string[] { };
            private double pidval;  // Stores the acquired PID value after executing the PID pattern.
            private List<string> loadedPatternFiles; // used to store previously loaded patterns so we don't try and double load.  Double Loading will cause an error, so always check this list to see if pattern was previously loaded.
            private double MIPIClockRate;  // MIPI NRZ Clock Rate (2 x Vector Rate)            
            private double StrobePoint;
            private bool forceDigiPatRegeneration = false; //false;  // Set this to true if you want to re-generate all .digipat files from the .vec files, even if the .vec files haven't changed.
            private int NumExecErrors; // Stores the number of bit errors from the most recently executed pattern.
            private Dictionary<string, uint> captureCounters = new Dictionary<string, uint>(); // This dictionary stores the # of captures each .vec contains (for .vec files that are converted to capture format)
            private string fileDir; // This is the path used to store intermediate digipatsrc, digipat, and other files.
            private TrigConfig triggerConfig = TrigConfig.None;  // No Triggering by default
            private PXI_Trig pxiTrigger = PXI_Trig.PXI_Trig7;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG7 shouldn't interfere.
            private uint regWriteTriggerCycleDelay = 0;

            private bool debug = true; // Turns on additional console messages if true




            #endregion

            /// <summary>
            /// Initialize the NI 6570 Instrument:
            ///   - Open Instrument session
            ///   - Reset Instrument and Unload All Patterns from Instrument Memory
            ///   - Configure Pin -> Channel Mapping
            ///   - Configure Timing for: MIPI (6556 style NRZ) & MIPI_SCLK_RZ (6570 style RZ)
            ///   - Configure 6570 in Digital mode by default (instead of PPMU mode)
            /// </summary>
            /// <param name="visaAlias">The VISA Alias of the instrument, typically NI6570.</param>            
            public NI6570(string visaAlias, bool AutoSubCal, s_assignMIPIpin[] mipiCfg)
            {
                //assign mipi pin - shaz 
                allMipiPinNames = new MipiPinNames[mipiCfg.Length];
                for (int cnt = 0; cnt < mipiCfg.Length; cnt++)
                {
                    allMipiPinNames[cnt].SclkPinName = mipiCfg[cnt].SclkPinName;
                    allMipiPinNames[cnt].SdataPinName = mipiCfg[cnt].SdataPinName;
                    allMipiPinNames[cnt].VioPinName = mipiCfg[cnt].VioPinName;

                    allMipiPinNames[cnt].SclkChanDIO = mipiCfg[cnt].SclkChanDIO;
                    allMipiPinNames[cnt].SdataChanDIO = mipiCfg[cnt].SdataChanDIO;
                    allMipiPinNames[cnt].VioChanDIO = mipiCfg[cnt].VioChanDIO;
                }

                // Clock Rate & Cable Delay
                MIPIClockRate = 26e6;//26e6;  // This is the Non-Return to Zero rate, Actual Vector rate is 1/2 of this.                

                // Set these values based on calling ((HSDIO.NI6570)HSDIO.Instrument).shmoo("QC_Test");
                // Ideally, try to set UserDelay = 0 if possible and only modify StrobePoint.
                StrobePoint = 120E-9; // 69 changed to 65 for Max-Q
                regWriteTriggerCycleDelay = 0;

                // Trigger Configuration;  This applies to the RegWrite command and will send out a hardware trigger
                // on the specified triggers (Digital Pin, PXI Backplane, or Both) at the end of the Register Write operation.
                triggerConfig = TrigConfig.Digital_Pin;
                pxiTrigger = PXI_Trig.PXI_Trig7;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG7 shouldn't interfere.               

                #region Initialize Private Variables
                fileDir = Path.GetTempPath() + "NI.Temp\\NI6570";
                Directory.CreateDirectory(fileDir);

                loadedPatternFiles = new List<string> { };

                #endregion

                // Initialize Instrument                
                DIGI = new NIDigital(visaAlias, false, true);


                #region NI Pin Map Configuration
                // Make sure you add all needed pins here so that they get auto-added to all NI-6570 digipat files.  If they aren't in allDutPins or allSystemPins, you can't use them.                

                #region Start Index of PhysicalPinsDefinition
                // Define pin and channel mapping here
                int i = 10;  // first index of Channel number - First MIPI pair sclk                
                foreach (MipiPinNames mipichans in allMipiPinNames)
                {
                    PinNamesAndChans[mipichans.SclkPinName] = mipichans.SclkChanDIO.ToString();
                    i++;
                    PinNamesAndChans[mipichans.SdataPinName] = mipichans.SdataChanDIO.ToString();
                    i++;
                    PinNamesAndChans[mipichans.VioPinName] = mipichans.VioChanDIO.ToString();
                    i++;
                }
                #endregion

                // Map extra pins 
                PinNamesAndChans[TrigPinName] = i.ToString();

                this.allDutPins = PinNamesAndChans.Keys.ToArray();

                allRffeChans = string.Join(", ", allDutPins);

                string allSclkPinNames = string.Join(", ", from m in allMipiPinNames select new { SclkChanName = m.SclkPinName }.SclkChanName);
                string allSdataPinNames = string.Join(", ", from m in allMipiPinNames select new { SdataChanName = m.SdataPinName }.SdataChanName);
                string allVioPinNames = string.Join(", ", from m in allMipiPinNames select new { VioChanName = m.VioPinName }.VioChanName);

                // Configure 6570 Pin Map with all pins
                DIGI.PinAndChannelMap.CreatePinMap(allDutPins, null);
                DIGI.PinAndChannelMap.CreateChannelMap(1);
                foreach (string pin in allDutPins)
                    DIGI.PinAndChannelMap.MapPinToChannel(pin, 0, PinNamesAndChans[pin]);

                DIGI.PinAndChannelMap.EndChannelMap();

                // Get DigitalPinSets
                allRffePins = DIGI.PinAndChannelMap.GetPinSet(allRffeChans);
                sclkPins = DIGI.PinAndChannelMap.GetPinSet(allSclkPinNames);
                sdataPins = DIGI.PinAndChannelMap.GetPinSet(allSdataPinNames);
                vioPins = DIGI.PinAndChannelMap.GetPinSet(allVioPinNames);
                trigPin = DIGI.PinAndChannelMap.GetPinSet(TrigPinName);

                #endregion

                #region MIPI Level Configuration
                double vih = 1.8;
                double vil = 0.0;
                double voh = 0.9;
                double vol = 0.8;
                double vtt = 3.0;

                sclkPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                sdataPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                vioPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                trigPin.DigitalLevels.ConfigureVoltageLevels(0.0, 5.0, 0.5, 2.5, 5.0); // Set VST Trigger Channel to 5V logic.  VST's PFI0 VIH is 2.0V, absolute max is 5.5V
                #endregion

                #region Timing Variable Declarations
                // Variables
                double period_dbl;
                Ivi.Driver.PrecisionTimeSpan period;
                Ivi.Driver.PrecisionTimeSpan driveOn;
                Ivi.Driver.PrecisionTimeSpan driveData;
                Ivi.Driver.PrecisionTimeSpan driveReturn;
                Ivi.Driver.PrecisionTimeSpan driveOff;
                Ivi.Driver.PrecisionTimeSpan compareStrobe;
                Ivi.Driver.PrecisionTimeSpan clockRisingEdgeDelay;
                Ivi.Driver.PrecisionTimeSpan clockFallingEdgeDelay;
                #endregion

                #region MIPI Timing Configuration
                #region Timing configuration for Return to Zero format Patterns.
                // All RegRead / RegWrite functions use the RZ format for SCLK

                // Vector Rate is 1/2 Clock Toggle Rate.
                // Compute timing values, shift all clocks out by 2 x periods so we can adjust the strobe "backwards" if needed.
                period_dbl = 1.0 / (MIPIClockRate / 2.0);
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.5 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(3.0 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * StrobePoint);

                clockRisingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl / 8.0);  // This is the amount of time after SDATA is set to high or low before SCLK is set high.
                // By setting this > 0, this will slightly delay the SCLK rising edge which can help ensure
                // SDATA is settled before clocking in the value at the DUT.
                // Note: This does not shift the Falling Edge of SCLK.  This means that adjusting this value will
                //  reduce the overall duty cycle of SCLK.  You must adjuct clockFallingEdgeDelay by the same amount
                //  if you would like to maintain a 50% duty cycle.
                clockFallingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);

                // Create Timeset
                DigitalTimeSet tsRZ = DIGI.Timing.CreateTimeSet(Timeset.MIPI_SCLK_RZ.ToString("g"));
                tsRZ.ConfigurePeriod(period);

                // Vio, Sdata, Trig
                tsRZ.ConfigureDriveEdges(vioPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsRZ.ConfigureCompareEdgesStrobe(vioPins, compareStrobe);
                tsRZ.ConfigureDriveEdges(sdataPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsRZ.ConfigureCompareEdgesStrobe(sdataPins, compareStrobe);
                tsRZ.ConfigureDriveEdges(trigPin, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsRZ.ConfigureCompareEdgesStrobe(trigPin, compareStrobe);
                // Sclk
                tsRZ.ConfigureDriveEdges(sclkPins, DriveFormat.ReturnToLow, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsRZ.ConfigureCompareEdgesStrobe(sclkPins, compareStrobe);
                #endregion

                #region Timing configuration for Non Return to Zero format Patterns (eg: 6556 style).
                // Standard .vec files use the Non Return to Zero Format

                //Actual Vector Rate is still 1/2 Clock Toggle Rate.
                // Compute timing values, shift all clocks out by 2 x periods so we can adjust the strobe "backwards" if needed.
                period_dbl = 1.0 / MIPIClockRate;
                period = Ivi.Driver.PrecisionTimeSpan.FromSeconds(period_dbl);
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.0 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(2.5 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(3.0 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(StrobePoint);

                clockRisingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0); //period / 8;  // This is the amount of time after SDATA is set to high or low before SCLK is set high.
                // By setting this > 0, this will slightly delay the SCLK rising edge which can help ensure
                // SDATA is settled before clocking in the value at the DUT.
                // Note: This does not shift the Falling Edge of SCLK.  This means that adjusting this value will
                //  reduce the overall duty cycle of SCLK.  You must adjuct clockFallingEdgeDelay by the same amount
                //  if you would like to maintain a 50% duty cycle.
                clockFallingEdgeDelay = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0);

                // Create Timeset
                DigitalTimeSet tsNRZ = DIGI.Timing.CreateTimeSet(Timeset.MIPI.ToString("g"));
                tsNRZ.ConfigurePeriod(period);


                // Vio, Sdata, Trig
                tsNRZ.ConfigureDriveEdges(vioPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsNRZ.ConfigureCompareEdgesStrobe(vioPins, compareStrobe);
                tsNRZ.ConfigureDriveEdges(sdataPins, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsNRZ.ConfigureCompareEdgesStrobe(sdataPins, compareStrobe);
                tsNRZ.ConfigureDriveEdges(trigPin, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);
                tsNRZ.ConfigureCompareEdgesStrobe(trigPin, compareStrobe);
                // Sclk
                tsNRZ.ConfigureDriveEdges(sclkPins, DriveFormat.NonReturn, driveOn + clockRisingEdgeDelay, driveData + clockRisingEdgeDelay, driveReturn + clockFallingEdgeDelay, driveOff + clockFallingEdgeDelay);
                tsNRZ.ConfigureCompareEdgesStrobe(sclkPins, compareStrobe);
                #endregion
                #endregion

                #region Configure 6570 for Digital Mode with HighZ Termination
                allRffePins.SelectedFunction = SelectedFunction.Digital;
                allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                #endregion

                #region Load Vectors for MIPI Register Write Read
                // Assert that the HSDIO is ready to load MIPI vectors
                usingMIPI = true;

                LoadVector_MipiRegIO();
                LoadVector_MipiHiZ();
                LoadVector_MipiReset();
                #endregion

            }



            /// <summary>
            /// Load the specified vector file into Instrument Memory.
            /// Will automatically convert from .vec format as needed and load into instrument memory.
            /// </summary>
            /// <param name="fullPaths">A list of absolute paths to be loaded.  Currenlty only supports 1 item in the list.</param>
            /// <param name="nameInMemory">The name by which to load and execute the pattern.</param>
            /// <param name="datalogResults">Specifies if the pattern's results should be added to the datalog</param>
            /// <returns>True if pattern load succeeds.</returns>
            public bool LoadVector(List<string> fullPaths, string nameInMemory, bool datalogResults)
            {

                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    nameInMemory = nameInMemory.Replace("_", "");   //CM Edited

                    HSDIO.datalogResults[nameInMemory] = datalogResults;
                    bool isDigipat = fullPaths[0].ToUpper().EndsWith(".DIGIPAT");
                    bool isVec = fullPaths[0].ToUpper().EndsWith(".VEC");
                    bool notLoaded = !loadedPatternFiles.Contains(fullPaths[0] + nameInMemory.ToLower());

                    // If this is a digipat file and it hasn't already been loaded into instrument memory, load it
                    if (isDigipat && notLoaded)
                    {
                        DIGI.LoadPattern(fullPaths[0]);
                        loadedPatternFiles.Add(fullPaths[0] + nameInMemory.ToLower());
                        return true;
                    }
                    else
                    {
                        throw new Exception("Unknown File Format for " + nameInMemory);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load vector file:\n" + fullPaths[0] + "\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Generate and load pattern for setting all pins (SCLK, SDATA, VIO) to HiZ mode
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            public bool LoadVector_MipiHiZ()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    List<string> pins = new List<string>();
                    List<string>[] patternvector = { new List<string>(), new List<string>() };

                    // Generate MIPI / RFFE Non-Extended Register Read Pattern
                    patternvector[0].Add("repeat(7)");
                    patternvector[1].Add("halt");



                    // Set VIO pins of all MIPI pairs to low and high
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        pins.Add(m.SclkPinName); pins.Add(m.SdataPinName); pins.Add(m.VioPinName);
                        patternvector[0].AddRange(new string[] { "0", "0", "0" });
                        patternvector[1].AddRange(new string[] { "0", "0", "0" });
                    }

                    // Set trigger pin don't care
                    pins.Add(HSDIO.TrigPinName);
                    patternvector[0].AddRange(new string[] { "X", "" });
                    patternvector[1].AddRange(new string[] { "X", "" });


                    int minorLength = patternvector[0].Count;
                    string[,] pattern = new string[patternvector.Length, minorLength];
                    for (int i = 0; i < patternvector.Length; i++)
                    {
                        var array = patternvector[i].ToArray();
                        if (array.Length != minorLength)
                        {
                            throw new ArgumentException
                                ("All arrays must be the same length");
                        }
                        for (int j = 0; j < minorLength; j++)
                        {
                            pattern[i, j] = array[j];
                        }
                    }

                    // Generate and load Pattern from the formatted array.
                    if (!this.GenerateAndLoadPattern(HiZ.ToLower(), pins.ToArray(), pattern, forceDigiPatRegeneration, Timeset.MIPI))
                    {
                        throw new Exception("Compile Failed");
                    }

                    HSDIO.datalogResults[HiZ] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi HiZ vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Generate and load pattern for sending MIPI Reset
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            public bool LoadVector_MipiReset()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Generate MIPI / RFFE Reset Waveform
                    // Set VIO Pin to HiZ (remove VIO Pin from DUT) for 1/2 of the specified number of seconds,
                    // then return VIO to DUT as 0;
                    double secondsReset = 0.002;
                    int numLines = (int)(MIPIClockRate * secondsReset);


                    List<string> pins = new List<string>();
                    List<string>[] patternvector = { new List<string>(), new List<string>(), new List<string>() };

                    // Generate MIPI / RFFE Non-Extended Register Read Pattern
                    patternvector[0].Add("repeat(" + ((MIPIClockRate * secondsReset) / 2) + ")");
                    patternvector[1].Add("repeat(" + ((MIPIClockRate * secondsReset) / 2) + ")");
                    patternvector[2].Add("halt");



                    // Set VIO pins of all MIPI pairs to low and high
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        pins.Add(m.SclkPinName); pins.Add(m.SdataPinName); pins.Add(m.VioPinName);
                        patternvector[0].AddRange(new string[] { "0", "0", "0" });
                        patternvector[1].AddRange(new string[] { "0", "0", "1" });
                        patternvector[2].AddRange(new string[] { "0", "0", "1" });
                    }


                    // Set trigger pin don't care
                    pins.Add(HSDIO.TrigPinName);
                    patternvector[0].AddRange(new string[] { "X", "" });
                    patternvector[1].AddRange(new string[] { "X", "" });
                    patternvector[2].AddRange(new string[] { "X", "" });

                    int minorLength = patternvector[0].Count;
                    string[,] pattern = new string[patternvector.Length, minorLength];
                    for (int i = 0; i < patternvector.Length; i++)
                    {
                        var array = patternvector[i].ToArray();
                        if (array.Length != minorLength)
                        {
                            throw new ArgumentException
                                ("All arrays must be the same length");
                        }
                        for (int j = 0; j < minorLength; j++)
                        {
                            pattern[i, j] = array[j];
                        }
                    }



                    // Generate and load Pattern from the formatted array.
                    if (!this.GenerateAndLoadPattern(Reset.ToLower(), allDutPins, pattern, forceDigiPatRegeneration, Timeset.MIPI))
                    {
                        throw new Exception("Compile Failed");
                    }

                    HSDIO.datalogResults[Reset] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi Reset vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Generate and load all patterns necessary for Register Read and Write (including extended R/W)
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            public bool LoadVector_MipiRegIO()
            {
                bool pass = true;
                pass = LoadVector_MipiRegWrite() & LoadVector_MipiRegRead() & LoadVector_MipiExtendedRegRead() & LoadVector_MipiExtendedRegWrite();
                return pass & LoadVector_MipiRegWriteRead() & LoadVector_MipiExtendedRegWriteRead();
            }

            /// <summary>
            /// Internal Function: Used to generate and load the non-extended register read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiRegRead()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0;
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        // Generate MIPI / RFFE Non-Extended Register Read Pattern
                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        List<string[]> pattern = new List<string[]>
                            {
                            #region RegisterRead pattern
                                new string[] {"source_start(SrcRegisterReadPair" + p.ToString () + ")", "0", "0", "1", "X", "Configure source"},
                                new string[] {"capture_start(CapRegisterReadPair"+ p.ToString() + ")", "0", "0", "1", "X", "Configure capture"},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"set_loop(reg0)","0", "0", "1", "X",""},                                
                                new string[] {"cmdstart:\nsource", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Pull Down Only"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 7"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 6"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 5"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 4"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 3"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 2"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 1"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 0"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Parity"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "X", "X", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterRead" + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                        {
                            throw new Exception("Compile Failed");
                        }

                        HSDIO.datalogResults["RegisterRead" + "Pair" + p.ToString()] = false;
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi RegisterRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the non-extended register write pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiRegWrite()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0; // Index of MIPI pair
                    // Generate MIPI / RFFE Non-Extended Register Write Pattern
                    foreach (MipiPinNames m in allMipiPinNames)
                    {

                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region RegisterWrite Pattern
                                new string[] {"source_start(SrcRegisterWritePair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"},
                                new string[] {"repeat(300)", "0", "0", "1", "0", "Idle"},
                                new string[] {"set_loop(reg0)","0", "0", "1", "0",""},                                
                                new string[] {"cmdstart:\nsource", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Register Write Command (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> triggerDelay = new List<string[]>
                            {
                                new string[] { "", "0", "0", "1", "0", "Trigger Delay Cycle" }
                            };
                        List<string[]> trigger = new List<string[]>
                            {
                            #region Trigger, Idle Halt
                                new string[] {"jump_if(!seqflag0, endofpattern)", "0", "0", "1", "0", "Check if Trigger Required, if not, go to halt."},
                                new string[] {"set_signal(event0)", "0", "0", "1", trigval, "Turn On PXI Backplane Trigger if enabled. Send Digital Pin Trigger if enabled."},
                                new string[] {"repeat(49)", "0", "0", "1", trigval, "Continue Sending Digital Pin Trigger if enabled."},
                                new string[] {"clear_signal(event0)", "0", "0", "1", "0", "PXI Backplane Trigger Off (if enabled). Digital Pin Trigger Off."},
                                new string[] {"repeat(49)", "0", "0", "1", "0", "Digital Pin Trigger Off."},
                                new string[] {"", "0", "0", "1", "X", "Digital Pin Trigger Tristate."},
                                new string[] {"endofpattern:\nrepeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };

                        // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                        List<string[]> pattern = new List<string[]> { };
                        pattern = pattern.Concat(patternStart).ToList();

                        for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                            pattern = pattern.Concat(triggerDelay).ToList();

                        pattern = pattern.Concat(trigger).ToList();

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterWrite" + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                        {
                            throw new Exception("Compile Failed");
                        }

                        HSDIO.datalogResults["RegisterWrite" + "Pair" + p.ToString()] = false;
                        p++;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi RegisterWrite vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the extended register write pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedRegWrite()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0; // index for MIPI pair
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        // Generate MIPI / RFFE Extended Register Write Patterns
                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region ExtendedRegisterWrite Pattern
                                new string[] {"repeat(300)", "0", "0", "1", "0", "Idle"},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Extended Register Write Command (0000)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> writeData = new List<string[]>
                            {
                            #region Write Data...
                                new string[] {"source", "1", "D", "1", "0", "Write Data 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""}
                            #endregion
                            };
                        List<string[]> busPark = new List<string[]>
                            {
                            #region Bus Park
                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""}
                            #endregion
                            };
                        List<string[]> triggerDelay = new List<string[]>
                            {
                                new string[] { "", "0", "0", "1", "0", "Trigger Delay Cycle" }
                            };
                        List<string[]> trigger = new List<string[]>
                            {
                            #region Trigger, Idle Halt
                                new string[] {"jump_if(!seqflag0, endofpattern)", "0", "0", "1", "0", "Check if Trigger Required, if not, go to halt."},
                                new string[] {"set_signal(event0)", "0", "0", "1", trigval, "Turn On PXI Backplane Trigger if enabled. Send Digital Pin Trigger if enabled."},
                                new string[] {"repeat(49)", "0", "0", "1", trigval, "Continue Sending Digital Pin Trigger if enabled."},
                                new string[] {"clear_signal(event0)", "0", "0", "1", "0", "PXI Backplane Trigger Off (if enabled). Digital Pin Trigger Off."},
                                new string[] {"repeat(49)", "0", "0", "1", "0", "Digital Pin Trigger Off."},
                                new string[] {"", "0", "0", "1", "X", "Digital Pin Trigger Tristate."},
                                new string[] {"endofpattern:\nrepeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };

                        for (int i = 1; i <= 16; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedRegisterWrite" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"}
                                };
                            pattern = pattern.Concat(patternStart).ToList();

                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(writeData).ToList();

                            pattern = pattern.Concat(busPark).ToList();

                            for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                                pattern = pattern.Concat(triggerDelay).ToList();

                            pattern = pattern.Concat(trigger).ToList();

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString());
                            }
                            HSDIO.datalogResults["ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedRegisterWrite vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the extended register read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedRegRead()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0; // index for MIPI pair
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        // Generate MIPI / RFFE Extended Register Read Patterns
                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region ExtendedRegisterRead Pattern
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> readData = new List<string[]>
                            {
                            #region Read Data...
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 7"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 6"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 5"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 4"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 3"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 2"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 1"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 0"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Parity"},
                                new string[] {"", "0", "X", "1", "X", ""}
                            #endregion
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
                            #region Bus Park, Idle, and Halt
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };

                        for (int i = 1; i <= 16; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure source"},
                                    new string[] {"capture_start(CapExtendedRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
                                };
                            pattern = pattern.Concat(patternStart).ToList();
                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(readData).ToList();
                            pattern = pattern.Concat(busParkIdleHalt).ToList();

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString());
                            }
                            HSDIO.datalogResults["ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedRegisterRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the non-extended register write and read pattern in one pattern file
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiRegWriteRead()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0; // Index of MIPI pair
                    // Generate MIPI / RFFE Non-Extended Register Write Pattern
                    foreach (MipiPinNames m in allMipiPinNames)
                    {

                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region RegisterWrite Pattern
                                new string[] {"source_start(SrcRegisterWriteReadPair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"},
                                new string[] {"capture_start(CapRegisterWriteReadPair" + p.ToString() + ")", "0", "0", "1", "0", "Configure capture"},
                                new string[] {"repeat(300)", "0", "0", "1", "0", "Idle"},
                                new string[] {"set_loop(reg0)","0", "0", "1", "0",""},                                
                                new string[] {"cmdstart:\nsource", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Register Write Command (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},                                
                                new string[] {"source", "1", "D", "1", "0", "Write Data 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> triggerDelay = new List<string[]>
                            {
                                new string[] { "", "0", "0", "1", "0", "Trigger Delay Cycle" }
                            };
                        List<string[]> trigger = new List<string[]>
                            {
                            #region Trigger
                                new string[] {"jump_if(!seqflag0, readpatternstart)", "0", "0", "1", "0", "Check if Trigger Required, if not, go to halt."},
                                new string[] {"set_signal(event0)", "0", "0", "1", trigval, "Turn On PXI Backplane Trigger if enabled. Send Digital Pin Trigger if enabled."},
                                new string[] {"repeat(49)", "0", "0", "1", trigval, "Continue Sending Digital Pin Trigger if enabled."},
                                new string[] {"clear_signal(event0)", "0", "0", "1", "0", "PXI Backplane Trigger Off (if enabled). Digital Pin Trigger Off."},
                                new string[] {"repeat(49)", "0", "0", "1", "0", "Digital Pin Trigger Off."},
                                new string[] {"", "0", "0", "1", "X", "Digital Pin Trigger Tristate."},                                
                            #endregion
                            };

                        List<string[]> readpattern = new List<string[]>
                            {
                            #region RegisterRead pattern, Idle halt
                                new string[] {"readpatternstart:\nset_loop(reg0)","0", "0", "1", "X",""},                                
                                new string[] {"cmd2start:\nsource", "0", "D", "1", "X", "extra bit to make its sample width = 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "extra bit to make its sample width = 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Register Read Command (011)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Pull Down Only"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 7"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 6"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 5"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 4"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 3"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 2"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 1"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 0"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Parity"},
                                new string[] {"", "0", "X", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"end_loop(cmd2start)", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "X", "X", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };

                        // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                        List<string[]> pattern = new List<string[]> { };
                        pattern = pattern.Concat(patternStart).ToList();

                        for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                            pattern = pattern.Concat(triggerDelay).ToList();

                        pattern = pattern.Concat(trigger).ToList();

                        pattern = pattern.Concat(readpattern).ToList();

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterWriteRead" + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                        {
                            throw new Exception("Compile Failed");
                        }

                        HSDIO.datalogResults["RegisterWriteRead" + "Pair" + p.ToString()] = false;
                        p++;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi RegisterWriteRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the extended register write and read pattern in one pattern file
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedRegWriteRead()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    int p = 0; // index for MIPI pair
                    foreach (MipiPinNames m in allMipiPinNames)
                    {
                        // Generate MIPI / RFFE Extended Register Write Patterns
                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region ExtendedRegisterWrite Pattern
                                new string[] {"repeat(300)", "0", "0", "1", "0", "Idle"},
                                new string[] {"set_loop(reg0)", "0", "0", "1", "0", ""},
                                new string[] {"cmdstart:\nsource", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Extended Register Write Command (0000)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> writeData = new List<string[]>
                            {
                            #region Write Data...
                                new string[] {"source", "1", "D", "1", "0", "Write Data 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Write Data 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""}
                            #endregion
                            };
                        List<string[]> busPark = new List<string[]>
                            {
                            #region Bus Park
                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""}
                            #endregion
                            };
                        List<string[]> triggerDelay = new List<string[]>
                            {
                                new string[] { "", "0", "0", "1", "0", "Trigger Delay Cycle" }
                            };
                        List<string[]> trigger = new List<string[]>
                            {
                            #region Trigger
                                new string[] {"jump_if(!seqflag0, readpatternstart)", "0", "0", "1", "0", "Check if Trigger Required, if not, go to halt."},
                                new string[] {"set_signal(event0)", "0", "0", "1", trigval, "Turn On PXI Backplane Trigger if enabled. Send Digital Pin Trigger if enabled."},
                                new string[] {"repeat(49)", "0", "0", "1", trigval, "Continue Sending Digital Pin Trigger if enabled."},
                                new string[] {"clear_signal(event0)", "0", "0", "1", "0", "PXI Backplane Trigger Off (if enabled). Digital Pin Trigger Off."},
                                new string[] {"repeat(49)", "0", "0", "1", "0", "Digital Pin Trigger Off."},
                                new string[] {"", "0", "0", "1", "X", "Digital Pin Trigger Tristate."},                                
                            #endregion
                            };
                        List<string[]> patternReadStart = new List<string[]>
                            {
                            #region ExtendedRegisterRead Pattern
                                new string[] {"readpatternstart:\nset_loop(reg0)", "0", "0", "1", "X", ""},
                                new string[] {"cmd2start:\nsource", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "0", "D", "1", "X", "Command Frame (010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Slave Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Extended Register Read Command (0010)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 7"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 6"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 5"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 4"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 3"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Address 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""}
                                #endregion
                            };
                        List<string[]> readData = new List<string[]>
                            {
                            #region Read Data...
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 7"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 6"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 5"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 4"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 3"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 2"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 1"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Read Data 0"},
                                new string[] {"", "0", "X", "1", "X", ""},
                                new string[] {"capture", "1", "V", "1", "X", "Parity"},
                                new string[] {"", "0", "X", "1", "X", ""}
                            #endregion
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
                            #region Bus Park, Idle, and Halt
                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"end_loop(cmd2start)", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "0", "0", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}
                            #endregion
                            };


                        for (int i = 1; i <= 16; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedRegisterWriteRead"  + i.ToString()  + "Pair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"},
                                    new string[] {"capture_start(CapExtendedRegisterWriteRead"  + i.ToString()  + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
                                };

                            // Concat Write pattern
                            pattern = pattern.Concat(patternStart).ToList();

                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(writeData).ToList();

                            pattern = pattern.Concat(busPark).ToList();

                            for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                                pattern = pattern.Concat(triggerDelay).ToList();

                            pattern = pattern.Concat(trigger).ToList();

                            // Concat Read pattern
                            pattern = pattern.Concat(patternReadStart).ToList();
                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(readData).ToList();
                            pattern = pattern.Concat(busParkIdleHalt).ToList();


                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString());
                            }
                            HSDIO.datalogResults["ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedRegisterWriteRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }


            /// <summary>
            /// Reset MIPI by setting VIO low and then high (see LoadVector_MipiReset)
            /// </summary>
            public void MipiReset()
            {
                SendVector(HSDIO.Reset.ToLower());
            }

            /// <summary>
            /// Set all Mipi pins to low (see LoadVector_HiZ)
            /// </summary>
            public void MipiHiZ()
            {
                SendVector(HSDIO.HiZ.ToLower());
            }

            /// <summary>
            /// Dynamic Register Write function.  This uses NI 6570 source memory to dynamically change
            /// the register address and write values in the pattern.
            /// This supports extended and non-extended register write.
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register address to write (hex)</param>
            /// <param name="data_hex">The data to write into the specified register in Hex.  Note:  Maximum # of bytes to write is 16.</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public void RegWrite(int pair, string slaveAddress_hex, string registerAddress_hex, string data_hex, bool sendTrigger = false)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                    if (sendTrigger)
                    {
                        // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_signal, clear_signal, and pulse_trigger opcodes
                        if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
                        {
                            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", pxiTrigger.ToString("g"));
                        }
                        else
                        {
                            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
                        }

                        // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
                        if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin)
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
                        DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");

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
                    nameInMemory += "Pair" + pair.ToString();

                    // Source buffer must contain 512 elements, even if sourcing less
                    uint[] dataArray = new uint[512];
                    if (!extendedWrite)
                    {
                        // Build non-exteded write command
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex, Command.REGISTERWRITE);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[2] = calculateParity(Convert.ToUInt32(data_hex, 16)); // final 9 bits
                    }
                    else
                    {
                        // Build extended read command data, setting read byte count and register address. 
                        // Note, write byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(writeByteCount - 1, 16), Command.EXTENDEDREGISTERWRITE);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[2] = calculateParity(Convert.ToUInt32(registerAddress_hex, 16)); // address 9 bits
                        // Convert Hex Data string to bytes and add to data Array
                        for (int i = 0; i < writeByteCount * 2; i += 2)
                            dataArray[3 + (i / 2)] = (uint)(calculateParity(Convert.ToByte(data_hex.Substring(i, 2), 16)));
                    }

                    // Configure 6570 to source data calculated above
                    DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                    // Write Sequencer Register reg0 = 1 -- access only one register address
                    DIGI.PatternControl.WriteSequencerRegister("reg0", 1);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory;

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                    // Get PassFail Results for site 0
                    Int64[] failureCount = sdataPins.GetFailCount();
                    NumExecErrors = (int)failureCount[0];
                    if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + registerAddress_hex + " Bit Errors: " + NumExecErrors.ToString());
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
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register address to read (hex)</param>
            /// <returns>The value of the specified register in Hex</returns>
            public string RegRead(int pair, string slaveAddress_hex, string registerAddress_hex)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };
                    bool extendedRead = Convert.ToInt32(registerAddress_hex, 16) > 31;    // any register address > 5 bits requires extended read
                    uint readByteCount = 1;
                    string nameInMemory = extendedRead ? "ExtendedRegisterRead" + readByteCount.ToString() : "RegisterRead";
                    nameInMemory += "Pair" + pair.ToString();

                    uint[] dataArray = new uint[512];
                    // Source buffer must contain 512 elements, even if sourcing less
                    if (!extendedRead)
                    {
                        // Build non-extended read command data
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex, Command.REGISTERREAD);
                        // Split data into array of data, all must be same # of bits (16) which must be specified when calling CreateSerial 
                        dataArray[0] = cmdBytesWithParity;
                    }
                    else
                    {
                        // Build extended read command data, setting read byte count and register address.
                        // Note, read byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(readByteCount - 1, 16), Command.EXTENDEDREGISTERREAD);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        dataArray[2] = (uint)(calculateParity(Convert.ToUInt16(registerAddress_hex, 16)));  // Final 9 bits to contains the address (for extended read) + parity.
                    }

                    // Configure to source data
                    DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, (uint)(extendedRead ? 9 : 16), BitOrder.MostSignificantBitFirst);
                    DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

                    // Configure to capture 8 bits (Ignore Parity)
                    DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Cap" + nameInMemory, readByteCount * 9, BitOrder.MostSignificantBitFirst);

                    // Write Sequencer Register reg0 = 1 -- access only one register address
                    DIGI.PatternControl.WriteSequencerRegister("reg0", 1);

                    // Choose Pattern to Burst
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory;

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                    // Get PassFail Results for site 0
                    passFail = DIGI.PatternControl.GetSitePassFail("");
                    Int64[] failureCount = sdataPins.GetFailCount();
                    NumExecErrors = (int)failureCount[0];
                    if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegRead " + registerAddress_hex + " Bit Errors: " + NumExecErrors.ToString());

                    // Retreive captured waveform
                    uint[][] capData = new uint[1][];
                    DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, 1, new TimeSpan(0, 0, 0, 0, 100), ref capData);

                    // Remove the parity bit 
                    capData[0][0] = (capData[0][0] >> 1) & 0xFF;

                    // Convert captured data to hex string and return
                    string returnval = capData[0][0].ToString("X");
                    if (debug) Console.WriteLine("Slave " + slaveAddress_hex + ", ReadReg " + registerAddress_hex + ": " + returnval);
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
            /// Dynamic Register Write and Read function for one or multiple Extended register adddress.  
            /// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.            
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            /// <param name="data_hex">The data to write into the respective specified register in Hex. </param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public string RegWriteReadAny(int pair, string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool sendTrigger = false)
            {
                try
                {
                    // Check if dataArray_size / (7 MIPI commands) <= 73  (see dataArray size at below)
                    if (registerAddress_hex.Count() > 73)
                    {
                        throw new Exception("Too much extended register address to write in one function call. Reduce it less than or equal to 102.");
                    }

                    List<MipiWaveforms> wfms = parseMipiRegAccessType(slaveAddress_hex, registerAddress_hex, data_hex);

                    string returnval = "";

                    int i = 0;
                    foreach (var w in wfms)
                    {
                        if (i != wfms.Count() - 1)
                            returnval += "," + RegWriteRead_Initiate(pair, slaveAddress_hex, w, false);
                        else
                            returnval += "," + RegWriteRead_Initiate(pair, slaveAddress_hex, w, sendTrigger);
                        i++;
                    }

                    returnval = returnval.Substring(1);

                    if (debug) Console.WriteLine("Slave " + slaveAddress_hex + ", ReadReg " + string.Join(" ", registerAddress_hex.Zip(returnval.Split(','), (r, d) => (r + ":" + d))));
                    return returnval;

                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Write or Read Register for Address " + string.Join(" ", registerAddress_hex) + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "";
                }
            }

            /// <summary>
            /// Dynamic Register Write and Read function for one or multiple Extended register adddress.  
            /// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.            
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="mipiCmd">MIPI command in text pattern such as "1C:38 00:01 02:02"</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            /// <returns></returns>
            public string RegWriteReadAny(int pair, string slaveAddress_hex, string mipiCmd, bool sendTrigger = false)
            {
                char[] delimiters = { ' ' };
                string[] regdata = mipiCmd.Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries);
                return RegWriteReadAny(pair, slaveAddress_hex,
                                         (from r in regdata select r.Substring(0, 2)).ToArray(),
                                         (from r in regdata select r.Substring(3, 2)).ToArray(), false);
            }

            #region Deprecated functions
            ///// <summary>
            ///// Dynamic Register Write function for one or multiple Non-Extended register adddress.  
            ///// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern.            
            ///// </summary>
            ///// <param name="pair">The MIPI pair number to write</param>
            ///// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            ///// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            ///// <param name="data_hex">The data to write into the respective specified register in Hex. </param>
            ///// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            //public void RegWriteNonExtended(int pair, string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool sendTrigger = false)
            //{
            //    try
            //    {
            //        // Check if non-extended register addresses are used. 
            //        foreach (var r in registerAddress_hex)
            //        {
            //            if ((Convert.ToInt32(r, 16) > 31))   // any register address > 5 bits requires extended read
            //            {
            //                throw new Exception("Should not call RegWriteNonExtended function with Extended Register Address input");
            //            }
            //        }

            //        // Check if dataArray_size / (3 MIPI commands) <= 170  (see dataArray size at below)
            //        if (registerAddress_hex.Count() > 170)
            //        {
            //            throw new Exception("Too much non-extended register address to write in one function call. Reduce it less than or equal to 170.");
            //        }

            //        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
            //        allRffePins.SelectedFunction = SelectedFunction.Digital;
            //        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

            //        if (sendTrigger)
            //        {
            //            // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_signal, clear_signal, and pulse_trigger opcodes
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
            //            {
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", pxiTrigger.ToString("g"));
            //            }
            //            else
            //            {
            //                // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
            //            }

            //            // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin)
            //            {
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", true);
            //            }
            //            else
            //            {
            //                // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //            }

            //            if (triggerConfig == TrigConfig.None)
            //            {
            //                throw new Exception("sendTrigger=True requested, but NI 6570 is not configured for Triggering.  Please update the NI6570 Constructor triggerConfig to use TrigConfig.Digital_Pin, TrigConfig.PXI_Backplane, or TrigConfig.Both.");
            //            }
            //        }
            //        else
            //        {
            //            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");

            //            // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //            DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //        }

            //        DIGI.PatternControl.Commit();

            //        for (int i = 0; i < data_hex.Count(); i++)
            //        {
            //            // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
            //            if (data_hex[i].Length % 2 == 1)
            //                data_hex[i] = data_hex[i].PadLeft(data_hex[i].Length + 1, '0');
            //        }

            //        // Burst pattern & check for Pass/Fail
            //        bool[] passFail = new bool[] { };
            //        string nameInMemory = "RegisterWrite";
            //        nameInMemory += "Pair" + pair.ToString();

            //        // Source buffer must contain 512 elements, even if sourcing less
            //        uint[] dataArray = new uint[512];


            //        int j = 0;
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build non-exteded write command
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERWRITE);
            //            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
            //            j++;
            //            dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
            //            j++;
            //            dataArray[j] = calculateParity(Convert.ToUInt32(data_hex[i], 16)); // final 9 bits
            //            j++;
            //        }

            //        // Configure 6570 to source data calculated above
            //        DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
            //        DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

            //        // Write Sequencer Register reg0 = number of register address to access
            //        DIGI.PatternControl.WriteSequencerRegister("reg0", registerAddress_hex.Count());

            //        // Choose Pattern to Burst
            //        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
            //        DIGI.PatternControl.StartLabel = nameInMemory;

            //        // Burst Pattern
            //        DIGI.PatternControl.Initiate();

            //        // Wait for Pattern Burst to complete
            //        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

            //        // Get PassFail Results for site 0
            //        Int64[] failureCount = sdataPins.GetFailCount();
            //        NumExecErrors = (int)failureCount[0];
            //        if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + string.Join(" ", registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());
            //    }
            //    catch (Exception e)
            //    {
            //        DIGI.PatternControl.Abort();
            //        MessageBox.Show("Failed to Write Register for Address " + string.Join(" ", registerAddress_hex) + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}

            ///// <summary>
            ///// Dynamic Register Read function for one or multiple Non-Extended register adddresses.  
            ///// This uses NI 6570 capture memory to receive the values from the DUT.            
            ///// </summary>
            ///// <param name="pair">The MIPI pair number to write</param>
            ///// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            ///// <param name="registerAddress_hex">The register addresses to read (hex)</param>
            ///// <returns>The value of the respective specified register in Hex</returns>
            //public string RegReadNonExtended(int pair, string slaveAddress_hex, string[] registerAddress_hex)
            //{
            //    try
            //    {
            //        // Check if non-extended register addresses are used. 
            //        foreach (var r in registerAddress_hex)
            //        {
            //            if ((Convert.ToInt32(r, 16) > 31))   // any register address > 5 bits requires extended read
            //            {
            //                throw new Exception("Should not call RegWriteNonExtended function with Extended Register Address input");
            //            }
            //        }

            //        // Check if dataArray_size / (3 MIPI commands) <= 170  (see dataArray size at below)
            //        if (registerAddress_hex.Count() > 170)
            //        {
            //            throw new Exception("Too much non-extended register address to write in one function call. Reduce it less than or equal to 170.");
            //        }

            //        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
            //        allRffePins.SelectedFunction = SelectedFunction.Digital;
            //        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

            //        // Burst pattern & check for Pass/Fail
            //        bool[] passFail = new bool[] { };
            //        uint readByteCount = 1;
            //        string nameInMemory = "RegisterRead";
            //        nameInMemory += "Pair" + pair.ToString();

            //        uint[] dataArray = new uint[512];
            //        // Source buffer must contain 512 elements, even if sourcing less
            //        int j = 0;
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build non-extended read command data
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERREAD);
            //            // Split data into array of data, all must be same # of bits (16) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity;
            //            j++;
            //        }


            //        // Configure to source data
            //        DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, (uint)(16), BitOrder.MostSignificantBitFirst);
            //        DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);

            //        // Configure to capture 8 bits (Ignore Parity)
            //        DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Cap" + nameInMemory, readByteCount * 9, BitOrder.MostSignificantBitFirst);

            //        // Write Sequencer Register reg0 = number of register address to access
            //        DIGI.PatternControl.WriteSequencerRegister("reg0", registerAddress_hex.Count());

            //        // Choose Pattern to Burst
            //        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
            //        DIGI.PatternControl.StartLabel = nameInMemory;

            //        // Burst Pattern
            //        DIGI.PatternControl.Initiate();

            //        // Wait for Pattern Burst to complete
            //        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

            //        // Get PassFail Results for site 0
            //        passFail = DIGI.PatternControl.GetSitePassFail("");
            //        Int64[] failureCount = sdataPins.GetFailCount();
            //        NumExecErrors = (int)failureCount[0];
            //        if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegRead " + string.Join(" ", registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());

            //        // Retreive captured waveform
            //        uint[][] capData = new uint[registerAddress_hex.Count()][];
            //        DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, registerAddress_hex.Count(), new TimeSpan(0, 0, 0, 0, 100), ref capData);

            //        // Remove the parity bit 
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //            capData[0][i] = (capData[0][i] >> 1) & 0xFF;

            //        // Convert captured data to hex string and return
            //        string returnval = string.Join(",", capData[0].Select(d => d.ToString("X2")));
            //        if (debug) Console.WriteLine("Slave " + slaveAddress_hex + ", ReadReg " + string.Join(" ", registerAddress_hex.Zip(capData[0], (r, d) => (r + ":" + d.ToString("X2")))));
            //        return returnval;
            //    }
            //    catch (Exception e)
            //    {
            //        DIGI.PatternControl.Abort();
            //        MessageBox.Show("Failed to Read Register for Address " + string.Join(" ", registerAddress_hex) + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        return "";
            //    }
            //}

            ///// <summary>
            ///// Dynamic Register Write and Read function for one or multiple Non-Extended register adddress.  
            ///// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.            
            ///// </summary>
            ///// <param name="pair">The MIPI pair number to write</param>
            ///// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            ///// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            ///// <param name="data_hex">The data to write into the respective specified register in Hex. </param>
            ///// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            //public string RegWriteReadNonExtended(int pair, string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool sendTrigger = false)
            //{
            //    try
            //    {
            //        // Check if non-extended register addresses are used. 
            //        foreach (var r in registerAddress_hex)
            //        {
            //            if ((Convert.ToInt32(r, 16) > 31))   // any register address > 5 bits requires extended read
            //            {
            //                throw new Exception("Should not call RegWriteNonExtended function with Extended Register Address input");
            //            }
            //        }

            //        // Check if dataArray_size / (5 MIPI commands) <= 102  (see dataArray size at below)
            //        if (registerAddress_hex.Count() > 102)
            //        {
            //            throw new Exception("Too much non-extended register address to write in one function call. Reduce it less than or equal to 102.");
            //        }

            //        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
            //        allRffePins.SelectedFunction = SelectedFunction.Digital;
            //        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

            //        if (sendTrigger)
            //        {
            //            // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_signal, clear_signal, and pulse_trigger opcodes
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
            //            {
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", pxiTrigger.ToString("g"));
            //            }
            //            else
            //            {
            //                // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
            //            }

            //            // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin)
            //            {
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", true);
            //            }
            //            else
            //            {
            //                // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //            }

            //            if (triggerConfig == TrigConfig.None)
            //            {
            //                throw new Exception("sendTrigger=True requested, but NI 6570 is not configured for Triggering.  Please update the NI6570 Constructor triggerConfig to use TrigConfig.Digital_Pin, TrigConfig.PXI_Backplane, or TrigConfig.Both.");
            //            }
            //        }
            //        else
            //        {
            //            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");

            //            // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //            DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //        }

            //        DIGI.PatternControl.Commit();

            //        for (int i = 0; i < data_hex.Count(); i++)
            //        {
            //            // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
            //            if (data_hex[i].Length % 2 == 1)
            //                data_hex[i] = data_hex[i].PadLeft(data_hex[i].Length + 1, '0');
            //        }

            //        // Burst pattern & check for Pass/Fail
            //        bool[] passFail = new bool[] { };
            //        string nameInMemory = "RegisterWriteRead";
            //        nameInMemory += "Pair" + pair.ToString();




            //        #region Configure Waveforms
            //        // Source buffer must contain 512 elements, even if sourcing less
            //        uint[] dataArray = new uint[512];

            //        int j = 0;
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build non-exteded write command
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERWRITE);
            //            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
            //            j++;
            //            dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
            //            j++;
            //            dataArray[j] = calculateParity(Convert.ToUInt32(data_hex[i], 16)); // final 9 bits
            //            j++;
            //        }

            //        uint readByteCount = 1;

            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build non-extended read command data
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERREAD);
            //            // Split data into array of data, all must be same # of bits (8) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity >> 9;    // first 9 bits
            //            j++;
            //            dataArray[j] = cmdBytesWithParity & 0xFF;  // second 9 bits
            //            j++;
            //        }

            //        // Configure 6570 to source data calculated above
            //        DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "SrcRegisterWriteReadPair" + pair.ToString(), SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
            //        DIGI.SourceWaveforms.WriteBroadcast("SrcRegisterWriteReadPair" + pair.ToString(), dataArray);

            //        // Configure to capture 8 bits (Ignore Parity)
            //        DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "CapRegisterReadPair" + pair.ToString(), readByteCount * 9, BitOrder.MostSignificantBitFirst);

            //        #endregion


            //        // Write Sequencer Register reg0 = number of register address to access
            //        DIGI.PatternControl.WriteSequencerRegister("reg0", registerAddress_hex.Count());

            //        // Choose Pattern to Burst
            //        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
            //        DIGI.PatternControl.StartLabel = nameInMemory;

            //        // Burst Pattern
            //        DIGI.PatternControl.Initiate();

            //        // Wait for Pattern Burst to complete
            //        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

            //        // Get PassFail Results for site 0
            //        Int64[] failureCount = sdataPins.GetFailCount();
            //        NumExecErrors = (int)failureCount[0];
            //        if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + string.Join(" ", registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());

            //        #region Fetch Captured waveform
            //        // Retreive captured waveform
            //        uint[][] capData = new uint[registerAddress_hex.Count()][];
            //        DIGI.CaptureWaveforms.Fetch("", "CapRegisterReadPair" + pair.ToString(), registerAddress_hex.Count(), new TimeSpan(0, 0, 0, 0, 100), ref capData);

            //        // Remove the parity bit 
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //            capData[0][i] = (capData[0][i] >> 1) & 0xFF;

            //        // Convert captured data to hex string and return
            //        string returnval = string.Join(",", capData[0].Select(d => d.ToString("X2")));
            //        if (debug) Console.WriteLine("Slave " + slaveAddress_hex + ", ReadReg " + string.Join(" ", registerAddress_hex.Zip(capData[0], (r, d) => (r + ":" + d.ToString("X2")))));
            //        #endregion

            //        return returnval;

            //    }
            //    catch (Exception e)
            //    {
            //        DIGI.PatternControl.Abort();
            //        MessageBox.Show("Failed to Write or Read Register for Address " + string.Join(" ", registerAddress_hex) + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        return "";
            //    }
            //}

            ///// <summary>
            ///// Dynamic Register Write and Read function for one or multiple Extended register adddress.  
            ///// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.            
            ///// </summary>
            ///// <param name="pair">The MIPI pair number to write</param>
            ///// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            ///// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            ///// <param name="data_hex">The data to write into the respective specified register in Hex. </param>
            ///// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            //public string RegWriteReadExtended(int pair, string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool sendTrigger = false)
            //{
            //    try
            //    {
            //        // Check if non-extended register addresses are used. 
            //        foreach (var r in registerAddress_hex)
            //        {
            //            if (!(Convert.ToInt32(r, 16) > 31))   // any register address > 5 bits requires extended read
            //            {
            //                throw new Exception("Should not call RegWriteExtended function with Non-Extended Register Address input");
            //            }
            //        }

            //        // Check if dataArray_size / (7 MIPI commands) <= 73  (see dataArray size at below)
            //        if (registerAddress_hex.Count() > 73)
            //        {
            //            throw new Exception("Too much extended register address to write in one function call. Reduce it less than or equal to 102.");
            //        }

            //        // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
            //        allRffePins.SelectedFunction = SelectedFunction.Digital;
            //        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

            //        if (sendTrigger)
            //        {
            //            // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_signal, clear_signal, and pulse_trigger opcodes
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
            //            {
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", pxiTrigger.ToString("g"));
            //            }
            //            else
            //            {
            //                // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //                DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
            //            }

            //            // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
            //            if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin)
            //            {
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", true);
            //            }
            //            else
            //            {
            //                // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //                DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //            }

            //            if (triggerConfig == TrigConfig.None)
            //            {
            //                throw new Exception("sendTrigger=True requested, but NI 6570 is not configured for Triggering.  Please update the NI6570 Constructor triggerConfig to use TrigConfig.Digital_Pin, TrigConfig.PXI_Backplane, or TrigConfig.Both.");
            //            }
            //        }
            //        else
            //        {
            //            // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
            //            DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");

            //            // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
            //            DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
            //        }

            //        DIGI.PatternControl.Commit();

            //        for (int i = 0; i < data_hex.Count(); i++)
            //        {
            //            // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
            //            if (data_hex[i].Length % 2 == 1)
            //                data_hex[i] = data_hex[i].PadLeft(data_hex[i].Length + 1, '0');
            //        }

            //        // Burst pattern & check for Pass/Fail
            //        bool[] passFail = new bool[] { };
            //        //uint writeByteCount = (uint)(data_hex.Length / 2);
            //        // ASSUME all data_hex have same byte count
            //        uint writeByteCount = (uint)(Math.Ceiling((double)(Convert.ToInt32(data_hex[0], 16)) / (double)256));

            //        string nameInMemory = "ExtendedRegisterWriteRead";
            //        nameInMemory += writeByteCount.ToString() + "Pair" + pair.ToString();




            //        #region Configure Waveforms
            //        // Source buffer must contain 512 elements, even if sourcing less
            //        uint[] dataArray = new uint[512];

            //        int j = 0;
            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build extended read command data, setting read byte count and register address. 
            //            // Note, write byte count is 0 indexed.
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(writeByteCount - 1, 16), Command.EXTENDEDREGISTERWRITE);
            //            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
            //            j++;
            //            dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
            //            j++;
            //            dataArray[j] = calculateParity(Convert.ToUInt32(registerAddress_hex[i], 16)); // address 9 bits
            //            j++;
            //            // Convert Hex Data string to bytes and add to data Array                        
            //            int nextj = 0;
            //            for (int k = 0; k < writeByteCount * 2; k += 2)
            //            {
            //                dataArray[j + (k / 2)] = (uint)(calculateParity(Convert.ToByte(data_hex[i].Substring(k, 2), 16)));
            //                nextj = j + (k / 2) + 1;
            //            }

            //            j = nextj;
            //        }

            //        uint readByteCount = 1;

            //        for (int i = 0; i < registerAddress_hex.Count(); i++)
            //        {
            //            // Build extended read command data, setting read byte count and register address.
            //            // Note, read byte count is 0 indexed.
            //            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(readByteCount - 1, 16), Command.EXTENDEDREGISTERREAD);
            //            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
            //            dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
            //            j++;
            //            dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
            //            j++;
            //            dataArray[j] = (uint)(calculateParity(Convert.ToUInt16(registerAddress_hex[i], 16)));  // Final 9 bits to contains the address (for extended read) + parity.
            //            j++;
            //        }

            //        // Configure 6570 to source data calculated above
            //        DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "SrcExtendedRegisterWriteReadPair" + pair.ToString(), SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
            //        DIGI.SourceWaveforms.WriteBroadcast("SrcExtendedRegisterWriteReadPair" + pair.ToString(), dataArray);

            //        // Configure to capture 8 bits (Ignore Parity)
            //        DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "CapExtendedRegisterReadPair" + pair.ToString(), readByteCount * 9, BitOrder.MostSignificantBitFirst);

            //        #endregion


            //        // Write Sequencer Register reg0 = number of register address to access
            //        DIGI.PatternControl.WriteSequencerRegister("reg0", registerAddress_hex.Count());

            //        // Choose Pattern to Burst
            //        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
            //        DIGI.PatternControl.StartLabel = nameInMemory;

            //        // Burst Pattern
            //        DIGI.PatternControl.Initiate();

            //        // Wait for Pattern Burst to complete
            //        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

            //        // Get PassFail Results for site 0
            //        Int64[] failureCount = sdataPins.GetFailCount();
            //        NumExecErrors = (int)failureCount[0];
            //        if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + string.Join(" ", registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());

            //        #region Fetch Captured waveform
            //        // Retreive captured waveform
            //        uint[][] capData = new uint[registerAddress_hex.Count() * readByteCount][];
            //        DIGI.CaptureWaveforms.Fetch("", "CapExtendedRegisterReadPair" + pair.ToString(), (registerAddress_hex.Count() * (int)readByteCount), new TimeSpan(0, 0, 0, 0, 100), ref capData);

            //        // Remove the parity bit 
            //        for (int i = 0; i < (registerAddress_hex.Count() * readByteCount); i++)
            //            capData[0][i] = (capData[0][i] >> 1) & 0xFF;

            //        // Convert captured data to hex string and return
            //        string returnval = string.Join(",", capData[0].Select(d => d.ToString("X2")));
            //        if (debug) Console.WriteLine("Slave " + slaveAddress_hex + ", ReadReg " + string.Join(" ", registerAddress_hex.Zip(capData[0], (r, d) => (r + ":" + d.ToString("X2")))));
            //        #endregion

            //        return returnval;

            //    }
            //    catch (Exception e)
            //    {
            //        DIGI.PatternControl.Abort();
            //        MessageBox.Show("Failed to Write or Read Register for Address " + string.Join(" ", registerAddress_hex) + ".\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        return "";
            //    }
            //}

            #endregion


            /// <summary>
            /// To burst the MIPI Write + Read pattern using the given sourcing waveform w
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="w">MIPI waveform to source</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            /// <returns>The captured data read from the register. Use this to verify with what you write</returns>
            private string RegWriteRead_Initiate(int pair, string slaveAddress_hex, MipiWaveforms w, bool sendTrigger = false)
            {
                #region Configure pins and trigger
                // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                allRffePins.SelectedFunction = SelectedFunction.Digital;
                allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                if (sendTrigger)
                {
                    // Configure the NI 6570 to connect PXI_TrigX to "event0" that can be used with the set_signal, clear_signal, and pulse_trigger opcodes
                    if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.PXI_Backplane)
                    {
                        DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", pxiTrigger.ToString("g"));
                    }
                    else
                    {
                        // Disable "event0" triggering.  This turns off PXI Backplane Trigger Generation.
                        DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");
                    }

                    // Set the Sequencer Flag 0 to indicate that a trigger should be sent on the TrigChan pin
                    if (triggerConfig == TrigConfig.Both || triggerConfig == TrigConfig.Digital_Pin)
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
                    DIGI.ExportSignal(SignalType.PatternOpcodeEvent, "patternOpcodeEvent0", "");

                    // Disable the Sequencer Flag 0 triggering.  This turns off Digital Pin Trigger Generation.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag0", false);
                }

                DIGI.PatternControl.Commit();
                #endregion

                // Assume all write byte count are same
                string nameInMemory = w.isExtended ? "ExtendedRegisterWriteRead" : "RegisterWriteRead";
                if (w.isExtended)
                    nameInMemory += w.writeByteCount[0].ToString() + "Pair" + pair.ToString();
                else
                    nameInMemory += "Pair" + pair.ToString();


                // Configure 6570 to source data calculated above
                DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, w.dataArray);

                // Configure to capture 8 bits (Ignore Parity)
                DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Cap" + nameInMemory, w.readByteCount[0] * 9, BitOrder.MostSignificantBitFirst);


                // Write Sequencer Register reg0 = number of register address to access
                DIGI.PatternControl.WriteSequencerRegister("reg0", w.cmdCount);


                // Choose Pattern to Burst
                // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                DIGI.PatternControl.StartLabel = nameInMemory;

                // Burst Pattern
                DIGI.PatternControl.Initiate();

                // Wait for Pattern Burst to complete
                DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                // Get PassFail Results for site 0
                Int64[] failureCount = sdataPins.GetFailCount();
                NumExecErrors = (int)failureCount[0];
                if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + string.Join(" ", w.registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());

                #region Fetch Captured waveform (assume all readByteCount are same)
                // Retreive captured waveform
                uint[][] capData = new uint[w.cmdCount * w.readByteCount[0]][];
                DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, (w.cmdCount * (int)w.readByteCount[0]), new TimeSpan(0, 0, 0, 0, 100), ref capData);

                // Remove the parity bit 
                for (int i = 0; i < (w.cmdCount * w.readByteCount[0]); i++)
                    capData[0][i] = (capData[0][i] >> 1) & 0xFF;

                // Convert captured data to hex string and return
                string returnval = string.Join(",", capData[0].Select(d => d.ToString("X2")));

                #endregion

                return returnval;
            }

            /// <summary>
            /// Used by RegWriteRead
            /// </summary>
            private class MipiWaveforms
            {
                public uint[] dataArray = new uint[512];
                public bool isExtended = false;
                public int cmdCount = 1;
                public uint[] writeByteCount;
                public uint[] readByteCount;
                public string[] registerAddress_hex;
            }

            /// <summary>
            /// Calculate data array for sourcing a MIPI Reg Write + Read in one pattern file
            /// This assumes the writeByteCount of the given data_hex has same byte count
            /// readByteCount is always 1. 
            /// </summary>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            /// <param name="data_hex">The data to write into the respective specified register in Hex</param>
            /// <param name="isExtended">Is Extended register address?</param>
            /// <returns></returns>
            private static MipiWaveforms calcMipiWaveforms(string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool isExtended)
            {
                MipiWaveforms w = new MipiWaveforms();
                w.isExtended = isExtended;
                w.cmdCount = registerAddress_hex.Count();
                w.writeByteCount = new uint[w.cmdCount];
                w.readByteCount = new uint[w.cmdCount];
                w.registerAddress_hex = registerAddress_hex;

                if (!isExtended)
                {
                    int j = 0;

                    #region Non-Extended RegWrite
                    for (int i = 0; i < registerAddress_hex.Count(); i++)
                    {
                        // Build non-exteded write command
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERWRITE);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        w.dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        j++;
                        w.dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        j++;
                        w.dataArray[j] = calculateParity(Convert.ToUInt32(data_hex[i], 16)); // final 9 bits
                        j++;
                        w.writeByteCount[i] = (uint)(data_hex.Length / 2);
                    }

                    #endregion

                    #region Non-Extended RegRead
                    for (int i = 0; i < registerAddress_hex.Count(); i++)
                    {
                        // Build non-extended read command data
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, registerAddress_hex[i], Command.REGISTERREAD);
                        // Split data into array of data, all must be same # of bits (8) which must be specified when calling CreateSerial 
                        w.dataArray[j] = cmdBytesWithParity >> 9;    // first 9 bits
                        j++;
                        w.dataArray[j] = cmdBytesWithParity & 0xFF;  // second 9 bits
                        j++;
                        w.readByteCount[i] = 1;
                    }
                    #endregion
                }
                else
                {
                    int j = 0;

                    #region Extended RegWrite
                    for (int i = 0; i < registerAddress_hex.Count(); i++)
                    {
                        // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                        if (data_hex[i].Length % 2 == 1)
                            data_hex[i] = data_hex[i].PadLeft(data_hex.Length + 1, '0');

                        w.writeByteCount[i] = (uint)(data_hex[i].Length / 2);


                        // Build extended write command data, setting read byte count and register address. 
                        // Note, write byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(w.writeByteCount[i] - 1, 16), Command.EXTENDEDREGISTERWRITE);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        w.dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        j++;
                        w.dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        j++;
                        w.dataArray[j] = calculateParity(Convert.ToUInt32(registerAddress_hex[i], 16)); // address 9 bits
                        j++;



                        // Convert Hex Data string to bytes and add to data Array                        
                        int nextj = 0;
                        for (int k = 0; k < w.writeByteCount[i] * 2; k += 2)
                        {
                            w.dataArray[j + (k / 2)] = (uint)(calculateParity(Convert.ToByte(data_hex[i].Substring(k, 2), 16)));
                            nextj = j + (k / 2) + 1;
                        }

                        j = nextj;
                    }
                    #endregion

                    #region Extended RegRead
                    for (int i = 0; i < registerAddress_hex.Count(); i++)
                    {
                        // Assume all readByteCount are 1
                        w.readByteCount[i] = 1;

                        // Build extended read command data, setting read byte count and register address.
                        // Note, read byte count is 0 indexed.
                        uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(w.readByteCount[i] - 1, 16), Command.EXTENDEDREGISTERREAD);
                        // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial 
                        w.dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                        j++;
                        w.dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                        j++;
                        w.dataArray[j] = (uint)(calculateParity(Convert.ToUInt16(registerAddress_hex[i], 16)));  // Final 9 bits to contains the address (for extended read) + parity.
                        j++;
                    }
                    #endregion
                }

                return w;
            }

            /// <summary>
            /// Parse each MIPI register acceess whether is extended or non-extended type
            /// </summary>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            /// <param name="data_hex">The data to write into the respective specified register in Hex</param>
            /// <returns>A list of mipi waveform to be sourced later</returns>
            private static List<MipiWaveforms> parseMipiRegAccessType(string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex)
            {
                List<MipiWaveforms> wfms = new List<MipiWaveforms>();

                // Define 2 temporary tuple lists of <registerAddress, data_hex, writeByteCount>
                // nonE is list of non-Extended 
                var nonE = new List<Tuple<string, string, uint>>();
                // E is list of Extended
                var E = new List<Tuple<string, string, uint>>();

                // Create a lambda function to get Mipi Waveform from nonE and E list (optional, for cosmetic purpose)
                var getWfm = new Func<List<Tuple<string, string, uint>>, bool, MipiWaveforms>(
                               (T, isEx)
                                => (calcMipiWaveforms(slaveAddress_hex,
                                                      T.Select((r) => (r.Item1)).ToArray(),
                                                      T.Select(r => r.Item2).ToArray(), isEx)));

                bool lastIsNonE = true;

                for (int i = 0; i < registerAddress_hex.Count(); i++)
                {
                    bool isExtended = Convert.ToInt32(registerAddress_hex[i], 16) > 31;    // any register address > 5 bits requires extended read
                    uint writeByteCount = (uint)(data_hex[i].Length / 2);
                    if (!isExtended)
                    {

                        #region Collect Non-Extended Reg Address Access
                        // If different write byte count, refresh the nonE list
                        if (nonE.Count() > 0 && nonE.Last().Item3 != writeByteCount)
                        {
                            wfms.Add(getWfm(nonE, false));  // Add into waveform list before delete
                            nonE.Clear();
                        }

                        nonE.Add(new Tuple<string, string, uint>(registerAddress_hex[i], data_hex[i], writeByteCount));

                        // If previous loop is an update of E and E is not empty currently
                        // Refresh the list of E
                        if (!lastIsNonE & E.Count() != 0)
                        {
                            wfms.Add(getWfm(E, true)); ;
                            E.Clear();
                        }
                        #endregion

                        lastIsNonE = true;
                    }
                    else
                    {
                        #region Collect Extended Reg Address Access
                        // If different write byte count, refresh the E list
                        if (E.Count() > 0 && E.Last().Item3 != writeByteCount)
                        {
                            wfms.Add(getWfm(E, true));  // Add into waveform list before delete
                            E.Clear();
                        }

                        // and then add the current item
                        E.Add(new Tuple<string, string, uint>(registerAddress_hex[i], data_hex[i], writeByteCount));

                        // If previous loop is an update of nonE and nonE is not empty currently
                        // Refresh the list of nonE 
                        if (lastIsNonE & nonE.Count() != 0)
                        {
                            wfms.Add(getWfm(nonE, false));
                            nonE.Clear();
                        }
                        #endregion

                        lastIsNonE = false;
                    }

                }

                #region Clear the items left in nonE and E
                if (nonE.Count() != 0)
                {
                    wfms.Add(getWfm(nonE, false));
                    nonE.Clear();
                }

                if (E.Count() != 0)
                {
                    wfms.Add(getWfm(E, true));
                    E.Clear();
                }
                #endregion

                return wfms;
            }


            /// <summary>
            /// A unit test to verify validity of parseMipiRegAccessType function
            /// </summary>
            private static void selfTest_parseMipiRegAccessType()
            {
                var v =
                HSDIO.NI6570.parseMipiRegAccessType("0x0E", new string[] { "5B", "1C", "1C", "1C", "5E", "5E" },
                                                                 new string[] { "FD", "38", "38", "38", "0C", "0C" });
                v =
                HSDIO.NI6570.parseMipiRegAccessType("0x0E", new string[] { "1C", "5B", "5C", "5C", "5E", "1C" },
                                                                     new string[] { "38", "FD", "67", "6789", "0C", "38" });
            }

            /// <summary>
            /// Source a current to specified pin
            /// </summary>
            /// <param name="PinName"></param>
            /// <param name="currentForce"></param>
            public void ForceCurrent(string PinName, double currentForce)
            {
                try
                {
                    // Configure for PPMU measurements, Output Current, Measure Voltage
                    // Configure for PPMU Measurements
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
                    pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCCurrent;

                    // Using the requested current to decide the current level range from the values supported for private release of 6570
                    double range = currentForce;
                    if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
                    else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
                    else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

                    // Set the current level range and voltage limits
                    pin.Ppmu.DCCurrent.CurrentLevelRange = Math.Abs(range);
                    pin.Ppmu.DCCurrent.VoltageLimitHigh = 1;    //Original: 5
                    pin.Ppmu.DCCurrent.VoltageLimitLow = -2;
                    // Configure Current Level and begin Sourcing
                    pin.Ppmu.DCCurrent.CurrentLevel = currentForce;
                    pin.Ppmu.Source();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ForceCurrent");
                }
            }

            /// <summary>
            /// Set specified pin to PPMU mode, and force a voltage
            /// </summary>
            /// <param name="PinName"></param>
            /// <param name="voltsForce"></param>
            /// <param name="currentLimit"></param>
            public void ForceVoltage(string PinName, double voltsForce, double currentLimit)
            {
                try
                {
                    // Configure 6570 for PPMU measurements, Output Voltage, Measure Current
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
                    pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCVoltage;
                    // Force Voltage Configure
                    pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;

                    // Using the requested current limit to decide the current level range from the values supported for private release of 6570
                    double range = currentLimit;
                    if (Math.Abs(range) < 2e-6) { range = 2e-6; } // +-2uA
                    else if (Math.Abs(range) < 32e-6) { range = 32e-6; } // +-32uA
                    else if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
                    else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
                    else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

                    pin.Ppmu.DCCurrent.CurrentLevelRange = range;
                    pin.Ppmu.DCVoltage.CurrentLimitRange = range;
                    // Perform Voltage Force
                    pin.Ppmu.Source();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "ForceVoltage");
                }
            }

            /// <summary>
            /// Measure Current at specified pin
            /// </summary>
            /// <param name="PinName">Name of listed pin in MIPI.allMIPIPinNames</param>
            /// <param name="NumAverages"></param>
            /// <param name="Result">Result of measured current</param>
            public void MeasureCurrent(string PinName, int NumAverages, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];

                    // Measure Current
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    //CM Edited: Added aperture time configuration
                    pin.Ppmu.ConfigureApertureTime(0.0001 * (double)NumAverages, NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);

                    meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Current);
                    // (Multiple measurement will result if the PinName is a set of multiple pin names. 
                    //  Here, even user assign multiple pin names, we only take the first and ignore the rest.
                    //  You can change this behaviour by returning all elements in meas array)
                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureCurrent");
                }
            }

            /// <summary>
            /// Measure voltage at specified pin
            /// </summary>
            /// <param name="PinName">Name of listed pin in MIPI.allMIPIPinNames</param>
            /// <param name="NumAverages"></param>
            /// <param name="Result">Result of measured voltage</param>
            public void MeasureVoltage(string PinName, int NumAverages, ref double Result)
            {
                try
                {
                    double[] meas = new double[32];
                    // Configure Number of Averages by setting the Apperture Time
                    NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = HSDIO.NI6570.DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
                    pin.Ppmu.ConfigureApertureTime(0.0020 * (double)(NumAverages), NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);

                    // Measure Voltage
                    meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Voltage);

                    // Select only the first result
                    // (Multiple measurement will result if the PinName is a set of multiple pin names. 
                    //  Here, even user assign multiple pin names, we only take the first and ignore the rest.
                    //  You can change this behaviour by returning all elements in meas array)
                    Result = meas[0];
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "MeasureVoltage");
                }
            }



            /// <summary>
            /// Returns the number of bit errors from the most recently executed pattern.
            /// </summary>
            /// <param name="nameInMemory">Not Used</param>
            /// <returns>Number of bit errors</returns>
            public int GetNumExecErrors(string nameInMemory)
            {
                Int64[] failureCount = sdataPins.GetFailCount();
                return (int)failureCount[0];
            }

            /// <summary>
            /// Send the pattern requested by nameInMemory.
            /// If requesting the PID pattern, generate the signal and store the result for later processing by InterpretPID
            /// </summary>
            /// <param name="nameInMemory">The requested pattern to generate</param>
            /// <returns>True if the pattern generated without bit errors</returns>
            public bool SendVector(string nameInMemory)
            {
                if (!usingMIPI) return true;

                try
                {
                    nameInMemory = nameInMemory.Replace("_", "");

                    if (nameInMemory.ToUpper() == "READPID")
                    {
                        // NOTE:  We only need to do one style read, both dynamic read and hard coded
                        //        pattern read can be done here to prove both work the same.

                        // Read PID using the dynamic RegRead command instead of the hard coded vector
                        //int dyanmicPIDRead = Convert.ToInt32(this.RegRead("1D"), 16);

                        // Read PID using the hard coded pattern instead of the dynamic RegRead command                        
                        pidval = this.SendPIDVector(nameInMemory);
                        Console.WriteLine("PID: " + pidval);

                        // Check if dynamic read matches hard coded read.
                        //return dyanmicPIDRead == pidval;
                        return true;
                    }
                    else
                    {
                        // This is not a special case such as PID
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                        // Select pattern to burst
                        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                        DIGI.PatternControl.StartLabel = nameInMemory.ToLower();

                        // Send the normal pattern file and store the number of bit errors from the SDATA pin
                        DIGI.PatternControl.Initiate();

                        // Wait for Pattern Burst to complete
                        DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                        // Get PassFail Results
                        bool[] passFail = DIGI.PatternControl.GetSitePassFail("");
                        Int64[] failureCount = this.sdataPins.GetFailCount();
                        NumExecErrors = (int)failureCount[0];
                        if (debug) Console.WriteLine("SendVector " + nameInMemory + " Bit Errors: " + NumExecErrors.ToString());

                        return passFail[0];
                    }
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to generate vector for " + nameInMemory + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            /// <summary>
            /// This function sends the ReadPID pattern.  This is the standard .vec pattern.
            /// The 6570 uses capture memory to get the data from every H/L location in the original .vec
            /// </summary>
            /// <param name="nameInMemory"></param>
            /// <returns></returns>
            private uint SendPIDVector(string nameInMemory)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                    // Local Variables
                    bool[] passFail = new bool[] { };
                    uint numBits = (captureCounters.ContainsKey(nameInMemory.ToUpper()) ? captureCounters[nameInMemory.ToUpper()] : 8);

                    // Create the capture waveform.
                    DIGI.CaptureWaveforms.CreateSerial(this.sdataPins, "Cap" + nameInMemory.ToLower(), numBits, BitOrder.MostSignificantBitFirst);

                    // Choose Pattern to Burst (ReadTempSense)
                    // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                    DIGI.PatternControl.StartLabel = nameInMemory.ToLower();

                    // Burst Pattern
                    DIGI.PatternControl.Initiate();

                    // Wait for Pattern Burst to complete
                    DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                    // Get PassFail Results for site 0
                    passFail = DIGI.PatternControl.GetSitePassFail("");
                    Int64[] failureCount = this.sdataPins.GetFailCount();
                    NumExecErrors = (int)failureCount[0];
                    if (debug) Console.WriteLine("SendPIDVector " + nameInMemory + " Bit Errors: " + NumExecErrors.ToString());

                    // Retreive captured waveform, sample count is 1 byte of data
                    uint[][] data = new uint[1][];
                    DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory.ToLower(), 1, new TimeSpan(0, 0, 0, 0, 100), ref data);

                    // Return PID Value as read from DUT.  Remove Parity and Bus Park bits by shifting right by 2.
                    return data[0][0] >> 2;  //*//CHANGE 10-15-2015

                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to generate vector for " + nameInMemory + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }
            }

            public void ConfigureSetting(bool script, string TestMode)
            {
            }

            public void SendTRIGVectors()
            {
                if (!usingMIPI) return;
                try
                {
                    // We will use sequencer flag 3 to send "script triggers" to the pattern.  The pattern will do a jump_cond(!seqflag3, <here>) to wait for this to be set in SW.
                    DIGI.PatternControl.WriteSequencerFlag("seqflag3", true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            /// <summary>
            /// Close the NI 6570 session when shutting down the application
            /// and ensure all patterns are unloaded and all channels are disconnected.
            /// </summary>
            public void Close()
            {
                allRffePins.SelectedFunction = SelectedFunction.Disconnect;
                DIGI.Dispose();
            }


            #region Avago SJC Specific Helper Functions

            /// <summary>
            /// NI Internal Function:  Generate the requested RFFE command
            /// </summary>
            /// <param name="registerAddress_hex_or_ByteCount">For non-extended read / write, this is the register address.  For extended read / write, this is the number of bytes to read.</param>
            /// <param name="instruction">EXTENDEDREGISTERWRITE, EXTENDEDREGISTERREAD, REGISTERWRITE, or REGISTERREAD</param>
            /// <returns>The RFFE Command + Parity</returns>
            private static uint generateRFFECommand(string slaveAddress_hex, string registerAddress_hex_or_ByteCount, Command instruction)
            {
                int slaveAddress = (Convert.ToByte(slaveAddress_hex, 16)) << 8;
                int commandFrame = 1 << 14;
                Byte regAddress = Convert.ToByte(registerAddress_hex_or_ByteCount, 16);

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
            private static uint calculateParity(uint cmdWithoutParity)
            {
                int x = (int)cmdWithoutParity;
                x ^= x >> 16;
                x ^= x >> 8;
                x ^= x >> 4;
                x &= 0x0F;
                bool parity = ((0x6996 >> x) & 1) != 0;
                return (uint)(cmdWithoutParity << 1 | Convert.ToByte(!parity));
            }

            /// <summary>
            /// Create a .digipatsrc file from the given inputs and compile into a .digipat file.
            /// Once compilation of the .digipat succeeds, load the pattern into instrument memory.
            /// </summary>
            /// <param name="patternName">The pattern name or "nameInMemory" used to execute this pattern later in the program</param>
            /// <param name="pinList">The pins associated with this pattern.  These must match the timeset.  For NRZ patterns, the timeset is "MIPI"; otherwise the timeset is "MIPI_SCLK_RZ"</param>
            /// <param name="pattern">The pattern specified by a 2-d array of strings, one column per pin, columns must correspond to pinList array.</param>
            /// <param name="overwrite">If a compiled .digipat already exists for this .vec and this is TRUE, re-compile and overwrite the original .digipat regardless of if the .vec has changed.  If FALSE, use the pre-existing .digipat if the .vec has not changed or create if it doesn't exist.</param>
            /// <param name="timeSet">Specify if this pattern should use the MIPI or the MIPI_SCLK_RZ timeset</param>
            /// <returns>True if pattern compilation and loading to instrument memory succeeds.</returns>
            public bool GenerateAndLoadPattern(string patternName, string[] pinList, string[,] pattern, bool overwrite, Timeset timeSet)
            {
                // Convert from string[,] into slightly better List<string[]>
                List<string[]> newPattern = new List<string[]>(pattern.GetLength(0));
                for (int x = 0; x < pattern.GetLength(0); x++)
                {
                    string[] tmp = new string[pattern.GetLength(1)];
                    for (int y = 0; y < pattern.GetLength(1); y++)
                        tmp[y] = pattern[x, y];
                    newPattern.Add(tmp);
                }
                return GenerateAndLoadPattern(patternName, pinList, newPattern, overwrite, timeSet);
            }

            /// <summary>
            /// Create a .digipatsrc file from the given inputs and compile into a .digipat file.
            /// Once compilation of the .digipat succeeds, load the pattern into instrument memory.
            /// </summary>
            /// <param name="patternName">The pattern name or "nameInMemory" used to execute this pattern later in the program</param>
            /// <param name="pinList">The pins associated with this pattern.  These must match the timeset.  For NRZ patterns, the timeset is "MIPI"; otherwise the timeset is "MIPI_SCLK_RZ"</param>
            /// <param name="pattern">The pattern specified by a list of string arrays, one list item containing a 1-d array of string for each line in the vector.  1-D array must correspond to pinList array.</param>
            /// <param name="overwrite">If a compiled .digipat already exists for this .vec and this is TRUE, re-compile and overwrite the original .digipat regardless of if the .vec has changed.  If FALSE, use the pre-existing .digipat if the .vec has not changed or create if it doesn't exist.</param>
            /// <param name="timeSet">Specify if this pattern should use the MIPI or the MIPI_SCLK_RZ timeset</param>
            /// <returns>True if pattern compilation and loading to instrument memory succeeds.</returns>
            public bool GenerateAndLoadPattern(string patternName, string[] vecpins, List<string[]> pattern, bool overwrite, Timeset timeSet)
            {
                #region Generate Paths & Constants
                patternName = patternName.Replace("_", "");
                string patternSavePath = fileDir + "\\" + patternName + ".digipat";
                string digipatsrcPath = Path.ChangeExtension(patternSavePath, "digipatsrc");
                string digipatPath = Path.ChangeExtension(patternSavePath, "digipat");
                #endregion

                #region Check if files exist and handle appropriately
                // Check if digipatsrc exists, and if so, check to see if the .vec checksum has changed
                // If the checksum has changed, set overwrite to true so that we force the regeneration
                // instead of loading a stale digipat file
                if (File.Exists(digipatsrcPath) && !overwrite)
                {
                    System.IO.StreamReader digipatsrcFileMD5 = new System.IO.StreamReader(digipatsrcPath);
                    try
                    {
                        string MD5 = digipatsrcFileMD5.ReadLine().Substring("// VECMD5: ".Length);
                        overwrite = MD5 != ComputeMD5Hash(pattern);
                    }
                    catch
                    {
                        overwrite = true;
                    }
                    digipatsrcFileMD5.Close();
                }

                if (File.Exists(digipatPath))
                {
                    if (overwrite)
                    {
#if NIDEEPDEBUG
                        Console.WriteLine("Overwriting previously compiled .digipat");
#endif
                        File.Delete(digipatPath);
                        if (File.Exists(Path.ChangeExtension(digipatPath, "digipat_index")))
                            File.Delete(Path.ChangeExtension(digipatPath, "digipat_index"));
                    }
                    else
                    {
                        // Compiled digipat already exists, just load it
                        DIGI.LoadPattern(digipatPath);
                        return true;
                    }
                }
                if (File.Exists(digipatsrcPath))
                {
                    // Delete digipatsrc file if it already exists, do this after digipat check (don't delete src if digipat already exists)
                    File.Delete(digipatsrcPath);
                }
                #endregion

                #region Generate .digipatsrc

                #region Open digipatsrc File
                System.IO.StreamWriter digipatsrcFile = new System.IO.StreamWriter(digipatsrcPath);
                #endregion

                #region Write Header
                digipatsrcFile.WriteLine("// VECMD5: " + ComputeMD5Hash(pattern));
                digipatsrcFile.WriteLine("// National Instruments Digital Pattern Text File.");
                digipatsrcFile.WriteLine("// Automatically Generated from the GenerateAndLoadPattern function.");
                digipatsrcFile.WriteLine("// Pattern Name: " + patternName);
                digipatsrcFile.WriteLine("// Generated Date: " + System.DateTime.Now.ToString());
                digipatsrcFile.WriteLine("//\n");
                digipatsrcFile.WriteLine("file_format_version 1.0;");
                digipatsrcFile.WriteLine("timeset " + timeSet.ToString("g") + ";");
                digipatsrcFile.Write("\n");
                #endregion

                #region Loop through vectors and store in digipatsrc File

                // Write start of pattern, line contains comma separated pin names
                string pinlist = string.Join(",", this.allDutPins).ToUpper();
                digipatsrcFile.WriteLine("pattern " + patternName + "(" + pinlist + ")");
                digipatsrcFile.WriteLine("{");

                // Write all vector lines
                foreach (string[] lineData in pattern)
                {
                    // Add Timeset and opcode at the start
                    string lineOutput = lineData[0] + "\t" + timeSet.ToString("g");
                    foreach (string pin in this.allDutPins)
                    {
                        if (vecpins.Contains(pin.ToUpper()))
                        {
                            lineOutput += "\t" + lineData[Array.IndexOf(vecpins, pin.ToUpper()) + 1];
                        }
                        else
                        {
                            lineOutput += "\t-";
                        }
                    }
                    // Handle Comment, it is always the last item in the string array
                    if (lineData[lineData.Count() - 1] != "")
                        lineOutput += @"; // " + lineData[lineData.Count() - 1] + "\n";
                    else
                        lineOutput += ";\n";

                    digipatsrcFile.Write(lineOutput);
                }

                // Close out pattern
                digipatsrcFile.WriteLine("}\n");
                #endregion

                #region close text digipatsrc File
                digipatsrcFile.Close();
                #endregion
                #endregion

                return this.CompileDigipatSrc(digipatsrcPath, digipatPath, patternName, this.allDutPins, true);
            }

            /// <summary>
            /// NI Internal Function:  Given a digipatsrc file, compile and save into the given digipat file, using
            /// the specified patternName and Pins.  Generate Paths, Check if compiler exists and handle appropriately, Create a dummy pinmap containing the specified pins (pinmap required by compiler), then compile the digipatsrc into digipat.
            /// </summary>
            /// <param name="digipatsrcPath">The Absolute path to the digipatsrc file</param>
            /// <param name="digipatPath">The Absolute path to the desired digipat file output</param>
            /// <param name="patternName">The name of the pattern, used later to load the file into memory</param>
            /// <param name="pins">The pins in the pattern file</param>
            /// <param name="addTrig">If True, this indicates that an extra trigger channel was added, but the pins array doesn't contain it so we should add it during compile</param>
            /// <param name="load">If True, this function will automatically load the pattern into instrument memory after a successful compile</param>
            /// <returns>True if compilation and loading succeeds</returns>
            private bool CompileDigipatSrc(string digipatsrcPath, string digipatPath, string patternName, string[] pins, bool load = true)
            {
                #region Generate Paths
                string compilerPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\National Instruments\\Digital Pattern Compiler\\DigitalPatternCompiler.exe";
                string pinmapPath = fileDir + "\\compiler.pinmap";
                #endregion

                #region Check if compiler exists and handle appropriately
                if (!File.Exists(compilerPath))
                {
                    // Compiler not found, can't proceed
                    throw new FileNotFoundException("Digital Pattern Compiler Not Found", compilerPath);
                }
                #endregion

                #region Constants
                patternName = patternName.Replace("_", "");
                string pinmapHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<PinMap schemaVersion=\"1.1\" xmlns=\"http://www.ni.com/TestStand/SemiconductorModule/PinMap.xsd\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n	<Instruments>\n		<NIDigitalPatternInstrument name=\"NI6570\" numberOfChannels=\"32\" />\n	</Instruments>\n	<Pins>\n";
                string pinmapMiddle = "\n	</Pins>\n	<PinGroups>\n	</PinGroups>\n	<Sites>\n		<Site siteNumber=\"0\" />\n	</Sites>\n	<Connections>\n";
                string pinmapFooter = "\n	</Connections>\n</PinMap>";
                #endregion

#if NIDEEPDEBUG
                Console.WriteLine("Compiling from .digipatsrc to .digipat");
#endif
                #region Create dummy pinmap to be used by compiler
                if (File.Exists(pinmapPath)) { File.Delete(pinmapPath); }
                System.IO.StreamWriter pinmapFile = new StreamWriter(pinmapPath);
                pinmapFile.Write(pinmapHeader);
                foreach (string pin in pins)
                {
                    pinmapFile.WriteLine("<DUTPin name=\"" + pin + "\" />");
                }

                pinmapFile.Write(pinmapMiddle);

                foreach (string pin in pins)
                {
                    pinmapFile.WriteLine("<Connection pin=\"" + pin + "\" siteNumber=\"0\" instrument=\"NI6570\" channel=\"" + PinNamesAndChans[pin] + "\" />");
                }

                pinmapFile.Write(pinmapFooter);
                pinmapFile.Close();
                #endregion

                #region Run Compiler
                // Setup Process
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                // Run Digital Pattern Compiler located at compilerPath
                startInfo.FileName = compilerPath;
                // Pass in the pinmap, compiled digipat path, and text digipatsrc paths; escape spaces properly for cmd line execution
                startInfo.Arguments = " -pinmap " + pinmapPath.Replace(" ", @"^ ") + " -o " + digipatPath.Replace(" ", @"^ ") + " " + digipatsrcPath.Replace(" ", @"^ ");
                // Run Process
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

#if NIDEEPDEBUG
                Console.WriteLine("Compilation " + (process.ExitCode == 0 ? "Succeeded.  Loading Pattern to Instrument Memory." : "Failed"));
#endif
                // Delete Temporary Pinmap
                //File.Delete(pinmapPath);
                #endregion

                #region Load Pattern to Instrument Memory
                // Check if process exited without error and return status.
                if (process.ExitCode == 0)
                {
                    // Compilation completed without error, try loading pattern now.
                    try
                    {
                        if (load)
                        {
                            DIGI.LoadPattern(digipatPath);
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else
                {
                    return false;
                }
                #endregion
            }

            /// <summary>
            /// NI Internal Function:  Compute the MD5 Hash of any file.
            /// </summary>
            /// <param name="filePath">The absolute path of the file for which to comput the MD5 Hash</param>
            /// <returns>The computed MD5 Hash String</returns>
            private string ComputeMD5Hash(string filePath)
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    return BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(filePath))) + "_" + this.version.ToString();
                }
            }
            /// <summary>
            /// NI Internal Function:  Compute the MD5 Hash of any file.
            /// </summary>
            /// <param name="pattern">The List of String Arrays representing a Pattern in memory</param>
            /// <returns>The computed MD5 Hash String</returns>
            private string ComputeMD5Hash(List<string[]> pattern)
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    string flattenedPattern = "";
                    foreach (var line in pattern.ToArray())
                    {
                        flattenedPattern += string.Join(",", line);
                    }
                    return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(flattenedPattern))) + "_" + this.version.ToString();
                }
            }
            #endregion


            #region Avago SJC Specific Enums
            /// <summary>
            /// Used to specify which timeset is used for a specified pattern.
            /// Get the string representation using Timeset.MIPI.ToString("g");
            /// </summary>
            public enum Timeset
            {
                MIPI,
                MIPI_SCLK_RZ
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
                REGISTERREAD
            };

            private System.Version version = new System.Version(1, 0, 1215, 1);
            #endregion
        }
    }
}
