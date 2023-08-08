using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace TestPlanDriver
{
    public partial class FormDoNOTTouch_DebugVars : Form
    {
        public FormDoNOTTouch_DebugVars()
        {
            InitializeComponent();
        }

        public DebugEnvVars TargetObject { get; set; }

        private void FormDoNOTTouch_DebugVars_Load(object sender, EventArgs e)
        {
            if (this.TargetObject == null)
            {
                this.TargetObject = new DebugEnvVars();
            }

            objectPropertyGrid.SelectedObject = this.TargetObject;
        }

        private void loadFromAtfConfigXmlButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = DialogResult.None;

            Thread t = new Thread((ThreadStart)(() =>
            {
                dialogResult = atfConfigXmlOpenFileDialog.ShowDialog();
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (dialogResult == DialogResult.OK)
            {
                if (!File.Exists(atfConfigXmlOpenFileDialog.FileName))
                {
                    MessageBox.Show(this,
                        $"File not found: {atfConfigXmlOpenFileDialog.FileName}",
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                using (DataSet ds = new DataSet())
                {
                    ds.ReadXml(atfConfigXmlOpenFileDialog.FileName);

                    if (ds.Tables.Contains("ConfigItem"))
                    {
                        DataTable dt = ds.Tables["ConfigItem"];
                        DataRow[] configRows = dt.Select("name IN ('IPAddress', 'TesterID', 'HandlerType', 'HandlerSN')");

                        if (configRows == null)
                        {
                            MessageBox.Show(this,
                                "No supported configuration property found.",
                                this.Text,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);

                            return;
                        }

                        foreach (DataRow r in dt.Rows)
                        {
                            switch (r["name"].ToString())
                            {
                                case "TesterID":
                                    this.TargetObject.TesterID = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "TesterType":
                                    this.TargetObject.TesterType = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "IPAddress":
                                    this.TargetObject.IPAddress = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "HandlerAddress":
                                    this.TargetObject.HandlerAddress = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "HandlerSN":
                                    this.TargetObject.HandlerSN = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "HandlerType":
                                    this.TargetObject.HandlerType = (r["value"] ?? String.Empty).ToString();
                                    break;

                                case "TesterHeaderMaxSitesNum":
                                    if (Int32.TryParse((r["value"] ?? String.Empty).ToString(), out int maxSiteNum))
                                    {
                                        this.TargetObject.MaxSitesNum = maxSiteNum;
                                    }
                                    break;

                                case "PCDShareFolderPath":
                                    this.TargetObject.PCDRemoteSharePath = (r["value"] ?? String.Empty).ToString();
                                    break;
                            }
                        }

                        objectPropertyGrid.SelectedObject = this.TargetObject;
                    }
                }
            }
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            this.TargetObject.Reset();
            objectPropertyGrid.SelectedObject = this.TargetObject;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.TargetObject = objectPropertyGrid.SelectedObject as DebugEnvVars;

            if (this.TargetObject != null)
            {
                try
                {
                    string debugEnvVarsXmlFileName = Path.Combine(Application.StartupPath, "DebugEnvVars.xml");
                    DebugEnvVars.SaveToXml(this.TargetObject, debugEnvVarsXmlFileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Error saving DebugEnvVars.xml\n{ex.Message}",
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    throw;
                }
            }

            Close();
        }
    }

    #region DebugEnvVars

    [Serializable]
    public class DebugEnvVars
    {
        public DebugEnvVars()
        {
            Reset();
        }

        #region ATF.Clotho

        //[Category("ATF.Clotho")]
        //public string CurUserName { get; set; }

        #endregion ATF.Clotho

        #region ATF.Config

        [Category("ATF.Config")]
        public string TesterID { get; set; }

        [Category("ATF.Config")]
        public string TesterType { get; set; }

        [Category("ATF.Config")]
        public string IPAddress { get; set; }

        [Category("ATF.Config")]
        public string HandlerAddress { get; set; }

        [Category("ATF.Config")]
        public string HandlerSN { get; set; }

        [Category("ATF.Config")]
        public string HandlerType { get; set; }

        [Category("ATF.Config")]
        public int MaxSitesNum { get; set; }

        [Category("ATF.Config")]
        public string PCDRemoteSharePath { get; set; }

        #endregion ATF.Config

        #region Operator Input

        [Category("Operator Input")]
        public string UserName { get; set; }

        [Category("Operator Input")]
        public string LotID { get; set; }

        [Category("Operator Input")]
        public string SubLotID { get; set; }

        [Category("Operator Input")]
        public string OpID { get; set; }

        [Category("Operator Input")]
        public string PCBID { get; set; }

        [Category("Operator Input")]
        public string ContractorID { get; set; }

        [Category("Operator Input")]
        public string AssemblyID { get; set; }

        [Category("Operator Input")]
        public string LoadBoardID { get; set; }

        #endregion Operator Input

        #region Runtime

        [Category("Runtime")]
        [Browsable(false)]
        [XmlIgnore]
        public string CurPackageTag { get; set; }

        #endregion Runtime

        public void CopyTo(DebugEnvVars obj)
        {
            this.TesterID = obj.TesterID;
            this.TesterType = obj.TesterType;
            this.IPAddress = obj.IPAddress;
            this.HandlerAddress = obj.HandlerAddress;
            this.HandlerSN = obj.HandlerSN;
            this.HandlerType = obj.HandlerType;
            this.MaxSitesNum = 1;
            this.PCDRemoteSharePath = obj.PCDRemoteSharePath;

            this.UserName = obj.UserName;
            this.LotID = obj.LotID;
            this.SubLotID = obj.SubLotID;
            this.OpID = obj.OpID;
            this.PCBID = obj.PCBID;
            this.ContractorID = obj.ContractorID;
            this.AssemblyID = obj.AssemblyID;
            this.LoadBoardID = obj.LoadBoardID;
            this.CurPackageTag = obj.CurPackageTag;
        }

        public void Reset()
        {
            this.TesterID = String.Empty;
            this.TesterType = String.Empty;
            this.IPAddress = String.Empty;
            this.HandlerAddress = String.Empty;
            this.HandlerSN = String.Empty;
            this.HandlerType = String.Empty;
            this.MaxSitesNum = 1;
            this.PCDRemoteSharePath = String.Empty;

            this.LotID = String.Empty;
            this.SubLotID = String.Empty;
            this.OpID = String.Empty;
            this.PCBID = String.Empty;
            this.ContractorID = String.Empty;
            this.AssemblyID = String.Empty;
            this.LoadBoardID = String.Empty;
            this.CurPackageTag = String.Empty;
        }

        public static DebugEnvVars LoadFromXml(string outputFileName)
        {
            using (Stream o = File.OpenRead(outputFileName))
            {
                var xmlSerializer = new XmlSerializer(typeof(DebugEnvVars));
                DebugEnvVars obj = xmlSerializer.Deserialize(o) as DebugEnvVars;
                return obj;
            }
        }

        public static void SaveToXml(DebugEnvVars obj, string outputFileName)
        {
            using (Stream outputStream = File.Create(outputFileName))
            {
                var xmlSerializer = new XmlSerializer(typeof(DebugEnvVars));
                xmlSerializer.Serialize(outputStream, obj);
            }
        }
    }

    #endregion DebugEnvVars
}