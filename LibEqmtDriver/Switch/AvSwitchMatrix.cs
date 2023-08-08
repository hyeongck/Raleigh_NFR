using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments.DAQmx;

namespace LibEqmtDriver.SCU
{
    public class AvSwitchMatrix:iSwitch 
    {
        private Task digitalWriteTaskP00;
        private Task digitalWriteTaskP01;
        private Task digitalWriteTaskP02;
        private Task digitalWriteTaskP03;
        private Task digitalWriteTaskP04;
        private Task digitalWriteTaskP05;
        private Task digitalWriteTaskP09;

        private DigitalSingleChannelWriter writerP00;
        private DigitalSingleChannelWriter writerP01;
        private DigitalSingleChannelWriter writerP02;
        private DigitalSingleChannelWriter writerP03;
        private DigitalSingleChannelWriter writerP04;
        private DigitalSingleChannelWriter writerP05;
        private DigitalSingleChannelWriter writerP09;


        #region iSwitch Members

        void iSwitch.Initialize()
        {
            try
            {
                Init();
            }
            catch (Exception ex)
            {
                throw new Exception("AvSwitchMatrix: Initialize -> " + ex.Message);
            }
        }

        void iSwitch.SetPath(string val)
        {
            try
            {
                SetPortPath(val);
            }
            catch (Exception ex)
            {
                throw new Exception("AvSwitchMatrix: SetPath -> " + ex.Message);
            }
        }

        void iSwitch.Reset()
        {
            try
            {

            }
            catch (Exception ex)
            {
                throw new Exception("AvSwitchMatrix: Reset -> " + ex.Message);
            }
        }

        #endregion


        private int[] ChannelValue = new int[12];
        private int Init()
        {
            try
            {

                digitalWriteTaskP00 = new Task();
                digitalWriteTaskP01 = new Task();
                digitalWriteTaskP02 = new Task();
                digitalWriteTaskP03 = new Task();
                digitalWriteTaskP04 = new Task();
                digitalWriteTaskP05 = new Task();

                digitalWriteTaskP09 = new Task();

                digitalWriteTaskP00.DOChannels.CreateChannel("DIO/port0", "port0",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP01.DOChannels.CreateChannel("DIO/port1", "port1",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP02.DOChannels.CreateChannel("DIO/port2", "port2",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP03.DOChannels.CreateChannel("DIO/port3", "port3",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP04.DOChannels.CreateChannel("DIO/port4", "port4",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP05.DOChannels.CreateChannel("DIO/port5", "port5",
                                ChannelLineGrouping.OneChannelForAllLines);

                digitalWriteTaskP09.DOChannels.CreateChannel("DIO/port9", "port9",
                                ChannelLineGrouping.OneChannelForAllLines);

                writerP00 = new DigitalSingleChannelWriter(digitalWriteTaskP00.Stream);
                writerP01 = new DigitalSingleChannelWriter(digitalWriteTaskP01.Stream);
                writerP02 = new DigitalSingleChannelWriter(digitalWriteTaskP02.Stream);
                writerP03 = new DigitalSingleChannelWriter(digitalWriteTaskP03.Stream);
                writerP04 = new DigitalSingleChannelWriter(digitalWriteTaskP04.Stream);
                writerP05 = new DigitalSingleChannelWriter(digitalWriteTaskP05.Stream);

                writerP09 = new DigitalSingleChannelWriter(digitalWriteTaskP09.Stream);


                writerP00.WriteSingleSamplePort(true, 0);
                writerP01.WriteSingleSamplePort(true, 0);
                writerP02.WriteSingleSamplePort(true, 0);
                writerP03.WriteSingleSamplePort(true, 0);
                writerP04.WriteSingleSamplePort(true, 0);
                writerP05.WriteSingleSamplePort(true, 0);
                writerP09.WriteSingleSamplePort(true, 0);


                return 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Initialize");
                return -1;
            }
        }
        private int SetPortPath(string PortPath)
        {
            try
            {
                string[] SwitchNoAndStatus = PortPath.Split(' ');

                int NoOfSwitch = SwitchNoAndStatus.Length;
                int[] SwitchNo = new int[NoOfSwitch];
                int[] SwitchStatus = new int[NoOfSwitch];

                string[] tempSwitchNoAndStatus = new string[2];


                // Generate arrays for switch no and switch on/off status
                for (int i = 0; i < NoOfSwitch; i++)
                {
                    tempSwitchNoAndStatus = SwitchNoAndStatus[i].Split(':');
                    SwitchNo[i] = Convert.ToInt32(tempSwitchNoAndStatus[0]);
                    SwitchStatus[i] = Convert.ToInt32(tempSwitchNoAndStatus[1]);
                }


                // Clear the channel write need status
                bool[] ChannelWriteNeeded = new bool[12];
                int PortNo = 0;
                int ChNo = 0;

                for (int i = 0; i < NoOfSwitch; i++)
                {
                    if (SwitchNo[i] == 48)
                        PortNo = 9;
                    else
                    {
                        PortNo = Convert.ToInt32(Math.Truncate(SwitchNo[i] / 8d));
                    }

                    ChNo = SwitchNo[i] - PortNo * 8;
                    int tempChValue = Convert.ToInt32(Math.Pow(2, ChNo));
                    int tempBitAnd = (ChannelValue[PortNo] & tempChValue);

                    if ((tempBitAnd == tempChValue) && (SwitchStatus[i] == 1))
                    {
                        // Do nothing
                    }
                    else if ((tempBitAnd == tempChValue) && (SwitchStatus[i] == 0))
                    {
                        ChannelValue[PortNo] -= tempChValue;
                        ChannelWriteNeeded[PortNo] = true;
                    }
                    else if ((tempBitAnd != tempChValue) && (SwitchStatus[i] == 1))
                    {
                        ChannelValue[PortNo] += tempChValue;
                        ChannelWriteNeeded[PortNo] = true;
                    }
                    else if ((tempBitAnd != tempChValue) && (SwitchStatus[i] == 0))
                    {
                        // Do nothing
                    }
                    else
                    {
                        MessageBox.Show("Error");
                    }
                }


                // Channel Write                
                if (ChannelWriteNeeded[0])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP00.WriteSingleSamplePort(true, ChannelValue[0]);
                if (ChannelWriteNeeded[1])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP01.WriteSingleSamplePort(true, ChannelValue[1]);
                if (ChannelWriteNeeded[2])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP02.WriteSingleSamplePort(true, ChannelValue[2]);
                if (ChannelWriteNeeded[3])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP03.WriteSingleSamplePort(true, ChannelValue[3]);
                if (ChannelWriteNeeded[4])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP04.WriteSingleSamplePort(true, ChannelValue[4]);
                if (ChannelWriteNeeded[5])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP05.WriteSingleSamplePort(true, ChannelValue[5]);
                if (ChannelWriteNeeded[9])
                    // writerP10.WriteSingleSamplePort(true, portValue);
                    writerP09.WriteSingleSamplePort(true, ChannelValue[9]);

                return 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "SetPortPath");
                return -1;
            }

        }

    }

}
