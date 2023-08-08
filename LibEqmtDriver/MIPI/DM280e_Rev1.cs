using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Aemulus.Hardware.DM280e;

namespace LibEqmtDriver.MIPI
{
    public class Aemulus_DM280e : iMiPiCtrl
    {
        //static string DM280_Address = "PXI2::0::INSTR";
        static int DM280_InitOpt = 0xF;
        static string DM280_OptString = "Simulate = 0, DriverSetup=Model:DM280e";

        DM280e AEM_DM280 = new DM280e(Lib_Var.myDM280Address, DM280_InitOpt, DM280_OptString);

        public void INITIALIZATION()
        {
            //Set CLK Freq
            int DM280_CLK = 26000000;
            double DM280_VIO = 1.8;
            Config_MIPI_CLK(DM280_CLK);
            Config_MIPI_Voltage(DM280_VIO);
        }
        public void Configure_Loopback(int _channel, int _loopback)
        {
            AEM_DM280.MIPI_ConfigureLoopback(_channel, _loopback);	
        }
        public void Configure_Delay(int _channel, int _delay)
        {
            AEM_DM280.MIPI_ConfigureDelay(_channel, _delay);
        }
        public void Config_MIPI_CLK(int _freq)
        {
            AEM_DM280.MIPI_ConfigureClock(_freq);
        }
        public void Config_MIPI_Voltage(double _voltage) //added 13 August 2013
        {
            double actual_voltage = 0;
            AEM_DM280.MIPI_ConfigureVoltageSupply(_voltage, out actual_voltage);
        }

        public void MIPI_Reg_Write(int _channel, int _slave_add, int _data_add, int _data)
        {
            int WR_Command = 0;
            int[] Data_frame = new int[16];
            if (_data_add == 0)
                WR_Command = (_slave_add << 8) | (0x1 << 7) | (_data);
            else if (_data_add > 0 && _data_add < 32)
            {
                WR_Command = (_slave_add << 8) | (0x2 << 5) | (_data_add);
                Data_frame[0] = _data;
            }

            AEM_DM280.MIPI_Write(_channel, WR_Command, Data_frame);
        }
        public void MIPI_Reg_Read(int _channel, int _speed, int _slave_add, int _data)
        {

            int RE_Command = (_slave_add << 8) | (0x3 << 5) | (_data);
            int[] Dummy_Data = new int[2];  //Dummy

            AEM_DM280.MIPI_Read(_channel, _speed, RE_Command, Dummy_Data);
        }

        public void MIPI_Retrieve(int _channel, out int _rd_byte_data, out int[] _data, out int[] _parity)
        {
            int temp_rd_byte_data = 0;
            int[] temp_data = new int[256];
            int[] temp_parity = new int[256];

            AEM_DM280.MIPI_Retrieve(_channel, out temp_rd_byte_data, temp_data, temp_parity);

            _rd_byte_data = temp_rd_byte_data;
            _data = temp_data;
            _parity = temp_parity;
        }
        public void MIPI_PM_Trigger_MLEO(int this_channel)
        {

            int this_slave_add = Lib_Var.SlaveAddress;
            int this_data_add = Lib_Var.PMTrig;
            int this_data = Lib_Var.PMTrig_Data;

            MIPI_Reg_Write(this_channel, this_slave_add, this_data_add, this_data);
        }

        public void MIPI_WR_RegX_MLEO(int this_channel, string mipi_reg_cond, int this_reg_no)
        {
            bool reg_en = new bool();
            int str_length = 0;
            int reg_data_integer = 999;

            int reg_slave_add = Lib_Var.SlaveAddress;
            int reg_channel = 999;
            int reg_data_add = this_reg_no;

            str_length = mipi_reg_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg_cond == "X" || mipi_reg_cond == "x")
                        reg_en = false;
                    else
                        throw new Exception("Write Register "  + this_reg_no + " : invalid string to disable " + mipi_reg_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg_int = 999;

                    mipi_reg_int = Convert.ToInt32(mipi_reg_cond, 16);

                    if (mipi_reg_int >= 0 && mipi_reg_int <= 255)
                    {
                        reg_en = true;
                        reg_data_integer = mipi_reg_int;
                    }

                    else
                        throw new Exception("Write Register " + this_reg_no + " : invalid data (not between 00 to FF) " + mipi_reg_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg_cond.Substring(0, 2);
                    string reg_data_str = "";
                    //string compare_reg_data_str = "0" + this_reg_no.ToString(); // variable to double confirm the register is correctly define in TCF
                    string compare_reg_data_str = this_reg_no.ToString("X2");

                    if (reg == compare_reg_data_str)
                    {
                        reg_data_str = mipi_reg_cond.Substring(2);

                        int mipi_reg_int = 999;

                        mipi_reg_int = Convert.ToInt32(reg_data_str, 16);

                        if (mipi_reg_int >= 0 && mipi_reg_int <= 255)
                        {
                            reg_en = true;
                            reg_data_integer = mipi_reg_int;
                        }
                        else
                            throw new Exception("Write Register " + this_reg_no + " : invalid data (not between 00 to FF) " + reg_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register " + this_reg_no + " : invalid data for register " + this_reg_no + mipi_reg_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register " + this_reg_no + " : invalid number of string " + mipi_reg_cond);
                }
            }
            else
                throw new Exception("Write Register " + this_reg_no + " : invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg_en == true)
            {
                MIPI_Reg_Write(reg_channel, reg_slave_add, reg_data_add, reg_data_integer);
            }

            reg_en = new bool();
        }
        public void MIPI_WR_Reg0_MLEO(int this_channel, string mipi_reg0_cond)
        {
            bool reg0_en = new bool();
            int str_length = 0;
            int reg0_data_integer = 999;

            int reg0_slave_add = Lib_Var.SlaveAddress;
            int reg0_channel = 999;
            int reg0_data_add = 0;

            str_length = mipi_reg0_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg0_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg0_cond == "X" || mipi_reg0_cond == "x")
                        reg0_en = false;
                    else
                        throw new Exception("Write Register 0 : invalid string to disable " + mipi_reg0_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg0_int = 999;

                    mipi_reg0_int = Convert.ToInt32(mipi_reg0_cond, 16);

                    if (mipi_reg0_int >= 0 && mipi_reg0_int <= 255)
                    {
                        reg0_en = true;
                        reg0_data_integer = mipi_reg0_int;
                    }

                    else
                        throw new Exception("Write Register 0 : invalid data (not between 00 to FF) " + mipi_reg0_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg0_cond.Substring(0, 2);
                    string reg0_data_str = "";

                    if (reg == "00")
                    {
                        reg0_data_str = mipi_reg0_cond.Substring(2);

                        int mipi_reg0_int = 999;

                        mipi_reg0_int = Convert.ToInt32(reg0_data_str, 16);

                        if (mipi_reg0_int >= 0 && mipi_reg0_int <= 255)
                        {
                            reg0_en = true;
                            reg0_data_integer = mipi_reg0_int;
                        }
                        else
                            throw new Exception("Write Register 0 : invalid data (not between 00 to FF) " + reg0_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 0 : invalid data for register 0" + mipi_reg0_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 0 : invalid number of string " + mipi_reg0_cond);
                }
            }
            else
                throw new Exception("Write Register 0: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg0_en == true)
            {
                MIPI_Reg_Write(reg0_channel, reg0_slave_add, reg0_data_add, reg0_data_integer);
            }

            reg0_en = new bool();
        }
        public void MIPI_WR_Reg1_MLEO(int this_channel, string mipi_reg1_cond)
        {
            bool reg1_en = new bool();
            int str_length = 0;
            int reg1_data_integer = 999;

            int reg1_slave_add = Lib_Var.SlaveAddress;
            int reg1_channel = 999;
            int reg1_data_add = 1;

            str_length = mipi_reg1_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg1_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg1_cond == "X" || mipi_reg1_cond == "x")
                        reg1_en = false;
                    else
                        throw new Exception("Write Register 1 : invalid string to disable " + mipi_reg1_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg1_int = 999;

                    mipi_reg1_int = Convert.ToInt32(mipi_reg1_cond, 16);

                    if (mipi_reg1_int >= 0 && mipi_reg1_int <= 255)
                    {
                        reg1_en = true;
                        reg1_data_integer = mipi_reg1_int;
                    }

                    else
                        throw new Exception("Write Register 1 : invalid data (not between 00 to FF) " + mipi_reg1_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg1_cond.Substring(0, 2);
                    string reg1_data_str = "";

                    if (reg == "01")
                    {
                        reg1_data_str = mipi_reg1_cond.Substring(2);

                        int mipi_reg1_int = 999;

                        mipi_reg1_int = Convert.ToInt32(reg1_data_str, 16);

                        if (mipi_reg1_int >= 0 && mipi_reg1_int <= 255)
                        {
                            reg1_en = true;
                            reg1_data_integer = mipi_reg1_int;
                        }
                        else
                            throw new Exception("Write Register 1 : invalid data (not between 00 to FF) " + reg1_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 1 : invalid data for register 0" + mipi_reg1_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 1 : invalid number of string " + mipi_reg1_cond);
                }
            }
            else
                throw new Exception("Write Register 1: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg1_en == true)
            {
                MIPI_Reg_Write(reg1_channel, reg1_slave_add, reg1_data_add, reg1_data_integer);
            }

            reg1_en = new bool();
        }
        public void MIPI_WR_Reg2_MLEO(int this_channel, string mipi_reg2_cond)
        {
            bool reg2_en = new bool();
            int str_length = 0;
            int reg2_data_integer = 999;

            int reg2_slave_add = Lib_Var.SlaveAddress;
            int reg2_channel = 999;
            int reg2_data_add = 2;      //be carefull - hardcoded reg address

            str_length = mipi_reg2_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg2_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg2_cond == "X" || mipi_reg2_cond == "x")
                        reg2_en = false;
                    else
                        throw new Exception("Write Register 2 : invalid string to disable " + mipi_reg2_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg2_int = 999;

                    mipi_reg2_int = Convert.ToInt32(mipi_reg2_cond, 16);

                    if (mipi_reg2_int >= 0 && mipi_reg2_int <= 255)
                    {
                        reg2_en = true;
                        reg2_data_integer = mipi_reg2_int;
                    }

                    else
                        throw new Exception("Write Register 1 : invalid data (not between 00 to FF) " + mipi_reg2_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg2_cond.Substring(0, 2);
                    string reg2_data_str = "";

                    if (reg == "02")
                    {
                        reg2_data_str = mipi_reg2_cond.Substring(2);

                        int mipi_reg2_int = 999;

                        mipi_reg2_int = Convert.ToInt32(reg2_data_str, 16);

                        if (mipi_reg2_int >= 0 && mipi_reg2_int <= 255)
                        {
                            reg2_en = true;
                            reg2_data_integer = mipi_reg2_int;
                        }
                        else
                            throw new Exception("Write Register 2 : invalid data (not between 00 to FF) " + reg2_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 2 : invalid data for register 0" + mipi_reg2_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 2 : invalid number of string " + mipi_reg2_cond);
                }
            }
            else
                throw new Exception("Write Register 2: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg2_en == true)
            {
                MIPI_Reg_Write(reg2_channel, reg2_slave_add, reg2_data_add, reg2_data_integer);
            }

            reg2_en = new bool();
        }
        public void MIPI_WR_Reg_MLEO(int this_channel, string mipi_reg_cond)
        {
            bool reg_en = new bool();
            int str_length = 0;
            int reg_data_integer = 0;

            int reg_slave_add = Lib_Var.SlaveAddress;
            int reg_channel = 999;
            int reg_data_add = 999;

            str_length = mipi_reg_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg_cond == "X" || mipi_reg_cond == "x")
                        reg_en = false;
                    else
                        throw new Exception("Write Register : invalid string to disable " + mipi_reg_cond);
                }
                else if (str_length == 4)
                {
                    string reg = mipi_reg_cond.Substring(0, 2);
                    string reg_data_str = "";

                    if (reg == "00")
                    {
                        reg_data_str = mipi_reg_cond.Substring(2);

                        int mipi_reg0_int = 999;

                        mipi_reg0_int = Convert.ToInt32(reg_data_str, 16);

                        if (mipi_reg0_int >= 0 && mipi_reg0_int <= 255)
                        {
                            reg_en = true;
                            reg_data_integer = mipi_reg0_int;
                            reg_data_add = 0;
                        }
                        else
                            throw new Exception("Write Register 0 : Register 0 invalid data (not between 00 to FF) " + reg_data_str);
                    }
                    else if (reg == "01")
                    {
                        reg_data_str = mipi_reg_cond.Substring(2);

                        int mipi_reg1_int = 999;

                        mipi_reg1_int = Convert.ToInt32(reg_data_str, 16);

                        if (mipi_reg1_int >= 0 && mipi_reg1_int <= 255)
                        {
                            reg_en = true;
                            reg_data_integer = mipi_reg1_int;
                            reg_data_add = 1;
                        }
                        else
                            throw new Exception("Write Register : Register 1 invalid data (not between 00 to FF) " + reg_data_str);

                    }
                    else
                    {
                        throw new Exception("Write Register : invalid string (only allow 4 charecters '0000' " + mipi_reg_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register : invalid number of string " + mipi_reg_cond);
                }
            }
            else
                throw new Exception("Write Register : invalid channel number " + this_channel);

            if (reg_en == true)
            {
                MIPI_Reg_Write(reg_channel, reg_slave_add, reg_data_add, reg_data_integer);
            }

            reg_en = new bool();
        }

        public void MIPI_Reg_RD_Array(int this_channel, out string[] rd_bk_reg, int this_reg_no)
        {
            int reg_slave_add = Lib_Var.SlaveAddress;
            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];
            rd_bk_reg = new string[this_reg_no];
            int i;

            //Config_MIPI_CLK(23000000);
            AEM_DM280.MIPI_ConfigureDelay(this_channel, 3);

            for (i = 0; i < this_reg_no; i++)
            {
                MIPI_Reg_Read(this_channel, 1, reg_slave_add, i);
            }
            MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);

            for (i = 0; i < this_reg_no; i++)
            {
                //rd_bk_reg[i] = "0" + i.ToString() + retrieve_data[i].ToString("X2");
                rd_bk_reg[i] = i.ToString("X2") + retrieve_data[i].ToString("X2");
            }

        }
        public void MIPI_Reg_RD(int this_channel, out string rd_bk_reg0, out string rd_bk_reg1, out string rd_bk_reg2)
        {
            int reg_slave_add = Lib_Var.SlaveAddress;
            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];

            //Config_MIPI_CLK(23000000);
            AEM_DM280.MIPI_ConfigureDelay(this_channel, 3);
            MIPI_Reg_Read(this_channel, 1, reg_slave_add, 0);
            MIPI_Reg_Read(this_channel, 1, reg_slave_add, 1);
            MIPI_Reg_Read(this_channel, 1, reg_slave_add, 2);
            MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);

            rd_bk_reg0 = "00" + retrieve_data[0].ToString("X2");
            rd_bk_reg1 = "01" + retrieve_data[1].ToString("X2");
            rd_bk_reg2 = "02" + retrieve_data[2].ToString("X2");

        }
        public void MIPI_Reg_RD(int this_channel, out string rd_bk_1C)
        {
            int reg_slave_add = Lib_Var.SlaveAddress;
            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];

            //Config_MIPI_CLK(23000000);
            AEM_DM280.MIPI_ConfigureDelay(this_channel, 3);
            MIPI_Reg_Read(this_channel, 1, reg_slave_add, 0x1C);
            //MIPI_Reg_Read(this_channel, 1, reg_slave_add, 1);
            MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);

            rd_bk_1C = "1C" + retrieve_data[0].ToString("X2");
        }


        public string MIPI_WR_Reg0_MLEO_Complete(int this_channel, string mipi_reg0_cond)
        {
            bool reg0_en = new bool();
            int str_length = 0;
            int reg0_data_integer = 999;

            int reg0_slave_add = Lib_Var.SlaveAddress;
            int reg0_channel = 999;
            int reg0_data_add = 0;

            int pm_trig_data_add = Lib_Var.PMTrig;
            int pm_trig_data = Lib_Var.PMTrig_Data;

            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];
            string ret = "";

            str_length = mipi_reg0_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg0_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg0_cond == "X" || mipi_reg0_cond == "x")
                    {
                        reg0_en = false;
                        ret = mipi_reg0_cond;
                    }
                    else
                        throw new Exception("Write Register 0 : invalid string to disable " + mipi_reg0_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg0_int = 999;

                    mipi_reg0_int = Convert.ToInt32(mipi_reg0_cond, 16);

                    if (mipi_reg0_int >= 0 && mipi_reg0_int <= 255)
                    {
                        reg0_en = true;
                        reg0_data_integer = mipi_reg0_int;
                    }

                    else
                        throw new Exception("Write Register 0 : invalid data (not between 00 to FF) " + mipi_reg0_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg0_cond.Substring(0, 2);
                    string reg0_data_str = "";

                    if (reg == "00")
                    {
                        reg0_data_str = mipi_reg0_cond.Substring(2);

                        int mipi_reg0_int = 999;

                        mipi_reg0_int = Convert.ToInt32(reg0_data_str, 16);

                        if (mipi_reg0_int >= 0 && mipi_reg0_int <= 255)
                        {
                            reg0_en = true;
                            reg0_data_integer = mipi_reg0_int;
                        }
                        else
                            throw new Exception("Write Register 0 : invalid data (not between 00 to FF) " + reg0_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 0 : invalid data for register 0" + mipi_reg0_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 0 : invalid number of string " + mipi_reg0_cond);
                }
            }
            else
                throw new Exception("Write Register 0: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg0_en == true)
            {
                MIPI_Reg_Write(reg0_channel, reg0_slave_add, reg0_data_add, reg0_data_integer);
                Thread.Sleep(1);
                MIPI_Reg_Write(this_channel, reg0_slave_add, pm_trig_data_add, pm_trig_data);

                Config_MIPI_CLK(9000000);
                MIPI_Reg_Read(this_channel, 1, reg0_slave_add, 0);
                MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);
                Config_MIPI_CLK(26000000);

                if (retrieve_data[0] == reg0_data_integer)
                {
                    reg0_en = new bool();
                    ret = retrieve_data[0].ToString("X2");
                }
                else
                {
                    reg0_en = new bool();
                    ret = "999";
                }
            }
            return ret;
        }
        public string MIPI_WR_Reg1_MLEO_Complete(int this_channel, string mipi_reg1_cond)
        {
            bool reg1_en = new bool();
            int str_length = 0;
            int reg1_data_integer = 999;

            int reg1_slave_add = Lib_Var.SlaveAddress;
            int reg1_channel = 999;
            int reg1_data_add = 1;

            int pm_trig_data_add = Lib_Var.PMTrig;
            int pm_trig_data = Lib_Var.PMTrig_Data;

            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];
            string ret = "";

            str_length = mipi_reg1_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg1_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg1_cond == "X" || mipi_reg1_cond == "x")
                    {
                        reg1_en = false;
                        ret = mipi_reg1_cond;
                    }
                    else
                        throw new Exception("Write Register 1 : invalid string to disable " + mipi_reg1_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg1_int = 999;

                    mipi_reg1_int = Convert.ToInt32(mipi_reg1_cond, 16);

                    if (mipi_reg1_int >= 0 && mipi_reg1_int <= 255)
                    {
                        reg1_en = true;
                        reg1_data_integer = mipi_reg1_int;
                    }

                    else
                        throw new Exception("Write Register 1 : invalid data (not between 00 to FF) " + mipi_reg1_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg1_cond.Substring(0, 2);
                    string reg1_data_str = "";

                    if (reg == "01")
                    {
                        reg1_data_str = mipi_reg1_cond.Substring(2);

                        int mipi_reg1_int = 999;

                        mipi_reg1_int = Convert.ToInt32(reg1_data_str, 16);

                        if (mipi_reg1_int >= 0 && mipi_reg1_int <= 255)
                        {
                            reg1_en = true;
                            reg1_data_integer = mipi_reg1_int;
                        }
                        else
                            throw new Exception("Write Register 1 : invalid data (not between 00 to FF) " + reg1_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 1 : invalid data for register 0" + mipi_reg1_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 1 : invalid number of string " + mipi_reg1_cond);
                }
            }
            else
                throw new Exception("Write Register 1: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg1_en == true)
            {
                MIPI_Reg_Write(reg1_channel, reg1_slave_add, reg1_data_add, reg1_data_integer);
                Thread.Sleep(1);
                MIPI_Reg_Write(this_channel, reg1_slave_add, pm_trig_data_add, pm_trig_data);

                Config_MIPI_CLK(9000000);
                MIPI_Reg_Read(this_channel, 1, reg1_slave_add, 1);
                MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);
                Config_MIPI_CLK(26000000);


                if (retrieve_data[0] == reg1_data_integer)
                {
                    reg1_en = new bool();
                    ret = retrieve_data[0].ToString("X2");
                }
                else
                {
                    reg1_en = new bool();
                    ret = "999";
                }
            }
            return ret;
        }
        public string MIPI_WR_Reg2_MLEO_Complete(int this_channel, string mipi_reg2_cond)
        {
            bool reg2_en = new bool();
            int str_length = 0;
            int reg2_data_integer = 999;

            int reg2_slave_add = Lib_Var.SlaveAddress;
            int reg2_channel = 999;
            int reg2_data_add = 1;

            int pm_trig_data_add = Lib_Var.PMTrig;
            int pm_trig_data = Lib_Var.PMTrig_Data;

            int this_rd_byte_data_count = 0;
            int[] retrieve_data = new int[256];
            int[] retrieve_parity = new int[256];
            string ret = "";

            str_length = mipi_reg2_cond.Length;

            if (this_channel == 0 || this_channel == 1)
            {
                reg2_channel = this_channel;

                if (str_length == 1)
                {
                    if (mipi_reg2_cond == "X" || mipi_reg2_cond == "x")
                    {
                        reg2_en = false;
                        ret = mipi_reg2_cond;
                    }
                    else
                        throw new Exception("Write Register 2 : invalid string to disable " + mipi_reg2_cond);
                }
                else if (str_length == 2)
                {
                    int mipi_reg2_int = 999;

                    mipi_reg2_int = Convert.ToInt32(mipi_reg2_cond, 16);

                    if (mipi_reg2_int >= 0 && mipi_reg2_int <= 255)
                    {
                        reg2_en = true;
                        reg2_data_integer = mipi_reg2_int;
                    }

                    else
                        throw new Exception("Write Register 2 : invalid data (not between 00 to FF) " + mipi_reg2_cond);
                }

                else if (str_length == 4)
                {
                    string reg = mipi_reg2_cond.Substring(0, 2);
                    string reg2_data_str = "";

                    if (reg == "01")
                    {
                        reg2_data_str = mipi_reg2_cond.Substring(2);

                        int mipi_reg2_int = 999;

                        mipi_reg2_int = Convert.ToInt32(reg2_data_str, 16);

                        if (mipi_reg2_int >= 0 && mipi_reg2_int <= 255)
                        {
                            reg2_en = true;
                            reg2_data_integer = mipi_reg2_int;
                        }
                        else
                            throw new Exception("Write Register 2 : invalid data (not between 00 to FF) " + reg2_data_str);
                    }
                    else
                    {
                        throw new Exception("Write Register 2 : invalid data for register 0" + mipi_reg2_cond);
                    }
                }
                else
                {
                    throw new Exception("Write Register 2 : invalid number of string " + mipi_reg2_cond);
                }
            }
            else
                throw new Exception("Write Register 2: invalid channel number (DM280 Channel only 0 or 1) " + this_channel);

            if (reg2_en == true)
            {
                MIPI_Reg_Write(reg2_channel, reg2_slave_add, reg2_data_add, reg2_data_integer);
                Thread.Sleep(1);
                MIPI_Reg_Write(this_channel, reg2_slave_add, pm_trig_data_add, pm_trig_data);

                Config_MIPI_CLK(9000000);
                MIPI_Reg_Read(this_channel, 1, reg2_slave_add, 1);
                MIPI_Retrieve(this_channel, out this_rd_byte_data_count, out retrieve_data, out retrieve_parity);
                Config_MIPI_CLK(26000000);


                if (retrieve_data[0] == reg2_data_integer)
                {
                    reg2_en = new bool();
                    ret = retrieve_data[0].ToString("X2");
                }
                else
                {
                    reg2_en = new bool();
                    ret = "999";
                }
            }
            return ret;
        }

        #region iMipiCtrl interface
        void iMiPiCtrl.Init(s_MIPI_PAIR[] mipiPairCfg)
        {
            INITIALIZATION();
        }
        void iMiPiCtrl.Init_ID(s_MIPI_PAIR[] mipiPairCfg)
        {
            //INITIALIZATION();
        }
        void iMiPiCtrl.TurnOn_VIO(int pair)
        {
            //not implemented
        }
        void iMiPiCtrl.TurnOff_VIO(int pair)
        {
            //not implemented
        }
        void iMiPiCtrl.SendAndReadMIPICodes(out bool ReadSuccessful, int Mipi_Reg)
        {
            int[] MIPI_arr = new int[Mipi_Reg];
            ReadSuccessful = true;
            bool[] T_ReadSuccessful = new bool[Mipi_Reg];
            string[] regX_value = new string[Mipi_Reg];
            string[] MIPI_RegCond = new string[Mipi_Reg];
            int i;
            int reg_Cnt;
            int PassRd, FailRd;

            //Initialize variable
            i = 0; reg_Cnt = 0;
            for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
            {
                T_ReadSuccessful[reg_Cnt] = true;
                regX_value[reg_Cnt] = "";
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

            for (i = 0; i < 10; i++)
            {
                reg_Cnt = 0; PassRd = 0; FailRd = 0; //reset read success counter

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    MIPI_WR_RegX_MLEO(LibEqmtDriver.MIPI.Lib_Var.ChannelUsed, MIPI_RegCond[reg_Cnt], reg_Cnt);
                    MIPI_PM_Trigger_MLEO(LibEqmtDriver.MIPI.Lib_Var.ChannelUsed);
                }

                MIPI_Reg_RD_Array(LibEqmtDriver.MIPI.Lib_Var.ChannelUsed, out regX_value, Mipi_Reg);

                for (reg_Cnt = 0; reg_Cnt < Mipi_Reg; reg_Cnt++)
                {
                    if (MIPI_RegCond[reg_Cnt].Length > 1)
                    {
                        if (MIPI_RegCond[reg_Cnt].Length == 2)
                        {
                            string reg_data_str = regX_value[reg_Cnt].Substring(2, 2);
                            if (MIPI_RegCond[reg_Cnt] != reg_data_str && LibEqmtDriver.MIPI.Lib_Var.ReadFunction == true)
                                T_ReadSuccessful[reg_Cnt] = false;
                            else
                                T_ReadSuccessful[reg_Cnt] = true;
                        }
                        if (MIPI_RegCond[reg_Cnt].Length == 4)
                        {
                            if (MIPI_RegCond[reg_Cnt] != regX_value[reg_Cnt] && LibEqmtDriver.MIPI.Lib_Var.ReadFunction == true)
                                T_ReadSuccessful[reg_Cnt] = false;
                            else
                                T_ReadSuccessful[reg_Cnt] = true;
                        }
                    }
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
                    ReadSuccessful = true;
                    break;
                }
                else
                    ReadSuccessful = false;
            }
        }
        void iMiPiCtrl.SendAndReadMIPICodesRev2(out bool ReadSuccessful, int Mipi_Reg, int MipiPairNo, int SlaveAddr)
        {
            throw new NotImplementedException();
        }
        void iMiPiCtrl.SendAndReadMIPICodesCustom(out bool ReadSuccessful, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            throw new NotImplementedException();
        }
        void iMiPiCtrl.ReadMIPICodesCustom(out int Result, string MipiRegMap, string TrigRegMap, int pair, int slvaddr)
        {
            throw new NotImplementedException();
        }
        void iMiPiCtrl.WriteOTPRegister(string efuseCtlReg_hex, string data_hex, int pair, int slvaddr, bool invertData = false)
        {
            throw new NotImplementedException();
        }
        void iMiPiCtrl.WriteMIPICodesCustom(string MipiRegMap, int pair, int slvaddr)
        {
            throw new NotImplementedException();
        }
        //void iMiPiCtrl.SetMeasureMIPIcurrent(int delayMs, int pair, int slvaddr, s_MIPI_DCSet[] setDC_Mipi, string[] measDC_MipiCh, out s_MIPI_DCMeas[] measDC_Mipi)
        //{
        //    //Not Implemented
        //    s_MIPI_DCMeas[] tmpMeasDC_Mipi = new s_MIPI_DCMeas[3];
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
            //Read_Temp(out tempC);
        }
        void iMiPiCtrl.ReadLoadboardsocketID(out string loadboardID, out string socketID)
        {
            loadboardID = "NaN";
            socketID = "NaN";
        }
        void iMiPiCtrl.BurstMIPIforNFR(int pair) // Ben, add for NF MIPI RISE
        {

        }
        void iMiPiCtrl.AbortBurst() // Ben, add for NF MIPI RISE
        {

        }

        public void ReadMIPICodesCustom(out int[] ReadResult, out bool bPass, string MipiRegMap, string PMTRigMap, int MipiPairNo, int SlaveAddr)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
