using NationalInstruments.ModularInstruments.NIDigital;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

    public class NI_PXIe6570 : Base_MIPI, iMiPiCtrl
    {
        private int slaveaddr, pairNo;
        private int ret = 0;
        private bool[] dataArray_Bool;
        private string moduleAlias = Lib_Var.myNI6570Address;
        public override string OptionString => base.OptionString;
        public override string ModelNumber => base.ModelNumber;

        // Initialize NI 6570 session; generate and load vector files of MIPI commands
        public HSDIO.NI6570 myMipiCtrl;

        public s_assignMIPIpin[] copyMipiPinNames;

        private LibEqmtDriver.Utility.HiPerfTimer HiTimer = new LibEqmtDriver.Utility.HiPerfTimer();

        public NI_PXIe6570(s_MIPI_PAIR[] mipiPairCfg, Dictionary<string, LibEqmtDriver.SMU.iSmu> PpmuResources = null)
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

            this.ModelNumber = "6570";
            this.OptionString = Simulated ? $"Simulate=1, DriverSetup=Model:{ModelNumber};" : string.Empty;

            myMipiCtrl = new HSDIO.NI6570(moduleAlias, true, copyMipiPinNames, PpmuResources, OptionString);
            INITIALIZATION();
        }

        #region iMipiCtrl interface

        void iMiPiCtrl.Init(s_MIPI_PAIR[] mipiPairCfg)
        {
            //not use
        }

        void iMiPiCtrl.Init_ID(s_MIPI_PAIR[] mipiPairCfg)
        {
            //not use
        }

        void iMiPiCtrl.TurnOn_VIO(int pair)
        {
            if (Lib_Var.b_setNIVIO)
            {
                myMipiCtrl.MipiVIOOn();

                Thread.Sleep(1);

                Lib_Var.b_setNIVIO = false;
            }
        }

        void iMiPiCtrl.TurnOff_VIO(int pair)
        {
            myMipiCtrl.MipiHiZ();
            Lib_Var.b_setNIVIO = true;
        }

        void iMiPiCtrl.SendAndReadMIPICodes(out bool ReadSuccessful, int Mipi_Reg)
        {
            //This function is for fixed MIPI Pair and Slave address
            pairNo = 0;                             //default using MIPI pair no 0 (fixed - hardcoded)
            slaveaddr = Lib_Var.SlaveAddress;      //default setting from config file

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

            // ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));
            ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));       //need 2nd read before NI return correctly
            Result = int.Parse(tmpRslt, System.Globalization.NumberStyles.HexNumber);               //convert HEX to INT
        }

        void iMiPiCtrl.ReadMIPICodesCustom(out int[] Result, out bool bPass, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            string tmpRslt = "";
            // Result = 0;
            // string[] tmpData = MipiRegMap.Split(':');

            //ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));
            //ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));       //need 2nd read before NI return correctly
            //Result = int.Parse(tmpRslt, System.Globalization.NumberStyles.HexNumber);               //convert HEX to INT


            string biasData = MipiRegMap;
            string[] biasDataArr = biasData.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            int[] result = new int[biasDataArr.Length];
            bPass = true;

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                string[] tmpData = biasDataArr[i].Split(':');
                ReadRegister_Single(ref tmpRslt, Convert.ToInt32(tmpData[0], 16));
                result[i] = Convert.ToInt32(tmpRslt, 16);

                bPass &= (result[i] == Convert.ToInt32(tmpData[1], 16));
            }
            Result = result;
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

        //void iMiPiCtrl.SetMeasureMIPIcurrent(int delayMs, int pair, int slvaddr, s_MIPI_DCSet[] setDC_Mipi, string[] measDC_MipiCh, out s_MIPI_DCMeas[] measDC_Mipi)
        //{
        //    //Initialize variable
        //    s_MIPI_DCMeas[] tmpMeasDC_Mipi = new s_MIPI_DCMeas[measDC_MipiCh.Length];

        //    //Note : this function for using multiple MIPI pair and slave address
        //    pairNo = pair;          //pass value from tcf for different mipi pair controller
        //    slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

        //    //DIO Ch alias name -> must be same as we define earlier during init instrument
        //    string tmpMipiPinNames = null;

        //    #region PPMU function - Force and Measure V/I
        //    #region Force PPMU
        //    for (int i = 0; i < setDC_Mipi.Length; i++)
        //    {
        //        switch (setDC_Mipi[i].ChNo)
        //        {
        //            case 0:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].SclkPinName;
        //                break;
        //            case 1:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].SdataPinName;
        //                break;
        //            case 2:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].VioPinName;
        //                break;
        //        }

        //        myMipiCtrl.ForceVoltage(tmpMipiPinNames, setDC_Mipi[i].VChSet, setDC_Mipi[i].IChSet);  // Force voltage on two pins
        //    }
        //    #endregion

        //    HiTimer.wait(Convert.ToDouble(delayMs));

        //    #region Measure PPMU
        //    for (int i = 0; i < measDC_MipiCh.Length; i++)
        //    {
        //        switch (Convert.ToInt16(measDC_MipiCh[i]))
        //        {
        //            case 0:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].SclkPinName;
        //                break;
        //            case 1:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].SdataPinName;
        //                break;
        //            case 2:
        //                tmpMipiPinNames = copyMipiPinNames[pairNo].VioPinName;
        //                break;
        //        }

        //        double meas = 0;
        //        myMipiCtrl.MeasureCurrent(tmpMipiPinNames, 10, ref meas);  // Measurement always return result of one pin,
        //        tmpMeasDC_Mipi[i].IChMeas = (float)meas;
        //        tmpMeasDC_Mipi[i].MipiPinNames = tmpMipiPinNames;
        //    }
        //    #endregion
        //    #endregion

        //    //return result
        //    measDC_Mipi = tmpMeasDC_Mipi;
        //}
        int iMiPiCtrl.SendVector(int pair, string nameInMemory)
        {
            //Not Implemented
            return 0;
        }

        int iMiPiCtrl.ReadVector(int pair, ref int VectorErrorCount, string nameInMemory)
        {
            //Not Implemented
            return 0;
        }

        bool iMiPiCtrl.LoadVector_PowerMode(string fullPath, string powerMode, int vecSetNo)
        {
            //Not Implemented
            return true;
        }

        void iMiPiCtrl.BoardTemperature(out double tempC)
        {
            tempC = -999;        //note temperature return out should be in rage of 25 degC
            tempC = myMipiCtrl.I2CTEMPSENSERead();
        }
        void iMiPiCtrl.ReadLoadboardsocketID(out string loadboardID, out string socketID)
        {
            loadboardID = "NaN";
            socketID = "NaN";
        }
        void iMiPiCtrl.BurstMIPIforNFR(int pair)
        {
            pairNo = pair;

            if (pairNo > 2)
                myMipiCtrl.BurstMIPIforNFR("MIPINFRWriteDual");

            else
                myMipiCtrl.BurstMIPIforNFR("MIPINFRWritePair" + pairNo.ToString());

            Lib_Var.b_setNIVIO = false;
        }
        void iMiPiCtrl.AbortBurst()
        {
            myMipiCtrl.AbortBurst();

            Lib_Var.b_setNIVIO = true;

        }
        #endregion iMipiCtrl interface

        #region Init function

        public void INITIALIZATION()
        {
            string dutSlaveAddress = "2";

            try
            {
                HSDIO.dutSlaveAddress = dutSlaveAddress;

                myMipiCtrl.LoadVector_EEPROM();
                myMipiCtrl.LoadVector_TEMPSENSEI2C(0);

                // Initial dummy read expected to be 0
                double labelReadTemp = myMipiCtrl.I2CTEMPSENSERead();

                string labelReadID = myMipiCtrl.EepromRead();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString(), "HSDIO MIPI");
            }
        }

        #endregion Init function

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

        #endregion small apps

        #region test mipi

        public void TRIG()
        {
            //Mipi_Write(pairNo, slaveaddr, 0x1c, 0x03);
            myMipiCtrl.RegWrite(pairNo, ToHex(slaveaddr), ToHex(Lib_Var.PMTrig), ToHex(Lib_Var.PMTrig_Data));
        }

        public bool Register_Change(int Mipi_Reg, bool IsOTPReg = false)
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
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg0;
                        break;

                    case 1:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg1;
                        break;

                    case 2:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg2;
                        break;

                    case 3:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg3;
                        break;

                    case 4:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg4;
                        break;

                    case 5:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg5;
                        break;

                    case 6:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg6;
                        break;

                    case 7:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg7;
                        break;

                    case 8:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg8;
                        break;

                    case 9:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_Reg9;
                        break;

                    case 10:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegA;
                        break;

                    case 11:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegB;
                        break;

                    case 12:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegC;
                        break;

                    case 13:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegD;
                        break;

                    case 14:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegE;
                        break;

                    case 15:
                        MIPI_RegCond[reg_Cnt] = LibEqmtDriver.MIPI.Lib_Var.MIPI_RegF;
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
                        myMipiCtrl.RegWrite(pairNo, ToHex(slaveaddr), MIPI_RegCond[reg_Cnt], ToHex(reg_Cnt), false, IsOTPReg);
                }

                TRIG();

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    T_ReadSuccessful[reg_Cnt] = true;
                    regX_value[reg_Cnt] = "";

                    if (MIPI_RegCond[reg_Cnt].ToUpper() != "X")
                    {
                        regX_value[reg_Cnt] = myMipiCtrl.RegRead(pairNo, ToHex(slaveaddr), ToHex(reg_Cnt), IsOTPReg);
                    }
                    else
                    {
                        regX_value[reg_Cnt] = MIPI_RegCond[reg_Cnt];
                    }

                    if (MIPI_RegCond[reg_Cnt] != regX_value[reg_Cnt] && LibEqmtDriver.MIPI.Lib_Var.ReadFunction == true)
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

                if (mipi_RegCond != regX_value[i] && LibEqmtDriver.MIPI.Lib_Var.ReadFunction == true)
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

        #endregion test mipi
    }

    public static class HSDIO
    {
        public static Dictionary<string, string> PinNamesAndChans = new Dictionary<string, string>();

        public static bool usingMIPI = false;
        public const string Reset = "RESET", HiZ = "HiZ", RegIO = "regIO", VIOON = "VIOON";
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

        #region TestBoard EEPROM & TEMPERATURE

        public static string dutSlaveAddress;
        public static decimal I2CTempSensorDeviceAddress;

        #endregion TestBoard EEPROM & TEMPERATURE

        public interface iHsdioInstrument
        {
            bool LoadVector(List<string> fullPaths, string nameInMemory, bool datalogResults);

            bool LoadVector_MipiHiZ();

            bool LoadVector_MipiReset();

            bool LoadVector_MipiRegIO();

            void ConfigureSetting(bool script, string TestMode);

            void SendTRIGVectors();

            int GetNumExecErrors(string nameInMemory);

            void RegWrite(int pair, string slaveAddress_hex, string registerAddress_hex, string data_hex, bool sendTrigger = false, bool IsOTPReg = false);

            string RegRead(int pair, string slaveAddress_hex, string registerAddress_hex, bool IsOTPReg = false);

            void Close();

            #region TestBoard EEPROM & TEMPERATURE

            bool LoadVector_EEPROM();

            bool LoadVector_TEMPSENSEI2C(decimal TempSensorAddress = 3);

            double I2CTEMPSENSERead();

            string EepromRead();

            #endregion TestBoard EEPROM & TEMPERATURE
        }

        public class NI6570 : iHsdioInstrument
        {
            /*
             * Notes:  Requires the following References added to project (set Copy Local = false):
             *   - NationalInstruments.ModularInstruments.NIDigital.Fx40
             *   - Ivi.Driver
             */

            // The Instrument Session
            //public static NIDigital DIGI;
            public NIDigital DIGI;

            public Dictionary<string, LibEqmtDriver.SMU.iSmu> PpmuResourcesLocal = null;

            #region Private Variables

            private string allRffeChans;
            private DigitalPinSet allRffePins, sdataPins, sclkPins, vioPins, trigPin, allRffePinsWoVIO; // Ben - Add the 'allRffePinsWoVIO' for VIO PPMU Setup
            private string[] allDutPins = new string[] { };
            private double pidval;  // Stores the acquired PID value after executing the PID pattern.
            private List<string> loadedPatternFiles; // used to store previously loaded patterns so we don't try and double load.  Double Loading will cause an error, so always check this list to see if pattern was previously loaded.
            private double MIPIClockRate;  // MIPI NRZ Clock Rate (2 x Vector Rate)
            private double StrobePoint;
            private double MIPINFRClockRate, StrobePointMIPINFR;
            private bool forceDigiPatRegeneration = true; //false;  // Set this to true if you want to re-generate all .digipat files from the .vec files, even if the .vec files haven't changed.
            private int NumExecErrors; // Stores the number of bit errors from the most recently executed pattern.
            private Dictionary<string, uint> captureCounters = new Dictionary<string, uint>(); // This dictionary stores the # of captures each .vec contains (for .vec files that are converted to capture format)
            private string fileDir; // This is the path used to store intermediate digipatsrc, digipat, and other files.
            private TrigConfig triggerConfig = TrigConfig.None;  // No Triggering by default
            private PXI_Trig pxiTrigger = PXI_Trig.PXI_Trig7;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG7 shouldn't interfere.
            private uint regWriteTriggerCycleDelay = 0;

            //private bool debug = true; // Turns on additional console messages if true
            private bool debug = false;
            private object lockObject = new object();

            #region TestBoard EEPROM & TEMPERATURE

            private string I2CVCCChanName = "VCC";
            private string I2CSDAChanName = "SDA";
            private string I2CSCKChanName = "SCK";
            private string TEMPSENSEI2CVCCChanName = "TSVCC";

            private string allEEPROMChans, allTEMPSENSEChans;
            private DigitalPinSet allEEPROMPins, EEPROMsckPin, allTEMPSENSEPins, TEMPSENSEsckPin, TEMPSENSEVccPin, EEPROMVccPin;

            private double EEPROMClockRate;  // EEPROM NRZ Clock Rate (2 x Vector Rate)

            #endregion TestBoard EEPROM & TEMPERATURE

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
            public NI6570(string visaAlias, bool AutoSubCal, s_assignMIPIpin[] mipiCfg, Dictionary<string, LibEqmtDriver.SMU.iSmu> PpmuResources = null, string Optionstring = "")
            {
                PpmuResourcesLocal = PpmuResources;
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
                MIPIClockRate = MIPI.Lib_Var.DUTMipi.MipiClockSpeed;   //52e6;  // This is the Return to Zero rate.
                MIPINFRClockRate = MIPI.Lib_Var.DUTMipi.MipiNFRClockSpeed;

                // Set these values based on calling ((HSDIO.NI6570)HSDIO.Instrument).shmoo("QC_Test");
                // Ideally, try to set UserDelay = 0 if possible and only modify StrobePoint.

                StrobePoint = (1 / MIPIClockRate) * MIPI.Lib_Var.DUTMipi.StrobePoint;
                StrobePointMIPINFR = (1 / MIPINFRClockRate) * MIPI.Lib_Var.DUTMipi.StrobePoint;

                regWriteTriggerCycleDelay = 0;

                // Trigger Configuration;  This applies to the RegWrite command and will send out a hardware trigger
                // on the specified triggers (Digital Pin, PXI Backplane, or Both) at the end of the Register Write operation.
                triggerConfig = TrigConfig.Digital_Pin;
                pxiTrigger = PXI_Trig.PXI_Trig7;  // TRIG0 - TRIG2 used by various other instruments in Clotho;  TRIG7 shouldn't interfere.

                #region Initialize Private Variables

                fileDir = Path.GetTempPath() + "NI.Temp\\NI6570";
                Directory.CreateDirectory(fileDir);

                loadedPatternFiles = new List<string> { };

                #endregion Initialize Private Variables

                // Initialize Instrument
                DIGI = new NIDigital(visaAlias, false, true, Optionstring);
                DIGI.Utility.ResetDevice();

                #region NI Pin Map Configuration

                // Make sure you add all needed pins here so that they get auto-added to all NI-6570 digipat files.  If they aren't in allDutPins or allSystemPins, you can't use them.

                #region Start Index of PhysicalPinsDefinition

                // Define pin and channel mapping here

                //disable by shaz - pin 16 clash with testboard eeprom if int i = 10
                //int i = 10;  // first index of Channel number - First MIPI pair sclk

                int i = 20;  // first index of Channel number - First MIPI pair sclk
                foreach (MipiPinNames mipichans in allMipiPinNames)
                {
                    PinNamesAndChans[mipichans.SclkPinName] = mipichans.SclkChanDIO.ToString();
                    i++;
                    PinNamesAndChans[mipichans.SdataPinName] = mipichans.SdataChanDIO.ToString();
                    i++;
                    PinNamesAndChans[mipichans.VioPinName] = mipichans.VioChanDIO.ToString();
                    i++;
                }

                #endregion Start Index of PhysicalPinsDefinition

                // Map extra pins
                PinNamesAndChans[TrigPinName] = "15"; //i.ToString();

                #region TestBoard EEPROM & TEMPERATURE

                // Map extra pins
                PinNamesAndChans[I2CVCCChanName] = "31"; //23
                PinNamesAndChans[I2CSCKChanName] = "16"; //16
                PinNamesAndChans[I2CSDAChanName] = "23"; //17
                PinNamesAndChans[TEMPSENSEI2CVCCChanName] = "20"; //20

                #endregion TestBoard EEPROM & TEMPERATURE

                this.allDutPins = PinNamesAndChans.Keys.ToArray();

                allRffeChans = string.Join(", ", allDutPins);

                string allRffeChansWoVIO = string.Join(",", allDutPins.Where(s => !s.Contains("VIO")));

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
                allRffePinsWoVIO = DIGI.PinAndChannelMap.GetPinSet(allRffeChansWoVIO);
                sclkPins = DIGI.PinAndChannelMap.GetPinSet(allSclkPinNames);
                sdataPins = DIGI.PinAndChannelMap.GetPinSet(allSdataPinNames);
                vioPins = DIGI.PinAndChannelMap.GetPinSet(allVioPinNames);
                trigPin = DIGI.PinAndChannelMap.GetPinSet(TrigPinName);

                #region TestBoard EEPROM & TEMPERATURE

                EEPROMClockRate = 2e5; // This is the Non-Return to Zero rate, Actual Vector rate is 1/2 of this.

                allEEPROMChans = I2CSCKChanName.ToUpper() + "," + I2CSDAChanName.ToUpper() + "," + I2CVCCChanName.ToUpper();
                allTEMPSENSEChans = I2CSCKChanName.ToUpper() + "," + I2CSDAChanName.ToUpper() + "," + TEMPSENSEI2CVCCChanName.ToUpper();

                allEEPROMPins = DIGI.PinAndChannelMap.GetPinSet(allEEPROMChans);
                EEPROMsckPin = DIGI.PinAndChannelMap.GetPinSet(I2CSCKChanName.ToUpper());
                EEPROMVccPin = DIGI.PinAndChannelMap.GetPinSet(I2CVCCChanName.ToUpper());
                allTEMPSENSEPins = DIGI.PinAndChannelMap.GetPinSet(allTEMPSENSEChans);
                TEMPSENSEsckPin = DIGI.PinAndChannelMap.GetPinSet(I2CSCKChanName.ToUpper());
                TEMPSENSEVccPin = DIGI.PinAndChannelMap.GetPinSet(TEMPSENSEI2CVCCChanName.ToUpper());

                #endregion TestBoard EEPROM & TEMPERATURE

                #endregion NI Pin Map Configuration

                #region MIPI Level Configuration

                double vih = MIPI.Lib_Var.DUTMipi.VIH;
                double vil = MIPI.Lib_Var.DUTMipi.VIL;
                double voh = MIPI.Lib_Var.DUTMipi.VOH;// change from 0.9V to 0.6V;
                double vol = MIPI.Lib_Var.DUTMipi.VOL;
                double vtt = MIPI.Lib_Var.DUTMipi.VTT;

                //sclkPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                //sdataPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                //vioPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                trigPin.DigitalLevels.ConfigureVoltageLevels(0.0, 5.0, 0.5, 2.5, 5.0); // Set VST Trigger Channel to 5V logic.  VST's PFI0 VIH is 2.0V, absolute max is 5.5V
                allRffePins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);

                #endregion MIPI Level Configuration

                #region EEPROM Level Configuration

                vih = 5.0;
                vil = 0.0;
                voh = 2.5;
                vol = 0.5;
                vtt = 5.0;
                allEEPROMPins.DigitalLevels.ConfigureVoltageLevels(vil, vih, vol, voh, vtt);
                //EEPROMVccPin.DigitalLevels.Vcom = vtt;

                #endregion EEPROM Level Configuration

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
                Ivi.Driver.PrecisionTimeSpan driveOn;
                Ivi.Driver.PrecisionTimeSpan driveData;
                Ivi.Driver.PrecisionTimeSpan driveReturn;
                Ivi.Driver.PrecisionTimeSpan driveOff;
                Ivi.Driver.PrecisionTimeSpan compareStrobe;
                Ivi.Driver.PrecisionTimeSpan clockRisingEdgeDelay;
                Ivi.Driver.PrecisionTimeSpan clockFallingEdgeDelay;

                #endregion Timing Variable Declarations

                #region MIPI Timing Configuration

                period_dbl = 1.0 / MIPIClockRate;

                CreateTimeSet(timeset: Timeset.MIPI_SCLK_RZ, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 1, clockRisingEdgeDelayRaw: period_dbl / 8); ///RZ
                CreateTimeSet(timeset: Timeset.MIPI_SCLK_NRZ, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 1, driveOffAmp: 1, compareStrobeAmp: 1, clockRisingEdgeDelayRaw: 0, driveallpin: DriveFormat.NonReturn, driveclkpin: DriveFormat.NonReturn); ///NRZ (eg: 6556 style).

                period_dbl = 1.0 / (MIPIClockRate / 2);
                CreateTimeSet(timeset: Timeset.MIPI_SCLK_RZ_HALF, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 2, clockRisingEdgeDelayRaw: period_dbl / 8); ///RZ
                CreateTimeSet(timeset: Timeset.MIPI_SCLK_NRZ_HALF, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 1, driveOffAmp: 1, compareStrobeAmp: 2, clockRisingEdgeDelayRaw: 0, driveallpin: DriveFormat.NonReturn, driveclkpin: DriveFormat.NonReturn); ///NRZ

                period_dbl = 1.0 / (MIPIClockRate / 4);
                CreateTimeSet(timeset: Timeset.MIPI_SCLK_RZ_QUAD, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 4, clockRisingEdgeDelayRaw: period_dbl / 8);
                CreateTimeSet(timeset: Timeset.MIPI_SCLK_NRZ_QUAD, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 1, driveOffAmp: 1, compareStrobeAmp: 4, clockRisingEdgeDelayRaw: 0, driveallpin: DriveFormat.NonReturn, driveclkpin: DriveFormat.NonReturn); ///NRZ

                #endregion MIPI Timing Configuration

                #region MIPI-NFR Timing configuration (Return to Zero format Patterns / 51.2 MHz)
                period_dbl = 1.0 / MIPINFRClockRate;
                CreateTimeSet(timeset: Timeset.MIPI_NFR, period_dbl: period_dbl, driveOnAmp: 0, driveDataAmp: 0, driveReturnAmp: 0.5, driveOffAmp: 1, compareStrobeAmp: 1, clockRisingEdgeDelayRaw: period_dbl / 8);
                #endregion

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

                // Shift all TEMPSENSE SCK edges by 1/4 Period so SDA is stable before clock rising edge
                driveOn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveData = Ivi.Driver.PrecisionTimeSpan.FromSeconds(0.25 * period_dbl);
                driveReturn = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                driveOff = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.25 * period_dbl);
                compareStrobe = Ivi.Driver.PrecisionTimeSpan.FromSeconds(1.15 * period_dbl);
                // Set TEMPSENSE SCK timing
                tsTEMPSENSENRZ.ConfigureDriveEdges(TEMPSENSEsckPin, DriveFormat.NonReturn, driveOn, driveData, driveReturn, driveOff);

                #endregion TEMPSENSE Timing Configuration

                #region Configure 6570 for Digital Mode with HighZ Termination

                if (MIPI.Lib_Var.isVioPpmu)
                {
                    allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                    allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }
                else
                {
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }

                allEEPROMPins.SelectedFunction = SelectedFunction.Digital;
                allEEPROMPins.DigitalLevels.TerminationMode = TerminationMode.HighZ;

                #endregion Configure 6570 for Digital Mode with HighZ Termination

                #region Load Vectors for MIPI Register Write Read

                // Assert that the HSDIO is ready to load MIPI vectors
                usingMIPI = true;

                // Use Tasks
                try
                {
                    bool pass = true;
                    ConcurrentBag<Task> _VectorTaskBags = new ConcurrentBag<Task>();

                    Timeset _Wtimeset = (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ : Timeset.MIPI_SCLK_NRZ);
                    Timeset _Rtimeset = (Lib_Var.DUTMipi.MipiType == "RZ" ? (Lib_Var.DUTMipi.MipiSyncWriteRead == true ? Timeset.MIPI_SCLK_RZ : Timeset.MIPI_SCLK_RZ_HALF) : (Lib_Var.DUTMipi.MipiSyncWriteRead == true ? Timeset.MIPI_SCLK_NRZ : Timeset.MIPI_SCLK_NRZ_HALF));

                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiRegRead(_Wtimeset, _Rtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedRegRead(_Wtimeset, _Rtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedLongRegRead(_Wtimeset, _Rtimeset)));

                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiRegWrite(_Wtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedRegWrite(_Wtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedLongRegWrite(_Wtimeset)));

                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiRegWriteRead(_Wtimeset, _Rtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedRegWriteRead(_Wtimeset, _Rtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiExtendedLongRegWriteRead(_Wtimeset, _Rtimeset)));

                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiVioOn(_Wtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiHiZ(_Wtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiReset(_Wtimeset)));
                    _VectorTaskBags.Add(Task.Factory.StartNew(() => LoadVector_MipiNFR()));

                    Task.WaitAll(_VectorTaskBags.ToArray());

                    do
                    {
                        if (_VectorTaskBags.TryTake(out Task t))
                            pass &= (t as Task<bool>).Result;
                    } while (!_VectorTaskBags.IsEmpty);

                    //LoadVector_MipiRegIO();

                    //LoadVectorTask = Task.Factory.StartNew(LoadVector_MipiRegIO);
                    //LoadVectorTask.Wait();

                    //LoadVectorTask = Task.Factory.StartNew(LoadVector_MipiHiZ);
                    //LoadVectorTask.Wait();

                    //LoadVectorTask = Task.Factory.StartNew(LoadVector_MipiReset);
                    //LoadVectorTask.Wait();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Load Vector : " + ex);
                }

#if (false)
                LoadVector_MipiRegIO();
                LoadVector_MipiHiZ();
                LoadVector_MipiReset();
#endif

                #endregion Load Vectors for MIPI Register Write Read

                // TDR
                if (false)
                {
                    DIGI.Timing.TdrEndpointTermination = TdrEndpointTermination.TdrToOpenCircuit;
                    Ivi.Driver.PrecisionTimeSpan[] offsets = allRffePins.Tdr(false);
                    allRffePins.ApplyTdrOffsets(offsets);
                }
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
                ts.ConfigureDriveEdges(pinSet: sclkPins, format: driveclkpin, driveOnEdge: driveOn + clockRisingEdgeDelay, driveDataEdge: driveData + clockRisingEdgeDelay, driveReturnEdge: driveclkpin == DriveFormat.NonReturn ? _ZeroTimeSpan : driveReturn + clockFallingEdgeDelay, driveOffEdge: driveOff + clockFallingEdgeDelay, driveData2Edge: _ZeroTimeSpan, driveReturn2Edge: _ZeroTimeSpan);
                ts.ConfigureCompareEdgesStrobe(pinSet: sclkPins, compareEdge: compareStrobe, compare2Edge: _ZeroTimeSpan);
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
            public bool LoadVector_MipiHiZ(Timeset WriteTimeset)
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
                    if (!this.GenerateAndLoadPattern(HiZ.ToLower(), pins.ToArray(), pattern, forceDigiPatRegeneration, WriteTimeset))
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
            public bool LoadVector_MipiReset(Timeset WriteTimeset)
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

                    //modified - Shaz 17Jan2019 because of added pin for TestBoard EEPROM and TempSensor
                    //// Generate and load Pattern from the formatted array.
                    //if (!this.GenerateAndLoadPattern(Reset.ToLower(), allDutPins, pattern, forceDigiPatRegeneration, Timeset.MIPI))
                    //{
                    //    throw new Exception("Compile Failed");
                    //}

                    // Generate and load Pattern from the formatted array.
                    if (!this.GenerateAndLoadPattern(Reset.ToLower(), pins.ToArray(), pattern, forceDigiPatRegeneration, WriteTimeset))
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

            public bool LoadVector_MipiVioOn(Timeset WriteTimeset)
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Generate MIPI / RFFE VIO Off Waveform

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
                        patternvector[0].AddRange(new string[] { "0", "0", "1" });
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
                    if (!this.GenerateAndLoadPattern(VIOON.ToLower(), pins.ToArray(), pattern, forceDigiPatRegeneration, WriteTimeset))
                    {
                        throw new Exception("Compile Failed");
                    }

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi VIO On vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                Timeset _Wtimeset = (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ : Timeset.MIPI_SCLK_NRZ);
                Timeset _Rtimeset = (Lib_Var.DUTMipi.MipiType == "RZ" ? (Lib_Var.DUTMipi.MipiSyncWriteRead == true ? Timeset.MIPI_SCLK_RZ : Timeset.MIPI_SCLK_RZ_HALF) : (Lib_Var.DUTMipi.MipiSyncWriteRead == true ? Timeset.MIPI_SCLK_NRZ : Timeset.MIPI_SCLK_NRZ_HALF));

                // Read pattern
                pass &= LoadVector_MipiRegRead(_Wtimeset, _Rtimeset);
                pass &= LoadVector_MipiExtendedRegRead(_Wtimeset, _Rtimeset);
                pass &= LoadVector_MipiExtendedLongRegRead(_Wtimeset, _Rtimeset);

                // Write pattern
                pass &= LoadVector_MipiRegWrite(_Wtimeset);
                pass &= LoadVector_MipiExtendedRegWrite(_Wtimeset);
                pass &= LoadVector_MipiExtendedLongRegWrite(_Wtimeset);

                // Write + read in one pattern
                pass &= LoadVector_MipiRegWriteRead(_Wtimeset, _Rtimeset);
                pass &= LoadVector_MipiExtendedRegWriteRead(_Wtimeset, _Rtimeset);
                pass &= LoadVector_MipiExtendedLongRegWriteRead(_Wtimeset, _Rtimeset);

                pass &= LoadVector_MipiVioOn(_Wtimeset);
                pass &= LoadVector_MipiHiZ(_Wtimeset);
                pass &= LoadVector_MipiReset(_Wtimeset);

                pass &= LoadVector_MipiNFR();

                return pass;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the non-extended register read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiRegRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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

                                #endregion RegisterRead pattern
                            };

                        if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterRead" + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                        {
                            throw new Exception("Compile Failed");
                        }

                        //if (!this.GenerateAndLoadPattern("OTPRegisterRead" + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                        //{
                        //    throw new Exception("Compile Failed");
                        //}

                        HSDIO.datalogResults["RegisterRead" + "Pair" + p.ToString()] = false;
                        //HSDIO.datalogResults["OTPRegisterRead" + "Pair" + p.ToString()] = false;
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
            private bool LoadVector_MipiRegWrite(Timeset WriteTimeSet)
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

#endregion RegisterWrite Pattern
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

#endregion Trigger, Idle Halt
                            };

                        // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                        List<string[]> pattern = new List<string[]> { };
                        pattern = pattern.Concat(patternStart).ToList();

                        for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                            pattern = pattern.Concat(triggerDelay).ToList();

                        pattern = pattern.Concat(trigger).ToList();

                        if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterWrite" + "Pair" + p.ToString(), pins, pattern, true, WriteTimeSet))
                        {
                            throw new Exception("Compile Failed");
                        }
                        //if (!this.GenerateAndLoadPattern("OTPRegisterWrite" + "Pair" + p.ToString(), pins, pattern, true, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_HALF :Timeset.MIPI_SCLK_NRZ_HALF)))
                        //{
                        //    throw new Exception("Compile Failed");
                        //}

                        HSDIO.datalogResults["RegisterWrite" + "Pair" + p.ToString()] = false;
                        //HSDIO.datalogResults["OTPRegisterWrite" + "Pair" + p.ToString()] = false;
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
            private bool LoadVector_MipiExtendedRegWrite(Timeset WriteTimeSet)
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

#endregion ExtendedRegisterWrite Pattern
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

#endregion Write Data...
                            };
                        List<string[]> busPark = new List<string[]>
                            {
#region Bus Park

                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""}

#endregion Bus Park
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

#endregion Trigger, Idle Halt
                            };

                        for (int i = 1; i <= 16; i++)
                        //for (int i = 1; i <= 2; i++)
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

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeSet))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString());
                            }
                            //if (!this.GenerateAndLoadPattern("OTPExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_HALF : Timeset.MIPI_SCLK_NRZ_HALF)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString()] = false;
                            //HSDIO.datalogResults["OTPExtendedRegisterWrite" + i.ToString() + "Pair" + p.ToString()] = false;
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
            /// Internal Function: Used to generate and load the extended long register write pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedLongRegWrite(Timeset WriteTimeSet)
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
#region ExtendedLongRegisterWrite Pattern

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
                                new string[] {"source", "1", "D", "1", "0", "Extended Long Register Write Command (00110)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 15"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 14"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 13"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 12"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 11"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 10"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 8"},
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

#endregion ExtendedLongRegisterWrite Pattern
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

#endregion Write Data...
                            };
                        List<string[]> busPark = new List<string[]>
                            {
#region Bus Park

                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""}

#endregion Bus Park
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

#endregion Trigger, Idle Halt
                            };

                        for (int i = 1; i <= 8; i++)
                        //for (int i = 1; i <= 2; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedLongRegisterWrite" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"}
                                };
                            pattern = pattern.Concat(patternStart).ToList();

                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(writeData).ToList();

                            pattern = pattern.Concat(busPark).ToList();

                            for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                                pattern = pattern.Concat(triggerDelay).ToList();

                            pattern = pattern.Concat(trigger).ToList();

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeSet))
                            {
                                throw new Exception("Compile Failed: ExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString());
                            }
                            //if (!this.GenerateAndLoadPattern("OTPExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_HALF : Timeset.MIPI_SCLK_NRZ_HALF)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString()] = false;
                            // HSDIO.datalogResults["OTPExtendedLongRegisterWrite" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedLongRegisterWrite vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the extended register read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedRegRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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

#endregion ExtendedRegisterRead Pattern
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

#endregion Read Data...
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
#region Bus Park, Idle, and Halt

                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "0", "0", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}

#endregion Bus Park, Idle, and Halt
                            };

                        for (int i = 1; i <= 16; i++)
                        //for (int i = 1; i <= 2; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                new string[] {"source_start(SrcExtendedRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure source"},
                                new string[] {"capture_start(CapExtendedRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
                                //new string[] {"source_start(SrcExtendedRegisterReadPair" + p.ToString() + ")", "0", "0", "1", "X", "Configure source"},
                                //new string[] {"capture_start(CapExtendedRegisterReadPair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
                            };
                            pattern = pattern.Concat(patternStart).ToList();
                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(readData).ToList();
                            pattern = pattern.Concat(busParkIdleHalt).ToList();

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns

                            if (!this.GenerateAndLoadPattern("ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString());
                            }
                            //if (!this.GenerateAndLoadPattern("OTPExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString()] = false;
                            //HSDIO.datalogResults["OTPExtendedRegisterRead" + i.ToString() + "Pair" + p.ToString()] = false;
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
            /// Internal Function: Used to generate and load the extended long register read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedLongRegRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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
#region ExtendedLongRegisterRead Pattern

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
                                new string[] {"source", "1", "D", "1", "X", "Extended Long Register Read Command (00111)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 15"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 14"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 13"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 12"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 11"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 10"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 8"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
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

#endregion ExtendedLongRegisterRead Pattern
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

#endregion Read Data...
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
#region Bus Park, Idle, and Halt

                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "0", "0", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}

#endregion Bus Park, Idle, and Halt
                            };

                        for (int i = 1; i <= 8; i++)
                        //for (int i = 1; i <= 2; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedLongRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure source"},
                                    new string[] {"capture_start(CapExtendedLongRegisterRead" + i + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
                                };
                            pattern = pattern.Concat(patternStart).ToList();
                            for (int j = 0; j < i; j++)
                                pattern = pattern.Concat(readData).ToList();
                            pattern = pattern.Concat(busParkIdleHalt).ToList();

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                            {
                                throw new Exception("Compile Failed: ExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString());
                            }
                            //if (!this.GenerateAndLoadPattern("OTPExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString()] = false;
                            //HSDIO.datalogResults["OTPExtendedLongRegisterRead" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedLongRegisterRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the non-extended register write and read pattern in one pattern file
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiRegWriteRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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

#endregion RegisterWrite Pattern
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

#endregion Trigger
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

#endregion RegisterRead pattern, Idle halt
                            };

                        // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                        List<string[]> pattern = new List<string[]> { };
                        pattern = pattern.Concat(patternStart).ToList();

                        for (int ff = 0; ff < this.regWriteTriggerCycleDelay; ff++)
                            pattern = pattern.Concat(triggerDelay).ToList();

                        pattern = pattern.Concat(trigger).ToList();

                        pattern = pattern.Concat(readpattern).ToList();

                        if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("RegisterWriteRead" + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                        {
                            throw new Exception("Compile Failed");
                        }

                        //if (!this.GenerateAndLoadPattern("OTPRegisterWriteRead" + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                        //{
                        //    throw new Exception("Compile Failed");
                        //}

                        HSDIO.datalogResults["RegisterWriteRead" + "Pair" + p.ToString()] = false;
                        //HSDIO.datalogResults["OTPRegisterWriteRead" + "Pair" + p.ToString()] = false;
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
            private bool LoadVector_MipiExtendedRegWriteRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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

#endregion ExtendedRegisterWrite Pattern
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

#endregion Write Data...
                            };
                        List<string[]> busPark = new List<string[]>
                            {
#region Bus Park

                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""}

#endregion Bus Park
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

#endregion Trigger
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

#endregion ExtendedRegisterRead Pattern
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

#endregion Read Data...
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
#region Bus Park, Idle, and Halt

                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"end_loop(cmd2start)", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "0", "0", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}

#endregion Bus Park, Idle, and Halt
                            };

                        for (int i = 1; i <= 16; i++)
                        //for (int i = 1; i <= 2; i++)
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

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                            {
                                throw new Exception("Compile Failed: ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString());
                            }

                            //if (!this.GenerateAndLoadPattern("OTPExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString()] = false;
                            //HSDIO.datalogResults["OTPExtendedRegisterWriteRead" + i.ToString() + "Pair" + p.ToString()] = false;
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
            /// Internal Function: Used to generate and load the extended long register write and read pattern in one pattern file
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_MipiExtendedLongRegWriteRead(Timeset WriteTimeset, Timeset ReadTimeSet)
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
                        // Generate MIPI / RFFE Extended Long Register Write Patterns
                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName, TrigPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
#region ExtendedLongRegisterWrite Pattern

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
                                new string[] {"source", "1", "D", "1", "0", "Extended Long Register Write Command (00110)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 15"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 14"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 13"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 12"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 11"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 10"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 8"},
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

#endregion ExtendedLongRegisterWrite Pattern
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

#endregion Write Data...
                            };
                        List<string[]> busPark = new List<string[]>
                            {
#region Bus Park

                                new string[] {"", "1", "0", "1", "0", "Bus Park"},
                                new string[] {"end_loop(cmdstart)", "0", "-", "1", "-", ""}

#endregion Bus Park
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

#endregion Trigger
                            };
                        List<string[]> patternReadStart = new List<string[]>
                            {
#region ExtendedLongRegisterRead Pattern

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
                                new string[] {"source", "1", "D", "1", "X", "Extended Long Register Read Command (00111)"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", ""},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 2"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 1"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Byte Count 0"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "X", "Parity"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 15"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 14"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 13"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 12"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 11"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 10"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 9"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Address 8"},
                                new string[] {"", "0", "-", "1", "-", ""},
                                new string[] {"source", "1", "D", "1", "0", "Parity"},
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

#endregion ExtendedLongRegisterRead Pattern
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

#endregion Read Data...
                            };
                        List<string[]> busParkIdleHalt = new List<string[]>
                            {
#region Bus Park, Idle, and Halt

                                new string[] {"", "1", "0", "1", "X", "Bus Park"},
                                new string[] {"end_loop(cmd2start)", "0", "-", "1", "-", ""},
                                new string[] {"capture_stop", "0", "0", "1", "X", ""},
                                new string[] {"repeat(300)", "0", "0", "1", "X", "Idle"},
                                new string[] {"halt", "0", "0", "1", "X", ""}

#endregion Bus Park, Idle, and Halt
                            };

                        // Extended Long only supports up to 8 bytes of data
                        for (int i = 1; i <= 8; i++)
                        //for (int i = 1; i <= 2; i++)
                        {
                            // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                            List<string[]> pattern = new List<string[]>
                                {
                                    new string[] {"source_start(SrcExtendedLongRegisterWriteRead"  + i.ToString()  + "Pair" + p.ToString() + ")", "0", "0", "1", "0", "Configure source"},
                                    new string[] {"capture_start(CapExtendedLongRegisterWriteRead"  + i.ToString()  + "Pair" + p.ToString() + ")", "0", "0", "1", "X", "Configure capture"}
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

                            if (Lib_Var.DUTMipi.MipiType == "RZ") pattern = RemoveNRZvector(pattern);

                            // Generate and load Pattern from the formatted array.
                            // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                            if (!this.GenerateAndLoadPattern("ExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, WriteTimeset, (Lib_Var.DUTMipi.MipiType == "RZ" ? ReadTimeSet : WriteTimeset)))
                            {
                                throw new Exception("Compile Failed: ExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString());
                            }
                            //if (!this.GenerateAndLoadPattern("OTPExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString(), pins, pattern, true, ReadTimeSet, (Lib_Var.DUTMipi.MipiType == "RZ" ? Timeset.MIPI_SCLK_RZ_QUAD : ReadTimeSet)))
                            //{
                            //    throw new Exception("Compile Failed: OTPExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString());
                            //}
                            HSDIO.datalogResults["ExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString()] = false;
                            //HSDIO.datalogResults["OTPExtendedLongRegisterWriteRead" + i.ToString() + "Pair" + p.ToString()] = false;
                        }
                        p++;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi ExtendedLongRegisterWriteRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            public void MipiVIOOn()
            {
                SendVector(HSDIO.VIOON.ToLower());
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
            /// This supports extended long, extended and non-extended register write.
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register address to write (hex)</param>
            /// <param name="data_hex">The data to write into the specified register in Hex.  Note:  Maximum # of bytes to write is 1 byte for non-extendedand, 16 for extended, 8 for extended long.</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public void RegWrite(int pair, string slaveAddress_hex, string registerAddress_hex, string data_hex, bool sendTrigger = false, bool IsOTPReg = false)
            {
                try
                {
                    #region Configure pins and trigger

                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    if (MIPI.Lib_Var.isVioPpmu)
                    {
                        allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                        allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

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

                    #endregion Configure pins and trigger

                    // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                    if (data_hex.Length % 2 == 1)
                        data_hex = data_hex.PadLeft(data_hex.Length + 1, '0');

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };
                    bool extendedWrite = Convert.ToInt32(registerAddress_hex, 16) > 0x1F;    // any register address > 5 bits requires extended read
                    bool isExtendedLong = Convert.ToInt32(registerAddress_hex, 16) > 0xFF;    // any register address > 8 bits requires extended long

                    uint writeByteCount = extendedWrite ? (uint)(data_hex.Length / 2) : 1;

                    string nameInMemory = extendedWrite ? "ExtendedRegisterWrite" + writeByteCount.ToString() : "RegisterWrite";
                    nameInMemory = isExtendedLong ? "ExtendedLongRegisterWrite" : nameInMemory;

                    if (extendedWrite)
                        nameInMemory += writeByteCount.ToString() + "Pair" + pair.ToString();
                    else
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
                        if (!isExtendedLong)
                        {
                            // Build extended write command data, setting write byte count and register address.
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
                        else
                        {
                            // Build extended long write command data, setting read byte count and register address.
                            // Note, write byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(writeByteCount - 1, 16), Command.EXTENDEDREGISTERWRITELONG);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits

                            // Split register address to two 9 bits packets (each have own parity)
                            uint regAddr = Convert.ToUInt32(registerAddress_hex, 16);
                            dataArray[2] = calculateParity(regAddr >> 8);
                            dataArray[3] = calculateParity(regAddr & 0xFF);
                        }
                    }

                    //string Labelname = nameInMemory;
                    if (Lib_Var.DUTMipi.MipiType == "RZ" && IsOTPReg && (Lib_Var.DUTMipi.MipiClockSpeed > Lib_Var.DUTMipi.MipiOTPBurnClockSpeed)) nameInMemory = "OTP" + nameInMemory;

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
            /// This supports extended long, extended and non-extended register read, but only read 1 data byte.
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register address to read (hex)</param>
            /// <returns>The value of the specified register in Hex</returns>
            public string RegRead(int pair, string slaveAddress_hex, string registerAddress_hex, bool IsOTPReg = false)
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    if (MIPI.Lib_Var.isVioPpmu)
                    {
                        allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                        allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };
                    bool extendedRead = Convert.ToInt32(registerAddress_hex, 16) > 0x1F;    // any register address > 5 bits requires extended read
                    bool isExtendedLong = Convert.ToInt32(registerAddress_hex, 16) > 0xFF;    // any register address > 8 bits requires extended long
                    uint readByteCount = 1;
                    string nameInMemory = extendedRead ? "ExtendedRegisterRead" : "RegisterRead";
                    nameInMemory = isExtendedLong ? "ExtendedLongRegisterRead" : nameInMemory;

                    if (extendedRead)
                        nameInMemory += readByteCount.ToString() + "Pair" + pair.ToString();
                    else
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
                        if (!isExtendedLong)
                        {
                            // Build extended read command data, setting read byte count and register address.
                            // Note, read byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(readByteCount - 1, 16), Command.EXTENDEDREGISTERREAD);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            dataArray[2] = (uint)(calculateParity(Convert.ToUInt16(registerAddress_hex, 16)));  // Final 9 bits to contains the address (for extended read) + parity.
                        }
                        else
                        {
                            // Build extended long read command data, setting read byte count and register address.
                            // Note, read byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(readByteCount - 1, 16), Command.EXTENDEDREGISTERREADLONG);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            dataArray[0] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            dataArray[1] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits

                            // Split register address to two 9 bits packets (each have own parity)
                            uint regAddr = Convert.ToUInt32(registerAddress_hex, 16);
                            dataArray[2] = calculateParity(regAddr >> 8);
                            dataArray[3] = calculateParity(regAddr & 0xFF);
                        }
                    }

                    //string Labelname = nameInMemory;
                    if (Lib_Var.DUTMipi.MipiType == "RZ" && IsOTPReg && (Lib_Var.DUTMipi.MipiClockSpeed > Lib_Var.DUTMipi.MipiOTPReadClockSpeed)) nameInMemory = "OTP" + nameInMemory;

                    // Configure to source data
                    DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, (uint)(extendedRead ? 9 : 16), BitOrder.MostSignificantBitFirst);
                    try
                    {
                        DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, dataArray);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

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
            /// Dynamic Register Write and Read function for one or multiple register adddresses (extended and extended long supported).
            /// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="registerAddress_hex">The register addresses to write (hex)</param>
            /// <param name="data_hex">The data to write into the respective specified register in Hex. </param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            public string RegWriteReadAny(int pair, string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool sendTrigger = false, bool IsOTPReg = false)
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

                    for (int i = 0; i < wfms.Count; i++)
                    {
                        if (i != 0)
                            returnval += ",";
                        if (i != wfms.Count() - 1)
                            returnval += RegWriteRead_Initiate(pair, slaveAddress_hex, wfms[i], false, IsOTPReg);
                        else
                            returnval += RegWriteRead_Initiate(pair, slaveAddress_hex, wfms[i], sendTrigger, IsOTPReg);
                    }

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
            /// Dynamic Register Write and Read function for one or multiple register adddresses (extended and extended long supported).
            /// This uses NI 6570 source memory to dynamically change the register address and write values in the pattern, and then read the register value to verify.
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="mipiCmd">MIPI command in text pattern such as "1C:38 00:01 02:02", where hex before colon is register address, after colon is data.</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            /// <returns></returns>
            public string RegWriteReadAny(int pair, string slaveAddress_hex, string mipiCmd, bool sendTrigger = false, bool IsOTPReg = false)
            {
                List<string> regAddr = new List<string>();
                List<string> data = new List<string>();

                Match match = Regex.Match(mipiCmd, @"([A-F0-9]+):([A-F0-9]+)");
                if (match.Success)
                {
                    regAddr.Add(match.Groups[1].Value);
                    data.Add(match.Groups[2].Value);

                    match = match.NextMatch();

                    while (match.Success)
                    {
                        regAddr.Add(match.Groups[1].Value);
                        data.Add(match.Groups[2].Value);
                        match = match.NextMatch();
                    }
                }

                return RegWriteReadAny(pair, slaveAddress_hex, regAddr.ToArray(), data.ToArray(), sendTrigger, IsOTPReg);
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

            #endregion Deprecated functions

            /// <summary>
            /// To burst the MIPI Write + Read pattern using the given sourcing waveform w
            /// </summary>
            /// <param name="pair">The MIPI pair number to write</param>
            /// <param name="slaveAddress_hex">The slave address to write (hex)</param>
            /// <param name="w">MIPI waveform to source</param>
            /// <param name="sendTrigger">If True, this function will send either a PXI Backplane Trigger, a Digital Pin Trigger, or both based on the configuration in the NI6570 constructor.</param>
            /// <returns>The captured data read from the register. Use this to verify with what you write</returns>
            private string RegWriteRead_Initiate(int pair, string slaveAddress_hex, MipiWaveforms w, bool sendTrigger = false, bool IsOTPReg = false)
            {
                #region Configure pins and trigger

                // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                if (MIPI.Lib_Var.isVioPpmu)
                {
                    allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                    allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }
                else
                {
                    allRffePins.SelectedFunction = SelectedFunction.Digital;
                    allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                }

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

                #endregion Configure pins and trigger

                // Assume all write byte count are same
                string nameInMemory = w.isExtended ? "ExtendedRegisterWriteRead" : "RegisterWriteRead";
                nameInMemory = w.isExtendedLong ? "ExtendedLongRegisterWriteRead" : nameInMemory;
                if (w.isExtended)
                    nameInMemory += w.writeByteCount[0].ToString() + "Pair" + pair.ToString();
                else
                    nameInMemory += "Pair" + pair.ToString();

                //string Labelname = nameInMemory;
                if (Lib_Var.DUTMipi.MipiType == "RZ" && IsOTPReg && (Lib_Var.DUTMipi.MipiClockSpeed != Lib_Var.DUTMipi.MipiOTPBurnClockSpeed)) nameInMemory = "OTP" + nameInMemory;

                // Configure 6570 to source data calculated above
                DIGI.SourceWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Src" + nameInMemory, SourceDataMapping.Broadcast, 9, BitOrder.MostSignificantBitFirst);
                DIGI.SourceWaveforms.WriteBroadcast("Src" + nameInMemory, w.dataArray);

                // Configure to capture data (9-bits each, including parity)
                DIGI.CaptureWaveforms.CreateSerial(allMipiPinNames[pair].SdataPinName, "Cap" + nameInMemory, 9, BitOrder.MostSignificantBitFirst);

                // Write Sequencer Register reg0 = number of register address to access
                DIGI.PatternControl.WriteSequencerRegister("reg0", w.cmdCount);

                // Choose Pattern to Burst
                DIGI.PatternControl.StartLabel = nameInMemory;

                // Burst Pattern
                DIGI.PatternControl.Initiate();

                // Wait for Pattern Burst to complete
                DIGI.PatternControl.WaitUntilDone(new TimeSpan(0, 0, 10));

                // Get PassFail Results for site 0
                Int64[] failureCount = sdataPins.GetFailCount();
                NumExecErrors = (int)failureCount[0];
                if (debug) Console.WriteLine("Pair " + pair + " Slave " + slaveAddress_hex + ", RegWrite " + string.Join(" ", w.registerAddress_hex) + " Bit Errors: " + NumExecErrors.ToString());

                return FetchCaptureWaveform(w, nameInMemory);
            }

            private string FetchCaptureWaveform(MipiWaveforms w, string nameInMemory)
            {
                int totalReadByteCount = w.readByteCount.Sum((x) => Convert.ToInt32(x));

                // Retreive captured waveform
                uint[][] capData = new uint[totalReadByteCount][];

                DIGI.CaptureWaveforms.Fetch("", "Cap" + nameInMemory, totalReadByteCount, new TimeSpan(0, 0, 0, 0, 100), ref capData);

                StringBuilder returnval = new StringBuilder();
                for (int i = 0, j = 0; i < totalReadByteCount; j++)
                {
                    if (i != 0)
                        returnval.Append(",");

                    for (int k = 0; k < w.readByteCount[j]; k++, i++)
                    {
                        // Remove parity bit in the last of each sample (9-bits long including parity)
                        // Then, convert to string in Hex expression
                        returnval.AppendFormat("{0:X2}", (capData[0][i] >> 1) & 0xFF);
                    }
                }

                return returnval.ToString();
            }

            /// <summary>
            /// Used by RegWriteRead
            /// </summary>
            private class MipiWaveforms
            {
                public uint[] dataArray = new uint[512];
                public bool isExtended = false;
                public bool isExtendedLong = false;
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
            private static MipiWaveforms calcMipiWaveforms(string slaveAddress_hex, string[] registerAddress_hex, string[] data_hex, bool isExtended, bool isExtendedLong = false)
            {
                MipiWaveforms w = new MipiWaveforms();
                w.isExtended = isExtended;
                w.isExtendedLong = isExtendedLong;
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
                        w.writeByteCount[i] = (uint)(data_hex[i].Length / 2);
                    }

                    #endregion Non-Extended RegWrite

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

                    #endregion Non-Extended RegRead
                }
                else
                {
                    if (!isExtendedLong)
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

                        #endregion Extended RegWrite

                        #region Extended RegRead

                        for (int i = 0; i < registerAddress_hex.Count(); i++)
                        {
                            // Read byte count should be same with write byte count
                            w.readByteCount[i] = w.writeByteCount[i];

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

                        #endregion Extended RegRead
                    }
                    else
                    {
                        int j = 0;

                        #region Extended Long RegWrite

                        for (int i = 0; i < registerAddress_hex.Count(); i++)
                        {
                            // Pad data_hex string to ensure it always has full bytes (eg: "0F" instead of "F")
                            if (data_hex[i].Length % 2 == 1)
                                data_hex[i] = data_hex[i].PadLeft(data_hex.Length + 1, '0');

                            w.writeByteCount[i] = (uint)(data_hex[i].Length / 2);

                            // Build extended write command data, setting read byte count and register address.
                            // Note, write byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(w.writeByteCount[i] - 1, 16), Command.EXTENDEDREGISTERWRITELONG);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            w.dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            j++;
                            w.dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            j++;

                            // Split register address to two 9 bits packets (each have own parity)
                            uint regAddr = Convert.ToUInt32(registerAddress_hex[i], 16);
                            w.dataArray[j] = calculateParity(regAddr >> 8);
                            j++;
                            w.dataArray[j] = calculateParity(regAddr & 0xFF);
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

                        #endregion Extended Long RegWrite

                        #region Extended Long RegRead

                        for (int i = 0; i < registerAddress_hex.Count(); i++)
                        {
                            // Read byte count should be same with write byte count
                            w.readByteCount[i] = w.writeByteCount[i];

                            // Build extended long read command data, setting read byte count and register address.
                            // Note, read byte count is 0 indexed.
                            uint cmdBytesWithParity = generateRFFECommand(slaveAddress_hex, Convert.ToString(w.readByteCount[i] - 1, 16), Command.EXTENDEDREGISTERREADLONG);
                            // Split data into array of data, all must be same # of bits (9) which must be specified when calling CreateSerial
                            w.dataArray[j] = cmdBytesWithParity >> 9; // first 8 bits (plus one extra 0 so this packet is also 9 bits)
                            j++;
                            w.dataArray[j] = cmdBytesWithParity & 0x1FF; // 2nd 9 bits
                            j++;

                            // Split register address to two 9 bits packets (each have own parity)
                            uint regAddr = Convert.ToUInt32(registerAddress_hex[i], 16);
                            w.dataArray[j] = calculateParity(regAddr >> 8);
                            j++;
                            w.dataArray[j] = calculateParity(regAddr & 0xFF);
                            j++;
                        }

                        #endregion Extended Long RegRead
                    }
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
                int lastWfmType = 0;

                // Define a temporary tuple lists of <registerAddress, data_hex, writeByteCount>
                var TupleList = new List<Tuple<string, string, uint>>();

                // Create a lambda function to get Mipi Waveform from Tuple list
                var getWfm = new Func<List<Tuple<string, string, uint>>, bool, bool, MipiWaveforms>(
                               (T, isEx, isExLong)
                                => (calcMipiWaveforms(slaveAddress_hex,
                                                      T.Select((r) => (r.Item1)).ToArray(),
                                                      T.Select(r => r.Item2).ToArray(), isEx, isExLong)));

                for (int i = 0; i < registerAddress_hex.Count(); i++)
                {
                    bool isExtended = Convert.ToInt32(registerAddress_hex[i], 16) > 0x1F;    // any register address > 5 bits requires extended
                    int wfmType = isExtended ? 1 : 0;

                    bool isExtendedLong = Convert.ToInt32(registerAddress_hex[i], 16) > 0xFF;    // any register address > 8 bits requires extended long
                    wfmType = isExtendedLong ? 3 : wfmType;

                    uint writeByteCount = (uint)(data_hex[i].Length / 2);

                    if (i == 0)
                    {
                        TupleList.Add(new Tuple<string, string, uint>(registerAddress_hex[i], data_hex[i], writeByteCount));
                        lastWfmType = wfmType;
                    }
                    else if (lastWfmType == wfmType && TupleList[0].Item3 == writeByteCount)
                    {
                        // Add to same TupleGroup
                        TupleList.Add(new Tuple<string, string, uint>(registerAddress_hex[i], data_hex[i], writeByteCount));
                    }
                    else
                    {
                        switch (lastWfmType)
                        {
                            case 0:   // Non-extended
                                wfms.Add(getWfm(TupleList, false, false));
                                break;

                            case 1:   // Extended
                                wfms.Add(getWfm(TupleList, true, false));
                                break;

                            default:  // Extended Long
                                wfms.Add(getWfm(TupleList, true, true));
                                break;
                        }
                        TupleList.Clear();

                        TupleList.Add(new Tuple<string, string, uint>(registerAddress_hex[i], data_hex[i], writeByteCount));
                        lastWfmType = wfmType;
                    }
                }

                #region Clear the items left in TupleGroup

                if (TupleList.Count() != 0)
                {
                    switch (lastWfmType)
                    {
                        case 0:   // Non-extended
                            wfms.Add(getWfm(TupleList, false, false));
                            break;

                        case 1:   // Extended
                            wfms.Add(getWfm(TupleList, true, false));
                            break;

                        default:  // Extended Long
                            wfms.Add(getWfm(TupleList, true, true));
                            break;
                    }
                }

                #endregion Clear the items left in TupleGroup

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

            #region move to ppmu class

            /// <summary>
            /// Source a current to specified pin
            /// </summary>
            /// <param name="PinName"></param>
            /// <param name="currentForce"></param>
            //public void ForceCurrent(string PinName, double currentForce)
            //{
            //    try
            //    {
            //        // Configure for PPMU measurements, Output Current, Measure Voltage
            //        // Configure for PPMU Measurements
            //        NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
            //        pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
            //        pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCCurrent;

            //        // Using the requested current to decide the current level range from the values supported for private release of 6570
            //        double range = currentForce;
            //        if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
            //        else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
            //        else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

            //        // Set the current level range and voltage limits
            //        pin.Ppmu.DCCurrent.CurrentLevelRange = Math.Abs(range);
            //        pin.Ppmu.DCCurrent.VoltageLimitHigh = 1;    //Original: 5
            //        pin.Ppmu.DCCurrent.VoltageLimitLow = -2;
            //        // Configure Current Level and begin Sourcing
            //        pin.Ppmu.DCCurrent.CurrentLevel = currentForce;
            //        pin.Ppmu.Source();
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.ToString(), "ForceCurrent");
            //    }
            //}

            ///// <summary>
            ///// Set specified pin to PPMU mode, and force a voltage
            ///// </summary>
            ///// <param name="PinName"></param>
            ///// <param name="voltsForce"></param>
            ///// <param name="currentLimit"></param>
            //public void ForceVoltage(string PinName, double voltsForce, double currentLimit)
            //{
            //    try
            //    {
            //        // Configure 6570 for PPMU measurements, Output Voltage, Measure Current
            //        NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
            //        pin.SelectedFunction = NationalInstruments.ModularInstruments.NIDigital.SelectedFunction.Ppmu;
            //        pin.Ppmu.OutputFunction = NationalInstruments.ModularInstruments.NIDigital.PpmuOutputFunction.DCVoltage;
            //        // Force Voltage Configure
            //        pin.Ppmu.DCVoltage.VoltageLevel = voltsForce;

            //        // Using the requested current limit to decide the current level range from the values supported for private release of 6570
            //        double range = currentLimit;
            //        if (Math.Abs(range) < 2e-6) { range = 2e-6; } // +-2uA
            //        else if (Math.Abs(range) < 32e-6) { range = 32e-6; } // +-32uA
            //        else if (Math.Abs(range) < 128e-6) { range = 128e-6; } // +-128uA
            //        else if (Math.Abs(range) < 2e-3) { range = 2e-3; } // +-2mA
            //        else if (Math.Abs(range) < 32e-3) { range = 32e-3; } // +-32mA}

            //        pin.Ppmu.DCCurrent.CurrentLevelRange = range;
            //        pin.Ppmu.DCVoltage.CurrentLimitRange = range;
            //        // Perform Voltage Force
            //        pin.Ppmu.Source();
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.ToString(), "ForceVoltage");
            //    }
            //}

            ///// <summary>
            ///// Measure Current at specified pin
            ///// </summary>
            ///// <param name="PinName">Name of listed pin in MIPI.allMIPIPinNames</param>
            ///// <param name="NumAverages"></param>
            ///// <param name="Result">Result of measured current</param>
            //public void MeasureCurrent(string PinName, int NumAverages, ref double Result)
            //{
            //    try
            //    {
            //        double[] meas = new double[32];

            //        // Measure Current
            //        NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
            //        //CM Edited: Added aperture time configuration
            //        pin.Ppmu.ConfigureApertureTime(0.0001 * (double)NumAverages, NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);

            //        meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Current);
            //        // (Multiple measurement will result if the PinName is a set of multiple pin names.
            //        //  Here, even user assign multiple pin names, we only take the first and ignore the rest.
            //        //  You can change this behaviour by returning all elements in meas array)
            //        Result = meas[0];
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.ToString(), "MeasureCurrent");
            //    }
            //}

            ///// <summary>
            ///// Measure voltage at specified pin
            ///// </summary>
            ///// <param name="PinName">Name of listed pin in MIPI.allMIPIPinNames</param>
            ///// <param name="NumAverages"></param>
            ///// <param name="Result">Result of measured voltage</param>
            //public void MeasureVoltage(string PinName, int NumAverages, ref double Result)
            //{
            //    try
            //    {
            //        double[] meas = new double[32];
            //        // Configure Number of Averages by setting the Apperture Time
            //        NationalInstruments.ModularInstruments.NIDigital.DigitalPinSet pin = DIGI.PinAndChannelMap.GetPinSet(PinName.ToUpper());
            //        pin.Ppmu.ConfigureApertureTime(0.0020 * (double)(NumAverages), NationalInstruments.ModularInstruments.NIDigital.PpmuApertureTimeUnits.Seconds);

            //        // Measure Voltage
            //        meas = pin.Ppmu.Measure(NationalInstruments.ModularInstruments.NIDigital.PpmuMeasurementType.Voltage);

            //        // Select only the first result
            //        // (Multiple measurement will result if the PinName is a set of multiple pin names.
            //        //  Here, even user assign multiple pin names, we only take the first and ignore the rest.
            //        //  You can change this behaviour by returning all elements in meas array)
            //        Result = meas[0];
            //    }
            //    catch (Exception e)
            //    {
            //        MessageBox.Show(e.ToString(), "MeasureVoltage");
            //    }
            //}

            #endregion move to ppmu class

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
                        if (MIPI.Lib_Var.isVioPpmu)
                        {
                            if (Enum.TryParse(nameInMemory.ToUpper(), out MIPI.Lib_Var.PPMUVioOverrideString vioEnum))
                            {
                                switch (vioEnum)
                                {
                                    case Lib_Var.PPMUVioOverrideString.RESET:
                                        PpmuResourcesLocal["VIOP0"].ForceVoltage(0, 0.032);
                                        PpmuResourcesLocal["VIOP0"].ForceVoltage(MIPI.Lib_Var.DUTMipi.VIOTargetVoltage, 0.032);
                                        PpmuResourcesLocal["VIOP1"].ForceVoltage(0, 0.032);
                                        PpmuResourcesLocal["VIOP1"].ForceVoltage(MIPI.Lib_Var.DUTMipi.VIOTargetVoltage, 0.032);
                                        break;

                                    case Lib_Var.PPMUVioOverrideString.HIZ: // HIZ = VIOOFF
                                        PpmuResourcesLocal["VIOP0"].ForceVoltage(0, 0.032);
                                        PpmuResourcesLocal["VIOP1"].ForceVoltage(0, 0.032);
                                        break;

                                    case Lib_Var.PPMUVioOverrideString.VIOON:
                                        PpmuResourcesLocal["VIOP0"].ForceVoltage(MIPI.Lib_Var.DUTMipi.VIOTargetVoltage, 0.032);
                                        PpmuResourcesLocal["VIOP1"].ForceVoltage(MIPI.Lib_Var.DUTMipi.VIOTargetVoltage, 0.032);
                                        break;
                                }
                                return true;
                            }

                            allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                            allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }
                        else
                        {
                            allRffePins.SelectedFunction = SelectedFunction.Digital;
                            allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }

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
                    if (MIPI.Lib_Var.isVioPpmu)
                    {
                        allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                        allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }
                    else
                    {
                        allRffePins.SelectedFunction = SelectedFunction.Digital;
                        allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                    }

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
                    case Command.EXTENDEDREGISTERWRITELONG:
                        maxRange = 0x07;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x30);
                        break;

                    case Command.EXTENDEDREGISTERREADLONG:
                        maxRange = 0x07;
                        modifiedAddress = Convert.ToByte(maxRange & regAddress | 0x38);
                        break;

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
            public bool GenerateAndLoadPattern(string patternName, string[] vecpins, List<string[]> pattern, bool overwrite, Timeset timeSet, Timeset timeSet_read = Timeset.MIPI_SCLK_RZ_HALF)
            {
                var m_Pattern = pattern;
                var m_vecpins = vecpins;
                var _TargetPins = this.allDutPins;

                #region SKIP VIO

                List<string[]> tmpPattern = new List<string[]>();

                if (Lib_Var.isVioPpmu)
                {
                    _TargetPins = _TargetPins.Where(t => !t.Contains("VIO")).ToArray();

                    if (m_vecpins.Any(t => t.ToUpper().Contains("VIO")))
                    {
                        Dictionary<string, bool> keyValuePairs = new Dictionary<string, bool>();

                        List<int> lVio = new List<int>();
                        int i = 1;
                        foreach (var t in m_vecpins)
                        {
                            if (t.ToUpper().Contains("VIO"))
                            {
                                lVio.Add(i);
                                keyValuePairs.Add(t, true);
                            }
                            else
                                keyValuePairs.Add(t, false);
                            i++;
                        }
                        m_vecpins = keyValuePairs.Where(v => v.Value == false).Select(k => k.Key).ToArray();

                        i = 0;

                        foreach (var t in pattern)
                        {
                            string[] tmp = new string[pattern.First().Length - lVio.Count];
                            for (int y = 0; y < t.Length; y++)
                            {
                                if (lVio.Any(v => v - y == 0)) continue;
                                tmp[i] = t[y];
                                i++;
                            }
                            tmpPattern.Add(tmp);
                            i = 0;
                        }
                        m_Pattern = tmpPattern;
                    }
                }

                #endregion SKIP VIO

                #region Generate Paths & Constants

                patternName = patternName.Replace("_", "");
                string patternSavePath = fileDir + "\\" + patternName + ".digipat";
                string digipatsrcPath = Path.ChangeExtension(patternSavePath, "digipatsrc");
                string digipatPath = Path.ChangeExtension(patternSavePath, "digipat");

                #endregion Generate Paths & Constants

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

                #endregion Check if files exist and handle appropriately

                #region Generate .digipatsrc

                #region Open digipatsrc File

                System.IO.StreamWriter digipatsrcFile = new System.IO.StreamWriter(digipatsrcPath);

                #endregion Open digipatsrc File

                #region Write Header

                digipatsrcFile.WriteLine("// VECMD5: " + ComputeMD5Hash(m_Pattern));
                digipatsrcFile.WriteLine("// National Instruments Digital Pattern Text File.");
                digipatsrcFile.WriteLine("// Automatically Generated from the GenerateAndLoadPattern function.");
                digipatsrcFile.WriteLine("// Pattern Name: " + patternName);
                digipatsrcFile.WriteLine("// Generated Date: " + System.DateTime.Now.ToString());
                digipatsrcFile.WriteLine("//\n");
                digipatsrcFile.WriteLine("file_format_version 1.1;");

                if (timeSet_read == timeSet)
                    digipatsrcFile.WriteLine("timeset " + timeSet.ToString("g") + ";");
                else
                    digipatsrcFile.WriteLine("timeset " + timeSet.ToString("g") + ", " + timeSet_read.ToString("g") + ";");
                digipatsrcFile.Write("\n");

                #endregion Write Header

                #region Loop through vectors and store in digipatsrc File

                // Write start of pattern, line contains comma separated pin names
                string pinlist = string.Join(",", _TargetPins).ToUpper();
                digipatsrcFile.WriteLine("pattern " + patternName + "(" + pinlist + ")");
                digipatsrcFile.WriteLine("{");

                // Write all vector lines
                foreach (string[] lineData in m_Pattern)
                {
                    //string lineOutput = lineData[0] + "\t" + timeSet.ToString("g");
                    string lineOutput = null;
                    int count = 0;

                    // Add Timeset and opcode at the start
                    foreach (string pin in _TargetPins)
                    {
                        if (count == 0)
                        {
                            if ((timeSet != timeSet_read) &&
                                (m_vecpins.Contains(allMipiPinNames[0].SdataPinName) || m_vecpins.Contains(allMipiPinNames[1].SdataPinName)))
                            {
                                if ((lineData[Array.IndexOf(m_vecpins, pin.ToUpper()) + 2].Contains('H') |
                                    lineData[Array.IndexOf(m_vecpins, pin.ToUpper()) + 2].Contains('L') |
                                    lineData[Array.IndexOf(m_vecpins, pin.ToUpper()) + 2].Contains('V')) |
                                    ((lineData[Array.IndexOf(m_vecpins, "SCLKP1") + 2] == "H") |
                                    (lineData[Array.IndexOf(m_vecpins, "SCLKP1") + 2] == "L") |
                                    (lineData[Array.IndexOf(m_vecpins, "SCLKP1") + 2] == "V")))
                                    //((lineData.Length > 4) && lineData[Array.IndexOf(m_vecpins, pin.ToUpper()) + 4] == "V"))        ///////////////////////add timeSet_read for H/L
                                    lineOutput = lineData[0] + "\t" + timeSet_read.ToString("g");
                                else
                                {
                                    lineOutput = lineData[0] + "\t" + timeSet.ToString("g");
                                }
                            }
                            else
                            {
                                lineOutput = lineData[0] + "\t" + timeSet.ToString("g");
                            }
                        }
                        if (m_vecpins.Contains(pin.ToUpper()))
                        {
                            lineOutput += "\t" + lineData[Array.IndexOf(m_vecpins, pin.ToUpper()) + 1];
                        }
                        else
                        {
                            //lineOutput += "\t-";
                            lineOutput += "\tX";
                        }
                        count++;
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

                #endregion Loop through vectors and store in digipatsrc File

                #region close text digipatsrc File

                digipatsrcFile.Close();

                #endregion close text digipatsrc File

                #endregion Generate .digipatsrc

                return this.CompileDigipatSrc(digipatsrcPath, digipatPath, patternName, _TargetPins, true);
            }

            private List<string[]> RemoveNRZvector(List<string[]> VectorList)
            {
                var toRemove = new HashSet<string[]>();

                //To Set NRZ pattern to remove
                string[] remove1 = new string[] { "", "0", "X", "1", "-", "" };
                string[] remove2 = new string[] { "", "0", "-", "1", "-", "" };
                string[] remove3 = new string[] { "", "0", "X", "1", "X", "" };

                for (int i = 0; i < VectorList.Count; i++)
                {
                    if (VectorList[i].SequenceEqual(remove1) || VectorList[i].SequenceEqual(remove2) || VectorList[i].SequenceEqual(remove3))
                        VectorList[i] = null;
                }

                VectorList.RemoveAll(s => s == null);

                return VectorList;
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
                string pinmapPath = Path.GetTempFileName() + ".pinmap";// fileDir + "\\compiler.pinmap";

                #endregion Generate Paths

                #region Check if compiler exists and handle appropriately

                if (!File.Exists(compilerPath))
                {
                    // Compiler not found, can't proceed
                    throw new FileNotFoundException("Digital Pattern Compiler Not Found", compilerPath);
                }

                #endregion Check if compiler exists and handle appropriately

                #region Constants

                patternName = patternName.Replace("_", "");
                string pinmapHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<PinMap schemaVersion=\"1.1\" xmlns=\"http://www.ni.com/TestStand/SemiconductorModule/PinMap.xsd\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n	<Instruments>\n		<NIDigitalPatternInstrument name=\"NI6570\" numberOfChannels=\"32\" />\n	</Instruments>\n	<Pins>\n";
                string pinmapMiddle = "\n	</Pins>\n	<PinGroups>\n	</PinGroups>\n	<Sites>\n		<Site siteNumber=\"0\" />\n	</Sites>\n	<Connections>\n";
                string pinmapFooter = "\n	</Connections>\n</PinMap>";

                #endregion Constants

#if NIDEEPDEBUG
            Console.WriteLine("Compiling from .digipatsrc to .digipat");
#endif

                #region Create dummy pinmap to be used by compiler

                using (System.IO.StreamWriter pinmapFile = new StreamWriter(pinmapPath, false))
                {
                    int i = 1;

                    pinmapFile.Write(pinmapHeader);
                    foreach (string pin in this.allDutPins) { pinmapFile.WriteLine("<DUTPin name=\"" + pin.ToUpper() + "\" />"); }
                    pinmapFile.Write(pinmapMiddle);
                    foreach (string pin in this.allDutPins) { pinmapFile.WriteLine("<Connection pin=\"" + pin.ToUpper() + "\" siteNumber=\"0\" instrument=\"NI6570\" channel=\"" + i++ + "\" />"); }
                    pinmapFile.Write(pinmapFooter);
                }

                #endregion Create dummy pinmap to be used by compiler

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

                #endregion Run Compiler

                #region Load Pattern to Instrument Memory

                // Check if process exited without error and return status.
                if (process.ExitCode == 0)
                {
                    // Compilation completed without error, try loading pattern now.
                    try
                    {
                        if (load)
                        {
                            lock (lockObject)
                                DIGI.LoadPattern(digipatPath);
                        }
                        return true;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        try
                        {
                            File.Delete(pinmapPath);
                            File.Delete(pinmapPath.Replace(".pinmap", ""));
                        }
                        catch { }
                    }
                }
                else
                {
                    return false;
                }

                #endregion Load Pattern to Instrument Memory
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

            #endregion Avago SJC Specific Helper Functions

            #region Avago SJC Specific Enums

            /// <summary>
            /// Used to specify which timeset is used for a specified pattern.
            /// Get the string representation using Timeset.MIPI.ToString("g");
            /// </summary>
            public enum Timeset
            {
                MIPI_SCLK_NRZ,
                MIPI_SCLK_NRZ_HALF,
                MIPI_SCLK_NRZ_QUAD,
                MIPI_SCLK_RZ,
                MIPI_SCLK_RZ_HALF,
                MIPI_SCLK_RZ_QUAD,
                MIPI_NFR,
                EEPROM,
                TEMPSENSE
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
                EXTENDEDREGISTERWRITELONG,
                EXTENDEDREGISTERREADLONG,
                EXTENDEDREGISTERWRITE,
                EXTENDEDREGISTERREAD,
                REGISTERWRITE,
                REGISTERREAD
            };

            private System.Version version = new System.Version(1, 0, 1215, 1);

            #endregion Avago SJC Specific Enums

            #region TestBoard EEPROM & TEMPERATURE

            /// <summary>
            /// Not Implemented yet
            /// </summary>
            /// <returns>False</returns>
            public bool LoadVector_EEPROM()
            {
                return LoadVector_EEPROMRead() & LoadVector_EEPROMWrite();  // &LoadVector_EEPROMEraseWriteEnable();
            }

            public bool LoadVector_TEMPSENSEI2C(decimal TempSensorAddress)
            {
                try
                {
                    if (!(TempSensorAddress >= 0 && TempSensorAddress <= 3)) throw new Exception("Temp Sensor Address out of range");
                    I2CTempSensorDeviceAddress = TempSensorAddress; //this only happens if the temp sensor device address is valid

                    LoadVector_ConfigRegister("0", "0", "0", "0", "0", "0", "0", "0", I2CTempSensorDeviceAddress); //seems a little hidden but it's the right place for now

                    return LoadVector_TEMPSENSEI2CRead(I2CTempSensorDeviceAddress);
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            /// <summary>
            /// Internal Function: Used to generate and load the EEPROM read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_EEPROMRead()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    string[] pins = new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), I2CVCCChanName.ToUpper() };
                    List<string[]> pattern = new List<string[]> { };
                    pattern.Add(new string[] { "source_start(SrcEEPROMRead)", "1", "1", "1", "Configure source" });
                    pattern.Add(new string[] { "capture_start(CapEEPROMRead)", "1", "1", "1", "Configure capture" });
                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "source", "1", "D", "1", "Write Register Address" });
                        pattern.Add(new string[] { "", "0", "-", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Read Device (10101001)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "capture", "1", "V", "1", "Read Register Data" });
                        pattern.Add(new string[] { "", "0", "X", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "1", "1", "NO ACK" });
                    pattern.Add(new string[] { "", "0", "1", "-", "" });

                    pattern.Add(new string[] { "", "1", "0", "1", "Stop Condition" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Stop Condition" });

                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });

                    pattern.Add(new string[] { "capture_stop", "1", "1", "1", "" });
                    pattern.Add(new string[] { "halt", "1", "1", "X", "halt" });

                    // Generate and load Pattern from the formatted array.
                    // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                    if (!this.GenerateAndLoadPattern("EEPROMRead", pins, pattern, true, Timeset.EEPROM))
                    {
                        throw new Exception("EEPROMRead Compile Failed");
                    }

                    HSDIO.datalogResults["EEPROMRead"] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load EEPROMRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the EEPROM Write pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_EEPROMWrite()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    string[] pins = new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), I2CVCCChanName.ToUpper() };
                    List<string[]> pattern = new List<string[]> { };

                    pattern.Add(new string[] { "source_start(SrcEEPROMWrite)", "1", "1", "1", "Configure source" });
                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "1 - Write Device (10101000)" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "source", "1", "D", "1", "Write Register Address" });
                        pattern.Add(new string[] { "", "0", "-", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "source", "1", "D", "1", "Write Data" });
                        pattern.Add(new string[] { "", "0", "-", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    pattern.Add(new string[] { "", "1", "0", "1", "Stop Condition" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Stop Condition" });

                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });

                    pattern.Add(new string[] { "halt", "1", "1", "1", "halt" });

                    // Generate and load Pattern from the formatted array.
                    if (!this.GenerateAndLoadPattern("EEPROMWrite", pins, pattern, true, Timeset.EEPROM))
                    {
                        throw new Exception("EEPROMWrite Compile Failed");
                    }

                    HSDIO.datalogResults["EEPROMWrite"] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load EEPROMRead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// Internal Function: Used to generate and load the TEMPSENSE read pattern
            /// </summary>
            /// <returns>True if pattern load succeeds.</returns>
            private bool LoadVector_TEMPSENSEI2CRead(decimal TempSensorAddress)
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    #region Set Device Address

                    //LoadVector_TEMPSENSE12C() filters out the invalid temp sensor addresses before here
                    string[] zTempSensorValidAddresses = { "00", "01", "10", "11" };
                    string zAddress = zTempSensorValidAddresses[(int)TempSensorAddress];

                    string A0 = "0", A1 = "0";

                    switch (zAddress)
                    {
                        case "00":
                            A0 = "0";
                            A1 = "0";
                            break;

                        case "01":
                            A0 = "0";
                            A1 = "1";
                            break;

                        case "10":
                            A0 = "1";
                            A1 = "0";
                            break;

                        case "11":
                            A0 = "1";
                            A1 = "1";
                            break;
                    }

                    #endregion Set Device Address

                    string[] pins = new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), TEMPSENSEI2CVCCChanName.ToUpper() };
                    List<string[]> patternInit = new List<string[]> { };
                    patternInit.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle with VDD high to turn on Temp Sensor" });
                    // Generate and load Pattern from the formatted array.
                    // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                    if (!this.GenerateAndLoadPattern("TEMPSENSEOn", pins, patternInit, true, Timeset.EEPROM))
                    {
                        throw new Exception("TEMPSENSEOn Compile Failed");
                    }
                    HSDIO.datalogResults["TEMPSENSEOn"] = false;

                    List<string[]> pattern = new List<string[]> { };
                    pattern.Add(new string[] { "capture_start(CapTEMPSENSERead)", "1", "1", "1", "Configure capture" });
                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A1, "1", A1 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A0, "1", A0 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Bit" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "", "1", "0", "1", "0 - Register Address (0x0 for MSB)" });
                        pattern.Add(new string[] { "", "0", "-", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A1, "1", A1 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A0, "1", A0 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Read Bit" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "capture", "1", "V", "1", "Read MSB Data" });
                        pattern.Add(new string[] { "", "0", "X", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "0", "1", "ACK - BY MASTER" });
                    pattern.Add(new string[] { "", "0", "0", "-", "" });

                    for (int i = 0; i < 8; i++)
                    {
                        pattern.Add(new string[] { "capture", "1", "V", "1", "Read LSB Data" });
                        pattern.Add(new string[] { "", "0", "X", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "1", "1", "NO ACK - BY MASTER" });
                    pattern.Add(new string[] { "", "0", "1", "-", "" });

                    pattern.Add(new string[] { "", "1", "0", "1", "Stop Condition" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Stop Condition" });

                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });

                    pattern.Add(new string[] { "capture_stop", "1", "1", "1", "" });
                    pattern.Add(new string[] { "halt", "1", "1", "1", "halt" });

                    // Generate and load Pattern from the formatted array.
                    // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                    if (!this.GenerateAndLoadPattern("TEMPSENSERead", pins, pattern, true, Timeset.EEPROM))
                    {
                        throw new Exception("TEMPSENSERead Compile Failed");
                    }

                    HSDIO.datalogResults["TEMPSENSERead"] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load TEMPSENSERead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            private bool LoadVector_ConfigRegister(string Res, string OpMode6, string OpMode5, string IntorCT, string INTPinPol, string CTPinPol, string FaultQ1, string FaultQ0, decimal TempSensorAddress)
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    if (!(TempSensorAddress >= 0 && TempSensorAddress <= 3)) throw new Exception("Temp Sensor Address out of range"); //This is only redundant if being called by LoadVector_TEMPSENSEI2C
                    I2CTempSensorDeviceAddress = TempSensorAddress; //this only happens if the temp sensor device address is valid

                    #region Set Device Address

                    //LoadVector_TEMPSENSE12C() filters out the invalid temp sensor addresses before here
                    string[] zTempSensorValidAddresses = { "00", "01", "10", "11" };
                    string zAddress = zTempSensorValidAddresses[(int)I2CTempSensorDeviceAddress];

                    string A0 = "1", A1 = "1";

                    switch (zAddress)
                    {
                        case "00":
                            A0 = "0";
                            A1 = "0";
                            break;

                        case "01":
                            A0 = "0";
                            A1 = "1";
                            break;

                        case "10":
                            A0 = "1";
                            A1 = "0";
                            break;

                        case "11":
                            A0 = "1";
                            A1 = "1";
                            break;
                    }

                    #endregion Set Device Address

                    #region Set the Configuration Register Values

                    ConfigRegisterSettings["FaultQueueBit0"] = FaultQ0; // "0"; // 00 = 1 fault, 01 = 2 faults, 10 = 3 faults, 11 = 4 faults (This is bits 1:0)
                    ConfigRegisterSettings["FaultQueueBit1"] = FaultQ1; // "0"; // 00 = 1 fault, 01 = 2 faults, 10 = 3 faults, 11 = 4 faults (This is bits 1:0)
                    ConfigRegisterSettings["CTPinPolarity"] = CTPinPol; // "0";  // 0 = Active Low, 1 = Active High (This is bit 2)
                    ConfigRegisterSettings["INTPinPolarity"] = INTPinPol; // "0"; // 0 = Active Low, 1 = Active High (This is bit 3)
                    ConfigRegisterSettings["INTorCTMode"] = IntorCT; // "0"; // 0 = Interrupt mode, 1 = Comparator mode (This is bit 4)
                    ConfigRegisterSettings["OperationModeBit5"] = OpMode5; // "0"; // 00 = Continuous conversion, 01 = One shot, 10 = 1 SPS mode, 11 = Shutdown (This is bits 6:5)
                    ConfigRegisterSettings["OperationModeBit6"] = OpMode6; // "0"; // 00 = Continuous conversion, 01 = One shot, 10 = 1 SPS mode, 11 = Shutdown (This is bits 6:5)
                    ConfigRegisterSettings["Resolution"] = Res; // "0"; // 0 = 13 bit resolution, 1 = 16 bit resolution (This is bit 7)

                    #endregion Set the Configuration Register Values

                    string[] pins = new string[] { I2CSCKChanName.ToUpper(), I2CSDAChanName.ToUpper(), TEMPSENSEI2CVCCChanName.ToUpper() };
                    List<string[]> pattern = new List<string[]> { };
                    pattern.Add(new string[] { "capture_start(CapConfRegWrite)", "1", "1", "1", "Configure capture" });
                    pattern.Add(new string[] { "repeat(1220)", "1", "1", "1", "Idle with VDD high for 6.1ms to allow first ADT7420 conversion" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Start Condition" });
                    pattern.Add(new string[] { "", "0", "0", "1", "Start Condition" });

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });  //Device address
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A1, "1", A1 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", A0, "1", A0 + " - ADT7410 Address (10010" + zAddress + ")" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", "0", "1", "0 - Write Bit" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    for (int i = 0; i < 6; i++)
                    {
                        pattern.Add(new string[] { "", "1", "0", "1", "0 - Register Address (0x03 for ConfigRegister)" });
                        pattern.Add(new string[] { "", "0", "-", "1", "" });
                    }

                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Register Address (0x03 for ConfigRegister)" });
                    pattern.Add(new string[] { "", "0", "-", "1", "" });
                    pattern.Add(new string[] { "", "1", "1", "1", "1 - Register Address (0x03 for ConfigRegister)" });
                    pattern.Add(new string[] { "", "0", "-", "1", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    pattern.Add(new string[] { "", "1", Res, "1", Res + "- Config Register Bit 7 Resolution" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", OpMode6, "1", OpMode6 + "- Config Register Bit 6 OpMode" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", OpMode5, "1", OpMode5 + "- Config Register Bit 5 OpMode" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", IntorCT, "1", IntorCT + "- Config Register Bit 4 IntOrCTMode" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", INTPinPol, "1", INTPinPol + "- Config Register Bit 3 IntPinPolarity" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", CTPinPol, "1", CTPinPol + "- Config Register Bit 2 CTPinPolarity" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", FaultQ1, "1", FaultQ1 + "- Config Register Bit 1 FaultQueue" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });
                    pattern.Add(new string[] { "", "1", FaultQ0, "1", FaultQ0 + "- Config Register Bit 0 FaultQueue" });
                    pattern.Add(new string[] { "", "0", "-", "-", "" });

                    pattern.Add(new string[] { "", "1", "L", "1", "ACK" });
                    pattern.Add(new string[] { "", "0", "X", "-", "" });

                    pattern.Add(new string[] { "", "1", "0", "1", "Stop Condition" });
                    pattern.Add(new string[] { "", "1", "1", "1", "Stop Condition" });

                    pattern.Add(new string[] { "repeat(300)", "1", "1", "1", "Idle" });

                    pattern.Add(new string[] { "capture_stop", "1", "1", "1", "" });
                    pattern.Add(new string[] { "halt", "1", "1", "1", "halt" });

                    // Generate and load Pattern from the formatted array.
                    // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                    if (!this.GenerateAndLoadPattern("ConfRegWrite", pins, pattern, true, Timeset.EEPROM))
                    {
                        throw new Exception("ConfRegWrite Compile Failed");
                    }

                    HSDIO.datalogResults["ConfRegWrite"] = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load TEMPSENSERead vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            /// <summary>
            /// I2C TEMPSENSE Read
            /// </summary>
            /// <returns>temperature as a double</returns>
            public double I2CTEMPSENSERead()
            {
                try
                {
                    // Make sure the 6570 is in the correct mode in case we were previously in PPMU mode.
                    allTEMPSENSEPins.SelectedFunction = SelectedFunction.Digital;
                    // Set TEMPSENSEVccPin to Activeload (Constant supply of vcom) to ensure that the Tempsensor is
                    // On all the time. Initial temperature reading will take approx 200 ms, if tempsensor is On, subsequent
                    // reading will only takes approx 4ms. - RON
                    TEMPSENSEVccPin.DigitalLevels.TerminationMode = TerminationMode.ActiveLoad;

                    double returnval = double.NaN;
                    // Burst pattern & check for Pass/Fail
                    bool[] passFail = new bool[] { };

                    // Configure to capture 16 bits
                    DIGI.CaptureWaveforms.CreateSerial(I2CSDAChanName.ToUpper(), "CapTEMPSENSERead", 16, BitOrder.MostSignificantBitFirst);

                    // RON - Removed as bursting pattern twice will delete the content of EEPROM
                    //if (!TempSenseStateOn)
                    //{
                    //    DIGI.PatternControl.BurstPattern("", "TEMPSENSEOn", new TimeSpan(0, 0, 10));
                    //    Thread.Sleep(260);
                    //    TempSenseStateOn = true;
                    //}

                    // Burst Pattern
                    passFail = DIGI.PatternControl.BurstPattern("", "TEMPSENSERead", false, new TimeSpan(0, 0, 10));

                    // Retreive captured waveform
                    uint[][] capData = new uint[][] { };
                    DIGI.CaptureWaveforms.Fetch("", "CapTEMPSENSERead", 1, new TimeSpan(0, 0, 0, 0, 100), ref capData);

                    if (passFail[0])
                    {
                        // Convert captured data
                        Int32 data = (Int32)capData[0][0];

                        if (ConfigRegisterSettings["Resolution"] == "0")
                        {
                            // shift right by 3 to remove flag bits which are the 3 LSBs
                            data = data >> 3;
                            // convert based on sign bit (bit 13 after the bitshift from above)
                            if ((data & (1 << 13 - 1)) != 0)
                            {
                                // negative conversion
                                returnval = ((double)data - 8192.0) / 16.0;
                            }
                            else
                            {
                                // positive conversion
                                returnval = (double)data / 16.0;
                            }
                        }

                        //// shift right by 3 to remove flag bits which are the 3 LSBs
                        //data = data >> 3;
                        //// convert based on sign bit (bit 13 after the bitshift from above)
                        //if ((data & (1 << 13 - 1)) != 0)
                        //{
                        //    // negative conversion
                        //    returnval = ((double)data - 8192.0) / 16.0;
                        //}
                        //else
                        //{
                        //    // positive conversion
                        //    returnval = (double)data / 16.0;
                        //}
                        else
                        {
                            if ((data & (1 << 16 - 1)) != 0)
                            {
                                // negative conversion
                                returnval = ((double)data - 65536.0) / 128.0;
                            }
                            else
                            {
                                // positive conversion
                                returnval = (double)data / 128.0;
                            }
                        }

                        if (debug) Console.WriteLine("I2CTEMPSENSERead: " + returnval);
                        return returnval;
                    }
                    else
                    {
                        return double.NaN;
                    }
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to Read I2C TEMPSENSE Register.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return double.NaN;
                }
            }

            /// <summary>
            /// EEPROM Read
            /// </summary>
            /// <returns></returns>
            public string EepromRead()
            {
                try
                {
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
                        passFail = DIGI.PatternControl.BurstPattern("", "EEPROMRead", false, new TimeSpan(0, 0, 10));

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

            #endregion TestBoard EEPROM & TEMPERATURE

            #region MIPI NFR - Add by ben

            public bool LoadVector_MipiNFR()
            {
                return LoadVector_MipiNFRWriteSinglePath() & LoadVector_MipiNFRWritDualPath();   // Write pattern for MIPI NFR
            }

            private bool LoadVector_MipiNFRWriteSinglePath()
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

                        string[] pins = new string[] { m.SclkPinName, m.SdataPinName, m.VioPinName };
                        string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                        List<string[]> patternStart = new List<string[]>
                            {
                            #region MIPI NFR Write Pattern
                                new string[] {"", "1", "0", "1",""},
                                new string[] {"TTT:", "1", "1", "1", ""},
                                new string[] { "", "1", "0", "1", ""},
                                new string[] { "", "1", "1", "1", ""},
                                new string[] {"jump(TTT)", "1", "0", "1", ""},
                                new string[] {"halt", "1", "0", "1", ""}
                                #endregion
                            };

                        // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                        List<string[]> pattern = new List<string[]> { };
                        pattern = pattern.Concat(patternStart).ToList();

                        // Generate and load Pattern from the formatted array.
                        // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                        if (!this.GenerateAndLoadPattern("MIPINFRWrite" + "Pair" + p.ToString(), pins, pattern, true, Timeset.MIPI_NFR))
                        {
                            throw new Exception("Compile Failed");
                        }

                        HSDIO.datalogResults["MIPINFRWrite" + "Pair" + p.ToString()] = false;
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

            private bool LoadVector_MipiNFRWritDualPath()
            {
                if (!usingMIPI)
                {
                    MessageBox.Show("Attempted to load HSDIO MIPI vector but HSDIO.Initialize wasn't called", "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Generate MIPI / RFFE Non-Extended Register Write Pattern

                    string[] pins = new string[] { allMipiPinNames[0].SclkPinName, allMipiPinNames[0].SdataPinName, allMipiPinNames[0].VioPinName ,
                                                        allMipiPinNames[1].SclkPinName, allMipiPinNames[1].SdataPinName, allMipiPinNames[1].VioPinName};
                    string trigval = (triggerConfig == TrigConfig.Digital_Pin || triggerConfig == TrigConfig.Both ? "1" : "0");
                    List<string[]> patternStart = new List<string[]>
                            {
                            #region MIPI NFR Write Pattern
                               new string[] {"", "1", "0", "1", "1", "0", "1","" },
                                new string[] {"TTT:", "1", "1", "1", "1", "1", "1", "" },
                                new string[] { "", "1", "0", "1", "1", "0", "1", "" },
                                new string[] { "", "1", "1", "1", "1", "1", "1", ""},
                                new string[] {"jump(TTT)", "1", "0", "1", "1", "0", "1", "" },
                                new string[] {"halt", "1", "0", "1", "1", "0", "1", "" }
                                #endregion
                            };

                    // Generate full pattern with the correct number of source / capture sections based on the 1-16 bit count
                    List<string[]> pattern = new List<string[]> { };
                    pattern = pattern.Concat(patternStart).ToList();

                    // Generate and load Pattern from the formatted array.
                    // Note: Regsiter read/write patterns are generated as Return to Zero Patterns
                    if (!this.GenerateAndLoadPattern("MIPINFRWriteDual", pins, pattern, true, Timeset.MIPI_NFR))
                    {
                        throw new Exception("Compile Failed");
                    }

                    HSDIO.datalogResults["MIPINFRWriteDual"] = false;
                    return true;

                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to load Mipi RegisterWrite vector\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            public bool BurstMIPIforNFR(string nameInMemory)
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
                        if (MIPI.Lib_Var.isVioPpmu)
                        {
                            allRffePinsWoVIO.SelectedFunction = SelectedFunction.Digital;
                            allRffePinsWoVIO.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }
                        else
                        {
                            allRffePins.SelectedFunction = SelectedFunction.Digital;
                            allRffePins.DigitalLevels.TerminationMode = TerminationMode.HighZ;
                        }

                        // Select pattern to burst
                        // Always use all lower case names for start labels due to known issue (some uppercase labels not valid)
                        DIGI.PatternControl.StartLabel = nameInMemory;

                        // Send the normal pattern file and store the number of bit errors from the SDATA pin
                        DIGI.PatternControl.Initiate();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    DIGI.PatternControl.Abort();
                    MessageBox.Show("Failed to generate vector for " + nameInMemory + ".\nPlease ensure LoadVector() was called.\n\n" + e.ToString(), "HSDIO MIPI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            public void AbortBurst()
            {
                DIGI.PatternControl.Abort();
            }

            public bool LoadVector_MipiHiZ()
            {
                throw new NotImplementedException();
            }

            public bool LoadVector_MipiReset()
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}