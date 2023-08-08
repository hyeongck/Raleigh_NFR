using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using MPAD_TestTimer;
using ClothoSharedItems;
using TestPlanCommon.NFModel;
using System.Security.Permissions;

namespace TestPlanCommon.CommonModel
{
    public class TestPlanStateModel
    {
        public bool Spara_Site { get; set; }
        public bool Pa_Site { get; set; } // Same as NF_Site
        public bool DcOtp_Site { get; set; } // 20201119 DcOTPtest To borrow Mipi & Otp function from RF1,
        /// <summary>
        /// False if any error is encountered in ATFInit().
        /// </summary>
        public bool programLoadSuccess { get; set; }
        public bool programUnloaded { get; set; }
        public bool litedrivermode { get; set; }

        public int fbar_tester_id = 0;

        private TesterManager m_modelTester;

        public ValidationDataObject ValidationDataObject { get; set; }

        private string currentTestResultFileName;

        public string CurrentTestResultFileName
        {
            get { return currentTestResultFileName.Replace(".CSV", ""); }
            set { currentTestResultFileName = value; }
        }

        public ITesterSite TesterSite
        {
            get { return m_modelTester.CurrentTester; }
        }

        public TestPlanStateModel()
        {
            Spara_Site = false;    // scope: Init+Test
            Pa_Site = true;       // scope: Init+Test
            programLoadSuccess = true;
            ValidationDataObject = new ValidationDataObject();
        }

        /// <summary>
        /// handle tester variation.
        /// </summary>
        /// <param name="currentTester"></param>
        public void SetTesterSite(ITesterSite currentTester)
        {
            m_modelTester = new TesterManager(currentTester);
        }

        public void SetUnloaded()
        {
            programUnloaded = true;
        }

        public void SetCurrentTestResult(string fileName)
        {
            currentTestResultFileName = fileName;
        }

        public void SetLoadFail()
        {
            programLoadSuccess = false;
        }

        public void SetLoadFail(bool isPass)
        {
            programLoadSuccess = programLoadSuccess && isPass;
        }

        public void SetLoadFail(ValidationDataObject vdo)
        {
            programLoadSuccess = programLoadSuccess && vdo.IsValidated;
            PromptManager.Instance.ShowError(vdo);
        }

        /// <summary>
        /// Check ValidationDataObject.IsValidated after call this.
        /// </summary>
        /// <param name="errorMessage"></param>
        public void SetLoadFail(string errorMessage)
        {
            ValidationDataObject.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Return true if tester is not defined.
        /// </summary>
        public bool CheckTesterType(string configXmlPath)
        {
            #region Check Test Site
            string tester_type = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_TYPE, "").Trim().ToUpper();

            switch (tester_type)
            {
                case "":
                    if (File.Exists(configXmlPath))
                    {
                        XmlDocument clothoConfig = new XmlDocument();
                        clothoConfig.Load(configXmlPath);

                        XmlNodeList clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/ToolSection/ConfigItem");

                        foreach (XmlNode xn in clothoConfigNodes)
                        {
                            if (xn.Attributes["name"].Value.ToString().Trim() == "TesterType")
                            {
                                if (xn.Attributes["value"].Value.ToString().Trim().ToUpper() == "BE-PXI-NI")
                                {
                                    Pa_Site = true;
                                    Spara_Site = false;
                                }
                                else if (xn.Attributes["value"].Value.ToString().Trim().ToUpper() == "BE-PXI-KEYSIGHT")
                                {
                                    Pa_Site = false;
                                    Spara_Site = true;
                                }
                                else
                                {
                                    Pa_Site = false;
                                    Spara_Site = false;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        Pa_Site = false;
                        Spara_Site = false;
                    }

                    break;
                case "BE-PXI-NI":
                    Pa_Site = true;
                    Spara_Site = false;
                    break;
                case "BE-PXI-KEYSIGHT":
                    Pa_Site = false;
                    Spara_Site = true;
                    break;
                default:
                    Pa_Site = false;
                    Spara_Site = false;
                    break;
            }

            bool isCondition1 = Pa_Site == false && Spara_Site == false;
            return isCondition1;

            #endregion
        }

        /// <summary>
        /// Return true if tester is not defined.
        /// </summary>
        public bool CheckTesterType2(string configXmlPath)
        {
            #region Check Test Site
            string tester_type = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_TYPE, "").Trim().ToUpper();

            string zrootpath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "");


            switch (tester_type)
            {
                case "":
                    if (File.Exists(configXmlPath))
                    {
                        switch (ClothoDataObject.Instance.ATFConfiguration.ToolSection.GetValue("TesterType")?.Trim()?.ToUpper())
                        {
                            case "BE-PXI-NI":
                                Pa_Site = true;
                                Spara_Site = true;
                                break;

                            case "BE-PXI-KEYSIGHT":
                                Pa_Site = false;
                                Spara_Site = true;
                                break;

                            default:
                                Pa_Site = false;
                                Spara_Site = false;
                                break;
                        }

                        string tester_id = ClothoDataObject.Instance.ATFConfiguration.ToolSection.GetValue("TesterID")?.Trim()?.ToUpper();
                        if (tester_id.EndsWith("-01") == true)
                        {
                            fbar_tester_id = 1;
                        }
                        else if (tester_id.EndsWith("-02") == true)
                        {
                            fbar_tester_id = 2;
                        }
                    }
                    else
                    {
                        Pa_Site = false;
                        Spara_Site = false;
                    }

                    break;

                case "BE-PXI-NI":
                    Pa_Site = true;
                    Spara_Site = true;
                    break;
                case "BE-PXI-KEYSIGHT":
                    Pa_Site = false;
                    Spara_Site = true;
                    break;
                default:
                    Pa_Site = false;
                    Spara_Site = false;
                    break;
            }

            bool isCondition1 = Pa_Site == false && Spara_Site == false;
            return isCondition1;

            #endregion
        }

        public string GetTesterId(string configXmlPath)
        {
            string[] ID = ClothoDataObject.Instance.ATFConfiguration.ToolSection.GetValue("TesterID")?.ToUpper()?.Split('-');
            string result = ID[ID.Length - 1];

            return result;
        }

        #region PA TP calls.

        public void SetTesterType(string testerType)
        {
            switch (testerType)
            {
                case "PA":
                    Pa_Site = true;
                    Spara_Site = false;
                    ClothoDataObject.Instance.TesterType = eTesterType.RF1;
                    break;

                case "FBAR":
                    Pa_Site = false;
                    Spara_Site = true;
                    ClothoDataObject.Instance.TesterType = eTesterType.RF2;
                    break;

                case "BOTH":
                    Pa_Site = true;
                    Spara_Site = true;
                    ClothoDataObject.Instance.TesterType = eTesterType.RF1 | eTesterType.RF2;
                    break;
            }
        }
        #endregion

        public Dictionary<string, string> GetAtfConfig(string configXmlPath)
        {
            Dictionary<string, string> configList = new Dictionary<string, string>();
            configList = configList.MergeLeft(ClothoDataObject.Instance.ATFConfiguration.ToolSection.ToDictionaryEx(x => x.name, y => y.value));
            configList = configList.MergeLeft(ClothoDataObject.Instance.ATFConfiguration.SystemSection.ToDictionaryEx(x => x.name, y => y.value));

            //if (!File.Exists(configXmlPath)) return configList;

            //XmlDocument clothoConfig = new XmlDocument();
            //clothoConfig.Load(configXmlPath);

            //XmlNodeList clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/ToolSection/ConfigItem");

            //if (clothoConfigNodes == null) return configList;

            //foreach (XmlNode xn in clothoConfigNodes)
            //{
            //    XmlAttribute nameValue = xn.Attributes["value"];
            //    string v1 = String.Empty;
            //    if (nameValue != null)
            //    {
            //        v1 = nameValue.Value;
            //    }
            //    configList.Add(xn.Attributes["name"].Value, v1);
            //}

            //clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/SystemSection/ConfigItem");

            //if (clothoConfigNodes == null) return configList;

            //foreach (XmlNode xn in clothoConfigNodes)
            //{
            //    XmlAttribute nameValue = xn.Attributes["value"];
            //    string v1 = String.Empty;
            //    if (nameValue != null)
            //    {
            //        v1 = nameValue.Value;
            //    }
            //    configList.Add(xn.Attributes["name"].Value, v1);
            //}

            return configList;
        }
    }

    public abstract class TestPlanBase : IATFTest
    {
        //protected ClothoConfigurationDataObject m_doClotho1;
        protected ClothoDataObject m_doClotho { get; private set; }
        protected TestPlanStateModel m_modelTpState;

        #region TestPlan Properties
        public NFProductionTestPlan m_NFTpProd;
        //public NFTestConditionReader m_NFtcfReader; // Add later by ben 22.07.26
        //public MultiSiteTestRunner m_modelTestRunner; // Add later by ben 22.07.26

        public int Sublot_StopOnContinueFail_1A_Count;
        public int Sublot_StopOnContinueFail_2A_Count;
        public int PassCount = 0;
        public int FailCount = 0;

        #endregion TestPlan Properties

        private Thread myThread;
#pragma warning disable CS0246 // The type or namespace name 'TCPHandlerProtocol' could not be found (are you missing a using directive or an assembly reference?)
        public TCPHandlerProtocol.HontechHandler handler;
#pragma warning restore CS0246 // The type or namespace name 'TCPHandlerProtocol' could not be found (are you missing a using directive or an assembly reference?)
        public string HandlerAddress = "2";
        public string Handler_Info = "FALSE";
        public string strHandlerType = "HandlerSim";
        public bool Flag_HandlerInfor = false;
        public bool Flag_Init_TTD = false;
        private bool CheckHandlerInformation = false;

        public string[] pjtTagNum = new string[] { };
        public bool SkipProdGUIFlag = false;

        public List<string> AIDPR { get; set; }

        private bool TCPSERVER = false;

        public TestPlanBase()
        {
            m_NFTpProd = new NFProductionTestPlan();
            //m_NFtcfReader = new NFTestConditionReader();
            //m_modelTestRunner = new MultiSiteTestRunner();
        }

        private void Login()
        {
            Application.Run(ClothoDataObject.Instance.SeoulHelper);
        }

        public virtual string DoATFInit(string args)
        {
            LoggingManager.Instance.SetService(ATFLogControl.Instance);

            m_doClotho = ClothoDataObject.Instance = ClothoDataObject.Instance ?? new ClothoDataObject();
            m_doClotho.Initialize();

            m_modelTpState = new TestPlanStateModel();

            if (!string.IsNullOrWhiteSpace(args))
            {
                Regex regexKeyValue = new Regex(@"(?<key>\w+)=(?<val>\w+)");
                var _args = args.ToUpper().Split(',');
                foreach (var _arg in _args)
                {
                    var rMatch = regexKeyValue.Match(_arg);
                    if (rMatch.Success)
                    {
                        if (ClothoDataObject.Instance.UserTestConfigs.ContainsKey(rMatch.Groups["key"].Value)) continue;
                        ClothoDataObject.Instance.UserTestConfigs.Add(rMatch.Groups["key"].Value, rMatch.Groups["val"].Value);
                    }
                }
            }

            foreach (var sItem in Enum.GetNames(typeof(RunOption)))
            {
                if (ClothoDataObject.Instance.Get_UserSetting_Condition(sItem.ToUpper(), "FALSE").CIvEquals("TRUE"))
                {
                    if (Enum.TryParse(sItem, out RunOption eItem))
                    {
                        if ((RunOption.SkipSelfCalibration | RunOption.SkipOTPBurn).HasFlag(eItem))
                        {
                            if (!ClothoDataObject.Instance.ContractManufacturer.CIvStartsWithAnyOf("Inari", "ASEK"))
                                ClothoDataObject.Instance.RunOptions |= eItem;
                        }
                        else
                        {
                            ClothoDataObject.Instance.RunOptions |= eItem;
                        }
                    }
                }
            }

            if (ClothoDataObject.Instance.SeoulHelper != null)
            {
                myThread = new Thread(Login);
                myThread.SetApartmentState(ApartmentState.MTA);
                myThread.IsBackground = true;
                myThread.Start();

                if (!ClothoDataObject.Instance.RunOptions.HasFlag(RunOption.SIMULATE))
                {
                    var SimulateMode = Helper.AutoClosingMessageBox.Show("Do you want 'Simulate MODE' ON?", "Seoul Only", 5000, MessageBoxButtons.YesNo, DialogResult.No, MessageBoxIcon.Question);
                    if (SimulateMode == DialogResult.Yes)
                    {
                        ClothoDataObject.Instance.RunOptions |= RunOption.SIMULATE;
                        ClothoDataObject.Instance.SeoulHelper.InvokeTitle(true);
                    }
                    else
                        ClothoDataObject.Instance.SeoulHelper.InvokeTitle(false);
                }
            }

            string tempLotID = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
            SkipProdGUIFlag =
                (ClothoDataObject.Instance.USERTYPE.HasFlag(eUSERTYPE.PPUSER) && tempLotID.ToUpper() == "SUBCAL") ||
                (ClothoDataObject.Instance.USERTYPE.HasFlag(eUSERTYPE.PPUSER) && tempLotID.ToUpper() == "GUCAL") ||
                (ClothoDataObject.Instance.USERTYPE.HasFlag(eUSERTYPE.SUSER) && tempLotID.ToUpper() == "GUCAL") ||
                (ClothoDataObject.Instance.USERTYPE.HasFlag(eUSERTYPE.SUSER) && tempLotID.ToUpper() == "SUBCAL");

            return String.Empty;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        public void KillThread()
        {
            myThread.Interrupt();
            myThread.Abort();
            myThread = null;
        }

        public virtual string DoATFUnInit(string args)
        {
            StopWatchManager.Instance.Start();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Enter DoATFUnInit: {0}\n", args);

            #region Custom UnInit Coding Section

            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom CloseLot Coding here --------------- //
            try
            {
                if (myThread != null)
                {
                    KillThread();
                }

                var processes = from p in System.Diagnostics.Process.GetProcessesByName("EXCEL") select p;
                foreach (var process in processes)
                {
                    if (process.MainWindowTitle == "")
                    {
                        try { process.Kill(); }
                        catch (System.ComponentModel.Win32Exception) { }
                    }
                }

                m_modelTpState.SetUnloaded();

                #region From ModularMain - RF1 & RF2
                //for (byte site = 0; site < Eq.NumSites; site++)
                //{
                //    Eq.Site[site].HSDIO?.Close();
                //    Eq.Site[site].RF?.close();
                //    if (Eq.Site[site].DC != null)
                //    {
                //        Parallel.ForEach(Eq.Site[site].DC, dc => { dc.Value?.Dispose(); });
                //    }
                //    ResultBuilder.Clear(site);
                //    DPAT.Clear(site);
                //}
                #endregion From ModularMain - RF1 & RF2

                // ----------- END of Custom CloseLot Coding --------------- //
                //////////////////////////////////////////////////////////////////////////////////

                #endregion Custom UnInit Coding Section

                ClothoDataObject.Instance = null;
            }
            catch (System.Threading.ThreadAbortException) { }
            finally { }

            StopWatchManager.Instance.Stop();
            return sb.ToString();
        }

        public virtual string DoATFLot(string args)
        {
            StopWatchManager.Instance.Start();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Enter DoATFLot: {0}\n", args);

            #region Custom CloseLot Coding Section

            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom CloseLot Coding here --------------- //

            

            // ----------- END of Custom CloseLot Coding --------------- //
            //////////////////////////////////////////////////////////////////////////////////

            #endregion Custom CloseLot Coding Section

            StopWatchManager.Instance.Stop();
            return sb.ToString();
        }

        public abstract ATFReturnResult DoATFTest(string args);

        //protected LoggingManager Log
        //{
        //    get { return LoggingManager.Instance; }
        //}

        //protected PromptManager MessageBox
        //{
        //    get { return PromptManager.Instance; }
        //}

        public void LogToLogServiceAndFile(LogLevel logLev, string str)
        {
            switch (logLev)
            {
                case LogLevel.Info:
                    LoggingManager.Instance.LogInfo(str);
                    break;

                case LogLevel.HighLight:
                    LoggingManager.Instance.LogHighlight(str);
                    break;

                case LogLevel.Warn:
                    LoggingManager.Instance.LogWarningTestPlan(str);
                    break;

                case LogLevel.Error:
                case LogLevel.Fatal:

                    LoggingManager.Instance.LogError(str);
                    break;
            }
            Console.WriteLine(str);
        }

        public ValidationDataObject Validate(Exception ex)
        {
            ValidationDataObject vdo = new ValidationDataObject();
            if (ex != null)
            {
                PromptManager.Instance.ShowError(ex);
                m_modelTpState.SetLoadFail();
                vdo.ErrorMessage = TestPlanRunConstants.RunFailureFlag + ex.Message;
                return vdo;
            }

            if (!m_modelTpState.programLoadSuccess)
            {
                vdo.ErrorMessage = TestPlanRunConstants.RunFailureFlag;
            }
            return vdo;
        }

        public string HandlerSN = "";
    }

    public class WebSocket : IDisposable
    {
        private TcpListener server = null;
        private TcpClient clientSocket = null;
        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        private static int counter = 0;
        private string date;

        public void h_client_OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsKey(clientSocket))
                clientList.Remove(clientSocket);
        }

        public void InitSocket()
        {
            server = new TcpListener(IPAddress.Any, 8989);
            clientSocket = default(TcpClient);
            server.Start();

            while (true)
            {
                try
                {
                    counter++;
                    clientSocket = server.AcceptTcpClient();

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string user_name = Encoding.Unicode.GetString(buffer, 0, bytes);
                    user_name = user_name.Substring(0, user_name.IndexOf("$"));

                    clientList.Add(clientSocket, user_name);
                    SendMessageAll(user_name + " join the room.", "", false);

                    handleClient h_client = new handleClient();
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }
                catch (SocketException) { break; }
                catch (Exception) { break; }
            }

            clientSocket.Close();
            server.Stop();
        }

        public void OnReceived(string message, string user_name)
        {
            if (message.Equals("Status"))
            {
                DisplayText(message);
            }
            else if (message.Equals("Reset"))
            {
                //m_prodTp.ShowInputGui(pjtTagNum, SkipProdGUIFlag);
            }
            else if (message.Equals("leaveChat"))
            {
                string displayMessage = "leave user : " + user_name;
                SendMessageAll("leaveChat", user_name, true);
            }
            else
            {
                string displayMessage = "From client : " + user_name + " : " + message;
                SendMessageAll(message, user_name, true);
            }
        }

        public void SendMessageAll(string message, string user_name, bool flag)
        {
            foreach (var pair in clientList)
            {
                date = DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss");

                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag)
                {
                    if (message.Equals("leaveChat"))
                        buffer = Encoding.Unicode.GetBytes(user_name + " leave the room.");
                    else
                        buffer = Encoding.Unicode.GetBytes("[ " + date + " ] " + user_name + " : " + message);
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message);
                }

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }

        public void DisplayText(string text)
        {
            Helper.AutoClosingMessageBox.Show(text, "Got message", 3000);
        }

        public void Dispose()
        {
            clientSocket?.Close();
            server?.Stop();

            server = null;
            clientSocket = null;
        }

        internal class handleClient
        {
            private TcpClient clientSocket = null;
            public Dictionary<TcpClient, string> clientList = null;

            public void startClient(TcpClient clientSocket, Dictionary<TcpClient, string> clientList)
            {
                this.clientSocket = clientSocket;
                this.clientList = clientList;

                Thread t_hanlder = new Thread(doChat);
                t_hanlder.IsBackground = true;
                t_hanlder.Start();
            }

            public delegate void MessageDisplayHandler(string message, string user_name);

            public event MessageDisplayHandler OnReceived;

            public delegate void DisconnectedHandler(TcpClient clientSocket);

            public event DisconnectedHandler OnDisconnected;

            private void doChat()
            {
                NetworkStream stream = null;
                try
                {
                    byte[] buffer = new byte[1024];
                    string msg = string.Empty;
                    int bytes = 0;
                    int MessageCount = 0;

                    while (true)
                    {
                        MessageCount++;
                        stream = clientSocket.GetStream();
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        msg = Encoding.Unicode.GetString(buffer, 0, bytes);
                        msg = msg.Substring(0, msg.IndexOf("$"));

                        if (OnReceived != null)
                            OnReceived(msg, clientList[clientSocket].ToString());
                    }
                }
                catch (SocketException se)
                {
                    Trace.WriteLine(string.Format("doChat - SocketException : {0}", se.Message));

                    if (clientSocket != null)
                    {
                        if (OnDisconnected != null)
                            OnDisconnected(clientSocket);

                        clientSocket.Close();
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                    if (clientSocket != null)
                    {
                        if (OnDisconnected != null)
                            OnDisconnected(clientSocket);

                        clientSocket.Close();
                        stream.Close();
                    }
                }
            }
        }
    }

}
