#define LOCAL_GUDB

using Avago.ATF.LogService;
using Avago.ATF.SchemaTypes;
using Avago.ATF.Shares;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Validators;
using ClothoSharedItems;
//using EqLib;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AvagoGU
{
    public partial class DataAnalysis 
    {
        public static bool isMagicBox = false;
        public static bool isDCsubstrate = false;
        public static string calibrationDirectoryName = "";

        public static class DataAnalysisFiles
        {
            public class AddOnInformation
            {
                public int V;
                public int GUBatch;
                public string CF_File;
                public string ContactorID;

                public AddOnInformation(int version)
                {
                    V = version;
                }
            }

            public static string computerName;
            public static string testPlanVersion;
            public static string dibID;
            public static string[] dibIDArray;
            public static string handlerSN;
            public static int handlerAddress;
            public static string lotID;
            public static string sublotID;
            public static string opID;
            public static string waferID;
            public static string testPlanName;
            public static string fileNameRoot;
            public static string calOrVrfy;
            public static string guDataDir;
            public static string guDataRemoteDir;

            public static string closingTimeCodeDatabaseFriendly;   // 11-Aug-2014 JJ Low
            public static string closingTimeCodeHumanFriendly;  // 11-Aug-2014 JJ Low
            public static string closingTimeCodeGalaxyFriendly; // 11-Aug-2014 JJ Low
            public static string stdResultFileName; // 11-Aug-2014 JJ Low
            public static string resultFilePath;    // 11-Aug-2014 JJ Low
            public static string remoteSharePath;   // 11-Aug-2014 JJ Low
            public static string contactorID;
            public static string[] contactorIDArray;
            public static string ipAddress;
            public static string InstrumentInfo;

            public static string zipFilePath;
            public static List<string> allAnalysisFiles = new List<string>();   //

            private const string IccCalAnalysisDir = "2_IccCalAnalysis";
            private const string CorrAnalysisDir = "3_CorrAnalysis";
            private const string VerifyAnalysisDir = "4_VerifyAnalysis";
            private const string DCAnalysisDir = "5_DCSubstrateAnalysis";
            private const string RefDataDir = "1_RefDataAnalysis";

            public static Dictionary<int, SortedList<int, List<string>>> refDataRepeatabilityLog = new Dictionary<int, SortedList<int, List<string>>>();

            public static void WriteAll()
            {
                try
                {
                    // Generate header info

                    DateTime datetime = DateTime.Now;   // 11-Aug-2014 JJ Low

                    closingTimeCodeDatabaseFriendly = string.Format("{0:yyyy-MM-dd_HH:mm:ss}", datetime);   // 11-Aug-2014 JJ Low
                    closingTimeCodeHumanFriendly = string.Format("{0:yyyy-MM-dd_HH.mm.ss}", datetime);  // 11-Aug-2014 JJ Low
                    closingTimeCodeGalaxyFriendly = string.Format("{0:yyyy_M_d H:m:s}", datetime);  // 11-Aug-2014 JJ Low
                    contactorID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_CONTACTOR_ID, ""); // 11-Aug-2014 JJ Low
                    contactorIDArray = contactorID.Split('_');

                    computerName = System.Environment.MachineName;
                    testPlanVersion = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TP_VER, "");
                    dibID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_DIB_ID, "");
                    dibIDArray = dibID.Split('_');
                    handlerSN = ClothoDataObject.Instance.ATFConfiguration.UserSection.GetValue("HandlerSN");
                    //handlerSN = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_HANDLER_SN, "");
                    lotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
                    sublotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_SUB_LOT_ID, "");
                    opID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_OP_ID, "");
                    waferID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_WAFER_ID, "");
                    string testerIdn = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_ID, "");
                    if (testerIdn == "") testerIdn = "IM001-01";
                    int siteno = Convert.ToInt32(testerIdn.Substring((testerIdn.Length - 1), 1));
                    string siteNoStr = "_Site" + siteno;
                    //string siteNoStr = (Eq.NumSites > 1) ? string.Format("_Site{0}", GU.siteNo + 1) : "";
                    testPlanName = Path.GetFileNameWithoutExtension(GU.GetTestPlanPath().TrimEnd('\\')) + siteNoStr;
                    ipAddress = GetLocalIPAddress();
                    InstrumentInfo = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_INSTRUMENT_INFO, "");
                    if (int.TryParse(ClothoDataObject.Instance.ATFConfiguration.UserSection.GetValue("HandlerAddress"), out int iHandlerAddr)) handlerAddress = iHandlerAddr;
                    else handlerAddress = 1;

                    fileNameRoot = testPlanName + "_" + GU.CorrFinishTimeHumanFriendly;

                    if (GU.GuMode == GU.GuModes.CorrVrfy)
                    {
                        calOrVrfy = GU.GuModes.CorrVrfy.ToString();
                    }
                    else
                    {
                        calOrVrfy = GU.GuModes.Vrfy.ToString();
                    }

                        //calOrVrfy =
                        ////(GuMode.Contains(GuModes.IccCorrVrfy) ? GuModes.IccCorrVrfy :
                        //(GuMode.Contains(GuModes.CorrVrfy) ? GuModes.CorrVrfy :
                        //    GuModes.Vrfy).ToString();


                    string passFailIndicator = "";
                    //if (GuMode.Contains(GuModes.IccCorrVrfy)) passFailIndicator += GuIccCalFailed.Contains(true) ? "F" : "P";
                    //if (GuMode.Contains(GuModes.IccCorrVrfy) | GuMode.Contains(GuModes.CorrVrfy)) passFailIndicator += GuCorrFailed.Contains(true) ? "F" : "P";
                    //if (GuMode.Contains(GuModes.CorrVrfy)) passFailIndicator += GuCorrFailed.Contains(true) ? "F" : "P";
                    if (GU.GuMode == GU.GuModes.IccCorrVrfy) passFailIndicator += GU.GuIccCalFailed.ContainsValue(true) ? "F" : "P";
                    if (GU.GuMode == GU.GuModes.IccCorrVrfy | GU.GuMode == GU.GuModes.CorrVrfy) passFailIndicator += GU.GuCorrFailed.ContainsValue(true) ? "F" : "P";
                    passFailIndicator += GU.GuVerifyFailed.ContainsValue(true) ? "F" : "P";

                    guDataDir = @"C:/Avago.ATF.Common.x64/AutoGUcalResults/" + GU.CorrFinishTimeHumanFriendly + "_" + testPlanName + "_" + calOrVrfy + "_" + passFailIndicator + @"/";

                    allAnalysisFiles.Clear();

                    if (!Directory.Exists(guDataDir + RefDataDir)) Directory.CreateDirectory(guDataDir + RefDataDir);
                    WriteRefFinalData(guDataDir + RefDataDir + "/GuRefFinalData_" + fileNameRoot + ".csv");
                    WriteRefDemoData(guDataDir + RefDataDir + "/GuRefDemoData_" + fileNameRoot + ".csv", GU.UnitType.Demo);
                    WriteRefDemoData(guDataDir + RefDataDir + "/GuRefPreDemoData_" + fileNameRoot + ".csv", GU.UnitType.Loose);
                    WriteRefDemoOffsets(guDataDir + RefDataDir + "/GuRefDemoOffsets_" + fileNameRoot + ".csv");
                    WriteRefRepeatFile(guDataDir + RefDataDir + "/GuRefRepeatability_" + fileNameRoot + ".txt");
                    WriteRefLooseDemoCorrCoeff(guDataDir + RefDataDir + "/GuRefLooseDemoCorrCoeff_" + fileNameRoot + ".csv");

                    //if (GuMode.Contains(GuModes.IccCorrVrfy))
                    //{
                    //    if (!Directory.Exists(guDataDir + IccCalAnalysisDir)) Directory.CreateDirectory(guDataDir + IccCalAnalysisDir);
                    //    WriteIccCalfactor(guDataDir + IccCalAnalysisDir + "/GuIccCalFactor_" + fileNameRoot + ".csv");
                    //    WriteIccCalData(guDataDir + IccCalAnalysisDir + "/GuIccCalData_" + fileNameRoot + ".csv");
                    //    WriteIccAvgError(guDataDir + IccCalAnalysisDir + "/GuIccAvgVrfyError_" + fileNameRoot + ".csv");
                    //}

                    //if (GuMode.Contains(GuModes.IccCorrVrfy) || GuMode.Contains(GuModes.CorrVrfy))
                    if (GU.GuMode == GU.GuModes.CorrVrfy)
                    {
                        if (!Directory.Exists(guDataDir + CorrAnalysisDir)) Directory.CreateDirectory(guDataDir + CorrAnalysisDir);
                        WriteCorrRawData(guDataDir + CorrAnalysisDir + "/GuCorrRawData_" + fileNameRoot + ".csv");
                        WriteCorrFactor(guDataDir + CorrAnalysisDir + "/GuCorrFactor_" + fileNameRoot + ".csv");
                        WriteCorrFactorNoDemo(guDataDir + CorrAnalysisDir + "/GuCorrFactorNoDemoOffset_" + fileNameRoot + ".csv");
                    }

                    if (!Directory.Exists(guDataDir + VerifyAnalysisDir)) Directory.CreateDirectory(guDataDir + VerifyAnalysisDir);
                    WriteRawData(guDataDir + VerifyAnalysisDir + "/GuRawData_" + fileNameRoot + ".csv");
                    WriteVrfyData(guDataDir + VerifyAnalysisDir + "/GuVrfyData_" + fileNameRoot + ".csv");
                    WriteVrfyError(guDataDir + VerifyAnalysisDir + "/GuVrfyError_" + fileNameRoot + ".csv");
                    WriteCorrCoeff(guDataDir + VerifyAnalysisDir + "/GuCorrCoeff_" + fileNameRoot + ".csv");

                    WriteLogFile(guDataDir + "/GuLogPrintout_" + fileNameRoot + ".txt");

                    //if (isDCsubstrate)
                    //{
                    //    if (!Directory.Exists(guDataDir + DCAnalysisDir)) Directory.CreateDirectory(guDataDir + DCAnalysisDir);
                    //    GU.WriteDCsubstrateRawData(guDataDir + DCAnalysisDir + "/DcSubstrate_" + fileNameRoot + ".csv");
                    //}

                    AddOnInformation adi = new AddOnInformation(1);
                    ///adi.ContactorID = contactorIDArray[0];
                    adi.ContactorID = "nan";
                    adi.CF_File = Path.GetFileName(ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, ""));
                    adi.GUBatch = GU.selectedBatch;
                    WriteAddOnInfoLogFile(guDataDir + "/GuAddOnInfo_" + fileNameRoot + ".adi", adi);

                    // track cable cal info of this gu cal - JJ Low
                    if (isMagicBox == true)
                    {
                        string magicbox_adi_folder = @"D:/ExpertCalSystem.Data/MagicBox/" + calibrationDirectoryName + @"/Data.Current/";

                        if (Directory.Exists(magicbox_adi_folder))
                        {
                            if (File.Exists(magicbox_adi_folder + @"MagicBoxMetaData.adi"))
                            {
                                File.Copy(magicbox_adi_folder + @"MagicBoxMetaData.adi", guDataDir + "GuMagicBoxMetaData_" + fileNameRoot + ".adi");

                                allAnalysisFiles.Add(guDataDir + "GuMagicBoxMetaData_" + fileNameRoot + ".adi");
                            }
                        }
                    }

                    //zip everything up for convenience
                    zipFilePath = ipAddress == "" ?
                        (guDataDir + GU.CorrFinishTimeHumanFriendly + "_" + testPlanName + "_" + calOrVrfy + "_" + passFailIndicator + "_" + RandomString(7) + ".zip") :
                        (guDataDir + "IP" + ipAddress + "_" + GU.CorrFinishTimeHumanFriendly + "_" + testPlanName + "_" + calOrVrfy + "_" + passFailIndicator + "_" + RandomString(7) + ".zip");

                    using (ZipFile zip = new ZipFile(zipFilePath))
                    {
                        foreach (string file in allAnalysisFiles)
                        {
                            string dir = (Path.GetDirectoryName(file) == Path.GetDirectoryName(guDataDir)) ? "" : Path.GetFileName(Path.GetDirectoryName(file));

                            zip.AddFile(file, dir);
                        }

                        // Add previous Correlation and Icc Cal factor file
                        if (File.Exists(GU.correlationFilePath))
                            zip.AddFile(GU.correlationFilePath, "PreviousProgramFactorFiles");
                        else
                            zip.AddEntry("PreviousProgramFactorFiles\\NoPreviousCorrFactorFile.txt", "");
                        if (File.Exists(GU.iccCalFilePath))
                            zip.AddFile(GU.iccCalFilePath, "PreviousProgramFactorFiles");
                        else
                            zip.AddEntry("PreviousProgramFactorFiles\\NoPreviousIccCalFactorFile.txt", "");

                        zip.AddFile(GU.benchDataPath, RefDataDir);

                        AddZipToZip(zip, @"C:\Avago.ATF.Common\Results\ProgramReport.zip", "ProgramReport");

                        zip.Save();
                    }

                    if (Directory.Exists(guDataRemoteDir)) File.Copy(zipFilePath, guDataRemoteDir + "\\" + Path.GetFileNameWithoutExtension(zipFilePath) + ".gucal");

                    // 25-Sept-2014 JJ Low
                    DateTime currentDateTime = DateTime.Now;

                    string dutInBatch = string.Join("+", GU.dutIdAllLoose[GU.selectedBatch]);

                    stdResultFileName = string.Format("{0}{1}_{2}_{3}_{4:yyyyMMdd_HHmmss}_{5}_{6}.csv",
                        GU.prodTag,
                        calOrVrfy,
                        GU.selectedBatch,
                        dutInBatch,
                        currentDateTime,
                        (ipAddress == null || ipAddress.Length == 0) ? "IP" : string.Format("IP{0}", ATFRTE.Instance.IPAddress),
                        UnisysTimestampEncoder.GetUnisysEncodeTimestamp(currentDateTime)
                        ).ToUpper();

                    WriteStdResultFile(stdResultFileName, GU.selectedBatch.ToString(), dutInBatch);

#if LOCAL_GUDB
                    
                    if (ClothoDataObject.Instance.LOCAL_GUDB_Enable)
                    {
                        // package_name : AFEM-8230-AP1-RF1_BE-PXI-NI_v0012
                        // gucal_filename: IP172.16.7.149_2022-05-08_17.38.56_ACFM-WH13-AP1-RF1_BE-ZNB_V0028_GuCorrVrfy_PF_WIML8I6.gucal
                        // total_param_count : TOTAL_TESTS
                        // attempt_count : <CorrelationFailures>9</CorrelationFailures>
                        // tester_name : computer name
                        // handler_name : SJHandlerSim1Site02
                        // product_tag  : product_tag

                        LocalGUdatabase.GUsqlite Db = new LocalGUdatabase.GUsqlite();

                        string Gucal_filename = Path.GetFileNameWithoutExtension(zipFilePath) + ".gucal";

                        Db.GUwriter.OpenDB();

                        Db.GUwriter.GenerateNewGUattempt(GU.prodTag);

                        //foreach (int site in runningGU.AllIndexOf(true))
                        foreach (int site in GU.sitesUserReducedList)
                        {
                            int attempt_count = cDatabase.Get_Vrfy_NumTries(GU.prodTag);     //thisProductsGuStatus[site].verificationFailures;
                            int CorrErrorCount = cDatabase.Get_Corr_NumTries(GU.prodTag);   //DicCorrError.Count();
                            int VerifyErrorCount = 0;
                            int Total_ParaCount = GU.testedTestNameList.Count();
                            bool Flag = true;

                            Db.GUwriter.InsertGUSummary(GU.prodTag,
                               Gucal_filename,
                               Total_ParaCount,
                               attempt_count,
                               (GU.GuMode == GU.GuModes.CorrVrfy) ? LocalGUdatabase.GUsqlite.GUType.GUCorrVrfy : LocalGUdatabase.GUsqlite.GUType.GUVrfy,
                               computerName,
                               handlerSN,
                               handlerAddress
                               );

                            Db.GUwriter.InsertGUCorrSummary(GU.selectedBatch, CorrErrorCount, CorrErrorCount == 0 ? true : false);

                            foreach (int Para_Num in GU.testedTestNameList.Keys)
                            {
                                string Parameter = GU.testedTestNameList[Para_Num];

                                bool Status_GU_CorrFactor = GU.DicCorrError.ContainsKey(Parameter) == true ? false : true;
                                bool Status_Verify_VerificationFactor = false;

                                bool Status_Corr = GU.GuCorrFailed[1] == true ? false : true;
                                bool Status_Verify = GU.GuVerifyFailed[1] == false ? true : false;

                                double GU_Corr_Raw_Data_Value = 0f;
                                double GU_CF_Factor_Value = GU.GuCalFactorsDict[1, Parameter];

                                double GU_CF_Upper_Limit = GU.hiLimCalMultiplyDict[GU.testedTestNameList[Para_Num]];
                                double GU_CF_Lower_Limit = GU.loLimCalMultiplyDict[GU.testedTestNameList[Para_Num]];

                                double GU_Verify_Upper_Limit = GU.hiLimVrfyDict[GU.testedTestNameList[Para_Num]];
                                double GU_Verify_Lower_Limit = GU.loLimVrfyDict[GU.testedTestNameList[Para_Num]];

                                double GU_Verify_Final_Ref_Value = 0f;
                                double GU_Verify_Error_Value = 0f;

                                double Measureddatawithoffset = 0f;

                                LocalGUdatabase.GUsqlite.CFType CF_Type = GU.factorMultiplyEnabledTests.Contains(Parameter) == true ? LocalGUdatabase.GUsqlite.CFType.Multiply : LocalGUdatabase.GUsqlite.CFType.Add;

                                Db.GUwriter.InsertGuCorrFactor(GU_CF_Factor_Value,
                                    Status_GU_CorrFactor,
                                    Parameter,
                                    CF_Type);

                                foreach (int dutID in GU.dutIdLooseUserReducedList)
                                {
                                    Status_Verify_VerificationFactor = GU.DicVerifyError[dutID].ContainsKey(Parameter) == true ? false : true;
                                    GU_Verify_Final_Ref_Value = GU.finalRefDataDict[GU.selectedBatch, Parameter, dutID];
                                    GU_Corr_Raw_Data_Value = GU.rawAllCorrDataDict[siteno, GU.testedTestNameList[Para_Num], dutID];
                                    Measureddatawithoffset = GU.correctedMsrDataDict[1, Parameter, dutID];
                                    GU_Verify_Error_Value = GU.correctedMsrErrorDict[1, Parameter, dutID];

                                    VerifyErrorCount = GU.DicVerifyError[dutID].Count();

                                    Db.GUwriter.InsertGuCorrRawData(Para_Num,
                                       Parameter,
                                       GU_Corr_Raw_Data_Value,
                                       GU_CF_Upper_Limit,
                                       GU_CF_Lower_Limit,
                                       dutID,
                                       GU_Verify_Final_Ref_Value,
                                       Status_GU_CorrFactor);

                                    Db.GUwriter.InserGuVerifyRawData(Para_Num,
                                        Parameter,
                                        Measureddatawithoffset,
                                        GU_Verify_Upper_Limit,
                                        GU_Verify_Lower_Limit,
                                        dutID,
                                        GU_Verify_Final_Ref_Value,
                                        GU_CF_Factor_Value,
                                        GU_Verify_Error_Value,
                                        Status_Verify_VerificationFactor);

                                    if (Flag)
                                    {
                                        Db.GUwriter.InsertGuPareto(dutID, VerifyErrorCount);

                                        Db.GUwriter.InsertGuVerifySummary(dutID,
                                                               GU.selectedBatch,
                                                               VerifyErrorCount,
                                                               Status_Verify);
                                    }
                                }
                                Flag = false;
                            }
                        }

                        Db.GUwriter.Commit();
                        //}
#endif
                        GU.DicVerifyError = new Dictionary<int, Dictionary<string, GU.VerifyError>>();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error while saving GU analysis files\n\n" + e.ToString());
                }
            }

            private static string RandomString(int length)
            {
                string randomString = "";

                Random r = new Random();

                for (int i = 0; i < length; i++)
                {
                    int randomByte = r.Next(48, 90);
                    while (randomByte >= 58 && randomByte <= 64) randomByte = r.Next(48, 90);   // avoid unwanted characters

                    randomString += Convert.ToChar(randomByte);
                }

                return randomString;
            }

            private static void printHeader(StreamWriter sw, string startTime, string finishTime)
            {
                sw.WriteLine("--- Global Info:");
                sw.WriteLine("Date," + finishTime);
                sw.WriteLine("StartTime," + startTime);
                sw.WriteLine("FinishTime," + finishTime);
                sw.WriteLine("TestPlanVersion," + testPlanVersion);
                sw.WriteLine("Product," + GU.prodTag);
                sw.WriteLine("TestPlan," + testPlanName + ".cs");
                sw.WriteLine("Lot," + lotID);
                sw.WriteLine("Sublot," + sublotID);
                sw.WriteLine("Wafer," + waferID);
                sw.WriteLine("TesterName," + computerName);
                sw.WriteLine("TesterIPaddress," + ipAddress);
                sw.WriteLine("Operator," + opID);
                sw.WriteLine("Handler ID," + handlerSN);
                sw.WriteLine("LoadBoardName," + dibIDArray[GU.siteNo -1]);
                sw.WriteLine("ContactorID," + contactorIDArray[GU.siteNo -1]);
                sw.WriteLine("InstrumentInfo," + InstrumentInfo);
            }

            private static void printSummary(StreamWriter sw)
            {
                sw.WriteLine();

                //foreach (int site in GU.runningGU.AllIndexOf(true))
                foreach (int site in GU.sitesUserReducedList)
                {
                    sw.WriteLine("\n");

                    //if (GuMode[site] == GuModes.IccCorrVrfy)
                    //{
                    //    if (!GuIccCalFailed[site])
                    //    {
                    //        //sw.WriteLine("\n\n#Site " + (site + 1) + " GU Icc Calibration PASSED");
                    //        sw.WriteLine("#GU Icc Calibration Summary,PASSED");
                    //    }
                    //    else
                    //    {
                    //        //sw.WriteLine("\n\n#Site " + (site + 1) + " GU Icc Calibration FAILED");
                    //        sw.WriteLine("#GU Icc Calibration Summary,FAILED");
                    //    }
                    //}

                    //if (GuMode[site] == GuModes.IccCorrVrfy || GuMode[site] == GuModes.CorrVrfy)
                    if (GU.GuMode == GU.GuModes.CorrVrfy)
                    {
                        if (!GU.GuCorrFailed[site])
                        {
                            //sw.WriteLine("#Site " + (site + 1) + " GU Correlation PASSED");
                            sw.WriteLine("#GU Correlation Summary,PASSED");
                        }
                        else
                        {
                            //sw.WriteLine("#Site " + (site + 1) + " GU Correlation FAILED");
                            sw.WriteLine("#GU Correlation Summary,FAILED");
                        }
                    }

                    if (!GU.GuVerifyFailed[site])
                    {
                        //sw.WriteLine("#Site " + (site + 1) + " GU Verification PASSED");
                        sw.WriteLine("#GU Verification Summary,PASSED");
                    }
                    else
                    {
                        //sw.WriteLine("#Site " + (site + 1) + " GU Verification FAILED");
                        sw.WriteLine("#GU Verification Summary,FAILED");
                    }
                }
            }

            internal static string GetLocalIPAddress()
            {
                return ATFRTE.Instance.IPAddress;
            }

            public static void WriteCorrFactor(string corrFactorFilePath)
            {
                using (StreamWriter corrFactorFile = new StreamWriter(new FileStream(corrFactorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(corrFactorFile, GU.CorrStartTime, GU.CorrFinishTime);

                    corrFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorFile.Write(testName + ",");
                    }
                    corrFactorFile.WriteLine("");

                    // write test numbers
                    corrFactorFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorFile.Write(GU.testNumDict[testName] + ",");
                    }

                    corrFactorFile.WriteLine("");

                    // write units
                    corrFactorFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorFile.Write(GU.unitsDict[testName] + ",");
                    }

                    corrFactorFile.WriteLine("");

                    // write high limits
                    corrFactorFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        try
                        {
                            if (GU.factorMultiplyEnabledTests.Contains(testName))
                            {
                                corrFactorFile.Write(GU.hiLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                            }
                            else
                            {
                                corrFactorFile.Write(GU.hiLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    corrFactorFile.WriteLine("");

                    // write low limits
                    corrFactorFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        if (GU.factorMultiplyEnabledTests.Contains(testName))
                        {
                            corrFactorFile.Write(GU.loLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                        else
                        {
                            corrFactorFile.Write(GU.loLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                    }
                    corrFactorFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        // correlation factor file
                        corrFactorFile.Write("999,,,,," + (site + 1) + ",,,,,");
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            corrFactorFile.Write(GU.GuCalFactorsDict[site, testName] + ",");
                        }
                        corrFactorFile.WriteLine("");
                    } // site loop

                    printSummary(corrFactorFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Correlation Factor Data saved to " + corrFactorFilePath);
                allAnalysisFiles.Add(corrFactorFilePath);
            }

            public static void WriteCorrFactorNoDemo(string corrFactorNoDemoFilePath)
            {
                using (StreamWriter corrFactorNoDemoFile = new StreamWriter(new FileStream(corrFactorNoDemoFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(corrFactorNoDemoFile, GU.CorrStartTime, GU.CorrFinishTime);

                    corrFactorNoDemoFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorNoDemoFile.Write(testName + ",");
                    }
                    corrFactorNoDemoFile.WriteLine("");

                    // write test numbers
                    corrFactorNoDemoFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorNoDemoFile.Write(GU.testNumDict[testName] + ",");
                    }

                    corrFactorNoDemoFile.WriteLine("");

                    // write units
                    corrFactorNoDemoFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        corrFactorNoDemoFile.Write(GU.unitsDict[testName] + ",");
                    }

                    corrFactorNoDemoFile.WriteLine("");

                    // write high limits
                    corrFactorNoDemoFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        if (GU.factorMultiplyEnabledTests.Contains(testName))
                        {
                            corrFactorNoDemoFile.Write(GU.hiLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                        else
                        {
                            corrFactorNoDemoFile.Write(GU.hiLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                    }
                    corrFactorNoDemoFile.WriteLine("");

                    // write low limits
                    corrFactorNoDemoFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        if (GU.factorMultiplyEnabledTests.Contains(testName))
                        {
                            corrFactorNoDemoFile.Write(GU.loLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                        else
                        {
                            corrFactorNoDemoFile.Write(GU.loLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                        }
                    }
                    corrFactorNoDemoFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        // correlation factor file
                        corrFactorNoDemoFile.Write("999,,,,," + (site + 1) + ",,,,,");
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            corrFactorNoDemoFile.Write((GU.GuCalFactorsDict[site, testName] - GU.demoBoardOffsets[GU.selectedBatch, testName]) + ",");
                        }
                        corrFactorNoDemoFile.WriteLine("");
                    } // site loop

                    printSummary(corrFactorNoDemoFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Correlation Factor (without Demo offsets) Data saved to " + corrFactorNoDemoFilePath);
                allAnalysisFiles.Add(corrFactorNoDemoFilePath);
            }

            public static void WriteRawData(string rawDataFilePath)
            {
                using (StreamWriter rawDataFile = new StreamWriter(new FileStream(rawDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(rawDataFile, GU.CorrStartTime, GU.CorrFinishTime);

                    rawDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawDataFile.Write(testName + ",");
                    }
                    rawDataFile.WriteLine("");

                    // write test numbers
                    rawDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    rawDataFile.WriteLine("");

                    // write units
                    rawDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    rawDataFile.WriteLine("");

                    // write high limits
                    rawDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawDataFile.Write("1,");   // ***these limits don't really apply to the data!
                    }
                    rawDataFile.WriteLine("");

                    // write low limits
                    rawDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawDataFile.Write("-1,");   // ***these limits don't really apply to the data!
                    }
                    rawDataFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        foreach (int dutID in GU.dutIdLooseUserReducedList)
                        {
                            // calibration data file, all runs of raw data
                            //rawDataFile.Write(dutID + "-run" + run + ",,,,," + site + ",,,,,");
                            rawDataFile.Write("PID-" + dutID + ",,,,," + (site + 1) + ",,,,,");
                            foreach (string testName in GU.testedTestNameList.Values)
                            {
                                rawDataFile.Write(GU.rawAllMsrDataDict[site, testName, dutID] + ",");
                            }
                            rawDataFile.WriteLine("");

                            if (GU.dutIDtestedDead.Contains(dutID)) continue;
                        } // dut loop
                    } // site loop

                    printSummary(rawDataFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Raw Data saved to " + rawDataFilePath);
                allAnalysisFiles.Add(rawDataFilePath);
            }

            //public static void WriteIccCalfactor(string iccCalFactorFilePath)
            //{
            //    using (StreamWriter iccCalFactorFile = new StreamWriter(new FileStream(iccCalFactorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            //    {
            //        printHeader(iccCalFactorFile, IccCalStartTime, IccCalFinishTime);

            //        List<string> IccTestNameList = new List<string>();
            //        if (GuMode.Contains(GuModes.IccCorrVrfy))
            //        {
            //            //IccTestNameList = new List<string>(IccCalFactorsTempDict[sitesUserReducedList[0]].Keys);
            //            IccTestNameList = IccCalTestNames.Key.Keys.ToList();
            //        }

            //        iccCalFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

            //        // write test names
            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(testName + ",");
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        // write test numbers
            //        iccCalFactorFile.Write("Test#,,,,,,,,,,");
            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(testNumDict[IccCalTestNames.Key[testName].PoutTestName] + ",");
            //        }

            //        iccCalFactorFile.WriteLine("");

            //        // write units
            //        iccCalFactorFile.Write("Unit,,,,,,,,,,");

            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(unitsDict[IccCalTestNames.Key[testName].PoutTestName] + ",");
            //        }

            //        iccCalFactorFile.WriteLine("");

            //        // write high limits
            //        iccCalFactorFile.Write("HighL,,,,,,,,,,");
            //        foreach (string testName in IccTestNameList)
            //        {
            //            if (hiLimCalAddDict.ContainsKey(testName))
            //            {
            //                iccCalFactorFile.Write(hiLimCalAddDict[testName] + ",");
            //            }
            //            else
            //            {
            //                iccCalFactorFile.Write("999,");
            //            }
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        // write low limits
            //        iccCalFactorFile.Write("LowL,,,,,,,,,,");

            //        foreach (string testName in IccTestNameList)
            //        {
            //            if (hiLimCalAddDict.ContainsKey(testName))
            //            {
            //                iccCalFactorFile.Write(loLimCalAddDict[testName] + ",");
            //            }
            //            else
            //            {
            //                iccCalFactorFile.Write("-999,");
            //            }
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        // write data
            //        foreach (int site in runningGU.AllIndexOf(true))
            //        {
            //            if (GuMode[site] != GuModes.IccCorrVrfy) continue;

            //            iccCalFactorFile.Write("InputGain,,,,," + (site + 1) + ",,,,,");
            //            foreach (string testName in IccTestNameList)
            //            {
            //                iccCalFactorFile.Write(GuCalFactorsDict[site, testName + IccCalGain.InputGain] + ",");
            //            }
            //            iccCalFactorFile.WriteLine("");

            //            iccCalFactorFile.Write("OutputGain,,,,," + (site + 1) + ",,,,,");
            //            foreach (string testName in IccTestNameList)
            //            {
            //                iccCalFactorFile.Write(GuCalFactorsDict[site, testName + IccCalGain.OutputGain] + ",");
            //            }
            //            iccCalFactorFile.WriteLine("");
            //        } // site loop

            //        printSummary(iccCalFactorFile);
            //    }  // Streamwriters

            //    LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Factors saved to " + iccCalFactorFilePath);
            //    allAnalysisFiles.Add(iccCalFactorFilePath);
            //}

            //public static void WriteIccAvgError(string iccCalAvgErrorFilePath)
            //{
            //    using (StreamWriter iccCalFactorFile = new StreamWriter(new FileStream(iccCalAvgErrorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            //    {
            //        printHeader(iccCalFactorFile, IccCalStartTime, IccCalFinishTime);

            //        List<string> IccTestNameList = IccCalTestNames.Icc.Keys.ToList();

            //        iccCalFactorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

            //        // write test names
            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(testName + ",");
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        // write test numbers
            //        iccCalFactorFile.Write("Test#,,,,,,,,,,");
            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(testNumDict[testName] + ",");
            //        }

            //        iccCalFactorFile.WriteLine("");

            //        // write units
            //        iccCalFactorFile.Write("Unit,,,,,,,,,,");

            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(unitsDict[testName] + ",");
            //        }

            //        iccCalFactorFile.WriteLine("");

            //        // write high limits
            //        iccCalFactorFile.Write("HighL,,,,,,,,,,");
            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(IccCalAvgErrorDict[GuMode.AllIndexOf(GuModes.IccCorrVrfy).First(), 1, testName].HiLim + ",");
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        // write low limits
            //        iccCalFactorFile.Write("LowL,,,,,,,,,,");

            //        foreach (string testName in IccTestNameList)
            //        {
            //            iccCalFactorFile.Write(IccCalAvgErrorDict[GuMode.AllIndexOf(GuModes.IccCorrVrfy).First(), 1, testName].LoLim + ",");
            //        }
            //        iccCalFactorFile.WriteLine("");

            //        foreach (int site in IccCalAvgErrorDict.Keys)
            //        {
            //            foreach (int attemptNum in IccCalAvgErrorDict[site].Keys)
            //            {
            //                iccCalFactorFile.Write("run-" + attemptNum + ",,,,," + (site + 1) + ",,,,,");
            //                foreach (string testName in IccTestNameList)
            //                {
            //                    iccCalFactorFile.Write(IccCalAvgErrorDict[site, attemptNum, testName].AvgError + ",");
            //                }
            //                iccCalFactorFile.WriteLine("");
            //            }
            //        }

            //        printSummary(iccCalFactorFile);
            //    }  // Streamwriters

            //    LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Average Error saved to " + iccCalAvgErrorFilePath);
            //    string lastFolderName = Path.GetFileName(Path.GetDirectoryName(iccCalAvgErrorFilePath));
            //    allAnalysisFiles.Add(iccCalAvgErrorFilePath);
            //}
            public static void WriteCorrRawData(string corrRawDataFilePath)
            {
                using (StreamWriter rawCorrDataFile = new StreamWriter(new FileStream(corrRawDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(rawCorrDataFile, GU.CorrStartTime, GU.CorrFinishTime);

                    rawCorrDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawCorrDataFile.Write(testName + ",");
                    }
                    rawCorrDataFile.WriteLine("");

                    // write test numbers
                    rawCorrDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawCorrDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    rawCorrDataFile.WriteLine("");

                    // write units
                    rawCorrDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawCorrDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    rawCorrDataFile.WriteLine("");

                    // write high limits
                    rawCorrDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawCorrDataFile.Write("1,");   // ***these limits don't really apply to the data!
                    }
                    rawCorrDataFile.WriteLine("");

                    // write low limits
                    rawCorrDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        rawCorrDataFile.Write("-1,");   // ***these limits don't really apply to the data!
                    }
                    rawCorrDataFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        foreach (int dutID in GU.dutIdLooseUserReducedList)
                        {
                            // calibration data file, all runs of raw data
                            //rawDataFile.Write(dutID + "-run" + run + ",,,,," + site + ",,,,,");
                            rawCorrDataFile.Write("PID-" + dutID + ",,,,," + (site + 1) + ",,,,,");
                            foreach (string testName in GU.testedTestNameList.Values)
                            {
                                rawCorrDataFile.Write(GU.rawAllCorrDataDict[site, testName, dutID] + ",");
                            }
                            rawCorrDataFile.WriteLine("");

                            if (GU.dutIDtestedDead.Contains(dutID)) continue;
                        } // dut loop
                    } // site loop

                    printSummary(rawCorrDataFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Corr Raw Data saved to " + corrRawDataFilePath);
                allAnalysisFiles.Add(corrRawDataFilePath);
            }

            public static void WriteVrfyData(string vrfyDataFilePath)
            {
                using (StreamWriter vrfyDataFile = new StreamWriter(new FileStream(vrfyDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(vrfyDataFile, GU.CorrStartTime, GU.CorrFinishTime);

                    vrfyDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyDataFile.Write(testName + ",");
                    }
                    vrfyDataFile.WriteLine("");

                    // write test numbers
                    vrfyDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    vrfyDataFile.WriteLine("");

                    // write units
                    vrfyDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    vrfyDataFile.WriteLine("");

                    // write high limits
                    vrfyDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyDataFile.Write("1,");   // ***these limits don't really apply to the data!
                    }
                    vrfyDataFile.WriteLine("");

                    // write low limits
                    vrfyDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyDataFile.Write("-1,");   // ***these limits don't really apply to the data!
                    }
                    vrfyDataFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        foreach (int dutID in GU.dutIdLooseUserReducedList)
                        {
                            if (GU.dutIDtestedDead.Contains(dutID)) continue;

                            vrfyDataFile.Write("PID-" + dutID + ",,,,," + (site + 1) + ",,,,,");
                            foreach (string testName in GU.testedTestNameList.Values)
                            {
                                vrfyDataFile.Write(GU.correctedMsrDataDict[site, testName, dutID] + ",");  // verification data file, the last run's error with correlation factors applied
                            }
                            vrfyDataFile.WriteLine("");
                        } // dut loop
                    } // site loop

                    printSummary(vrfyDataFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Verification Data saved to " + vrfyDataFilePath);
                allAnalysisFiles.Add(vrfyDataFilePath);
            }

            public static void WriteVrfyError(string vrfyErrorFilePath)
            {
                using (StreamWriter vrfyErrorFile = new StreamWriter(new FileStream(vrfyErrorFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(vrfyErrorFile, GU.CorrStartTime, GU.CorrFinishTime);

                    vrfyErrorFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyErrorFile.Write(testName + ",");
                    }
                    vrfyErrorFile.WriteLine("");

                    // write test numbers
                    vrfyErrorFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyErrorFile.Write(GU.testNumDict[testName] + ",");
                    }

                    vrfyErrorFile.WriteLine("");

                    // write units
                    vrfyErrorFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyErrorFile.Write(GU.unitsDict[testName] + ",");
                    }

                    vrfyErrorFile.WriteLine("");

                    // write high limits
                    vrfyErrorFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyErrorFile.Write(GU.hiLimVrfyDict[testName] + ",");   // ***these limits don't really apply to the data!
                    }
                    vrfyErrorFile.WriteLine("");

                    // write low limits
                    vrfyErrorFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyErrorFile.Write(GU.loLimVrfyDict[testName] + ",");   // ***these limits don't really apply to the data!
                    }
                    vrfyErrorFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        foreach (int dutID in GU.dutIdLooseUserReducedList)
                        {
                            if (GU.dutIDtestedDead.Contains(dutID)) continue;

                            vrfyErrorFile.Write("PID-" + dutID + ",,,,," + (site + 1) + ",,,,,");
                            foreach (string testName in GU.testedTestNameList.Values)
                            {
                                vrfyErrorFile.Write(GU.correctedMsrErrorDict[site, testName, dutID] + ",");
                            }
                            vrfyErrorFile.WriteLine("");
                        } // dut loop
                    } // site loop

                    printSummary(vrfyErrorFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Verification Error saved to " + vrfyErrorFilePath);
                allAnalysisFiles.Add(vrfyErrorFilePath);
            }

            public static void WriteCorrCoeff(string vrfyCorrCoeffFilePath)
            {
                using (StreamWriter vrfyCorrCoeffFile = new StreamWriter(new FileStream(vrfyCorrCoeffFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(vrfyCorrCoeffFile, GU.CorrStartTime, GU.CorrFinishTime);

                    vrfyCorrCoeffFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(testName + ",");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write test numbers
                    vrfyCorrCoeffFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(GU.testNumDict[testName] + ",");
                    }

                    vrfyCorrCoeffFile.WriteLine("");

                    // write units
                    vrfyCorrCoeffFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(GU.unitsDict[testName] + ",");
                    }

                    vrfyCorrCoeffFile.WriteLine("");

                    // write high limits
                    vrfyCorrCoeffFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write("1,");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write low limits
                    vrfyCorrCoeffFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write("-1,");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        vrfyCorrCoeffFile.Write("999,,,,," + (site + 1) + ",,,,,");
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            vrfyCorrCoeffFile.Write(GU.corrCoeffDict[site, testName] + ",");
                        }
                        vrfyCorrCoeffFile.WriteLine("");
                    } // site loop

                    printSummary(vrfyCorrCoeffFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Verification Correlation Coefficients saved to " + vrfyCorrCoeffFilePath);
                allAnalysisFiles.Add(vrfyCorrCoeffFilePath);
            }

            public static void WriteLogFile(string logMessagesFilePath)
            {
                using (StreamWriter logMessagesFile = new StreamWriter(new FileStream(logMessagesFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(logMessagesFile, GU.IccCalStartTime, GU.CorrFinishTime);

                    logMessagesFile.WriteLine("\r\n\r\nMessages logged during " + "GU Calibration" + ":\r\n--------------------------------------------------------------\r\n\r\n");
                    foreach (string str in GU.loggedMessages)
                    {
                        logMessagesFile.WriteLine(str);
                    }
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Log Messages saved to " + logMessagesFilePath);
                allAnalysisFiles.Add(logMessagesFilePath);
            }

            public static void WriteAddOnInfoLogFile(string logPath, AddOnInformation obj)
            {
                var json = new JavaScriptSerializer().Serialize(obj);

                using (StreamWriter logMessagesFile = new StreamWriter(new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    logMessagesFile.WriteLine(json);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Misc Log Messages saved to " + logPath);
                allAnalysisFiles.Add(logPath);
            }

            public static void WriteIccCalData(string iccCalDataFilePath)
            {
                using (StreamWriter iccCalDataFile = new StreamWriter(new FileStream(iccCalDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(iccCalDataFile, GU.IccCalStartTime, GU.IccCalFinishTime);

                    iccCalDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.IccCalTestNames.All)
                    {
                        iccCalDataFile.Write(testName + ",");
                    }
                    iccCalDataFile.WriteLine("");

                    // write test numbers
                    iccCalDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.IccCalTestNames.All)
                    {
                        iccCalDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    iccCalDataFile.WriteLine("");

                    // write units
                    iccCalDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.IccCalTestNames.All)
                    {
                        iccCalDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    iccCalDataFile.WriteLine("");

                    // write high limits
                    iccCalDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.IccCalTestNames.All)
                    {
                        iccCalDataFile.Write("999,");
                    }
                    iccCalDataFile.WriteLine("");

                    // write low limits
                    iccCalDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.IccCalTestNames.All)
                    {
                        iccCalDataFile.Write("-999,");
                    }
                    iccCalDataFile.WriteLine("");

                    // write data
                    //foreach (int site in runningGU.AllIndexOf(true))
                    foreach (int site in GU.sitesUserReducedList)
                    {
                        foreach (int dutID in GU.dutIdLooseUserReducedList)
                        {
                            // calibration data file, all runs of raw data
                            //rawDataFile.Write(dutID + "-run" + run + ",,,,," + site + ",,,,,");
                            iccCalDataFile.Write("PID-" + dutID + ",,,,," + (site + 1) + ",,,,,");
                            foreach (string testName in GU.IccCalTestNames.All)
                            {
                                iccCalDataFile.Write(GU.rawIccCalMsrDataDict[site, testName, dutID] + ",");
                            }
                            iccCalDataFile.WriteLine("");

                            if (GU.dutIDtestedDead.Contains(dutID)) continue;
                        } // dut loop
                    } // site loop

                    printSummary(iccCalDataFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Icc Cal Data saved to " + iccCalDataFilePath);
                allAnalysisFiles.Add(iccCalDataFilePath);
            }

            public static void WriteRefFinalData(string benchDataFilePath)
            {
                using (StreamWriter benchDataFile = new StreamWriter(new FileStream(benchDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    benchDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        benchDataFile.Write(testName + ",");
                    }
                    benchDataFile.WriteLine("");

                    // write test numbers
                    benchDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        benchDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    benchDataFile.WriteLine("");

                    // write units
                    benchDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        benchDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    benchDataFile.WriteLine("");

                    // write high limits
                    benchDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        benchDataFile.Write("1,");
                    }
                    benchDataFile.WriteLine("");

                    // write low limits
                    benchDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.testedTestNameList.Values)
                    {
                        benchDataFile.Write("-1,");
                    }
                    benchDataFile.WriteLine("");

                    foreach (int dutID in GU.dutIdLooseUserReducedList)
                    {
                        benchDataFile.Write("PID-" + dutID + ",,,,,,,,,,");
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            benchDataFile.Write(GU.finalRefDataDict[GU.selectedBatch, testName, dutID] + ",");
                        }
                        benchDataFile.WriteLine("");
                    } // dut loop

                    printSummary(benchDataFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Final Reference Data saved to " + benchDataFilePath);
                allAnalysisFiles.Add(benchDataFilePath);
            }

            public static void WriteRefDemoData(string demoDataFilePath, GU.UnitType unitType)
            {
                using (StreamWriter demoDataFile = new StreamWriter(new FileStream(demoDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    demoDataFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoDataFile.Write(testName + ",");
                    }
                    demoDataFile.WriteLine("");

                    // write test numbers
                    demoDataFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoDataFile.Write(GU.testNumDict[testName] + ",");
                    }

                    demoDataFile.WriteLine("");

                    // write units
                    demoDataFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoDataFile.Write(GU.unitsDict[testName] + ",");
                    }

                    demoDataFile.WriteLine("");

                    // write high limits
                    demoDataFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoDataFile.Write("1,");
                    }
                    demoDataFile.WriteLine("");

                    // write low limits
                    demoDataFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoDataFile.Write("-1,");
                    }
                    demoDataFile.WriteLine("");

                    foreach (int dutID in GU.dutIdAllDemo[GU.selectedBatch])
                    {
                        demoDataFile.Write("PID-" + dutID + ",,,,,,,,,,");
                        foreach (string testName in GU.benchTestNameList.Values)
                        {
                            demoDataFile.Write(GU.demoDataDict[GU.selectedBatch, testName, unitType, dutID] + ",");
                        }
                        demoDataFile.WriteLine("");
                    } // dut loop

                    printSummary(demoDataFile);
                }  // Streamwriters

                if (unitType == GU.UnitType.Demo)
                    GU.LogToLogServiceAndFile(LogLevel.HighLight, "Reference Demo Data saved to " + demoDataFilePath);
                else
                    GU.LogToLogServiceAndFile(LogLevel.HighLight, "Reference Pre-Demo Data saved to " + demoDataFilePath);

                allAnalysisFiles.Add(demoDataFilePath);
            }

            public static void WriteRefDemoOffsets(string demoOffsetFilePath)
            {
                using (StreamWriter demoOffsetFile = new StreamWriter(new FileStream(demoOffsetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    demoOffsetFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoOffsetFile.Write(testName + ",");
                    }
                    demoOffsetFile.WriteLine("");

                    // write test numbers
                    demoOffsetFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoOffsetFile.Write(GU.testNumDict[testName] + ",");
                    }

                    demoOffsetFile.WriteLine("");

                    // write units
                    demoOffsetFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoOffsetFile.Write(GU.unitsDict[testName] + ",");
                    }

                    demoOffsetFile.WriteLine("");

                    // write high limits
                    demoOffsetFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoOffsetFile.Write("1,");
                    }
                    demoOffsetFile.WriteLine("");

                    // write low limits
                    demoOffsetFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        demoOffsetFile.Write("-1,");
                    }
                    demoOffsetFile.WriteLine("");

                    // write data
                    foreach (int dutID in GU.dutIdAllDemo[GU.selectedBatch])
                    {
                        demoOffsetFile.Write("PID-" + dutID + ",,,,,,,,,,");
                        foreach (string testName in GU.benchTestNameList.Values)
                        {
                            demoOffsetFile.Write(GU.demoBoardOffsetsPerDut[GU.selectedBatch, testName, dutID] + ",");
                        }
                        demoOffsetFile.WriteLine("");
                    } // dut loop

                    printSummary(demoOffsetFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "DemoBoard offsets saved to " + demoOffsetFilePath);
                allAnalysisFiles.Add(demoOffsetFilePath);
            }

            public static void WriteRefRepeatFile(string refRepeatFilePath)
            {
                using (StreamWriter refRepeatFile = new StreamWriter(new FileStream(refRepeatFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(refRepeatFile, "", "");

                    refRepeatFile.WriteLine("\r\n\r\nREADME:");
                    refRepeatFile.WriteLine("    Please include units in reference data file so that decibel values are ranked correctly.\r\n");
                    refRepeatFile.WriteLine("    Higher Rank = better repeatability. Rank values are between +-10\r\n");
                    refRepeatFile.WriteLine("    Formula:  DataRange = +- 2^(-Rank) * T\r\n");
                    refRepeatFile.WriteLine("    If units are non-decibel, then T = 1% (of median value).");
                    refRepeatFile.WriteLine("       Ranks are as follows: (extending to +-10)");
                    refRepeatFile.WriteLine("       Rank    DataRange");
                    refRepeatFile.WriteLine("       -3      +-8%");
                    refRepeatFile.WriteLine("       -2      +-4%");
                    refRepeatFile.WriteLine("       -1      +-2%");
                    refRepeatFile.WriteLine("        0      +-1%");
                    refRepeatFile.WriteLine("       +1      +-0.5%");
                    refRepeatFile.WriteLine("       +2      +-0.25%");
                    refRepeatFile.WriteLine("       +3      +-0.125%");

                    refRepeatFile.WriteLine("\r\n    If units are decibel (dB/dBm/dBc), then T is a number between 0.1dB at [median = +30dB] and 0.4dB at [median = -70dB].");
                    refRepeatFile.WriteLine("       Ranks are as follows: (extending to +-10) (for median value = 30dB, therefore T = 0.1dB)");
                    refRepeatFile.WriteLine("       Rank    DataRange");
                    refRepeatFile.WriteLine("       -3      +-0.8dB");
                    refRepeatFile.WriteLine("       -2      +-0.4dB");
                    refRepeatFile.WriteLine("       -1      +-0.2dB");
                    refRepeatFile.WriteLine("        0      +-0.1dB");
                    refRepeatFile.WriteLine("       +1      +-0.05dB");
                    refRepeatFile.WriteLine("       +2      +-0.025dB");
                    refRepeatFile.WriteLine("       +3      +-0.0125dB\r\n");

                    refRepeatFile.WriteLine("    * Asterisk indicates an outlier. Outliers are removed from the data.\r\n");

                    foreach (int repRank in refDataRepeatabilityLog[GU.selectedBatch].Keys)
                    {
                        refRepeatFile.WriteLine("\r\n");

                        refDataRepeatabilityLog[GU.selectedBatch][repRank].Sort();

                        foreach (string msg in refDataRepeatabilityLog[GU.selectedBatch][repRank])
                        {
                            refRepeatFile.WriteLine("Rank " + repRank.ToString("+#;-#;0") + ", " + msg);
                        }
                    }
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Ref Data Repeatability Check saved to " + refRepeatFilePath);
                allAnalysisFiles.Add(refRepeatFilePath);
            }

            public static void WriteRefLooseDemoCorrCoeff(string refLooseDemoCorrCoeff)
            {
                using (StreamWriter vrfyCorrCoeffFile = new StreamWriter(new FileStream(refLooseDemoCorrCoeff, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    printHeader(vrfyCorrCoeffFile, "", "");

                    vrfyCorrCoeffFile.Write("\nParameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                    // write test names
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(testName + ",");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write test numbers
                    vrfyCorrCoeffFile.Write("Test#,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(GU.testNumDict[testName] + ",");
                    }

                    vrfyCorrCoeffFile.WriteLine("");

                    // write units
                    vrfyCorrCoeffFile.Write("Unit,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(GU.unitsDict[testName] + ",");
                    }

                    vrfyCorrCoeffFile.WriteLine("");

                    // write high limits
                    vrfyCorrCoeffFile.Write("HighL,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write("1,");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write low limits
                    vrfyCorrCoeffFile.Write("LowL,,,,,,,,,,");

                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write("-1,");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    // write data
                    vrfyCorrCoeffFile.Write("999,,,,,,,,,,");
                    foreach (string testName in GU.benchTestNameList.Values)
                    {
                        vrfyCorrCoeffFile.Write(GU.demoLooseCorrCoeff[GU.selectedBatch, testName] + ",");
                    }
                    vrfyCorrCoeffFile.WriteLine("");

                    printSummary(vrfyCorrCoeffFile);
                }  // Streamwriters

                GU.LogToLogServiceAndFile(LogLevel.HighLight, "Verification Correlation Coefficients saved to " + refLooseDemoCorrCoeff);
                allAnalysisFiles.Add(refLooseDemoCorrCoeff);
            }

            //public static void WriteDCsubstrateRawData(string rawDataFilePath)
            //{
            //    using (StreamWriter rawDataFile = new StreamWriter(new FileStream(rawDataFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            //    {
            //        string Header = "";
            //        string Spec = "";
            //        string Data = "";
            //        int i = 0;
            //        int Raw = 0;

            //        double Vcc_temperature = 0f;
            //        double Vcc2_temperature = 0f;
            //        double Vbatt_temperature = 0f;
            //        double Vdd_temperature = 0f;

            //        //ChoonChin (20211001) - Comment out for RF2
            //        double SA_temperature = EqLib.Eq.Site[0].RF.SA.ReadTemp();
            //        double SG_temperature = EqLib.Eq.Site[0].RF.SG.ReadTemp();

            //        Vcc_temperature = EqLib.Eq.Site[0].DC["Vcc"].ReadTemp(Vcc_temperature);
            //        Vcc2_temperature = EqLib.Eq.Site[0].DC["Vcc2"].ReadTemp(Vcc_temperature);
            //        Vbatt_temperature = EqLib.Eq.Site[0].DC["Vbatt"].ReadTemp(Vbatt_temperature);
            //        Vdd_temperature = EqLib.Eq.Site[0].DC["Vdd"].ReadTemp(Vdd_temperature);

            //        Header += ",PassFail,EndTime,TesterName";

            //        Header += "PassFail," + (GU.runningDCVerify == true ? "1" : "0");
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "FailedCH," + GU.FailedCh.Replace(',', ';').Replace(" ", "");
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "EndTime," + Convert.ToString(closingTimeCodeHumanFriendly);
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "TesterName," + handlerSN;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "HostIpAddress," + ipAddress;
            //        rawDataFile.WriteLine(Header); Header = "";

            //        Header += "Temp_SA," + SA_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "Temp_SG," + SG_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "Temp_VCC," + Vcc_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "Temp_VCC2," + Vcc2_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "Temp_VBATT," + Vbatt_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";
            //        Header += "Temp_VDD," + Vdd_temperature;
            //        rawDataFile.WriteLine(Header); Header = "";

            //        rawDataFile.WriteLine(Header); Header = "";

            //        Header += "," + (GU.runningDCVerify == true ? "1" : "0") + "," + Convert.ToString(closingTimeCodeHumanFriendly) + "," + handlerSN;

            //        Header += "Parameter,";

            //        foreach (string key in DCLeakageDataDict.Keys)
            //        {
            //            if (i == DCLeakageDataDict.Count - 1) { Header += key; }
            //            else Header += key + ",";

            //            i++;
            //        }

            //        foreach (Dictionary<string, string> value in Sheet.testPlan)
            //        {
            //            if (value["EquipmentName"] != "None")
            //            {
            //                Header += "," + value["EquipmentName"] + "_LowL," + value["EquipmentName"] + "_HighL";
            //            }

            //        }

            //        rawDataFile.WriteLine(Header); Header = "";

            //        for (int j = 0; j < 2; j++)
            //        {
            //            if (j == 0) Spec += "HighL";
            //            else Spec += "LowL";

            //            foreach (Dictionary<string, string> value in Sheet.testPlan)
            //            {
            //                if (value["EquipmentName"] != "None")
            //                {
            //                    if (j == 0) Spec += "," + value["Spec_Max"];
            //                    else Spec += "," + value["Spec_Min"];
            //                }
            //            }
            //            rawDataFile.WriteLine(Spec); Spec = "";
            //        }

            //        while (true)
            //        {
            //            int k = 0;

            //            if (Raw == 0)
            //            {
            //                Data += Raw + 1 + ",";
            //            }
            //            foreach (string key in DCLeakageDataDict.Keys)
            //            {
            //                double[] Value = DCLeakageDataDict[key];

            //                if (k == DCLeakageDataDict.Count - 1) { Data += Value[Raw]; }
            //                else Data += Value[Raw] + ",";

            //                k++;
            //            }
            //            Data += "," + Convert.ToString(Raw + 1);

            //            foreach (Dictionary<string, string> value in Sheet.testPlan)
            //            {
            //                if (value["EquipmentName"] != "None")
            //                {
            //                    Data += "," + value["Spec_Min"] + "," + value["Spec_Max"];
            //                }

            //            }
            //            Data += "," + (GU.runningDCVerify == true ? "1" : "0") + "," + Convert.ToString(closingTimeCodeHumanFriendly) + "," + handlerSN;

            //            rawDataFile.WriteLine(Data); Data = "";

            //            Raw++;

            //            if (Raw == 2000) break;
            //        }
            //    }  // Streamwriters
            //    allAnalysisFiles.Add(rawDataFilePath);
            //}

            public static void AddZipToZip(ZipFile zipToUpdate, string zipToAdd, string folder)
            {
                if (File.Exists(zipToAdd))
                {
                    using (ZipFile zip1 = ZipFile.Read(zipToAdd))
                    {
                        foreach (ZipEntry z in zip1)
                        {
                            MemoryStream stream = new MemoryStream();
                            z.Extract(stream);
                            stream.Seek(0, SeekOrigin.Begin);
                            zipToUpdate.AddEntry(folder + "\\" + z.FileName, stream);
                        }
                    }
                }
            }

            // 15-Aug-2014 (JJ Low)
            private static string GetSystemConfigFile()
            {
                string configPath = Application.StartupPath + @"\Configuration\ATFConfig.xml";

                if (!File.Exists(configPath))
                {
                    configPath = @"C:\Avago.ATF." + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion + @"\Configuration\ATFConfig.xml";
                }
                return configPath;
            }

            private static string GetSystemResultBackupPath()
            {
                //string resultBackupPath = @"C:\Avago.ATF.Common\Results.Backup";
                string resultBackupPath = @"C:\Avago.ATF.Common\CustomGUCalData";
                if (!Directory.Exists(resultBackupPath)) Directory.CreateDirectory(resultBackupPath);

                // available for Clotho 2.1.5 and above
                //resultFilePath = ATFMiscConstants.ClothoResultsAbsBackupPath;

                // for Clotho 2.1.4 and below
                //if (ATFConfig.Instance.getSpecificItem(ATFConfigConstants.TagSystemResultBackupPath) != null)
                //{
                //    resultFilePath = ATFConfig.Instance.getSpecificItem(ATFConfigConstants.TagSystemResultBackupPath).Value;
                //}

                return resultBackupPath;
            }

            public static void WriteStdResultFile(string resultFileName, string lotID, string subLotID)
            {
                try
                {
                    // read config file
                    ATFConfig.Instance.ResetConfig();

                    string ConfigFile = GetSystemConfigFile();
                    string strlog = XMLValidate.ValidateXMLFile(ConfigFile, ATFMiscConstants.ATFConfigFileXSDName);

                    if (strlog.EndsWith("Succeed"))
                    {
                        strlog = ATFConfig.Instance.ParseConfigXMLFileIntoMemory(ConfigFile);

                        resultFilePath = GetSystemResultBackupPath();

                        //if (strlog.EndsWith("Succeed"))
                        //{
                        //    if (ATFConfig.Instance.getSpecificItem(ATFConfigConstants.TagSystemResultRemoteSharePath) != null)
                        //    {
                        //        remoteSharePath = ATFConfig.Instance.getSpecificItem(ATFConfigConstants.TagSystemResultRemoteSharePath).Value;
                        //    }
                        //}
                        //else
                        //{
                        //    throw new Exception("Failed to parse configuration file");
                        //}
                    }
                    else
                    {
                        throw new Exception("Unable to find configuration file");
                    }

                    if (!Directory.Exists(resultFilePath))
                    {
                        Directory.CreateDirectory(resultFilePath);
                    }

                    using (StreamWriter resultFile = new StreamWriter(new FileStream(resultFilePath + @"\" + resultFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        resultFile.WriteLine("--- Global Info:");
                        resultFile.WriteLine("Date," + closingTimeCodeDatabaseFriendly);
                        resultFile.WriteLine("SetupTime, ");
                        resultFile.WriteLine("StartTime, ");
                        resultFile.WriteLine("FinishTime, ");
                        resultFile.WriteLine("TestPlan," + testPlanName + ".cs");
                        resultFile.WriteLine("TestPlanVersion," + testPlanVersion);
                        resultFile.WriteLine("Lot," + lotID);
                        resultFile.WriteLine("Sublot," + subLotID);
                        resultFile.WriteLine("Wafer," + waferID);
                        resultFile.WriteLine("WaferOrientation,NA");
                        resultFile.WriteLine("TesterName," + computerName);
                        resultFile.WriteLine("TesterType," + computerName);
                        resultFile.WriteLine("Product," + GU.prodTag);
                        resultFile.WriteLine("Operator," + opID);
                        resultFile.WriteLine("ExecType,");
                        resultFile.WriteLine("ExecRevision,");
                        resultFile.WriteLine("RtstCode,");
                        resultFile.WriteLine("PackageType,");
                        resultFile.WriteLine("Family,");
                        resultFile.WriteLine("SpecName," + Path.GetFileName(ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TL_FULLPATH, "")));
                        resultFile.WriteLine("SpecVersion,");
                        resultFile.WriteLine("FlowID,");
                        resultFile.WriteLine("DesignRevision,");
                        resultFile.WriteLine("--- Site details:,");
                        resultFile.WriteLine("Testing sites,");
                        resultFile.WriteLine("Handler ID," + handlerSN);
                        resultFile.WriteLine("Handler type,");
                        resultFile.WriteLine("LoadBoardName," + dibIDArray[GU.siteNo -1]);
                        resultFile.WriteLine("ContactorID," + contactorIDArray[GU.siteNo -1]);
                        resultFile.WriteLine("--- Options:,");
                        resultFile.WriteLine("UnitsMode,");
                        resultFile.WriteLine("--- ConditionName:,");
                        resultFile.WriteLine("EMAIL_ADDRESS,");
                        resultFile.WriteLine("Translator,");
                        resultFile.WriteLine("Wafer_Diameter,");
                        resultFile.WriteLine("Facility,");
                        resultFile.WriteLine("HostIpAddress," + ipAddress);
                        resultFile.WriteLine("Temperature,");
                        resultFile.WriteLine("PcbLot,");
                        resultFile.WriteLine("AssemblyLot,");
                        resultFile.WriteLine("VerificationUnit,");
                        resultFile.WriteLine(",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,");

                        // write CF
                        string correlationFile = Path.GetFileName(ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_CF_FULLPATH, ""));

                        //foreach (int site in runningGU.AllIndexOf(true))
                        foreach (int site in GU.sitesUserReducedList)
                        {
                            resultFile.Write("#CF," + correlationFile + ",,,,,,,,,");
                            foreach (string testName in GU.testedTestNameList.Values)
                            {
                                resultFile.Write(GU.GuCalFactorsDict[site, testName] + ",");
                            }
                            resultFile.WriteLine("");
                        }

                        // write test names
                        resultFile.Write("Parameter,SBIN,HBIN,DIE_X,DIE_Y,SITE,TIME,TOTAL_TESTS,LOT_ID,WAFER_ID,");

                        // write test names
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            resultFile.Write(testName + ",");
                        }
                        resultFile.WriteLine("");

                        // write test numbers
                        resultFile.Write("Tests#,,,,,,Sec,,,,");

                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            resultFile.Write(GU.testNumDict[testName] + ",");
                        }
                        resultFile.WriteLine("");

                        // write units
                        resultFile.Write("Unit,,,,,,,,,,");

                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            resultFile.Write(GU.unitsDict[testName] + ",");
                        }
                        resultFile.WriteLine("");

                        // write high limits
                        resultFile.Write("HighL,,,,,,,,,,");
                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            if (GU.factorMultiplyEnabledTests.Contains(testName))
                            {
                                resultFile.Write(GU.hiLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                            }
                            else
                            {
                                resultFile.Write(GU.hiLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                            }
                        }
                        resultFile.WriteLine("");

                        // write low limits
                        resultFile.Write("LowL,,,,,,,,,,");

                        foreach (string testName in GU.testedTestNameList.Values)
                        {
                            if (GU.factorMultiplyEnabledTests.Contains(testName))
                            {
                                resultFile.Write(GU.loLimCalMultiplyDict[testName] + ",");   // ***these limits don't really apply to the data!
                            } 
                            else
                            {
                                resultFile.Write(GU.loLimCalAddDict[testName] + ",");   // ***these limits don't really apply to the data!
                            }
                        }
                        resultFile.WriteLine("");

                        // write data
                        //foreach (int site in runningGU.AllIndexOf(true))
                        foreach (int site in GU.sitesUserReducedList)
                        {
                            foreach (int dutID in GU.dutIdLooseUserReducedList)
                            {
                                if (GU.dutIDtestedDead.Contains(dutID)) continue;

                                resultFile.Write("PID-" + dutID + ",,,0,0," + site + ",," + GU.testedTestNameList.Count.ToString() + ",,,");
                                foreach (string testName in GU.testedTestNameList.Values)
                                {
                                    resultFile.Write(GU.correctedMsrDataDict[site, testName, dutID] + ",");  // verification data file, the last run's error with correlation factors applied
                                }
                                resultFile.WriteLine("");
                            } // dut loop
                        } // site loop
                        printSummary(resultFile);

                        resultFile.Close();
                    }  // Streamwriters

                    //if (remoteSharePath.Length > 0)
                    //{
                    //    DirectoryInfo dirInfo = new DirectoryInfo(remoteSharePath);
                    //    if (dirInfo.Exists)
                    //    {
                    //        File.Copy(resultFilePath + @"\" + resultFileName, remoteSharePath + @"\" + resultFileName);
                    //    }
                    //}

                    GU.LogToLogServiceAndFile(LogLevel.Info, "STD Result file saved to " + resultFilePath + @"\" + resultFileName);
                }
                catch (Exception ex)
                {
                    GU.LogToLogServiceAndFile(LogLevel.Error, "Unable to save STD Result to " + resultFilePath + @"\" + resultFileName + ". Error: " + ex.Message);
                }
            }
        }
    }
}