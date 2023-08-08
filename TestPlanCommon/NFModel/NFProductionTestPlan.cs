using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using MPAD_TestTimer;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using ZDB.ShareLib;
using TestPlanCommon.CommonModel;

namespace TestPlanCommon.NFModel
{
    public class NFProductionTestPlan : ProductionTestPlanBase
    {
        public ProductionTestTimeController TestTimeLogController { get; set; }

        public NFProductionTestPlan()
        {
            TestTimeLogController = new ProductionTestTimeController();
        }
    }

    public class ProductionTestTimeController
    {
        private string m_dataFilePath;
        private List<string> m_passedPcdList;
        private ProductionTestTimeReportGenerator m_model;
        private TestLineFixedCondition m_fixedCond;
        private ProductionTestTimeDebug m_debugModel;

        /// <summary>
        /// Default is deactivated. After Initialize(), this is set and won't change.
        /// </summary>
        public bool IsDeactivated { get; set; }

        public ProductionTestTimeController()
        {
            IsDeactivated = true;
            m_model = new ProductionTestTimeReportGenerator();
            m_debugModel = new ProductionTestTimeDebug();
            m_debugModel.IsDebugMode = false;
        }

        public void Initialize(TestPlanStateModel modelTpState, string atfConfigFilePath,
            Dictionary<string, string>[] dicTestCondTemp)
        {
            List<Dictionary<string, string>> dicTestCondTempNa = new List<Dictionary<string, string>>();
            ConvDicToList(dicTestCondTemp, ref dicTestCondTempNa);

            string pcd = m_debugModel.GetPcd();
            Initialize(pcd);
            if (IsDeactivated) return;

            Dictionary<string, string> atfConfig = modelTpState.GetAtfConfig(atfConfigFilePath);
            TestLineFixedCondition fc = new TestLineFixedCondition();
            fc.SetContractManufacturer(atfConfig["ATFResultRemoteSharePath"]);
            fc.TesterName = atfConfig["TesterID"];
            fc.PcdPackageName = pcd;
            fc.TestType = "NFR";

            AddFixedCondition(fc);
            AddTest(dicTestCondTempNa);

            StopWatchManager.Instance.IsActivated = true;
        }

        private void ConvDicToList(Dictionary<string, string>[] DicTemp, ref List<Dictionary<string, string>> ListTemp)
        {
            ListTemp = new List<Dictionary<string, string>>();

            for (int i = 0; i < DicTemp.Count(); i++)
            {
                ListTemp.Add(DicTemp[i]);
            }
        }

        public void Save()
        {
            if (IsDeactivated) return;

            //bool isTestPlanPassed = m_debugModel.IsPass(MyProduct.ResultBuilder.FailedTests.Count);
            bool isTestPlanPassed = m_debugModel.IsPass2ndTime();
            if (!isTestPlanPassed) return;

            //List<PaStopwatch2> swList = StopWatchManager.Instance.GetList(0);
            List<PaStopwatch2> swList = StopWatchManager.Instance.GetList();
            Save(swList);
            // Set to false to disable.
            StopWatchManager.Instance.IsActivated = !m_debugModel.IsToDeactivate();
        }

        private void Initialize(string currentPcdName)
        {
            // empty in LiteDriver mode.
            if (String.IsNullOrEmpty(currentPcdName))
            {
                IsDeactivated = true;
                return;
            }

            m_dataFilePath = GetReportFileFullPath("TestTimeLog.txt");
            bool isFileExist = File.Exists(m_dataFilePath);
            if (!isFileExist)
            {
                IsDeactivated = false;
                return;
            }

            m_passedPcdList = ReadSavedPcdList();
            bool isCurrentPassed = m_passedPcdList.Contains(currentPcdName);

            if (isCurrentPassed)
            {
                IsDeactivated = true;     // once set, will no longer activate.
                return;
            }

            IsDeactivated = false;
        }

        private void AddFixedCondition(TestLineFixedCondition fixedc)
        {
            if (IsDeactivated) return;
            m_model.AddFixedCondition(fixedc);
            m_fixedCond = fixedc;
        }

        private void AddTest(
            List<Dictionary<string, string>> DicTestCondTemp)
        {
            if (IsDeactivated) return;
            m_model.AddTest(DicTestCondTemp);
        }

        private void Save(List<PaStopwatch2> swList)
        {
            // If has checked already, skip checking to speed up.
            if (IsDeactivated) return;
            Save(m_fixedCond.PcdPackageName, swList, m_fixedCond.ContractManufacturer);
            // after first successful save, don't do again.
            IsDeactivated = m_debugModel.IsToDeactivate();
        }

        private void Save(string currentPcdName, List<PaStopwatch2> swList,
            string cmName)
        {
            // Create report.
            string reportContent = m_model.CreateReport(swList);
            string destReportFileName = m_model.GetReportFileName();
            string destReportFullPath = GetReportFileFullPath(destReportFileName);

            StreamWriter sw = File.CreateText(destReportFullPath);
            sw.Write(reportContent);
            sw.Flush();
            sw.Close();

            // Transfer to shared folder.
            CopyToServer(cmName, destReportFullPath);

            // Save to database.
            m_passedPcdList = ReadSavedPcdList();
            bool isExist = m_passedPcdList.Contains(currentPcdName);
            if (isExist)
            {

                if (!String.IsNullOrEmpty(currentPcdName))
                {
                    m_passedPcdList.Add(currentPcdName);
                }

                StringBuilder sb = new StringBuilder();
                foreach (string pcd in m_passedPcdList)
                {
                    sb.AppendLine(pcd);
                }

                sw = File.CreateText(m_dataFilePath);
                sw.Write(sb.ToString());
                sw.Flush();
                sw.Close();
            }

            string msg2 = String.Format("Test Time dashboard file generated in {0}", destReportFullPath);
            LoggingManager.Instance.LogInfoTestPlan(msg2);
            msg2 = String.Format("Test Time database log generated in {0}", m_dataFilePath);
            LoggingManager.Instance.LogInfoTestPlan(msg2);
        }

        private List<string> ReadSavedPcdList()
        {
            m_passedPcdList = new List<string>();
            bool isFirstTime = !File.Exists(m_dataFilePath);
            if (isFirstTime)
            {
                return m_passedPcdList;
            }

            FileInfo fi1 = new FileInfo(m_dataFilePath);
            StreamReader sr = new StreamReader(fi1.OpenRead());

            while (sr.Peek() >= 0)
            {
                m_passedPcdList.Add(sr.ReadLine());
            }

            sr.Close();

            return m_passedPcdList;
        }

        private bool CopyToServer(string cmName, string sourceFullPath)
        {
            // Default : BRCMPENANG
            string serverIp = "10.12.112.47";
            string userName = @"BRCMLTD\pcdreader";
            string password = "pc6re@6er";

            switch (cmName)
            {
                case "Inari P13":
                    serverIp = "172.16.11.13";
                    userName = "avago_user";
                    password = "@vag0u23r";
                    break;
                case "ASEK":
                    serverIp = "59.7.230.37";
                    userName = "avagoadm";
                    password = "avagoadm0319";
                    break;
                default:
                    cmName = "BRCMPENANG";
                    break;
            }

            Tuple<Boolean, String> result = ZDB.ShareLib.XFer.Push(sourceFullPath, serverIp, userName, password);
            string msg = String.Format("TTD file copied from {0} to {1} server.",
                sourceFullPath, cmName);
            if (!result.Item1)
            {
                msg = String.Format("Failed to copy TTD file from {0} to {1} server.",
                    sourceFullPath, cmName);
            }
            LoggingManager.Instance.LogInfoTestPlan(msg);

            return result.Item1;

        }

        private string GetReportFileFullPath(string fileName)
        {
            string dbPath = "C:\\TEMP";
            if (!Directory.Exists(dbPath))
            {
                dbPath = Path.GetTempPath();
            }

            string fp = Path.Combine(dbPath, fileName);
            return fp;
        }
    }

    public class ProductionTestTimeReportGenerator
    {
        private List<TestLineConditionNFR> m_condList;
        private TestLineFixedCondition m_fixedCond;

        public ProductionTestTimeReportGenerator()
        {
            m_separator = ',';
        }

        public void AddFixedCondition(TestLineFixedCondition fixedc)
        {
            m_fixedCond = fixedc;
        }

        public void AddTest(
            List<Dictionary<string, string>> DicTestCondTemp)
        {
            m_condList = new List<TestLineConditionNFR>();

            for (int testIndex = 0; testIndex < DicTestCondTemp.Count; testIndex++)
            {
                TestLineConditionNFR tlc = FormPaBandInfo(DicTestCondTemp[testIndex]);
                if (tlc == null) continue;
                //tlc.LineNumber = testIndex;     // start with 1 not 0.
                m_condList.Add(tlc);
            }
        }

        private TestLineConditionNFR FormPaBandInfo(Dictionary<string, string> tcLine)
        {
            TestLineConditionNFR c = new TestLineConditionNFR();

            string testMode = tcLine["TEST MODE"].Trim().ToUpper();
            c.TestMode = testMode;

            bool isToIgnore = true;

            switch (testMode)
            {
                case "MXA_TRACE":
                case "PXI_TRACE":
                case "NF":
                    c.TxBand = tcLine["TX1_BAND"];
                    c.RxBand = tcLine["RX1_BAND"];
                    c.Modulation = tcLine["MODULATION"];
                    c.StartFreq = tcLine["START_RXFREQ1"];
                    c.StopFreq = tcLine["STOP_RXFREQ1"];
                    c.LineNumber = System.Convert.ToInt32(tcLine["TEST NUMBER"]);
                    c.SwitchMode = tcLine["SWITCHING BAND"];
                    isToIgnore = false;
                    break;

                case "MIPI":
                case "SWITCH":
                    c.TxBand = tcLine["TX1_BAND"];
                    c.RxBand = tcLine["RX1_BAND"];
                    c.Modulation = null;
                    c.StartFreq = null;
                    c.StopFreq = null;
                    c.LineNumber = System.Convert.ToInt32(tcLine["TEST NUMBER"]);
                    c.SwitchMode = tcLine["SWITCHING BAND"];
                    isToIgnore = false;
                    break;

                case "MIPI_OTP":
                    c.TxBand = null;
                    c.RxBand = null;
                    c.Modulation = null;
                    c.StartFreq = null;
                    c.StopFreq = null;
                    c.LineNumber = System.Convert.ToInt32(tcLine["TEST NUMBER"]);
                    c.SwitchMode = tcLine["SWITCHING BAND"];
                    isToIgnore = false;
                    break;

                case "DC":
                    c.TxBand = null;
                    c.RxBand = null;
                    c.Modulation = null;
                    c.StartFreq = null;
                    c.StopFreq = null;
                    c.LineNumber = System.Convert.ToInt32(tcLine["TEST NUMBER"]);
                    c.SwitchMode = null;
                    isToIgnore = false;
                    break;

                default:
                    break;
            }
           
            return isToIgnore ? null : c;
        }

        public string GetReportFileName()
        {
            // Example: 2018-12-19T031143
            string sortableDt = DateTime.Now.ToString("yyyy-MM-ddThhmmss");
            string fn = String.Format("{0}_{1}_{2}.ttd",
                m_fixedCond.PcdPackageName, m_fixedCond.TesterName, sortableDt);
            return fn;
        }

        public string CreateReport(List<PaStopwatch2> swList)
        {
            if (swList.Count == 0) return String.Empty;

            // filter stopwatch to get only test. Combine stopwatch and test condition.
            List<PaStopwatch2> testSwList = new List<PaStopwatch2>();

            foreach (PaStopwatch2 sw in swList)
            {
                bool isATest = sw.ParentName == String.Empty && sw.IsHasNameType;
                if (!isATest) continue;
                testSwList.Add(sw);
            }

            // stopwatch count will be more than test count. Due to BurnOtpFlag() after run test.
            string msg = String.Format("Stopwatch count: {0}, Test parameter count: {1}",
                testSwList.Count, m_condList.Count);
            LoggingManager.Instance.LogInfo(msg);

            List<TestLineCollectionNFR> reportLines = new List<TestLineCollectionNFR>();

            bool isValidCount = testSwList.Count >= m_condList.Count;

            if (!isValidCount)
            {
                string errorMsg = String.Format("Error: {0}", msg);
                return errorMsg;
            }

            //for (int i = 0; i < m_condList.Count; i++)
            //{
            //    TestLineCollectionNFR rl = new TestLineCollectionNFR();
            //    rl.TcfIndex = m_condList[i].LineNumber;
            //    rl.ElapsedMs = testSwList[i].ElapsedMs;
            //    rl.TestConditions = m_condList[i];
            //    reportLines.Add(rl);
            //}


            double sumOfTest = 0;

            for (int i = 0; i < m_condList.Count; i++)
            {
                TestLineCollectionNFR rl = new TestLineCollectionNFR();
                rl.TcfIndex = i + 1;
                rl.ElapsedMs = testSwList[i].ElapsedMs;
                rl.TestConditions = m_condList[i];
                reportLines.Add(rl);
                sumOfTest = sumOfTest + testSwList[i].ElapsedMs;
            }

            // Add Total test time
            TestLineCollectionNFR totalTestTimeRl = new TestLineCollectionNFR();
            totalTestTimeRl.TcfIndex = -1;
            totalTestTimeRl.ElapsedMs = sumOfTest;
            totalTestTimeRl.TestConditions = new TestLineConditionNFR();
            totalTestTimeRl.TestConditions.TestMode = "TIME_Total";
            reportLines.Add(totalTestTimeRl);


            foreach (PaStopwatch2 sw in swList)
            {
                bool executionTime = sw.Name == "TIME_DoATFTest";
                if (!executionTime) continue;
                TestLineCollectionNFR rl = new TestLineCollectionNFR();
                rl.TcfIndex = -1;
                rl.ElapsedMs = sw.ElapsedMs - sumOfTest;
                rl.TestConditions = new TestLineConditionNFR();
                rl.TestConditions.TestMode = "TIME_Overhead";
                reportLines.Add(rl);
            }

            StringBuilder sb = new StringBuilder();

            // generate header.
            sb.AppendLine(CreateColumnHeader1());

            List<string> m_fixedColumns = new List<string>();
            // d1 = 30/12/2018   d2 = 30/12/2018 10:08 AM
            string d1 = DateTime.Now.ToString("d", System.Globalization.CultureInfo.CreateSpecificCulture("en-MY"));
            string d2 = DateTime.Now.ToString("g", System.Globalization.CultureInfo.CreateSpecificCulture("en-MY"));

            m_fixedColumns.AddRange(new string[] { d1, d2,
                m_fixedCond.PcdPackageName, m_fixedCond.TestType,
                m_fixedCond.TesterName, m_fixedCond.ContractManufacturer });
            string fixedColumns = String.Join(m_separator.ToString(), m_fixedColumns.ToArray());

            // generate content.
            foreach (TestLineCollectionNFR testConditionLine in reportLines)
            {
                FormLine(sb, fixedColumns, testConditionLine);
            }

            return sb.ToString();
        }

        private char m_separator;

        public char Separator
        {
            get { return m_separator; }
            set
            {
                if (value == '\0') return;        // null character.
                if (Char.IsWhiteSpace(value)) return;
                m_separator = value;
            }
        }

        private string CreateColumnHeader1()
        {
            List<string> col = new List<string>();
            col.AddRange(new string[] { "Date", "DateTime", "PCDPackageName", "TestType" });
            col.AddRange(new string[] { "TesterName", "CM" });
            col.AddRange(new string[] { "TestNumber", "Time_ms", "TestMode", "TxBand", "RxBand", "RxStartFreq", "RxStopFreq", "Modulation", "Switch_Mode" });

            string c1 = String.Join(m_separator.ToString(), col.ToArray());
            return c1;
        }

        private void FormLine(StringBuilder sb, string fixedColumns, TestLineCollectionNFR tlList)
        {
            sb.Append(fixedColumns);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TcfIndex);         // TestNumber
            sb.Append(m_separator, 1);
            string ms = Convert(tlList.ElapsedMs);
            sb.Append(ms);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.TestMode);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.TxBand);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.RxBand);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.StartFreq);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.StopFreq);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.Modulation);
            sb.Append(m_separator, 1);
            sb.Append(tlList.TestConditions.SwitchMode);

            sb.AppendLine();
        }

        private string Convert(double timeMs)
        {
            return timeMs.ToString("F4");
        }
    }

    public class TestLineCollectionNFR
    {
        public int TcfIndex { get; set; }
        public string UniqueHeaderName { get; set; }
        public double ElapsedMs { get; set; }
        public TestLineConditionNFR TestConditions { get; set; }
    }

    /// <summary>
    /// Joker.
    /// </summary>
    public class TestLineConditionNFR
    {
        public string TestMode { get; set; }
        public string TxBand { get; set; }
        public string RxBand { get; set; }
        public string Modulation { get; set; }
        public string StartFreq { get; set; }
        public string StopFreq { get; set; }
        public int LineNumber { get; set; }
        public string SwitchMode { get; set; }
    }   
}
