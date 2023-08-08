using Avago.ATF.CrossDomainAccess;
using Avago.ATF.IOLibrary;
using Avago.ATF.LightweightDriver;
using Avago.ATF.Shares;
using Avago.ATF.StandardLibrary;
using ClothoSharedItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TestPlanDriver
{
    public partial class FormDoNOTTouch : Form
    {
        // Switch Rule from here
        // NOTE for x86, need 3 layer upwards
        //      for ANY CPU, only need 2 layer upwards
        private string RulePath = "";

        private string TestPlanPath = "";

        private bool m_1stTimeRunTestPlan = true;

        private bool needUninit = false;

        private int _maxSitesNum = 0;

        private TestPlanLightweightRunner TheRunner = new TestPlanLightweightRunner();

        private string liteDriverStartupPath = "";
        private bool stopLooping = false;

        private DebugEnvVars _debugEnvVars = new DebugEnvVars();

        public FormDoNOTTouch()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();

            this.Text = "TestPlanPlugInDriver V" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            liteDriverStartupPath = Application.StartupPath;
            string debugEnvVarsXmlFileName = Path.Combine(liteDriverStartupPath, "DebugEnvVars.xml");

            if (File.Exists(debugEnvVarsXmlFileName))
            {
                try
                {
                    _debugEnvVars = DebugEnvVars.LoadFromXml(debugEnvVarsXmlFileName);
                }
                finally
                {
                    if (_debugEnvVars == null)
                    {
                        _debugEnvVars = new DebugEnvVars();
                    }
                }
            }

            // "C:\\Avago.ATF\\Data\\ATFTestPlanTemplate\\TestPlanDriver\\bin\\Debug"
            // So no matter that what's the end, just pick the substring until 2rd '\' then append "\Data\TestPlans"
            // Here need set up the TestPlan Root Folder Path
            bool validPath = false;
            int catchPos = liteDriverStartupPath.IndexOf('\\', 0);
            if (catchPos > -1)
            {
                // get 1st, then find 2nd
                catchPos = liteDriverStartupPath.IndexOf('\\', catchPos + 1);
                if (catchPos > -1)
                {
                    string versionRootPath = liteDriverStartupPath.Substring(0, catchPos);

#if CLOTHO_ORIGINAL
                    // get the 2nd, enough for us to build
                    ATFRTE.Instance.TestPlanRootFolder = versionRootPath + @"\Data\TestPlans";
                    TestPlanPath = versionRootPath + @"\Data\ATFTestPlanTemplate\TestPlanDriver\TestPlan.cs";
                    RulePath = versionRootPath + @"\Data\ATFTestPlanTemplate\TestPlanDriver\Rule.cs";
                    validPath = true;
#else

                    // get the 2nd, enough for us to build
                    ATFRTE.Instance.TestPlanRootFolder = versionRootPath + @"\Data\TestPlans";
                    string TestPlanDriverDir = liteDriverStartupPath.Remove(liteDriverStartupPath.IndexOf("\\TestPlanDriver\\")) + "\\TestPlanDriver\\";
                    TestPlanPath = TestPlanDriverDir + "TestPlan.cs";
                    RulePath = TestPlanDriverDir + "Rule.cs";
                    validPath = File.Exists(TestPlanPath) & File.Exists(RulePath);
#endif
                }
            }

            if (!validPath)
            {
                MessageBox.Show("Fail to build up the test plans root folder path from " + liteDriverStartupPath, "Abort!");
                Application.Exit();
            }

            DirectoryInfo directory = new DirectoryInfo(ATFRTE.Instance.TestPlanRootFolder);
            int idx = -1;
            int pkgDemoIdx = 0;
            foreach (DirectoryInfo d in directory.GetDirectories())
            {
                idx++;
                string name = d.Name.Trim();

                if (string.Compare(name, "PkgDemo", true) == 0)
                    pkgDemoIdx = idx;

                comboBoxPackages.Items.Add(name);
            }

            // Default use the 1st one
            if (comboBoxPackages.Items.Count > 0)
            {
                comboBoxPackages.SelectedIndex = pkgDemoIdx;
                ATFRTE.Instance.CurPackageTag = (string)comboBoxPackages.Items[pkgDemoIdx];
            }

            string err = "";
            List<string> calHandlerTypes = CalHandlerScanner.CollectAllHandlerTypes(ref err);
            if (calHandlerTypes == null)
            {
                MessageBox.Show("Fail to allocate CAL Handler Types: " + err, "Abort!");
                Application.Exit();
            }

            comboBoxCalHandlerSelector.DataSource = calHandlerTypes;
            comboBoxCalHandlerSelector.SelectedIndex = 0;
            linkLabelResultFilePath.Text = "";

            ATFRTE.Instance.HandlerType = ((string)comboBoxCalHandlerSelector.SelectedItem).Substring(HandlerConstants.Tag_CalHandlerName_PREFIX.Length);
        }

        /// <summary>
        /// Nothing to do with Rule stuff
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonInit_Click(object sender, EventArgs e)
        {
            if (ATFRTE.Instance.CurPackageTag.Length < 1)
            {
                MessageBox.Show("MUST Provide Valid Package", "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(ATFRTE.Instance.TestPlanRootFolder + @"\" + ATFRTE.Instance.CurPackageTag))
            {
                MessageBox.Show("Package " + ATFRTE.Instance.CurPackageTag + " NOT Exist!", "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string testplanClassName = ClothoFileUtilities.Get1stMatchedSeg(TestPlanPath, TestPlanContentConstants.TAG_TestPlanClassStart, TestPlanContentConstants.TAG_TestPlanClassEnd);
            if (testplanClassName == "")
                return;

            IATFTest testPlanInstance;
            try
            {
                testPlanInstance = (IATFTest)Activator.CreateInstance(Type.GetType(testplanClassName));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fail to Create '" + testplanClassName + "' Instance: " + ex.Message, "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string ruleClassName = "";
            IATFAdaptiveSampling ruleInstance = null;

            if (checkBoxAdaptiveSamplingOnOff.Checked)
            {
                ruleClassName = ClothoFileUtilities.Get1stMatchedSeg(RulePath, TestPlanContentConstants.TAG_RuleClassStart, TestPlanContentConstants.TAG_RuleClassEnd);
                if (ruleClassName == "")
                    return;

                try
                {
                    ruleInstance = (IATFAdaptiveSampling)Activator.CreateInstance(Type.GetType(ruleClassName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fail to Create '" + ruleClassName + "' Instance: " + ex.Message, "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (textBoxTestArgString.Text.Trim().EndsWith(";"))
            {
                MessageBox.Show("Parameter string Must NOT End with ';'.", "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            #region Init MaxSiteNum Assignment

            _maxSitesNum = (int)numericUpDownMaxSitesNum.Value;
            if ((_maxSitesNum < 2) || ((_maxSitesNum % 2) != 0))
            {
                MessageBox.Show("Invalid MaxSitesNum: " + _maxSitesNum.ToString(), "Abort Test Plan Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ATFSharedData.Instance.SetMaxHWSites(_maxSitesNum);

            #endregion Init MaxSiteNum Assignment

            #region Set ATFRTE and ATFCrossDomainWrapper before ATFInit()

            _debugEnvVars.CurPackageTag = (string)comboBoxPackages.SelectedItem;

            //// Overwrite MaxSiteNum from UI setting.
            //_debugEnvVars.MaxSitesNum = _maxSitesNum;

            ATFRTE.Instance.CurDevelopTestPlan = Path.GetFileName(this.TestPlanPath);
            ATFRTE.Instance.CurDevelopTestPlanFullPath = this.TestPlanPath;
            ATFRTE.Instance.CurLotID = _debugEnvVars.LotID;
            ATFRTE.Instance.CurPackageTag = _debugEnvVars.CurPackageTag;
            ATFRTE.Instance.CurUserAccessLevel = AuthorityGroup.Development;
            ATFRTE.Instance.CurUserName = _debugEnvVars?.UserName ?? "sUser";
            ATFRTE.Instance.HandlerAddress = _debugEnvVars.HandlerAddress;
            ATFRTE.Instance.HandlerSN = _debugEnvVars.HandlerSN;
            ATFRTE.Instance.HandlerType = _debugEnvVars.HandlerType;
            ATFRTE.Instance.IPAddress = _debugEnvVars.IPAddress;
            ATFRTE.Instance.MaxSitesNum = _debugEnvVars.MaxSitesNum;
            ATFRTE.Instance.PCDRemoteSharePath = _debugEnvVars.PCDRemoteSharePath;
            ATFRTE.Instance.TesterID = _debugEnvVars.TesterID;
            ATFRTE.Instance.TesterType = _debugEnvVars.TesterType;
            // ATFRTE.Instance.TestPlanRootFolder already set in ctor().

            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_HANDLER_SN, _debugEnvVars.HandlerSN);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_HANDLER_Type, _debugEnvVars.HandlerType);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_IPADDRESS, _debugEnvVars.IPAddress);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_LOT_ID, _debugEnvVars.LotID);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_TESTER_ID, _debugEnvVars.TesterID);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_TESTER_TYPE, _debugEnvVars.TesterType);

            ATFCrossDomainWrapper.StoreIntToCache(PublishTags.PUBTAG_CUR_SN, TheRunner.CurrentSN + 1);

            #endregion Set ATFRTE and ATFCrossDomainWrapper before ATFInit()

            Trace.WriteLine("Call INIT with " + textBoxInitArgString.Text.Trim());
            string initRet = TheRunner.Init(testPlanInstance, textBoxInitArgString.Text.Trim(), TestPlanPath, ruleInstance, RulePath, checkBoxBuddyFile.Checked, checkBoxTraceFile.Checked);

            linkLabelResultFilePath.Text = TheRunner.ResultFilePath;

            if (initRet.StartsWith(TestPlanRunConstants.RunFailureFlag))
            {
                MessageBox.Show(initRet, "INIT FAILURE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                Trace.WriteLine("INIT Result: " + initRet);
            }

            if (checkBoxAdaptiveSamplingOnOff.Checked)
                // First time clean up
                ATFSharedData.Instance.ResetAdaptiveSamplingRuleConfig();

            buttonInit.Enabled = false;
            buttonStartTestPlan.Enabled = true;
            buttonDoLot.Enabled = true;
            buttonUnInit.Enabled = true;
            buttonExit.Enabled = false;

            checkBoxAdaptiveSamplingOnOff.Enabled = false;
            checkBoxBuddyFile.Enabled = false;
            checkBoxTraceFile.Enabled = false;
            checkBoxCalFileInterpolate.Enabled = true;

            textBoxDoLotArgString.Enabled = true;
            textBoxTestArgString.Enabled = true;
            numericUpDownLoopCnt.Enabled = true;
            numericUpDownLoopDelay.Enabled = true;

            buttonLoopAbort.Enabled = false;

            needUninit = true;
        }

        /// <summary>
        /// Nothing to do with Rule Stuff
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUnInit_Click(object sender, EventArgs e)
        {
            Trace.WriteLine("Call UNINIT with " + textBoxUnInitArgString.Text.Trim());
            string uninitRet = TheRunner.UnInit(textBoxUnInitArgString.Text.Trim());

            if (uninitRet.StartsWith(TestPlanRunConstants.RunFailureFlag))
            {
                MessageBox.Show(uninitRet, "UN-INIT FAILURE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Trace.WriteLine("UNINIT Result: " + uninitRet);
            }

            m_1stTimeRunTestPlan = true;
            buttonExit.Enabled = true;
            needUninit = false;
            buttonDoLot.Enabled = false;
            buttonInit.Enabled = true;
            buttonStartTestPlan.Enabled = false;
            buttonUnInit.Enabled = false;
            linkLabelResultFilePath.Text = "";
        }

        /// <summary>
        /// Rule Relevant
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStartTestPlan_Click(object sender, EventArgs e)
        {
            stopLooping = false;

            int loopcnt = (int)numericUpDownLoopCnt.Value;
            if (loopcnt > 1)
                buttonLoopAbort.Enabled = true;
            else
                buttonLoopAbort.Enabled = false;

            ATFReturnResult testplanRet = null;
            string ruleRet = "";

            listBoxRunResult.Items.Add("****************************************");
            listBoxRunResult.Items.Add("");
            listBoxRunResult.SelectedIndex = listBoxRunResult.Items.Count - 1;

            for (int idx = 0; idx < loopcnt; idx++)
            {
                Application.DoEvents();
                if (stopLooping)
                {
                    listBoxRunResult.Items.Add("Abort per request");
                    listBoxRunResult.Items.Add("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                    listBoxRunResult.Items.Add("");
                    listBoxRunResult.SelectedIndex = listBoxRunResult.Items.Count - 1;
                }

                listBoxRunResult.Items.Add(string.Format("{0}:     #{1}/{2} Run ({3})", DateTime.Now.ToString(), idx + 1, loopcnt, textBoxTestArgString.Text.Trim()));

                if (checkBoxAdaptiveSamplingOnOff.Checked)
                {
                    Dictionary<string, bool> tempMask = ATFSharedData.Instance.ASRuleBitMask;
                    StringBuilder sbTemp = new StringBuilder("BitMask: ");
                    foreach (string key in tempMask.Keys)
                        sbTemp.AppendFormat("{0}: {1}; ", key, tempMask[key] ? "1" : "0");
                    listBoxRunResult.Items.Add(sbTemp.ToString());
                }

                listBoxRunResult.SelectedIndex = listBoxRunResult.Items.Count - 1;

                #region Set ATFRTE and ATFCrossDomainWrapper before first ATFTest()

                if (m_1stTimeRunTestPlan)
                {
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_ASSEMBLY_ID, _debugEnvVars.AssemblyID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_CUR_RESULT_FILE, Path.GetFileName(linkLabelResultFilePath.Text));
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_CONTACTOR_ID, _debugEnvVars.ContractorID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_DIB_ID, _debugEnvVars.LoadBoardID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_OP_ID, _debugEnvVars.OpID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PACKAGE_TAG, _debugEnvVars.CurPackageTag);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, ClothoDataObject.Instance.ClothoRootDir);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PACKAGE_TP_FULLPATH, this.TestPlanPath);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PCB_ID, _debugEnvVars.PCBID);
                    ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_SUB_LOT_ID, _debugEnvVars.SubLotID);
                }

                ATFCrossDomainWrapper.StoreIntToCache(PublishTags.PUBTAG_CUR_SN, TheRunner.CurrentSN + 1);

                #endregion Set ATFRTE and ATFCrossDomainWrapper before first ATFTest()

                // Set as ManualClickStyle running
                ATFCrossDomainWrapper.SetTriggerByManualClickFlag(true);

                this.Cursor = Cursors.WaitCursor;
                try
                {
                    testplanRet = TheRunner.Test(textBoxTestArgString.Text.Trim(), checkBoxAdaptiveSamplingOnOff.Checked, ref ruleRet, m_1stTimeRunTestPlan);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Test Plan Execution Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }

                if (testplanRet == null) return;
                listBoxRunResult.Items.Add(string.Format("{0}:     TestPlan Return: {1}", DateTime.Now.ToString(), StringProcessHelper.ConvertFullResultToStringWithLengthLimit(testplanRet, 500)));

                if (stopLooping)
                {
                    listBoxRunResult.Items.Add("Abort per request");
                    break;
                }

                if (checkBoxAdaptiveSamplingOnOff.Checked)
                {
                    listBoxRunResult.Items.Add(string.Format("{0}:     Rule Return: {1}", DateTime.Now.ToString(), ruleRet));

                    Dictionary<string, bool> tempMask = ATFSharedData.Instance.ASRuleBitMask;
                    StringBuilder sbTemp = new StringBuilder("BitMask: ");
                    foreach (string key in tempMask.Keys)
                        sbTemp.AppendFormat("{0}: {1}; ", key, tempMask[key] ? "1" : "0");
                    listBoxRunResult.Items.Add(sbTemp.ToString());
                }
                listBoxRunResult.SelectedIndex = listBoxRunResult.Items.Count - 1;

                // After 1st time run complete
                if (m_1stTimeRunTestPlan)
                {
                    m_1stTimeRunTestPlan = false;
                }

                Thread.Sleep((int)numericUpDownLoopDelay.Value);
            }
        }

        private void checkBoxCalFileInterpolate_CheckedChanged(object sender, EventArgs e)
        {
            ATFCrossDomainWrapper.Cal_SwitchInterpolationFlag(checkBoxCalFileInterpolate.Checked);
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonDoLot_Click(object sender, EventArgs e)
        {
            Trace.WriteLine("Call DoLot with " + textBoxDoLotArgString.Text.Trim());
            string doLotRet = TheRunner.CloseLot(textBoxDoLotArgString.Text.Trim());

            #region Set ATFRTE and ATFCrossDomainWrapper after ATFLot()

            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_ASSEMBLY_ID, String.Empty);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_CONTACTOR_ID, String.Empty);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_DIB_ID, String.Empty);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_OP_ID, String.Empty);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_PCB_ID, String.Empty);
            ATFCrossDomainWrapper.StoreStringToCache(PublishTags.PUBTAG_SUB_LOT_ID, String.Empty);
            ATFCrossDomainWrapper.StoreIntToCache(PublishTags.PUBTAG_CUR_SN, TheRunner.CurrentSN + 1);

            #endregion Set ATFRTE and ATFCrossDomainWrapper after ATFLot()

            if (doLotRet.StartsWith(TestPlanRunConstants.RunFailureFlag))
            {
                MessageBox.Show(doLotRet, "DoLot FAILURE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                Trace.WriteLine("DoLot Result: " + doLotRet);
            }
        }

        private void comboBoxPackages_SelectedIndexChanged(object sender, EventArgs e)
        {
            ATFRTE.Instance.CurPackageTag = (string)comboBoxPackages.SelectedItem;
        }

        private void FormDoNOTTouch_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (needUninit)
            {
                MessageBox.Show("'Un-Init' is Required Before Exit.", "Run 'Un-Init'", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                this.Cursor = Cursors.WaitCursor;

                try
                {
                    Trace.WriteLine("Call UNINIT with " + textBoxUnInitArgString.Text.Trim());
                    string uninitRet = TheRunner.UnInit(textBoxUnInitArgString.Text.Trim());

                    if (uninitRet.StartsWith(TestPlanRunConstants.RunFailureFlag))
                    {
                        MessageBox.Show(uninitRet, "UN-INIT FAILURE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        Trace.WriteLine("UNINIT Result: " + uninitRet);
                    }
                }
                catch
                {
                    return;
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void comboBoxCalHandlerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            ATFRTE.Instance.HandlerType = ((string)comboBoxCalHandlerSelector.SelectedItem).Substring(HandlerConstants.Tag_CalHandlerName_PREFIX.Length);
        }

        private void buttonLoopAbort_Click(object sender, EventArgs e)
        {
            stopLooping = true;
        }

        private void linkLabelResultFilePath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string resultfullpath = linkLabelResultFilePath.Text;
            if (File.Exists(linkLabelResultFilePath.Text))
            {
                Process process = new Process();
                process.StartInfo.FileName = linkLabelResultFilePath.Text;
                process.Start();
            }
            else
            {
                MessageBox.Show("The result file not exist", "Result File Access Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openDebugEnvVarsButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new FormDoNOTTouch_DebugVars())
            {
                dlg.TargetObject = _debugEnvVars;

                this.Invoke((MethodInvoker)(() =>
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        dlg.TargetObject.CopyTo(_debugEnvVars);
                    }
                }));
            }
        }
    }
}