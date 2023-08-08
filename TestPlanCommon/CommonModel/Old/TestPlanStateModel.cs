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
using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using MPAD_TestTimer;
using ClothoSharedItems;

namespace TestPlanCommon
{
    public class TestPlanStateModel
    {
        public bool Spara_Site { get; set; }
        public bool Pa_Site { get; set; }
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
            Spara_Site = true;     // scope: Init+Test
            Pa_Site = false;       // scope: Init+Test
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
                                    Spara_Site = true;
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
                            }

                            if (xn.Attributes["name"].Value.ToString().Trim() == "TesterID")
                            {
                                string[] ID = xn.Attributes["value"].Value.ToString().Trim().ToUpper().Split('-');

                                //TODO CCT Commented out for Joker.
                                //TCF_Setting["TesterID"] = ID[ID.Length - 1];


                                string tester_id = xn.Attributes["value"].Value.ToString().Trim().ToUpper();
                                if (tester_id.EndsWith("-01") == true)
                                {
                                    fbar_tester_id = 1;
                                }
                                else if (tester_id.EndsWith("-02") == true)
                                {
                                    fbar_tester_id = 2;
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
            string result = "";
            if (File.Exists(configXmlPath))
            {
                XmlDocument clothoConfig = new XmlDocument();
                clothoConfig.Load(configXmlPath);

                XmlNodeList clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/ToolSection/ConfigItem");

                foreach (XmlNode xn in clothoConfigNodes)
                {
                    if (xn.Attributes["name"].Value.ToString().Trim() == "TesterID")
                    {
                        string[] ID = xn.Attributes["value"].Value.ToString().Trim().ToUpper().Split('-');

                        result = ID[ID.Length - 1];

                        break;
                    }
                }
            }

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
                    break;
                case "FBAR":
                    Pa_Site = false;
                    Spara_Site = true;
                    break;
                case "BOTH":
                    Pa_Site = true;
                    Spara_Site = true;
                    break;
            }
        }
        #endregion

        public Dictionary<string, string> GetAtfConfig(string configXmlPath)
        {
            Dictionary<string, string> configList = new Dictionary<string, string>();

            if (!File.Exists(configXmlPath)) return configList;

            XmlDocument clothoConfig = new XmlDocument();
            clothoConfig.Load(configXmlPath);

            XmlNodeList clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/ToolSection/ConfigItem");

            if (clothoConfigNodes == null) return configList;

            foreach (XmlNode xn in clothoConfigNodes)
            {
                XmlAttribute nameValue = xn.Attributes["value"];
                string v1 = String.Empty;
                if (nameValue != null)
                {
                    v1 = nameValue.Value;
                }
                configList.Add(xn.Attributes["name"].Value, v1);
            }

            clothoConfigNodes = clothoConfig.SelectNodes("/ATFConfiguration/SystemSection/ConfigItem");

            if (clothoConfigNodes == null) return configList;

            foreach (XmlNode xn in clothoConfigNodes)
            {
                XmlAttribute nameValue = xn.Attributes["value"];
                string v1 = String.Empty;
                if (nameValue != null)
                {
                    v1 = nameValue.Value;
                }
                configList.Add(xn.Attributes["name"].Value, v1);
            }

            return configList;
        }
    }

    public abstract class TestPlanBase : IATFTest
    {
        //protected ClothoConfigurationDataObject m_doClotho1;
        protected ClothoDataObject m_doClotho1 { get; private set; }
        protected TestPlanStateModel m_modelTpState;

        #region TestPlan Properties
        #endregion TestPlan Properties

        private Thread myThread;
        private bool TCPSERVER = false;

        public virtual string DoATFInit(string args)
        {
            LoggingManager.Instance.SetService(ATFLogControl.Instance);
            //m_doClotho1 = new ClothoConfigurationDataObject();
            m_doClotho1 = ClothoDataObject.Instance = ClothoDataObject.Instance ?? new ClothoDataObject();
            m_doClotho1.Initialize();

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
                myThread = new Thread(() =>
                {
                    Application.Run(ClothoDataObject.Instance.SeoulHelper);
                });
                myThread.IsBackground = true;
                myThread.SetApartmentState(ApartmentState.STA);
                myThread.Start();

                if (TCPSERVER)
                {
                    WebSocket customSocket = new WebSocket();
                    Thread t = new Thread(customSocket.InitSocket);
                    t.IsBackground = true;
                    t.Start();
                }
            }

            return String.Empty;
        }

        public virtual string DoATFUnInit(string args)
        {
            StopWatchManager.Instance.Start();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Enter DoATFUnInit: {0}\n", args);

            #region Custom UnInit Coding Section

            //////////////////////////////////////////////////////////////////////////////////
            // ----------- ONLY provide your Custom CloseLot Coding here --------------- //

            if (myThread != null)
            {
                myThread.Interrupt();
                myThread.Abort();
                myThread = null;
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


            // ----------- END of Custom CloseLot Coding --------------- //
            //////////////////////////////////////////////////////////////////////////////////

            #endregion Custom UnInit Coding Section

            StopWatchManager.Instance.Stop();
            ClothoDataObject.Instance = null;
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
    }

    public class TesterManager
    {
        public ITesterSite CurrentTester;

        public TesterManager(ITesterSite testerLocation)
        {
            CurrentTester = testerLocation;
        }
    }

    public interface ITesterSite
    {
        string GetVisaAlias(string visaAlias, byte site);
        string GetHandlerName();

        //EqSwitchMatrix.Rev GetSwitchMatrixRevision();
        List<KeyValuePair<string, string>> GetSmuSetting();

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
