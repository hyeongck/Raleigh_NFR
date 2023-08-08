using ClothoSharedItems.Device;
using NationalInstruments.DAQmx;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ClothoSharedItems
{
    public partial class FormSeoulHelper : Form
    {
        private bool Initialzed = false;
        private RunOption FixedAfterLock = RunOption.RxFunctional;
        private RunOption FixedFromTCF = RunOption.Burn2DID | RunOption.Read2DID | RunOption.SIMULATE;
        private Version version = new Version(1, 0, 2);
        private bool FTPEnable = false;
        private string BasePath;
        private string MyWaveformDic;
        private BackgroundWorker _DownloadWorker = null;

        public FormSeoulHelper()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();

            InvokeTitle();

            this.DoubleBuffered();
            dgvEbrs.DoubleBuffered();
            dgvWaferSet.DoubleBuffered();

            clbTestOption.Items.AddRange(Enum.GetNames(typeof(RunOption)));

            _DownloadWorker = new BackgroundWorker();
            _DownloadWorker.WorkerReportsProgress = false;
            _DownloadWorker.WorkerSupportsCancellation = true;
            _DownloadWorker.DoWork += new DoWorkEventHandler(Downloader_DoWork);
            _DownloadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Downloader_RunWorkerComplete);
        }

        private void FormSeoulHelper_Load(object sender, EventArgs e)
        {
            var options = Enum.GetValues(typeof(RunOption)) as RunOption[];
            for (int i = 0; i < options.Count(); i++)
            {
                if (FixedFromTCF.HasFlag(options[i]))
                    clbTestOption.SetItemCheckState(i, CheckState.Indeterminate);
                else
                    clbTestOption.SetItemChecked(i, ClothoDataObject.Instance.RunOptions.HasFlag(options[i]));
            }

            Initialzed = true;
            GetEBRDatafromGoogleSheet();

            dgvWaferSet.Columns.Add("ColWafer", "Wafer");
            dgvWaferSet.Columns.Add("ColCount", "Count");
            dgvWaferSet.Columns.Add("ColPIDstart", "PID Start");
            dgvWaferSet.Columns.Add("ColPIDend", "PID End");

            dgvWaferSet.Columns["ColPIDstart"].ReadOnly = true;
            dgvWaferSet.Columns["ColPIDend"].ReadOnly = true;
        }

        public void FormDispose()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    this.Dispose();
                }));
            }
            else
            {
                this.Dispose();
            }
        }

        private void btnCheckFTP_Click(object sender, EventArgs e)
        {
            using (Helper.FTPClient fc = new Helper.FTPClient())
            {
                FTPEnable = fc.isValidConnection();
                btnFTPDownload.Enabled = FTPEnable;

                if (FTPEnable)
                {
                    BasePath = Path.GetDirectoryName(ClothoDataObject.Instance.ClothoRootDir);
                    MyWaveformDic = Path.Combine(BasePath, "FileNeeded", "waveform-info.xml");
                    fc.DownloadFileList("_RF1_Share/waveform-info.xml", Path.GetDirectoryName(MyWaveformDic));

                    if (File.Exists(MyWaveformDic))
                    {
                        //AppendMessage("Seoul FTP connection OK, you can download waveform and QC from Jay world.");

                        Readwaveformset(MyWaveformDic);
                        btnCheckFTP.Enabled = false;
                    }
                    else
                        btnFTPDownload.Enabled = false;
                }
            }
        }

        private void FormSeoulHelper_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                //MessageBox.Show(ClothoConfigurationDataObject.Instance.RunOptions.ToString());
            }
            else if (e.KeyCode == Keys.F5)
            {
                GetEBRDatafromGoogleSheet();
            }
        }

        private void clbTestOption_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var TargetOption = (RunOption)Enum.Parse(typeof(RunOption), clbTestOption.Items[e.Index].ToString());

            if (FixedFromTCF.HasFlag(TargetOption))
            {
                e.NewValue = CheckState.Indeterminate;
                return;
            }

            if ((Initialzed == true) &&
               (ClothoDataObject.Instance.RunOptionLocked && FixedAfterLock.HasFlag(TargetOption)))
            {
                if (e.NewValue == CheckState.Checked) e.NewValue = CheckState.Unchecked;
                else if (e.NewValue == CheckState.Unchecked) e.NewValue = CheckState.Checked;
                return;
            }

            if (e.NewValue == CheckState.Checked)
            {
                if (!ClothoDataObject.Instance.RunOptions.HasFlag(TargetOption))
                    ClothoDataObject.Instance.RunOptions |= TargetOption;
            }
            else if (e.NewValue == CheckState.Unchecked)
            {
                if (ClothoDataObject.Instance.RunOptions.HasFlag(TargetOption))
                    ClothoDataObject.Instance.RunOptions &= ~TargetOption;
            }
        }

        private void btnKillExcel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.ProcessStartInfo pri = new System.Diagnostics.ProcessStartInfo();
            System.Diagnostics.Process pro = new System.Diagnostics.Process();

            pri.FileName = "cmd.exe";
            pri.CreateNoWindow = true;
            pri.UseShellExecute = false;

            pri.RedirectStandardInput = true;
            pri.RedirectStandardOutput = true;
            pri.RedirectStandardError = true;

            pro.StartInfo = pri;
            pro.Start();

            var _currTcf = Path.GetFileName(Avago.ATF.StandardLibrary.ATFCrossDomainWrapper.GetStringFromCache(Avago.ATF.StandardLibrary.PublishTags.PUBTAG_PACKAGE_TCF_FULLPATH, ""));
            var processes = from p in System.Diagnostics.Process.GetProcessesByName("EXCEL") select p;
            foreach (var process in processes)
            {
                if (process.MainWindowTitle == "" || process.MainWindowTitle.CIvContains(_currTcf))
                {
                    try { process.Kill(); }
                    catch (System.ComponentModel.Win32Exception) { }
                    catch (Exception) { }
                }
            }

            pro.StandardInput.WriteLine(@"taskkill /f /im Avago.ATF.LogService.exe");
            pro.StandardInput.WriteLine(@"taskkill /f /im Avago.ATF.UIs.exe");
            pro.StandardInput.WriteLine(@"taskkill /f /im TestPlanDriver.exe");
            pro.StandardInput.Close();
            pro.Close();
        }

        /// <summary>
        /// https://developers.google.com/sheets/api/guides/concepts
        /// API Key from Jay
        /// </summary>
        private void GetEBRDatafromGoogleSheet()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    #region Non-official api

                    //using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.fureweb.com/spreadsheets/1V3YxArv2WsMhSwL47YmooI1NdR-YNU1MG_OoMW2H-Gk"))
                    //{
                    //    request.Headers.TryAddWithoutValidation("accept", "*/*");
                    //    var response = httpClient.SendAsync(request);
                    //    response.Wait();
                    //    string strJson = response.Result.Content.ReadAsStringAsync().Result;

                    //    var jsonLinq = JObject.Parse(strJson);

                    //    // Find the first array using Linq
                    //    var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();
                    //    var trgArray = new JArray();
                    //    foreach (JObject row in srcArray.Children<JObject>())
                    //    {
                    //        var cleanRow = new JObject();
                    //        foreach (JProperty column in row.Properties())
                    //        {
                    //            // Only include JValue types
                    //            if (column.Name.CIvEqualsAnyOf("EBR Type", "Project", "CM", "EBR", "Sub Lot #", "Purpose", "Out Q'ty"))
                    //            {
                    //                if (column.Value is JValue)
                    //                {
                    //                    string colName = column.Name;
                    //                    if (colName.CIvEquals("Sub Lot #")) colName = "SubLot";

                    //                    cleanRow.Add(colName, column.Value);
                    //                }
                    //            }
                    //        }

                    //        cleanRow.TryGetValue("Out Q'ty", out JToken tVal);
                    //        if (string.IsNullOrWhiteSpace(tVal?.ToString()))
                    //            trgArray.Add(cleanRow);
                    //    }

                    //    var dt = JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());

                    //    dgvEbrs.DataSource = dt;

                    //    if (dgvEbrs.RowCount > 1)
                    //        dgvEbrs.FirstDisplayedScrollingRowIndex = dgvEbrs.RowCount - 1;
                    //}

                    #endregion Non-official api

                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://content-sheets.googleapis.com/v4/spreadsheets/1V3YxArv2WsMhSwL47YmooI1NdR-YNU1MG_OoMW2H-Gk/values/'Summary'!a%3As?valueRenderOption=FORMATTED_VALUE&key=AIzaSyBY5nc2u_sFKSRJ2Qnw-6yxFScwfl5d2r4"))
                    {
                        var response = httpClient.SendAsync(request);
                        response.Wait();
                        string strJson = response.Result.Content.ReadAsStringAsync().Result;
                        var jsonLinq = JObject.Parse(strJson);

                        // Find the first array using Linq
                        var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();

                        DataTable dtAll = new DataTable();
                        foreach (var jRow in srcArray.Children())
                        {
                            if (dtAll.Columns.Count == 0)
                            {
                                foreach (var jItem in jRow)
                                {
                                    string colName = jItem.ToString();
                                    if (colName.CIvEquals("Sub Lot #")) colName = "SubLot";

                                    dtAll.Columns.Add(colName);
                                }
                            }
                            else
                            {
                                dtAll.Rows.Add(jRow.ToArray());
                            }
                        }

                        DataTable dtSubset = dtAll.DefaultView.ToTable(false, new string[] { "EBR Type", "CM", "Project", "EBR", "SubLot", "Purpose" });
                        dgvEbrs.DataSource = dtSubset;

                        if (dgvEbrs.RowCount > 1)
                            dgvEbrs.FirstDisplayedScrollingRowIndex = dgvEbrs.RowCount - 1;
                    }
                }
            }
            catch
            {
                //System.AggregateException; inside non-avaialbe network service
            }
        }

        private void textBoxFilter_TextChanged(object sender, EventArgs e)
        {
            if (dgvEbrs.DataSource != null)
            {
                List<string> lbQuery = new List<string>();
                if (!string.IsNullOrWhiteSpace(tbxFilterPjt.Text)) lbQuery.Add(string.Format("{0} like '%{1}%'", "Project", tbxFilterPjt.Text));
                if (!string.IsNullOrWhiteSpace(tbxFilterEBR.Text)) lbQuery.Add(string.Format("{0} like '%{1}%'", "EBR", tbxFilterEBR.Text));
                if (!string.IsNullOrWhiteSpace(tbxFilterSublot.Text)) lbQuery.Add(string.Format("{0} like '%{1}%'", "SubLot", tbxFilterSublot.Text));

                (dgvEbrs.DataSource as DataTable).DefaultView.RowFilter = lbQuery.JoinToString(" AND ");
            }
        }

        private void tbxWafers_TextChanged(object sender, EventArgs e)
        {
            dgvWaferSet.Rows.Clear();

            if (!string.IsNullOrWhiteSpace(tbxWafers.Text))
            {
                using (StringReader sr = new StringReader(tbxWafers.Text))
                {
                    Regex reg = new Regex(@"(?<wafer>\S+)(\s?)+-(\s?)+(?<count>\d+)");
                    string line = "";
                    int stackInit = 1;
                    int stackPost = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var isMatch = reg.Match(line);
                        if (isMatch.Success)
                        {
                            var numthis = int.Parse(isMatch.Groups["count"].Value);
                            stackPost += numthis;

                            string[] row1 = new string[] { isMatch.Groups["wafer"].Value, isMatch.Groups["count"].Value, stackInit.ToString(), stackPost.ToString() };
                            var dgvRow = dgvWaferSet.Rows.Add(row1);
                            stackInit += numthis;
                        }
                    }

                    UpdateWaferInfo();
                }
            }
        }

        private void dgvWaferSet_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvWaferSet.Columns["ColWafer"].Index)
            {
                if (dgvWaferSet["ColCount", e.RowIndex].Value == null)
                    dgvWaferSet["ColCount", e.RowIndex].Value = "1";

                dgvWaferSet.Update();
            }
            else if (!validated && e.ColumnIndex == dgvWaferSet.Columns["ColCount"].Index)
            {
                dgvWaferSet[e.ColumnIndex, e.RowIndex].Value = 0;
                dgvWaferSet.Update();
                validated = true;
            }

            int stackInit = 1;
            int stackPost = 0;

            for (int i = 0; i < dgvWaferSet.Rows.Count - 1; i++)
            {
                var strcolcount = dgvWaferSet["ColCount", i].Value ?? "1";
                var numthis = int.Parse(strcolcount.ToString());

                stackPost += numthis;
                dgvWaferSet["ColPIDstart", i].Value = stackInit;
                dgvWaferSet["ColPIDend", i].Value = stackPost;
                stackInit += numthis;
                dgvWaferSet.Update();
                UpdateWaferInfo();
            }
        }

        private void UpdateWaferInfo()
        {
            Dictionary<int, string> dicWaferset = new Dictionary<int, string>();

            try
            {
                for (int i = 0; i < dgvWaferSet.Rows.Count - 1; i++)
                {
                    if (!int.TryParse(dgvWaferSet[dgvWaferSet.Columns["ColCount"].Index, i].Value.ToString(), out int Cout) || Cout == 0) continue;

                    var startVal = Convert.ToInt32(dgvWaferSet[dgvWaferSet.Columns["ColPIDstart"].Index, i].Value);
                    var EndVal = Convert.ToInt32(dgvWaferSet[dgvWaferSet.Columns["ColPIDend"].Index, i].Value);
                    var WaferString = dgvWaferSet[dgvWaferSet.Columns["ColWafer"].Index, i].Value?.ToString() ?? "";

                    for (int j = startVal; j <= EndVal; j++)
                    {
                        dicWaferset.Add(j, WaferString);
                    }
                }
            }
            catch { dicWaferset = null; }
            finally { ClothoDataObject.Instance.WaferInformation = dicWaferset; }
        }

        private bool validated = false;

        private void dgvWaferSet_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == dgvWaferSet.Columns["ColCount"].Index)
            {
                int newInteger;
                if (dgvWaferSet.Rows[e.RowIndex].IsNewRow) { return; }

                if (!int.TryParse(e.FormattedValue.ToString(), out newInteger) || newInteger < 0 || newInteger > 40000)
                {
                    validated = false;
                    MessageBox.Show("the value must be between 0 and 40000");
                }
                else
                    validated = true;
            }
        }

        private void dgvWaferSet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                var selectedCellCount = dgvWaferSet.GetCellCount(DataGridViewElementStates.Selected);
                if (selectedCellCount > 1)
                {
                    bool initValReturned = false;
                    string initValue = "";

                    for (int i = 0; i < selectedCellCount; i++)
                    {
                        if (dgvWaferSet.SelectedCells[i].ColumnIndex <= dgvWaferSet.Columns["ColCount"].Index)
                        {
                            if (initValReturned == false && dgvWaferSet[dgvWaferSet.SelectedCells[i].ColumnIndex, dgvWaferSet.SelectedCells[i].RowIndex].Value != null)
                            {
                                initValue = (sender as DataGridViewTextBoxEditingControl).Text;
                                initValReturned = true;
                                continue;
                            }
                            else if (initValReturned)
                            {
                                dgvWaferSet[dgvWaferSet.SelectedCells[i].ColumnIndex, dgvWaferSet.SelectedCells[i].RowIndex].Value = initValue;
                            }
                        }
                    }
                    dgvWaferSet.Update();
                    dgvWaferSet.EndEdit();
                    UpdateWaferInfo();
                }
            }
        }

        private void dgvWaferSet_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is DataGridViewTextBoxEditingControl tb)
            {
                tb.KeyDown -= dgvWaferSet_KeyDown;
                tb.KeyDown += dgvWaferSet_KeyDown;
            }
        }

        public void InvokeTitle(bool Simulate = false)
        {
            string TargetText = string.Format("Seoul Helper v{0}{1}", version.ToString(3), Simulate ? " [Simulated]" : "");

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    this.Text = TargetText;
                }));
            }
            else
            {
                this.Text = TargetText;
            }
        }

        private Dictionary<string, string> wvfrmInfos = new Dictionary<string, string>();
        private Dictionary<string, string> QcInfos = new Dictionary<string, string>();

        private void btnFTPDownload_Click(object sender, EventArgs e)
        {
            if (ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.Waveform]?.Count() > 0 || ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.QC]?.Count() > 0)
            {
                if (_DownloadWorker.IsBusy != true)
                {
                    tbxLogs.Text = "";
                    btnFTPDownload.BackColor = SystemColors.Control;
                    btnFTPDownload.Enabled = false;

                    Application.DoEvents();

                    _DownloadWorker.RunWorkerAsync();
                }
            }
        }

        public void AppendMessage(string message, bool passFail = true)
        {
            if (tbxLogs.InvokeRequired)
            {
                tbxLogs.BeginInvoke(new MethodInvoker(delegate
                {
                    tbxLogs.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]: {(passFail ? " PASS " : " FAIL ")}{message}{Environment.NewLine}");
                    tbxLogs.ScrollToCaret();
                }));
            }
            else
            {
                tbxLogs.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]: {(passFail ? " PASS " : " FAIL ")}{message}{Environment.NewLine}");
                tbxLogs.ScrollToCaret();
            }
        }

        private void Downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            Parallel.ForEach(ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.Waveform], item =>
           {
               string file;

               if (ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.Waveform].TryTake(out file))
               {
                   if (wvfrmInfos.ContainsKey(file))
                   {
                       string downloadPath = wvfrmInfos[file];
                       string sPath = BasePath + downloadPath;

                       using (Helper.FTPClient client = new Helper.FTPClient())
                       {
                           if (!Directory.Exists(sPath)) Directory.CreateDirectory(sPath);

                           client.DownloadFileList($"{downloadPath}", sPath);
                           AppendMessage($"Download; {downloadPath}", true);
                       }
                   }
                   else
                   {
                       //AppendMessageToError(string.Format("Not supported waveform, {0}", file));
                   }
               }
           });

            Parallel.ForEach(ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.QC], item =>
            {
                string file;

                if (ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.QC].TryTake(out file))
                {
                    if (QcInfos.ContainsKey(file))
                    {
                        string downloadPath = QcInfos[file];
                        string sPath = BasePath + downloadPath;

                        using (Helper.FTPClient client = new Helper.FTPClient())
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(sPath))) Directory.CreateDirectory(Path.GetDirectoryName(sPath));

                            client.DownloadFileList($"{downloadPath}", Path.GetDirectoryName(sPath));
                            AppendMessage($"Download; {downloadPath}", true);
                        }
                    }
                    else
                    {
                        //AppendMessageToError(string.Format("Not supported vector, {0}", file));
                    }
                }
            });
        }

        private void Downloader_RunWorkerComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }
            else
            {
            }

            if (ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.Waveform]?.Count() > 0 || ClothoDataObject.Instance.DicFailedLoadItems[eLoadItems.QC]?.Count() > 0)
            {
                btnFTPDownload.Enabled = true;
                btnFTPDownload.Text = "RETRY DOWNLOAD";
                btnFTPDownload.BackColor = Color.OrangeRed;
            }
            else
            {
                btnFTPDownload.Text = "Wvfrm/QC PASS";
                btnFTPDownload.BackColor = Color.SkyBlue;
                _DownloadWorker = null;
            }
        }

        private void Readwaveformset(string _Path)
        {
            XElement xmlRoot = XDocument.Load(_Path).Root;
            var xelWaveforms = xmlRoot.Element("Waveforms");
            var xelQcs = xmlRoot.Element("QCs");

            if (xelWaveforms != null)
            {
                wvfrmInfos = xelWaveforms
                    .Elements("waveform")
                    .Select(x => new XmlxyConfigField
                    {
                        xargs = ((string)x.Attribute("I")),
                        Path = (string)x.Attribute("Path")
                    }).ToDictionary(x => x.xargs, y => y.Path) as Dictionary<string, string>;
            }
            if (xelQcs != null)
            {
                QcInfos = xelQcs
                    .Elements("QC")
                    .Select(x => new XmlxyConfigField
                    {
                        xargs = ((string)x.Attribute("vec")),
                        Path = (string)x.Attribute("Path")
                    }).ToDictionary(x => x.xargs, y => y.Path) as Dictionary<string, string>;
            }
        }

        public class XmlxyConfigField
        {
            public string xargs { get; set; }
            public string Path { get; set; }
        }

        private void btnDebugFeature_Click(object sender, EventArgs e)
        {
            Helper.CheckSystemEventsHandlersForFreeze();
        }

        private TemptronicTHChamber t1 = new TemptronicTHChamber();

        private void btnScpiConnect_Click(object sender, EventArgs e)
        {
            t1 = DevSCPI.GetFirstDeviceOfType<TemptronicTHChamber>();
            t1.GPIBAddress = tbxGPIB.Text.Trim();
            if (t1.Open(1))
            {
                gbxTempbox.Enabled = true;
                btnTempOnOff.Text = t1.Power == ONOFF.ON ? "ON" : "OFF";
            }
        }

        private void btnSetTemperature_Click(object sender, EventArgs e)
        {
            if (t1.IsOpen)
            {
                t1.SetTemperature((double)numericUpDown1.Value);
                t1.Power = ONOFF.ON;
                btnTempOnOff.Text = "ON";
            }
        }

        private void btnNITemp_Click(object sender, EventArgs e)
        {
            double result = -999;
            result = Get_Temperature_NISensor();

            if (lblCurrentTempFromNI.InvokeRequired)
            {
                lblCurrentTempFromNI.Invoke(new MethodInvoker(delegate () { lblCurrentTempFromNI.Text = result.ToString(); }));
            }
            else
            {
                lblCurrentTempFromNI.Text = result.ToString();
            }
        }

        public double Get_Temperature_NISensor()
        {
            NationalInstruments.DAQmx.Task temperatureTask = new NationalInstruments.DAQmx.Task();
            AIChannel myAIChannel;
            myAIChannel = temperatureTask.AIChannels.CreateThermocoupleChannel(
                "Dev1/ai0",
                "Temperature",
                -50,
                150,
                AIThermocoupleType.E,
                AITemperatureUnits.DegreesC,
                25
            );

            AnalogSingleChannelReader reader = new
                AnalogSingleChannelReader(temperatureTask.Stream);

            double analogDataIn = double.NaN;
            try
            {
                analogDataIn = reader.ReadSingleSample();
            }
            catch { }
            return analogDataIn;
        }

        public void TemtronicOnOFF(ONOFF _ONOFF)
        {
            if (t1.IsOpen)
            {
                t1.Power = _ONOFF;

                if (btnTempOnOff.InvokeRequired)
                {
                    btnTempOnOff.Invoke(new MethodInvoker(delegate () { btnTempOnOff.Text = _ONOFF.ToString(); }));
                }
                else
                {
                    btnTempOnOff.Text = _ONOFF.ToString();
                }
            }
        }

        private void btnTempOnOff_Click(object sender, EventArgs e)
        {
            if (t1.IsOpen)
            {
                var onoff = t1.Power;

                if (onoff == ONOFF.ON) onoff = ONOFF.OFF;
                else onoff = ONOFF.ON;

                TemtronicOnOFF(onoff);
            }
        }

        private void btnUnsubscribe_Click(object sender, EventArgs e)
        {
            Helper.UnsubscribeSystemEvents();
        }
    }
}