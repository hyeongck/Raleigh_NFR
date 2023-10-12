using Avago.ATF.StandardLibrary;
using ClothoSharedItems;
using MyProduct;
using MPAD_TestTimer;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LibEqmtDriver;
using System.Collections.Generic;

namespace TestPlanCommon
{
    public class ProductionTestPlanBase
    {
        public ProductionLib2.ProductionTestInputForm prodInputForm { get; set; }
        public string LocSetFilePath { get; private set; }
        public string MIPIaddr { get; private set; }
        public MyDUT mydut;

        /// <summary>
        /// Use Generic way for both RF1 and RF2
        /// </summary>
        /// <param name="pjt_tagNum">Verify load_board_id/contactor_id</param>
        /// <param name="SkipProductGUIFlag">production gui show or not</param>
        public void ShowInputGui(string[] pjt_tagNum, bool SkipProductGUIFlag, MyDUT my)
        {
            //ShowInputGui(
            //    pjt_tagNum,
            //    ClothoDataObject.Instance.Get_TCF_Condition("Sample_Version", "PRODUCTION"),
            //    SkipProductGUIFlag,
            //    ClothoDataObject.Instance.Get_TCF_Condition("WebQueryValidation", "FALSE"),
            //    ClothoDataObject.Instance.Get_TCF_Condition("WebServerURL", ""));

            this.mydut = my;

            ShowInputGui(
                pjt_tagNum,
                mydut.DicCalInfo[DataFilePath.Sample_Version],
                SkipProductGUIFlag,
                Convert.ToString(MyDUT.WebQueryValidation),
                mydut.DicCalInfo[DataFilePath.WebServerURL]);
        }

        private void ShowInputGui(string[] pjt_tagNum, string sample_version = "PRODUCTION", bool SkipProductGUIFlag = false, string webQueryValidation = "FALSE", string webServerUrl = "")
        {
            LoggingManager.Instance.LogInfoTestPlan("Waiting for Production Information...");

            prodInputForm = new ProductionLib2.ProductionTestInputForm(
                IsEngineeringGUI: webQueryValidation.CIvEquals("FALSE") || !sample_version.CIvEquals("PRODUCTION"),
                webQueryValidation: webQueryValidation,
                webServerURL: webServerUrl,
                Clotho_User: ClothoDataObject.Instance.USERTYPE);


            string tempLotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
            prodInputForm.LotID = tempLotID;        //Pass LotID to production GUI.

            prodInputForm.productTag = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, ClothoDataObject.Instance.Get_Digital_Definition("GuPartNo", ""));

            string lbID = LibEqmtDriver.MIPI.Lib_Var.labelReadID;
            prodInputForm.LoadBoardID = lbID;

            string sID = LibEqmtDriver.MIPI.Lib_Var.labelSocketID;
            prodInputForm.ContactorID = sID;

            if (!SkipProductGUIFlag || !Convert.ToBoolean(webQueryValidation))
            {
                DialogResult rslt = prodInputForm.ShowDialog();

                if (rslt == DialogResult.OK)
                {
                    //OtpReadTest.mfg_ID = prodInputForm.MfgLotID; //ChoonChin (20220225) - Add Mfg readback error check.
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_OP_ID, prodInputForm.OperatorID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_LOT_ID, prodInputForm.LotID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_SUB_LOT_ID, prodInputForm.SublotID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_DIB_ID, prodInputForm.LoadBoardID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_CONTACTOR_ID, prodInputForm.ContactorID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_HANDLER_SN, prodInputForm.HandlerID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PCB_ID, "NA");
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_WAFER_ID, "NA");
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_ASSEMBLY_ID, "MFG" + prodInputForm.MfgLotID);
                    //ChoonChin (20200213) - Instrument info
                    ///ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_INSTRUMENT_INFO, Eq.InstrumentInfo);
                    //ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_DIB_ID, prodInputForm.LoadBoardID);
                }
            }
            else
            {
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_OP_ID, "A0001");
                //ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_LOT_ID, prodInputForm.LotID);
                if (prodInputForm.productTag.Contains("QA")) ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_SUB_LOT_ID, "1AQA");
                else ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_SUB_LOT_ID, "1A");
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_DIB_ID, "LB-0000-000");
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_CONTACTOR_ID, "NaN");
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PCB_ID, "NA");
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_WAFER_ID, "NA");
                ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_ASSEMBLY_ID, "MFG" + "000001");
            }
        }

        private string GetTestPlanPath()
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

        public bool ConfirmRevisionIDfromUser(string revID)
        {
            ProductionLib2.InsepctRevisionMessage msg = new ProductionLib2.InsepctRevisionMessage(revID);
            msg.ShowDialog();

            return msg.PassFlag;
        }

        public void LockClotho()
        {
            if (ClothoDataObject.Instance.EngineeringModewoProduction ||
                ((ClothoDataObject.Instance.USERTYPE & (eUSERTYPE.SUSER | eUSERTYPE.DEBUG)) > 0))
                return;
            string clotho_tester_id = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_ID, "Clotho");
            //Thread t1 = new Thread(LockClotho.LockClothoInputUI);
            Thread t1 = new Thread(ProductionLib2.LockClotho.LockClothoInputUI);
            t1.Start(clotho_tester_id);
        }
    }

    public class ProductionTestTimeDebug
    {
        public bool IsDebugMode { get; set; }
        private int m_pcdCounter;
        private int m_passCount;        // monitor the number of pass, 2nd pass to generate time log.

        public ProductionTestTimeDebug()
        {
        }

        public string GetPcd()
        {
            // delete file.
            string logDb = GetReportFileFullPath("TestTimeLog.txt");
            bool isFileExist = File.Exists(logDb);
            if (isFileExist)
            {
                File.Delete(logDb);
            }

            if (IsDebugMode)
            {
                string debugPcd = String.Format("DebugPCD{0}", m_pcdCounter++);
                return debugPcd;
            }
            string pcd = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TAG, "");
            if (String.IsNullOrEmpty(pcd))
            {
                return String.Empty;
            }
            string pcdVersion = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_VER, "");
            string pcdNameVersion = String.Format("{0}_{1}", pcd, pcdVersion);
            return pcdNameVersion;
        }

        /// <summary>
        /// True if pass for the 2nd time in a test session.
        /// </summary>
        public bool IsPass2ndTime()
        {
            if (IsDebugMode)
            {
                m_passCount++;
                string msg = String.Format("TestTimeDashboard:Pass count is {0}", m_passCount);
                LoggingManager.Instance.LogInfo(msg);
                return m_passCount > 1;
            }
            if (ResultBuilder.FailedTests.Length > 0)
            {
                bool isPass = ResultBuilder.FailedTests[0].Count == 0;
                if (isPass)
                {
                    m_passCount++;
                }

                bool is2ndPass = m_passCount == 2;
                return is2ndPass;
            }

            m_passCount++;
            string msg2 = String.Format("TestTimeDashboard:Pass count is {0}", m_passCount);
            LoggingManager.Instance.LogInfo(msg2);
            bool is2ndPass2 = m_passCount == 2;
            return is2ndPass2;
        }

        public bool IsPassTest()
        {
            if (IsDebugMode)
            {
                m_passCount++;
                string msg = String.Format("TestTimeDashboard:Pass count is {0}", m_passCount);
                LoggingManager.Instance.LogInfo(msg);
                return m_passCount > 1;
            }
            if (ResultBuilder.FailedTests.Length > 0)
            {
                bool isPass = ResultBuilder.FailedTests[0].Count == 0;
                if (isPass)
                    return true;
                return false;
            }

            m_passCount++;
            string msg2 = String.Format("TestTimeDashboard:Pass count is {0}", m_passCount);
            LoggingManager.Instance.LogInfo(msg2);
            bool is2ndPass2 = m_passCount == 2;
            return is2ndPass2;
        }

        public bool IsToDeactivate()
        {
            return !IsDebugMode;
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

    public class TestLineFixedCondition
    {
        public string PcdPackageName { get; set; }
        public string ProductTag { get; set; }
        public string TesterName { get; set; }
        public string ZDbFolder { get; set; }

        /// <summary>
        /// RF1 or RF2.
        /// </summary>
        public string TestType { get; set; }

        public string ContractManufacturer { get; set; }

        public void SetContractManufacturer(string zdbFolder)
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

                default:
                    ContractManufacturer = "Others";
                    break;
            }
        }
    }
}