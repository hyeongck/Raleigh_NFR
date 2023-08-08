using Avago.ATF.Shares;
using Avago.ATF.StandardLibrary;
using MPAD_TestTimer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ClothoSharedItems
{
    public class ClothoDataObject
    {
        public string ClothoRootDir { get; private set; }
        public string ConfigXmlPath { get; private set; }
        public string TDRRootDir { get; private set; }
        public ATFConfiguration ATFConfiguration { get; private set; }
        public Dictionary<string, string> TCF_Setting { get; set; }
        public Dictionary<string, string> UserTestConfigs { get; private set; } = new Dictionary<string, string>();
        public bool EngineeringModewoProduction { get; set; }
        public bool EngineeringMode { get; set; }
        public eUSERTYPE USERTYPE { get; private set; }
        public Dictionary<string, string> Digital_Definitions_Part_Specific { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, double> DicTestPA2a { get; private set; } = new Dictionary<string, double>();
        public List<KeyValuePair<Regex, double>> CustomScreenRegexFor2DID { get; private set; } = new List<KeyValuePair<Regex, double>>();
        public string ContractManufacturer { get; set; }
        public string ZDbFolder { get; set; }
        public DigitalOption DigitalOption { get; private set; }
        public Dictionary<string, bool> DicWaveformLoadPassFail { get; private set; } = new Dictionary<string, bool>();
        public Dictionary<eLoadItems, ConcurrentBag<string>> DicFailedLoadItems { get; private set; } = new Dictionary<eLoadItems, ConcurrentBag<string>>();
        public bool LOCAL_GUDB_Enable { get; set; }

        public View.MainWindow mwDebugger { get; set; }
        public FormSeoulHelper SeoulHelper { get; set; }
        public PackageHelper PackageHelper { get; private set; } = new PackageHelper();

        public Dictionary<int, string> WaferInformation { get; set; }
        public bool HMUEnable { get; private set; }
        public double SYSTEM_MAX_FREQ { get; private set; }

        public RunOption RunOptions;
        public eTesterType TesterType;

        private string DigitalDefinition_Path = "";
        private bool isDigitalInformationLoaded = false;
        public bool EnableOnlySeoulUser { get; private set; } = false;
        public bool SeoulQATester { get; private set; } = false;
        public bool IsSTA = false;
        public bool RunOptionLocked = false;

        public string Get_TCF_Condition(string key, string initVal = null)
        {
            if (TCF_Setting == null)
                return string.Empty;
            else
            {
                if (TCF_Setting.ContainsKey(key))
                    return TCF_Setting[key];
                else if (initVal == null)
                {
                    MessageBox.Show("Warning: The TCF_Setting for: " + key + " does not exist in TCF_Setting", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return string.Empty;
                }
                else
                    return initVal.ToUpper();
            }
        }

        public string Get_UserSetting_Condition(string key, string initVal = null)
        {
            string _key = key.ToUpper();
            if (UserTestConfigs.ContainsKey(_key))
                return UserTestConfigs[_key].ToUpper();
            else if (initVal == null)
            {
                MessageBox.Show("Warning: The UserSetting for: " + key + " does not exist in UserTestConfigs", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            else
                return initVal.ToUpper();
        }

        public string Get_Digital_Definition(string key, string initVal = null)
        {
            string _key = key.ToUpper();
            if (Digital_Definitions_Part_Specific.ContainsKey(_key))
                return Digital_Definitions_Part_Specific[_key].ToUpper();
            else if (initVal == null)
            {
                MessageBox.Show("Warning: The Register definition for: " + key + " does not exist in OTP_Registers_Part_Specific found in Digital_Definitions_Part_Specific.xml", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            else
                return initVal.ToUpper();
        }

        private void SetContractManufacturer(string zdbFolder)
        {
            ZDbFolder = zdbFolder;

            switch (zdbFolder)
            {
                case @"\\192.168.1.41\zdbrelay\Trace_Data":
                    ContractManufacturer = "Inari P3";
                    break;

                case @"\\192.168.11.7\zDB\ZDBFolder":
                    ContractManufacturer = "Inari P8";
                    break;

                case @"\\172.16.11.14\zDB\ZDBFolder":
                    ContractManufacturer = "Inari P13";
                    break;

                case @"\\10.50.10.35\avago\ZDBFolder":
                    ContractManufacturer = "ASEK";
                    break;

                case @"\\rdkna04.kor.broadcom.net\zdb\ZDBFolder":
                    ContractManufacturer = "Seoul";
                    EngineeringModewoProduction = true;
                    EngineeringMode = true;
                    EnableOnlySeoulUser = true;
                    SeoulQATester = ATFRTE.Instance.TesterID.CIvContains("QA-");
                    SeoulHelper = new FormSeoulHelper();
                    break;

                default:
                    ContractManufacturer = "Others";
                    break;
            }
        }

        public static ClothoDataObject Instance { get; set; }

        public ClothoDataObject()
        {
            HMUEnable = false;

            ClothoRootDir = GetTestPlanPath();
            RunOptions = RunOption.None;
            TesterType = eTesterType.None;
            DigitalOption = new DigitalOption();
            IsSTA = (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA);
            RunOptionLocked = false;
            EngineeringModewoProduction = false;
            EngineeringMode = false;

            string currentUser = ATFRTE.Instance?.CurUserName?.ToString()?.ToUpper() ?? "DEBUG";
            if (Enum.TryParse(currentUser, out eUSERTYPE _USERTYPE)) USERTYPE = _USERTYPE;
            else USERTYPE = eUSERTYPE.DEBUG;

            TDRRootDir = System.IO.Path.Combine("C:\\Avago.ATF.Common\\Input");
            foreach (var item in Enum.GetValues(typeof(eLoadItems)) as eLoadItems[])
            {
                DicFailedLoadItems[item] = new ConcurrentBag<string>();
            }

            Regex avgoPattern = new Regex(@"Avago\.ATF\.\d\.\d\.\d", RegexOptions.IgnoreCase);
            var avgoVersion = avgoPattern.Match(ClothoRootDir);
            if (!avgoVersion.Success) avgoVersion = avgoPattern.Match(System.Environment.CurrentDirectory);
            ConfigXmlPath = System.IO.Path.Combine("C:\\", avgoVersion.Success ? avgoVersion.Value : "Avago.ATF.3.1.4", @"System\Configuration\ATFConfig.xml"); //  @"C:\Avago.ATF.3.1.3\System\Configuration\ATFConfig.xml";
            if (!System.IO.File.Exists(ConfigXmlPath)) LoggingManager.Instance.LogError(string.Format("Check Clotho config!\n{0}", ConfigXmlPath));

            if (File.Exists(ConfigXmlPath))
            {
                XDocument xdoc = XDocument.Load(ConfigXmlPath);
                ATFConfiguration = new ATFConfiguration(xdoc.Root);
                SetContractManufacturer(ATFConfiguration.SystemSection.GetValue("ATFResultRemoteSharePath"));
            }

            if (File.Exists(Path.Combine(ClothoRootDir, "FileNeeded", "Unit_Screen_List.csv")))
            {
                string tempStr;
                List<string> arrModIDList = new List<string>();

                using (var reader = new StreamReader(Path.Combine(ClothoRootDir, "FileNeeded", "Unit_Screen_List.csv")))
                {
                    while ((tempStr = reader.ReadLine()) != null)
                    {
                        arrModIDList.Add(tempStr.Trim());
                    }

                    Dictionary<string, double> myDic = new Dictionary<string, double>();

                    if (arrModIDList.Count > 1)
                    {
                        var header = arrModIDList.First().Split(',');
                        var mfgid_index = Array.FindIndex(header, s => s.CIvEquals("MFG_ID"));
                        var modid_index = Array.FindIndex(header, s => s.CIvEquals("OTP_MODULE_ID"));
                        var _2did_index = Array.FindIndex(header, s => s.CIvEquals("2DID"));
                        var mode_index = Array.FindIndex(header, s => s.CIvEquals("MODE"));

                        try
                        {
                            for (int i = 1; i < arrModIDList.Count; i++)
                            {
                                string[] arrs = arrModIDList[i].Split(',');

                                if (_2did_index > -1 && arrs[_2did_index] != "")
                                {
                                    myDic.Add(arrs[_2did_index], Convert.ToDouble(arrs[mode_index]));
                                }
                                else
                                {
                                    myDic.Add(arrs[mfgid_index] + "," + arrs[modid_index], Convert.ToDouble(arrs[mode_index]));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("Please check the unit id with the same unit id has been added\n{0}", ex.Message));
                        }

                        //myDic = arrModIDList
                        //.Skip(1)
                        //.Select(s => new
                        //{
                        //    mfgid = s.Split(',')[mfgid_index],
                        //    modid = s.Split(',')[modid_index],
                        //    mode = Convert.ToDouble(s.Split(',')[mode_index])
                        //})
                        //.GroupBy(s => (s.mfgid + "," + s.modid))
                        //.Select(grp => grp.First())
                        //.ToDictionary(KeyStr => KeyStr.mfgid + "," + KeyStr.modid, ValDobuble => ValDobuble.mode);
                    }

                    DicTestPA2a = myDic;
                }
            }
            else
            {
                if ((File.Exists(Path.Combine(ClothoRootDir, "FileNeeded"))))
                    using (StreamWriter sw = new StreamWriter(Path.Combine(ClothoRootDir, "FileNeeded", "Example_Unit_Screen_List.csv"), false))
                    {
                        sw.WriteLine(string.Format("{0},{1},{2},{3}", "MFG_ID", "OTP_MODULE_ID", "2DID", "MODE"));
                        sw.WriteLine(string.Format("{0},{1},{2},{3}", "12345", "13401070117", "", "1"));
                        sw.WriteLine(string.Format("{0},{1},{2},{3}", "", "", "'221404229993004404060362", "1"));
                    }
            }

            if (File.Exists(Path.Combine(ClothoRootDir, "FileNeeded", "Custom_Screen_Regex_2DID.csv")))
            {
                using (var reader = new StreamReader(Path.Combine(ClothoRootDir, "FileNeeded", "Custom_Screen_Regex_2DID.csv")))
                {
                    CustomScreenRegexFor2DID = new List<KeyValuePair<Regex, double>>();
                    string tempStr;
                    List<string> arrModIDList = new List<string>();
                    while ((tempStr = reader.ReadLine()) != null)
                    {
                        arrModIDList.Add(tempStr);
                    }

                    if (arrModIDList.Count > 1)
                    {
                        var header = arrModIDList.First().Split(',');
                        var regex_index = Array.FindIndex(header, s => s.CIvEquals("REGEX_2DID"));
                        var bin_index = Array.FindIndex(header, s => s.CIvEquals("BIN"));

                        try
                        {
                            for (int i = 1; i < arrModIDList.Count; i++)
                            {
                                string[] arrs = arrModIDList[i].Split(',');

                                if (regex_index > -1 && arrs[regex_index] != "")
                                {
                                    CustomScreenRegexFor2DID.Add(new KeyValuePair<Regex, double>(new Regex(arrs[regex_index]), Convert.ToDouble(arrs[bin_index])));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Instance.LogError(ex.Message);
                        }
                    }
                }
            }
            else
            {
                if ((File.Exists(Path.Combine(ClothoRootDir, "FileNeeded"))))
                    using (StreamWriter sw = new StreamWriter(Path.Combine(ClothoRootDir, "FileNeeded", "Example_Custom_Screen_Regex_2DID.csv"), false))
                    {
                        sw.WriteLine(string.Format("{0},{1}", "REGEX_2DID", "BIN"));
                        sw.WriteLine(string.Format("{0},{1}", "'221404229993004404060362", "1"));
                        sw.WriteLine(string.Format("{0},{1}", @"\d{12}00360302\d{4}", "1"));
                    }
            }

            InitializeDigitalDefinition();
        }

        public void Initialize()
        {
        }

        public void InitializeDigitalDefinition(string filename = "Digital_Definitions_Part_Specific.xml")
        {
            string TargetPath = Path.Combine(ClothoRootDir, "FileNeeded", filename.Trim());
            if (TargetPath.CIvEndsWith(".xml") && File.Exists(TargetPath))
            {
                if (DigitalDefinition_Path.CIvEquals(TargetPath) == false)
                {
                    XDocument xdoc = XDocument.Load(TargetPath);
                    XElement xmlRoot = xdoc.Root;
                    XElement xelPAConfigs = xmlRoot;

                    if (xmlRoot.Name.LocalName.CIvEquals("Config"))
                    {
                        xelPAConfigs = xmlRoot.Element("PAConfigs");
                    }

                    if (xelPAConfigs != null)
                    {
                        Digital_Definitions_Part_Specific = xelPAConfigs
                                                                .Elements("PAConfig")
                                                                .Select(x => new XmlPAConfigField
                                                                {
                                                                    name = ((string)x.Attribute("name")).ToUpper(),
                                                                    value = (string)x.Attribute("value")
                                                                }).ToDictionaryEx(x => x.name, y => y.value) as Dictionary<string, string>;
                    }

                    if (xmlRoot.Element("TestConfigs") != null)
                    {
                        if (isDigitalInformationLoaded) UserTestConfigs = new Dictionary<string, string>();

                        foreach (var settingconfig in xmlRoot.Element("TestConfigs").Elements())
                        {
                            if (!UserTestConfigs.ContainsKey(settingconfig.Attribute("name").Value.ToUpper()))
                                UserTestConfigs.Add(settingconfig.Attribute("name").Value.ToUpper(), settingconfig.Attribute("value").Value.ToUpper());
                        }
                    }

                    DigitalOption.EnableWrite0 = Get_Digital_Definition("EANBLEWRITE0", "TRUE").CIvEquals("TRUE");
                    DigitalOption.EnableRegWrite = Get_Digital_Definition("EANBLEREGWRITE", "TRUE").CIvEquals("TRUE");

                    List<int> _regFrames = new List<int>();
                    foreach (var _hVal in Get_Digital_Definition("RegWriteFrames", "1C").SplitToArray(','))
                    {
                        if (int.TryParse(_hVal.Trim(), System.Globalization.NumberStyles.HexNumber, null, out int result) && result.IsInRange(1, 0x1f))
                            _regFrames.Add(result);
                    }
                    DigitalOption.RegWriteFrames = _regFrames;

                    isDigitalInformationLoaded = true;
                }
                DigitalDefinition_Path = TargetPath;

                HMUEnable = Get_UserSetting_Condition("HMU_ENABLE", "FALSE").CIvEquals("TRUE");
                DigitalOption.TDRtoTx = Get_Digital_Definition("TDR_TX", "FALSE").CIvEquals("TRUE");
                DigitalOption.TDRtoRx = Get_Digital_Definition("TDR_RX", "TRUE").CIvEquals("TRUE");
                SYSTEM_MAX_FREQ = Convert.ToDouble(Get_UserSetting_Condition("SYSTEM_MAX_FREQ", "6e9"));
                if (HMUEnable == false && SYSTEM_MAX_FREQ > 6e9) SYSTEM_MAX_FREQ = 6e9;
            }
        }

        private static string GetTestPlanPath()
        {
            string basePath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "");

            if (basePath == "")   // Lite Driver mode
            {
                string tcfPath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TCF_FULLPATH, "");

                int pos1 = tcfPath.IndexOf("TestPlans") + "TestPlans".Length + 1;
                int pos2 = tcfPath.IndexOf('\\', pos1);

                basePath = tcfPath.Remove(pos2);
            }

            return basePath + "\\";
        }

        public readonly Dictionary<byte, int[]> EqTriggerArray = new Dictionary<byte, int[]>()
        {
            //public enum TriggerLine
            //{
            //    None = 0,
            //PxiTrig0 = 1,
            //PxiTrig1 = 2,
            //PxiTrig2 = 3,
            //PxiTrig3 = 4,
            //PxiTrig4 = 5,
            //PxiTrig5 = 6,
            //PxiTrig6 = 7,
            //PxiTrig7 = 8,
            //FrontPanel0 = 9,
            //FrontPanel1 = 10,
            //FrontPanel2 = 11,
            //FrontPanel3 == 12
            //}

            {0, new int[4]{1, 2, 3, 4} },
            {1, new int[4]{5, 6, 7, 8} }
        };
    }
}