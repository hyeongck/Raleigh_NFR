using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
//debug 20190311 Aemulus: Add
using System.Collections.Generic;
using Aemulus.Hardware;
using System.ComponentModel;

namespace LibEqmtDriver.MIPI
{
    public class Aemulus_DM482e : iMiPiCtrl
    {        
        int slaveaddr, pairNo;
        int ret = 0;
        bool[] dataArray_Bool;
        string moduleAlias = Lib_Var.myDM482Address;

        // Initialize Aemulus session; 
        Aemulus_DM482e myMipiCtrl;

        static Stopwatch my482Watch = new Stopwatch();  //20190311 Aemulus: Add

        //debug
        public string SCL;
        public string SDA;
        public string VDD_TEMP;
        public string VCC_ROM;
        public string VPUP_EEPROM;
        public string SIO_EEPROM;
		//20210915
		public string VPUP_SOCKET_EEPROM;
		public string SIO_SOCKET_EEPROM;

        int pmTrigAddr = Lib_Var.PMTrig;
        int pmTrigData = Lib_Var.PMTrig_Data;

        private double vih, vil, voh, vol, ioh, iol, vch, vcl, vt;

        public Aemulus.Hardware.DM myDM;
        LibEqmtDriver.Utility.HiPerfTimer HiTimer = new LibEqmtDriver.Utility.HiPerfTimer();
        Stopwatch Speedo = new Stopwatch();

        public bool enableVector = false;

        // Ben - add for ppmu
        //Dictionary<string, LibEqmtDriver.SMU.iSmu> PpmuResourcesLocal = null;
        public s_MIPI_PAIR[] mipiPair;

        double[] priorVoltage = new double[4]; // VIO0 -> Tx, VIO1 -> Rx
        double[] priorCurrentLim = new double[4];
        double[] priorApertureTime = new double[4];

        //public Aemulus_DM482e(Dictionary<string, LibEqmtDriver.SMU.iSmu> PpmuResources = null)
        public Aemulus_DM482e()
        {
            //PpmuResourcesLocal = PpmuResources;
            myDM = new Aemulus.Hardware.DM(Lib_Var.HW_Profile, 3, 0, 0, false, 0x0f);    //3
                                                                                          //myDM = new Aemulus.Hardware.DM(Lib_Var.HW_Profile, 1, 0, 0, false, 0x0f);      //20181203 Aemulus: original set dpingroup3 (pin 0-11), change to dpingroup1 (pin 0-5
            for (int i = 0; i <priorVoltage.Length; i++)
            {
                priorVoltage[i] = 0;
                priorCurrentLim[i] = 0;
                priorApertureTime[i] = 0;
            }
            
        }

        #region iMipiCtrl interface

        void iMiPiCtrl.Init(s_MIPI_PAIR[] mipiPairCfg)
        { 
            mipiPair = new s_MIPI_PAIR[mipiPairCfg.Length];

            for (int i = 0; i < mipiPair.Length; i++)
            {
                mipiPair[i].PAIRNO = mipiPairCfg[i].PAIRNO;

                //set MIPI pin alias name
                mipiPair[i].SCLK = "P" + mipiPairCfg[i].SCLK;
                mipiPair[i].SDATA = "P" + mipiPairCfg[i].SDATA;
                mipiPair[i].SVIO = "P" + mipiPairCfg[i].SVIO;                

                //set mipi pin no
                mipiPair[i].SCLK_pinNo = Int32.Parse(mipiPairCfg[i].SCLK);
                mipiPair[i].SDATA_pinNo = Int32.Parse(mipiPairCfg[i].SDATA);
                mipiPair[i].SVIO_pinNo = Int32.Parse(mipiPairCfg[i].SVIO);
            }

            INITIALIZATION();
        }
        void iMiPiCtrl.Init_ID(s_MIPI_PAIR[] mipiPairCfg)
        {
            mipiPair = new s_MIPI_PAIR[mipiPairCfg.Length];

            for (int i = 0; i < mipiPair.Length; i++)
            {
                mipiPair[i].PAIRNO = mipiPairCfg[i].PAIRNO;

                //set MIPI pin alias name
                mipiPair[i].SCLK = "P" + mipiPairCfg[i].SCLK;
                mipiPair[i].SDATA = "P" + mipiPairCfg[i].SDATA;
                mipiPair[i].SVIO = "P" + mipiPairCfg[i].SVIO;

                //set mipi pin no
                mipiPair[i].SCLK_pinNo = Int32.Parse(mipiPairCfg[i].SCLK);
                mipiPair[i].SDATA_pinNo = Int32.Parse(mipiPairCfg[i].SDATA);
                mipiPair[i].SVIO_pinNo = Int32.Parse(mipiPairCfg[i].SVIO);
            }

            INITIALIZATION_ID();
        }

        void iMiPiCtrl.TurnOn_VIO(int pair)
        {
            VIO_ON(pair);
        }
        void iMiPiCtrl.TurnOff_VIO(int pair)
        {
            if (pair < mipiPair.Length)
                VIO_OFF(pair);
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

            ReadSuccessful = Register_ChangeRev2(Mipi_Reg);
        }   //no use
        void iMiPiCtrl.SendAndReadMIPICodesCustom(out bool ReadSuccessful, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            ReadSuccessful = Register_Change_Custom(MipiRegMap, TrigRegMap, true);
        }
        void iMiPiCtrl.ReadMIPICodesCustom(out int Result, string MipiRegMap, string TrigRegMap, int pair, int slvaddr) //20181212 - Aemulus: Add Extended Read Long condotion
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller

            string tmpRslt = "";
            Result = 0;
            bool extReg = true;
            string[] tmpData = MipiRegMap.Split(':');

            //Count number of bit in the Address to decide using ext / ext long
            int count = 0;
            int address_dec = Convert.ToInt32(tmpData[0], 16);
            while (address_dec != 0)
            {
                count++;
                address_dec >>= 1;
            }
            int Num_Of_Bit = count;

            //Choose ext / ext long
            if (Num_Of_Bit <= 8)     //using Extented Read function
            {
                extReg = true;
            }
            else                    //using Extended Read Long function
            {
                extReg = false;
            }

            Read_Register_Address_Rev2(ref tmpRslt, Convert.ToInt32(tmpData[0], 16), extReg);
            Result = int.Parse(tmpRslt, System.Globalization.NumberStyles.HexNumber);               //convert HEX to INT
        }
        void iMiPiCtrl.WriteMIPICodesCustom(string MipiRegMap, int pair, int slvaddr)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller
            bool extReg = true;

            WriteRegister_Rev2(MipiRegMap, extReg);
        }
        void iMiPiCtrl.WriteOTPRegister(string efuseCtlReg_hex, string data_hex, int pair, int slvaddr, bool invertData = false)
        {
            //Note : this function for using multiple MIPI pair and slave address
            pairNo = pair;          //pass value from tcf for different mipi pair controller
            slaveaddr = slvaddr;    //pass value from tcf for different mipi pair controller
            string mipiRegMap = efuseCtlReg_hex + ":" + data_hex;   //construct mipiRegMap to this format only example "C0:9E" - where C0 is efuseCtrl Address , 9E is efuseData to burn
            bool extReg = true;

            WriteRegister_Rev2(mipiRegMap, extReg);
        }
        //void iMiPiCtrl.SetMeasureMIPIcurrent(int delayMs, int pair, int slvaddr, s_MIPI_DCSet[] setDC_Mipi, string[] measDC_MipiCh, out s_MIPI_DCMeas[] measDC_Mipi)
        //{
        //    s_MIPI_DCMeas[] tmpMeasDC_Mipi = new s_MIPI_DCMeas[3];
        //    measDC_Mipi = tmpMeasDC_Mipi;
        //}
        int iMiPiCtrl.SendVector(int pair, string nameInMemory)
        {
            int ret = 0;
            ret = VecWrite();
            return ret;
        }
        int iMiPiCtrl.ReadVector(int pair, ref int VectorErrorCount, string nameInMemory)
        {
            VectorErrorCount = -99999;
            int ret = 0;
            ret += switchMIPItoVec(pair);
            //debug
            //LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
            //timer.wait(1);
            ret += VecRead(ref VectorErrorCount);
            //debug
            //ret += VecRead(ref VectorErrorCount);
            ret += switchVectoMIPI(pair);
           
            return ret;
        }
        bool iMiPiCtrl.LoadVector_PowerMode(string fullPath, string powerMode, int vecSetNo)
        {
            int ret = 0;

            //Load vector into DM module based on the selected vector set/file, let say now only using 2 vector set/file
            ret += myDM.DPINVecLoad(moduleAlias, 0, vecSetNo, fullPath);  

            if (ret==0)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }
        void iMiPiCtrl.BoardTemperature(out double tempC)
        {
            tempC = -999;        //note temperature return out should be in rage of 25 degC
            Read_Temp(out tempC);
        }
        void iMiPiCtrl.ReadLoadboardsocketID(out string loadboardID, out string socketID)
        {
            Init_I2C_PINOT(out loadboardID, out socketID);
        }

        void iMiPiCtrl.BurstMIPIforNFR(int pair) // Ben, add for NF MIPI RISE
        {

        }
        void iMiPiCtrl.AbortBurst() // Ben, add for NF MIPI RISE
        {

        }
        #endregion

        #region Init function
        public void INITIALIZATION()
        {
            //Important : Init_I2C() must be initialize before rest of MIPI initialization process
            //Else none of the MIPI Controller or MIPI Vector function will work - Shaz 19/4/2019
            //Init_I2C_PINOT(out string loadboardID, out string socketID);

            for (int i = 0; i < mipiPair.Length; i++)
            {
                if(enableVector)
                {
                    InitVec(mipiPair[i].PAIRNO, 2 * Convert.ToInt32(MIPI.Lib_Var.DUTMipi.MipiClockSpeed), MIPI.Lib_Var.DUTMipi.VIOTargetVoltage);  //20190107 Aemulus: Add this function                 
                    SetVecInputDelay(mipiPair[i].PAIRNO, 1 / 26000000); //20190114 Aemulus: Add this function
                }

                //Actual DUT MIPI
                InitMipi(mipiPair[i].PAIRNO, Convert.ToInt32(MIPI.Lib_Var.DUTMipi.MipiClockSpeed), MIPI.Lib_Var.DUTMipi.VIOTargetVoltage);
                SetMipiInputDelay(mipiPair[i].PAIRNO, 2); //depend on cable length

                OnOff_VIO(true, mipiPair[i].PAIRNO);
                OnOff_CLKDATA(true, mipiPair[i].PAIRNO);
            }
        }

        public void INITIALIZATION_ID()
        {
            Init_I2C_PINOT(out string loadboardID, out string socketID);
        }

        public int InitVec(int mipi_pair, int ClockFrequency, double mipi_voltage)  //20190107 Aemulus: Add this function
        {
            int ret = 0;
            double Threhold = 1.2;
            vih = mipi_voltage;
            vil = 0;
            voh = Threhold; //(vih - vil) / 3;
            vol = Threhold; // (vih - vil) / 3;
            ioh = 0.01;
            iol = 0.01;
            vch = mipi_voltage;
            vcl = 0;
            vt = 1.6;

            double Freq = ClockFrequency;
            double Period = (1 / Freq);   //20190108 Aemulus: remove 0.5* because using 1/52e6 

            ////Count number of Vector Set/File
            //int VectorSetCount = 0;
            //string VectorFolder = "C:\Users\Broadcom\Downloads\JY";
            //foreach (string EveryFile in Directory.EnumerateFiles(VectorFolder, "*.vec"));
            //{
            //   VectorSetCount ++;
            //}




            ////Store VectorFileName and its number of lines to use d in resourceArr
            //int[] resourceArr = new int[1024];  //0~1023
           
            //string line;
            //try
            //{
            //    StreamReader sr = new StreamReader("C:\Users\Broadcom\Downloads\JY\OTPLNA_Rev13.vec"); //Pass file path & file name to StreamReader Constructor

            //    while(line != null)
            //    {
            //        line = sr.ReadLine();   //Read the line of text
            //    }

            //    sr.Close(); //Close the file 
            //}


            //var NumberOfLine = 0;
            //using (var reader = File.OpenText(@"C:\file.txt"))  //@File Path
            //{
            //    if(reader.ReadLine() == null)
            //    {
 
            //        while (reader.ReadLine() != null && BlankCount)  //also loop if null more than ?? also jump out
            //        {
            //           ++NumberOfLine;
            //        }
 
            //    }
            //    return NumberOfLine;
            //}
            
            //Assign resourceArr
            int[] resourceArr = new int[2];
            resourceArr[0] = 80; 
            resourceArr[1] = 5;

            ret += myDM.Force(mipiPair[mipi_pair].SVIO, 0);     //SVIO vector mode
            ret += myDM.Force(mipiPair[mipi_pair].SDATA, 0);    //SDATA vector mode
            ret += myDM.Force(mipiPair[mipi_pair].SCLK, 0);     //SCLK vector mode

            ret += myDM.DPINLevel(mipiPair[mipi_pair].SVIO, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(mipiPair[mipi_pair].SCLK, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(mipiPair[mipi_pair].SDATA, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);

            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SVIO, false, false, false, false); //High Z
            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SCLK, false, false, false, false); //High Z
            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SDATA, true, false, false, false); //Termination Mode

            ret += myDM.ConfigureVectorEngineAttribute(moduleAlias, false, false);
            ret += myDM.DPINPeriod(moduleAlias, 0, Period); //Select timing set

            ret += myDM.DPINVectorResourceAllocation(moduleAlias, 2, resourceArr); //Select how many vector set/file to used

            ////Get vector set/file path
            //string FileName_OTPLNA_Rev13 = "OTPLNA_Rev13.vec";
            //string FileName_OTPLNA_VERIFY_Rev13 = "OTPLNA_VERIFY_Rev13.vec";
           
            //string OTPLNA_Rev13 = Path.GetFullPath(FileName_OTPLNA_Rev13);
            //string OTPLNA_VERIFY_Rev13 = Path.GetFullPath(FileName_OTPLNA_VERIFY_Rev13);

            ////Load vector into DM module based on the selected vector set/file, let say now only using 2 vector set/file
            //ret += myDM.DPINVecLoad(moduleAlias, 0, 0, OTPLNA_Rev13);           //Load vector set/file 0 -> OTPLNA_Rev13
            //ret += myDM.DPINVecLoad(moduleAlias, 0, 1, OTPLNA_VERIFY_Rev13);    //Load vector set/file 1 -> OTPLNA_VERIFY_Rev13

            //added by shaz
            string filename0 = Lib_Var.VectorPATH + "OTP\\RX\\AEMULUS\\AE_OTPLNA_Rev13.vec";
            string filename1 = Lib_Var.VectorPATH + "OTP\\RX\\AEMULUS\\AE_OTPLNA_VERIFY_Rev13.vec";
            //Load vector into DM module based on the selected vector set/file, let say now only using 2 vector set/file
            if (File.Exists(filename0))
                ret += myDM.DPINVecLoad(moduleAlias, 0, 0, filename0);    //Load vector set/file 0 -> OTPLNA_Rev13
            if (File.Exists(filename1))
                ret += myDM.DPINVecLoad(moduleAlias, 0, 1, filename1);    //Load vector set/file 1 -> OTPLNA_VERIFY_Rev13

            ret += myDM.DPINOn(mipiPair[mipi_pair].SVIO);   //Turn on SVIO
            ret += myDM.DPINOn(mipiPair[mipi_pair].SCLK);   //Turn on SCLK
            ret += myDM.DPINOn(mipiPair[mipi_pair].SDATA);  //Turn on SDATA

            return ret;

        }
        public int InitMipi(int mipi_pair, int freq_Hz, double mipi_voltage)
        {
            double Threhold = 1.2;
            ret += myDM.MIPI_ConfigureClock(moduleAlias, mipiPair[mipi_pair].PAIRNO, freq_Hz);

            vih = mipi_voltage;
            vil = 0;
            //voh = Threhold; //(vih - vil) / 3;
            //vol = Threhold; // (vih - vil) / 3;
            //ioh = 0.01;
            //iol = 0.01;
            voh = 0.370;// Threhold; //(vih - vil) / 3;  //20201201
            vol = 0.369;// Threhold; // (vih - vil) / 3; //20201201
            ioh = 0.025;// 0.01; //202011117
            iol = 0.025;// 0.01; //202011117
            vch = mipi_voltage;
            vcl = 0;
            vt = 1.6;
            //DM482e spec
            //=====================================================
            //Driver (VIH, VIL)   -1.4V to 6V 
            //Comparator (VOH, VOL)      -2.0V to 7V
            //Current Load (IOH, IOL)    -12mA to 12mA
            //Clamp Voltage Range High Side (VCH)      -1.0V to 6.0V
            //Clamp Voltage Range Low Side (VCL)       -1.5V to 5.0V
            //Termination Voltage (VT)   -2.0V to 6.0V
            //=====================================================

            int state = 0; //Pin Electronics
            int statePMU = 1; //PMU
            int stateVIO = 2; //DIO

            //DM482e_DPINForce state  
            //=====================================================
            //0 : DM482E_CONST_FORCE_STATE_VECTOR(Pin Electronics) 
            //1 : DM482E_CONST_FORCE_STATE_PMU (Pin Measurement Unit)
            //2 : DM482E_CONST_FORCE_STATE_DIO
            //5 : DM482E_CONST_FORCE_STATE_CLOCK
            //6 : DM482E_CONST_FORCE_STATE_INVERTED_CLOCK
            //=====================================================

            #region Config MIPI

            //for Mipi
            ret += myDM.Force(mipiPair[mipi_pair].SVIO, stateVIO);
            ret += myDM.Force(mipiPair[mipi_pair].SCLK, state);
            ret += myDM.Force(mipiPair[mipi_pair].SDATA, state);

            ret += myDM.DPINLevel(mipiPair[mipi_pair].SVIO, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(mipiPair[mipi_pair].SCLK, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(mipiPair[mipi_pair].SDATA, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);

            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SVIO, false, false, false, false); //High Z
            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SCLK, false, false, false, false); //High Z
            ret += myDM.ConfigurePEAttribute(mipiPair[mipi_pair].SDATA, true, false, false, false); //Termination Mode

            ret += myDM.SetPinDirection(mipiPair[mipi_pair].SVIO, 1);

            //for PMU
            ret += myDM.ConfigurePowerLineFrequency(moduleAlias, 50);

            ret += myDM.ConfigurePMUSense(mipiPair[mipi_pair].SVIO, 0); //0 local, 1 remote 
            ret += myDM.ConfigurePMUSense(mipiPair[mipi_pair].SCLK, 0); //0 local, 1 remote 
            ret += myDM.ConfigurePMUSense(mipiPair[mipi_pair].SDATA, 0); //0 local, 1 remote 

            ret += myDM.ConfigurePMUSamplingTime(mipiPair[mipi_pair].SVIO, 0.0001, 0);
            ret += myDM.ConfigurePMUSamplingTime(mipiPair[mipi_pair].SCLK, 0.0001, 0);
            ret += myDM.ConfigurePMUSamplingTime(mipiPair[mipi_pair].SDATA, 0.0001, 0);

            #endregion

            //20201217
            ret += myDM.Force(mipiPair[mipi_pair].SVIO, 1); //PMU mode
            ret += myDM.ConfigurePMUOutputFunction(mipiPair[mipi_pair].SVIO, 0);
            ret += myDM.ConfigurePMUSamplingTime(mipiPair[mipi_pair].SVIO, 0.01, 1);
            ret += myDM.ConfigurePMUCurrentLimitRange(mipiPair[mipi_pair].SVIO, 0.025);
            ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);

            for (int i = 0; i < priorCurrentLim.Length; i++)
            {
                priorCurrentLim[i] = 0.025;
            }

            return ret;
        }
        public int InitI2C(int freq_Hz, double mipi_voltage) //20190311 Aemulus: Add this function
        {
            int ret = 0;
            int I2C_group = 1;

            //reset to default
            ret += myDM.Reset(moduleAlias);

            SCL = "P6";         //ch 6
            SDA = "P7";         //ch 7
            VDD_TEMP = "P8";    //ch 8
            VCC_ROM = "P11";    //ch 11

            ret += myDM.I2C_CHSEL(moduleAlias, I2C_group, SCL, SDA);    //use ch 6~11 => i2c_group = 1
            ret += myDM.I2C_CONFIGURE(moduleAlias, I2C_group, freq_Hz); //use ch 6~11 => i2c_group = 1
          
            vih = mipi_voltage;
            vil = 0;
            voh = (vih - vil) / 3;
            vol = (vih - vil) / 3;
            ioh = 0.001;
            iol = 0.001;
            vch = mipi_voltage;
            vcl = 0;
            vt = 1.6;

            int statePE = 0;    //Pin Electronics mode
            int steatePMU = 1;  //PMU mode
            int stateDIO = 2;   //DIO mode

            ret += myDM.Force(VDD_TEMP, stateDIO);  //DIO mode
            ret += myDM.Force(VCC_ROM, stateDIO);   //DIO mode
            ret += myDM.Force(SCL, statePE);        //PE mode
            ret += myDM.Force(SDA, statePE);        //PE mode

            ret += myDM.DPINLevel(VDD_TEMP, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(VCC_ROM, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(SCL, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
            ret += myDM.DPINLevel(SDA, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);

            ret += myDM.ConfigurePEAttribute(VDD_TEMP, false, false, false, false);
            ret += myDM.ConfigurePEAttribute(VCC_ROM, false, false, false, false);
            ret += myDM.ConfigurePEAttribute(SCL, false, false, false, false);
            ret += myDM.ConfigurePEAttribute(SDA, true, false, false, false);

            //0 = input, 1 = output
            ret += myDM.SetPinDirection(VDD_TEMP, 1);   //set as output
            ret += myDM.SetPinDirection(VCC_ROM, 1);    //set as output

            ret += myDM.DPINOn(VDD_TEMP);   //enable pin
            ret += myDM.DPINOn(VCC_ROM);    //enable pin
            ret += myDM.DPINOn(SCL);        //enable pin
            ret += myDM.DPINOn(SDA);        //enable pin

            ret += myDM.DrivePin(VDD_TEMP, 1);  //drive VDD high
            ret += myDM.DrivePin(VCC_ROM, 1);   //drive VCC high

            return ret;
        }
		
		public int InitI2C_PINOT(int freq_Hz, double driveVoltage)
		{
			int ret = 0;
			
			int I2C_Group = 1; // use Ch 6-11 
			
            SCL = "P6";         	//ch 6 pin 49
            SDA = "P11";        	//ch 11 pin 9
            VDD_TEMP = "P8";    	//ch 8 pin 45


            VPUP_EEPROM = "P10";    //ch 10 (HV5) pin 3
            SIO_EEPROM = "P7";		//ch 7 pin 15
			//20210915
			VPUP_SOCKET_EEPROM = "P2";	//ch 2 (HV1) HV1
			SIO_SOCKET_EEPROM = "P9";	//ch 9 pin 11
			
			double vih = driveVoltage; //3.3V
           // vih = 2.8; //3.3V
            double vil = 0;
            double voh = (vih - vil) / 3;
            double vol = (vih - vil) / 3;
            double ioh = 0.001;  
			double iol = 0.001;  
			double vch = driveVoltage;
			double vcl = 0;
            double vt = 1.6;
            double vt_HVref = 0.55; //for VPUP_EEPROM which is connected to HV5 (follow DM_CH10 setting)
            double vt_SIO = 0;
         //   vt_HVref = 0.75;

            int timingSetNo = 0;
			double timingSetPeriod = 1e-6;	//1us
			
			int statePE = 0;    //Pin Electronics mode
			int statePMU = 1;  //PMU mode
			int stateDIO = 2;   //DIO mode
			
			int isComErrorDetected = 0;
			
			int[] vecResourceArray = new int[4];
			vecResourceArray[0] = 1;
            vecResourceArray[1] = 1;
            vecResourceArray[2] = 1;
            vecResourceArray[3] = 1;

            // Reset module to default setting
            ret += myDM.Reset(moduleAlias);
			
			// Setup I2C, use Ch6-11 (SCL = CH6, SDA = CH11)
			ret += myDM.I2C_CHSEL(moduleAlias, I2C_Group, SCL, SDA);
			ret += myDM.I2C_CONFIGURE(moduleAlias, I2C_Group, freq_Hz);
			
			ret += myDM.Force(VDD_TEMP, stateDIO);			//DIO mode
			ret += myDM.Force(VPUP_EEPROM, stateDIO);		//DIO mode
			ret += myDM.Force(VPUP_SOCKET_EEPROM, stateDIO);//DIO mode 20210915
			ret += myDM.Force(SCL, statePE);			//PE mode
			ret += myDM.Force(SDA, statePE);			//PE mode
			ret += myDM.Force(SIO_EEPROM, statePE);		//PE mode
			ret += myDM.Force(SIO_SOCKET_EEPROM, statePE);	//PE mode 20210915
			
			ret += myDM.DPINLevel(VDD_TEMP, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
			ret += myDM.DPINLevel(SCL, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
			ret += myDM.DPINLevel(SDA, vih, vil, voh, vol, ioh, iol, vch, vcl, vt);
			ret += myDM.DPINLevel(SIO_EEPROM, vih, vil, voh, vol, ioh, iol, vch, vcl, vt_SIO);
			ret += myDM.DPINLevel(SIO_SOCKET_EEPROM, vih, vil, voh, vol, ioh, iol, vch, vcl, vt_SIO); //20210915

			//Vpup voltage is dependant on vt as it is driving from HV5
			ret += myDM.DPINLevel(VPUP_EEPROM, vih, vil, voh, vol, ioh, iol, vch, vcl, vt_HVref);
			ret += myDM.DPINLevel(VPUP_SOCKET_EEPROM, vih, vil, voh, vol, ioh, iol, vch, vcl, vt_HVref); //20210915
			
			ret += myDM.ConfigurePEAttribute(VDD_TEMP, false, false, false, false);
			ret += myDM.ConfigurePEAttribute(SCL, false, false, false, false);
			ret += myDM.ConfigurePEAttribute(SDA, true, false, false, false);
			ret += myDM.ConfigurePEAttribute(SIO_EEPROM, false, false, false, false);			//Disable input termination
			ret += myDM.ConfigurePEAttribute(SIO_SOCKET_EEPROM, false, false, false, false);	//Disable input termination 20210915

			//Enable High-Voltage mode to use HV APIs
			ret += myDM.ConfigurePEAttribute(VPUP_EEPROM, false, true, false, false); 
			ret += myDM.ConfigurePEAttribute(VPUP_SOCKET_EEPROM, false, true, false, false);	//20210915
			
			//Disable run vector on trigger/continuous
			ret += myDM.ConfigureVectorEngineAttribute(moduleAlias, false, false);
			
			//Only 3 vector sets for EEPROM SIO vector (EEPROM_PowerOnReset, EEPROM_Write, EEPROM_Read)
			ret += myDM.DPINVectorResourceAllocation(moduleAlias, 4, vecResourceArray);
			ret += myDM.DPINPeriod(moduleAlias, timingSetNo, timingSetPeriod);
			
			//0 = input, 1 = output
			ret += myDM.SetPinDirection(VDD_TEMP, 1);		//set as output
			ret += myDM.SetPinDirection(VPUP_EEPROM, 1);	//set as output
			ret += myDM.SetPinDirection(VPUP_SOCKET_EEPROM, 1); //set as output 20210915

			ret += myDM.DPINOn(VDD_TEMP);					//enable pin
			ret += myDM.DPINOn(SCL);					//enable pin
			ret += myDM.DPINOn(SDA);					//enable pin
			ret += myDM.DPINHVOn(VPUP_EEPROM);			//enable pin
			ret += myDM.DPINHVOn(VPUP_SOCKET_EEPROM);	//enable pin 20210915
			ret += myDM.DPINOn(SIO_EEPROM);
			ret += myDM.DPINOn(SIO_SOCKET_EEPROM);		//20210915

			ret += myDM.DrivePin(VPUP_EEPROM, 1);			//drive Vpup high
			ret += myDM.DrivePin(VPUP_SOCKET_EEPROM, 1);	//drive Vpup Socket high 20210915
			ret += myDM.DrivePin(VDD_TEMP, 1);			//drive VDD high
			
			//Run EEPROM_PowerOnReset to configure I2C in standard mode
			ret += EEPROM_PowerOnReset_PINOT(out isComErrorDetected);

			//20210915 added back
            //Run EEPROM_PowerOnReset to configure I2C in standard mode
            ret += EEPROM_PowerOnReset_Socket_PINOT(out isComErrorDetected);


            return ret;
		}
       
        public int switchMIPItoVec(int mipi_pair)   //20190107 Aemulus: Add this function
        {
            int ret = 0;
            ret += myDM.MIPI_Connect(moduleAlias, mipi_pair, 0);    //Disconnect mipi
            //debug
            //OnOff_VIO(false, mipi_pair);    //20190305 Aemulus: Debug - off dio
            int state_PE = 0;   //vector mode
            int state_PMU = 1;  //pmu mode
            int state_DIO = 2;  //dio mode

            //debug
            //ret += myDM.Force(mipiPair[mipi_pair].SVIO, state_PE);
            ret += myDM.Force(mipiPair[mipi_pair].SDATA, state_PE);
            ret += myDM.Force(mipiPair[mipi_pair].SCLK, state_PE);

            ////debug
            //ret += myDM.Force(mipiPair[mipi_pair].SVIO, state_PMU);
            //ret += myDM.ConfigurePMUOutputFunction(mipiPair[mipi_pair].SVIO, 0);
            //ret += myDM.ConfigurePMUCurrentLimitRange(mipiPair[mipi_pair].SVIO, 0.000100);
            //ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0.0);
            //ret += myDM.DPINOn(mipiPair[mipi_pair].SVIO);
            //ret += myDM.ConfigurePMUCurrentLevel(mipiPair[mipi_pair].SVIO, 1.8);

            return ret;
        }
        public int switchVectoMIPI(int mipi_pair)   //20190107 Aemulus: Add this function
        {
            int ret = 0;
            int state_PE = 0;   //vector mode
            int state_PMU = 1;  //pmu mode
            int state_DIO = 2;  //dio mode

            ret += myDM.Force(mipiPair[mipi_pair].SVIO, state_DIO); // since mipi using dio, switch to dio mode
            ret += myDM.Force(mipiPair[mipi_pair].SDATA, state_PE);
            ret += myDM.Force(mipiPair[mipi_pair].SCLK, state_PE);

            ret += myDM.SetPinDirection(mipiPair[mipi_pair].SVIO, 1);
            OnOff_VIO(true, mipi_pair); //on VIO
            ret += myDM.MIPI_Connect(moduleAlias, mipi_pair, 1);    //Connect mipi

            return ret;
        }
        
        public int SetMipiInputDelay(int mipi_pair, int delay)
        {
            ret += myDM.MIPI_ConfigureDelay(moduleAlias, mipi_pair, delay);
            return ret;
        }
        public int SetVecInputDelay(int mipi_pair, double delay)    //20190114 Aemulus: Add this function
        {
            ret += myDM.ConfigureInputChannelDelay(mipiPair[mipi_pair].SDATA, delay);
            return ret;
        }
        public int SetI2CInputDelay(int delay)    //20190311 Aemulus: Add this function
        {
            ret += myDM.MIPI_ConfigureDelay(moduleAlias, 0, delay);
            //debug
            //ret += myDM.MIPI_ConfigureDelay(moduleAlias, 1, delay);
            
            return ret;
        }

        #endregion

        #region RunDMVector

        public int VecWrite()   //20190107 Aemulus: Add this function
        {
            int ret = 0;
            int dm_status = 999;
            int ct = 0;

            dm_status = 99;
            while(dm_status != 0)
            {
                //status 0: completed
                //status 1: busy
                ret += myDM.AcquireVecEngineStatus(moduleAlias, out dm_status);
                
                if((dm_status == 0) || (ct == 30))
                {
                    ret += myDM.RunVector(moduleAlias, 0);  //Run vector set/file 0 -> OTPLNA_Rev13
                    break;
                }
                //20190305 Aemulus
                LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
                timer.wait(1);
                ct++;
            }
            return ret;
        }
        public int VecRead(ref int vectorfailcount)    //20190107 Aemulus: Add this function
        {
            vectorfailcount = -99999;
            int ret = 0;
            int dm_status = 999;
            int ct = 0;
            int start = -999;

            dm_status = 99;
            while (dm_status != 0)
            {
                //status 0: completed
                //status 1: busy
                ret += myDM.AcquireVecEngineStatus(moduleAlias, out dm_status);

                if ((dm_status == 0) || (ct == 30))
                {
                    ret += myDM.RunVector(moduleAlias, 1);  //Run vector set/file 1 -> OTPLNA_VERIFY_Rev13
                    break;
                }
                //20190305 Aemulus
                LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
                timer.wait(1);
                ct++;
            }

            dm_status = 99;
            while (dm_status != 0)
            {
                //status 0: completed
                //status 1: busy
                ret += myDM.AcquireVecEngineStatus(moduleAlias, out dm_status);

                if ((dm_status == 0) || (ct == 30))
                {
                    ret += myDM.AcquireVectorFailCount(moduleAlias, out vectorfailcount);   //vectorfailcount return total fail count during read back
                    break;
                }
                //20190305 Aemulus
                LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
                timer.wait(1);
                ct++;
            }

            return ret;
        }

        #endregion

        #region control mipi
        //public void VIO_OFF(int mipi_pair)
        //{
        //    ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 0); //vio drive low
        //    //20201217
        //   // ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);
        //}
        //public void VIO_ON(int mipi_pair)
        //{
        //    ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 1); //vio drive high
        //    //20201217
        //   // ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1.8);
        //}
        //public int OnOff_VIO(bool isON, int mipi_pair)
        //{
        //    if (isON)
        //    {
        //        ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 1); //vio drive high
        //       //20201217
        //       // ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1.8);
        //    }
        //    else
        //    {
        //        ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 0); //vio drive low
        //        //20201217
        //        //ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);
        //    }
        //    //20190305 Aemulus: Debug -add some delay to get stable vio before start test
        //    LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
        //    timer.wait(1);

        //    return ret;
        //}
        public void VIO_OFF(int mipi_pair)
        {
            //ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 0); //vio drive low
            //20201217
            ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);

            priorVoltage[mipi_pair] = 0;

        }
        public void VIO_ON(int mipi_pair)
        {
            //ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 1); //vio drive high
            //20201217
            //ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1.8);

            // To prevent the damage of the ESD Circuit by the fast Vio rising - Ivan
            if (priorVoltage[mipi_pair] != 1.8)
            {
                //ret += myDM.ConfigurePMUCurrentLimitRange(mipiPair[mipi_pair].SVIO, 0.025);
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0.3);
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0.6);
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1);
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1.8);

          //      Thread.Sleep(2);
            }
     

            //priorCurrentLim = 0.025;
            priorVoltage[mipi_pair] = 1.8;
        }
        public void VIO_OFF_Temp(int mipi_pair)
        {
            ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 0); //vio drive low
        }
        public void VIO_ON_Temp(int mipi_pair)
        {
            ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 1); //vio drive high
        }
        public int OnOff_VIO(bool isON, int mipi_pair)
        {
            if (isON)
            {
                //ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 1); //vio drive high
                //20201217
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 1.8);
            }
            else
            {
                //ret += myDM.DrivePin(mipiPair[mipi_pair].SVIO, 0); //vio drive low
                //20201217
                ret += myDM.ConfigurePMUVoltageLevel(mipiPair[mipi_pair].SVIO, 0);
            }
            //20190305 Aemulus: Debug -add some delay to get stable vio before start test
            LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
            timer.wait(1);

            return ret;
        }
        public int OnOff_CLKDATA(bool isON, int mipi_pair)
        {

            if (isON)
            {
                //connect mipi
                ret += myDM.MIPI_Connect(moduleAlias, mipi_pair, 1);
                ret += myDM.DPINOn(mipiPair[mipi_pair].SVIO);
                ret += myDM.DPINOn(mipiPair[mipi_pair].SCLK);
                ret += myDM.DPINOn(mipiPair[mipi_pair].SDATA);
            }
            else
            {
                //disconnect mipi
                ret += myDM.MIPI_Connect(moduleAlias, mipi_pair, 0);
                ret += myDM.DPINOff(mipiPair[mipi_pair].SVIO);
                ret += myDM.DPINOff(mipiPair[mipi_pair].SCLK);
                ret += myDM.DPINOff(mipiPair[mipi_pair].SDATA);
            }

            return ret;
        }
        public int ClampCurrent(string PinAlias, double currentLevel)
        {
            ret += myDM.ConfigurePMUCurrentLimitRange(PinAlias, currentLevel);
            //priorCurrentLim = currentLevel;
            return ret;
        }
        public int DriveVoltage(string PinAlias, double voltageLevel)
        {
            ret += myDM.ConfigurePMUVoltageLevel(PinAlias, voltageLevel);
            return ret;
        }
        /// <summary>
        /// Register Read
        /// </summary>
        /// <param name="mipi_pair">pair 0 = pin 0,1,2 (CLK DATA VIO); pair 1 = pin 3,4,5 (CLK DATA VIO)...pair 3 max</param>
        /// <param name="slaveaddress">max 0x0f hex</param>
        /// <param name="address">max 0x1f hex [4:0]</param>
        /// <param name="data">max 0xff hex [7:0]</param>
        /// <param name="isfullSpeed">true = fullspeed(26MHz), false = halfspeed(13MHz)</param>
        /// <returns>0 = no error</returns>
        public int Mipi_Read(int mipi_pair, int slaveaddress, int address, int data, bool isfullSpeed)
        {
            int speed = 0;
            //full speed of half speed read
            if (isfullSpeed)
                speed = 1;
            else
                speed = 0;

            //command frame
            int command = (slaveaddress << 8) + 0x60 + (address & 0x1f);

            //data frame
            int[] tempdata = new int[1];
            tempdata[0] = data;

            //reg read
            ret += myDM.MIPI_Read(moduleAlias, mipi_pair, speed, command, tempdata);

            return ret;
        }
        /// <summary>
        /// Register Write
        /// </summary>
        /// <param name="mipi_pair">pair 0 = pin 0,1,2 (CLK DATA VIO); pair 1 = pin 3,4,5 (CLK DATA VIO)...pair 3 max</param>
        /// <param name="slaveaddress">max 0x0f hex</param>
        /// <param name="address">max 0x1f hex [4:0]</param>
        /// <param name="data">max 0xff (1 byte data)[7:0]</param>
        /// <returns></returns>
        public int Mipi_Write(int mipi_pair, int slaveaddress, int address, int data)
        {
            //command frame
            int command = ((slaveaddress & 0x1f) << 8) + 0x40 + (address & 0x1f);

            //data frame
            int[] tempdata = new int[1];
            tempdata[0] = data;

            //reg write
            ret += myDM.MIPI_Write(moduleAlias, mipi_pair, command, tempdata);//DM482.DM482e_MIPI_RFFE_WR(session, mipi_pair, command, tempdata);

            return ret;
        }
        public int Mipi_Retrieve(int mipi_pair, out int rd_byte_data_count, int[] rd_data_array, out int[] rd_data_array_hex, int[] parity_check_array)
        {
            int rd_byte_data_count_ = 0;
            ret += myDM.MIPI_Retrieve(moduleAlias, mipi_pair, ref rd_byte_data_count_, rd_data_array, parity_check_array);

            //decode to hex value
            rd_data_array_hex = new int[rd_data_array.Length];
            for (int i = 0; i < rd_data_array.Length; i++)
            {
                rd_data_array_hex[i] = decodetohexvalue(rd_data_array[i]);
            }
            rd_byte_data_count = rd_byte_data_count_;
            return ret;
        }
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
        /// Extended Register Read
        /// </summary>
        /// <param name="mipi_pair">pair 0 = pin 0,1,2 (CLK DATA VIO); pair 1 = pin 3,4,5 (CLK DATA VIO)...pair 3 max</param>
        /// <param name="slaveaddress">max 0x0f hex</param>
        /// <param name="address">max 0xff (1 byte data)[7:0]</param>
        /// <param name="data">max 16 array size</param>
        /// <param name="byteCount">max 16</param>
        /// <param name="isfullSpeed">true = fullspeed(26MHz), false = halfspeed(13MHz)</param>
        /// <returns>0 = no error</returns>
        public int Mipi_Read_ext(int mipi_pair, int slaveaddress, int address, int data, int byteCount, bool isfullSpeed)
        {
            int speed = 0;

            //full speed of half speed read
            if (isfullSpeed)
                speed = 1;
            else
                speed = 0;

            //command frame
            int command = (slaveaddress << 8) + 0x20 + (byteCount & 0x0f);

            //data frame
            int[] Address_data = new int[1];
            Address_data[0] = address;

            //reg read
            ret += myDM.MIPI_Read(moduleAlias, mipi_pair, speed, command, Address_data);

            return ret;
        }
        /// Extended Register Write 
        /// </summary>
        /// <param name="mipi_pair">pair 0 = pin 0,1,2 (CLK DATA VIO); pair 1 = pin 3,4,5 (CLK DATA VIO)...pair 3 max</param>
        /// <param name="slaveaddress">max 0x0f hex</param>
        /// <param name="Address">max 0xff [7:0]</param>
        /// <param name="data">max 16 data array</param>
        /// <param name="byteCount">max 16</param>
        /// <returns></returns>
        public int Mipi_Write_ext(int mipi_pair, int slaveaddress, int Address, int data, int byteCount)
        {
            //command frame
            int command = ((slaveaddress & 0x0f) << 8) + (byteCount & 0x0f);

            //data frame
            int[] Address_data = new int[byteCount + 2];
            Address_data[0] = Address;
            for (int i = 0; i <= byteCount; i++)
            {
                Address_data[i + 1] = data;
            }

            //reg write
            ret += ret += myDM.MIPI_Write(moduleAlias, mipi_pair, command, Address_data);//DM482.DM482e_MIPI_RFFE_WR(session, mipi_pair, command, Address_data);

            return ret;
        }
		
		
        //public int Mipi_Write_ext_long(int mipi_pair, int slaveaddress, int Address, int data, int byteCount)   //20181203 Aemulus: Add Extended Write Long Function
        //{
        //    //command frame
        //    int command = ((slaveaddress & 0x0f) << 8) + 0x30 + (byteCount & 0x07);     //0x30 - 00110000 (include bc)

        //    //data frame
        //    int[] Address_data = new int[byteCount + 3];
        //    Address_data[0] = Address >> 8;         //Address bit 15:8
        //    Address_data[1] = Address & 0x00ff;		//Address bit 7:0


        //    //int[] temp_data = new int[8];
        //    //for (int j = 0; j <= byteCount; j++)
        //    //{
        //    //    if (j == 0)
        //    //    {
        //    //        temp_data[j] = data;
        //    //    }
        //    //    else if (j == 1)
        //    //    {
        //    //        temp_data[j] = data >> 8;
        //    //    }
        //    //    else if (j == 2)
        //    //    {
        //    //        temp_data[j] = data >> 16;
        //    //    }
        //    //    else if (j == 3)
        //    //    {
        //    //        temp_data[j] = data >> 24;
        //    //    }
        //    //    else if (j == 4)
        //    //    {
        //    //        temp_data[j] = data >> 32;
        //    //    }
        //    //    else if (j == 5)
        //    //    {
        //    //        temp_data[j] = data >> 40;
        //    //    }
        //    //    else if (j == 6)
        //    //    {
        //    //        temp_data[j] = data >> 48;
        //    //    }
        //    //    else if (j == 7)
        //    //    {
        //    //        temp_data[j] = data >> 56;
        //    //    }
        //    //    else
        //    //    {
        //    //        ;
        //    //    }
        //    //}

        //    //for (int i = 0; i <= byteCount; i++)
        //    //{
        //    //    Address_data[i + 2] = temp_data[i];
        //    //}


        //    for (int i = 0; i <= byteCount; i++)
        //    {
        //        Address_data[i + 2] = data;
        //    }

        //    //reg write
        //    ret += ret += myDM.MIPI_Write(moduleAlias, mipi_pair, command, Address_data);
        //    //DM482.DM482e_MIPI_RFFE_WR(session, mipi_pair, command, Address_data);

        //    return ret;
        //}
        public int Mipi_Read_ext_long(int mipi_pair, int slaveaddress, int address, int data, int byteCount, bool isfullSpeed)  //20181203 Aemulus: Add Extended Read Long Function
        {
            int speed = 0;

            //full speed of half speed read
            if (isfullSpeed)
                speed = 1;
            else
                speed = 0;

            //command frame
            int command = (slaveaddress << 8) + 0x38 + (byteCount & 0x07);              //0x38 - 00111000 (include bc)

            //data frame
            int[] Address_data = new int[10];       //0-1 for address; 2-9 for store data read from address
            Address_data[0] = address >> 8;         //address bit 15:8
            Address_data[1] = address & 0x00ff;		//address bit 7:0
            Address_data[2] = 0x00;
            Address_data[3] = 0x00;
            Address_data[4] = 0x00;
            Address_data[5] = 0x00;
            Address_data[6] = 0x00;
            Address_data[7] = 0x00;
            Address_data[8] = 0x00;
            Address_data[9] = 0x00;

            //reg read
            ret += myDM.MIPI_Read(moduleAlias, mipi_pair, speed, command, Address_data);

            return ret;
        }
		
        #endregion

        #region test mipi
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
                        WRITE_Register_Address(MIPI_RegCond[reg_Cnt], reg_Cnt);
                }

                TRIG();

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    T_ReadSuccessful[reg_Cnt] = true;
                    regX_value[reg_Cnt] = "";

                    if (MIPI_RegCond[reg_Cnt].ToUpper() != "X")
                    {
                        Read_Register_Address(ref result, reg_Cnt);
                        regX_value[reg_Cnt] = result;
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
        public bool Register_Change_Custom(string _cmd, string _cmdTrig, bool ext_reg)
        {
            //_cmd must be in this format -> "01:01 02:00 05:00 06:40 07:00 08:00 09:00 1C:01 1C:02 1C:03 1C:04 1C:05 1C:06 1C:07"
            // ext_reg use when your register address is above 1F (5 bit)
            bool result;
            int limit = 0;

            while (true)
            {
                result = false;
                WriteRegister_Rev2(_cmd, ext_reg);
                if (_cmdTrig.ToUpper() != "NONE")
                {
                    WriteRegister_Rev2(_cmdTrig, ext_reg);      //write PM Trigger
                }
                ReadRegister_Rev2(_cmd, ext_reg, out result);
                //20201217
                double measV = 0;
                ret += myDM.PMUMeasure(mipiPair[pairNo].SVIO, 1, ref measV);

                if (result)
                    break;      //exit loop when result = true

                limit = limit + 1;
                if (limit > 10) break;      //allow 10 try before exit
            }
            return result;
        }
        public bool Register_ChangeRev2(int Mipi_Reg)
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
                            WRITE_Register_Address(MIPI_RegCond[reg_Cnt], reg_Cnt);
                    }

                    TRIG_REV2();

                    for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                    {
                        T_ReadSuccessful[reg_Cnt] = true;
                        regX_value[reg_Cnt] = "";

                        if (MIPI_RegCond[reg_Cnt].ToUpper() != "X")
                        {
                            Read_Register_Address(ref result, reg_Cnt);
                            regX_value[reg_Cnt] = result;
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
        public void TRIG()
        {
            //Mipi_Write(pairNo, slaveaddr, 0x1c, 0x03);
            Mipi_Write(pairNo, slaveaddr, pmTrigAddr, pmTrigData);
        }
        public void TRIG_REV2()
        {
            //Note : use default PM TRigger for all 7 register 
            //Default Set -> 1C:01 1C:02 1C:03 1C:04 1C:05 1C:06 1C:07
            string pmTrig = "1C:01 1C:02 1C:03 1C:04 1C:05 1C:06 1C:07";
            string[] pmTrigArray = pmTrig.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            for (int i = 0; i < pmTrigArray.Length; i++)
            {
                string[] tmpData = pmTrigArray[i].Split(':');
                Mipi_Write(pairNo, slaveaddr, Convert.ToInt32(tmpData[0], 16), Convert.ToInt32(tmpData[1],16));
            }
        }
        public void WRITE_Register_Address(string _cmd, int RegAddr)
        {
            int data = int.Parse(_cmd, System.Globalization.NumberStyles.HexNumber);
            Mipi_Write(pairNo, slaveaddr, RegAddr, data);
        }
        public string Read_Register_Address(ref string x, int RegAddr)
        {
            int bytecount = 1;
            int dum = 0x0;
            //read
            Mipi_Read(pairNo, slaveaddr, RegAddr, dum, true);

            //retrieve
            int count = 0;
            int[] dataarray = new int[bytecount + 1];
            int[] datahex = new int[bytecount + 1];
            int[] parityarray = new int[bytecount + 1];
            Mipi_Retrieve(pairNo, out count, dataarray, out datahex, parityarray);

            //Mipi_Retrieve(1, out count, dataarray, out datahex, parityarray);

            string tempresult = "";
            // F -> 0F
            if (datahex[0] <= 15)
            {
                tempresult = "0" + datahex[0].ToString("X");
            }
            else
            {
                tempresult = datahex[0].ToString("X");
            }
            x = tempresult;
            return x;
        }
        public string Read_Register_Address_Rev2(ref string x, int RegAddr, bool extReg) //20181212 - Aemulus: Add Extended Read Long condotion
        {
            int bytecount = 1;
            int dum = 0x0;
            //read
            if (!extReg)        //extReg = false
            {
                //Mipi_Read(pairNo, slaveaddr, RegAddr, dum, true);
                Mipi_Read_ext_long(pairNo, slaveaddr, RegAddr, dum, 0, true);
            }
            else                //extReg = true
            {
                Mipi_Read_ext(pairNo, slaveaddr, RegAddr, dum, 0, true);
            }
            
            //retrieve
            int count = 0;
            int[] dataarray = new int[bytecount + 1];
            int[] datahex = new int[bytecount + 1];
            int[] parityarray = new int[bytecount + 1];
            Mipi_Retrieve(pairNo, out count, dataarray, out datahex, parityarray);

            //Mipi_Retrieve(1, out count, dataarray, out datahex, parityarray);

            string tempresult = "";
            // F -> 0F
            if (datahex[0] <= 15)
            {
                tempresult = "0" + datahex[0].ToString("X");
            }
            else
            {
                tempresult = datahex[0].ToString("X");
            }
            x = tempresult;
            return x;
        }
        public void WriteRegister_Rev2(string _cmd, bool extReg)
        {
            string biasData = _cmd;
            string[] biasDataArr = biasData.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            for (int i = 0; i < biasDataArr.Length; i++)
            {
                string[] tmpData = biasDataArr[i].Split(':');
                if (!extReg)
                {
                    Mipi_Write(pairNo, slaveaddr, Convert.ToInt32(tmpData[0], 16), Convert.ToInt32(tmpData[1], 16));
                }
                else
                {
                    Mipi_Write_ext(pairNo, slaveaddr, Convert.ToInt32(tmpData[0], 16), Convert.ToInt32(tmpData[1], 16), 0);
                }
            }
        }
        public void ReadRegister_Rev2(string _cmd, bool extReg, out bool readSuccessful)
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

                Read_Register_Address_Rev2(ref result, Convert.ToInt32(tmpData[0], 16), extReg);
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
        #endregion

        #region I2C

        public void Init_I2C()
        {
            //int MIPI_pair = 0;
            InitI2C(400000, 3.3);   //20190311 Aemulus: Add this function. 20190314 BCM: use 400k Hz
            SetI2CInputDelay(2);    //depend on cable length

            //debug only
            double temp = 0;        //note temperature return out should be in rage of 25 degC
            Read_Temp(out temp);
            //debug only
            string data = "";       //test board id should be 115
            data = EEPROMread();
        }
        public void Init_I2C_PINOT(out string loadboardID, out string socketID)
        {
            //int MIPI_pair = 0;
            InitI2C_PINOT(400000, 3.3);
            SetI2CInputDelay(2);    //depend on cable length

            //debug only
            double temp = 0;        //note temperature return out should be in rage of 25 degC
            Read_Temp(out temp);
           // TempSensor_PowerDown_PINOT();

            //debug only
            //string loadboardID = "";       //test board id should be 115
            //string socketID = "";

            loadboardID = EEPROMread_PINOT();
            loadboardID = EEPROMread_PINOT();
            if (loadboardID == "")
            {
                loadboardID = "NaN";
            }
            Lib_Var.labelReadID = loadboardID;

            socketID = EEPROMread_Socket_PINOT();
            socketID = EEPROMread_Socket_PINOT();
            if (socketID == "")
            {
                socketID = "NaN";
            }
            Lib_Var.labelSocketID = socketID;

            EEPROM_PowerDown_PINOT();
        }
        //20190311 Aemulus: Add this function
        public void EEPROMwrite(string dataWrite)
        {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(dataWrite + '\0');
            if (byteArray.Length > 256)
            {
                Console.WriteLine("Exceeded maximum data length of 255 characters, \nEEPROM will not be written");
            }

            for (int tryWrite = 0; tryWrite < 5; tryWrite++)
            {
                for (ushort reg = 0; reg < byteArray.Length; reg++)
                {
                    EEPROM_Write(reg, byteArray[reg]);
                }
                if (EEPROMread() == dataWrite)
                {
                    Console.WriteLine("Writing and readback successful");
                    return;
                }
            }
            Console.WriteLine("Writing NOT successful");
        }
        public string EEPROMread()    //20199311 Aemulus: Add this function
        {
            List<byte> readDataList = new List<byte>();
            for (ushort reg = 0; reg < 256; reg++)
            {
                byte readData = EEPROM_Read(reg);
                if (readData == 0)
                {
                    break;
                }
                readDataList.Add(readData);
            }
            return System.Text.Encoding.UTF8.GetString(readDataList.ToArray());
        }
        public string EEPROMread_PINOT()    //20199311 Aemulus: Add this function
        {
            int ret = 0;
            int isComErrorDetected = 0;
            int readData_Int32 = 0;

            List<byte> readDataList = new List<byte>();
            for (ushort reg = 0; reg < 256; reg++)
            {
                //byte readData = EEPROM_Read(reg);
                ret = EEPROM_Read_PINOT(reg, out readData_Int32, out isComErrorDetected);
                if (readData_Int32 == 0)
                {
                    break;
                }
                readDataList.Add(Convert.ToByte(readData_Int32));
            }
            return System.Text.Encoding.UTF8.GetString(readDataList.ToArray());
        }
        public string EEPROMread_Socket_PINOT()    //20199311 Aemulus: Add this function; 20210915 changed pin num to "P9"
        {
            int ret = 0;
            int isComErrorDetected = 0;
            int readData_Int32 = 0;

            List<byte> readDataList = new List<byte>();
            for (ushort reg = 0; reg < 256; reg++)
            {
                //byte readData = EEPROM_Read(reg);
                ret = EEPROM_Read_Socket_PINOT(reg, out readData_Int32, out isComErrorDetected);
                if (readData_Int32 == 0)
                {
                    break;
                }
                readDataList.Add(Convert.ToByte(readData_Int32));
            }
            return System.Text.Encoding.UTF8.GetString(readDataList.ToArray());
        }
        public int EEPROM_Write(ushort reg, int data)   //20199311 Aemulus: Add this function
        {
            int ret = 0;

            //dpingroup = 1 -> i2cgroup = 0     (pin 0~5)
            //dpingroup = 2 -> i2cgroup = 0     (pin 6~11)
            //dpingroup = 3 -> i2cgroup = 0/1   (0 = pin 0~5, 1 = pin 6~11)
            int I2C_group = 1;

            //Returns the number of read back data from the I2C device.
            int rddata_count = 0;

            //Returns an array of read back data from I2C device. Sufficient memory space must be allocated.
            int[] rddata = new int[2];

            //If any bit of SDA sampled is above VOL but below VOH, then, the respective bit for i2c_rddata_biterror output will be high. Sufficient memory space must be allocated.
            int[] biterror = new int[2];

            //Specifies the timeout setting of the I2C operation, in seconds. (0.01 <= timeout <= 5second)
            double timeout = 1;

            //Specifies the number of I2C commands. (1~256)
            int Wr_command_count = 3;

            //I2C command
            //Specifies the number of I2C commands. (1~256)
            //bit0-7 is the i2c data
            //bit 8 is rsbit, if set 1, then, restart is perform after driving bit0-7
            //bit 9 is stopbit, if set 1, then, stop is perform after driving bit0-7
            //bit 10 is writebit, if set 1, then, sda bit0-7 is output during i2c operation
            //bit 11 is readbit, if set 1, then, sda bit0-7  is input during i2c operation
            //bit 12 is ackbit. If ackbit 1 during writebit 1, then, ignore ACK, no restart if no ACK from slave
            int[] Wr_command = new int[3];    //I2C Write

            //I2C write command               ack rd  wr  stop    re  8bitdata
            Wr_command[0] = 0x4A8;        //  0   0   1   0       0   10101000  //Device address = 1010, E2=1 (VCC connect), A9=0, A8=0, r/w=0
            Wr_command[1] = 0x400 + reg;  //  0   0   1   0       0   00000000  //Address = reg
            Wr_command[2] = 0x600 + data; //  0   0   1   1       0   00000000  //Data = data, stopbit = 1

            ret += myDM.I2C_START(moduleAlias, I2C_group, Wr_command_count, Wr_command, out rddata_count, rddata, biterror, timeout);

            return ret;
        }
        public byte EEPROM_Read(ushort reg) //20199311 Aemulus: Add this function
        {
            int ret = 0;
            byte readData = 0;

            //dpingroup = 1 -> i2cgroup = 0     (pin 0~5)
            //dpingroup = 2 -> i2cgroup = 0     (pin 6~11)
            //dpingroup = 3 -> i2cgroup = 0/1   (0 = pin 0~5, 1 = pin 6~11)
            int I2C_group = 1;
            
            //Returns the number of read back data from the I2C device.
            int rddata_count = 0;

            //Returns an array of read back data from I2C device. Sufficient memory space must be allocated.
            int[] rddata = new int[2];

            //If any bit of SDA sampled is above VOL but below VOH, then, the respective bit for i2c_rddata_biterror output will be high. Sufficient memory space must be allocated.
            int[] biterror = new int[2];

            //Specifies the timeout setting of the I2C operation, in seconds. (0.01 <= timeout <= 5second)
            double timeout = 1;

            //Specifies the number of I2C commands. (1~256)
            int Rd_command_count = 4;

            //I2C command
            //Specifies the number of I2C commands. (1~256)
            //bit0-7 is the i2c data
            //bit 8 is rsbit, if set 1, then, restart is perform after driving bit0-7
            //bit 9 is stopbit, if set 1, then, stop is perform after driving bit0-7
            //bit 10 is writebit, if set 1, then, sda bit0-7 is output during i2c operation
            //bit 11 is readbit, if set 1, then, sda bit0-7  is input during i2c operation
            //bit 12 is ackbit. If ackbit 1 during writebit 1, then, ignore ACK, no restart if no ACK from slave
            int[] Rd_command = new int[5];

            //I2C read command                ack rd  wr  stop    re  8bitdata
            Rd_command[0] = 0x4A8;        //  0   0   1   0       0   10101000  //Device address = 1010, E2=1 (VCC connect), A9=0, A8=0, r/w=0
            Rd_command[1] = 0x500 + reg;  //  0   0   1   0       1   00000000  //Address = reg, rsbit = 1
            Rd_command[2] = 0x4A9;        //  0   0   1   0       0   10101001  //Device address = 1010, E2=1 (VCC connect), A9=0, A8=0, r/w=1
            Rd_command[3] = 0xA00;        //  0   1   0   1       0   00000000  //stopbit = 1

            ret += myDM.I2C_START(moduleAlias, I2C_group, Rd_command_count, Rd_command, out rddata_count, rddata, biterror, timeout);

            readData = Convert.ToByte(rddata[0]);

            return readData;
        }
		//20200128 Aemulus: Change device address - PINOT Test Board
        public int EEPROM_PowerOnReset_PINOT(out int isComErrorDetected)
        {
            int ret = 0;

            //RESET-TRPT-TDACK-START-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-STOP
            //OC = Opcode
            //MA = Memory Address
            //D = Data

            int DM_LOW = 0;
            int DM_HIZ = 4;
            int totalWriteVectorLines = 1014;    //refer to I2C write vector file
            int count = 0;

            int vectorSetNo = 0;
            int timingSetNo = 0;
            int option = 0;

            double vectorTimeOut_s = 1;
            int vecEngineStatus = 0;

            int vectorStartAddress = 0;
            int SIO_Ch = 7; //DM_CH7

            //ACK - 565
            int[] ACKVectorAddress = new int[1] { 821 };    //refer back to I2C read vector file

            int tempData = 0;

            Stopwatch sw = new Stopwatch();

            int[] historyRamData = new int[totalWriteVectorLines];
            isComErrorDetected = 0; //I2C communication status

            int[] vectorGroup0 = new int[totalWriteVectorLines];    //CH5...0, trig0
            int[] vectorGroup1 = new int[totalWriteVectorLines];    //CH11...6, trig1
            int[] SIO_Ch_VectorArray = new int[totalWriteVectorLines];  //SIO_Ch is located at DM_CH7 (group1)

            #region LoadVectorArray

            string[] SIO_Ch_Vector_Format = new string[5]{"IDLE", "RESET", "TPRT", "TDACK", "STOP"};

            int[] SIO_Ch_writeDataHigh = new int[24];
            for (int i = 0; i < SIO_Ch_writeDataHigh.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_writeDataHigh[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataHigh[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_writeDataLow = new int[24];
            for (int i = 0; i < SIO_Ch_writeDataLow.Length; i++)
            {
                if (i <= 8)
                {
                    SIO_Ch_writeDataLow[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataLow[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_ACK = new int[24];
            for (int i = 0; i < SIO_Ch_ACK.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_ACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_ACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_RESET = new int[500];
            for (int i = 0; i < SIO_Ch_RESET.Length; i++)
            {
                SIO_Ch_RESET[i] = DM_LOW;
            }

            int[] SIO_Ch_IDLE = new int[300];
            for (int i = 0; i < SIO_Ch_IDLE.Length; i++)
            {
                SIO_Ch_IDLE[i] = DM_HIZ;
            }

            int[] SIO_Ch_TPRT = new int[18];
            for (int i = 0; i < SIO_Ch_TPRT.Length; i++)
            {
                SIO_Ch_TPRT[i] = DM_HIZ;
            }

            int[] SIO_Ch_TDACK = new int[24];
            for (int i = 0; i < SIO_Ch_TDACK.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_TDACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_TDACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_START = new int[160];  //160 us
            for (int i = 0; i < SIO_Ch_START.Length; i++)
            {
                SIO_Ch_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP = new int[172];   //172 us (including 12 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP.Length; i++)
            {
                SIO_Ch_STOP[i] = DM_HIZ;
            }

            for (int i = 0; i < SIO_Ch_Vector_Format.Length; i++)
            {
                if (SIO_Ch_Vector_Format[i] == "START")
                {
                    for (int j = 0; j < SIO_Ch_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_START[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "1")
                {
                    for (int j = 0; j < SIO_Ch_writeDataHigh.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataHigh[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "0")
                {
                    for (int j = 0; j < SIO_Ch_writeDataLow.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataLow[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "IDLE")
                {
                    for (int j = 0; j < SIO_Ch_IDLE.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_IDLE[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "RESET")
                {
                    for (int j = 0; j < SIO_Ch_RESET.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_RESET[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "TPRT")
                {
                    for (int j = 0; j < SIO_Ch_TPRT.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_TPRT[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "TDACK")
                {
                    for (int j = 0; j < SIO_Ch_TDACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_TDACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "ACK")
                {
                    for (int j = 0; j < SIO_Ch_ACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_ACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP")
                {
                    for (int j = 0; j < SIO_Ch_STOP.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP[j];
                    }
                }
            }

            count = 0;

            //create vector group 0 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup0[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            //create vector group 1 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup1[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (SIO_Ch_VectorArray[i] << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            ret += myDM.DPINVecLoadArray(moduleAlias, option, timingSetNo, 0, vectorSetNo, totalWriteVectorLines, vectorGroup0, vectorGroup1);

            #endregion

            #region RunVector

            ret += myDM.RunVector(moduleAlias, vectorSetNo);

            sw.Reset();
            sw.Stop();

            while (sw.Elapsed.TotalSeconds < vectorTimeOut_s)
            {
                sw.Start();
                ret += myDM.AcquireVecEngineStatus(moduleAlias, out vecEngineStatus);                   //Wait till complete

                if (vecEngineStatus == 0 || ret != 0)
                {
                    sw.Stop();
                    break;
                }

                sw.Stop();
            }

            #endregion

            #region ReadHistoryRam

            //Check all ACK bits status
            for (int i = 0; i < ACKVectorAddress.Length; i++)
            {
                ret += myDM.ReadHistoryRam(moduleAlias, 1, ACKVectorAddress[i], vectorSetNo, historyRamData);

                tempData = (historyRamData[0] >> (SIO_Ch * 2)) & 0x3;

                if (tempData == 0x01)   //Check DRACK (DRACK must be LOW) - DRACK = Discovery Response ACK 
                {
                    isComErrorDetected = 1; //not successful
                    break;
                }
            }

            #endregion

            ret += myDM.StopVector(moduleAlias);

            return ret;
        }
		 public int EEPROM_PowerOnReset_Socket_PINOT(out int isComErrorDetected)
        {
            int ret = 0;

            //RESET-TRPT-TDACK-START-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-STOP
            //OC = Opcode
            //MA = Memory Address
            //D = Data

            int DM_LOW = 0;
            int DM_HIZ = 4;
            int totalWriteVectorLines = 1014;    //refer to I2C write vector file
            int count = 0;

            int vectorSetNo = 0;
            int timingSetNo = 0;
            int option = 0;

            double vectorTimeOut_s = 1;
            int vecEngineStatus = 0;

            int vectorStartAddress = 0;
            int SIO_Ch = 9; //DM_CH7

            //ACK - 565
            int[] ACKVectorAddress = new int[1] { 821 };    //refer back to I2C read vector file

            int tempData = 0;

            Stopwatch sw = new Stopwatch();

            int[] historyRamData = new int[totalWriteVectorLines];
            isComErrorDetected = 0; //I2C communication status

            int[] vectorGroup0 = new int[totalWriteVectorLines];    //CH5...0, trig0
            int[] vectorGroup1 = new int[totalWriteVectorLines];    //CH11...6, trig1
            int[] SIO_Ch_VectorArray = new int[totalWriteVectorLines];  //SIO_Ch is located at DM_CH7 (group1)

            #region LoadVectorArray

            string[] SIO_Ch_Vector_Format = new string[5]{"IDLE", "RESET", "TPRT", "TDACK", "STOP"};

            int[] SIO_Ch_writeDataHigh = new int[24];
            for (int i = 0; i < SIO_Ch_writeDataHigh.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_writeDataHigh[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataHigh[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_writeDataLow = new int[24];
            for (int i = 0; i < SIO_Ch_writeDataLow.Length; i++)
            {
                if (i <= 8)
                {
                    SIO_Ch_writeDataLow[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataLow[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_ACK = new int[24];
            for (int i = 0; i < SIO_Ch_ACK.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_ACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_ACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_RESET = new int[500];
            for (int i = 0; i < SIO_Ch_RESET.Length; i++)
            {
                SIO_Ch_RESET[i] = DM_LOW;
            }

            int[] SIO_Ch_IDLE = new int[300];
            for (int i = 0; i < SIO_Ch_IDLE.Length; i++)
            {
                SIO_Ch_IDLE[i] = DM_HIZ;
            }

            int[] SIO_Ch_TPRT = new int[18];
            for (int i = 0; i < SIO_Ch_TPRT.Length; i++)
            {
                SIO_Ch_TPRT[i] = DM_HIZ;
            }

            int[] SIO_Ch_TDACK = new int[24];
            for (int i = 0; i < SIO_Ch_TDACK.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_TDACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_TDACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_START = new int[160];  //160 us
            for (int i = 0; i < SIO_Ch_START.Length; i++)
            {
                SIO_Ch_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP = new int[172];   //172 us (including 12 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP.Length; i++)
            {
                SIO_Ch_STOP[i] = DM_HIZ;
            }

            for (int i = 0; i < SIO_Ch_Vector_Format.Length; i++)
            {
                if (SIO_Ch_Vector_Format[i] == "START")
                {
                    for (int j = 0; j < SIO_Ch_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_START[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "1")
                {
                    for (int j = 0; j < SIO_Ch_writeDataHigh.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataHigh[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "0")
                {
                    for (int j = 0; j < SIO_Ch_writeDataLow.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataLow[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "IDLE")
                {
                    for (int j = 0; j < SIO_Ch_IDLE.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_IDLE[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "RESET")
                {
                    for (int j = 0; j < SIO_Ch_RESET.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_RESET[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "TPRT")
                {
                    for (int j = 0; j < SIO_Ch_TPRT.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_TPRT[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "TDACK")
                {
                    for (int j = 0; j < SIO_Ch_TDACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_TDACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "ACK")
                {
                    for (int j = 0; j < SIO_Ch_ACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_ACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP")
                {
                    for (int j = 0; j < SIO_Ch_STOP.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP[j];
                    }
                }
            }

            count = 0;

            //create vector group 0 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup0[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            //create vector group 1 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup1[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (SIO_Ch_VectorArray[i] << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            ret += myDM.DPINVecLoadArray(moduleAlias, option, timingSetNo, 0, vectorSetNo, totalWriteVectorLines, vectorGroup0, vectorGroup1);

            #endregion

            #region RunVector

            ret += myDM.RunVector(moduleAlias, vectorSetNo);

            sw.Reset();
            sw.Stop();

            while (sw.Elapsed.TotalSeconds < vectorTimeOut_s)
            {
                sw.Start();
                ret += myDM.AcquireVecEngineStatus(moduleAlias, out vecEngineStatus);                   //Wait till complete

                if (vecEngineStatus == 0 || ret != 0)
                {
                    sw.Stop();
                    break;
                }

                sw.Stop();
            }

            #endregion

            #region ReadHistoryRam

            //Check all ACK bits status
            for (int i = 0; i < ACKVectorAddress.Length; i++)
            {
                ret += myDM.ReadHistoryRam(moduleAlias, 1, ACKVectorAddress[i], vectorSetNo, historyRamData);

                tempData = (historyRamData[0] >> (SIO_Ch * 2)) & 0x3;

                if (tempData == 0x01)   //Check DRACK (DRACK must be LOW) - DRACK = Discovery Response ACK 
                {
                    isComErrorDetected = 1; //not successful
                    break;
                }
            }

            #endregion

            ret += myDM.StopVector(moduleAlias);

            return ret;
        }
        //20200128 Aemulus: Change device address - PINOT Test Board
        public int EEPROM_Read_PINOT(int reg, out int data, out int isComErrorDetected)
        {
            int ret = 0;

            int vecEngineStatus = 0;
            double vectorTimeOut_s = 1;

            int vectorStartAddress = 0;
            int totalWriteVectorLines = 1116;    //refer to I2C write vector file
            int vectorSetNo = 2;
            int timingSetNo = 0;
            int option = 0;

            int SIO_Ch = 7; //DM_CH7
            int tBITVecLineCount = 12;  //total of 60 vector lines per tBIT, 1us per each vector line. Hence tBIT = 60 us
            int readDataBitWidth = 8;
            int readDataStartAddress = 849; //refer back to I2C read vector file

            //ACK - 259, 367, 837
            //NACK - 945
            int[] ACKVectorAddress = new int[4] { 259, 367, 837, 945 };	//refer back to I2C read vector file

            int count = 0;
            int tempData = 0;

            data = 0;

            Stopwatch sw = new Stopwatch();

            int[] historyRamData = new int[(tBITVecLineCount * (readDataBitWidth - 1) + 1)];

            isComErrorDetected = 0; //I2C communication status

            //START-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-MA7-MA6-MA5-MA4-MA3-MA2-MA1-MA0-ACK-STOP-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-D7-D6-D5-D4-D3-D2-D1-D0-NACK-STOP
            //OC = Opcode
            //MA = Memory Address
            //D = Data

            int DM_LOW = 0;
            int DM_HIZ = 4;

            int[] vectorGroup0 = new int[totalWriteVectorLines];    //CH5...0, trig0
            int[] vectorGroup1 = new int[totalWriteVectorLines];    //CH11...6, trig1
            int[] SIO_Ch_VectorArray = new int[totalWriteVectorLines];  //SIO_Ch is located at DM_CH7 (group1)

            string[] SIO_Ch_Vector_Format = new string[39]{"START", "1", "0", "1", "0", "1", "1", "1", "0", "ACK",			//device address
														   "0", "0", "0", "0", "0", "0", "0", "0", "ACK", "STOP_START",			//memory address (element 10 to 17)
														   "1", "0", "1", "0", "1", "1", "1", "1", "ACK",					//device address
														   "R", "R", "R", "R", "R", "R", "R", "R", "NACK", "STOP"};     //read data (element 29 to 36)

            #region LoadVectorArray

            for (int i = 10; i < 18; i++)
            {
                //Memory Address Bit 7..0
                if ((reg >> (17 - i) & 0x1) == 0x1)
                {
                    SIO_Ch_Vector_Format[i] = "1";
                }
                else
                {
                    SIO_Ch_Vector_Format[i] = "0";
                }
            }

            int[] SIO_Ch_writeDataHigh = new int[12];
            for (int i = 0; i < SIO_Ch_writeDataHigh.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_writeDataHigh[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataHigh[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_writeDataLow = new int[12];
            for (int i = 0; i < SIO_Ch_writeDataLow.Length; i++)
            {
                if (i <= 8)
                {
                    SIO_Ch_writeDataLow[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataLow[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_ACK = new int[12];
            for (int i = 0; i < SIO_Ch_ACK.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_ACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_ACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_NACK = new int[12];
            for (int i = 0; i < SIO_Ch_NACK.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_NACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_NACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_START = new int[160];  //160 us
            for (int i = 0; i < SIO_Ch_START.Length; i++)
            {
                SIO_Ch_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP = new int[162];   //162 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP.Length; i++)
            {
                SIO_Ch_STOP[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP_START = new int[362];   //162 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP_START.Length; i++)
            {
                SIO_Ch_STOP_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_READDATA = new int[12];    //12 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_READDATA.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_READDATA[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_READDATA[i] = DM_HIZ;
                }
            }

            for (int i = 0; i < SIO_Ch_Vector_Format.Length; i++)
            {
                if (SIO_Ch_Vector_Format[i] == "START")
                {
                    for (int j = 0; j < SIO_Ch_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_START[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "1")
                {
                    for (int j = 0; j < SIO_Ch_writeDataHigh.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataHigh[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "0")
                {
                    for (int j = 0; j < SIO_Ch_writeDataLow.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataLow[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "R")
                {
                    for (int j = 0; j < SIO_Ch_READDATA.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_READDATA[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "ACK")
                {
                    for (int j = 0; j < SIO_Ch_ACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_ACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "NACK")
                {
                    for (int j = 0; j < SIO_Ch_NACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_NACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP")
                {
                    for (int j = 0; j < SIO_Ch_STOP.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP_START")
                {
                    for (int j = 0; j < SIO_Ch_STOP_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP_START[j];
                    }
                }
            }

            count = 0;

            //create vector group 0 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup0[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            //create vector group 1 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup1[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (SIO_Ch_VectorArray[i] << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            ret += myDM.DPINVecLoadArray(moduleAlias, option, timingSetNo, 0, vectorSetNo, totalWriteVectorLines, vectorGroup0, vectorGroup1);

            #endregion

            #region RunVector

            for (int i = 0; i < 2; i++)
            {
                ret += myDM.RunVector(moduleAlias, vectorSetNo);

                sw.Reset();
                sw.Stop();

                while (sw.Elapsed.TotalSeconds < vectorTimeOut_s)
                {
                    sw.Start();
                    ret += myDM.AcquireVecEngineStatus(moduleAlias, out vecEngineStatus);                   //Wait till complete

                    if (vecEngineStatus == 0 || ret != 0)
                    {
                        sw.Stop();
                        break;
                    }

                    sw.Stop();
                }
            }

            Thread.Sleep(1);

            #endregion

            #region ReadHistoryRAM

            //Check all ACK bits status
            for (int i = 0; i < ACKVectorAddress.Length; i++)
            {
                ret += myDM.ReadHistoryRam(moduleAlias, 1, ACKVectorAddress[i], vectorSetNo, historyRamData);

                tempData = (historyRamData[0] >> (SIO_Ch * 2)) & 0x3;

                if (i == 3)	//Check NACK (NACK must be HIGH)
                {
                    if (tempData == 0x00)
                    {
                        isComErrorDetected = 1; //not successful
                        break;
                    }
                }
                else	//Check ACK (ACK must be LOW)
                {
                    if (tempData == 0x01)
                    {
                        isComErrorDetected = 1; //not successful
                        break;
                    }
                }
            }

            if (isComErrorDetected == 1)
            {
                goto END_TEST;
            }

            ret += myDM.ReadHistoryRam(moduleAlias, (tBITVecLineCount * (readDataBitWidth - 1) + 1), readDataStartAddress, vectorSetNo, historyRamData);

            for (int i = 0; i <= tBITVecLineCount * (readDataBitWidth - 1); i += tBITVecLineCount)
            {
                //AND with 0x3 since history RAM logic data only represented by 2 bits
                tempData = (historyRamData[i] >> (SIO_Ch * 2)) & 0x3;

                if (tempData == 0x00)   //Logic Low
                {
                    data += (0 << ((readDataBitWidth - 1) - count));
                }
                else if (tempData == 0x01)  //Logic High
                {
                    data += (1 << ((readDataBitWidth - 1) - count));
                }
                count++;
            }

        #endregion

        END_TEST:

            ret += myDM.StopVector(moduleAlias);

            return ret;
        }
        public int EEPROM_Read_Socket_PINOT(int reg, out int data, out int isComErrorDetected)
        {
            int ret = 0;

            int vecEngineStatus = 0;
            double vectorTimeOut_s = 1;

            int vectorStartAddress = 0;
            int totalWriteVectorLines = 1116;    //refer to I2C write vector file
            int vectorSetNo = 3;
            int timingSetNo = 0;
            int option = 0;

            int SIO_Ch = 9;//7; //DM_CH7 20210915 (changed pin from 7 to 9)
            int tBITVecLineCount = 12;  //total of 60 vector lines per tBIT, 1us per each vector line. Hence tBIT = 60 us
            int readDataBitWidth = 8;
            int readDataStartAddress = 849; //refer back to I2C read vector file

            //ACK - 259, 367, 837
            //NACK - 945
            int[] ACKVectorAddress = new int[4] { 259, 367, 837, 945 };	//refer back to I2C read vector file

            int count = 0;
            int tempData = 0;

            data = 0;

            Stopwatch sw = new Stopwatch();

            int[] historyRamData = new int[(tBITVecLineCount * (readDataBitWidth - 1) + 1)];

            isComErrorDetected = 0; //I2C communication status

            //START-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-MA7-MA6-MA5-MA4-MA3-MA2-MA1-MA0-ACK-STOP-OC3-OC2-OC1-OC0-A2-A1-A0-R/W*-ACK-D7-D6-D5-D4-D3-D2-D1-D0-NACK-STOP
            //OC = Opcode
            //MA = Memory Address
            //D = Data

            int DM_LOW = 0;
            int DM_HIZ = 4;

            int[] vectorGroup0 = new int[totalWriteVectorLines];    //CH5...0, trig0
            int[] vectorGroup1 = new int[totalWriteVectorLines];    //CH11...6, trig1
            int[] SIO_Ch_VectorArray = new int[totalWriteVectorLines];  //SIO_Ch is located at DM_CH7 (group1) 20210915 (SIO_ch = 9)

            string[] SIO_Ch_Vector_Format = new string[39]{"START", "1", "0", "1", "0", "0", "0", "0", "0", "ACK",			//device address
														   "0", "0", "0", "0", "0", "0", "0", "0", "ACK", "STOP_START",			//memory address (element 10 to 17)
														   "1", "0", "1", "0", "0", "0", "0", "1", "ACK",					//device address
														   "R", "R", "R", "R", "R", "R", "R", "R", "NACK", "STOP"};     //read data (element 29 to 36)

            #region LoadVectorArray

            for (int i = 10; i < 18; i++)
            {
                //Memory Address Bit 7..0
                if ((reg >> (17 - i) & 0x1) == 0x1)
                {
                    SIO_Ch_Vector_Format[i] = "1";
                }
                else
                {
                    SIO_Ch_Vector_Format[i] = "0";
                }
            }

            int[] SIO_Ch_writeDataHigh = new int[12];
            for (int i = 0; i < SIO_Ch_writeDataHigh.Length; i++)
            {
                if (i <= 2)
                {
                    SIO_Ch_writeDataHigh[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataHigh[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_writeDataLow = new int[12];
            for (int i = 0; i < SIO_Ch_writeDataLow.Length; i++)
            {
                if (i <= 8)
                {
                    SIO_Ch_writeDataLow[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_writeDataLow[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_ACK = new int[12];
            for (int i = 0; i < SIO_Ch_ACK.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_ACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_ACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_NACK = new int[12];
            for (int i = 0; i < SIO_Ch_NACK.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_NACK[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_NACK[i] = DM_HIZ;
                }
            }

            int[] SIO_Ch_START = new int[160];  //160 us
            for (int i = 0; i < SIO_Ch_START.Length; i++)
            {
                SIO_Ch_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP = new int[162];   //162 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP.Length; i++)
            {
                SIO_Ch_STOP[i] = DM_HIZ;
            }

            int[] SIO_Ch_STOP_START = new int[362];   //162 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_STOP_START.Length; i++)
            {
                SIO_Ch_STOP_START[i] = DM_HIZ;
            }

            int[] SIO_Ch_READDATA = new int[12];    //12 us (including 2 us of tRCV)
            for (int i = 0; i < SIO_Ch_READDATA.Length; i++)
            {
                if (i <= 1)
                {
                    SIO_Ch_READDATA[i] = DM_LOW;
                }
                else
                {
                    SIO_Ch_READDATA[i] = DM_HIZ;
                }
            }

            for (int i = 0; i < SIO_Ch_Vector_Format.Length; i++)
            {
                if (SIO_Ch_Vector_Format[i] == "START")
                {
                    for (int j = 0; j < SIO_Ch_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_START[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "1")
                {
                    for (int j = 0; j < SIO_Ch_writeDataHigh.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataHigh[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "0")
                {
                    for (int j = 0; j < SIO_Ch_writeDataLow.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_writeDataLow[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "R")
                {
                    for (int j = 0; j < SIO_Ch_READDATA.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_READDATA[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "ACK")
                {
                    for (int j = 0; j < SIO_Ch_ACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_ACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "NACK")
                {
                    for (int j = 0; j < SIO_Ch_NACK.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_NACK[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP")
                {
                    for (int j = 0; j < SIO_Ch_STOP.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP[j];
                    }
                }
                else if (SIO_Ch_Vector_Format[i] == "STOP_START")
                {
                    for (int j = 0; j < SIO_Ch_STOP_START.Length; j++)
                    {
                        SIO_Ch_VectorArray[count++] = SIO_Ch_STOP_START[j];
                    }
                }
            }

            count = 0;

            //create vector group 0 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
                vectorGroup0[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            //create vector group 1 data
            for (int i = 0; i < totalWriteVectorLines; i++)
            {
				//20210915, changed SIO_ch array to ch 9
                vectorGroup1[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (SIO_Ch_VectorArray[i] << 10) + (DM_HIZ << 7) +
                                  (DM_HIZ << 4) + (DM_HIZ << 1) + DM_LOW;
				//vectorGroup1[i] = (DM_HIZ << 16) + (DM_HIZ << 13) + (DM_HIZ << 10) + (DM_HIZ << 7) +
                //                  (SIO_Ch_VectorArray[i] << 4) + (DM_HIZ << 1) + DM_LOW;
            }

            ret += myDM.DPINVecLoadArray(moduleAlias, option, timingSetNo, 0, vectorSetNo, totalWriteVectorLines, vectorGroup0, vectorGroup1);

            #endregion

            #region RunVector

            for(int i = 0; i < 2; i++)
            {
                ret += myDM.RunVector(moduleAlias, vectorSetNo);

                sw.Reset();
                sw.Stop();

                while (sw.Elapsed.TotalSeconds < vectorTimeOut_s)
                {
                    sw.Start();
                    ret += myDM.AcquireVecEngineStatus(moduleAlias, out vecEngineStatus);                   //Wait till complete

                    if (vecEngineStatus == 0 || ret != 0)
                    {
                        sw.Stop();
                        break;
                    }

                    sw.Stop();
                }
            }

            Thread.Sleep(1);

            #endregion

            #region ReadHistoryRAM

            //Check all ACK bits status
            for (int i = 0; i < ACKVectorAddress.Length; i++)
            {
                ret += myDM.ReadHistoryRam(moduleAlias, 1, ACKVectorAddress[i], vectorSetNo, historyRamData);

                tempData = (historyRamData[0] >> (SIO_Ch * 2)) & 0x3;

                if (i == 3)	//Check NACK (NACK must be HIGH)
                {
                    if (tempData == 0x00)
                    {
                        isComErrorDetected = 1; //not successful
                        break;
                    }
                }
                else	//Check ACK (ACK must be LOW)
                {
                    if (tempData == 0x01)
                    {
                        isComErrorDetected = 1; //not successful
                        break;
                    }
                }
            }

            if (isComErrorDetected == 1)
            {
                goto END_TEST;
            }

            ret += myDM.ReadHistoryRam(moduleAlias, (tBITVecLineCount * (readDataBitWidth - 1) + 1), readDataStartAddress, vectorSetNo, historyRamData);

            for (int i = 0; i <= tBITVecLineCount * (readDataBitWidth - 1); i += tBITVecLineCount)
            {
                //AND with 0x3 since history RAM logic data only represented by 2 bits
                tempData = (historyRamData[i] >> (SIO_Ch * 2)) & 0x3;

                if (tempData == 0x00)   //Logic Low
                {
                    data += (0 << ((readDataBitWidth - 1) - count));
                }
                else if (tempData == 0x01)  //Logic High
                {
                    data += (1 << ((readDataBitWidth - 1) - count));
                }
                count++;
            }

        #endregion

        END_TEST:

            ret += myDM.StopVector(moduleAlias);

            return ret;
        }
        //20190311 Aemulus: Add this function
        public int Read_Temp(out double temp)
        {
            int ret = 0;

            int I2C_group = 1;
            int command_count = 4;
            int[] command = new int[5];
            int rddata_count = 0;
            int[] rddata = new int[2];
            int[] biterror = new int[2];
            double timeout = 1; 

            Thread.Sleep(20);       //in milliseconds      
            
            //I2C read command          ack   rd  wr  stop    rs  8bit data            
            command[0] = 0x490;     //  0     0   1   0       0   10010000     //From datasheet: i2c bus address = 10010, A0 = 0, A1 = 0, r/w=0
            command[1] = 0x500;     //  0     0   1   0       1   00000000     //Register = 0x0 & 1, read two byte
            command[2] = 0x491;     //  0     0   1   0       0   10010001     //From datasheet: i2c bus address = 10010, A0 = 0, A1 = 0, r/w=1
            command[3] = 0x1800;    //  1     1   0   0       0   00000000     //acknowledge, then followed by reading the second byte
            command[4] = 0xA00;     //  0     1   0   1       0   00000000     //stop

            my482Watch.Reset();
            my482Watch.Start();

            ret += myDM.I2C_START(moduleAlias, I2C_group, command_count + 1, command, out rddata_count, rddata, biterror, timeout);

            my482Watch.Stop();

            float my482tx = my482Watch.ElapsedMilliseconds;

            TempConv(rddata, out temp); //Convert result

            return ret;
        }
        public double TempConv(int[] rddata, out double temp)   //20190311 Aemulus: Add this funcion
        {
            int data = 0;
            //double temp = 0;
            data = (rddata[0] << 8) + rddata[1];
            if ((data >> 15) == 1)  //To check whether the result is positive or negative sign (bit-15 is sign bit)
            {
                //Negative conversion
                return temp = (double)((data >> 3) - 8192) / 16;
            }
            else
            {
                //Positive conversion
                return temp = (double)(data >> 3) / 16;
            }
        }
        public int EEPROM_PowerDown_PINOT()
        {
            int ret = 0;

            ret += myDM.DrivePin(SIO_EEPROM, 0);			//drive SIO Low
            ret += myDM.DrivePin(VPUP_EEPROM, 0);			//drive Vpup Low
			ret += myDM.DrivePin(SIO_SOCKET_EEPROM, 0);		//drive SIO Socket Low 20210915
			ret += myDM.DrivePin(VPUP_SOCKET_EEPROM, 0);	//drive Vpup Socket Low 20210915

            ret += myDM.DPINOff(SIO_EEPROM);            //disable pin
            ret += myDM.DPINHVOff(VPUP_EEPROM);			//disable pin
			ret += myDM.DPINOff(SIO_SOCKET_EEPROM);		//disable pin 20210915
			ret += myDM.DPINHVOff(VPUP_SOCKET_EEPROM);	//disable pin 20210915

            return ret;
        }
        public int TempSensor_PowerDown_PINOT()
        {
            int ret = 0;

            ret += myDM.DrivePin(SCL, 0);			//drive SCL LOW
            ret += myDM.DrivePin(SDA, 0);			//drive SDA LOW
            ret += myDM.DrivePin(VDD_TEMP, 0);			//drive VDD LOW

            ret += myDM.DPINOff(SCL);					//disable pin
            ret += myDM.DPINOff(SDA);					//disable pin 
            ret += myDM.DPINOff(VDD_TEMP);				//disable pin

            return ret;
        }

        public void ReadMIPICodesCustom(out int[] ReadResult, out bool bPass, string MipiRegMap, string PMTRigMap, int MipiPairNo, int SlaveAddr)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
