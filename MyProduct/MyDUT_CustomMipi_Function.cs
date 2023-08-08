using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Avago.ATF.StandardLibrary;
using Ivi.Visa.Interop;
using LibEqmtDriver;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
//using ni_NoiseFloor;
using NationalInstruments.RFmx.InstrMX;
using NationalInstruments.RFmx.SpecAnMX;
using MPAD_TestTimer;
using TCPHandlerProtocol;
using ni_NoiseFloorWrapper;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using System.Threading.Tasks;
using ClothoSharedItems;
using Avago.ATF.Shares;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MyProduct
{
    public partial class MyDUT : IDisposable
    {
        #region Small Function for OTP & MIPI
        public void JediOTPBurn(string efuseCtlReg_hex, int[] efuseDataByteNum, string data_hex, string cusMipiPair, string cusSlaveAddr, bool invertData = false)
        {
            //EFuse Control Register Definition (0xC0) - efuseCtlReg_hex
            //Bit7[program] Bit6[NA] Bit5[a2] Bit4[a1] Bit3[a0] Bit2[b2] Bit1[b1] Bit0[b0] - burnDataDec
            //a[2:0]:	Address of six 8-bit eFuse cells - efuseDataByteNum[x]
            //b[2:0]:	Bit address of the 8-bit eFuse cells - data_hex

            int programMode = 1;

            try
            {
                int data_dec = Convert.ToInt32(data_hex, 16);

                if (data_dec > 255)
                {
                    MessageBox.Show("Error: Cannot burn decimal values greater than 255", "BurnOTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // burn the data - Bit by Bit
                for (int bit = 0; bit < 8; bit++)
                {
                    int bitVal = (int)Math.Pow(2, bit);

                    if ((bitVal & data_dec) == (invertData ? 0 : bitVal))
                    {
                        int burnDataDec = (programMode << 7) + (efuseDataByteNum[2] << 5) + (efuseDataByteNum[1] << 4) + (efuseDataByteNum[0] << 3) + bit;

                        // Convert integer as a hex in a string variable
                        string hexValue = burnDataDec.ToString("X");
                        Eq.Site[0]._EqMiPiCtrl.WriteOTPRegister(efuseCtlReg_hex, hexValue, Convert.ToInt16(cusMipiPair), Convert.ToInt32((cusSlaveAddr), 16));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "OTP Burn Error");
            }
        }
        public void Sort_MSBnLSB(Int32 decData, out string ID_MSB, out string ID_LSB)
        {
            int OtpExpectedValue1 = 52;
            int OtpExpectedValue2 = 1;
            ID_MSB = "0x00";            //set to default
            ID_LSB = "0x01";            //set to default
            int math1, math1a, math2;

            if (true)
            {
                math1 = Convert.ToInt32(decData / 256);
                if ((math1 * 256) > decData)
                {
                    math1 = math1 - 1;
                }
                math1a = math1 * 256;
                math2 = decData - math1a;
                OtpExpectedValue2 = math1;
                OtpExpectedValue1 = math2;

                ID_MSB = decToHex(OtpExpectedValue2);
                ID_LSB = decToHex(OtpExpectedValue1);
            }
        }
        public string decToHex(int number)
        {
            int number1a, number1b, number1c;
            string hex1, hex2;
            string number2 = "0x00";

            number1a = Convert.ToInt32(number / 16);

            if ((number1a * 16) > number)
            {
                number1a = number1a - 1;
            }
            number1b = number1a * 16;
            number1c = number - number1b;

            hex1 = decToHex2(number1a);
            hex2 = decToHex2(number1c);

            number2 = "0x" + hex1 + hex2;

            return number2;
        }
        public string decToHex2(int number)
        {
            string hexa = "0";
            switch (number)
            {
                case 0:
                    hexa = "0";
                    break;
                case 1:
                    hexa = "1";
                    break;
                case 2:
                    hexa = "2";
                    break;
                case 3:
                    hexa = "3";
                    break;
                case 4:
                    hexa = "4";
                    break;
                case 5:
                    hexa = "5";
                    break;
                case 6:
                    hexa = "6";
                    break;
                case 7:
                    hexa = "7";
                    break;
                case 8:
                    hexa = "8";
                    break;
                case 9:
                    hexa = "9";
                    break;
                case 10:
                    hexa = "A";
                    break;
                case 11:
                    hexa = "B";
                    break;
                case 12:
                    hexa = "C";
                    break;
                case 13:
                    hexa = "D";
                    break;
                case 14:
                    hexa = "E";
                    break;
                case 15:
                    hexa = "F";
                    break;

                default:
                    throw new Exception("Can't convert number to Hex: " + number);
            }

            return hexa;

        }
        public void dutTempSensor(double data_dec, out double dutTempC)
        {
            //init to default 
            dutTempC = -999;
            double tempCalc = -999;
            double minTempC = -20;
            double maxTempC = 130;

            //Note : Temp Sensor range from -20C to 130C
            //0x00 -> -20C , 0xFF -> 130C , so for 0x00 to 0xFF -> temperature range = 130C - (-20C) = 150C
            //temp change per point = 150C/255 = 0.588C
            //equation to convert data_dec from register to tempC
            double dutTempRangeC = maxTempC - minTempC;
            tempCalc = (data_dec * (dutTempRangeC / 255)) + minTempC;
            dutTempC = Math.Round(tempCalc, 3);
        }
        public void searchMIPIKey(string testParam, string searchKey, out string CusMipiRegMap, out string CusPMTrigMap, out string CusSlaveAddr, out string CusMipiPair, out string CusMipiSite, out bool b_mipiTKey)
        {
            //initialize variable - reset to default
            b_mipiTKey = false;
            CusMipiRegMap = null;
            CusPMTrigMap = null;
            CusSlaveAddr = null;
            CusMipiPair = null;
            CusMipiSite = null;

            //Data from Mipi custom spreadsheet 
            foreach (Dictionary<string, string> currMipiReg in DicMipiKey)
            {
                currMipiReg.TryGetValue("MIPI KEY", out DicMipiTKey);

                DicMipiTKey = DicMipiTKey.ToUpper();

                if (searchKey.ToUpper() == DicMipiTKey)
                {
                    currMipiReg.TryGetValue("REGMAP", out CusMipiRegMap);
                    currMipiReg.TryGetValue("TRIG", out CusPMTrigMap);
                    currMipiReg.TryGetValue("SLAVEADDR", out CusSlaveAddr);
                    currMipiReg.TryGetValue("MIPI_PAIR", out CusMipiPair);
                    currMipiReg.TryGetValue("MIPI_SITE", out CusMipiSite);
                    b_mipiTKey = true;          //change flag if match
                }
            }

            if (!b_mipiTKey)        //if cannot find , show error
                MessageBox.Show("Failed to find MIPI KEY (" + searchKey.ToUpper() + ") in MIPI sheet \n\n", testParam.ToUpper(), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public void readout_OTPReg_viaEffectiveBit(int delay_mSec, string SwBand, string CusMipiRegMap, string CusPMTrigMap, string CusSlaveAddr, string CusMipiPair, string CusMipiSite, out int decDataOut, out string dataSizeHex)
        {
            Stopwatch tTime111 = new Stopwatch();

            tTime111.Reset();
            tTime111.Start();

            biasDataArr = null;
            dataSizeHex = null;
            decDataOut = 0;

            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            //Init variable & sorted effective bit data
            appendBinary = null;
            dataDec = new int[biasDataArr.Length];
            dataBinary = new string[biasDataArr.Length];

            //Note : effective bit are selected if any of the bit is set to '1' in CusMipiRegMap data column (in hex format >> register:effective bits) => 0x42:0x03
            //example CusMipiRegMap must be in '42:03 43:FF' where 0x43 is LSB reg address and 0xFF (1111 1111) all 8 bits are to be effectively read
            //0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read

            //example after MIPI read => reg 0x43 = data read 0xCB (11001011) &  reg 0x42 = data read 0x2E (00101110)
            //Effective bit decode example => for reg 0x42 since all 8bits will be use , effectiveBitData(0xCB) = 11001011
            //while reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
            //reported data => 10 11001011 => convert to dec = 715

            //Set MIPI and Read MIPI

            Eq.Site[0]._EqMiPiCtrl.TurnOn_VIO(Convert.ToInt16(CusMipiPair));        //mipi pair - derive from MIPI spereadsheet
       //     DelayMs(delay_mSec);

            double test1 = tTime111.ElapsedMilliseconds;

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                Eq.Site[0]._EqMiPiCtrl.ReadMIPICodesCustom(out tmpOutData, biasDataArr[i], CusPMTrigMap, Convert.ToInt16(CusMipiPair), Convert.ToInt32((CusSlaveAddr), 16));
                dataDec[i] = tmpOutData;
                dataBinary[i] = Convert.ToString(tmpOutData, 2).PadLeft(8, '0');        //Convert DEC to 8 Bit Binary
            }

            double test2 = tTime111.ElapsedMilliseconds;
            //sorted out MIPI data base effective bit selection and publish result
            for (int i = 0; i < biasDataArr.Length; i++)
            {
                //sort out the effective bit - register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                int tempReg_Dec = 0;
                string[] tempReg_Hex = new string[2];
                tempReg_Hex = biasDataArr[i].Split(':');

                try
                {
                    tempReg_Dec = int.Parse(tempReg_Hex[1], System.Globalization.NumberStyles.HexNumber);       //convert Effective BIT for given register address from HEX to Decimal
                    dataSizeHex = dataSizeHex + tempReg_Hex[1];
                }
                catch (Exception)
                {
                    MessageBox.Show("!!! WRONG SELECTIVE BIT FORMAT !!!\n" +
                        "DATA MUST BE IN HEX FORMAT (" + SwBand + " : " + biasDataArr[i] + ")\n" +
                        "PLS CHECK & FIXED IN MIPI WORKSHEET");
                }

                string tempReg_Binary = Convert.ToString(tempReg_Dec, 2).PadLeft(8, '0');                       //Convert DEC to 8 Bit Binary

                //sort out the effective data base of effective bit of a given register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                //example after MIPI read => reg 0x42 = data read 0x2E (00101110)
                //reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
                char[] selectiveBitReg_Binary = new char[8];
                char[] selectiveData_Binary = new char[8];

                //stored in charArray format in Binary form
                selectiveBitReg_Binary = tempReg_Binary.ToCharArray();
                selectiveData_Binary = dataBinary[i].ToCharArray();

                for (int j = 0; j < selectiveBitReg_Binary.Length; j++)
                {
                    if (selectiveBitReg_Binary[j] == '1')
                    {
                        appendBinary = appendBinary + selectiveData_Binary[j];   //construct and concatenations binary data bit by bit
                    }
                }
            }
            double test3 = tTime111.ElapsedMilliseconds;
            tTime111.Stop();
            decDataOut = Convert.ToInt32(appendBinary, 2);            //Convert Binary to Decimal
        }
        public void burn_OTPReg_viaEffectiveBit(string testParam, string CusMipiRegMap, string CusMipiPair, string CusSlaveAddr, string[] dataHex)
        {
            #region Decode MIPI Register and Burn OTP
            biasDataArr = null;

            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            int[] efuseCtrlAddress = new int[4];
            string[] tempData = new string[2];

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                //Note : EFuse Control Register
                //efuse cell_0 (0xE0, mirror address 0x0D)
                //efuse cell_1 (0xE1, mirror address 0x0E)
                //efuse cell_2 (0xE2, mirror address 0x21)
                //efuse cell_3 (0xE3, mirror address 0x40)
                //efuse cell_4 (0xE4, mirror address 0x41)

                tempData = biasDataArr[i].Split(':');
                switch (tempData[0].ToUpper())
                {
                    case "E0":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E1":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E2":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E3":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E4":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E5":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E6":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E7":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 1;
                        break;
                    default:
                        MessageBox.Show("Test Parameter : " + testParam + "(" + tempData[0].ToUpper() + ") - OTP Address not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                        break;
                }

                #region Burn OTP Data

                if (BurnOTP)
                {
                    //Burn3x to double confirm the otp programming is done completely
                    for (int cnt = 0; cnt < 3; cnt++)
                    {
                        JediOTPBurn("C0", efuseCtrlAddress, dataHex[i], CusMipiPair, CusSlaveAddr);
                    }
                }

                #endregion
            }

            #endregion
        }
        public void mask_viaEffectiveBit(string[] dataHex, string SwBand, string CusMipiRegMap, out int decDataOut)
        {
            biasDataArr = null;
            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            //Init variable & sorted effective bit data
            dataSizeHex = null;
            decDataOut = 0;
            appendBinary = null;
            dataDec = new int[biasDataArr.Length];
            dataBinary = new string[biasDataArr.Length];

            //Note : effective bit are selected if any of the bit is set to '1' in CusMipiRegMap data column (in hex format >> register:effective bits) => 0x42:0x03
            //example CusMipiRegMap must be in '42:03 43:FF' where 0x43 is LSB reg address and 0xFF (1111 1111) all 8 bits are to be effectively read
            //0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read

            //example after MIPI read => reg 0x43 = data read 0xCB (11001011) &  reg 0x42 = data read 0x2E (00101110)
            //Effective bit decode example => for reg 0x42 since all 8bits will be use , effectiveBitData(0xCB) = 11001011
            //while reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
            //reported data => 10 11001011 => convert to dec = 715

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                tmpOutData = int.Parse(dataHex[i], System.Globalization.NumberStyles.HexNumber);
                dataDec[i] = tmpOutData;
                dataBinary[i] = Convert.ToString(tmpOutData, 2).PadLeft(8, '0');        //Convert DEC to 8 Bit Binary
            }

            //sorted out MIPI data base effective bit selection and publish result
            for (int i = 0; i < biasDataArr.Length; i++)
            {
                //sort out the effective bit - register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                int tempReg_Dec = 0;
                string[] tempReg_Hex = new string[2];
                tempReg_Hex = biasDataArr[i].Split(':');

                try
                {
                    tempReg_Dec = int.Parse(tempReg_Hex[1], System.Globalization.NumberStyles.HexNumber);       //convert Effective BIT for given register address from HEX to Decimal
                    dataSizeHex = dataSizeHex + tempReg_Hex[1];
                }
                catch (Exception)
                {
                    MessageBox.Show("!!! WRONG SELECTIVE BIT FORMAT !!!\n" +
                        "DATA MUST BE IN HEX FORMAT (" + SwBand + " : " + biasDataArr[i] + ")\n" +
                        "PLS CHECK & FIXED IN MIPI WORKSHEET");
                }

                string tempReg_Binary = Convert.ToString(tempReg_Dec, 2).PadLeft(8, '0');                       //Convert DEC to 8 Bit Binary

                //sort out the effective data base of effective bit of a given register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                //example after MIPI read => reg 0x42 = data read 0x2E (00101110)
                //reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
                char[] selectiveBitReg_Binary = new char[8];
                char[] selectiveData_Binary = new char[8];

                //stored in charArray format in Binary form
                selectiveBitReg_Binary = tempReg_Binary.ToCharArray();
                selectiveData_Binary = dataBinary[i].ToCharArray();

                for (int j = 0; j < selectiveBitReg_Binary.Length; j++)
                {
                    if (selectiveBitReg_Binary[j] == '1')
                    {
                        appendBinary = appendBinary + selectiveData_Binary[j];   //construct and concatenations binary data bit by bit
                    }
                }
            }

            decDataOut = Convert.ToInt32(appendBinary, 2);            //Convert Binary to Decimal
        }
        //mipi data return array of Hex - mostly using for 2DID otp program because of multiple register address
        public void readout_OTPReg_viaEffectiveBit(int delay_mSec, string SwBand, string CusMipiRegMap, string CusPMTrigMap, string CusSlaveAddr, string CusMipiPair, string CusMipiSite, out string[] hexDataOut, out string dataSizeHex)
        {
            biasDataArr = null;
            dataSizeHex = null;

            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            //Init variable & sorted effective bit data
            appendBinary = null;
            dataDec = new int[biasDataArr.Length];
            dataBinary = new string[biasDataArr.Length];
            hexDataOut = new string[biasDataArr.Length];

            //Note : effective bit are selected if any of the bit is set to '1' in CusMipiRegMap data column (in hex format >> register:effective bits) => 0x42:0x03
            //example CusMipiRegMap must be in '42:03 43:FF' where 0x43 is LSB reg address and 0xFF (1111 1111) all 8 bits are to be effectively read
            //0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read

            //example after MIPI read => reg 0x43 = data read 0xCB (11001011) &  reg 0x42 = data read 0x2E (00101110)
            //Effective bit decode example => for reg 0x42 since all 8bits will be use , effectiveBitData(0xCB) = 11001011
            //while reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
            //reported data => 10 11001011 => convert to dec = 715

            //Set MIPI and Read MIPI
            //Eq.Site[0]._EqMiPiCtrl.TurnOff_VIO(Convert.ToInt16(CusMipiPair));        //mipi pair - derive from MIPI spereadsheet()
            //DelayMs(delay_mSec);
            Eq.Site[0]._EqMiPiCtrl.TurnOn_VIO(Convert.ToInt16(CusMipiPair));        //mipi pair - derive from MIPI spereadsheet
       //     DelayMs(delay_mSec);

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                Eq.Site[0]._EqMiPiCtrl.ReadMIPICodesCustom(out tmpOutData, biasDataArr[i], CusPMTrigMap, Convert.ToInt16(CusMipiPair), Convert.ToInt32((CusSlaveAddr), 16));
                dataDec[i] = tmpOutData;
                dataBinary[i] = Convert.ToString(tmpOutData, 2).PadLeft(8, '0');        //Convert DEC to 8 Bit Binary
            }

            //sorted out MIPI data base effective bit selection and publish result
            for (
                int i = 0; i < biasDataArr.Length; i++)
            {
                //sort out the effective bit - register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                int tempReg_Dec = 0;
                string[] tempReg_Hex = new string[2];
                tempReg_Hex = biasDataArr[i].Split(':');

                try
                {
                    tempReg_Dec = int.Parse(tempReg_Hex[1], System.Globalization.NumberStyles.HexNumber);       //convert Effective BIT for given register address from HEX to Decimal
                    dataSizeHex = dataSizeHex + tempReg_Hex[1];
                }
                catch (Exception)
                {
                    MessageBox.Show("!!! WRONG SELECTIVE BIT FORMAT !!!\n" +
                        "DATA MUST BE IN HEX FORMAT (" + SwBand + " : " + biasDataArr[i] + ")\n" +
                        "PLS CHECK & FIXED IN MIPI WORKSHEET");
                }

                string tempReg_Binary = Convert.ToString(tempReg_Dec, 2).PadLeft(8, '0');                       //Convert DEC to 8 Bit Binary

                //sort out the effective data base of effective bit of a given register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                //example after MIPI read => reg 0x42 = data read 0x2E (00101110)
                //reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
                char[] selectiveBitReg_Binary = new char[8];
                char[] selectiveData_Binary = new char[8];

                //stored in charArray format in Binary form
                selectiveBitReg_Binary = tempReg_Binary.ToCharArray();
                selectiveData_Binary = dataBinary[i].ToCharArray();

                //reset binary data for every register address
                appendBinary = null;

                for (int j = 0; j < selectiveBitReg_Binary.Length; j++)
                {
                    if (selectiveBitReg_Binary[j] == '1')
                    {
                        appendBinary = appendBinary + selectiveData_Binary[j];   //construct and concatenations binary data bit by bit
                    }
                }

                hexDataOut[i] = Convert.ToInt32(appendBinary, 2).ToString("X2"); //Convert 8 bit base Binary to base Hex 0x00
            }
        }
        public void mask_viaEffectiveBit(string[] dataHex, string SwBand, string CusMipiRegMap, out string[] hexDataOut)
        {
            biasDataArr = null;
            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter

            //Init variable & sorted effective bit data
            dataSizeHex = null;
            appendBinary = null;
            dataDec = new int[biasDataArr.Length];
            dataBinary = new string[biasDataArr.Length];
            hexDataOut = new string[biasDataArr.Length];

            //Note : effective bit are selected if any of the bit is set to '1' in CusMipiRegMap data column (in hex format >> register:effective bits) => 0x42:0x03
            //example CusMipiRegMap must be in '42:03 43:FF' where 0x43 is LSB reg address and 0xFF (1111 1111) all 8 bits are to be effectively read
            //0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read

            //example after MIPI read => reg 0x43 = data read 0xCB (11001011) &  reg 0x42 = data read 0x2E (00101110)
            //Effective bit decode example => for reg 0x42 since all 8bits will be use , effectiveBitData(0xCB) = 11001011
            //while reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
            //reported data => 10 11001011 => convert to dec = 715

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                tmpOutData = int.Parse(dataHex[i], System.Globalization.NumberStyles.HexNumber);
                dataDec[i] = tmpOutData;
                dataBinary[i] = Convert.ToString(tmpOutData, 2).PadLeft(8, '0');        //Convert DEC to 8 Bit Binary
            }

            //sorted out MIPI data base effective bit selection and publish result
            for (int i = 0; i < biasDataArr.Length; i++)
            {
                //sort out the effective bit - register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                int tempReg_Dec = 0;
                string[] tempReg_Hex = new string[2];
                tempReg_Hex = biasDataArr[i].Split(':');

                try
                {
                    tempReg_Dec = int.Parse(tempReg_Hex[1], System.Globalization.NumberStyles.HexNumber);       //convert Effective BIT for given register address from HEX to Decimal
                    dataSizeHex = dataSizeHex + tempReg_Hex[1];
                }
                catch (Exception)
                {
                    MessageBox.Show("!!! WRONG SELECTIVE BIT FORMAT !!!\n" +
                        "DATA MUST BE IN HEX FORMAT (" + SwBand + " : " + biasDataArr[i] + ")\n" +
                        "PLS CHECK & FIXED IN MIPI WORKSHEET");
                }

                string tempReg_Binary = Convert.ToString(tempReg_Dec, 2).PadLeft(8, '0');                       //Convert DEC to 8 Bit Binary

                //sort out the effective data base of effective bit of a given register address
                //Example : 0x42 is MSB reg address and 0x03 (0000 0011) where on bit0 and bit1 are to be effectively read
                //example after MIPI read => reg 0x42 = data read 0x2E (00101110)
                //reg 0x43 only bit0 and bit1 will be taken (*note: shown in bracket) , effectiveBitData(0x2E) = 001011 (10)
                char[] selectiveBitReg_Binary = new char[8];
                char[] selectiveData_Binary = new char[8];

                //stored in charArray format in Binary form
                selectiveBitReg_Binary = tempReg_Binary.ToCharArray();
                selectiveData_Binary = dataBinary[i].ToCharArray();

                //reset binary data for every register address
                appendBinary = null;

                for (int j = 0; j < selectiveBitReg_Binary.Length; j++)
                {
                    if (selectiveBitReg_Binary[j] == '1')
                    {
                        appendBinary = appendBinary + selectiveData_Binary[j];   //construct and concatenations binary data bit by bit
                    }
                }

                hexDataOut[i] = Convert.ToInt32(appendBinary, 2).ToString("X2"); //Convert 8 bit base Binary to base Hex 0x00
            }
        }
        public void AceOTPBurn(string efuseCtlReg_hex, int[] efuseDataByteNum, string data_hex, string cusMipiPair, string cusSlaveAddr, bool invertData = false)
        {
            //EFuse Control Register Definition (0xC0) - efuseCtlReg_hex
            //Bit7[program] Bit6[a3] Bit5[a2] Bit4[a1] Bit3[a0] Bit2[b2] Bit1[b1] Bit0[b0] - burnDataDec
            //a[3:0]:	Address of eleven 8-bit eFuse cells - efuseDataByteNum[x] - From 0xE0 until 0xEB
            //b[2:0]:	Bit address of the 8-bit eFuse cells - data_hex

            int programMode = 1;

            try
            {
                int data_dec = Convert.ToInt32(data_hex, 16);

                if (data_dec > 255)
                {
                    MessageBox.Show("Error: Cannot burn decimal values greater than 255", "BurnOTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // burn the data - Bit by Bit
                for (int bit = 0; bit < 8; bit++)
                {
                    int bitVal = (int)Math.Pow(2, bit);

                    if ((bitVal & data_dec) == (invertData ? 0 : bitVal))
                    {
                        int burnDataDec = (programMode << 7) + (efuseDataByteNum[3] << 6) + (efuseDataByteNum[2] << 5) + (efuseDataByteNum[1] << 4) + (efuseDataByteNum[0] << 3) + bit;

                        // Convert integer as a hex in a string variable
                        string hexValue = burnDataDec.ToString("X");
                        Eq.Site[0]._EqMiPiCtrl.WriteOTPRegister(efuseCtlReg_hex, hexValue, Convert.ToInt16(cusMipiPair), Convert.ToInt32((cusSlaveAddr), 16));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ACE OTP Burn Error");
            }
        }
        public void burn_AceOTPReg_viaEffectiveBit(string testParam, string CusMipiRegMap, string CusMipiPair, string CusSlaveAddr, string[] dataHex)
        {
            #region Decode MIPI Register and Burn OTP
            biasDataArr = null;

            biasDataArr = CusMipiRegMap.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);     //split string with blank space as delimiter
            int[] efuseCtrlAddress = new int[4];
            string[] tempData = new string[2];

            for (int i = 0; i < biasDataArr.Length; i++)
            {
                //Note : EFuse Control Register
                //efuse cell_0 (0xE0, mirror address 0x40)
                //efuse cell_1 (0xE1, mirror address 0x41)
                //efuse cell_2 (0xE2, mirror address 0x21)

                tempData = biasDataArr[i].Split(':');
                switch (tempData[0].ToUpper())
                {
                    case "40":
                    case "E0":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "41":
                    case "E1":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "21":
                    case "E2":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E3":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E4":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E5":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E6":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E7":
                        efuseCtrlAddress[3] = 0;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "E8":
                        efuseCtrlAddress[3] = 1;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "E9":
                        efuseCtrlAddress[3] = 1;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 1;
                        break;
                    case "EA":
                        efuseCtrlAddress[3] = 1;
                        efuseCtrlAddress[2] = 0;
                        efuseCtrlAddress[1] = 1;
                        efuseCtrlAddress[0] = 0;
                        break;
                    case "EB":
                        //efuseCtrlAddress[3] = 1;
                        //efuseCtrlAddress[2] = 0;
                        //efuseCtrlAddress[1] = 1;
                        //efuseCtrlAddress[0] = 1;
                        efuseCtrlAddress[3] = 1;
                        efuseCtrlAddress[2] = 1;
                        efuseCtrlAddress[1] = 0;
                        efuseCtrlAddress[0] = 0;
                        break;
                    default:
                        MessageBox.Show("Test Parameter : " + testParam + "(" + tempData[0].ToUpper() + ") - OTP Address not supported at this moment.", "MyDUT", MessageBoxButtons.OK);
                        break;
                }

                #region Burn OTP Data

                if (BurnOTP)
                {
                    //Burn3x to double confirm the otp programming is done completely
                    for (int cnt = 0; cnt < 3; cnt++)
                    {
                        //AceOTPBurn("C0", efuseCtrlAddress, dataHex[i], CusMipiPair, CusSlaveAddr);
                        AceOTPBurn("F0", efuseCtrlAddress, dataHex[i], CusMipiPair, CusSlaveAddr);
                    }
                }

                #endregion
            }

            #endregion
        }
        public bool WriteandReadMipi(string CusMipiRegMap, string CusPMTrigMap, string CusMipiPair, string CusSlaveAddr)
        {
            Eq.Site[0]._EqMiPiCtrl.WriteMIPICodesCustom(CusMipiRegMap, Convert.ToInt16(CusMipiPair), Convert.ToInt32((CusSlaveAddr), 16));
            Eq.Site[0]._EqMiPiCtrl.ReadMIPICodesCustom(out int[] TmpResult, out bool bPassFail, CusMipiRegMap, CusPMTrigMap, Convert.ToInt16(CusMipiPair), Convert.ToInt32((CusSlaveAddr), 16));

            return bPassFail;
        }
        #endregion
    }
}
