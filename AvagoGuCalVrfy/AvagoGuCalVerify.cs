using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using System.Runtime.InteropServices;
using Ionic.Zip;
using System.Threading;
using ClothoSharedItems;

namespace AvagoGU
{
    public class GU
    {
        #region variable declarations

        public static SingleProductStatus thisProductsGuStatus;
        public static AllProductStatusFile guStatusFile;
        public static string productTag;

        const bool ENABLE_GUCALVERIFY_MODULE = true;   // TOUCH
        const bool ENABLE_ICC_CAL = true;  // *** temp, automate this somehow

        internal const bool passwordProtectUI = false;  // TOUCH. determines whether or not GU UI is password protected
        internal const string password = "avago";  // TOUCH. The password required to run GU, if passwordProtectUI = true

        internal const int minDutsForGu = 1;  // TOUCH. the minimum number of DUTs for valid GU Cal, else program complains

        public static int numIgnoreLoops = 0;   // Input to Init. Program will run numIgnoreLoops runs before recording data
        public static int numAvgLoops = 0;   // Input to Init. After running ignore loops, program will run numAvgLoops runs and take the average
        public static int runLoopDelay = 0;  // Input to Init. The delay (in milliseconds) between loops
        //public static int numRunLoops = 1;   // Program will loop this many times, includes ignore loops and average loops
        //public static int runLoop = 2 * numRunLoops;  //Initialize to big number in case developer leaves out loop
        public static double numRunLoops = 1;   // Program will loop this many times, includes ignore loops and average loops
        public static double runLoop = 2 * numRunLoops;  //Initialize to big number in case developer leaves out loop

        public static string correlationFilePath = "";
        public static string iccCalFilePath = "";
        public static string correlationTemplatePath = "";
        public static string iccCalTemplatePath = "";
        public static string benchDataPath = "";

        public static string testertype;
        public static byte siteNo = 1;

        public static GuModes GuMode = GuModes.IccCorrVrfy;      // this is the original mode that operator selects
        public static Dictionary<GuModes, string> GuModeNames = new Dictionary<GuModes, string>()
        {
            { GuModes.IccCorrVrfy, "GU Icc/Corr/Verify" },
            { GuModes.CorrVrfy, "GU Corr/Verify" },
            { GuModes.Vrfy, "GU Verify" },
            { GuModes.None, "" },
        };
        public static bool runningGU = true;   // indicates if any GU mode is running at this moment. Must init to true
        //public static bool runningGUIccCal = false;   // indicates if Icc Cal is running at this moment. Must init to false
        public static bool[] runningGUIccCal = new bool[64];
        public static int currentGuDutIndex = 0;
        public static int currentGuDutSN = 0;
        public static bool autoSnSequencing = false;   // was the device SN read, or should we enforce the same SN order as in GU ref file?
        static int currentGuSiteIndex = 0;

        internal static List<int> sitesAllExistingList = new List<int>();
        internal static List<int> sitesUserReducedList = new List<int>();
        public static int selectedBatch = 1;
        public static Dictionary<int, List<int>> dutIdAllLoose = new Dictionary<int, List<int>>();
        public static Dictionary<int, List<int>> dutIdAllDemo = new Dictionary<int, List<int>>();
        public static List<int> dutIdLooseUserReducedList = new List<int>();
        internal static List<int> dutIDtestedDead = new List<int>();
        internal static HashSet<int> dutIDfailedVrfy = new HashSet<int>();

        public static Dictionary<int, bool> GuIccCalFailed = new Dictionary<int, bool>();
        public static Dictionary<int, bool> GuCorrFailed = new Dictionary<int, bool>();
        public static Dictionary<int, bool> GuVerifyFailed = new Dictionary<int, bool>();
        public static bool iccCalInaccuracyDetected = false;
        public static bool oopsRanTheWrongSite = false;
        public static bool forceReload = false;
        public static string prodTag = "";

        const string iccCalTestNameExtension = "_IccCal";
        const string iccCalFileNameExtension = "_IccCal";

        const string ConstCalSheetName = "CalPath";

        public enum IccCalLoss
        {
            InputLoss, OutputLoss
        }

        public static Dictionary.TripleKey<int, string, int, double> finalRefDataDict = new Dictionary.TripleKey<int, string, int, double>();   // [batchID, test name, dut ID, data value]
        //static Dictionary.QuadKey<int, string, UnitType, int, float> demoDataDict = new Dictionary.QuadKey<int, string, UnitType, int, float>();   // [batchID, test name, loose/demo, dut ID, data value]
        //static Dictionary.DoubleKey<int, string, float> demoBoardOffsets = new Dictionary.DoubleKey<int, string, float>();  // [batchID, test name, offset value]
        //static Dictionary.DoubleKey<int, string, float> demoLooseCorrCoeff = new Dictionary.DoubleKey<int, string, float>();  // [batchID, test name, corr coefficient]
        //static Dictionary.TripleKey<int, string, int, float> demoBoardOffsetsPerDut = new Dictionary.TripleKey<int, string, int, float>();   // [batchID, test name, dut ID, data value]
        //static Dictionary<string, float> loLimCalAddDict = new Dictionary<string, float>();   // [test name, limit]
        //static Dictionary<string, float> hiLimCalAddDict = new Dictionary<string, float>();   // [test name, limit]
        //static Dictionary<string, float> loLimCalMultiplyDict = new Dictionary<string, float>();   // [test name, limit]
        //static Dictionary<string, float> hiLimCalMultiplyDict = new Dictionary<string, float>();   // [test name, limit]
        //static Dictionary<string, float> loLimVrfyDict = new Dictionary<string, float>();   // [test name, limit]
        //static Dictionary<string, float> hiLimVrfyDict = new Dictionary<string, float>();   // [test name, limit]
        public static Dictionary.QuadKey<int, string, UnitType, int, double> demoDataDict = new Dictionary.QuadKey<int, string, UnitType, int, double>();   // [batchID, test name, loose/demo, dut ID, data value]
        public static Dictionary.DoubleKey<int, string, double> demoBoardOffsets = new Dictionary.DoubleKey<int, string, double>();  // [batchID, test name, offset value]
        public static Dictionary.DoubleKey<int, string, double> demoLooseCorrCoeff = new Dictionary.DoubleKey<int, string, double>();  // [batchID, test name, corr coefficient]
        public static Dictionary.TripleKey<int, string, int, double> demoBoardOffsetsPerDut = new Dictionary.TripleKey<int, string, int, double>();   // [batchID, test name, dut ID, data value]
        public static Dictionary<string, double> loLimCalAddDict = new Dictionary<string, double>();   // [test name, limit]
        public static Dictionary<string, double> hiLimCalAddDict = new Dictionary<string, double>();   // [test name, limit]
        public static Dictionary<string, double> loLimCalMultiplyDict = new Dictionary<string, double>();   // [test name, limit]
        public static Dictionary<string, double> hiLimCalMultiplyDict = new Dictionary<string, double>();   // [test name, limit]
        public static Dictionary<string, double> loLimVrfyDict = new Dictionary<string, double>();   // [test name, limit]
        public static Dictionary<string, double> hiLimVrfyDict = new Dictionary<string, double>();   // [test name, limit]
        public static List<string> factorAddEnabledTests = new List<string>();    // list of test names that use Add calfactor
        public static List<string> factorMultiplyEnabledTests = new List<string>();    // list of test names that use Multiply calfactor

        public static Dictionary.TripleKey<int, string, int, double> rawAllCorrDataDict = new Dictionary.TripleKey<int, string, int, double>();   // [site, test name, dut ID, data value]    this is all the correlation raw data

        public static Dictionary<string, string> unitsDict = new Dictionary<string, string>();    // [test name, units]
        public static Dictionary<string, bool> unitsAreDb = new Dictionary<string, bool>();    // [test name, are dB?]
        public static Dictionary<string, int> testNumDict = new Dictionary<string, int>();    // [test name, test num]
        //static Dictionary.QuadKey<int, string, int, int, float> rawAllMsrDataDict = new Dictionary.QuadKey<int, string, int, int, float>();   // [site, test name, dut ID, run loop, data value]    this is all the raw data
        public static Dictionary.TripleKey<int, string, int, double> rawAllMsrDataDict = new Dictionary.TripleKey<int, string, int, double>();   // [site, test name, dut ID, data value]    this is all the raw data
        public static Dictionary.TripleKey<int, string, int, double> correctedMsrDataDict = new Dictionary.TripleKey<int, string, int, double>();                                                                                                                                  //static Dictionary.QuadKey<int, string, int, int, double> rawIccCalMsrDataDict = new Dictionary.QuadKey<int, string, int, int, double>();   // [site, test name, dut ID, run loop, data value] 
        public static Dictionary.TripleKey<int, string, int, double> rawIccCalMsrDataDict = new Dictionary.TripleKey<int, string, int, double>();   // [site, test name, dut ID, data value]
        //static Dictionary.QuadKey<int, string, int, int, float> rawIccCalMsrDataDict = new Dictionary.QuadKey<int, string, int, int, float>();   // [site, test name, dut ID, run loop, data value] 
        public static Dictionary.TripleKey<int, int, string, IccCalError> IccCalAvgErrorDict = new Dictionary.TripleKey<int, int, string, IccCalError>();   // [site, test name, dut ID, data value]
        public static Dictionary.TripleKey<int, string, int, double> avgMsrDataDict = new Dictionary.TripleKey<int, string, int, double>();   // [site, test name, dut ID, data value]    this is the average of the raw data runs
                                                                                                                                       //static Dictionary.TripleKey<int, string, int, double> correctedMsrDataDict = new Dictionary.TripleKey<int, string, int, double>();
        public static Dictionary.TripleKey<int, string, int, double> correctedMsrErrorDict = new Dictionary.TripleKey<int, string, int, double>();                                                                                                                            // static Dictionary.TripleKey<int, string, int, double> correctedMsrErrorDict = new Dictionary.TripleKey<int, string, int, double>();

        //static Dictionary.TripleKey<int, string, int, float> avgMsrDataDict = new Dictionary.TripleKey<int, string, int, float>();   // [site, test name, dut ID, data value]    this is the average of the raw data runs
        //static Dictionary.TripleKey<int, string, int, float> correctedMsrDataDict = new Dictionary.TripleKey<int, string, int, float>();
        //static Dictionary.TripleKey<int, string, int, float> correctedMsrErrorDict = new Dictionary.TripleKey<int, string, int, float>();
        public static Dictionary.DoubleKey<int, string, double> IccCalFactorsTempDict = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, loss factor]  this is a temporary dictionary that is only populated when " + GuModeNames[GuMode] + " is running
        public static Dictionary.DoubleKey<int, string, double> IccServoTargetCorrection = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, error]  pre-existing icc servo errors get loaded into here
        public static Dictionary.DoubleKey<int, string, double> IccServoNewTargetCorrection = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, error]  updated icc servo errors get loaded into here
        public static Dictionary.DoubleKey<int, string, double> GuCalFactorsDict = new Dictionary.DoubleKey<int, string, double>();
        //static Dictionary.DoubleKey<int, string, double> IccCalFactorsTempDict = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, loss factor]  this is a temporary dictionary that is only populated when " + GuModeNames[GuMode] + " is running
        //public static Dictionary.DoubleKey<int, string, float> GuCalFactorsDict = new Dictionary.DoubleKey<int, string, float>();
        //static Dictionary.DoubleKey<int, string, float> IccCalFactorsTempDict = new Dictionary.DoubleKey<int, string, float>();  // [site, test name, loss factor]  this is a temporary dictionary that is only populated when " + GuModeNames[GuMode] + " is running
        //static Dictionary.DoubleKey<int, string, float> IccServoTargetCorrection = new Dictionary.DoubleKey<int, string, float>();  // [site, test name, error]  pre-existing icc servo errors get loaded into here
        //static Dictionary.DoubleKey<int, string, float> IccServoNewTargetCorrection = new Dictionary.DoubleKey<int, string, float>();  // [site, test name, error]  updated icc servo errors get loaded into here
        public static Dictionary<string, bool> ApplyIccServoTargetCorrection = new Dictionary<string, bool>();    //
        //public static Dictionary.DoubleKey<int, string, float> IccServoVSGlevel = new Dictionary.DoubleKey<int, string, float>();  // [site, test name, VSG level]  this is used 
        //static Dictionary.DoubleKey<int, string, float> IccServoNewVSGlevel = new Dictionary.DoubleKey<int, string, float>();  // [site, test name, VSG level]  this is used 

        //static Dictionary.DoubleKey<int, string, float> corrCoeffDict = new Dictionary.DoubleKey<int, string, float>();    // [site, test name, correlation coefficient]
        public static Dictionary.DoubleKey<int, string, double> IccServoVSGlevel = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, VSG level]  this is used 
        public static Dictionary.DoubleKey<int, string, double> IccServoNewVSGlevel = new Dictionary.DoubleKey<int, string, double>();  // [site, test name, VSG level]  this is used 
        public static Dictionary.DoubleKey<int, string, double> corrCoeffDict = new Dictionary.DoubleKey<int, string, double>();    // [site, test name, correlation coefficient]
        public static SortedList<int, string> benchTestNameList = new SortedList<int, string>();  // Test names found in bench data file. This is deemed the superset. Code checks that no extra non-zero CF parameters are found in correlation file or run-time tests
        //public static List<string> corrFileTestNameList = new List<string>();  // Test names found in correlation file.
        public static List<string> corrFileTestNameList = new List<string>();  // Test names found in correlation file.
        public static List<string> iccCalTemplateTestNameList = new List<string>();  // Test names found in Icc Cal file.
        public static SortedList<int, string> testedTestNameList = new SortedList<int, string>();  // code will ensure that testedTestNameList is a subset of benchTestNameList, for all non-zero CF parameters
        public static HashSet<string> testNamesFailedVrfy = new HashSet<string>();
        public static byte currentGUattemptNumber = 1;
        public static byte iccCalNumInaccurateAttempts = 0;

        public static Dictionary<string, CorrError> DicCorrError = new Dictionary<string, CorrError>();
        public static Dictionary<int, Dictionary<string, VerifyError>> DicVerifyError = new Dictionary<int, Dictionary<string, VerifyError>>();
        public static Dictionary<string, string> iccCalFactorRedirect = new Dictionary<string, string>();  // so that tests can use optionally use calfactors from other tests

        public static ATFLogControl logger = ATFLogControl.Instance;  // for writing to Clotho Logger
        public static WindowControl wnd = new WindowControl();
        public static List<string> loggedMessages = new List<string>();

        public static bool previousIccCalFactorsExist = false;
        public static bool IccCalTemplateExists = false;

        public static string IccCalStartTime = "";
        public static string IccCalFinishTime = "";
        public static string CorrStartTime = "";
        public static string CorrFinishTimeHumanFriendly = "";
        public static string CorrFinishTime = "";


        public class IccCalTestNames
        {
            public static List<string> All = new List<string>();   //full ordered list so we don't lose order
            
            public static Dictionary<string, IccCalTestNames> Pin = new Dictionary<string, IccCalTestNames>(); 
            public static Dictionary<string, IccCalTestNames> Pout = new Dictionary<string, IccCalTestNames>();
            public static Dictionary<string, IccCalTestNames> Icc = new Dictionary<string, IccCalTestNames>();
            public static Dictionary<string, IccCalTestNames> Key = new Dictionary<string, IccCalTestNames>();
            //public static Dictionary.TripleKey<double, float, string, IccCalTestNames> Freq = new Dictionary.TripleKey<double, float, string, IccCalTestNames>();
            public static Dictionary.TripleKey<double, double, string, IccCalTestNames> Freq = new Dictionary.TripleKey<double, double, string, IccCalTestNames>();

            public string PoutTestName;
            public string PinTestName;
            public string IccTestName;
            public string KeyName;
            public double TargetPout;
            public double Frequency;
            public string IQname;

            public IccCalTestNames(string pinTestName, string poutTestName, string iccTestName, string keyName, double targetPout, double frequency, string iqName)
            {
                this.PoutTestName = poutTestName;
                this.PinTestName = pinTestName;
                this.IccTestName = iccTestName;
                this.KeyName = keyName;
                this.TargetPout = targetPout;
                this.Frequency = frequency;
                this.IQname = iqName;

            }

            public static void Add(string pinTestName, string poutTestName, string iccTestName, string keyName, double targetPout, double frequency, string iqName)
            {
                if (All.Contains(pinTestName)) return;

                keyName += iccCalTestNameExtension;

                IccCalTestNames names = new IccCalTestNames(pinTestName, poutTestName, iccTestName, keyName, targetPout, frequency, iqName);

                Pin.Add(pinTestName, names);
                Pout.Add(poutTestName, names);
                Icc.Add(iccTestName, names);
                Key.Add(keyName, names);

                Freq[frequency, targetPout, iqName] = names;

                //Dictionary<string, IccCalTestNames> fhahd = Freq2[2505].OrderByDescending(t => t.Key).First().Value;   // for getting highest Pout conditions at certain freq

                All.Add(pinTestName);
                All.Add(poutTestName);
                All.Add(iccTestName);

            }

        }

        public class IccCalError
        {
            public double AvgError;
            public double HiLim;
            public double LoLim;

            public IccCalError(double avgError, double loLim, double hiLim)
            {
                this.AvgError = avgError;
                this.HiLim = hiLim;
                this.LoLim = loLim;
            }
        }

        public class CorrError
        {
            public int Gu_id;
            public string Parameter;
            public double HiLim;
            public double LoLim;
            public double Factor;

            public CorrError(int Gu_id, string Parameter, double loLim, double hiLim, double Factor)
            {
                this.Gu_id = Gu_id;
                this.Parameter = Parameter;
                this.HiLim = hiLim;
                this.LoLim = loLim;
                this.Factor = Factor;
            }
        }

        public class VerifyError
        {
            public int Gu_id;
            public string Parameter;
            public double HiLim;
            public double LoLim;
            public double correctedMsrError;

            public VerifyError(int Gu_id, string Parameter, double loLim, double hiLim, double correctedMsrError)
            {
                this.Gu_id = Gu_id;
                this.Parameter = Parameter;
                this.HiLim = hiLim;
                this.LoLim = loLim;
                this.correctedMsrError = correctedMsrError;
            }
        }

        public enum GuModes
        {
            IccCorrVrfy,
            CorrVrfy,
            Vrfy,
            None
        }

        // KKL : Set for Production Set Cal Packages
        //internal static s_ProductionVariable ProductionSettings = new s_ProductionVariable();

        #endregion

        public enum UI
        {
            Production, Engineering, FullAutoCal
        }

        internal static class ProductionSettings
        {
            public static bool b_FlagProduction;   // Set Production Flag
            public static bool ICC_Cal_PassStatus;
            public static bool Corr_PassStatus;
            public static bool Vrfy_PassStatus;
            public static bool VrfyBypass_Allow;
            public static DateTime Vrfy_Date;
            public static bool NewDevice;
            public static bool b_Disable_MsgFlag;
        }



        public static void DoInit_afterCustomCode(bool useProductionGUI, bool promptEachDevice, string productTag, string guZipRemoteSavePath, int NumIgnoreLoops = 0, int NumAvgLoops = 1, int LoopDelayMilliSeconds = 0)
        {
            try
            {
                numIgnoreLoops = NumIgnoreLoops;
                numAvgLoops = NumAvgLoops;
                runLoopDelay = LoopDelayMilliSeconds;
                numRunLoops = numIgnoreLoops + numAvgLoops;
                prodTag = productTag;
                DataAnalysis.DataAnalysisFiles.guDataRemoteDir = guZipRemoteSavePath; //DH LocalGUdatabase

                //clear previous measurements
                rawAllMsrDataDict.Clear();
                avgMsrDataDict.Clear();
                correctedMsrDataDict.Clear();
                correctedMsrErrorDict.Clear();
                dutIDtestedDead.Clear();
                iccCalInaccuracyDetected = false;
                testNamesFailedVrfy.Clear();
                corrFileTestNameList.Clear();

                if (GuMode == GuModes.IccCorrVrfy) GuCalFactorsDict.Clear();
                IccCalFactorsTempDict.Clear();
                // In case Icc servo needs to learn Icc target compensation
                //IccServoTargetCorrection = new Dictionary.DoubleKey<int, string, float>(IccServoNewTargetCorrection);
                IccServoTargetCorrection = new Dictionary.DoubleKey<int, string, double>(IccServoNewTargetCorrection);
                IccServoNewTargetCorrection.Clear();
                IccServoNewVSGlevel.Clear();
                corrCoeffDict.Clear();

                if (prodTag == "") prodTag = "Test";


                #region KK code that used to be in TestPlan.cs

                // KKL : Added Database tracking on ICC and GU Cal Status
              
                cDatabase.Load_Database();

                ProductionSettings.b_FlagProduction = useProductionGUI;
                ProductionSettings.b_Disable_MsgFlag = !promptEachDevice;   // set True if to remove all message box

                if (cDatabase.Check_Device_Exist(prodTag))
                {
                    ProductionSettings.ICC_Cal_PassStatus = cDatabase.Get_ICC_PassStatus(prodTag);
                    ProductionSettings.Corr_PassStatus = cDatabase.Get_Corr_PassStatus(prodTag);
                    ProductionSettings.Vrfy_PassStatus = cDatabase.Get_Vrfy_PassStatus(prodTag);
                    ProductionSettings.Vrfy_Date = cDatabase.Get_Vrfy_Date(prodTag);

                    ProductionSettings.VrfyBypass_Allow = cDatabase.Get_VrfyBypassAllowed(prodTag);
                }
                else
                {
                    ProductionSettings.NewDevice = true;
                    cDatabase.CreateNewDevice(prodTag);
                }

                #endregion


                if (!ENABLE_GUCALVERIFY_MODULE)
                {
                    runningGU = false;
                    return;
                }

                sitesAllExistingList = ATFCrossDomainWrapper.GetValidSitesIndexes();   // *** this doesn't appear to work from Lite Driver
                sitesAllExistingList = new List<int> { 1 };    // *** remove this if above line is working

                //wnd.MinimizeExcel();   // minimize Excel test condition file during debug, it's annoying

                runningGU = true;

                //Read files
                //ReadExcelTCF();
                ReadExcelTCF(ConstCalSheetName);
                ReadBenchData();
                ReadGuCorrelationFile();  // must do this after reading bench data
                ReadIccCalfactorFile();

                if (!runningGU)   // if error peviously ocurred while opening files (that's the only way runningGUCalVrfy would be false at this point)
                {
                    MessageBox.Show(wnd.ShowOnTop(), GuModeNames[GuMode] + " cannot be run due to file errors.\nPlease see LogService for error details.",
                        GuModeNames[GuMode],
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    LogToLogServiceAndFile(LogLevel.Error, "*** " + GuModeNames[GuMode] + " cannot be run due to file errors, please see above for error details");
                    if (ProductionSettings.b_FlagProduction) forceReload = true;
                    return;
                }


                if (ProductionSettings.b_FlagProduction) // Production UI   // production mode, regular product ID detected
                {
                    #region Production Mode

                    // KKL: Replace UI interface for Production based on GO Team feedback.
                    frmProductionVerification ProdUI = new frmProductionVerification();

                    DialogResult Rslt = ProdUI.ShowDialog();

                    switch (Rslt)
                    {
                        case DialogResult.OK:     // .OK means run Icc + Corr + Verify
                            GuMode = GuModes.IccCorrVrfy;
                            runningGUIccCal[siteNo] = ENABLE_ICC_CAL;
                            runningGU = true;
                            break;
                        case DialogResult.Ignore:  // .Ignore means run Corr + Verify
                            GuMode = GuModes.CorrVrfy;
                            runningGUIccCal[siteNo] = false;
                            runningGU = true;
                            break;
                        case DialogResult.Retry:   // .Retry means run Verify
                            GuMode = GuModes.Vrfy;
                            runningGUIccCal[siteNo] = false;
                            runningGU = true;
                            numIgnoreLoops = 0;
                            numAvgLoops = 1;
                            numRunLoops = numIgnoreLoops + numAvgLoops;
                            break;
                        default:                     // Don't run any GU
                            GuMode = GuModes.None;
                            runningGUIccCal[siteNo] = false;
                            runningGU = false;
                            LogToLogServiceAndFile(LogLevel.HighLight, DateTime.Now.ToString() + ": GU Verfication Process By-passed!!");
                            break;
                    }

                    #endregion
                }

                else // Old UI
                {
                    if (prodTag.ToLower().Contains("_gu_") & prodTag.ToLower().Contains("_verify"))   // in production mode, this is the key that tells us to run " + GuModeNames[GuMode] + "
                    {
                        #region David's original production mode (no UI, depends on ProductTag)

                        List<string> dutIDsInProductTag = new List<string>();
                        dutIDsInProductTag = prodTag.Split(';').Skip(1).ToList();

                        int productTagInt = 0;

                        foreach (string dutIDstr in dutIDsInProductTag)
                        {
                            if (ExtractDutID(dutIDstr, out productTagInt))
                            {
                                //if (dutIDAllInFileList.Contains(productTagInt))  // if everything is good
                                if (true)  // if everything is good
                                {
                                    dutIdLooseUserReducedList.Add(productTagInt);
                                }
                                else
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: entered ProductTag " + prodTag + ", but GU bench data file " + benchDataPath + " does not have data for Device " + productTagInt + ". Cannot run " + GuModeNames[GuMode] + ".");
                                    MessageBox.Show(wnd.ShowOnTop(), "Entered ProductTag " + prodTag + ", but GU bench data file\n" + benchDataPath + "\ndoes not have data for Device " + productTagInt + ".\n\nCannot run " + GuModeNames[GuMode] + ".",
                                        GuModeNames[GuMode],
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                    runningGU = false;
                                }
                            }
                            else   // ProductTag didn't end with numerical serial number
                            {
                                runningGU = false;
                                MessageBox.Show("ProductTag\n        " + prodTag + "\nis not formatted correctly for GU Cal.\n\nThere are three possible formats. Examples are:\n\ndilong_gu_icc_cal_verify;1;3;4\ndilong_gu_cal_verify;1;3;4\ndilong_gu_verify;1;3;4\n\nPlease modify ProductTag in database and reload program.\n\nCannot continue " + GuModeNames[GuMode] + ".",
                                    GuModeNames[GuMode],
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }
                        }

                        sitesUserReducedList.Add(1);   // *** needs update for multisite

                        if (prodTag.ToLower().Contains("_gu_icc_corr_verify"))  // Run Icc cal, GU Cal, and Verify
                        {
                            GuMode = GuModes.IccCorrVrfy;
                            runningGUIccCal[siteNo] = ENABLE_ICC_CAL;
                            runningGU = true;
                        }
                        else if (prodTag.ToLower().Contains("_gu_corr_verify"))  // Run GU Cal, and Verify
                        {
                            GuMode = GuModes.CorrVrfy;
                            runningGUIccCal[siteNo] = false;
                            runningGU = true;
                        }
                        else if (prodTag.ToLower().Contains("_gu_verify")) // Run Verify
                        {
                            GuMode = GuModes.Vrfy;
                            runningGUIccCal[siteNo] = false;
                            runningGU = true;

                            numIgnoreLoops = 0;
                            numAvgLoops = 1;
                            numRunLoops = numIgnoreLoops + numAvgLoops;
                        }
                        else
                        {
                            MessageBox.Show("ProductTag\n        " + prodTag + "\nis not formatted correctly for GU Cal.\n\nThere are three possible formats. Examples are:\n\ndilong_gu_icc_cal_verify;1;3;4\ndilong_gu_cal_verify;1;3;4\ndilong_gu_verify;1;3;4\n\nPlease modify ProductTag in database and reload program.\n\nCannot continue " + GuModeNames[GuMode] + ".",
                            GuModeNames[GuMode],
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            runningGU = false;
                            GuMode = GuModes.None;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Engineering Mode (shows old UI)

                        frmVerification UIGuCalVrfy = new frmVerification();   // use this UI instead of test info file

                        //DialogResult selectedButton = UIGuCalVrfy.ShowDialog(wnd.ShowOnTop());   // *** wnd.ShowOnTop() was causing Clotho crash in 1.2.1!!!!!    // GetVSWindow makes Visual Studio IDE the parent, so that GU UI doesn't get lost behind other windows
                        DialogResult selectedButton = UIGuCalVrfy.ShowDialog();

                        switch (selectedButton)
                        {
                            case DialogResult.OK:     // .OK means run Icc + Corr + Verify
                                GuMode = GuModes.IccCorrVrfy;
                                runningGUIccCal[siteNo] = ENABLE_ICC_CAL;
                                runningGU = true;
                                break;
                            case DialogResult.Ignore:  // .Ignore means run Corr + Verify
                                GuMode = GuModes.CorrVrfy;
                                runningGUIccCal[siteNo] = false;
                                runningGU = true;
                                break;
                            case DialogResult.Retry:   // .Retry means run Verify
                                GuMode = GuModes.Vrfy;
                                runningGUIccCal[siteNo] = false;
                                runningGU = true;
                                numIgnoreLoops = 0;
                                numAvgLoops = 1;
                                numRunLoops = numIgnoreLoops + numAvgLoops;
                                break;
                            default:                     // Don't run any GU
                                GuMode = GuModes.None;
                                runningGUIccCal[siteNo] = false;
                                runningGU = false;
                                break;
                        }
                        #endregion
                    }
                }

                // remove the unrequested dut data from our records, this is useful for averaging requested duts
                foreach (string testName in benchTestNameList.Values)
                {
                    foreach (int dutID in dutIdAllLoose[selectedBatch])
                    {
                        if (!dutIdLooseUserReducedList.Contains(dutID))
                        {
                            finalRefDataDict[selectedBatch][testName].Remove(dutID);
                        }
                    }
                }


                // remove the gu calfactors for the sites we are going to run
                //if (GuMode != GuModes.Vrfy)
                if (runningGUIccCal[siteNo])   // need to remove Icc Cal Factors if we are running Icc Cal, since Icc Cal factors get added to LossPout
                {
                    foreach (int site in sitesAllExistingList)
                    {
                        if (sitesUserReducedList.Contains(site))
                        {
                            try { GuCalFactorsDict[site].Clear(); }
                            catch { }
                        }
                    }
                }

                //test_info_path = programLoadPath + file_prefix + "_Verification_test_info.txt";  // obsolete, test info file
                //getTestInfo_GUCalVrfy(test_info_path);   // obsolete, test info file
                //if (runningGUCalVrfy) PromptUserYesNo_GUCalVrfy(out runningGUCalVrfy, false);   // obsolete, test info file

                if (runningGU)
                {
                    ReadGuCorrelationTemplate();

                    if (runningGUIccCal[siteNo])
                    {
                        ReadIccCalfactorTemplate();
                    }

                    if (!runningGU)   // if error peviously ocurred while opening Template files (that's the only way runningGUCalVrfy would be false at this point)
                    {
                        MessageBox.Show(wnd.ShowOnTop(), GuModeNames[GuMode] + " cannot be run due to file errors.\nPlease see LogService for error details.",
                            GuModeNames[GuMode],
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        LogToLogServiceAndFile(LogLevel.Error, "*** " + GuModeNames[GuMode] + " cannot be run due to file errors, please see above for error details");
                        if (ProductionSettings.b_FlagProduction) forceReload = true;
                        return;
                    }

                    foreach (int site in sitesAllExistingList)
                    {
                        GuCorrFailed[site] = false;
                        GuVerifyFailed[site] = false;
                        GuIccCalFailed[site] = false;
                    }

                    oopsRanTheWrongSite = false;

                    currentGuSiteIndex = 0;

                    InstructOperator();

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error in DoInit_afterCustomCode\n\n" + e.ToString(), GuModeNames[GuMode]);
            }

        }

        public static void VerificationCheck(int site, string testName)  //Ivan
        {
            if (runningGUIccCal[site]) return;

            if (DicVerifyError.Count == 0)
            {
                foreach (int dutID in dutIdLooseUserReducedList)
                {
                    DicVerifyError.Add(dutID, new Dictionary<string, VerifyError>());
                }
            }

            //foreach (int dutID in dutIdLooseUserReducedList)
            //{
            //    //VERIFICATION SECTION
            //    // loop through each device
            //    // apply calfactor to measured data (becomes "corrected data")
            //    // and check against verification limits

            //    // grab dictionary values for easier debug viewing
            //    double rawMsrData = rawAllMsrDataDict[site, testName, dutID];   // use duts final loop for verification data
            //    double benchData = finalRefDataDict[selectedBatch, testName, dutID];

            //    double correctedMsrData = 0;
            //    double correctedMsrError = 0;

            //    // apply GU calfactor to data ("corrected data") and compute error ("corrected error")
            //    if (factorAddEnabledTests.Contains(testName))   // addition calfactor
            //    {
            //        correctedMsrData = rawMsrData + GuCalFactorsDict[site, testName];
            //    }
            //    else if (factorMultiplyEnabledTests.Contains(testName))   // multiplication calfactor
            //    {
            //        correctedMsrData = rawMsrData * GuCalFactorsDict[site, testName];
            //    }
            //    else
            //    {
            //        correctedMsrData = rawMsrData;  // no GU calfactor being applied
            //    }

            //    correctedMsrDataDict[site, testName, dutID] = correctedMsrData;

            //    correctedMsrError = correctedMsrData - benchData;
            //    correctedMsrErrorDict[site, testName, dutID] = correctedMsrError;

            //    // check corrected data against verification limits
            //    if (!dutIDtestedDead.Contains(dutID) & (correctedMsrError < loLimVrfyDict[testName] | correctedMsrError > hiLimVrfyDict[testName]))
            //    {
            //        DicVerifyError[dutID].Add(testName, new VerifyError(dutID, testName, loLimVrfyDict[testName], hiLimVrfyDict[testName], correctedMsrError));

            //        GuVerifyFailed[site] = true;
            //        LogToLogServiceAndFile(LogLevel.Warn, "*** Failed GU Verification limits, site " + site + ", Device " + dutID + ", test " + testNumDict[testName] + ", " + testName + ", LowL: " + loLimVrfyDict[testName] + ", measure error: " + correctedMsrError + ", HighL: " + hiLimVrfyDict[testName]);

            //        dutIDfailedVrfy.Add(dutID);
            //        testNamesFailedVrfy.Add(testName);
            //    }
            //}
        }

        public static void DoTest_beforeCustomCode(int guDutSN = -1)
        {
            try
            {
                if (!ENABLE_GUCALVERIFY_MODULE) return;

                // nag operator to reload program
                if (forceReload | (prodTag.Contains("STDUNIT") & !runningGU))  // if we've set forceReload anywhere, or any time productTag contains "STDUNIT" and we're not running GU.
                {
                    DialogResult result = DialogResult.No;
                    while (result != DialogResult.Yes)
                    {
                        result = MessageBox.Show("Cannot continue until program is reloaded!\n\nPlease click Yes, then reload the program.",   // don't show on top because PPUser only has handler cycling, so we have to manually kill Clotho each time
                            GuModeNames[GuMode],
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Hand,
                            MessageBoxDefaultButton.Button2);
                        Thread.Sleep(200);
                    }
                }

                if (runningGU)
                {
                    autoSnSequencing = guDutSN != -1;
                    currentGuDutSN = autoSnSequencing ? guDutSN : dutIdLooseUserReducedList[currentGuDutIndex];

                    if (!dutIdLooseUserReducedList.Contains(currentGuDutSN))
                    {
                        LogToLogServiceAndFile(LogLevel.Error, "ERROR: Serial number " + currentGuDutSN + " was read from device, but this is not in the list of expected serial numbers. Aborting " + GuModeNames[GuMode]);
                        MessageBox.Show("Serial number " + currentGuDutSN + " was read from device, but is not in the list of expected serial numbers.\n\nAborting " + GuModeNames[GuMode], GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Error);

                        runningGU = false;
                        forceReload = true;
                    }

                    MessageBoxAsync.Wait();

                    if (!ProductionSettings.b_Disable_MsgFlag) InstructNextUnit(); // Way Ling & KKL : To Prompt Message on next unit ID for ICC, GU and GU verify

                    List<int> active_sites = new List<int>() { 1 };    // *** need to talk to Clotho to get active sites
                    if (active_sites.Count > 1 | !active_sites.Contains(sitesUserReducedList[currentGuSiteIndex])) oopsRanTheWrongSite = true;  // Check if operator ran the wrong site
                }

                if (oopsRanTheWrongSite)
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Incorrect site selected from handler");

                    StringBuilder msg = new StringBuilder();

                    msg.Append("Incorrect site selected from handler !!!");
                    msg.Append("\nAborting " + GuModeNames[GuMode]);
                    msg.Append("\nPlease reload the program and re-run " + GuModeNames[GuMode]);

                    MessageBox.Show(wnd.ShowOnTop(), msg.ToString(),
                        GuModeNames[GuMode],
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    runningGU = false;
                    forceReload = true;
                }


                // Log the currently running Verification device ID
                if (runningGU)
                {
                    if (runningGUIccCal[siteNo])
                    {
                        LogToLogServiceAndFile(LogLevel.HighLight, "GU Device #" + currentGuDutSN + " being run on site " + sitesUserReducedList[currentGuSiteIndex] + " for Icc Calibration");
                    }
                    else
                    {
                        LogToLogServiceAndFile(LogLevel.HighLight, "GU Device #" + currentGuDutSN + " being run on site " + sitesUserReducedList[currentGuSiteIndex]);
                    }

                    if (currentGuDutIndex == 0)
                    {
                        if (runningGUIccCal[siteNo])
                        {
                            IccCalStartTime = string.Format("{0:yyyy_M_d H:m:s}", DateTime.Now);
                        }
                        else
                        {
                            CorrStartTime = string.Format("{0:yyyy_M_d H:m:s}", DateTime.Now);
                        }
                    }
                   

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error in DoTest_beforeCustomCode\n\n" + e.ToString(), GuModeNames[GuMode]);
            }
        } 



        public static void DoTest_afterCustomCode(ref ATFReturnResult res)
        {
            try
            {
                if (!runningGU | !ENABLE_GUCALVERIFY_MODULE)
                {
                    runLoop = 2 * numRunLoops;   // set runLoop to big number so looping doesn't continue
                    return;
                }
                else
                {
                    if (runLoop <= numIgnoreLoops)   // ignore the first few loops data
                    {
                        //LogToLogServiceAndFile(LogLevel.HighLight, "    GU loop " + runLoop + " completed, data ignored, pausing " + string.Format("{0:f3}", (float)runLoopDelay / 1000f) + "s for next loop");
                        LogToLogServiceAndFile(LogLevel.HighLight, "    GU loop " + runLoop + " completed, data ignored, pausing " + string.Format("{0:f3}", (double)runLoopDelay / 1000f) + "s for next loop");
                        Thread.Sleep(runLoopDelay);
                        if (GuMode != GuModes.Vrfy) res.Data.Clear();  // clear sb for next loop
                        return;
                    }

                    else if (runLoop <= numRunLoops)  // after the ignored loops have completed, begin averaging loops
                    {
                        if (runLoop < numRunLoops)
                        {
                            LogToLogServiceAndFile(LogLevel.HighLight, "    GU loop " + runLoop + " completed, data averaged, pausing " + string.Format("{0:f3}", runLoopDelay / 1000) + "s for next loop");
                            Thread.Sleep(runLoopDelay);
                        }

                        #region Grab Data
                        // store dut's measured data for later processing

                        foreach (ATFReturnPararResult param in res.Data)
                        {
                            if (!benchTestNameList.Values.Contains(param.Name))
                            {
                                //if (!missingBenchTestNames.Contains(testName))
                                //{
                                //    LogToLogServiceAndFile(LogLevel.Warn, "\nTest  " + testName + " was ran, but this test is not found in GU bench data file:\n        " + benchDataPath);
                                //    missingBenchTestNames.Add(testName);
                                //}
                                continue;   // don't record tests that aren't in the bench data file
                            }
                            if (!testedTestNameList.ContainsValue(param.Name)) testedTestNameList.Add(testNumDict[param.Name], param.Name);

                            double rawMsrData = (double)param.Vals.First();

                            //rawAllMsrDataDict[sitesUserReducedList[currentGuSiteIndex], param.Name, currentGuDutSN, runLoop] = rawMsrData;  // record all the raw data
                            //avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex], param.Name, currentGuDutSN] += rawMsrData / numAvgLoops;   // record the average data, the /numAvgLoops is where the averaging occurrs
                            rawAllMsrDataDict[sitesUserReducedList[currentGuSiteIndex], param.Name, currentGuDutSN] = rawMsrData;  // record all the raw data
                            avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex], param.Name, currentGuDutSN] += rawMsrData / numAvgLoops;   // record the average data, the /numAvgLoops is where the averaging occurrs


                            #region Flag Bad Contacts
                            // flag completely dead parts
                            //if ((testName.ToLower().Contains("lpm") | testName.ToLower().Contains("hpm"))
                            //    & (testName.ToLower().StartsWith("pout") | testName.ToLower().StartsWith("p-out") | testName.ToLower().StartsWith("p_out")))
                            if (IccCalTestNames.Pout.ContainsKey(param.Name))
                            {
                                double deadError = 0f;
                                if (runningGUIccCal[siteNo])
                                {
                                    if (finalRefDataDict[selectedBatch, param.Name, currentGuDutSN] > 10f) deadError = 5f;
                                    else deadError = 15f;
                                }
                                else
                                {
                                    deadError = 5f;
                                }

                                if (Math.Abs(rawMsrData - finalRefDataDict[selectedBatch, param.Name, currentGuDutSN]) > deadError)
                                {
                                    LogToLogServiceAndFile(LogLevel.Warn, "\nPossible contact issue: device" + currentGuDutSN + ", Measured Pout = " + rawMsrData + ", Reference Pout = " + finalRefDataDict[selectedBatch, param.Name, currentGuDutSN] + ", for test " + param.Name + "\n        So, device " + currentGuDutSN + " data will be ignored. " + (GuCorrFailed[sitesUserReducedList[currentGuSiteIndex]] ? "" : GuModeNames[GuMode] + " can still pass"));
                                    if (!dutIDtestedDead.Contains(currentGuDutSN))
                                    {
                                        dutIDtestedDead.Add(currentGuDutSN);
                                        if ((double)dutIDtestedDead.Count() >= (double)dutIdLooseUserReducedList.Count() * 0.35f)
                                        {
                                            if (runningGU)
                                            {
                                                LogToLogServiceAndFile(LogLevel.Error, "\nToo many contact issues detected, cannot pass " + GuModeNames[GuMode]);
                                                MessageBox.Show("Too many contact issues detected, cannot pass " + GuModeNames[GuMode], GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                            //runningGUCalVrfy = false;
                                            //runningGUIccCal = false;
                                            GuCorrFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                                        }
                                    }
                                }
                            }
                            #endregion

                        } // slicing through sb

                        if (GuMode != GuModes.Vrfy)
                        {
                            res.Data.Clear(); // clear result for next loop, including the very last loop because we don't want Clotho to generate a data file with old correlation factors
                            ATFResultBuilder.AddResult(ref res, benchTestNameList.Values.Last(), "", 0);  // datalog something so we don't get an error
                        }
                        #endregion
                    }  // if averaged loops

                    if (runLoop == numRunLoops)  // if last loop of the dut
                    {
                        currentGuDutIndex++;
                        if (currentGuDutIndex > dutIdLooseUserReducedList.Count - 1)  // site done, if we've tested all the duts on this site
                        {
                            currentGuDutIndex = 0;  // reset dut index for next site

                            // compute calfactors & errors for each test and check against calibration & verification limits for the site we just ran
                            foreach (string testName in testedTestNameList.Values)
                            {
                                double measuredAvg = 0;
                                double benchAvg = 0;
                                double factor = 0; // initialize to 0 in case there is no calfactor being used for this test               

                                if (GuMode != GuModes.Vrfy)
                                {
                                    #region Calculate GU Correlation Factors

                                    // filter out dead parts
                                    if (dutIDtestedDead.Count() > 0 && dutIDtestedDead.Count < avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Count)
                                    {
                                        measuredAvg = avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Where(x => !dutIDtestedDead.Contains(x.Key)).ToDictionary(pair => pair.Key, pair => pair.Value).Values.Average();   // average of all the duts average measurement  (two levels of averaging)
                                        benchAvg = finalRefDataDict[selectedBatch][testName].Where(x => !dutIDtestedDead.Contains(x.Key)).ToDictionary(pair => pair.Key, pair => pair.Value).Values.Average();    //  average of all the duts bench measurement
                                    }
                                    else
                                    {
                                        measuredAvg = avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Values.Average();   // average of all the duts average measurement  (two levels of averaging)
                                        benchAvg = finalRefDataDict[selectedBatch][testName].Values.Average();    //  average of all the duts bench measurement
                                    }

                                    double loLimCal = 0;
                                    double hiLimCal = 0;

                                    if (runningGUIccCal[siteNo])   // Icc Cal limits
                                    {
                                        if (IccCalTestNames.Pout.ContainsKey(testName))
                                        {
                                            double PinMeasuredAvg = avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][IccCalTestNames.Pout[testName].PinTestName].Values.Average();   // average of all the duts average measurement  (two levels of averaging)
                                            double PinBenchAvg = finalRefDataDict[selectedBatch][IccCalTestNames.Pout[testName].PinTestName].Values.Average();    //  average of all the duts bench measurement

                                            double pinLossFactor = PinMeasuredAvg - PinBenchAvg;
                                            double poutLossFactor = benchAvg - measuredAvg;

                                            IccCalFactorsTempDict[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Pout[testName].KeyName + IccCalLoss.OutputLoss] = poutLossFactor;  // store the Icc Calfactor to dictionary
                                            IccCalFactorsTempDict[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Pout[testName].KeyName + IccCalLoss.InputLoss] = pinLossFactor;  // store the Icc Calfactor to dictionary

                                            //IccServoNewVSGlevel[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Pout[testName].KeyName] = PinMeasuredAvg;  // VSG reference levels for VST
                                            IccServoNewVSGlevel[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Pout[testName].KeyName] = PinMeasuredAvg - pinLossFactor;  // VSG reference levels for VST
 
                                            if (factorAddEnabledTests.Contains(IccCalTestNames.Pout[testName].KeyName))
                                            {
                                                loLimCal = loLimCalAddDict[IccCalTestNames.Pout[testName].KeyName];
                                                hiLimCal = hiLimCalAddDict[IccCalTestNames.Pout[testName].KeyName];
                                            }
                                            else   // if no previous Icc Calfactor file, or new test
                                            {
                                                loLimCal = -999;
                                                hiLimCal = 999;
                                            }

                                            // check for limits failure
                                            if (poutLossFactor < loLimCal | poutLossFactor > hiLimCal)
                                            {
                                                GuIccCalFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                                                LogToLogServiceAndFile(LogLevel.Warn, "*** Failed GU Icc Cal-factor limits, site " + sitesUserReducedList[currentGuSiteIndex] + ", test " + testNumDict[testName] + ", " + IccCalTestNames.Pout[testName].KeyName + IccCalLoss.OutputLoss + ", LowL: " + loLimCal + ", calfactor: " + poutLossFactor + ", HighL: " + hiLimCal);
                                            }
                                            if (pinLossFactor < loLimCal | pinLossFactor > hiLimCal)
                                            {
                                                GuIccCalFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                                                LogToLogServiceAndFile(LogLevel.Warn, "*** Failed GU Icc Cal-factor limits, site " + sitesUserReducedList[currentGuSiteIndex] + ", test " + testNumDict[testName] + ", " + IccCalTestNames.Pout[testName].KeyName + IccCalLoss.InputLoss + ", LowL: " + loLimCal + ", calfactor: " + pinLossFactor + ", HighL: " + hiLimCal);
                                            }

                                        }

                                    }
                                    else   // Correlation limits
                                    {
                                        if (factorAddEnabledTests.Contains(testName))
                                        {
                                            factor = benchAvg - measuredAvg;
                                            loLimCal = loLimCalAddDict[testName];
                                            hiLimCal = hiLimCalAddDict[testName];
                                        }
                                        else if (factorMultiplyEnabledTests.Contains(testName))   // multiplication calfactor
                                        {
                                            factor = benchAvg / measuredAvg;
                                            loLimCal = loLimCalMultiplyDict[testName];
                                            hiLimCal = hiLimCalMultiplyDict[testName];
                                        }

                                        GuCalFactorsDict[sitesUserReducedList[currentGuSiteIndex], testName] = factor;  // store the GU corr-factor to dictionary

                                        // check for limits failure
                                        if (factor < loLimCal | factor > hiLimCal)
                                        {
                                            GuCorrFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                                            if (!DicCorrError.ContainsKey(testName)) DicCorrError.Add(testName, new CorrError(0, testName, loLimCal, hiLimCal, factor));
                                            LogToLogServiceAndFile(LogLevel.Warn, "*** Failed GU Corr-factor limits, site " + sitesUserReducedList[currentGuSiteIndex] + ", test " + testNumDict[testName] + ", " + testName + ", LowL: " + loLimCal + ", calfactor: " + factor + ", HighL: " + hiLimCal);
                                        }

                                    }


                                    #endregion
                                }
                                else
                                {
                                    ATFCrossDomainWrapper.Correlation_ApplyCorFactorToParameter(testName, ref factor);
                                }

                                double loLimVrfy = loLimVrfyDict[testName];
                                double hiLimVrfy = hiLimVrfyDict[testName];

                                if (testName.Contains("M_OTP_MODULE_ID"))
                                {
                                    loLimVrfy = -1000000000000;
                                    hiLimVrfy = 1000000000000;
                                }

                                if (!runningGUIccCal[siteNo])
                                {
                                    #region Verification Check

                                    foreach (int dutID in dutIdLooseUserReducedList)
                                    {
                                        //VERIFICATION SECTION
                                        // loop through each device
                                        // apply calfactor to measured data (becomes "corrected data")
                                        // and check against verification limits

                                        // grab dictionary values for easier debug viewing
                                        //float rawMsrData = rawAllMsrDataDict[sitesUserReducedList[currentGuSiteIndex], testName, dutID, numRunLoops];   // use duts final loop for verification data
                                        //float benchData = finalRefDataDict[selectedBatch, testName, dutID];

                                        double rawMsrData = rawAllMsrDataDict[sitesUserReducedList[currentGuSiteIndex], testName, dutID];
                                        double benchData = finalRefDataDict[selectedBatch, testName, dutID];

                                        double correctedMsrData = 0;
                                        double correctedMsrError = 0;


                                        //float correctedMsrData = 0;
                                        //float correctedMsrError = 0;

                                        // apply GU calfactor to data ("corrected data") and compute error ("corrected error")
                                        if (factorAddEnabledTests.Contains(testName))   // addition calfactor
                                        {
                                            correctedMsrData = rawMsrData + factor;
                                        }
                                        else if (factorMultiplyEnabledTests.Contains(testName))   // multiplication calfactor
                                        {
                                            correctedMsrData = rawMsrData * factor;
                                        }
                                        else
                                        {
                                            correctedMsrData = rawMsrData;  // no GU calfactor being applied
                                        }

                                        correctedMsrDataDict[sitesUserReducedList[currentGuSiteIndex], testName, dutID] = correctedMsrData;

                                        correctedMsrError = correctedMsrData - benchData;
                                        correctedMsrErrorDict[sitesUserReducedList[currentGuSiteIndex], testName, dutID] = correctedMsrError;

                                        // check corrected data against verification limits
                                        if (!dutIDtestedDead.Contains(dutID) & (correctedMsrError < loLimVrfy | correctedMsrError > hiLimVrfy))
                                        {
                                            GuVerifyFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                                            LogToLogServiceAndFile(LogLevel.Warn, "*** Failed GU Verification limits, site " + sitesUserReducedList[currentGuSiteIndex] + ", Device " + dutID + ", test " + testNumDict[testName] + ", " + testName + ", LowL: " + loLimVrfy + ", measure error: " + correctedMsrError + ", HighL: " + hiLimVrfy);

                                            dutIDfailedVrfy.Add(dutID);
                                            testNamesFailedVrfy.Add(testName);
                                        }
                                    }
                                    VerificationCheck(siteNo, testName);
                                    #endregion

                                    #region Correlation Coefficient Calculation

                                    if (dutIDtestedDead.Count() == 0)
                                    {
                                        double[] benchArray = finalRefDataDict[selectedBatch][testName].Values.ToArray();
                                        double[] measArray = correctedMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Values.ToArray();
                                        corrCoeffDict[sitesUserReducedList[currentGuSiteIndex], testName] = pearsoncorr2(benchArray, measArray, benchArray.Length);
                                    }
                                    else if (dutIDtestedDead.Count < avgMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Count)
                                    {
                                        double[] benchArray = finalRefDataDict[selectedBatch][testName].Where(x => !dutIDtestedDead.Contains(x.Key)).ToDictionary(pair => pair.Key, pair => pair.Value).Values.ToArray();
                                        double[] measArray = correctedMsrDataDict[sitesUserReducedList[currentGuSiteIndex]][testName].Where(x => !dutIDtestedDead.Contains(x.Key)).ToDictionary(pair => pair.Key, pair => pair.Value).Values.ToArray();
                                        corrCoeffDict[sitesUserReducedList[currentGuSiteIndex], testName] = pearsoncorr2(benchArray, measArray, benchArray.Length);
                                    }
                                    else
                                    {
                                        corrCoeffDict[sitesUserReducedList[currentGuSiteIndex], testName] = 0;
                                    }

                                    #endregion

                                    #region Icc Cal - average error & servo target compensation

                                    if (GuMode == GuModes.IccCorrVrfy && IccCalTestNames.Icc.Keys.Contains(testName))
                                    {
                                        double averageIccError = measuredAvg - benchAvg;  // this is raw error, not corrected error

                                        double limitFractional = 0.01f;  // 1%
                                        IccCalAvgErrorDict[sitesUserReducedList[currentGuSiteIndex], currentGUattemptNumber, testName] = new IccCalError(averageIccError, -benchAvg * limitFractional, benchAvg * limitFractional);
                                        double fractionalError = Math.Abs(averageIccError / benchAvg);
                                        if (fractionalError > limitFractional)
                                        {
                                            iccCalInaccuracyDetected = true;
                                            LogToLogServiceAndFile(LogLevel.Warn, "*** Icc average error > " + (limitFractional * 100f) + "%, may need rerun Icc Cal to improve accuracy. Test " + testName + ", average error: " + averageIccError + "A = " + (fractionalError * 100f) + "%");
                                        }

                                        if (ApplyIccServoTargetCorrection[IccCalTestNames.Icc[testName].KeyName])
                                            IccServoNewTargetCorrection[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Icc[testName].KeyName] = IccServoTargetCorrection[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Icc[testName].KeyName] - averageIccError * 0.8f;
                                        else
                                            IccServoNewTargetCorrection[sitesUserReducedList[currentGuSiteIndex], IccCalTestNames.Icc[testName].KeyName] = 0;

                                    }

                                    #endregion

                                }

                            }  // foreach testName, check GU Calfactor and Verification

                            #region Print Site Summary
                            if (false & !runningGUIccCal[siteNo])   // don't print site summary, we're not multisite yet
                            {
                                StringBuilder msg = new StringBuilder();

                                // print out the summary so far
                                msg.Append("\r\n-------------------------------------------------------------");
                                msg.Append("\r\n" + GuModeNames[GuMode] + " FINISHED FOR SITE " + sitesUserReducedList[currentGuSiteIndex]);
                                msg.Append("\r\n-------------------------------------------------------------");
                                msg.Append("\r\n----        Summary by site:");

                                foreach (int site in sitesUserReducedList)
                                {
                                    if (site > sitesUserReducedList[currentGuSiteIndex]) break;

                                    if (GuMode == GuModes.IccCorrVrfy)
                                    {
                                        if (GuIccCalFailed.ContainsValue(true)) msg.Append("\r\n****  GU Icc Calibration FAILED on SITE " + site);
                                        else msg.Append("\r\n----        GU Icc Calibration PASSED on SITE " + site);
                                    }
                                    if (GuMode != GuModes.Vrfy)
                                    {
                                        if (GuCorrFailed[site]) msg.Append("\r\n****  GU Correlation FAILED on SITE " + site);
                                        else msg.Append("\r\n----        GU Correlation PASSED on SITE " + site);
                                    }
                                    if (GuVerifyFailed[site]) msg.Append("\r\n****  GU Verification FAILED on SITE " + site);
                                    else msg.Append("\r\n----        GU Verification PASSED on SITE " + site);
                                }
                                msg.Append("\r\n-------------------------------------------------------------");

                                LogToLogServiceAndFile(LogLevel.HighLight, msg.ToString());
                            }  // if !runningGUIccCal[siteNo], print summary by site
                            #endregion

                            currentGuSiteIndex++;
                            if (currentGuSiteIndex >= sitesUserReducedList.Count)   // Procedure is done, if we've tested all sites
                            {
                                if (runningGUIccCal[siteNo])  // Icc cal is done, now run Corr + Vrfy
                                {
                                    #region Finished Icc Cal, now GU Cal
                                    IccCalFinishTime = string.Format("{0:yyyy_M_d H:m:s}", DateTime.Now);
                                    runningGUIccCal[siteNo] = false;
                                    GuCalFactorsDict = new Dictionary.DoubleKey<int, string, double>(IccCalFactorsTempDict);   // going into the actual GU Cal, we'll keep the Icc Calfactors only.
                                    if (!GuCorrFailed.ContainsValue(true) && !GuIccCalFailed.ContainsValue(true))
                                    {
                                        IccServoVSGlevel = new Dictionary.DoubleKey<int, string, double>(IccServoNewVSGlevel);
                                    }

                                    //rawIccCalMsrDataDict = new Dictionary.QuadKey<int, string, int, int, double>(rawAllMsrDataDict);
                                    rawIccCalMsrDataDict[siteNo] = new Dictionary.DoubleKey<string, int, double>(rawAllMsrDataDict[siteNo]);
                                    // clear previous measurements
                                    avgMsrDataDict.Clear();
                                    rawAllMsrDataDict.Clear();
                                    currentGuSiteIndex = 0;

                                    InstructOperator();
                                    #endregion
                                }
                                else  // everything is done
                                {
                                    #region Finished Entire Procedure

                                    DateTime datetime = DateTime.Now;

                                    CorrFinishTimeHumanFriendly = string.Format("{0:yyyy-MM-dd_HH.mm.ss}", datetime);
                                    CorrFinishTime = string.Format("{0:yyyy_M_d H:m:s}", datetime);

                                    // KKL : To update the Cal Status to DB
                                    cDatabase.Update_Database(prodTag, !GU.GuIccCalFailed.ContainsValue(true), !GU.GuCorrFailed.ContainsValue(true), !GU.GuVerifyFailed.ContainsValue(true));

                                    runningGU = false;
                                    
                                    StringBuilder msgFinal = new StringBuilder();

                                    msgFinal.Append("\r\n\r\n-------------------------------------------------------------");
                                    msgFinal.Append("\r\n----  " + GuModeNames[GuMode] + " COMPLETE");

                                    if (GuMode == GuModes.IccCorrVrfy)
                                    {
                                        if (GuIccCalFailed.ContainsValue(true)) msgFinal.Append("\r\n****  Final result:  GU Icc Calibration FAILED");
                                        else msgFinal.Append("\r\n----  Final result:  GU Icc Calibration PASSED");
                                    }
                                    if (GuMode != GuModes.Vrfy)
                                    {
                                        if (GuCorrFailed.ContainsValue(true)) msgFinal.Append("\r\n****  Final result:  GU Correlation FAILED");
                                        else msgFinal.Append("\r\n----  Final result:  GU Correlation PASSED");
                                    }
                                    if (GuVerifyFailed.ContainsValue(true)) msgFinal.Append("\r\n****  Final result:  GU Verification FAILED");
                                    else msgFinal.Append("\r\n----  Final result:  GU Verification PASSED");

                                    msgFinal.Append("\r\n-------------------------------------------------------------");
                                    LogToLogServiceAndFile(LogLevel.HighLight, msgFinal.ToString());

                                    DataAnalysis.DataAnalysisFiles.WriteAll();  // write the custom data files

                                    if (GuIccCalFailed.ContainsValue(true) | GuCorrFailed.ContainsValue(true) | GuVerifyFailed.ContainsValue(true))
                                    {
                                        if (GuMode == GuModes.IccCorrVrfy & !previousIccCalFactorsExist & !IccCalTemplateExists) WriteIccCalfactorFile();  // create Icc Calfactor file from scratch if no file/template exists. This helps developers get going faster.

                                        #region Prompt for Rerun

                                        //runningGU = DialogResult.Yes == MessageBox.Show(wnd.ShowOnTop(), GuModeNames[GuMode] + " FAILED.\nWould you like to re-run " + GuModeNames[GuMode] + "?", GuModeNames[GuMode], MessageBoxButtons.YesNo);

                                        if (GuMode == GuModes.IccCorrVrfy && !IccCalTemplateExists)
                                        {
                                            MessageBoxAsync.Show(GuModeNames[GuMode] + " dummy run completed.\n\nA default Icc Cal file has been created at:\n\n" + iccCalFilePath + "\n\nPlease use this default Icc Cal file to create an Icc Cal Template file,\nthen reload the program and rerun " + GuModeNames[GuMode], GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                            runningGU = false;
                                        }
                                        else if (GuMode == GuModes.IccCorrVrfy && iccCalInaccuracyDetected)
                                        {
                                            if (++iccCalNumInaccurateAttempts >= 3)
                                            {
                                                MessageBoxAsync.Show(GuModeNames[GuMode] + " FAILED.\n\nIcc Cal has detected a system Icc repeatablity issue.\nPlease contact engineer for debugging.", GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                                                runningGU = false;
                                            }
                                            else
                                            {
                                                MessageBoxAsync.Show(GuModeNames[GuMode] + " FAILED.\n\nIcc Cal needs to be rerun to improve accuracy.\nPlease click OK to rerun Icc Cal immediately without unloading the program.", GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
                                                runningGU = true;   // Always assume rerun. This encourages operator not to unload the program. Also, never allow continue test after fail.
                                            }
                                        }
                                        else
                                        {
                                            MessageBoxAsync.Show(GuModeNames[GuMode] + " FAILED.\nYou may rerun GU Cal immediately by clicking OK.", GuModeNames[GuMode], MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
                                            runningGU = true;   // Always assume rerun. This encourages operator not to unload the program. Also, never allow continue test after fail.
                                        }

                                        if (runningGU)   // if rerun
                                        {
                                            currentGUattemptNumber++;

                                            GuCorrFailed[1] = false;
                                            GuVerifyFailed[1] = false;
                                            GuIccCalFailed[1] = false;
                                            runningGUIccCal[siteNo] = ENABLE_ICC_CAL & GuMode == GuModes.IccCorrVrfy;

                                            //clear previous measurements
                                            rawAllMsrDataDict.Clear();
                                            avgMsrDataDict.Clear();
                                            correctedMsrDataDict.Clear();
                                            correctedMsrErrorDict.Clear();
                                            dutIDtestedDead.Clear();
                                            iccCalInaccuracyDetected = false;
                                            testNamesFailedVrfy.Clear();
                                            if (GuMode == GuModes.IccCorrVrfy) GuCalFactorsDict.Clear();
                                            IccCalFactorsTempDict.Clear();
                                            // In case Icc servo needs to learn Icc target compensation
                                            IccServoTargetCorrection = new Dictionary.DoubleKey<int, string, double>(IccServoNewTargetCorrection);
                                            IccServoNewTargetCorrection.Clear();
                                            IccServoNewVSGlevel.Clear();
                                            corrCoeffDict.Clear();
                                            oopsRanTheWrongSite = false;
                                            currentGuSiteIndex = 0;

                                            InstructOperator();
                                        }
                                        else
                                        {
                                            forceReload = true;
                                        }
                                        #endregion
                                    }
                                    else  // everything passed, write Correlation File, conclude GU Cal, demand program reload
                                    {
                                        #region Everything Passed
                                        if (GuMode == GuModes.Vrfy)
                                        {
                                            // message box doesn't stop execution, won't hang the handler
                                            MessageBoxAsync.Show(GuModeNames[GuMode] + " PASSED.\n\nMay continue testing without reload.",
                                                GuModeNames[GuMode],
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information,
                                                MessageBoxDefaultButton.Button1);
                                        }
                                        else
                                        {
                                            WriteGuCorrelationFile();  // write a new correlation file
                                            if (GuMode == GuModes.IccCorrVrfy)
                                            {
                                                WriteIccCalfactorFile();  // write a new Icc calfactor file
                                            }

                                            forceReload = true;

                                            if (GuMode == GuModes.IccCorrVrfy)
                                            {
                                                // message box doesn't stop execution, won't hang the handler
                                                MessageBoxAsync.Show(GuModeNames[GuMode] + " PASSED.\nMust now reload program.\n\nPlease click OK, then reload the program.\n\n" +
                                                "Correlation Factor File updated:\n" + correlationFilePath + "\n\n" +
                                                "Icc CalFactor File updated:\n" + iccCalFilePath,
                                                    GuModeNames[GuMode],
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Information,
                                                    MessageBoxDefaultButton.Button1);
                                            }
                                            else
                                            {
                                                // message box doesn't stop execution, won't hang the handler
                                                MessageBoxAsync.Show(GuModeNames[GuMode] + " PASSED.\nMust now reload program.\n\nPlease click OK, then reload the program.\n\n" +
                                                "Correlation Factor File updated:\n" + correlationFilePath,
                                                    GuModeNames[GuMode],
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Information,
                                                    MessageBoxDefaultButton.Button1);
                                            }
                                        }

                                        #endregion
                                    }


                                    #endregion
                                }  // if running Icc Cal

                            }  // if all sites done

                            else  // if not all sites done
                            {
                                InstructOperator();
                            }  // if not all sites done

                        } // if all duts done

                        //LogToLogServiceAndFile(LogLevel.Warn, "NOTICE: Please ignore the warning \"Empty return from Test Plan Run\" during " + GuModeNames[GuMode] + ".");

                    }  // if all loops done for individual dut

                } // if running_verification    
            }
            catch (Exception e)
            {
                MessageBox.Show("Error during DoTest_afterCustomCode\n\n" + e.ToString(), GuModeNames[GuMode]);
            }
        }


        // Coded by wayling Added by KKL to indicate the Next Unit 
        private static void InstructNextUnit()
        {
            if (!runningGU || autoSnSequencing) return;

            string msgTitle = runningGUIccCal[siteNo] ? "Next GU Unit - GU Verification" : "Next GU Unit";


            StringBuilder msg = new StringBuilder();
            msg.Append("      Please prepare the next GU Device #s " + dutIdLooseUserReducedList[currentGuDutIndex]);
            msg.Append("\n      remain using SITE " + sitesUserReducedList[currentGuSiteIndex]);

            MessageBox.Show(wnd.ShowOnTop(), msg.ToString(), msgTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            LogToLogServiceAndFile(LogLevel.HighLight, msg.ToString());

        }


        public static void SimulateTesting(ref ATFReturnResult res)
        {   // this method is not necessary. I just use it during simulation to call AddResult with simulated values.

            if (!runningGU)  return;

            double measureAveragePercent = 95f;   // affects whether we pass/fail GU Cal (during simulation), 100 = same average as bench data
            double measureRangePercent = 2f;   // affects whether we pass/fail GU Verify (during simulation), 0 = perfect repeatability

            //foreach (string testName in benchTestNameList)
            for (int i = 0; i < benchTestNameList.Count-4; i++)
            {
                string testName = benchTestNameList[i];

                double outputCalfactor = getIccCalfactor(testName, GU.IccCalLoss.OutputLoss);

                Thread.Sleep(1);   // this is necessary to ensure we generate a new random number each time, probably based on the clock cycle

                Random rand = new Random();
                double randPercent = measureAveragePercent/100f + rand.Next((int)(-measureRangePercent * 1e6), (int)(measureRangePercent * 1e6)) / 100e6f;   // random data within +-errorPercent

                double result = randPercent * finalRefDataDict[selectedBatch, testName, dutIdLooseUserReducedList[currentGuDutIndex]];
                ATFResultBuilder.AddResult(ref res, testName, "", result);
            }

        }


        private static void ReadExcelTCF()
        {
            // Get GuBenchDataFile_Path from Excel test condition file

            string basePath = GetTestPlanPath();

            bool continueSheets = true;
            int sheet = 0;

            // search through Excel test condition file for GuBenchDataFile_Path
            while (continueSheets & sheet < 50)
            {
                try
                {
                    string temp = ATFCrossDomainWrapper.Excel_Get_Input(++sheet, 1, 1);
                }
                catch
                {
                    break;   // stop searching if sheet doesn't exist
                }

                int numRows = 100;
                int numCols = 2;

                Tuple<bool, string, string[,]> sheetContents = ATFCrossDomainWrapper.Excel_Get_IputRange(sheet, 1, 1, numRows, numCols);

                bool continueRows = true;
                int row = -1;

                while (continueRows & row < numRows - 1)
                {
                    string cellValue = sheetContents.Item3[++row, 0];  // row and column appear to be safe from exceptions, but sheet can throw exception if doesn't exist

                    switch (cellValue.Replace("_", "").ToLower())
                    {
                        case "gubenchdatafilepath":
                            benchDataPath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "gucorrtemplatepath":
                            correlationTemplatePath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "guicccaltemplatepath":
                            iccCalTemplatePath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "#end":
                            continueRows = false;
                            break;
                    }
                }

                if (benchDataPath != "" & correlationTemplatePath != "" & iccCalTemplatePath != "") break;

            }

            if (correlationTemplatePath == "")
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: GuCorrTemplate_Path not found in TCF Main Sheet.\n        Cannot run GU Cal.");
                runningGU = false;
            }
            else if (!File.Exists(correlationTemplatePath))
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: No GU Correlation Template found at:\n" + correlationTemplatePath + "\n        Cannot run GU Cal.");
                runningGU = false;
            }

            IccCalTemplateExists = File.Exists(iccCalTemplatePath);

        }

        private static void ReadExcelTCF(string sheetName)
        {
            // Get GuBenchDataFile_Path from Excel test condition file

            string basePath = GetTestPlanPath();

            bool continueSheets = true;

            // search through Excel test condition file for GuBenchDataFile_Path
            while (continueSheets)
            {
                try
                {
                    string temp = ATFCrossDomainWrapper.Excel_Get_Input(sheetName, 1, 1);
                }
                catch
                {
                    break;   // stop searching if sheet doesn't exist
                }

                int numRows = 100;
                int numCols = 2;

                Tuple<bool, string, string[,]> sheetContents = ATFCrossDomainWrapper.Excel_Get_IputRange(sheetName, 1, 1, numRows, numCols);

                bool continueRows = true;
                int row = -1;

                while (continueRows & row < numRows - 1)
                {
                    string cellValue = sheetContents.Item3[++row, 0];  // row and column appear to be safe from exceptions, but sheet can throw exception if doesn't exist

                    switch (cellValue.Replace("_", "").ToLower())
                    {
                        case "gubenchdatafilepath":
                            benchDataPath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "gucorrtemplatepath":
                            correlationTemplatePath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "guicccaltemplatepath":
                            iccCalTemplatePath = basePath + sheetContents.Item3[row, 1];
                            break;
                        case "#end":
                            continueRows = false;
                            break;
                    }
                }

                if (benchDataPath != "" & correlationTemplatePath != "" & iccCalTemplatePath != "") break;

            }

            if (correlationTemplatePath == "")
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: GuCorrTemplate_Path not found in TCF Main Sheet.\n        Cannot run GU Cal.");
                runningGU = false;
            }
            else if (!File.Exists(correlationTemplatePath))
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: No GU Correlation Template found at:\n" + correlationTemplatePath + "\n        Cannot run GU Cal.");
                runningGU = false;
            }

            IccCalTemplateExists = File.Exists(iccCalTemplatePath);

        }

        private static void ReadBenchData()
        {
            const int START_COL = 11;   // this first column in the csv file which is a test parameter
            int batchColumn = 0;  // Identify the Package Column {Set "DIE_X" as Package for Production)

            List<string> benchNamesTemp = new List<string>();

            //Dictionary.QuadKey<int, string, int, UnitType, List<float>> allData = new Dictionary.QuadKey<int, string, int, UnitType, List<float>>();    // [batchID, testName, dut#, loose/demo, list of measurements]
            Dictionary.QuadKey<int, string, int, UnitType, List<double>> allData = new Dictionary.QuadKey<int, string, int, UnitType, List<double>>();    // [batchID, testName, dut#, loose/demo, list of measurements]
            dutIdAllLoose = new Dictionary<int, List<int>>();
            dutIdAllDemo = new Dictionary<int, List<int>>();
            //keng shan added to clear all dic in order to enable reload without exit clotho
            testNumDict.Clear();
            benchTestNameList.Clear();
            unitsDict.Clear();
            unitsAreDb.Clear();
            hiLimVrfyDict.Clear();

            if (benchDataPath == "")
            {
                runningGU = false;
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: GuBenchDataFile_Path not found in Excel Test Condition File. Cannot run " + GuModeNames[GuMode] + ".");
                return;
            }
            else if (!File.Exists(benchDataPath))
            {
                runningGU = false;
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Bench Data File doesn't exist: " + benchDataPath + ". Cannot run " + GuModeNames[GuMode] + ".");
                return;
            }

            bool TolHiVrfyFound = false;
            bool TolLoVrfyFound = false;

            using (StreamReader br = new StreamReader(new FileStream(benchDataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                while (!br.EndOfStream)
                {
                    string[] csvLine = br.ReadLine().Split(',');

                    switch (csvLine[0].ToLower())
                    {
                        case "parameter":
                            //benchNamesTemp = csvLine.Skip(START_COL - 1).TakeWhile(testName => testName != "PassFail").ToList();    // uses Linq methods Skip, TakeWhile, and ToList
                            benchNamesTemp = csvLine.Skip(START_COL - 1).ToList();
                            batchColumn = Array.IndexOf(csvLine, "SBIN");
                            HashSet<string> tempNames = new HashSet<string>();
                            foreach (string name in benchNamesTemp.ToList())  // check for duplicate names
                            {
                                if (!tempNames.Add(name))
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Duplicate test: " + name + "\r\n    in " + benchDataPath + "\r\n    Cannot run " + GuModeNames[GuMode]);
                                    benchNamesTemp.Remove(name);
                                    runningGU = false;
                                }
                            }
                            continue;
                        case "tests#":
                        case "test#":
                            //int[] testNumArray = Array.ConvertAll(csvLine.Skip(START_COL).ToArray(), x => int.Parse(x));   // obsolete, I just left here as a syntax reference for converting string array to int array
                            //int[] testNumArray = csvLine.Skip(START_COL - 1).Select(x => int.Parse(x)).ToArray();    // obsolete, I just left here as a syntax reference for converting string array to int array
                            for (int i = 0; i < benchNamesTemp.Count; i++)
                            {
                                try
                                {
                                    int testNum = Convert.ToInt16(csvLine[START_COL - 1 + i]);
                                    testNumDict.Add(benchNamesTemp[i], testNum);
                                    benchTestNameList.Add(testNum, benchNamesTemp[i]);
                                }
                                catch
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Test# format error, test " + benchNamesTemp[i] + "\r\n    in " + benchDataPath + "\r\nCannot run " + GuModeNames[GuMode]);
                                    runningGU = false;
                                }
                            }
                            continue;
                        case "unit":
                        case "units":
                            for (int i = 0; i < benchNamesTemp.Count; i++)
                            {
                                try
                                {
                                    unitsDict.Add(benchNamesTemp[i], (csvLine[START_COL - 1 + i]));   // will generate exception if Unit row length isn't as long as Parameter row length
                                    unitsAreDb.Add(benchNamesTemp[i], unitsDict[benchNamesTemp[i]].ToLower().Contains("db"));
                                }
                                catch   // allow no units, no error thrown
                                {
                                    unitsDict.Add(benchNamesTemp[i], "");
                                }
                                if (unitsDict[benchNamesTemp[i]] == "")
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Units row must be filled out in " + benchDataPath + ", Cannot continue " + GuModeNames[GuMode]);
                                    runningGU = false;
                                }
                                //scale_ary[i] = GetScale(unitsDict[benchNamesTemp[i]]);  // Clotho does not scale for units so neither does this GU module
                            }
                            continue;
                        case "highl":
                            TolHiVrfyFound = true;
                            for (int i = 0; i < benchNamesTemp.Count; i++)
                            {
                                try
                                {
                                    if (csvLine[i + START_COL - 1].ToLower() == "auto")
                                    {
                                        hiLimVrfyDict[benchNamesTemp[i]] = SetAutoVeriLimit(benchNamesTemp[i]);
                                    }
                                    else if (csvLine[i + START_COL - 1] != "")
                                    {
                                        hiLimVrfyDict[benchNamesTemp[i]] = Convert.ToSingle(csvLine[i + START_COL - 1]);
                                    }
                                    else
                                    {
                                        hiLimVrfyDict[benchNamesTemp[i]] = 9999999f;   // Allow blanks, no error thrown
                                    }
                                }
                                catch  // if bad format, throw error message and stop " + GuModeNames[GuMode] + "
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: HighL format error in file " + benchDataPath + ", test " + benchNamesTemp[i] + ", Cannot continue " + GuModeNames[GuMode]);
                                    runningGU = false;
                                }
                            }
                            continue;
                        case "lowl":
                            TolLoVrfyFound = true;
                            for (int i = 0; i < benchNamesTemp.Count; i++)
                            {
                                try
                                {
                                    if (csvLine[i + START_COL - 1].ToLower() == "auto")
                                    {
                                        loLimVrfyDict[benchNamesTemp[i]] = -SetAutoVeriLimit(benchNamesTemp[i]);
                                    }

                                    else if (csvLine[i + START_COL - 1] != "")
                                    {
                                        loLimVrfyDict[benchNamesTemp[i]] = Convert.ToSingle(csvLine[i + START_COL - 1]);
                                    }
                                    else
                                    {
                                        loLimVrfyDict[benchNamesTemp[i]] = -9999999f;   // Allow blanks, no error thrown
                                    }
                                }
                                catch  // if bad format, throw error message and stop " + GuModeNames[GuMode] + "
                                {
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: LowL format error in file " + benchDataPath + ", test " + benchNamesTemp[i] + ", Cannot continue " + GuModeNames[GuMode]);
                                    runningGU = false;
                                }
                            }
                            continue;
                    }

                    int dut_id_int;

                    if (TolHiVrfyFound & TolLoVrfyFound)
                    {
                        if (ExtractDutID(csvLine[0], out dut_id_int))
                        {
                            int batch = -1;
                            try
                            {
                                batch = int.Parse(csvLine[batchColumn]);
                                if (batch < 1) throw new Exception();   // Do not allow 0, since the earlier practice was to assign batch=0 to demo units. That would now cause the demo data to be ignored.
                            }
                            catch
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: GU Batch number required in column 2 of GU Ref Data file:\n        "
                                    + benchDataPath
                                    + "\n        Valid Batch number is an integer > 0."
                                    + "\n        Cannot continue " + GuModeNames[GuMode]);
                                
                                runningGU = false;
                            }

                            if (!dutIdAllLoose.ContainsKey(batch)) dutIdAllLoose.Add(batch, new List<int>());
                            if (!dutIdAllDemo.ContainsKey(batch)) dutIdAllDemo.Add(batch, new List<int>());
                            
                            UnitType looseOrSolder = UnitType.Loose;

                            for (int col = 0; col < START_COL; col++)
                            {
                                if (csvLine[col].ToLower().Contains("demo"))
                                {
                                    looseOrSolder = UnitType.Demo;
                                    break;
                                }
                            }

                            if (looseOrSolder == UnitType.Loose && !dutIdAllLoose[batch].Contains(dut_id_int)) dutIdAllLoose[batch].Add(dut_id_int);
                            else if (looseOrSolder == UnitType.Demo && !dutIdAllDemo[batch].Contains(dut_id_int)) dutIdAllDemo[batch].Add(dut_id_int);

                            for (int i = 0; i < benchNamesTemp.Count; i++)
                            {
                                try
                                {
                                    double dataVal = csvLine[i + START_COL - 1] == "" ? 0 : Convert.ToSingle(csvLine[i + START_COL - 1]);

                                    if (allData[batch, benchNamesTemp[i], dut_id_int, looseOrSolder] == default(List<double>)) allData[batch, benchNamesTemp[i], dut_id_int, looseOrSolder] = new List<double>();
                                    allData[batch, benchNamesTemp[i], dut_id_int, looseOrSolder].Add(dataVal);
                                }
                                catch  // if data is blank or formatted improperly, throw error and stop " + GuModeNames[GuMode] + "
                                {
                                    finalRefDataDict[selectedBatch, benchNamesTemp[i], dut_id_int] = -1;
                                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Data format error in file " + benchDataPath + ", test " + benchNamesTemp[i] + ", Device #" + dut_id_int + ", Cannot continue " + GuModeNames[GuMode]);
                                    runningGU = false;
                                }
                            }
                            continue;
                        }

                    } // if limits found

                } // end reading file  
            }  // using streamreader
            
            

            // Make sure we found limits
            if (!TolHiVrfyFound)
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: HighL row not found in " + benchDataPath + ". Cannot run " + GuModeNames[GuMode]);
                runningGU = false;
            }
            if (!TolLoVrfyFound)
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: LowL row not found in " + benchDataPath + ". Cannot run " + GuModeNames[GuMode]);
                runningGU = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////
            // Compute "simulated solder data" from loose and demo data
            ///////////////////////////////////////////////////////////////////////////////////

            foreach (int batch in dutIdAllDemo.Keys)
            {
                foreach (int dutIDdemo in dutIdAllDemo[batch].ToList())
                {
                    if (!dutIdAllLoose[batch].Contains(dutIDdemo))
                    {
                        LogToLogServiceAndFile(LogLevel.Warn, "NOTICE: In file " + benchDataPath + "\nDevice " + dutIDdemo + " has DemoBoard data but no Loose data found.\nDevice " + dutIDdemo + " DemoBoard data will be ignored");
                        dutIdAllDemo[batch].Remove(dutIDdemo);
                    }
                }

                // get list of only loose samples without demo samples
                dutIdAllLoose[batch] = new List<int>(dutIdAllLoose[batch].Except(dutIdAllDemo[batch]));


                // check for outliers / repeatability issues
                DataAnalysis.DataAnalysisFiles.refDataRepeatabilityLog[batch] = new SortedList<int, List<string>>();

                foreach (int dutID in dutIdAllLoose[batch].Union(dutIdAllDemo[batch]))
                {
                    foreach (UnitType t in Enum.GetValues(typeof(UnitType)))
                    {
                        if (t == UnitType.Demo && !dutIdAllDemo[batch].Contains(dutID)) continue;

                        foreach (string testName in benchNamesTemp)
                        {
                            double[] dutData = allData[batch, testName, dutID, t].ToArray();
                            int repeatabilityRank;

                            List<int> outliers = RankRepeatability(dutData, unitsAreDb[testName], -2, out repeatabilityRank);
                            bool removeData = outliers.Count() == 1;

                            if (removeData)
                            {
                                allData[batch, testName, dutID, t].RemoveAt(outliers[0]);   // actually remove the data from GU calculations
                            }

                            string dutDataStr = "";
                            for (int i = 0; i < dutData.Length; i++)
                            {
                                dutDataStr += dutData[i];
                                if (removeData & outliers.Contains(i)) dutDataStr += "*";  // indicate that data was removed
                                if (i != dutData.Length - 1) dutDataStr += ", ";
                            }

                            if (!DataAnalysis.DataAnalysisFiles.refDataRepeatabilityLog[batch].ContainsKey(repeatabilityRank)) DataAnalysis.DataAnalysisFiles.refDataRepeatabilityLog[batch].Add(repeatabilityRank, new List<string>());

                            DataAnalysis.DataAnalysisFiles.refDataRepeatabilityLog[batch][repeatabilityRank].Add(testName + ", " + (t == UnitType.Loose ? "Loose" : "Demo ") + " unit " + dutID + ", values: " + dutDataStr);
                        }
                    }
                }


                // Compute and apply Demo Board offsets
                foreach (string testName in benchNamesTemp)
                {
                    if (dutIdAllDemo[batch].Count() > 0)
                    {
                        //List<float> demoBrdOffsetsTemp = new List<float>();
                        foreach (int dutIDdemo in dutIdAllDemo[batch])
                        {
                            double averageDemoData = allData[batch, testName, dutIDdemo, UnitType.Demo].Average();
                            double averageLooseData = allData[batch, testName, dutIDdemo, UnitType.Loose].Average();
                            demoDataDict[batch, testName, UnitType.Demo, dutIDdemo] = averageDemoData;
                            demoDataDict[batch, testName, UnitType.Loose, dutIDdemo] = averageLooseData;
                            demoBoardOffsetsPerDut[batch, testName, dutIDdemo] = averageDemoData - averageLooseData;
                        }
                        demoBoardOffsets[batch, testName] = demoBoardOffsetsPerDut[batch][testName].Values.Average();
                    }
                    else
                    {
                        demoBoardOffsets[batch, testName] = 0;
                    }
                    foreach (int dutIDloose in dutIdAllLoose[batch])
                    {
                        finalRefDataDict[batch, testName, dutIDloose] = allData[batch, testName, dutIDloose, UnitType.Loose].Average() + demoBoardOffsets[batch, testName];
                    }
                }

                // calculate the correlation coefficient between loose and demo
                foreach (string testName in benchNamesTemp)
                {
                    if (dutIdAllDemo[batch].Count() > 0)
                    {
                        double[] demoArray = (from pair in demoDataDict[selectedBatch][testName][UnitType.Demo] orderby pair.Key ascending select pair.Value).ToArray();
                        double[] looseArray = (from pair in demoDataDict[selectedBatch][testName][UnitType.Loose] orderby pair.Key ascending select pair.Value).ToArray();


                        demoLooseCorrCoeff[batch,testName] = pearsoncorr2(demoArray, looseArray, demoArray.Length);
                    }
                }


                // Make sure we found device data
                if (dutIdAllLoose[batch].Count < 1)
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Insufficient data found in " + benchDataPath + "\n    for batch " + batch + ". Cannot run " + GuModeNames[GuMode]);
                    runningGU = false;
                }


            }

            selectedBatch = dutIdAllDemo.Keys.First();

            allData.Clear();  // free some memory
        }


        public static string GetTestPlanPath()
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


        private static double SetAutoVeriLimit(string testname)
        {
            testname = testname.ToLower();

            if (testname.StartsWith("pin") | testname.StartsWith("p-in") | testname.StartsWith("p_in"))
            {
                return 1f;  // 0.5
            }
            if (testname.StartsWith("pout") | testname.StartsWith("p-out") | testname.StartsWith("p_out"))
            {
                return 1f; // 0.5
            }
            if (testname.StartsWith("gain"))
            {
                return 1f; // 0.5
            }
            //if ((testname.StartsWith("icc") | testname.StartsWith("itotal")) & testname.Contains("dbm") & !testname.Contains("pin"))
            // Don't check verification on Itotal for Dilong because Ibatt measurement is garbage on bench
            if (testname.StartsWith("icc") & testname.Contains("dbm") & !testname.Contains("pin"))
            {
                if (testname.Contains("lpm")) return 0.002f;
                else return 0.012f;
            }
            if (testname.Contains("aclr") | testname.Contains("acpr"))
            {
                if (testname.Contains("aclr1l") | testname.Contains("acpr1l"))
                {
                    return 3.0f;   // 1.5
                }
                return 3f;
            }
            if (testname.StartsWith("cpl") | testname.StartsWith("couple"))
            {
                return 1f;  // 0.5
            }

            return 9999999f;
        }



        private static double GetScale(string units)
        {
            
            return 1f;  // Clotho does not perform any units scaling, so neither will this GU Cal Verify module.
                        // The test developer is responsible for ensuring that limits, correlation factors, and test results have same units.

            switch (units[0])
            {
                case 'm':
                    return 1e-3f;
                case 'u':
                    return 1e-6f;
                case 'n':
                    return 1e-9f;
                case 'p':
                    return 1e-12f;
                case 'k':
                case 'K':
                    return 1e3f;
                case 'M':
                    return 1e6f;
                case 'G':
                    return 1e9f;
                default:
                    return 1f;
            }
        }



        private static bool ExtractDutID(string in_dut_id_str, out int out_dut_id_int)
        {

            out_dut_id_int = 0;

            for (int i = in_dut_id_str.Length-1; i >= 0; i--)
            {
                int asci = (int) in_dut_id_str[i];
                if (asci < 48 | asci > 57)     // if not a numerical character
                {
                    if (i != in_dut_id_str.Length)
                    {
                        out_dut_id_int = Convert.ToInt32(in_dut_id_str.Remove(0,i+1));
                        return true;
                    }
                    else  // The field did not end with numerical characters. No part ID could be found.
                    {
                        return false;
                    }
                }else if (i==0)
                {
                    out_dut_id_int = Convert.ToInt32(in_dut_id_str);
                    return (true);
                }
            
            }

            return (false);
        }



        private static void InstructOperator()
        {
            if (!runningGU) return;

            LogToLogServiceAndFile(LogLevel.HighLight, "");
            
            StringBuilder msg = new StringBuilder();

            msg.Append("      Please prepare tray with GU Device #s ");
            foreach (int dut in dutIdLooseUserReducedList) msg.Append(dut + ", ");
            //if (dutIdLooseUserReducedList.Count > 1)  msg.Append("\r\n        (in that exact order)");   // no longer necessary with auto SN sequencing
            msg.Append("\r\n      Then run the tray with the handler");
            msg.Append("\r\n      using SITE " + sitesUserReducedList[currentGuSiteIndex] + " only");

            string msgTitle = GuModeNames[GuMode];
            if (runningGUIccCal[siteNo]) msgTitle += " - GU Verification";
            else msgTitle += " - Corr & Verify";

            //MessageBox.Show(wnd.ShowOnTop(), msg.ToString(), msgTitle);
            MessageBoxAsync.Show(msg.ToString(), msgTitle, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            LogToLogServiceAndFile(LogLevel.HighLight, msg.ToString());

        }



        public static void LogToLogServiceAndFile(LogLevel logLev, string str)
        {
            loggedMessages.Add(str);
            logger.Log(logLev, str);
            Console.WriteLine(str);
        }



        private static void WriteGuCorrelationFile()
        {

            Dictionary<string, bool> limitsWritten = corrFileTestNameList.ToDictionary(k => k, v => false);

            using (StreamWriter corrFile = new StreamWriter(new FileStream(correlationFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                corrFile.WriteLine("ParameterName,Factor_Add,Factor_Add_LowLimit,Factor_Add_HighLimit,Factor_Multiply,Factor_Multiply_LowLimit,Factor_Multiply_HighLimit");

                foreach (string testName in corrFileTestNameList)   // write every test that was in previous correlation file
                {
                    if (testName.Contains(iccCalTestNameExtension))  continue;    // don't write Icc calfactors to correlation file, write to different file

                    corrFile.Write(testName + ",");

                    //if (!testedTestNameList.ContainsValue(testName) & !testName.Contains(iccCalTestNameExtension))
                    //{
                    //    LogToLogServiceAndFile(LogLevel.Warn, "NOTICE: Test " + testName + " found in previous Correlation File, but was not currently tested.\nTest will be included in new Correlation File with calfactor of 0.0");
                    //}
                    
                    foreach (int site in sitesAllExistingList)   // *** need some updating here for multisite
                    {
                        // write Factor_Add
                        if ((factorAddEnabledTests.Contains(testName) & testedTestNameList.ContainsValue(testName)) | testName.Contains(iccCalTestNameExtension))  // if theres a previous add factor and the test was actually tested, or if it's an Icc Calfactor
                        {
                            corrFile.Write(GuCalFactorsDict[site, testName].ToString() + ",");   // Factor_Add
                        }
                        else if (factorAddEnabledTests.Contains(testName) & !testedTestNameList.ContainsValue(testName))
                        {
                            corrFile.Write("0.000011,");   // if running a reduced test list, put a non-zero offset so that full test list doesn't lose ability to update corrfactor
                        }
                        else
                        {
                            corrFile.Write("0,");   // no Factor_Add
                        }

                        // write Factor_Add limits
                        if (!limitsWritten[testName])
                        {
                            corrFile.Write(loLimCalAddDict[testName].ToString() + ",");   // Factor_Add_LowLimit
                            corrFile.Write(hiLimCalAddDict[testName].ToString() + ",");   // Factor_Add_HighLimit
                        }

                        // write Factor_Multiply
                        if (factorMultiplyEnabledTests.Contains(testName) & testedTestNameList.ContainsValue(testName))  // if theres a previous multiply factor, and the test was actually tested
                        {
                            corrFile.Write(GuCalFactorsDict[site, testName].ToString() + ",");  // Factor_Multiply
                        }
                        else if (factorMultiplyEnabledTests.Contains(testName) & !testedTestNameList.ContainsValue(testName))
                        {
                            corrFile.Write("0.000011,");   // if running a reduced test list, put a non-zero offset so that full test list doesn't lose ability to update corrfactor
                        }
                        else
                        {
                            corrFile.Write("0,");   // no Factor_Multiply
                        }

                        // write Factor_Multiply limits
                        if (!limitsWritten[testName])
                        {
                            corrFile.Write(loLimCalMultiplyDict[testName].ToString() + ",");   // Factor_Multiply_LowLimit
                            corrFile.Write(hiLimCalMultiplyDict[testName].ToString() + ",");   // Factor_Multiply_HighLimit
                            limitsWritten[testName] = true;
                        }

                        corrFile.WriteLine();

                    } // site loop
                } // testName loop

            } // using StreamWriter

            // Make a backup as well
            StringBuilder corrFileNameBackup = new StringBuilder();
            corrFileNameBackup.Append(correlationFilePath);
            corrFileNameBackup.Insert(corrFileNameBackup.ToString().LastIndexOf('\\') + 1, @"Backup\");
            corrFileNameBackup.Insert(corrFileNameBackup.ToString().LastIndexOf('.'), "_Backup_" + CorrFinishTimeHumanFriendly);

            string path = Path.GetDirectoryName(corrFileNameBackup.ToString());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            File.Copy(correlationFilePath, corrFileNameBackup.ToString());
            
            // add new correlation file to zip
            using (ZipFile zip = ZipFile.Read(DataAnalysis.DataAnalysisFiles.zipFilePath))
            {
                zip.AddFile(correlationFilePath, "NewProgramFactorFiles");
                zip.Save();
            }

            // Write new correlation factor file
            LogToLogServiceAndFile(LogLevel.HighLight, "Updated Correlation File saved to\n        " + correlationFilePath
                 + "\n    and also\n        " + corrFileNameBackup.ToString());

        }



        public static void ReadGuCorrelationFile()
        {
            if (GU.sitesAllExistingList.Count < 1)
            {
                GU.sitesAllExistingList = ATFCrossDomainWrapper.GetValidSitesIndexes();
                sitesAllExistingList = new List<int> { 1 };
            }
            if (!runningGU)  return;  // if there was an error while opening bench data file, don't even bother opening this calfactor file

            correlationFilePath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, "");

            // workaround for Clotho bug, suser pointing to TestPlans directory
            if (ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_VER, "") == "")  // is there a better way to know if I'm suser?
            {
                string correlationFileName = Path.GetFileName(ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, ""));
                correlationFilePath = @"C:\Avago.ATF.Common.x64\CorrelationFiles\Development\" + correlationFileName;
            }

            if (correlationFilePath == "" | correlationFilePath == null)
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Failed to determine Correlation File path. Cannot run " + GuModeNames[GuMode] + ".");
                LogToLogServiceAndFile(LogLevel.Error, "       Please ensure that test plan header includes BuddyCorrelation in Test Plan Properties Section.");
                LogToLogServiceAndFile(LogLevel.Error, "       Cannot run " + GuModeNames[GuMode] + ".");
                runningGU = false;
                return;
            }

            Dictionary<string, int> headerColumnLocaton = new Dictionary<string, int>();

            if (!File.Exists(correlationFilePath))
            {
                if (false & wnd.usingLiteDriver)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.Append("NOTICE: No correlation file was found at:");
                    msg.Append("\n        " + correlationFilePath);
                    msg.Append("\n        " + GuModeNames[GuMode] + " can still be run, but will generate a new correlation file from scratch");
                    msg.Append("\n        with all parameters using Factor_Add and +-999 limits");

                    LogToLogServiceAndFile(LogLevel.Warn, msg.ToString());
                    MessageBox.Show(wnd.ShowOnTop(), msg.ToString(),
                        GuModeNames[GuMode],
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    factorAddEnabledTests = benchTestNameList.Values.ToList();   // if theres no existing calfactor file, assume all calfactors will be add factors. This provides an easy way for operator to generate calfactor file from scratch.
                    loLimCalAddDict = benchTestNameList.Values.ToDictionary(k => k, v => (double)-999f);
                    hiLimCalAddDict = benchTestNameList.Values.ToDictionary(k => k, v => (double)999f);
                    loLimCalMultiplyDict = benchTestNameList.Values.ToDictionary(k => k, v => (double)-999f);
                    hiLimCalMultiplyDict = benchTestNameList.Values.ToDictionary(k => k, v => (double)999f);
                    corrFileTestNameList = benchTestNameList.Values.ToList();
                }
                else
                {
                    StringBuilder msg = new StringBuilder();
                    msg.Append("NOTICE: No correlation file was found at:");
                    msg.Append("\n        " + correlationFilePath);
                    msg.Append("\n        " + GuModeNames[GuMode] + " can not be run.");

                    LogToLogServiceAndFile(LogLevel.Warn, msg.ToString());
                    MessageBox.Show(wnd.ShowOnTop(), msg.ToString(),
                        GuModeNames[GuMode],
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    
                    runningGU = false;
                }
                return;
            }

            using (StreamReader calFile = new StreamReader(new FileStream(correlationFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                bool headerFound = false;

                while (!calFile.EndOfStream)
                {
                    //string[] csvLine = calFile.ReadLine().Split(',');
                    string[] csvLine = calFile.ReadLine().Split(',').TakeWhile(v => v != "").ToArray();

                    //if (csvLine.TakeWhile(v => v != "").Count() < 7) continue;  // skip unimportant lines
                    if (csvLine.Count() < 7) continue;  // skip unimportant lines

                    string testName = csvLine[0];

                    switch (testName)
                    {
                        case "ParameterName":   // the header row
                            headerFound = true;
                            for (int i = 0; i < csvLine.Length; i++)
                            {
                                headerColumnLocaton.Add(csvLine[i], i);
                            }
                            continue;

                        default:   // calfactor data row
                            if (!headerFound)
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Header row (begins with ParameterName) not found in " + correlationFilePath + "\n       Cannot run " + GuModeNames[GuMode]);
                                runningGU = false;
                                return;
                            }

                            corrFileTestNameList.Add(testName);

                            foreach (int site in sitesAllExistingList)  // *** multisite needs more work
                            {
                                //if (!sitesUserReducedList.Contains(site)) continue;  // only read in the calfactors for the sites we are not running. But this list is not populated yet.

                                float factorAdd = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Add"]]);
                                float factorMultiply = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Multiply"]]);

                                if (factorAdd != 0)
                                {
                                    GuCalFactorsDict[site, testName] = factorAdd;   // read these in so we can potentially run GU cal on only 1 site later, and rewrite existing calfactors to other sites in calfactor file
                                }
                                else  // store calfactor multiply, even if it's zero
                                {
                                    GuCalFactorsDict[site, testName] = factorMultiply;   // read these in so we can potentially run GU cal on only 1 site later, and rewrite existing calfactors to other sites in calfactor file
                                }
                            }  // site loop

                            continue;
                    }  // switch first cell in row
                }  // while (!calFile.EndOfStream)
            } // using streamreader

        }



        private static void ReadGuCorrelationTemplate()
        {
            if (!runningGU) return;  // if there was an error while opening bench data file, don't even bother opening this file

            if (File.Exists(correlationTemplatePath))
            {
                LogToLogServiceAndFile(LogLevel.HighLight, "GU Correlation Template found at:\n" + correlationTemplatePath);
            }
            else
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: No GU Correlation Template found at: " + correlationTemplatePath + "\r\n    Cannot run " + GuModeNames[GuMode]);
                runningGU = false;
                return;
            }

            Dictionary<string, int> headerColumnLocaton = new Dictionary<string, int>();

            // reset the information, even though not necessary
            hiLimCalAddDict = new Dictionary<string, double>();
            loLimCalAddDict = new Dictionary<string, double>();
            hiLimCalMultiplyDict = new Dictionary<string, double>();
            loLimCalMultiplyDict = new Dictionary<string, double>();
            factorAddEnabledTests = new List<string>();
            factorMultiplyEnabledTests = new List<string>();

            using (StreamReader calFile = new StreamReader(new FileStream(correlationTemplatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                bool headerFound = false;

                while (!calFile.EndOfStream)
                {
                    //string[] csvLine = calFile.ReadLine().Split(',');
                    string[] csvLine = calFile.ReadLine().Split(',').TakeWhile(v => v != "").ToArray();

                    //if (csvLine.TakeWhile(v => v != "").Count() < 7) continue;  // skip unimportant lines
                    if (csvLine.Count() < 7) continue;  // skip unimportant lines

                    string testName = csvLine[0];

                    switch (testName)
                    {
                        case "ParameterName":   // the header row
                            headerFound = true;
                            for (int i = 0; i < csvLine.Length; i++)
                            {
                                headerColumnLocaton.Add(csvLine[i], i);
                            }
                            continue;

                        default:   // calfactor data row
                            if (!headerFound)
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Header row (begins with ParameterName) not found in " + correlationTemplatePath + "\n       Cannot run " + GuModeNames[GuMode]);
                                runningGU = false;
                                return;
                            }

                            double hiLimCalAdd = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Add_HighLimit"]]);
                            double loLimCalAdd = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Add_LowLimit"]]);
                            double hiLimMultiplyAdd = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Multiply_HighLimit"]]);
                            double loLimMultiplyAdd = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Multiply_LowLimit"]]);


                            hiLimCalAddDict[testName] = hiLimCalAdd;
                            loLimCalAddDict[testName] = loLimCalAdd;
                            hiLimCalMultiplyDict[testName] = hiLimMultiplyAdd;
                            loLimCalMultiplyDict[testName] = loLimMultiplyAdd;

                            if (Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Add"]]) != 0) factorAddEnabledTests.Add(testName);   // will need _Site1 for multisite
                            else if (Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Multiply"]]) != 0) factorMultiplyEnabledTests.Add(testName);   // will need _Site1 for multisite

                            if ((factorAddEnabledTests.Contains(testName) | factorMultiplyEnabledTests.Contains(testName)) & !benchTestNameList.ContainsValue(testName))   // this dictionary was populated during reading of GU bench data file, should contain all test names
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: " + testName + " has non-zero factor in " + correlationTemplatePath + "\n       but not found in GU bench data file. Cannot run " + GuModeNames[GuMode] + ".");
                                runningGU = false;
                            }

                            continue;
                    }  // switch first cell in row
                }  // while (!calFile.EndOfStream)
            } // using streamreader

            foreach (string testName in corrFileTestNameList.Except(hiLimCalAddDict.Keys))  // ensure that all parameters are found in template file
            {
                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Test " + testName + " found in Correlation File, but not found in Correlation Template File. Cannot run " + GuModeNames[GuMode]);
                runningGU = false;
            }

        }


        private static void WriteIccCalfactorFile()
        {
            if (!ENABLE_ICC_CAL)  return;

            string iccCalFilePath = correlationFilePath.ToString().Insert(correlationFilePath.LastIndexOf('.'), iccCalFileNameExtension);

            List<string> iccCalPoutTestNameList = IccCalTemplateExists ?
                iccCalTemplateTestNameList :
                IccCalTestNames.Key.Keys.ToList();

            Dictionary<string, bool> limitsWritten = iccCalPoutTestNameList.ToDictionary(k => k, v => false);

            using (StreamWriter corrFile = new StreamWriter(new FileStream(iccCalFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                corrFile.WriteLine("ParameterName,InputLoss,OutputLoss,LowLimit,HighLimit,IccServoTargetCorrection,VSGlevel");

                foreach (string testName in iccCalPoutTestNameList)   // write every test that was in previous correlation file
                {
                    corrFile.Write(testName + ",");

                    foreach (int site in sitesAllExistingList)   // *** need some updating here for multisite
                    {
                        // write Factor_Add
                        if (IccCalTemplateExists)
                        {
                            if (iccCalFactorRedirect.ContainsKey(testName))
                            {
                                corrFile.Write(iccCalFactorRedirect[testName] + "," + iccCalFactorRedirect[testName] + ",");   // Factor_Add
                            }
                            else if (factorAddEnabledTests.Contains(testName))
                            {
                                if (!previousIccCalFactorsExist & (GuIccCalFailed[site] | GuCorrFailed[site] | GuVerifyFailed[site]))
                                {
                                    corrFile.Write("0.01,0.01,");   // This should only happen when creating Icc Cal file from scratch and failed
                                }
                                else
                                {
                                    corrFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.InputLoss].ToString() + ",");   // Input Loss
                                    corrFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.OutputLoss].ToString() + ",");   // Output Loss
                                }
                            }
                            else
                            {
                                corrFile.Write("0,0,");   // Factor_Add
                            }

                        }
                        else if (!previousIccCalFactorsExist)
                        {
                            if (GuIccCalFailed[site] | GuCorrFailed[site] | GuVerifyFailed[site])
                            {
                                corrFile.Write("0.01,0.01,");   // Create Icc Cal file from scratch even if failed
                            }
                            else
                            {
                                corrFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.InputLoss].ToString() + ",");   // Input Loss
                                corrFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.OutputLoss].ToString() + ",");   // Output Loss
                            }
                        }
                        else
                        {
                            throw new Exception("Algorithm disturbed. GU Cal should not run without Icc Template.");
                        }

                        // write Factor_Add limits
                        if (!limitsWritten[testName])
                        {
                            if (loLimCalAddDict.ContainsKey(testName) & hiLimCalAddDict.ContainsKey(testName))
                            {
                                corrFile.Write(loLimCalAddDict[testName].ToString() + ",");   // Factor_Add_LowLimit
                                corrFile.Write(hiLimCalAddDict[testName].ToString() + ",");   // Factor_Add_HighLimit
                            }
                            else
                            {
                                corrFile.Write("-999,");   // Factor_Add_LowLimit
                                corrFile.Write("999,");   // Factor_Add_HighLimit
                                LogToLogServiceAndFile(LogLevel.Warn, "NOTICE: " + testName + " was tested but not found in previous Icc Calfactor file\n        " + iccCalFilePath + "\n        So limits of +-999 will be written to new Icc Calfactor file.");
                            }
                            limitsWritten[testName] = true;
                        }

                        if (!previousIccCalFactorsExist & (GuIccCalFailed[site] | GuCorrFailed[site] | GuVerifyFailed[site]))    // creating Icc Cal file from scratch even if something failed
                        {
                            corrFile.Write("0,");
                            corrFile.Write("-50,");
                        }
                        else
                        {
                            corrFile.Write(IccServoNewTargetCorrection[site, testName] + ",");
                            corrFile.Write(IccServoNewVSGlevel[site, testName] + ",");
                        }

                        corrFile.WriteLine();

                    } // site loop
                } // testName loop

            } // using StreamWriter

            // Make a backup as well
            StringBuilder iccCalFileNameBackup = new StringBuilder();
            iccCalFileNameBackup.Append(iccCalFilePath);
            iccCalFileNameBackup.Insert(iccCalFileNameBackup.ToString().LastIndexOf('\\') + 1, @"Backup\");
            iccCalFileNameBackup.Insert(iccCalFileNameBackup.ToString().LastIndexOf('.'), "_Backup_" + CorrFinishTimeHumanFriendly);

            string path = Path.GetDirectoryName(iccCalFileNameBackup.ToString());
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            File.Copy(iccCalFilePath, iccCalFileNameBackup.ToString());

            // add new correlation file to zip
            using (ZipFile zip = ZipFile.Read(DataAnalysis.DataAnalysisFiles.zipFilePath))
            {
                zip.AddFile(iccCalFilePath, "NewProgramFactorFiles");
                zip.Save();
            }

            LogToLogServiceAndFile(LogLevel.HighLight, "Updated Icc Calfactor File saved to\n        " + iccCalFilePath
                + "\n    and also\n        " + iccCalFileNameBackup.ToString());

        }



        private static void ReadIccCalfactorFile()
        {
            if (!runningGU | !ENABLE_ICC_CAL) return;  // if there was an error while opening bench data file, don't even bother opening this calfactor file

            iccCalFilePath = correlationFilePath.Insert(correlationFilePath.LastIndexOf('.'), iccCalFileNameExtension);

            if (iccCalFilePath == "")
            {
                runningGU = false;
                return;
            }

            Dictionary<string, int> headerColumnLocaton = new Dictionary<string, int>();

            if (!File.Exists(iccCalFilePath))
            {
                StringBuilder msg = new StringBuilder();
                msg.Append("NOTICE: No GU Verification Calfactor file was found at:");
                msg.Append("\r\n        " + iccCalFilePath);
                msg.Append("\r\n\r\nA new GU Verification Calfactor File will be created.");
                msg.Append("\r\nGU Verificationl will need to pass twice.");

                LogToLogServiceAndFile(LogLevel.Warn, msg.ToString());
                MessageBox.Show(wnd.ShowOnTop(), msg.ToString(),
                    "" + GuModeNames[GuMode] + " - GU Verification",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                previousIccCalFactorsExist = false;

                //KKL : Set all flag to fail
                ProductionSettings.ICC_Cal_PassStatus = true;// no icc cal need
                ProductionSettings.Corr_PassStatus = false;
                ProductionSettings.Vrfy_PassStatus = false;

                //shaz : manually copied icc template
                File.Copy(iccCalTemplatePath, iccCalFilePath, true);

                return;
            }
            else
            {
                previousIccCalFactorsExist = true;
            }

            List<string> testNamesInIccCalFile = new List<string>();

            using (StreamReader calFile = new StreamReader(new FileStream(iccCalFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                bool headerFound = false;

                while (!calFile.EndOfStream)
                {
                    string[] csvLine = calFile.ReadLine().Split(',');

                    if (csvLine.TakeWhile(v => v != "").Count() < 4) continue;  // skip unimportant lines

                    string testName = csvLine[0];

                    switch (testName)
                    {
                        case "ParameterName":   // the header row
                            headerFound = true;
                            for (int i = 0; i < csvLine.Length; i++)
                            {
                                headerColumnLocaton.Add(csvLine[i].Trim(), i);
                            }
                            continue;

                        default:   // calfactor data row
                            if (!headerFound)
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Header row (begins with ParameterName) not found in " + iccCalFilePath);
                                LogToLogServiceAndFile(LogLevel.Error, "       Cannot run " + GuModeNames[GuMode]);
                                runningGU = false;
                                return;
                            }

                            testNamesInIccCalFile.Add(testName);

                            foreach (int site in sitesAllExistingList)  // *** multisite needs more work
                            {
                                //if (!sitesUserReducedList.Contains(site)) continue;  // only read in the calfactors for the sites we are not running. But this list is not populated yet.

                                double inputLoss = 0, outputLoss = 0;

                                if (!double.TryParse(csvLine[headerColumnLocaton["InputLoss"]], out inputLoss) | !double.TryParse(csvLine[headerColumnLocaton["OutputLoss"]], out outputLoss))
                                {
                                    if (csvLine[headerColumnLocaton["InputLoss"]].Length > csvLine[headerColumnLocaton["OutputLoss"]].Length)   // always redirect both input and output loss. Do this in case use has not filled out a cell.
                                        iccCalFactorRedirect[csvLine[headerColumnLocaton["ParameterName"]]] = csvLine[headerColumnLocaton["InputLoss"]];
                                    else
                                        iccCalFactorRedirect[csvLine[headerColumnLocaton["ParameterName"]]] = csvLine[headerColumnLocaton["OutputLoss"]];
                                }
                                else
                                {
                                    GuCalFactorsDict[site, testName + IccCalLoss.InputLoss] = inputLoss;   // read these in so we can potentially run GU cal on only 1 site later, and rewrite existing calfactors to other sites in calfactor file
                                    GuCalFactorsDict[site, testName + IccCalLoss.OutputLoss] = outputLoss;   // read these in so we can potentially run GU cal on only 1 site later, and rewrite existing calfactors to other sites in calfactor file
                                }

                                IccServoTargetCorrection[site, testName] = headerColumnLocaton.ContainsKey("IccServoTargetCorrection") ? Convert.ToSingle(csvLine[headerColumnLocaton["IccServoTargetCorrection"]]) : 0;
                                IccServoVSGlevel[site, testName] = headerColumnLocaton.ContainsKey("VSGlevel") ? Convert.ToSingle(csvLine[headerColumnLocaton["VSGlevel"]]) : 0;
                            }  // site loop

                            continue;
                    }  // switch first cell in row
                }  // while (!calFile.EndOfStream)
            } // using streamreader

            foreach (KeyValuePair<string, string> kv in iccCalFactorRedirect)
            {
                if (!testNamesInIccCalFile.Contains(kv.Value))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: \"" + kv.Value + "\" is not an Icc calibrated test name\n     but test " + kv.Key + " is trying to use its calfactor.\n     Please correct this in " + iccCalFilePath + "\n     Cannot run " + GuModeNames[GuMode]);
                    runningGU = false;
                }
            }

        }



        private static void ReadIccCalfactorTemplate()
        {
            if (!runningGU | !ENABLE_ICC_CAL) return;  // if there was an error while opening bench data file, don't even bother opening this calfactor file

            if (previousIccCalFactorsExist)
            {
                if (iccCalTemplatePath == "")
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: GuIccCalTemplate_Path not found in TCF.\n        Cannot run GU Cal.");
                    runningGU = false;
                    return;
                }
                if (File.Exists(iccCalTemplatePath))
                {
                    LogToLogServiceAndFile(LogLevel.HighLight, "GU Icc Cal Template found at:\n" + iccCalTemplatePath);
                }
                else
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: No GU Icc Cal Template found at: " + iccCalTemplatePath + "\r\n    Cannot run " + GuModeNames[GuMode]);
                    runningGU = false;
                    return;
                }
            }
            else
            {
                if (!IccCalTemplateExists)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.Append("NOTICE: No GU Verification Template file was found at:");
                    msg.Append("\r\n        " + iccCalTemplatePath);
                    msg.Append("\r\n\r\nGU Verification will run in dummy mode, in order to create a default GU Verification Calfactor File.");
                    msg.Append("\r\nUpon completing the dummy run,\nplease convert the default GU Verification Calfactor File into an GU Verification Template File.");

                    LogToLogServiceAndFile(LogLevel.Warn, msg.ToString());
                    MessageBox.Show(wnd.ShowOnTop(), msg.ToString(),
                        "" + GuModeNames[GuMode] + " - GU Verification",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;   // if no previous Icc Cal file, allow Icc Cal file creation regardless of Template existing
                }
            }

            iccCalFactorRedirect.Clear();

            Dictionary<string, int> headerColumnLocaton = new Dictionary<string, int>();

            using (StreamReader calFile = new StreamReader(new FileStream(iccCalTemplatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                bool headerFound = false;

                while (!calFile.EndOfStream)
                {
                    string[] csvLine = calFile.ReadLine().Split(',');

                    if (csvLine.TakeWhile(v => v != "").Count() < 4) continue;  // skip unimportant lines

                    string testName = csvLine[0];

                    switch (testName)
                    {
                        case "ParameterName":   // the header row
                            headerFound = true;
                            for (int i = 0; i < csvLine.Length; i++)
                            {
                                headerColumnLocaton.Add(csvLine[i].Trim(), i);
                            }
                            continue;

                        default:   // calfactor data row
                            if (!headerFound)
                            {
                                LogToLogServiceAndFile(LogLevel.Error, "ERROR: Header row (begins with ParameterName) not found in " + iccCalTemplatePath);
                                LogToLogServiceAndFile(LogLevel.Error, "       Cannot run " + GuModeNames[GuMode]);
                                runningGU = false;
                                return;
                            }

                            iccCalTemplateTestNameList.Add(testName);

                            double hiLimCalAdd = Convert.ToSingle(csvLine[headerColumnLocaton["HighLimit"]]);
                            double loLimCalAdd = Convert.ToSingle(csvLine[headerColumnLocaton["LowLimit"]]);

                            hiLimCalAddDict[testName] = hiLimCalAdd;
                            loLimCalAddDict[testName] = loLimCalAdd;

                            foreach (int site in sitesAllExistingList)  // *** multisite needs more work
                            {
                                double inputLoss = 0, outputLoss = 0; // = Convert.ToSingle(csvLine[headerColumnLocaton["Factor_Add"]]);

                                if (!double.TryParse(csvLine[headerColumnLocaton["InputLoss"]], out inputLoss) | !double.TryParse(csvLine[headerColumnLocaton["OutputLoss"]], out outputLoss))
                                {
                                    if (csvLine[headerColumnLocaton["InputLoss"]].Length > csvLine[headerColumnLocaton["OutputLoss"]].Length)   // always redirect both input and output loss. Do this in case use has not filled out a cell.
                                        iccCalFactorRedirect[csvLine[headerColumnLocaton["ParameterName"]]] = csvLine[headerColumnLocaton["InputLoss"]];
                                    else
                                        iccCalFactorRedirect[csvLine[headerColumnLocaton["ParameterName"]]] = csvLine[headerColumnLocaton["OutputLoss"]];
                                }
                                else
                                {
                                    if (inputLoss != 0 || outputLoss != 0) factorAddEnabledTests.Add(testName);
                                }
                            }  // site loop

                            continue;
                    }  // switch first cell in row
                }  // while (!calFile.EndOfStream)
            } // using streamreader

            foreach (KeyValuePair<string, string> kv in iccCalFactorRedirect)
            {
                if (!factorAddEnabledTests.Contains(kv.Value))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: \"" + kv.Value + "\" is not an Icc calibrated test name\n     but test " + kv.Key + " is trying to use its calfactor.\n     Please correct this in " + iccCalTemplatePath + "\n     Cannot run " + GuModeNames[GuMode]);
                    runningGU = false;
                }
            }

            if (previousIccCalFactorsExist & IccServoTargetCorrection.Count() > 0)
            {
                foreach (string testName in IccServoTargetCorrection.First().Value.Keys.Except(hiLimCalAddDict.Keys))  // ensure that all parameters are found in template file
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Test " + testName + " found in Icc Calfactor File, but not found in Icc Cal Template File. Cannot run " + GuModeNames[GuMode]);
                    runningGU = false;
                }
            }

        }


        public static List<int> RankRepeatability(double[] array, bool unitsAreDb, int outlierRank, out int worstRank)
        {
            const int maxRank = 10;

            worstRank = maxRank;

            if (array.Length < 2) return new List<int>();   // can't do outlier detection on 1 sample

            double[] sortedArray = array.ToArray();
            Array.Sort(sortedArray);
            double medianIndex = ((double)sortedArray.Length + 1.0) / 2.0 - 1;
            double median = (sortedArray[(int)Math.Floor(medianIndex)] + sortedArray[(int)Math.Ceiling(medianIndex)]) / 2.0;

            double[] absDevs = new double[sortedArray.Length];

            for (int i = 0; i < array.Length; i++)
            {
                absDevs[i] = Math.Abs(array[i] - median);
            }

            List<int> outlierIndices = new List<int>();
            double safeDeviation = 0;

            if (unitsAreDb)
            {
                safeDeviation = 0.02 + 100000 / Math.Pow(Math.Max(median, -100) + 187.0, 2.6);   // formula which allows larger tolerance for smaller dB
            }
            else
            {
                safeDeviation = Math.Abs(0.01 * median);
            }


            for (int i = 0; i < array.Length; i++)
            {
                int repRank = maxRank;

                if (absDevs[i] != 0)
                {
                    repRank = -(int)(Math.Ceiling(Math.Log(absDevs[i] / safeDeviation, 2.0)));
                }

                repRank = Math.Max(-maxRank, repRank);
                repRank = Math.Min(maxRank, repRank);
                
                worstRank = Math.Min(worstRank, repRank);

                if (repRank <= outlierRank)
                {
                    outlierIndices.Add(i);
                }
            }

            return outlierIndices;

        }


        public static List<int> RankRepeatability(float[] array, bool unitsAreDb, int outlierRank, out int worstRank)
        {
            double[] dblArray = new double[array.Length];

            Array.Copy(array, dblArray, array.Length);

            return RankRepeatability(dblArray, unitsAreDb, outlierRank, out worstRank);

        }

        public static double getValueWithCF(int site, string testName, double raw_value)
        {
            double final_value = raw_value;

            try
            {
                if (GuMode != null)
                {
                    if (runningGU & (GuMode != GuModes.Vrfy)) return raw_value;

                    double calfactor = GuCalFactorsDict[site, testName];

                    if (calfactor != 0)
                    {
                        if (factorAddEnabledTests.Contains(testName))
                            final_value += calfactor;
                        else if (factorMultiplyEnabledTests.Contains(testName))
                            final_value *= calfactor;
                    }

                    //if (factorMultiplyEnabledTests.Contains(testName))
                    //{
                    //    if(calfactor != 0)
                    //        final_value *= calfactor;
                    //}
                    //if (factorAddEnabledTests.Contains(testName))
                    //{
                    //    final_value += calfactor;
                    //}
                }

                return final_value;
            }
            catch (Exception e)
            {
                return raw_value;
            }
        }

        //private static class DataAnalysisFiles2
        //{
        //    public static string computerName;
        //    public static string testPlanVersion;
        //    public static string dibID;
        //    public static string handlerSN;
        //    public static string lotID;
        //    public static string sublotID;
        //    public static string opID;
        //    public static string waferID;
        //    public static string testPlanName;
        //    public static string fileNameRoot;
        //    public static string calOrVrfy;
        //    public static string guDataDir;
        //    public static string guDataRemoteDir;

        //    public static string ipAddress;

        //    public static string zipFilePath;
        //    public static List<string> allAnalysisFiles = new List<string>();   //

        //    private const string IccCalAnalysisDir = "2_IccCalAnalysis";
        //    private const string CorrAnalysisDir = "3_CorrAnalysis";
        //    private const string VerifyAnalysisDir = "4_VerifyAnalysis";
        //    private const string RefDataDir = "1_RefDataAnalysis";

        //    public static Dictionary<int, SortedList<int, List<string>>> refDataRepeatabilityLog = new Dictionary<int, SortedList<int, List<string>>>();

        //    public static void WriteAll()
        //    {
        //        try
        //        {
        //            // Generate header info

        //            computerName = System.Environment.MachineName;
        //            testPlanVersion = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TP_VER, "");
        //            dibID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_DIB_ID, "");
        //            handlerSN = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_SN, "");
        //            lotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
        //            sublotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_SUB_LOT_ID, "");
        //            opID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_OP_ID, "");
        //            waferID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_WAFER_ID, "");
        //            //testPlanName = (new StackFrame(2)).GetMethod().DeclaringType.FullName;
        //            testPlanName = Path.GetFileNameWithoutExtension(GetTestPlanPath().TrimEnd('\\')); //Ivan
        //            ipAddress = GetLocalIPAddress();

        //            fileNameRoot = testPlanName + "_" + CorrFinishTimeHumanFriendly;

        //            calOrVrfy = "_Gu" + GuMode.ToString();

        //            string passFailIndicator = "";
        //            if (GuMode == GuModes.IccCorrVrfy) passFailIndicator += GuIccCalFailed.ContainsValue(true) ? "F" : "P";
        //            if (GuMode == GuModes.IccCorrVrfy | GuMode == GuModes.CorrVrfy) passFailIndicator += GuCorrFailed.ContainsValue(true) ? "F" : "P";
        //            passFailIndicator += GuVerifyFailed.ContainsValue(true) ? "F" : "P";

        //            guDataDir = @"C:/Avago.ATF.Common.x64/AutoGUcalResults/" + CorrFinishTimeHumanFriendly + "_" + testPlanName + calOrVrfy + "_" + passFailIndicator + @"/";

        //            allAnalysisFiles.Clear();

        //            if (!Directory.Exists(guDataDir + RefDataDir)) Directory.CreateDirectory(guDataDir + RefDataDir);
        //            WriteRefFinalData(guDataDir + RefDataDir + "/GuRefFinalData_" + fileNameRoot + ".csv");
        //            WriteRefDemoData(guDataDir + RefDataDir + "/GuRefDemoData_" + fileNameRoot + ".csv", UnitType.Demo);
        //            WriteRefDemoData(guDataDir + RefDataDir + "/GuRefPreDemoData_" + fileNameRoot + ".csv", UnitType.Loose);
        //            WriteRefDemoOffsets(guDataDir + RefDataDir + "/GuRefDemoOffsets_" + fileNameRoot + ".csv");
        //            WriteRefRepeatFile(guDataDir + RefDataDir + "/GuRefRepeatability_" + fileNameRoot + ".txt");
        //            WriteRefLooseDemoCorrCoeff(guDataDir + RefDataDir + "/GuRefLooseDemoCorrCoeff_" + fileNameRoot + ".csv");
                    
        //            if (GuMode == GuModes.IccCorrVrfy)
        //            {
        //                if (!Directory.Exists(guDataDir + IccCalAnalysisDir)) Directory.CreateDirectory(guDataDir + IccCalAnalysisDir);
        //                WriteIccCalfactor(guDataDir + IccCalAnalysisDir + "/GuIccCalFactor_" + fileNameRoot + ".csv");
        //                WriteIccCalData(guDataDir + IccCalAnalysisDir + "/GuIccCalData_" + fileNameRoot + ".csv");
        //                WriteIccAvgError(guDataDir + IccCalAnalysisDir + "/GuIccAvgVrfyError_" + fileNameRoot + ".csv");
        //            }

        //            if (GuMode == GuModes.IccCorrVrfy || GuMode == GuModes.CorrVrfy)
        //            {
        //                if (!Directory.Exists(guDataDir + CorrAnalysisDir)) Directory.CreateDirectory(guDataDir + CorrAnalysisDir);
        //                WriteCorrFactor(guDataDir + CorrAnalysisDir + "/GuCorrFactor_" + fileNameRoot + ".csv");
        //                WriteCorrFactorNoDemo(guDataDir + CorrAnalysisDir + "/GuCorrFactorNoDemoOffset_" + fileNameRoot + ".csv");
        //            }

        //            if (!Directory.Exists(guDataDir + VerifyAnalysisDir)) Directory.CreateDirectory(guDataDir + VerifyAnalysisDir);
        //            WriteRawData(guDataDir + VerifyAnalysisDir + "/GuRawData_" + fileNameRoot + ".csv");
        //            WriteVrfyData(guDataDir + VerifyAnalysisDir + "/GuVrfyData_" + fileNameRoot + ".csv");
        //            WriteVrfyError(guDataDir + VerifyAnalysisDir + "/GuVrfyError_" + fileNameRoot + ".csv");
        //            WriteCorrCoeff(guDataDir + VerifyAnalysisDir + "/GuCorrCoeff_" + fileNameRoot + ".csv");

        //            WriteLogFile(guDataDir + "/GuLogPrintout_" + fileNameRoot + ".txt");

        //            //zip everything up for convenience
        //            zipFilePath = ipAddress == "" ?
        //                (guDataDir + CorrFinishTimeHumanFriendly + "_" + testPlanName + calOrVrfy + "_" + passFailIndicator + "_" + RandomString(7) + ".zip") :
        //                (guDataDir + "IP" + ipAddress + "_" + CorrFinishTimeHumanFriendly + "_" + testPlanName + calOrVrfy + "_" + passFailIndicator + "_" + RandomString(7) + ".zip");

        //            using (ZipFile zip = new ZipFile(zipFilePath))
        //            {
        //                foreach (string file in allAnalysisFiles)
        //                {
        //                    string dir = (Path.GetDirectoryName(file) == Path.GetDirectoryName(guDataDir)) ? "" : Path.GetFileName(Path.GetDirectoryName(file));

        //                    zip.AddFile(file, dir);
        //                }

        //                // Add previous Correlation and Icc Cal factor file
        //                if (File.Exists(correlationFilePath))
        //                    zip.AddFile(correlationFilePath, "PreviousProgramFactorFiles");
        //                else
        //                    zip.AddEntry("PreviousProgramFactorFiles\\NoPreviousCorrFactorFile.txt", "");
        //                if (File.Exists(iccCalFilePath))
        //                    zip.AddFile(iccCalFilePath, "PreviousProgramFactorFiles");
        //                else
        //                    zip.AddEntry("PreviousProgramFactorFiles\\NoPreviousIccCalFactorFile.txt", "");

        //                zip.AddFile(benchDataPath, RefDataDir);

        //                AddZipToZip(zip, @"C:\Avago.ATF.Common\Results\ProgramReport.zip", "ProgramReport");

        //                zip.Save();
        //            }

        //            if (Directory.Exists(guDataRemoteDir)) File.Copy(zipFilePath, guDataRemoteDir + "\\" + Path.GetFileNameWithoutExtension(zipFilePath) + ".gucal");
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show("Error while saving GU analysis files\n\n" + e.ToString());
        //        }

        //    }


        //    private static string RandomString(int length)
        //    {
        //        string randomString = "";

        //        Random r = new Random();

        //        for (int i = 0; i < length; i++)
        //        {
        //            int randomByte = r.Next(48, 90);
        //            while (randomByte >= 58 && randomByte <= 64) randomByte = r.Next(48, 90);   // avoid unwanted characters

        //            randomString += Convert.ToChar(randomByte);
        //        }

        //        return randomString;
        //    }


        //    private static void printHeader(StreamWriter sw, string startTime, string finishTime)
        //    {
        //        sw.WriteLine("--- Global Info:");
        //        sw.WriteLine("Date," + finishTime);
        //        sw.WriteLine("StartTime," + startTime);
        //        sw.WriteLine("FinishTime," + finishTime);
        //        sw.WriteLine("TestPlanVersion," + testPlanVersion);
        //        sw.WriteLine("Product," + prodTag);
        //        sw.WriteLine("TestPlan," + testPlanName + ".cs");
        //        sw.WriteLine("Lot," + lotID);
        //        sw.WriteLine("Sublot," + sublotID);
        //        sw.WriteLine("Wafer," + waferID);
        //        sw.WriteLine("TesterName," + computerName);
        //        sw.WriteLine("TesterIPaddress," + ipAddress);
        //        sw.WriteLine("Operator," + opID);
        //        sw.WriteLine("Handler ID," + handlerSN);
        //        sw.WriteLine("LoadBoardName," + dibID);
        //    }

        //    private static void printSummary(StreamWriter sw)
        //    {
        //        sw.WriteLine();

        //        if (GuMode == GuModes.IccCorrVrfy)
        //        {
        //            if (!GuIccCalFailed.ContainsValue(true))
        //            {
        //                sw.WriteLine("\n\n#GU Icc Calibration Summary, PASSED");
        //            }
        //            else
        //            {
        //                sw.WriteLine("\n\n#GU Icc Calibration Summary, FAILED");
        //            }
        //        }

        //        if (GuMode == GuModes.IccCorrVrfy || GuMode == GuModes.CorrVrfy)
        //        {
        //            if (!GuCorrFailed.ContainsValue(true))
        //            {
        //                sw.WriteLine("#GU Correlation Summary, PASSED");
        //            }
        //            else
        //            {
        //                sw.WriteLine("#GU Correlation Summary, FAILED");
        //            }
        //        }

        //        if (!GuVerifyFailed.ContainsValue(true))
        //        {
        //            sw.WriteLine("#GU Verification Summary, PASSED");
        //        }
        //        else
        //        {
        //            sw.WriteLine("#GU Verification Summary, FAILED");
        //        }

        //    }

        //    internal static string GetLocalIPAddress()
        //    {
        //        return ATFRTE.Instance.IPAddress;
        //    }


        //    public static void WriteCorrFactor(string corrFactorFilePath)
        //    {
        //        using (StreamWriter corrFactorFile = new StreamWriter(new FileStream(corrFactorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(corrFactorFile, CorrStartTime, CorrFinishTime);

        //            corrFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorFile.Write(testName + ",");
        //            }
        //            corrFactorFile.WriteLine("");

        //            // write test numbers
        //            corrFactorFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorFile.Write(testNumDict[testName] + ",");
        //            }

        //            corrFactorFile.WriteLine("");

        //            // write units
        //            corrFactorFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorFile.Write(unitsDict[testName] + ",");
        //            }

        //            corrFactorFile.WriteLine("");

        //            // write high limits
        //            corrFactorFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                if (factorMultiplyEnabledTests.Contains(testName))
        //                {
        //                    corrFactorFile.Write(hiLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //                else
        //                {
        //                    corrFactorFile.Write(hiLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }

        //            }
        //            corrFactorFile.WriteLine("");

        //            // write low limits
        //            corrFactorFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                if (factorMultiplyEnabledTests.Contains(testName))
        //                {
        //                    corrFactorFile.Write(loLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //                else
        //                {
        //                    corrFactorFile.Write(loLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //            }
        //            corrFactorFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                // correlation factor file
        //                corrFactorFile.Write("999,,,,," + site + ",,,,,");
        //                foreach (string testName in testedTestNameList.Values)
        //                {
        //                    corrFactorFile.Write(GuCalFactorsDict[site, testName] + ",");
        //                }
        //                corrFactorFile.WriteLine("");

        //            } // site loop

        //            printSummary(corrFactorFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Correlation Factor Data saved to " + corrFactorFilePath);
        //        allAnalysisFiles.Add(corrFactorFilePath);
        //    }


        //    public static void WriteCorrFactorNoDemo(string corrFactorNoDemoFilePath)
        //    {
        //        using (StreamWriter corrFactorNoDemoFile = new StreamWriter(new FileStream(corrFactorNoDemoFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(corrFactorNoDemoFile, CorrStartTime, CorrFinishTime);

        //            corrFactorNoDemoFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorNoDemoFile.Write(testName + ",");
        //            }
        //            corrFactorNoDemoFile.WriteLine("");

        //            // write test numbers
        //            corrFactorNoDemoFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorNoDemoFile.Write(testNumDict[testName] + ",");
        //            }

        //            corrFactorNoDemoFile.WriteLine("");

        //            // write units
        //            corrFactorNoDemoFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                corrFactorNoDemoFile.Write(unitsDict[testName] + ",");
        //            }

        //            corrFactorNoDemoFile.WriteLine("");

        //            // write high limits
        //            corrFactorNoDemoFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                if (factorMultiplyEnabledTests.Contains(testName))
        //                {
        //                    corrFactorNoDemoFile.Write(hiLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //                else
        //                {
        //                    corrFactorNoDemoFile.Write(hiLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }

        //            }
        //            corrFactorNoDemoFile.WriteLine("");

        //            // write low limits
        //            corrFactorNoDemoFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                if (factorMultiplyEnabledTests.Contains(testName))
        //                {
        //                    corrFactorNoDemoFile.Write(loLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //                else
        //                {
        //                    corrFactorNoDemoFile.Write(loLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
        //                }
        //            }
        //            corrFactorNoDemoFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                // correlation factor file
        //                corrFactorNoDemoFile.Write("999,,,,," + site + ",,,,,");
        //                foreach (string testName in testedTestNameList.Values)
        //                {
        //                    corrFactorNoDemoFile.Write((GuCalFactorsDict[site, testName] - demoBoardOffsets[selectedBatch, testName]) + ",");
        //                }
        //                corrFactorNoDemoFile.WriteLine("");

        //            } // site loop

        //            printSummary(corrFactorNoDemoFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Correlation Factor (without Demo offsets) Data saved to " + corrFactorNoDemoFilePath);
        //        allAnalysisFiles.Add(corrFactorNoDemoFilePath);
        //    }


        //    public static void WriteRawData(string rawDataFilePath)
        //    {

        //        using (StreamWriter rawDataFile = new StreamWriter(new FileStream(rawDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {

        //            printHeader(rawDataFile, CorrStartTime, CorrFinishTime);

        //            rawDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                rawDataFile.Write(testName + ",");
        //            }
        //            rawDataFile.WriteLine("");

        //            // write test numbers
        //            rawDataFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                rawDataFile.Write(testNumDict[testName] + ",");
        //            }

        //            rawDataFile.WriteLine("");

        //            // write units
        //            rawDataFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                rawDataFile.Write(unitsDict[testName] + ",");
        //            }

        //            rawDataFile.WriteLine("");

        //            // write high limits
        //            rawDataFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                rawDataFile.Write("1,");   // ***these limits don't really apply to the data!
        //            }
        //            rawDataFile.WriteLine("");

        //            // write low limits
        //            rawDataFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                rawDataFile.Write("-1,");   // ***these limits don't really apply to the data!
        //            }
        //            rawDataFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                foreach (int dutID in dutIdLooseUserReducedList)
        //                {
        //                    // calibration data file, all runs of raw data
        //                    for (int run = numIgnoreLoops + 1; run <= numRunLoops; run++)
        //                    {
        //                        //rawDataFile.Write(dutID + "-run" + run + ",,,,," + site + ",,,,,");
        //                        rawDataFile.Write("PID-" + dutID + ",,,,," + site + ",,,,,");
        //                        foreach (string testName in testedTestNameList.Values)
        //                        {
        //                            rawDataFile.Write(rawAllMsrDataDict[site, testName, dutID, run] + ",");
        //                        }
        //                        rawDataFile.WriteLine("");
        //                    }

        //                    if (dutIDtestedDead.Contains(dutID)) continue;

        //                } // dut loop

        //            } // site loop

        //            printSummary(rawDataFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Raw Data saved to " + rawDataFilePath);
        //        allAnalysisFiles.Add(rawDataFilePath);

        //    }


        //    public static void WriteIccCalfactor(string iccCalFactorFilePath)
        //    {

        //        using (StreamWriter iccCalFactorFile = new StreamWriter(new FileStream(iccCalFactorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(iccCalFactorFile, IccCalStartTime, IccCalFinishTime);

        //            List<string> IccTestNameList = new List<string>();
        //            if (GuMode == GuModes.IccCorrVrfy)
        //            {
        //                //IccTestNameList = new List<string>(IccCalFactorsTempDict[sitesUserReducedList[0]].Keys);
        //                IccTestNameList = IccCalTestNames.Key.Keys.ToList();
        //            }

        //            iccCalFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(testName + ",");
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write test numbers
        //            iccCalFactorFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(testNumDict[IccCalTestNames.Key[testName].PoutTestName] + ",");
        //            }

        //            iccCalFactorFile.WriteLine("");

        //            // write units
        //            iccCalFactorFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(unitsDict[IccCalTestNames.Key[testName].PoutTestName] + ",");
        //            }

        //            iccCalFactorFile.WriteLine("");

        //            // write high limits
        //            iccCalFactorFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in IccTestNameList)
        //            {
        //                if (hiLimCalAddDict.ContainsKey(testName))
        //                {
        //                    iccCalFactorFile.Write(hiLimCalAddDict[testName] + ",");
        //                }
        //                else
        //                {
        //                    iccCalFactorFile.Write("999,");
        //                }
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write low limits
        //            iccCalFactorFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in IccTestNameList)
        //            {
        //                if (hiLimCalAddDict.ContainsKey(testName))
        //                {
        //                    iccCalFactorFile.Write(loLimCalAddDict[testName] + ",");
        //                }
        //                else
        //                {
        //                    iccCalFactorFile.Write("-999,");
        //                }
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                iccCalFactorFile.Write("InputLoss,,,,," + site + ",,,,,");
        //                foreach (string testName in IccTestNameList)
        //                {
        //                    iccCalFactorFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.InputLoss] + ",");
        //                }
        //                iccCalFactorFile.WriteLine("");

        //                iccCalFactorFile.Write("OutputLoss,,,,," + site + ",,,,,");
        //                foreach (string testName in IccTestNameList)
        //                {
        //                    iccCalFactorFile.Write(GuCalFactorsDict[site, testName + IccCalLoss.OutputLoss] + ",");
        //                }
        //                iccCalFactorFile.WriteLine("");
        //            } // site loop

        //            printSummary(iccCalFactorFile);
        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Factors saved to " + iccCalFactorFilePath);
        //        allAnalysisFiles.Add(iccCalFactorFilePath);
        //    }


        //    public static void WriteIccAvgError(string iccCalAvgErrorFilePath)
        //    {

        //        using (StreamWriter iccCalFactorFile = new StreamWriter(new FileStream(iccCalAvgErrorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(iccCalFactorFile, IccCalStartTime, IccCalFinishTime);

        //            List<string> IccTestNameList = IccCalTestNames.Icc.Keys.ToList();

        //            iccCalFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(testName + ",");
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write test numbers
        //            iccCalFactorFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(testNumDict[testName] + ",");
        //            }

        //            iccCalFactorFile.WriteLine("");

        //            // write units
        //            iccCalFactorFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(unitsDict[testName] + ",");
        //            }

        //            iccCalFactorFile.WriteLine("");

        //            // write high limits
        //            iccCalFactorFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(IccCalAvgErrorDict[sitesUserReducedList.First(), 1, testName].HiLim + ","); 
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write low limits
        //            iccCalFactorFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in IccTestNameList)
        //            {
        //                iccCalFactorFile.Write(IccCalAvgErrorDict[sitesUserReducedList.First(), 1, testName].LoLim + ",");
        //            }
        //            iccCalFactorFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                for (int attemptNum = 1; attemptNum <= currentGUattemptNumber; attemptNum++)
        //                {
        //                    iccCalFactorFile.Write( "run-" + attemptNum + ",,,,," + site + ",,,,,");
        //                    foreach (string testName in IccTestNameList)
        //                    {
        //                        iccCalFactorFile.Write(IccCalAvgErrorDict[site, attemptNum, testName].AvgError + ",");
        //                    }
        //                    iccCalFactorFile.WriteLine("");
        //                }

        //            } // site loop

        //            printSummary(iccCalFactorFile);
        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Average Error saved to " + iccCalAvgErrorFilePath);
        //        string lastFolderName = Path.GetFileName(Path.GetDirectoryName(iccCalAvgErrorFilePath));
        //        allAnalysisFiles.Add(iccCalAvgErrorFilePath);
        //    }


        //    public static void WriteVrfyData(string vrfyDataFilePath)
        //    {
        //        using (StreamWriter vrfyDataFile = new StreamWriter(new FileStream(vrfyDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(vrfyDataFile, CorrStartTime, CorrFinishTime);

        //            vrfyDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyDataFile.Write(testName + ",");
        //            }
        //            vrfyDataFile.WriteLine("");

        //            // write test numbers
        //            vrfyDataFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyDataFile.Write(testNumDict[testName] + ",");
        //            }

        //            vrfyDataFile.WriteLine("");

        //            // write units
        //            vrfyDataFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyDataFile.Write(unitsDict[testName] + ",");
        //            }

        //            vrfyDataFile.WriteLine("");

        //            // write high limits
        //            vrfyDataFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyDataFile.Write("1,");   // ***these limits don't really apply to the data!
        //            }
        //            vrfyDataFile.WriteLine("");

        //            // write low limits
        //            vrfyDataFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyDataFile.Write("-1,");   // ***these limits don't really apply to the data!
        //            }
        //            vrfyDataFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                foreach (int dutID in dutIdLooseUserReducedList)
        //                {
        //                    if (dutIDtestedDead.Contains(dutID)) continue;

        //                    vrfyDataFile.Write("PID-" + dutID + ",,,,," + site + ",,,,,");
        //                    foreach (string testName in testedTestNameList.Values)
        //                    {
        //                        vrfyDataFile.Write(correctedMsrDataDict[site, testName, dutID] + ",");  // verification data file, the last run's error with correlation factors applied
        //                    }
        //                    vrfyDataFile.WriteLine("");
        //                } // dut loop

        //            } // site loop

        //            printSummary(vrfyDataFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Verification Data saved to " + vrfyDataFilePath);
        //        allAnalysisFiles.Add(vrfyDataFilePath);
        //    }


        //    public static void WriteVrfyError(string vrfyErrorFilePath)
        //    {
        //        using (StreamWriter vrfyErrorFile = new StreamWriter(new FileStream(vrfyErrorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(vrfyErrorFile, CorrStartTime, CorrFinishTime);

        //            vrfyErrorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyErrorFile.Write(testName + ",");
        //            }
        //            vrfyErrorFile.WriteLine("");

        //            // write test numbers
        //            vrfyErrorFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyErrorFile.Write(testNumDict[testName] + ",");
        //            }

        //            vrfyErrorFile.WriteLine("");

        //            // write units
        //            vrfyErrorFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyErrorFile.Write(unitsDict[testName] + ",");
        //            }

        //            vrfyErrorFile.WriteLine("");

        //            // write high limits
        //            vrfyErrorFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyErrorFile.Write(hiLimVrfyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //            }
        //            vrfyErrorFile.WriteLine("");

        //            // write low limits
        //            vrfyErrorFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyErrorFile.Write(loLimVrfyDict[testName] + ",");   // ***these limits don't really apply to the data!
        //            }
        //            vrfyErrorFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                foreach (int dutID in dutIdLooseUserReducedList)
        //                {
        //                    if (dutIDtestedDead.Contains(dutID)) continue;

        //                    vrfyErrorFile.Write("PID-" + dutID + ",,,,," + site + ",,,,,");
        //                    foreach (string testName in testedTestNameList.Values)
        //                    {
        //                        vrfyErrorFile.Write(correctedMsrErrorDict[site, testName, dutID] + ",");
        //                    }
        //                    vrfyErrorFile.WriteLine("");
        //                } // dut loop

        //            } // site loop

        //            printSummary(vrfyErrorFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Verification Error saved to " + vrfyErrorFilePath);
        //        allAnalysisFiles.Add(vrfyErrorFilePath);
        //    }


        //    public static void WriteCorrCoeff(string vrfyCorrCoeffFilePath)
        //    {
        //        using (StreamWriter vrfyCorrCoeffFile = new StreamWriter(new FileStream(vrfyCorrCoeffFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(vrfyCorrCoeffFile, CorrStartTime, CorrFinishTime);

        //            vrfyCorrCoeffFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(testName + ",");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write test numbers
        //            vrfyCorrCoeffFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(testNumDict[testName] + ",");
        //            }

        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write units
        //            vrfyCorrCoeffFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(unitsDict[testName] + ",");
        //            }

        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write high limits
        //            vrfyCorrCoeffFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write("1,");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write low limits
        //            vrfyCorrCoeffFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write("-1,");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                vrfyCorrCoeffFile.Write("999,,,,," + site + ",,,,,");
        //                foreach (string testName in testedTestNameList.Values)
        //                {
        //                    vrfyCorrCoeffFile.Write(corrCoeffDict[site, testName] + ",");
        //                }
        //                vrfyCorrCoeffFile.WriteLine("");

        //            } // site loop

        //            printSummary(vrfyCorrCoeffFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Verification Correlation Coefficients saved to " + vrfyCorrCoeffFilePath);
        //        allAnalysisFiles.Add(vrfyCorrCoeffFilePath);
        //    }


        //    public static void WriteLogFile(string logMessagesFilePath)
        //    {
        //        using (StreamWriter logMessagesFile = new StreamWriter(new FileStream(logMessagesFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(logMessagesFile, IccCalStartTime, CorrFinishTime);

        //            logMessagesFile.WriteLine("\r\n\r\nMessages logged during " + GuModeNames[GuMode] + ":\r\n--------------------------------------------------------------\r\n\r\n");
        //            foreach (string str in loggedMessages)
        //            {
        //                logMessagesFile.WriteLine(str);
        //            }

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Log Messages saved to " + logMessagesFilePath);
        //        allAnalysisFiles.Add(logMessagesFilePath);
        //    }


        //    public static void WriteIccCalData(string iccCalDataFilePath)
        //    {

        //        using (StreamWriter iccCalDataFile = new StreamWriter(new FileStream(iccCalDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(iccCalDataFile, IccCalStartTime, IccCalFinishTime);

        //            iccCalDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in IccCalTestNames.All)
        //            {
        //                iccCalDataFile.Write(testName + ",");
        //            }
        //            iccCalDataFile.WriteLine("");

        //            // write test numbers
        //            iccCalDataFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in IccCalTestNames.All)
        //            {
        //                iccCalDataFile.Write(testNumDict[testName] + ",");
        //            }

        //            iccCalDataFile.WriteLine("");

        //            // write units
        //            iccCalDataFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in IccCalTestNames.All)
        //            {
        //                iccCalDataFile.Write(unitsDict[testName] + ",");
        //            }

        //            iccCalDataFile.WriteLine("");

        //            // write high limits
        //            iccCalDataFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in IccCalTestNames.All)
        //            {
        //                iccCalDataFile.Write("999,");
        //            }
        //            iccCalDataFile.WriteLine("");

        //            // write low limits
        //            iccCalDataFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in IccCalTestNames.All)
        //            {
        //                iccCalDataFile.Write("-999,");
        //            }
        //            iccCalDataFile.WriteLine("");


                    
        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                foreach (int dutID in dutIdLooseUserReducedList)
        //                {
        //                    // calibration data file, all runs of raw data
        //                    for (int run = numIgnoreLoops + 1; run <= numRunLoops; run++)
        //                    {
        //                        //rawDataFile.Write(dutID + "-run" + run + ",,,,," + site + ",,,,,");
        //                        iccCalDataFile.Write("PID-" + dutID + ",,,,," + site + ",,,,,");
        //                        foreach (string testName in IccCalTestNames.All)
        //                        {
        //                            iccCalDataFile.Write(rawIccCalMsrDataDict[site, testName, dutID, run] + ",");
        //                        }
        //                        iccCalDataFile.WriteLine("");
        //                    }

        //                    if (dutIDtestedDead.Contains(dutID)) continue;

        //                } // dut loop

        //            } // site loop



        //            printSummary(iccCalDataFile);
        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Data saved to " + iccCalDataFilePath);
        //        allAnalysisFiles.Add(iccCalDataFilePath);
        //    }


        //    public static void WriteRefFinalData(string benchDataFilePath)
        //    {

        //        using (StreamWriter benchDataFile = new StreamWriter(new FileStream(benchDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            benchDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                benchDataFile.Write(testName + ",");
        //            }
        //            benchDataFile.WriteLine("");

        //            // write test numbers
        //            benchDataFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                benchDataFile.Write(testNumDict[testName] + ",");
        //            }

        //            benchDataFile.WriteLine("");

        //            // write units
        //            benchDataFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                benchDataFile.Write(unitsDict[testName] + ",");
        //            }

        //            benchDataFile.WriteLine("");

        //            // write high limits
        //            benchDataFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                benchDataFile.Write("1,");
        //            }
        //            benchDataFile.WriteLine("");

        //            // write low limits
        //            benchDataFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in testedTestNameList.Values)
        //            {
        //                benchDataFile.Write("-1,");
        //            }
        //            benchDataFile.WriteLine("");

        //            foreach (int dutID in dutIdLooseUserReducedList)
        //            {
        //                benchDataFile.Write("PID-" + dutID + ",,,,,,,,,,");
        //                foreach (string testName in testedTestNameList.Values)
        //                {
        //                    benchDataFile.Write(finalRefDataDict[selectedBatch, testName, dutID] + ",");
        //                }
        //                benchDataFile.WriteLine("");

        //            } // dut loop

        //            printSummary(benchDataFile);
        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Final Reference Data saved to " + benchDataFilePath);
        //        allAnalysisFiles.Add(benchDataFilePath);
        //    }


        //    public static void WriteRefDemoData(string demoDataFilePath, UnitType unitType)
        //    {

        //        using (StreamWriter demoDataFile = new StreamWriter(new FileStream(demoDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            demoDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoDataFile.Write(testName + ",");
        //            }
        //            demoDataFile.WriteLine("");

        //            // write test numbers
        //            demoDataFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoDataFile.Write(testNumDict[testName] + ",");
        //            }

        //            demoDataFile.WriteLine("");

        //            // write units
        //            demoDataFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoDataFile.Write(unitsDict[testName] + ",");
        //            }

        //            demoDataFile.WriteLine("");

        //            // write high limits
        //            demoDataFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoDataFile.Write("1,");
        //            }
        //            demoDataFile.WriteLine("");

        //            // write low limits
        //            demoDataFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoDataFile.Write("-1,");
        //            }
        //            demoDataFile.WriteLine("");

        //            foreach (int dutID in dutIdAllDemo[selectedBatch])
        //            {
        //                demoDataFile.Write("PID-" + dutID + ",,,,,,,,,,");
        //                foreach (string testName in benchTestNameList.Values)
        //                {
        //                    demoDataFile.Write(demoDataDict[selectedBatch, testName, unitType, dutID] + ",");
        //                }
        //                demoDataFile.WriteLine("");

        //            } // dut loop

        //            printSummary(demoDataFile);
        //        }  // Streamwriters

        //        if (unitType == UnitType.Demo)
        //            LogToLogServiceAndFile(LogLevel.HighLight, "Reference Demo Data saved to " + demoDataFilePath);
        //        else
        //            LogToLogServiceAndFile(LogLevel.HighLight, "Reference Pre-Demo Data saved to " + demoDataFilePath);

        //        allAnalysisFiles.Add(demoDataFilePath);
        //    }


        //    public static void WriteRefDemoOffsets(string demoOffsetFilePath)
        //    {

        //        using (StreamWriter demoOffsetFile = new StreamWriter(new FileStream(demoOffsetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            demoOffsetFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");
                    
        //            // write test names
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoOffsetFile.Write(testName + ",");
        //            }
        //            demoOffsetFile.WriteLine("");

        //            // write test numbers
        //            demoOffsetFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoOffsetFile.Write(testNumDict[testName] + ",");
        //            }

        //            demoOffsetFile.WriteLine("");

        //            // write units
        //            demoOffsetFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoOffsetFile.Write(unitsDict[testName] + ",");
        //            }

        //            demoOffsetFile.WriteLine("");

        //            // write high limits
        //            demoOffsetFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoOffsetFile.Write("1,");
        //            }
        //            demoOffsetFile.WriteLine("");

        //            // write low limits
        //            demoOffsetFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                demoOffsetFile.Write("-1,");
        //            }
        //            demoOffsetFile.WriteLine("");

        //            // write data
        //            foreach (int dutID in dutIdAllDemo[selectedBatch])
        //            {
        //                demoOffsetFile.Write("PID-" + dutID + ",,,,,,,,,,");
        //                foreach (string testName in benchTestNameList.Values)
        //                {
        //                    demoOffsetFile.Write(demoBoardOffsetsPerDut [selectedBatch, testName, dutID] + ",");
        //                }
        //                demoOffsetFile.WriteLine("");

        //            } // dut loop

        //            printSummary(demoOffsetFile);
        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "DemoBoard offsets saved to " + demoOffsetFilePath);
        //        allAnalysisFiles.Add(demoOffsetFilePath);
        //    }


        //    public static void WriteRefRepeatFile(string refRepeatFilePath)
        //    {

        //        using (StreamWriter refRepeatFile = new StreamWriter(new FileStream(refRepeatFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(refRepeatFile, "", "");

        //            refRepeatFile.WriteLine("\r\n\r\nREADME:");
        //            refRepeatFile.WriteLine("    Please include units in reference data file so that decibel values are ranked correctly.\r\n");
        //            refRepeatFile.WriteLine("    Higher Rank = better repeatability. Rank values are between +-10\r\n");
        //            refRepeatFile.WriteLine("    Formula:  DataRange = +- 2^(-Rank) * T\r\n");
        //            refRepeatFile.WriteLine("    If units are non-decibel, then T = 1% (of median value).");
        //            refRepeatFile.WriteLine("       Ranks are as follows: (extending to +-10)");
        //            refRepeatFile.WriteLine("       Rank    DataRange");
        //            refRepeatFile.WriteLine("       -3      +-8%");
        //            refRepeatFile.WriteLine("       -2      +-4%");
        //            refRepeatFile.WriteLine("       -1      +-2%");
        //            refRepeatFile.WriteLine("        0      +-1%");
        //            refRepeatFile.WriteLine("       +1      +-0.5%");
        //            refRepeatFile.WriteLine("       +2      +-0.25%");
        //            refRepeatFile.WriteLine("       +3      +-0.125%");

        //            refRepeatFile.WriteLine("\r\n    If units are decibel (dB/dBm/dBc), then T is a number between 0.1dB at [median = +30dB] and 0.4dB at [median = -70dB].");
        //            refRepeatFile.WriteLine("       Ranks are as follows: (extending to +-10) (for median value = 30dB, therefore T = 0.1dB)");
        //            refRepeatFile.WriteLine("       Rank    DataRange");
        //            refRepeatFile.WriteLine("       -3      +-0.8dB");
        //            refRepeatFile.WriteLine("       -2      +-0.4dB");
        //            refRepeatFile.WriteLine("       -1      +-0.2dB");
        //            refRepeatFile.WriteLine("        0      +-0.1dB");
        //            refRepeatFile.WriteLine("       +1      +-0.05dB");
        //            refRepeatFile.WriteLine("       +2      +-0.025dB");
        //            refRepeatFile.WriteLine("       +3      +-0.0125dB\r\n");
                    
        //            refRepeatFile.WriteLine("    * Asterisk indicates an outlier. Outliers are removed from the data.\r\n");

        //            foreach (int repRank in refDataRepeatabilityLog[selectedBatch].Keys)
        //            {
        //                refRepeatFile.WriteLine("\r\n");

        //                refDataRepeatabilityLog[selectedBatch][repRank].Sort();

        //                foreach (string msg in refDataRepeatabilityLog[selectedBatch][repRank])
        //                {
        //                    refRepeatFile.WriteLine("Rank " + repRank.ToString("+#;-#;0") + ", " + msg);
        //                }
        //            }

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Ref Data Repeatability Check saved to " + refRepeatFilePath);
        //        allAnalysisFiles.Add(refRepeatFilePath);
        //    }


        //    public static void WriteRefLooseDemoCorrCoeff(string refLooseDemoCorrCoeff)
        //    {
        //        using (StreamWriter vrfyCorrCoeffFile = new StreamWriter(new FileStream(refLooseDemoCorrCoeff, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        //        {
        //            printHeader(vrfyCorrCoeffFile, "", "");

        //            vrfyCorrCoeffFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

        //            // write test names
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(testName + ",");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write test numbers
        //            vrfyCorrCoeffFile.Write("Test#,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(testNumDict[testName] + ",");
        //            }

        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write units
        //            vrfyCorrCoeffFile.Write("Unit,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write(unitsDict[testName] + ",");
        //            }

        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write high limits
        //            vrfyCorrCoeffFile.Write("HighL,,,,,,,,,,");
        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write("1,");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write low limits
        //            vrfyCorrCoeffFile.Write("LowL,,,,,,,,,,");

        //            foreach (string testName in benchTestNameList.Values)
        //            {
        //                vrfyCorrCoeffFile.Write("-1,");
        //            }
        //            vrfyCorrCoeffFile.WriteLine("");

        //            // write data
        //            foreach (int site in sitesUserReducedList)
        //            {
        //                vrfyCorrCoeffFile.Write("999,,,,," + site + ",,,,,");
        //                foreach (string testName in benchTestNameList.Values)
        //                {
        //                    vrfyCorrCoeffFile.Write(demoLooseCorrCoeff[selectedBatch, testName] + ",");
        //                }
        //                vrfyCorrCoeffFile.WriteLine("");

        //            } // site loop

        //            printSummary(vrfyCorrCoeffFile);

        //        }  // Streamwriters

        //        LogToLogServiceAndFile(LogLevel.HighLight, "Verification Correlation Coefficients saved to " + refLooseDemoCorrCoeff);
        //        allAnalysisFiles.Add(refLooseDemoCorrCoeff);
        //    }


        //    public static void AddZipToZip(ZipFile zipToUpdate, string zipToAdd, string folder)
        //    {
        //        if (File.Exists(zipToAdd))
        //        {
        //            using (ZipFile zip1 = ZipFile.Read(zipToAdd))
        //            {
        //                foreach (ZipEntry z in zip1)
        //                {
        //                    MemoryStream stream = new MemoryStream();
        //                    z.Extract(stream);
        //                    stream.Seek(0, SeekOrigin.Begin);
        //                    zipToUpdate.AddEntry(folder + "\\" + z.FileName, stream);
        //                }
        //            }
        //        }
        //    }


        //}


        public enum UnitType
        {
            Loose,
            Demo
        }


        private class browserThread
        {
            private string title = "";
            private string file = "";

            public static string show(string caption)
            {
                browserThread bt = new browserThread(caption);
                Thread t = new Thread(bt.doWork);
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
                return bt.file;
            }

            private browserThread(string title)
            {
                this.title = title;
            }

            private void doWork()
            {   // this method used only for Lite Driver, to obtain Correlation File path

                OpenFileDialog browseFile = new OpenFileDialog();

                browseFile.Filter = "CSV Files (*.csv)|*.csv";
                string initDir = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TL_FULLPATH, "");
                if (initDir == "")
                {
                    initDir = "c:\\";
                }
                else
                {
                    initDir.Remove(initDir.LastIndexOf("\\"));
                }
                browseFile.InitialDirectory = initDir;
                browseFile.Title = title;

                if (browseFile.ShowDialog(wnd.ShowOnTop()) == DialogResult.OK)
                {
                    file = browseFile.FileName;
                }

            }

        }


        public class MessageBoxAsync
        {
            private static Thread msgBoxAsyncThread;
            private static object msgBoxAsyncLocker = new object();

            public static void Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
            {
                (new Thread(() => MessageBoxAsync2(message, title, buttons, icon, defaultButton))).Start();
                Thread.Sleep(10);   // helps ensure the correct order or appearance of message boxes
            }

            private static void MessageBoxAsync2(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
            {
                lock (msgBoxAsyncLocker)
                {
                    if (msgBoxAsyncThread != null) msgBoxAsyncThread.Join();

                    msgBoxAsyncThread = new Thread(() => MessageBox.Show(message, title, buttons, icon, defaultButton));
                    msgBoxAsyncThread.SetApartmentState(ApartmentState.STA);
                    msgBoxAsyncThread.Start();
                }
            }

            public static void Wait()
            {
                lock (msgBoxAsyncLocker)
                {
                    if (msgBoxAsyncThread != null) msgBoxAsyncThread.Join();
                }
            }
        }

        public static double getIccCalfactor(string testName, IccCalLoss inOrOut)
        {
            double calfactor = 0;
            int site = 1;

            if (!ENABLE_ICC_CAL | runningGUIccCal[siteNo])
            {
                return 0;
            }

            if (iccCalFactorRedirect.ContainsKey(testName + iccCalTestNameExtension))
            {
                calfactor = GuCalFactorsDict[site, iccCalFactorRedirect[testName + iccCalTestNameExtension] + inOrOut];
            }
            else
            {
                calfactor = GuCalFactorsDict[site, testName + iccCalTestNameExtension + inOrOut];
            }

            return calfactor;
        }


        public static double getGUcalfactor(int site, string testName)
        {
            if (runningGU & GuMode != GuModes.Vrfy) return 0;   // don't provide old correlation factors

            try
            {
                double calfactor = GuCalFactorsDict[site, testName];
                return calfactor;
            }
            catch
            {
                return 0;
            }
        }



        private static double pearsoncorr2(double[] x, double[] y, int n)
        {
            /*************************************************************************
            Pearson product-moment correlation coefficient

            Input parameters:
                X       -   sample 1 (array indexes: [0..N-1])
                Y       -   sample 2 (array indexes: [0..N-1])
                N       -   N>=0, sample size:
                            * if given, only N leading elements of X/Y are processed
                            * if not given, automatically determined from input sizes

            Result:
                Pearson product-moment correlation coefficient
                (zero for N=0 or N=1)

              -- ALGLIB --
                 Copyright 28.10.2010 by Bochkanov Sergey
            *************************************************************************/

            double result = 0;
            int i = 0;
            double xmean = 0;
            double ymean = 0;
            double v = 0;
            double x0 = 0;
            double y0 = 0;
            double s = 0;
            bool samex = new bool();
            bool samey = new bool();
            double xv = 0;
            double yv = 0;
            double t1 = 0;
            double t2 = 0;

            //ap.assert(n >= 0, "PearsonCorr2: N<0");
            //ap.assert(ap.len(x) >= n, "PearsonCorr2: Length(X)<N!");
            //ap.assert(ap.len(y) >= n, "PearsonCorr2: Length(Y)<N!");
            //ap.assert(apserv.isfinitevector(x, n), "PearsonCorr2: X is not finite vector");
            //ap.assert(apserv.isfinitevector(y, n), "PearsonCorr2: Y is not finite vector");

            //
            // Special case
            //
            if (n <= 1)
            {
                result = 0;
                return (double)result;
            }

            //
            // Calculate mean.
            //
            //
            // Additonally we calculate SameX and SameY -
            // flag variables which are set to True when
            // all X[] (or Y[]) contain exactly same value.
            //
            // If at least one of them is True, we return zero
            // (othwerwise we risk to get nonzero correlation
            // because of roundoff).
            //
            xmean = 0;
            ymean = 0;
            samex = true;
            samey = true;
            x0 = x[0];
            y0 = y[0];
            v = (double)1 / (double)n;
            for (i = 0; i <= n - 1; i++)
            {
                s = x[i];
                samex = samex & (double)(s) == (double)(x0);
                xmean = xmean + s * v;
                s = y[i];
                samey = samey & (double)(s) == (double)(y0);
                ymean = ymean + s * v;
            }
            if (samex | samey)
            {
                result = 0;
                return (double)result;
            }

            //
            // numerator and denominator
            //
            s = 0;
            xv = 0;
            yv = 0;
            for (i = 0; i <= n - 1; i++)
            {
                t1 = x[i] - xmean;
                t2 = y[i] - ymean;
                xv = xv + Math.Pow(t1, 2.0);
                yv = yv + Math.Pow(t2, 2.0);
                s = s + t1 * t2;
            }
            if ((double)(xv) == (double)(0) | (double)(yv) == (double)(0))
            {
                result = 0;
            }
            else
            {
                result = s / (Math.Sqrt(xv) * Math.Sqrt(yv));
            }
            return (double)result;
        }



        public class IccSearchCoreAlgo
        {
            private double TargetPout;
            private double Frequency;
            private double LossInput;
            private double LossAntPath;
            private int DelaySg;
            private string ModulationStd;
            private string Waveform;

            private string poutTestName;
            private string pinTestName;
            private string iccTestName;
            private string keyName;
            private bool ApplyIccTargetCorrection;

            public IccSearchCoreAlgo(double _TargetPout, double _Frequency, double _LossInput, double _LossOutput, int _DelaySg,
                string _ModulationMode, string _Waveform,
                string _poutTestName, string _pinTestName, string _iccTestName, string _keyName, bool _applyIccTargetCorrection)
            {
                this.TargetPout = _TargetPout;
                this.Frequency = _Frequency;
                this.LossInput = _LossInput;
                this.LossAntPath = _LossOutput;
                this.DelaySg = _DelaySg;
                this.ModulationStd = _ModulationMode;
                this.Waveform = _Waveform;

                this.poutTestName = _poutTestName;
                this.pinTestName = _pinTestName;
                this.iccTestName = _iccTestName;
                this.keyName = _keyName;
                this.ApplyIccTargetCorrection = _applyIccTargetCorrection;

            }

            public bool Execute(IinstrumentCalls InstrCalls, ref double Pin, ref double Pout, ref double Icc)
            {

                const double COARSE_DB_STEP = 1f;       // used for determining maximum input power for fine search
                const double FINE_DB_UPPER_MARGIN = 1f;       // add up to FINE_DB_UPPER_MARGIN to maximum input power for fine search
                const double IDD_TOLERANCE = 200f;      // servos to tolerance of target_idd/IDD_TOLERANCE
                const int MAX_SEARCH_ATTEMPTS = 10;        // will perform the entire SA search up to MAX_SEARCH_ATTEMPTS times until succeeds

                ApplyIccServoTargetCorrection[keyName + iccCalTestNameExtension] = ApplyIccTargetCorrection;
                if (iccCalFactorRedirect.ContainsKey(keyName + iccCalTestNameExtension)) return true;
                if (!factorAddEnabledTests.Contains(keyName + iccCalTestNameExtension) & IccCalTemplateExists) return true;

                if (!benchTestNameList.ContainsValue(iccTestName))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Icc Cal method was passed \"" + iccTestName + "\" but this test name is not found in the bench data file\n" + benchDataPath + "\n      Cannot continue " + GuModeNames[GuMode]);
                    runningGUIccCal[siteNo] = false;
                    runningGU = false;
                    forceReload = true;
                    return false;
                }
                if (!benchTestNameList.ContainsValue(pinTestName))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Icc Cal method was passed \"" + pinTestName + "\" but this test name is not found in the bench data file\n" + benchDataPath + "\n      Cannot continue " + GuModeNames[GuMode]);
                    runningGUIccCal[siteNo] = false;
                    runningGU = false;
                    forceReload = true;
                    return false;
                }
                if (!benchTestNameList.ContainsValue(poutTestName))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Icc Cal method was passed \"" + poutTestName + "\" but this test name is not found in the bench data file\n" + benchDataPath + "\n      Cannot continue " + GuModeNames[GuMode]);
                    runningGUIccCal[siteNo] = false;
                    runningGU = false;
                    forceReload = true;
                    return false;
                }
                if (IccCalTemplateExists & !iccCalTemplateTestNameList.Contains(keyName + iccCalTestNameExtension))
                {
                    LogToLogServiceAndFile(LogLevel.Error, "ERROR: Icc Cal method was passed \"" + poutTestName + "\" but " + poutTestName + iccCalTestNameExtension + " is not found in the Icc Cal Template file\n" + iccCalTemplatePath + "\n      Cannot continue " + GuModeNames[GuMode]);
                    runningGUIccCal[siteNo] = false;
                    runningGU = false;
                    forceReload = true;
                    return false;
                }

                double benchIcc = GU.finalRefDataDict[selectedBatch, iccTestName, currentGuDutSN];
                if (ApplyIccTargetCorrection) benchIcc += IccServoTargetCorrection[1, keyName + iccCalTestNameExtension];
                double benchPin = GU.finalRefDataDict[selectedBatch, pinTestName, currentGuDutSN];
                double benchPout = GU.finalRefDataDict[selectedBatch, poutTestName, currentGuDutSN];

                IccCalTestNames.Add(pinTestName, poutTestName, iccTestName, keyName, TargetPout, Frequency, ModulationStd + "-" + Waveform);

                if (!IccCalTemplateExists)
                {
                    Pin = benchPin;
                    Pout = benchPout;
                    Icc = benchIcc;
                    GuIccCalFailed[sitesUserReducedList[currentGuSiteIndex]] = true;
                    return true;
                }

                double iccTolerance = benchIcc / IDD_TOLERANCE;
                double rfLvl = 0;
                bool coarseSearchDone = false;
                bool servoPassed = false;

                // BEGIN SEARCH ATTEMPTS LOOP.  Each search attempt performs a coarse and fine search with slightly different boundaries, to maximize servo success
                for (int searchAttempt = 0; searchAttempt < MAX_SEARCH_ATTEMPTS; searchAttempt++)
                {
                    // COARSE DETERMINATION OF HI POWER LIMIT    (ensures that we don't compress the part during fine search)
                    coarseSearchDone = false;
                    double pinLoLim = benchPin - 8f;  // P-in starts out this low
                    double pinHiLim = benchPin + 5f;  // P-in could potentially go this high, but not likely since P-in stops increasing once measured Icc exceeds bench Icc

                    for (int coarseIndex = 0; coarseIndex < (pinHiLim - pinLoLim) / COARSE_DB_STEP + 1; coarseIndex++)
                    {
                        rfLvl = pinLoLim + coarseIndex * COARSE_DB_STEP + searchAttempt / MAX_SEARCH_ATTEMPTS * 0.9f;

                        InstrCalls.SetPowerLevel(rfLvl + LossInput);

                        Thread.Sleep(DelaySg);

                        Icc = InstrCalls.MeasureIcc();

                        if (Icc > benchIcc + iccTolerance)
                        {
                            pinHiLim = rfLvl + (double)searchAttempt / (double)MAX_SEARCH_ATTEMPTS * FINE_DB_UPPER_MARGIN;
                            pinLoLim = rfLvl - COARSE_DB_STEP * 3;
                            coarseSearchDone = true;
                            break;
                        }
                    }  // coarse precision loop

                    if (!coarseSearchDone) break;

                    // FINE ICC SEARCH    (within icc_tolerance)
                    double previousIcc = 0;
                    double previousRFLvl = 0f;
                    double iccPinSlope = 0f;

                    for (int iteration = 1; iteration < 50; iteration++)
                    {
                        previousRFLvl = rfLvl;
                        previousIcc = Icc;

                        if (iteration == 1)
                        {
                            rfLvl = pinHiLim - 2f;
                        }
                        else if (iteration == 2)
                        {
                            rfLvl = pinHiLim - 1f;
                        }
                        else
                        {
                            rfLvl = rfLvl + (double)(benchIcc - Icc) * 0.8f / (iccPinSlope);   // *0.8 helps avoid overshooting beyong limits
                            if (rfLvl < pinLoLim | rfLvl > pinHiLim)
                            {
                                break;  // P-in exceeded limits, try next search attempt
                            }
                        }

                        InstrCalls.SetPowerLevel(rfLvl + LossInput);

                        Thread.Sleep(DelaySg);

                        Icc = InstrCalls.MeasureIcc();

                        if (Math.Abs(Icc - benchIcc) <= iccTolerance)
                        {
                            servoPassed = true;
                            break;
                        }

                        if (iteration > 1)
                        {
                            iccPinSlope = (double)(Icc - previousIcc) / (rfLvl - previousRFLvl);
                        }

                    }  // fine precision loop

                    if (servoPassed) break;  // conclude search attempts

                }  // search attempts (includes coarse and fine precision searches)

                //This is the line to comment out for disabling the plot being displayed.
                //Calc.PinSweep.AnalyzePinSweep_ShowPlot(this, false);

                Pout = InstrCalls.MeasurePout();

                Pout += LossAntPath;

                Pin = rfLvl;

                if (!servoPassed)
                {
                    LogToLogServiceAndFile(LogLevel.Warn, "Icc search failed, test " + poutTestName + ", target Icc: " + benchIcc + "A, measured Icc: " + Icc + "A");
                    GuIccCalFailed[sitesUserReducedList[currentGuSiteIndex]] = true;   
                }

                return servoPassed;

            }

            public interface IinstrumentCalls
            {
                double MeasurePout();
                double MeasureIcc();
                void SetPowerLevel(double powerLevel);
            }
        }


        public class Dictionary   // extend the Dictionary class to include double-key, triple-key, and quadruple key dictionaries
        {
            public class DoubleKey<T1, T2, V> : Dictionary<T1, Dictionary<T2, V>>   // the <> denote a user-defined generic collection.
            {         //We see data type variables T1, T2, and V instead of data types. This get replaced during compilation with whatever types were designated at instantiation code.

                public DoubleKey()
                {
                }

                public DoubleKey(DoubleKey<T1, T2, V> copyFromDict)   // for copying a DoubleKeyDictionary into another DoubleKeyDictionary
                {
                    foreach (T1 key1 in copyFromDict.Keys)    // this is necessary so that inner dictionaries don't share memory with copyFromDict's inner dictionaries
                    {
                        this[key1] = new Dictionary<T2, V>(copyFromDict[key1]);
                    }
                }

                public V this[T1 key1, T2 key2]      // error-safe 2D indexer (provides index index ability for objects), returns type V
                {
                    get
                    {
                        V outv = default(V);
                        if (this.ContainsKey(key1))
                        {
                            if (this[key1].ContainsKey(key2))
                            {
                                outv = this[key1][key2];
                            }
                        }
                        return outv;   // returns the private dictionary value at the requested keys
                    }
                    set
                    {
                        if (!this.ContainsKey(key1))
                        {
                            this[key1] = new Dictionary<T2, V>();
                        }
                        this[key1][key2] = value;    // set the value at the requested keys, will overwrite existing key & value if present. Note that .Add method generates fault if key exists
                    }
                }
            }


            public class TripleKey<T1, T2, T3, V> : Dictionary<T1, DoubleKey<T2, T3, V>> // the <> denote a user-defined generic collection.
            {         //We see data type variables T1, T2, T3 and V instead of data types. This get replaced during compilation with whatever types were designated at instantiation code.

                public TripleKey()
                {
                }

                public TripleKey(TripleKey<T1, T2, T3, V> copyFromDict)   // for copying a TripleKeyDictionary into another TripleKeyDictionary
                {
                    foreach (T1 key1 in copyFromDict.Keys)    // this is necessary so that inner dictionaries don't share memory with copyFromDict's inner dictionaries
                    {
                        this[key1] = new DoubleKey<T2, T3, V>(copyFromDict[key1]);
                    }
                }

                public V this[T1 key1, T2 key2, T3 key3]      // error-safe indexer (provides index index ability for objects), returns type V
                {
                    get
                    {
                        V outv = default(V);
                        if (this.ContainsKey(key1))
                        {
                            outv = this[key1][key2, key3];
                        }
                        return outv;   // returns the private dictionary value at the requested keys
                    }
                    set
                    {
                        if (!this.ContainsKey(key1))
                        {
                            this[key1] = new DoubleKey<T2, T3, V>(); //Dictionary<T2, Dictionary<T3, V>>();   // create the new inner-dictionary if it doesn't already exist
                        }
                        this[key1][key2, key3] = value;    // set the value at the requested keys, will overwrite existing key & value if present. Note that .Add method generates fault if key exists
                    }
                }
            }


            public class QuadKey<T1, T2, T3, T4, V> : Dictionary<T1, TripleKey<T2, T3, T4, V>>   // the <> denote a user-defined generic collection.
            {         //We see data type variables T1, T2, T3, T4 and V instead of data types. This get replaced during compilation with whatever types were designated at instantiation code.

                public QuadKey()
                {
                }

                public QuadKey(QuadKey<T1, T2, T3, T4, V> copyFromDict)   // for copying a QuadKeyDictionary into another QuadKeyDictionary
                {
                    foreach (T1 key1 in copyFromDict.Keys)    // this is necessary so that inner dictionaries don't share memory with copyFromDict's inner dictionaries
                    {
                        this[key1] = new TripleKey<T2, T3, T4, V>(copyFromDict[key1]);
                    }
                }

                public V this[T1 key1, T2 key2, T3 key3, T4 key4]      // error-safe indexer (provides index index ability for objects), returns type V
                {
                    get
                    {
                        V outv = default(V);
                        if (this.ContainsKey(key1))
                        {
                            outv = this[key1][key2, key3, key4];
                        }
                        return outv;   // returns the private dictionary value at the requested keys
                    }
                    set
                    {
                        if (!this.ContainsKey(key1))
                        {
                            this[key1] = new TripleKey<T2, T3, T4, V>(); //Dictionary<T2, Dictionary<T3, Dictionary<T4, V>>>();   // create the new inner-dictionary if it doesn't already exist
                        }
                        this[key1][key2, key3, key4] = value;    // set the value at the requested keys, will overwrite existing key & value if present. Note that .Add method generates fault if key exists
                    }
                }
            }

        }

    }   // class



    public class WindowControl
    {
        // example calls
        // static WindowControl wnd = new WindowControl();
        // wnd.MinimizeExcel();
        // MessageBox.Show(wnd.ShowOnTop(), "this message should always show up on top");
        // if (wnd.usingLiteDriver) {}

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public WindowControl()
        {
            string ownerProcName = "";

            //check if using Lite Driver
            string file_pathname = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string[] dllPathSplit = file_pathname.Split('\\');
            if (dllPathSplit[2] != "System")
            {
                _usingLiteDriver = true;
                ownerProcName = "devenv";  // Visual Studio will be the owner window
            }
            else
            {
                _usingLiteDriver = false;
                ownerProcName = "Avago.ATF.UIs";  // Clotho will be the owner window
            }

            // grab owner handle
            Process[] ownerProc = Process.GetProcessesByName(ownerProcName);  // gets handle of owner window   
            if (ownerProc.Count() != 0)
            {
                ownerMainHandle = ownerProc[0].MainWindowHandle;
                ownerMainWindow = new ConvertHandleToIWin32Window(ownerMainHandle);
            }
        }

        private static bool _usingLiteDriver;
        private static IntPtr ownerMainHandle;
        private static IWin32Window ownerMainWindow;

        public bool usingLiteDriver
        {
            get { return _usingLiteDriver; }
        }

        public void MinimizeExcel()
        {
            ShowWindowAsync(FindWindow("XLMAIN", null), 6);  // minimize Excel
        }

        public IWin32Window ShowOnTop()
        {
            SetForegroundWindow(ownerMainHandle);  // bring owner window to foreground
            Thread.Sleep(5); // this is necessary so that form doesn't launch before owner window takes foreground. The delay doesn't matter for production, since we never use message boxes or forms during test.
            return (ownerMainWindow);  // return the IWin32Window of owner window, so messageboxes and forms always show up on top of other windows
        }

        private class ConvertHandleToIWin32Window : System.Windows.Forms.IWin32Window
        {
            public ConvertHandleToIWin32Window(IntPtr handle)
            {
                _hwnd = handle;
            }

            public IntPtr Handle
            {
                get { return _hwnd; }
            }

            private IntPtr _hwnd;
        }

    }


}



