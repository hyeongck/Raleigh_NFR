using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using Avago.ATF.CrossDomainAccess;
using ClothoSharedItems;
using MPAD_TestTimer;
using LibEqmtDriver;
using AvagoGU;

namespace MyProduct
{
    public static class ResultBuilder
    {
        public static Dictionary<string, string> ParametersList = new Dictionary<string, string>();
        public static Dictionary<string, double> ParametersDict = new Dictionary<string, double>();
        public static Dictionary<int, SerialDef> All;
        public static bool Isfirststep;
        public static bool testLimitsExist;
        public static double tempvalue;
        private static Dictionary<string, RangeDef> tlPassDict;
        public static List<string>[] FailedTests = new List<string>[4];
        public static bool[] DuplicatedModuleID;
        public static int[] DuplicatedModuleIDCtr;
        public static bool headerFileMode = false;
        public static bool corrFileExists;
        private static readonly object locker = new object();
        public static List<int> ValidSites = new List<int>();
        public static ATFReturnResult results = new ATFReturnResult();
        public static List<int> SitesAndPhases;
        public static bool LiteDriver;
        public static bool firstData;
        //public static TestLib.AIDPR.iAIDPR AI;
        private static bool TestLimitAndCorrelationAvail;

        // MFG, Module ID, GuCalFactorsDict
        // 12.01.2021 - add for save MFG_ID & OTP_MODULE_ID through ATFCrossDomainWrapper.SetMfgIDAndModuleIDBySite()
        public static long M_MFG_ID { get; set; }
        public static long M_OTP_MODULE_ID_MIPI { get; set; }
        public static long M_Previous_OTP_MODULE_ID_MIPI { get; set; }
        public static long M_OTP_MODULE_ID_2DID { get; set; }
        public static long M_OTP_MODULE_ID_2DID_SYSTEM { get; set; }
        public static long M_Previous_OTP_MODULE_ID_2DID { get; set; }
        public static string M_OTP_CheckAll { get; set; }
        public static string M_Previous_OTP_CheckAll { get; set; }
        //correlation factor 
        public static List<string> corrFileTestNameList = new List<string>();  // Test names found in correlation file.
        public static Dictionary<string, double> GuCalFactorsDict = new Dictionary<string, double>();

        static ResultBuilder()
        {
            TestLimitAndCorrelationAvail = !string.IsNullOrWhiteSpace(ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TL_FULLPATH, string.Empty));

            //for (int i = 0; i < FailedTests.Length; i++)
            //{
            //    FailedTests[i] = new List<string>() { "program loading" };
            //}

            //try
            //{
            //    All = ATFCrossDomainWrapper.TestLimit_GetAllSerials();
            //    tlPassDict = ATFSharedData.Instance.TestLimitData.TSF.TestLimitsRange;
            //    testLimitsExist = true;
            //}
            //catch
            //{
            //    testLimitsExist = false;   // no test limit file
            //}

            //corrFileExists = "" != ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, "");

            //LiteDriver = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "") == "";
        }

        public static void LoadingCFandTL()
        {
            for (int i = 0; i < FailedTests.Length; i++)
            {
                FailedTests[i] = new List<string>() { "program loading" };
            }

            if (TestLimitAndCorrelationAvail)
            {
                try
                {
                    All = ATFCrossDomainWrapper.TestLimit_GetAllSerials();
                    tlPassDict = ATFSharedData.Instance.TestLimitData.TSF.TestLimitsRange;
                    testLimitsExist = true;
                }
                catch
                {
                    testLimitsExist = false;   // no test limit file
                }
            }

            corrFileExists = "" != ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, "");

            LiteDriver = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "") == "";
        }

        //public static void BeforeTest()
        //{
        //    GetValidSites();
        //    Eq.CurrentSplitTestPhase = SplitTestPhase.NoSplitTest;
        //    InitializeResults_Parallel();
        //}

        //public static void BeforeTest_SplitTest()
        //{
        //    GetValidSites();

        //    //if (!GU.runningGU.Contains(true))
        //    if (!GU.runningGU == true)
        //    {
        //        SitesAndPhases = GetSitesAndPhases();
        //    }

        //    Eq.CurrentSplitTestPhase = GetGlobalSplitPhase();

        //    InitializeResults();
        //}

        //private static SplitTestPhase GetGlobalSplitPhase()
        //{
        //    int site = SitesAndPhases.AllIndexOf(1, 2).First();

        //    if (((site ^ SitesAndPhases[site]) & 1) == 1)
        //    {
        //        return SplitTestPhase.PhaseA;
        //    }
        //    else
        //    {
        //        return SplitTestPhase.PhaseB;
        //    }
        //}

        //private static void GetValidSites()
        //{
        //    ValidSites.Clear();

        //    //if (true || LiteDriver || GU.runningGU.Contains(true))
        //    if (true || LiteDriver || GU.runningGU == true)
        //    {
        //        for (byte site = 0; site < Eq.NumSites; site++) ResultBuilder.ValidSites.Add(site);
        //    }
        //    else
        //    {
        //        List<int> ClothoValidSites = ATFCrossDomainWrapper.GetValidSitesIndexes();
        //        foreach (int site in ClothoValidSites) ValidSites.Add(site - 1);
        //    }
        //}
        public static void InitializeResults()
        {
            DuplicatedModuleID[0] = false;
            //DuplicatedModuleIDCtr[0] = 0;
            FailedTests[0].Clear();

            results.InitializeSite(0);
        }

        //private static void InitializeResults()
        //{
        //    for (int site = 0; site < SitesAndPhases.Count(); site++)
        //    {
        //        if (SitesAndPhases[site] != 2)
        //        {
        //            DuplicatedModuleID[site] = false;
        //            FailedTests[site].Clear();

        //            results.InitializeSite(site);
        //        }
        //    }
        //}

        //private static void InitializeResults_Parallel()
        //{
        //    for (int site = 0; site < Eq.NumSites; site++)
        //    {
        //        DuplicatedModuleID[site] = false;
        //        FailedTests[site].Clear();

        //        results.InitializeSite(site);
        //    }
        //}

        //private static ATFReturnResult CloneWithNan(this ATFReturnResult origResults)
        //{
        //    ATFReturnResult finalResults = new ATFReturnResult();

        //    var valsAllNan = Enumerable.Repeat(double.NaN, Eq.NumSites);

        //    List<int> SitesReadyforDataLog;

        //    if (Eq.CurrentSplitTestPhase == SplitTestPhase.NoSplitTest)
        //    {
        //        SitesReadyforDataLog = new List<int>();
        //        for (int site = 0; site < Eq.NumSites; site++) SitesReadyforDataLog.Add(site);
        //    }
        //    else
        //    {
        //        SitesReadyforDataLog = SitesAndPhases.AllIndexOf(2).ToList();
        //    }

        //    foreach (ATFReturnPararResult singleParam in origResults.Data)
        //    {
        //        ATFReturnPararResult finalSingleParam = new ATFReturnPararResult(singleParam.Name, singleParam.Unit);

        //        finalSingleParam.Vals = valsAllNan.ToList();

        //        foreach (int site in SitesReadyforDataLog)
        //        {
        //            finalSingleParam.Vals[site] = singleParam.Vals[site];
        //        }

        //        finalResults.Data.Add(finalSingleParam);
        //    }

        //    return finalResults;
        //}

        //public static ATFReturnResult FormatResultsForReturnToClotho()
        //{
        //    if (Eq.CurrentSplitTestPhase == SplitTestPhase.NoSplitTest)
        //    {
        //        return results.CloneWithNan();
        //    }
        //    else
        //    {
        //        if (SitesAndPhases.Contains(2))
        //        {
        //            return results.CloneWithNan();
        //        }
        //        else
        //        {
        //            return new ATFReturnResult(TestPlanRunConstants.RunSkipFlag + " No Stage 2 Result");
        //        }
        //    }
        //}

        //public static ATFReturnResult FormatResultsForReturnToClotho_ParallelTest_nono()
        //{
        //    return results.CloneWithNan();
        //}

        public static void InitializeSite(this ATFReturnResult data, int site)
        {
            foreach (ATFReturnPararResult rps in results.Data)
            {
                while (rps.Vals.Count < site + 1) rps.Vals.Add(double.NaN);

                rps.Vals[site] = double.NaN;
            }
        }

        public static bool CheckPass(string testName, double value)
        {
            try
            {
                if (testLimitsExist)
                {
                    if (testName.CIvContains("OTP_READ-RF2-PASS-FLAG"))// .CIvStartsWith("OTP_READ -") && testName.CIvEndsWith("-PASS-FLAG"))
                        return true;

                    if (tlPassDict.ContainsKey(testName))
                    {
                        return tlPassDict[testName].checkRange(value);
                    }
                    else return true;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        public static double GetUpperLimit(string testName)
        {
            try
            {
                if (testLimitsExist)
                    return tlPassDict[testName].TheMax;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Clear all result before starting the next run.
        /// </summary>
        public static void Clear()
        {
            results.Data.Clear();
            ParametersDict.Clear();
        }

        /// <summary>
        /// If testName exists, update. Otherwise, add.
        /// </summary>
        public static void UpdateResult(byte site, string testName, string units,
            double rawResult, byte decimalPlaces = byte.MaxValue,
            bool skipCheckSpec = false)
        {
            AddResult(site, testName, units, rawResult, false, decimalPlaces, skipCheckSpec);
        }

        /// </summary>
        public static void AddResult(byte site, string testName, string units,
            double rawResult, byte decimalPlaces = byte.MaxValue,
            bool skipCheckSpec = false)
        {
            AddResult(site, testName, units, rawResult, true, decimalPlaces, skipCheckSpec);
        }

        /// <summary>
        /// If testName exists, show error. No duplicate.
        /// </summary>
        //public static void AddResult(byte site, string testName, string units,
        //    double rawResult, AIDPR.iAIDPR AI, byte decimalPlaces = byte.MaxValue,
        //    bool skipCheckSpec = false)
        //{
        //    if (!Isfirststep)
        //    {
        //        if (!AI.Enabled)
        //        {
        //            AddResult(site, testName, units, rawResult, true, decimalPlaces, skipCheckSpec);
        //        }
        //        else if (AI.Enabled && !AI.IsGroupSkip)
        //        {
        //            if (AI.SkipACLR)
        //            {
        //                if (!testName.Contains("ACLR")) AddResult(site, testName, units, rawResult, true, decimalPlaces, skipCheckSpec);
        //            }
        //            else if (AI.SkipH2 || AI.SkipH3 || AI.SkipEVM || AI.SkipTXLEAKAGE)
        //            {
        //                if (!AI.ReductionItem.Contains(testName)) AddResult(site, testName, units, rawResult, true, decimalPlaces, skipCheckSpec);
        //            }
        //            else
        //            {
        //                AddResult(site, testName, units, rawResult, true, decimalPlaces, skipCheckSpec);
        //            }
        //        }
        //        else
        //        {
        //        }
        //    }
        //    else { if (!AI.Total_Para.ContainsKey(testName)) AI.Total_Para.Add(testName, units); ParametersDict.Add(testName, rawResult); }
        //}

        public static void AddResult(byte site, string testName, string units, double rawResult,
            bool isCheckDuplicate, byte decimalPlaces = byte.MaxValue, bool skipSpecCheck = false)
        {

            if (headerFileMode)
            {
                ATFResultBuilder.AddResult(ref results, testName, units, 999);
                return;
            }

            if (decimalPlaces != byte.MaxValue) rawResult = Math.Round(rawResult, decimalPlaces);

            if (double.IsNaN(rawResult) || double.IsInfinity(rawResult))  // force failure if not a number
            {
                rawResult = Math.Max(999999, GetUpperLimit(testName) + 100);

                //if (GU.factorMultiplyEnabledTests.Contains(testName)) rawResult /= GU.getGUcalfactor(site, testName);
                //else rawResult -= GU.getGUcalfactor(site, testName);
            }

            if (true)  // force failed tests[] to be populated
            {
                //TODO JJ and Keng Shan
                // Case Hallasan2.
                //double corrResult = GU.factorMultiplyEnabledTests.Contains(testName) ?
                //               rawResult * GU.getGUcalfactor(site, testName) :
                //               rawResult + GU.getGUcalfactor(site, testName);
                // Case Joker-Pinot.
                double corrResult = GU.getValueWithCF(site, testName, rawResult);

                if (isCheckDuplicate)
                {
                    if (ParametersDict.ContainsKey(testName))
                    {
                        string msg = string.Format("Duplicate test parameters headers: {0}", testName);
                        PromptManager.Instance.ShowInfo(msg);
                        LoggingManager.Instance.LogInfoTestPlan(msg);
                    }
                    else
                    {
                        ParametersDict.Add(testName, corrResult);
                    }
                }

                if (!CheckPass(testName, corrResult) && !skipSpecCheck)
                    FailedTests[site].Add(testName);
            }
            AddResult(site, testName, units, rawResult);
        }

        private static void AddResult(byte site, string testName, string units, double rawResult)
        {
            ATFReturnPararResult tr = new ATFReturnPararResult(testName, units);

            // Single site normal flow.
            if (site == 0)
            {
                tr.Vals.Add(rawResult);
                results.Data.Add(tr);
                return;
            }

            // Multi-Site flow.
            while (tr.Vals.Count < site + 1)
            {
                tr.Vals.Add(double.NaN);
            }
            tr.Vals[site] = rawResult;
            results.Data.Add(tr);
        }

        public static void Legacy_AddResult(byte site, ref ATFReturnResult results, string testName, string units, double rawResult, byte decimalPlaces = byte.MaxValue)
        {
            if (headerFileMode)
            {
                ATFResultBuilder.AddResult(ref results, testName, units, 999);
                return;
            }

            if (decimalPlaces != byte.MaxValue) rawResult = Math.Round(rawResult, decimalPlaces);

            if (double.IsNaN(rawResult) || double.IsInfinity(rawResult))  // force failure if not a number
            {
                rawResult = Math.Max(999999, GetUpperLimit(testName) + 100);

                if (GU.factorMultiplyEnabledTests.Contains(testName)) rawResult /= GU.getGUcalfactor(site, testName);
                else rawResult -= GU.getGUcalfactor(site, testName);
            }

            double corrResult = GU.factorMultiplyEnabledTests.Contains(testName) ?
                rawResult * GU.getGUcalfactor(site, testName) :
                rawResult + GU.getGUcalfactor(site, testName);

            if (!CheckPass(testName, corrResult)) FailedTests[site].Add(testName);

            ATFResultBuilder.AddResult(ref results, testName, units, rawResult);
        }

        //private static List<int> GetSitesAndPhases()
        //{
        //    bool handlerDriverSentSOT = !(LiteDriver || ATFCrossDomainWrapper.GetTriggerByManualClickFlag());

        //    if (handlerDriverSentSOT)
        //    {
        //        return ATFCrossDomainWrapper.GetHandlerSiteStates();
        //    }
        //    else
        //    {
        //        return ManuallyCycleSitesAndPhases();
        //    }
        //}

        //private static List<int> ManuallyCycleSitesAndPhases()
        //{
        //    List<int> sitesAndPhases;

        //    if (IsIrregularSitesAndPhases())
        //    {
        //        sitesAndPhases = ConstructInitialSitesAndPhases();
        //    }
        //    else if (Eq.CurrentSplitTestPhase == SplitTestPhase.PhaseA)
        //    {
        //        sitesAndPhases = ConstructSitesAndPhases(SplitTestPhase.PhaseB);
        //    }
        //    else
        //    {
        //        sitesAndPhases = ConstructSitesAndPhases(SplitTestPhase.PhaseA);
        //    }

        //    ATFCrossDomainWrapper.SetManualClickSiteStates(sitesAndPhases);

        //    return sitesAndPhases;
        //}

        //private static List<int> ConstructInitialSitesAndPhases()
        //{
        //    List<int> SitesAndPhases_local = new List<int>();

        //    for (int site = 0; site < Eq.NumSites; site++)
        //    {
        //        if (site % 2 == 0) SitesAndPhases_local.Add(1);
        //        else SitesAndPhases_local.Add(0);
        //    }

        //    return SitesAndPhases_local;
        //}

        //private static List<int> ConstructSitesAndPhases(SplitTestPhase phase)
        //{
        //    List<int> SitesAndPhases_local = new List<int>();

        //    switch (phase)
        //    {
        //        case SplitTestPhase.PhaseA:
        //            for (int site = 0; site < Eq.NumSites; site++)
        //            {
        //                if (site % 2 == 0) SitesAndPhases_local.Add(1);
        //                else SitesAndPhases_local.Add(2);
        //            }
        //            break;

        //        case SplitTestPhase.PhaseB:
        //            for (int site = 0; site < Eq.NumSites; site++)
        //            {
        //                if (site % 2 == 0) SitesAndPhases_local.Add(2);
        //                else SitesAndPhases_local.Add(1);
        //            }

        //            break;
        //    }

        //    return SitesAndPhases_local;
        //}

        //private static bool IsInitialSitesAndPhases()
        //{
        //    List<int> initialSitesAndPhases = ConstructInitialSitesAndPhases();

        //    for (int site = 0; site < SitesAndPhases.Count(); site++)
        //    {
        //        if (SitesAndPhases[site] != initialSitesAndPhases[site]) return false;
        //    }

        //    return true;
        //}

        //private static bool IsIrregularSitesAndPhases()
        //{
        //    if (SitesAndPhases == null) return true;

        //    return SitesAndPhases.Contains(0) && !IsInitialSitesAndPhases();
        //}

        public static void BuildResults(ref ATFReturnResult results, string paraName, string unit, double value)
        {
            if (paraName.Contains("MFG") || (paraName.Contains("OTP_MODULE_ID") && !(paraName.Contains("_2"))))
            {
                paraName = "M_" + paraName;

                if (paraName.Contains("MFG"))
                    M_MFG_ID = Convert.ToInt64(value);
                else
                {
                    long local_module_id = Convert.ToInt64(value), Previous_local_module_id = 0;

                    if (ClothoDataObject.Instance.EnableOnlySeoulUser)
                    {
                        M_OTP_MODULE_ID_MIPI = local_module_id;
                        Previous_local_module_id = M_Previous_OTP_MODULE_ID_MIPI;
                    }
                    else
                    //{
                    //    M_OTP_MODULE_ID_2DID = local_module_id;
                    //    Previous_local_module_id = M_Previous_OTP_MODULE_ID_2DID;
                    //}
                    {
                        M_OTP_MODULE_ID_MIPI = local_module_id;
                        Previous_local_module_id = M_Previous_OTP_MODULE_ID_MIPI;
                    }

                    if ((local_module_id == Previous_local_module_id) && (M_OTP_CheckAll == M_Previous_OTP_CheckAll))
                    {
                        value = -1;
                    }
                }
            }
            if (paraName.Contains("M_OTP_STATUS_CMOS-WAFER-X") || paraName.Contains("M_OTP_STATUS_CMOS-WAFER-Y") || paraName.Contains("M_OTP_STATUS_CMOS-WAFER-LOT") || paraName.Contains("M_OTP_STATUS_CMOS-WAFER-ID")
                || paraName.Contains("M_OTP_STATUS_LNA-WAFER-X") || paraName.Contains("M_OTP_STATUS_LNA-WAFER-Y") || paraName.Contains("M_OTP_STATUS_LNA-WAFER-LOT") || paraName.Contains("M_OTP_STATUS_LNA-WAFER-ID"))
            {
                M_OTP_CheckAll = M_OTP_CheckAll + Convert.ToString(value);
            }

            ATFResultBuilder.AddResult(ref results, paraName, unit, value);

            //if (corrFileTestNameList.Contains(paraName)) value += GuCalFactorsDict[paraName];

            tempvalue = GU.GuCalFactorsDict[1, paraName];

            if (GU.corrFileTestNameList.Contains(paraName)) value += GU.GuCalFactorsDict[1, paraName];

            if (!CheckPass(paraName, value))
            {
                //for (int site = 0; site < Eq.NumSites; site++)
                //{
                //    FailedTests[site].Add(paraName);
                //}
                FailedTests[0].Add(paraName);
            }
        }
    }

    public static class OldResultBuilder
    {
        private static Dictionary<int, SerialDef> All;
        private static bool testLimitsExist;
        private static SerialDef serialDef;
        public static bool headerFileMode = false;
        public static ATFReturnResult results = new ATFReturnResult();
        public static List<string> FailedTests = new List<string>();
        public static List<string> FailedTestsFlag = new List<string>();

        //Logger
        static ATFLogControl logger = ATFLogControl.Instance;
        static List<string> loggedMessages = new List<string>();

        // MFG, Module ID, GuCalFactorsDict
        public static long M_MFG_ID { get; set; }
        public static long M_OTP_MODULE_ID { get; set; }
        //correlation factor 
        public static List<string> corrFileTestNameList = new List<string>();  // Test names found in correlation file.
        public static Dictionary<string, float> GuCalFactorsDict = new Dictionary<string, float>();

        private static void LogToLogServiceAndFile(LogLevel logLev, string str)
        {
            loggedMessages.Add(str);
            logger.Log(logLev, str);
            Console.WriteLine(str);
        }

        static OldResultBuilder()
        {
            try
            {
                All = ATFCrossDomainWrapper.TestLimit_GetAllSerials();
                serialDef = All[1];
                testLimitsExist = true;
            }
            catch
            {
                testLimitsExist = false;   // no test limit file
            }
        }

        public static void InitializeResults()
        {
            //Reset failed test
            FailedTests.Clear();
            FailedTestsFlag.Clear();
        }

        public static bool CheckPass(string testName, double value)
        {
            try
            {
                if (testLimitsExist)
                    return serialDef.RangeCollection[testName].Range.checkRange(value);
                else
                    return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool CheckPassBin1(string testName, double value)
        {
            try
            {
                if (testLimitsExist)
                    if (serialDef.RangeCollection.ContainsKey(testName))
                        return serialDef.RangeCollection[testName].Range.checkRange(value);
                    else return true;
                else
                    return true;
            }
            catch
            {
                return true;
            }
        }

        public static double GetUpperLimit(string testName)
        {
            try
            {
                if (testLimitsExist)
                    return serialDef.RangeCollection[testName].Range.TheMax;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        //public static void BuildResults(ref ATFReturnResult results, string paraName, string unit, double value)
        //{
        //    if (paraName.Contains("MFG") || (paraName.Contains("OTP_MODULE_ID") && !(paraName.Contains("_2"))))
        //    {
        //        paraName = "M_" + paraName;

        //        if (paraName.Contains("MFG"))
        //            M_MFG_ID = Convert.ToInt64(value);
        //        else
        //            M_OTP_MODULE_ID = Convert.ToInt64(value);
        //    }

        //    ATFResultBuilder.AddResult(ref results, paraName, unit, value);

        //    if (GuCalFactorsDict.ContainsKey(paraName)) value += GuCalFactorsDict[paraName];

        //    if (!CheckPass(paraName, value))
        //    {
        //        FailedTests.Add(paraName);
        //    }
        //}
    }
}
