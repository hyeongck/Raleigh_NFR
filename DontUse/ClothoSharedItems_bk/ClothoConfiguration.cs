using Avago.ATF.Shares;
using Avago.ATF.StandardLibrary;
using MPAD_TestTimer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public Dictionary<string, string> UserTestConfigs { get; private set; }
        public bool EngineeringModewoProduction { get; set; }
        public eUSERTYPE USERTYPE { get; private set; }
        public Dictionary<string, string> Digital_Definitions_Part_Specific { get; private set; }
        public Dictionary<string, double> DicTestPA2a { get; private set; }
        public string ContractManufacturer { get; set; }
        public string ZDbFolder { get; set; }
        public DigitalOption DigitalOption { get; private set; }

        public View.MainWindow mwDebugger { get; set; }
        public FormSeoulHelper SeoulHelper { get; set; }

        public Dictionary<int, string> WaferInformation { get; set; }

        public RunOption RunOptions;

        private string DigitalDefinition_Path = "";
        private bool isDigitalInformationLoaded = false;
        public bool EnableOnlySeoulUser = false;
        public bool IsSTA = false;
        public bool RunOptionLocked = false;

        public string Get_TCF_Condition(string key, string initVal = null)
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

        public string Get_UserSetting_Condition(string key, string initVal = null)
        {
            if (UserTestConfigs.ContainsKey(key))
                return UserTestConfigs[key];
            else if (initVal == null)
            {
                MessageBox.Show("Warning: The UserSetting for: " + key + " does not exist in UserTestConfigs", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            else
                return initVal;
        }

        public string Get_Digital_Definition(string key, string initVal = null)
        {
            if (Digital_Definitions_Part_Specific.ContainsKey(key.ToUpper()))
                return Digital_Definitions_Part_Specific[key.ToUpper()].ToUpper();
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
                    EnableOnlySeoulUser = true;
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
            UserTestConfigs = new Dictionary<string, string>();
            ClothoRootDir = GetTestPlanPath();
            RunOptions = RunOption.None;
            DigitalOption = new DigitalOption();
            IsSTA = (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA);
            RunOptionLocked = false;

            string currentUser = ATFRTE.Instance?.CurUserName?.ToString()?.ToUpper() ?? "DEBUG";
            if (Enum.TryParse(currentUser, out eUSERTYPE _USERTYPE)) USERTYPE = _USERTYPE;
            else USERTYPE = eUSERTYPE.DEBUG;

            TDRRootDir = System.IO.Path.Combine("C:\\Avago.ATF.Common\\Input");

            System.Text.RegularExpressions.Regex avgoPattern = new System.Text.RegularExpressions.Regex(@"Avago\.ATF\.\d\.\d\.\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
                        var mode_index = Array.FindIndex(header, s => s.CIvEquals("MODE"));

                        myDic = arrModIDList
                            .Skip(1)
                            .Select(s => new
                            {
                                mfgid = s.Split(',')[mfgid_index],
                                modid = s.Split(',')[modid_index],
                                mode = Convert.ToDouble(s.Split(',')[mode_index])
                            })
                            .GroupBy(s => (s.mfgid + "," + s.modid))
                            .Select(grp => grp.First())
                            .ToDictionary(KeyStr => KeyStr.mfgid + "," + KeyStr.modid, ValDobuble => ValDobuble.mode);
                    }

                    DicTestPA2a = myDic;
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
                    Digital_Definitions_Part_Specific = xmlRoot
                                                            .Elements("PAConfig")
                                                            .Select(x => new XmlPAConfigField
                                                            {
                                                                name = ((string)x.Attribute("name")).ToUpper(),
                                                                value = (string)x.Attribute("value")
                                                            }).ToDictionaryEx(x => x.name, y => y.value) as Dictionary<string, string>;

                    if (xmlRoot.Element("TestConfigs") != null)
                    {
                        if (isDigitalInformationLoaded) UserTestConfigs = new Dictionary<string, string>();

                        foreach (var settingconfig in xmlRoot.Element("TestConfigs").Elements())
                        {
                            if (!UserTestConfigs.ContainsKey(settingconfig.Name.LocalName))
                                UserTestConfigs.Add(settingconfig.Name.LocalName, settingconfig.Value);
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
    }
}