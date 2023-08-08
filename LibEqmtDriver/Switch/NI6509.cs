using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments.DAQmx;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ClothoLibAlgo;

namespace LibEqmtDriver.SCU
{
    public class NI6509 : Base_Switch, iSwitch
    {
        //private static bool IscounterEnable = true;
        private static ArrayList TaskList;

        private static Task[] digitalWriteTaskP00 = new Task[2];
        private static Task[] digitalWriteTaskP01 = new Task[2];
        private static Task[] digitalWriteTaskP02 = new Task[2];
        private static Task[] digitalWriteTaskP03 = new Task[2];
        private static Task[] digitalWriteTaskP04 = new Task[2];
        private static Task[] digitalWriteTaskP05 = new Task[2];

        private static Task[] digitalWriteTaskP09 = new Task[2];
        private static Task[] digitalWriteTaskP10 = new Task[2];
        private static Task[] digitalWriteTaskP11 = new Task[2];

        private static DigitalSingleChannelWriter[] writerP00 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP01 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP02 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP03 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP04 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP05 = new DigitalSingleChannelWriter[2];

        private static DigitalSingleChannelWriter[] writerP09 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP10 = new DigitalSingleChannelWriter[2];
        private static DigitalSingleChannelWriter[] writerP11 = new DigitalSingleChannelWriter[2];

        private static uint SPDT1prevValue = 0;
        private static uint SPDT2prevValue = 0;
        private static uint SPDT3prevValue = 0;
        private static uint SPDT4prevValue = 0;
        private static uint SP6T1prevValue = 0;
        private static uint SP6T2prevValue = 0;

        private static SwitchStatusFile RemoteStatusfile;
        private static SingleSwitchStatus RemoteSwitchStatus;
        private string statusFileDir = @"D:\ExpertCalSystem.Data\MagicBox\MechanicalSw.xml";
        private static SwitchStatusFile LocalStatusfile;
        private static SingleSwitchStatus LocalSwitchStatus;
        private string LocalStatusFileDir = @"C:\Avago.ATF.Common.x64\Database\MechanicalSw.xml";
        public string IOAddress;
        public int swNum;
        public string strCurrentSWConfig = "";
        public override string ModelNumber { get => "NI6509"; }


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

        public void UpdateRemoteXmlFile()
        {
            //Update remote Status file, to cater the event where software crashes
            //before the remote status file is updated at the end of the lot
            if (LocalSwitchStatus.SPDT1Count > RemoteSwitchStatus.SPDT1Count)
            { RemoteSwitchStatus.SPDT1Count = LocalSwitchStatus.SPDT1Count; }
            if (LocalSwitchStatus.SPDT2Count > RemoteSwitchStatus.SPDT2Count)
            { RemoteSwitchStatus.SPDT2Count = LocalSwitchStatus.SPDT2Count; }
            if (LocalSwitchStatus.SPDT3Count > RemoteSwitchStatus.SPDT3Count)
            { RemoteSwitchStatus.SPDT3Count = LocalSwitchStatus.SPDT3Count; }
            if (LocalSwitchStatus.SPDT4Count > RemoteSwitchStatus.SPDT4Count)
            { RemoteSwitchStatus.SPDT4Count = LocalSwitchStatus.SPDT4Count; }
            if (LocalSwitchStatus.SP6T1Count_1 > RemoteSwitchStatus.SP6T1Count_1)
            { RemoteSwitchStatus.SP6T1Count_1 = LocalSwitchStatus.SP6T1Count_1; }
            if (LocalSwitchStatus.SP6T1Count_2 > RemoteSwitchStatus.SP6T1Count_2)
            { RemoteSwitchStatus.SP6T1Count_2 = LocalSwitchStatus.SP6T1Count_2; }
            if (LocalSwitchStatus.SP6T1Count_3 > RemoteSwitchStatus.SP6T1Count_3)
            { RemoteSwitchStatus.SP6T1Count_3 = LocalSwitchStatus.SP6T1Count_3; }
            if (LocalSwitchStatus.SP6T1Count_4 > RemoteSwitchStatus.SP6T1Count_4)
            { RemoteSwitchStatus.SP6T1Count_4 = LocalSwitchStatus.SP6T1Count_4; }
            if (LocalSwitchStatus.SP6T1Count_5 > RemoteSwitchStatus.SP6T1Count_5)
            { RemoteSwitchStatus.SP6T1Count_5 = LocalSwitchStatus.SP6T1Count_5; }
            if (LocalSwitchStatus.SP6T1Count_6 > RemoteSwitchStatus.SP6T1Count_6)
            { RemoteSwitchStatus.SP6T1Count_6 = LocalSwitchStatus.SP6T1Count_6; }
            if (LocalSwitchStatus.SP6T2Count_1 > RemoteSwitchStatus.SP6T2Count_1)
            { RemoteSwitchStatus.SP6T2Count_1 = LocalSwitchStatus.SP6T2Count_1; }
            if (LocalSwitchStatus.SP6T2Count_2 > RemoteSwitchStatus.SP6T2Count_2)
            { RemoteSwitchStatus.SP6T2Count_2 = LocalSwitchStatus.SP6T2Count_2; }
            if (LocalSwitchStatus.SP6T2Count_3 > RemoteSwitchStatus.SP6T2Count_3)
            { RemoteSwitchStatus.SP6T2Count_3 = LocalSwitchStatus.SP6T2Count_3; }
            if (LocalSwitchStatus.SP6T2Count_4 > RemoteSwitchStatus.SP6T2Count_4)
            { RemoteSwitchStatus.SP6T2Count_4 = LocalSwitchStatus.SP6T2Count_4; }
            if (LocalSwitchStatus.SP6T2Count_5 > RemoteSwitchStatus.SP6T2Count_5)
            { RemoteSwitchStatus.SP6T2Count_5 = LocalSwitchStatus.SP6T2Count_5; }
            if (LocalSwitchStatus.SP6T2Count_6 > RemoteSwitchStatus.SP6T2Count_6)
            { RemoteSwitchStatus.SP6T2Count_6 = LocalSwitchStatus.SP6T2Count_6; }
        }

        //Constructor
        public NI6509(string ioAddress, int i = 0)
        {
            Address = ioAddress;
            swNum = i;

            Initialize();
        }
        NI6509() { }


        #region iSwitch Interface

        public override void Close()
        {

        }

        public override void Initialize()
        {
            int i = swNum;

            try
            {
                digitalWriteTaskP00[i] = new Task();
                digitalWriteTaskP01[i] = new Task();
                digitalWriteTaskP02[i] = new Task();
                digitalWriteTaskP03[i] = new Task();
                digitalWriteTaskP04[i] = new Task();
                digitalWriteTaskP05[i] = new Task();

                digitalWriteTaskP09[i] = new Task();
                digitalWriteTaskP10[i] = new Task();
                digitalWriteTaskP11[i] = new Task();

                digitalWriteTaskP00[i].DOChannels.CreateChannel(IOAddress + "/port0", "port0",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP01[i].DOChannels.CreateChannel(IOAddress + "/port1", "port1",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP02[i].DOChannels.CreateChannel(IOAddress + "/port2", "port2",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP03[i].DOChannels.CreateChannel(IOAddress + "/port3", "port3",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP04[i].DOChannels.CreateChannel(IOAddress + "/port4", "port4",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP05[i].DOChannels.CreateChannel(IOAddress + "/port5", "port5",
                                ChannelLineGrouping.OneChannelForAllLines);

                digitalWriteTaskP09[i].DOChannels.CreateChannel(IOAddress + "/port9", "port9",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP10[i].DOChannels.CreateChannel(IOAddress + "/port10", "port10",
                                ChannelLineGrouping.OneChannelForAllLines);
                digitalWriteTaskP11[i].DOChannels.CreateChannel(IOAddress + "/port11", "port11",
                                ChannelLineGrouping.OneChannelForAllLines);

                writerP00[i] = new DigitalSingleChannelWriter(digitalWriteTaskP00[i].Stream);
                writerP01[i] = new DigitalSingleChannelWriter(digitalWriteTaskP01[i].Stream);
                writerP02[i] = new DigitalSingleChannelWriter(digitalWriteTaskP02[i].Stream);
                writerP03[i] = new DigitalSingleChannelWriter(digitalWriteTaskP03[i].Stream);
                writerP04[i] = new DigitalSingleChannelWriter(digitalWriteTaskP04[i].Stream);
                writerP05[i] = new DigitalSingleChannelWriter(digitalWriteTaskP05[i].Stream);

                writerP09[i] = new DigitalSingleChannelWriter(digitalWriteTaskP09[i].Stream);
                writerP10[i] = new DigitalSingleChannelWriter(digitalWriteTaskP10[i].Stream);
                writerP11[i] = new DigitalSingleChannelWriter(digitalWriteTaskP11[i].Stream);

                writerP00[i].WriteSingleSamplePort(true, 0);
                writerP01[i].WriteSingleSamplePort(true, 0);
                writerP02[i].WriteSingleSamplePort(true, 0);
                writerP03[i].WriteSingleSamplePort(true, 0);
                writerP04[i].WriteSingleSamplePort(true, 0);
                writerP05[i].WriteSingleSamplePort(true, 0);
                writerP09[i].WriteSingleSamplePort(true, 0);
                writerP10[i].WriteSingleSamplePort(true, 0);
                writerP11[i].WriteSingleSamplePort(true, 0);

                RemoteStatusfile = SwitchStatusFile.ReadFromFileOrNew(statusFileDir);
                LocalStatusfile = SwitchStatusFile.ReadFromFileOrNew(LocalStatusFileDir);
                //SetSerialNumber("12345");
                string switchId = GetSerialNumber();

                if (RemoteStatusfile != null) RemoteSwitchStatus = RemoteStatusfile.GetSingleSwitchStatus(switchId);
                else
                {
                    RemoteSwitchStatus = new SingleSwitchStatus();
                    RemoteStatusfile = new SwitchStatusFile();
                }

                if (LocalStatusfile != null) LocalSwitchStatus = LocalStatusfile.GetSingleSwitchStatus(switchId);
                else
                {
                    LocalSwitchStatus = new SingleSwitchStatus();
                    LocalStatusfile = new SwitchStatusFile();
                }

                UpdateRemoteXmlFile();

                RemoteStatusfile.SaveToFile();

            }
            catch (Exception e)
            {
                Helper.AutoClosingMessageBox.Show(e.ToString(), "Initialize");
            }
        }

        public override void SetPath(object state)
        {
            string val = (string)state;
            SetPath(val);
        }

        public override void SetPath(string val)
        {
            int j = swNum; // switch number (eg. 0 = sw#1, 1 = sw#2)
            var currentdata = CompareSWPath(val.Split(';')); //Compare the switch configuration between before and after
            List<System.Threading.Tasks.Task> swTasks = new List<System.Threading.Tasks.Task>();

            try
            {
                foreach (var item in currentdata)
                {
                    if (item.Key)
                    {
                        swTasks.Add(System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            switch (item.Value.Key.ToUpper())
                            {
                                case "P0":
                                    writerP00[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                case "P1":
                                    writerP01[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                case "P2":
                                    writerP02[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                case "P3":
                                    writerP03[j].WriteSingleSamplePort(true, item.Value.Value);
                                    SP6T1SwitchCount((uint)item.Value.Value);
                                    break;
                                case "P4":
                                    writerP04[j].WriteSingleSamplePort(true, item.Value.Value);
                                    SP6T2SwitchCount((uint)item.Value.Value);
                                    break;
                                case "P5":
                                    writerP05[j].WriteSingleSamplePort(true, item.Value.Value);
                                    SPDTSwitchCount((uint)item.Value.Value);
                                    break;
                                case "P9":
                                    writerP09[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                case "P10":
                                    writerP10[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                case "P11":
                                    writerP11[j].WriteSingleSamplePort(true, item.Value.Value);
                                    break;
                                default:
                                    MessageBox.Show("Port No : " + item.Value.Value, "Only P0,P1,P2,P3,P4,P5 AND P9,P10,P11 ALLOWED !!!!\n" + "Pls check your switching configuration in Input Folder");
                                    break;
                            }
                        }));
                    }
                }

                swTasks.ForEach(t => t.Wait());
            }
            catch (Exception ex)
            {
                throw new Exception("NI6509 DIO Error : SetPath -> " + ex.Message);
            }

            #region old verstion
            //try
            //{
            //    for (int i = 0; i < tempdata.Length; i++)
            //    {
            //        tempdata2 = tempdata[i].Split('_');

            //        switch (tempdata2[0].ToUpper())
            //        {
            //            case "P0":
            //                System.Threading.Tasks.Task.Factory.StartNew(() => {
            //                    writerP00[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                });
            //                break;
            //            case "P1":
            //                writerP01[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                break;
            //            case "P2":
            //                writerP02[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                break;
            //            case "P3":
            //                {
            //                    writerP03[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                    SP6T1SwitchCount(Convert.ToUInt32(tempdata2[1]));
            //                }
            //                break;
            //            case "P4":
            //                {
            //                    writerP04[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                    SP6T2SwitchCount(Convert.ToUInt32(tempdata2[1]));
            //                }
            //                break;
            //            case "P5":
            //                {
            //                    writerP05[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                    SPDTSwitchCount(Convert.ToUInt32(tempdata2[1]));
            //                }
            //                break;
            //            case "P9":
            //                writerP09[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                break;
            //            case "P10":
            //                writerP10[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                break;
            //            case "P11":
            //                writerP11[j].WriteSingleSamplePort(true, Convert.ToUInt32(tempdata2[1]));
            //                break;
            //            default:
            //                MessageBox.Show("Port No : " + tempdata2[1].ToUpper(), "Only P0,P1,P2,P3,P4,P5 AND P9,P10,P11 ALLOWED !!!!\n" + "Pls check your switching configuration in Input Folder");
            //                break;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("NI6509 DIO : SetPath -> " + ex.Message);
            //}
            #endregion

        }

        public override int SPDT1CountValue()
        {
            return RemoteSwitchStatus.SPDT1Count;
        }

        public override int SPDT2CountValue()
        {
            return RemoteSwitchStatus.SPDT2Count;
        }

        public override int SPDT3CountValue()
        {
            return RemoteSwitchStatus.SPDT3Count;
        }

        public override int SPDT4CountValue()
        {
            return RemoteSwitchStatus.SPDT4Count;
        }

        public override int SP6T1_1CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_1;
        }

        public override int SP6T1_2CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_2;
        }

        public override int SP6T1_3CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_3;
        }

        public override int SP6T1_4CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_4;
        }

        public override int SP6T1_5CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_5;
        }

        public override int SP6T1_6CountValue()
        {
            return RemoteSwitchStatus.SP6T1Count_6;
        }

        public override int SP6T2_1CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_1;
        }

        public override int SP6T2_2CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_2;
        }

        public override int SP6T2_3CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_3;
        }

        public override int SP6T2_4CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_4;
        }

        public override int SP6T2_5CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_5;
        }

        public override int SP6T2_6CountValue()
        {
            return RemoteSwitchStatus.SP6T2Count_6;
        }

        public override void SaveRemoteMechSwStatusFile()
        {
            RemoteStatusfile.SaveToFile();
        }

        public override void SaveLocalMechSwStatusFile()
        {
            LocalSwitchStatus.SPDT1Count = RemoteSwitchStatus.SPDT1Count;
            LocalSwitchStatus.SPDT2Count = RemoteSwitchStatus.SPDT2Count;
            LocalSwitchStatus.SPDT3Count = RemoteSwitchStatus.SPDT3Count;
            LocalSwitchStatus.SPDT4Count = RemoteSwitchStatus.SPDT4Count;
            LocalSwitchStatus.SP6T1Count_1 = RemoteSwitchStatus.SP6T1Count_1;
            LocalSwitchStatus.SP6T1Count_2 = RemoteSwitchStatus.SP6T1Count_2;
            LocalSwitchStatus.SP6T1Count_3 = RemoteSwitchStatus.SP6T1Count_3;
            LocalSwitchStatus.SP6T1Count_4 = RemoteSwitchStatus.SP6T1Count_4;
            LocalSwitchStatus.SP6T1Count_5 = RemoteSwitchStatus.SP6T1Count_5;
            LocalSwitchStatus.SP6T1Count_6 = RemoteSwitchStatus.SP6T1Count_6;
            LocalSwitchStatus.SP6T2Count_1 = RemoteSwitchStatus.SP6T2Count_1;
            LocalSwitchStatus.SP6T2Count_2 = RemoteSwitchStatus.SP6T2Count_2;
            LocalSwitchStatus.SP6T2Count_3 = RemoteSwitchStatus.SP6T2Count_3;
            LocalSwitchStatus.SP6T2Count_4 = RemoteSwitchStatus.SP6T2Count_4;
            LocalSwitchStatus.SP6T2Count_5 = RemoteSwitchStatus.SP6T2Count_5;
            LocalSwitchStatus.SP6T2Count_6 = RemoteSwitchStatus.SP6T2Count_6;
            LocalStatusfile.SaveToFile();
        }

        public override string GetInstrumentInfo()
        {
            return "SWMatrix = " + "*" + GetSerialNumber() + "; ";
        }

        public override void Reset()
        {
            throw new NotImplementedException();
        }

        #endregion iSwitch Members

        List<KeyValuePair<bool, KeyValuePair<string, int>>> SwConfigBefore = new List<KeyValuePair<bool, KeyValuePair<string, int>>>();
        public List<KeyValuePair<bool, KeyValuePair<string, int>>> CompareSWPath(string[] inputPath)
        {
            List<KeyValuePair<bool, KeyValuePair<string, int>>> isListComparable = new List<KeyValuePair<bool, KeyValuePair<string, int>>>();
            foreach (var selPath in inputPath)
            {
                var sKey = selPath.Split('_')[0];
                var sVal = int.Parse(selPath.Split('_')[1]);

                if (!SwConfigBefore.Any(s => s.Value.Key == sKey && s.Value.Value == sVal))
                    isListComparable.Add(new KeyValuePair<bool, KeyValuePair<string, int>>(true, new KeyValuePair<string, int>(sKey, sVal)));
                else
                    isListComparable.Add(new KeyValuePair<bool, KeyValuePair<string, int>>(false, new KeyValuePair<string, int>(sKey, sVal)));
            }
            return SwConfigBefore = isListComparable;
        }

        public void SP6T1SwitchCount(uint data)
        {
            // if (!IscounterEnable) return;
            if (SP6T1prevValue != data)
            {
                if ((data & 1) != 0) { RemoteSwitchStatus.SP6T1Count_1++; }
                if ((data & 2) != 0) { RemoteSwitchStatus.SP6T1Count_2++; }
                if ((data & 4) != 0) { RemoteSwitchStatus.SP6T1Count_3++; }
                if ((data & 8) != 0) { RemoteSwitchStatus.SP6T1Count_4++; }
                if ((data & 16) != 0) { RemoteSwitchStatus.SP6T1Count_5++; }
                if ((data & 32) != 0) { RemoteSwitchStatus.SP6T1Count_6++; }
            }
            SP6T1prevValue = data;
        }

        public void SP6T2SwitchCount(uint data)
        {
            // if (!IscounterEnable) return;
            if (SP6T2prevValue != data)
            {
                if ((data & 1) != 0) { RemoteSwitchStatus.SP6T2Count_1++; }
                if ((data & 2) != 0) { RemoteSwitchStatus.SP6T2Count_2++; }
                if ((data & 4) != 0) { RemoteSwitchStatus.SP6T2Count_3++; }
                if ((data & 8) != 0) { RemoteSwitchStatus.SP6T2Count_4++; }
                if ((data & 16) != 0) { RemoteSwitchStatus.SP6T2Count_5++; }
                if ((data & 32) != 0) { RemoteSwitchStatus.SP6T2Count_6++; }
            }
            SP6T2prevValue = data;
        }

        public void SPDTSwitchCount(uint data)
        {
            //SPDT1
            //  if (!IscounterEnable) return;
            uint SPDT1CurValue = data & 1;
            //if (SPDT1CurValue != 0)
            //{
            if (SPDT1prevValue != SPDT1CurValue)
            {
                RemoteSwitchStatus.SPDT1Count++;
            }
            SPDT1prevValue = SPDT1CurValue;
            //}

            //SPDT2
            uint SPDT2CurValue = data & 2;
            //if (SPDT2CurValue != 0)
            //{
            if (SPDT2prevValue != SPDT2CurValue)
            {
                RemoteSwitchStatus.SPDT2Count++;
            }
            SPDT2prevValue = SPDT2CurValue;
            //}

            //SPDT3
            uint SPDT3CurValue = data & 4;
            //if (SPDT3CurValue != 0)
            //{
            if (SPDT3prevValue != SPDT3CurValue)
            {
                RemoteSwitchStatus.SPDT3Count++;
            }
            SPDT3prevValue = SPDT3CurValue;
            //}

            //SPDT4
            uint SPDT4CurValue = data & 8;
            //if (SPDT4CurValue != 0)
            //{
            if (SPDT4prevValue != SPDT4CurValue)
            {
                RemoteSwitchStatus.SPDT4Count++;
            }
            SPDT4prevValue = SPDT4CurValue;
            //}

        }

        public static string GetSerialNumber()
        {
            string strSMsn = "";
            string verboseSerialNumber = "XXXXXXXX";

            try
            {
                bool blnDetectedSM = false;
                string strSMfolder = "SwitchMatrixInfo";
                string strSMfile = "SwitchMatrixSN";
                string strSMfileNamePath = "C:\\Avago.ATF.Common\\DataLog\\" + strSMfolder + "\\" + strSMfile;
                DriveInfo[] mydrives = DriveInfo.GetDrives();

                mydrives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable).ToArray();

                if (mydrives.Length == 0)
                {
                    //IscounterEnable = false;
                    Helper.AutoClosingMessageBox.Show("No USB Device found. MagicBox is not being detected.", "MagicBox");
                }
                else
                {
                    foreach (DriveInfo drive in mydrives)
                    {
                        if (blnDetectedSM) break;

                        #region Method#1 - Detect all drives (incl. C, E (SD card)

                        uint serialNum, serialNumLength, flags;
                        StringBuilder volumename = new StringBuilder(256);
                        StringBuilder fstype = new StringBuilder(256);
                        bool ok = false;

                        foreach (string drives in Environment.GetLogicalDrives())
                        {
                            ok = GetVolumeInformation(drives, volumename, (uint)volumename.Capacity - 1, out serialNum,
                                                   out serialNumLength, out flags, fstype, (uint)fstype.Capacity - 1);
                            if (ok)
                            {
                                //if (drive.Name == drives)
                                if (drives == "D:\\")
                                {
                                    //Check if this is MagicBox, by detecting the MagicBox folder....
                                    if (Directory.Exists(drives + "ExpertCalSystem.Data\\MagicBox"))
                                    {
                                        string encryptedSNfile = drives + @"ExpertCalSystem.Data\MagicBox\SerialNumber.dat";
                                        //MagicBox detected
                                        blnDetectedSM = true;

                                        //Check if the SN is different / same
                                        if (File.Exists(encryptedSNfile))
                                        {
                                            using (StreamReader r = new StreamReader(encryptedSNfile))
                                            {
                                                string line;
                                                int line_index = 0;
                                                while ((line = r.ReadLine()) != null)
                                                {
                                                    switch (line_index)
                                                    {
                                                        case 0:
                                                            verboseSerialNumber = line;
                                                            break;

                                                        case 1:
                                                            strSMsn = Encrypt.DecryptString(line, serialNum.ToString());
                                                            break;
                                                    }
                                                    line_index++;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            ok = false;
                        }

                        #endregion Method#1 - Detect all drives (incl. C, E (SD card)

                        if (verboseSerialNumber != strSMsn)
                        {
                            strSMsn = "INVALID";
                        }
                    }
                }
            }
            catch { }

            return strSMsn;
        }

        public static void SetSerialNumber(string serial_number)
        {
            try
            {
                bool blnDetectedSM = false;
                string strSMfolder = "SwitchMatrixInfo";
                string strSMfile = "SwitchMatrixSN";
                string strSMfileNamePath = "C:\\Avago.ATF.Common\\DataLog\\" + strSMfolder + "\\" + strSMfile;
                DriveInfo[] mydrives = DriveInfo.GetDrives();

                mydrives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable).ToArray();

                if (mydrives.Length == 0)
                {
                    Helper.AutoClosingMessageBox.Show("No USB Device found. MagicBox is not being detected.", "MagicBox");
                }
                else
                {
                    foreach (DriveInfo drive in mydrives)
                    {
                        if (blnDetectedSM) break;

                        #region Method#1 - Detect all drives (incl. C, E (SD card)

                        uint serialNum, serialNumLength, flags;
                        StringBuilder volumename = new StringBuilder(256);
                        StringBuilder fstype = new StringBuilder(256);
                        bool ok = false;

                        foreach (string drives in Environment.GetLogicalDrives())
                        {
                            ok = GetVolumeInformation(drives, volumename, (uint)volumename.Capacity - 1, out serialNum,
                                                   out serialNumLength, out flags, fstype, (uint)fstype.Capacity - 1);
                            if (ok)
                            {
                                //if (drive.Name == drives)
                                if (drives == "D:\\")
                                {
                                    if (Directory.Exists(drives + "ExpertCalSystem.Data\\MagicBox") == false)
                                    {
                                        Directory.CreateDirectory(drives + "ExpertCalSystem.Data\\MagicBox");
                                    }

                                    string encryptedSNfile = drives + @"ExpertCalSystem.Data\MagicBox\SerialNumber.dat";
                                    //MagicBox detected
                                    blnDetectedSM = true;

                                    if (File.Exists(encryptedSNfile))
                                        File.Delete(encryptedSNfile);

                                    Thread.Sleep(10);

                                    using (StreamWriter w = new StreamWriter(encryptedSNfile, false))
                                    {
                                        w.WriteLine(serial_number);
                                        w.WriteLine(Encrypt.EncryptString(serial_number, serialNum.ToString()));
                                        w.Close();
                                    }

                                    File.SetAttributes(encryptedSNfile, FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                                    break;

                                }
                            }
                            ok = false;
                        }

                        #endregion Method#1 - Detect all drives (incl. C, E (SD card)
                    }
                }
            }
            catch { }
        }

        // MagicBox
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetVolumeInformation(string Volume, StringBuilder VolumeName, uint VolumeNameSize, out uint SerialNumber, out uint SerialNumberLength, out uint flags, StringBuilder fs, uint fs_size);

    }

    public static class Encrypt
    {
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string initVector = "smpadmagicbox!@#";
        // This constant is used to determine the keysize of the encryption algorithm
        private const int keysize = 256;
        //Encrypt
        public static string EncryptString(string plainText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }
        //Decrypt
        public static string DecryptString(string cipherText, string passPhrase)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }
    }
}
