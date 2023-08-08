using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using ClothoLibAlgo;

namespace LibEqmtDriver.SCU
{
    public class SwitchStatusFile : List<SingleSwitchStatus>
    {
        private string statusFileFullPath;
        private XmlSerializer myXmlSerializer;

        public static SwitchStatusFile ReadFromFileOrNew(string statusFileFullPath)
        {
            SwitchStatusFile myProductStatus = null;
            XmlSerializer myXmlSerializer = new XmlSerializer(typeof(SwitchStatusFile));

            try
            {
                if (File.Exists(statusFileFullPath))
                {
                    using (StreamReader sr = new StreamReader(statusFileFullPath))
                    {
                        myProductStatus = (SwitchStatusFile)myXmlSerializer.Deserialize(sr);
                    }
                }
                else
                {
                    myProductStatus = new SwitchStatusFile();
                }

                myProductStatus.statusFileFullPath = statusFileFullPath;
                myProductStatus.myXmlSerializer = myXmlSerializer;

                return myProductStatus;
            }
           catch
            {
                return myProductStatus;
            }
        }

        public SingleSwitchStatus GetSingleSwitchStatus(string switchId)
        {
            SingleSwitchStatus mySingleSwitchStatus = null;

            try
            {
                mySingleSwitchStatus = this.FirstOrDefault(x => x.SwitchId == switchId);

                if (mySingleSwitchStatus == null)
                {
                    mySingleSwitchStatus = new SingleSwitchStatus(switchId);
                    this.Add(mySingleSwitchStatus);
                }

                return mySingleSwitchStatus;
            }
            catch
            {
                return mySingleSwitchStatus;
            }
           
        }

        public void SaveToFile()
        {
            if (!Directory.Exists(Path.GetDirectoryName(statusFileFullPath)))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(statusFileFullPath));
                }
                catch (Exception e)
                {
                    Helper.AutoClosingMessageBox.Show(e.ToString(), "Error");
                    return;
                }

            }

            this.Sort((x, y) => x.SwitchId.CompareTo(y.SwitchId));

            using (StreamWriter sw = new StreamWriter(statusFileFullPath))
            {
                myXmlSerializer.Serialize(sw, this);
            }
        }
    }

    public class SingleSwitchStatus
    {
        [XmlAttribute("ID")]
        public string SwitchId;
        [XmlElement("SPDT1Count")]
        public int SPDT1Count = 0;
        [XmlElement("SPDT1DateInstall")]
        public string SPDT1DateInstall = new DateTime(2020, 1, 1).ToShortDateString();
        [XmlElement("SPDT2Count")]
        public int SPDT2Count = 0;        
        [XmlElement("SPDT2DateInstall")]
        public string SPDT2DateInstall = new DateTime(2020, 1, 1).ToShortDateString();
        [XmlElement("SPDT3Count")]
        public int SPDT3Count = 0;       
        [XmlElement("SPDT3DateInstall")]
        public string SPDT3DateInstall = new DateTime(2020, 1, 1).ToShortDateString();
        [XmlElement("SPDT4Count")]
        public int SPDT4Count = 0;       
        [XmlElement("SPDT4DateInstall")]
        public string SPDT4DateInstall = new DateTime(2020, 1, 1).ToShortDateString();
        [XmlElement("SP6T1Count_1")]
        public int SP6T1Count_1 = 0;
        [XmlElement("SP6T1Count_2")]
        public int SP6T1Count_2 = 0;
        [XmlElement("SP6T1Count_3")]
        public int SP6T1Count_3 = 0;
        [XmlElement("SP6T1Count_4")]
        public int SP6T1Count_4 = 0;
        [XmlElement("SP6T1Count_5")]
        public int SP6T1Count_5 = 0;
        [XmlElement("SP6T1Count_6")]
        public int SP6T1Count_6 = 0;
        [XmlElement("SP6T1DateInstall")]
        public string SP6T1DateInstall = new DateTime(2020, 1, 1).ToShortDateString();
        [XmlElement("SP6T2Count_1")]
        public int SP6T2Count_1 = 0;
        [XmlElement("SP6T2Count_2")]
        public int SP6T2Count_2 = 0;
        [XmlElement("SP6T2Count_3")]
        public int SP6T2Count_3 = 0;
        [XmlElement("SP6T2Count_4")]
        public int SP6T2Count_4 = 0;
        [XmlElement("SP6T2Count_5")]
        public int SP6T2Count_5 = 0;
        [XmlElement("SP6T2Count_6")]
        public int SP6T2Count_6 = 0;
        [XmlElement("SP6T2DateInstall")]
        public string SP6T2DateInstall = new DateTime(2020, 1, 1).ToShortDateString();

        public SingleSwitchStatus()
        {
        }
        public SingleSwitchStatus(string ID)
        {
            SwitchId = ID;
        }
    }
}
