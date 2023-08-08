using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.Linq;
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;

namespace TCPHandlerProtocol
{
    public class ExistechHandler
    {
        private TcpClient tcpclnt = new TcpClient();
        private bool _connected = false;
        protected int _test_site = 1;
        protected string server_ip_address = "192.168.0.101";
        byte[] responseBytes = new byte[1000];
        bool responseEnd = false;

        public enum Commands
        {
            StartOfLot,
            StartOfLotQuery,
            StartOfLotSplitTestQuery,
            EndOfLot,
            EndOfLotQuery,
            LastTestedInTheLotQuery,
            DutIDQuery,
            DutIDSet,
            ContactForceQuery,
            MoveForward,
            MoveReverse,
            StartOfTestQuery,
            TestInProgress,
            EndOfTest,
            UtilizationPerHourQuery,
            PickUpHeadTestYieldQuery,
            PickUpHeadTestYieldResetQuery,
            HandlerIDQuery,
            HandlerStatusQuery,
            ChangeProfile,
            GetProfile,
            ResetHandler,
            AutomaticMode,
            PurgingMode,
            StartTrayMapSiteA1,
            StartTrayMapSiteA2,
            StartTrayMapSiteB1,
            StartTrayMapSiteB2,
            BarcodeIDQuery
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public int Site
        {
            set
            {
                switch (value)
                {
                    case 1:
                    default:
                        _test_site = 1;
                        server_ip_address = "192.168.0.101";
                        break;

                    case 2:
                        _test_site = 2;
                        server_ip_address = "192.168.0.102";
                        break;

                    case 3:
                        _test_site = 3;
                        server_ip_address = "192.168.0.103";
                        break;

                    case 4:
                        _test_site = 4;
                        server_ip_address = "192.168.0.104";
                        break;
                }
            }
            get
            {
                return _test_site;
            }
        }

        public ExistechHandler()
        {
            _test_site = 1;
            server_ip_address = "127.0.0.1";
        }

        public ExistechHandler(int site)
        {
            switch (site)
            {
                case 0:
                    _test_site = 1;
                    server_ip_address = "127.0.0.1";
                    break;

                case 1:
                default:
                    _test_site = 1;
                    server_ip_address = "192.168.0.101";
                    break;

                case 2:
                    _test_site = 2;
                    server_ip_address = "192.168.0.102";
                    break;

                case 3:
                    _test_site = 3;
                    server_ip_address = "192.168.0.103";
                    break;

                case 4:
                    _test_site = 4;
                    server_ip_address = "192.168.0.104";
                    break;
            }
        }

        ~ExistechHandler()
        {
            Disconnect();
        }

        public void StartOfLot()
        {
            SendCommand(Commands.StartOfLot);
        }

        public HandlerInfo StartOfLotQuery()
        {
            int temp = 0;
            HandlerInfo handlerinfo = new HandlerInfo();
            string result = SendCommand(Commands.StartOfLotQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "HandlerInfo")
                    {
                        if (int.TryParse(xmlhandlerinfo.Element("PlungerSettlingTime").Value, out temp))
                        {
                            handlerinfo.PlungerSettlingTime = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("StartOfTestDelay").Value, out temp))
                        {
                            handlerinfo.StartOfTestDelay = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("TurretSpeed").Value, out temp))
                        {
                            handlerinfo.TurretSpeed = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("NoOfTestSite").Value, out temp))
                        {
                            handlerinfo.NoOfTestSite = temp;
                        }
                        handlerinfo.PlungerType = xmlhandlerinfo.Element("PlungerType").Value;
                        handlerinfo.SoftwareVersion = xmlhandlerinfo.Element("SoftwareVersion").Value;
                        handlerinfo.SerialNo = xmlhandlerinfo.Element("SerialNo").Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return handlerinfo;
        }

        public HandlerInfo StartOfLotSplitTestQuery()
        {
            int temp = 0;
            HandlerInfo handlerinfo = new HandlerInfo();
            string result = SendCommand(Commands.StartOfLotSplitTestQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "HandlerInfo")
                    {
                        if (int.TryParse(xmlhandlerinfo.Element("PlungerSettlingTime").Value, out temp))
                        {
                            handlerinfo.PlungerSettlingTime = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("StartOfTestDelay").Value, out temp))
                        {
                            handlerinfo.StartOfTestDelay = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("TurretSpeed").Value, out temp))
                        {
                            handlerinfo.TurretSpeed = temp;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("NoOfTestSite").Value, out temp))
                        {
                            handlerinfo.NoOfTestSite = temp;
                        }
                        handlerinfo.PlungerType = xmlhandlerinfo.Element("PlungerType").Value;
                        handlerinfo.SoftwareVersion = xmlhandlerinfo.Element("SoftwareVersion").Value;
                        handlerinfo.SerialNo = xmlhandlerinfo.Element("SerialNo").Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return handlerinfo;
        }

        public void EndOfLot()
        {
            SendCommand(Commands.EndOfLot);
        }

        public List<HandlerLotInfo> EndOfLotQuery()
        {
            int temp_int = 0;
            Int64 temp_int64 = 0;
            Decimal temp_decimal = 0;
            DateTime temp_timestamp = DateTime.MinValue;
            List<HandlerLotInfo> handlerlotinfo = new List<HandlerLotInfo>();
            string result = SendCommand(Commands.EndOfLotQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "HandlerLotInfo")
                    {
                        IEnumerable<XElement> duts = xmlhandlerinfo.Elements("StartOfTest");

                        foreach (XElement dut in duts)
                        {
                            HandlerLotInfo hli = new HandlerLotInfo();

                            if (DateTime.TryParse(dut.Element("Timestamp").Value, out temp_timestamp))
                            {
                                hli.Timestamp = temp_timestamp;
                            }
                            if (Decimal.TryParse(dut.Element("PlungerForce").Value, out temp_decimal))
                            {
                                hli.PlungerForce = temp_decimal;
                            }
                            if (int.TryParse(dut.Element("PlungerHeadNo").Value, out temp_int))
                            {
                                hli.PlungerHeadNo = temp_int;
                            }
                            if (Decimal.TryParse(dut.Element("WorkingStroke").Value, out temp_decimal))
                            {
                                hli.WorkingStroke = temp_decimal;
                            }
                            if (int.TryParse(dut.Element("PickUpHeadNo").Value, out temp_int))
                            {
                                hli.PickUpHeadNo = temp_int;
                            }
                            if (Int64.TryParse(dut.Element("BarcodeID").Value, out temp_int64))
                            {
                                hli.BarcodeID = temp_int64;  
                            }
                            else
                            {
                                hli.BarcodeID = -1;    
                            }
                            if (dut.Element("BarcodeID").Value != null)
                            {
                                hli.strBarcodeID = dut.Element("BarcodeID").Value;      //to support 16x16 2DID >> 24 Decimal character
                            }
                            else
                            {
                                hli.strBarcodeID = "-1";    //to support 16x16 2DID >> 24 Decimal character
                            }

                            handlerlotinfo.Add(hli);
                        }
                    }
                }
            }
            catch { }

            return handlerlotinfo;
        }

        public HandlerLotInfo LastTestedInTheLotQuery()
        {
            Int64 temp_int64 = 0;
            int temp_int = 0;
            Decimal temp_decimal = 0;
            DateTime temp_timestamp = DateTime.MinValue;
            HandlerLotInfo handlerlotinfo = new HandlerLotInfo();
            string result = SendCommand(Commands.LastTestedInTheLotQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "HandlerLotInfo")
                    {
                        XElement dut = xmlhandlerinfo.Elements("StartOfTest").FirstOrDefault();

                        if (DateTime.TryParse(dut.Element("Timestamp").Value, out temp_timestamp))
                        {
                            handlerlotinfo.Timestamp = temp_timestamp;
                        }
                        if (Decimal.TryParse(dut.Element("PlungerForce").Value, out temp_decimal))
                        {
                            handlerlotinfo.PlungerForce = temp_decimal;
                        }
                        if (int.TryParse(dut.Element("PlungerHeadNo").Value, out temp_int))
                        {
                            handlerlotinfo.PlungerHeadNo = temp_int;
                        }
                        if (int.TryParse(dut.Element("WorkingStroke").Value, out temp_int))
                        {
                            handlerlotinfo.WorkingStroke = temp_int;
                        }
                        if (int.TryParse(dut.Element("PickUpHeadNo").Value, out temp_int))
                        {
                            handlerlotinfo.PickUpHeadNo = temp_int;
                        }
                        if (Int64.TryParse(dut.Element("BarcodeID").Value, out temp_int64))
                        {
                            handlerlotinfo.BarcodeID = temp_int64;   
                        }
                        else
                        {
                            handlerlotinfo.BarcodeID = -999;                          
                        }
                        if(dut.Element("BarcodeID").Value != null)
                        {
                            handlerlotinfo.strBarcodeID = dut.Element("BarcodeID").Value;   //to support 16x16 2DID >> 24 Decimal character
                        }
                        else
                        {
                            handlerlotinfo.strBarcodeID = "-999";   //to support 16x16 2DID >> 24 Decimal character
                        }
                    }
                }
            }
            catch { }
            return handlerlotinfo;
        }

        public void MoveForward(int index_number)
        {
            SendCommand(Commands.MoveForward, index_number.ToString());
        }

        public void MoveReverse(int index_number)
        {
            SendCommand(Commands.MoveReverse, index_number.ToString());
        }

        public HandlerSOT StartOfTestQuery()
        {
            int temp_int = 0;
            HandlerSOT handler_sot = new HandlerSOT();
            string result = SendCommand(Commands.StartOfTestQuery);

            try
            {
                if(result.Length > 0)
                {
                    XElement xmlSOT = XElement.Parse(result);

                    if (xmlSOT.Name.LocalName == "SOT")
                    {

                        if (int.TryParse(xmlSOT.Element("Status").Value, out temp_int))
                        {
                            handler_sot.Status = temp_int;
                        }
                        if (int.TryParse(xmlSOT.Element("IndexMoved").Value, out temp_int))
                        {
                            handler_sot.IndexMoved = temp_int;
                        }
                        if (int.TryParse(xmlSOT.Element("Site1").Value, out temp_int))
                        {
                            handler_sot.Site1 = temp_int;
                        }
                        if (int.TryParse(xmlSOT.Element("Site2").Value, out temp_int))
                        {
                            handler_sot.Site2 = temp_int;
                        }
                    }
                }
            }
            catch { }
            return handler_sot;
        }

        public void TestInProgress()
        {
            SendCommand(Commands.TestInProgress);
        }

        public void EndOfTest(int bin_number)
        {
            SendCommand(Commands.EndOfTest, bin_number.ToString());
        }

        public List<HandlerUPH> HandlerUtilizationPerHourQuery()
        {
            int temp_int = 0;
            DateTime temp_datetime = DateTime.MinValue;
            List<HandlerUPH> handleruph = new List<HandlerUPH>();
            string result = SendCommand(Commands.UtilizationPerHourQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandleruph = XElement.Parse(result);

                    if (xmlhandleruph.Name.LocalName == "HandlerUPH")
                    {
                        XElement today = xmlhandleruph.Elements("Date").Where(x => DateTime.Parse(x.Attribute("value").Value + " 00:00:00").Date == DateTime.Today).FirstOrDefault();

                        if (today != null)
                        {
                            IEnumerable<XElement> hours = today.Elements("Time");

                            foreach (XElement hour in hours)
                            {
                                HandlerUPH uph = new HandlerUPH();

                                if (DateTime.TryParse(today.Attribute("value").Value + " " + hour.Attribute("value").Value, out temp_datetime))
                                {
                                    uph.Timestamp = temp_datetime;
                                }
                                if (int.TryParse(hour.Element("RunTime").Value, out temp_int))
                                {
                                    uph.RunTime = temp_int;
                                }
                                if (int.TryParse(hour.Element("ErrorTime").Value, out temp_int))
                                {
                                    uph.ErrorTime = temp_int;
                                }
                                if (int.TryParse(hour.Element("IdlingTime").Value, out temp_int))
                                {
                                    uph.IdlingTime = temp_int;
                                }
                                if (int.TryParse(hour.Element("InitTime").Value, out temp_int))
                                {
                                    uph.InitTime = temp_int;
                                }
                                if(int.TryParse(hour.Element("SetupTime").Value, out temp_int))
                                {
                                    uph.SetupTime = temp_int;
                                }

                                handleruph.Add(uph);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return handleruph;
        }

        public virtual List<PickUpHeadTestYield> PickUpHeadTestYieldQuery(bool reset = false)
        {
            int temp_int = 0;
            double temp_double = 0;
            DateTime temp_datetime = DateTime.MinValue;
            List<PickUpHeadTestYield> handlertestyield = new List<PickUpHeadTestYield>();
            string result = string.Empty;

            if (reset == false)
            {
                result = SendCommand(Commands.PickUpHeadTestYieldQuery);
            }
            else
            {
                result = SendCommand(Commands.PickUpHeadTestYieldResetQuery);
            }

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlertestyield = XElement.Parse(result);

                    if (xmlhandlertestyield.Name.LocalName == "HandlerYield")
                    {
                        IEnumerable<XElement> testsites = xmlhandlertestyield.Elements("TestSite");

                        foreach (XElement testsite in testsites)
                        {
                            IEnumerable<XElement> pickupheads = testsite.Elements("Head");

                            foreach (XElement pickuphead in pickupheads)
                            {
                                PickUpHeadTestYield testyield = new PickUpHeadTestYield();

                                if(int.TryParse(testsite.Attribute("ID").Value, out temp_int))
                                {
                                    testyield.TestSite = temp_int;
                                }
                                if (int.TryParse(pickuphead.Attribute("ID").Value, out temp_int))
                                {
                                    testyield.PickUpHeadNo = temp_int;
                                }
                                if (double.TryParse(pickuphead.Element("Yield").Value, out temp_double))
                                {
                                    testyield.TestYield = temp_double;
                                }
                                if (int.TryParse(pickuphead.Element("Count").Value, out temp_int))
                                {
                                    testyield.TotalCount = temp_int;
                                }
                                handlertestyield.Add(testyield);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return handlertestyield;
        }

        public HandlerID HandlerIDQuery()
        {
            HandlerID handler_id = new HandlerID();
            string result = SendCommand(Commands.HandlerIDQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandler_id = XElement.Parse(result);

                    if (xmlhandler_id.Name.LocalName == "HandlerID")
                    {
                        handler_id.SoftwareVersion = xmlhandler_id.Element("SoftwareVersion").Value;
                        handler_id.SerialNo = xmlhandler_id.Element("SerialNo").Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return handler_id;
        }

        public HandlerStatus HandlerStatusQuery()
        {
            int temp = 0;
            HandlerStatus handler_status = new HandlerStatus();
            string result = SendCommand(Commands.HandlerStatusQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandler_status = XElement.Parse(result);

                    if (xmlhandler_status.Name.LocalName == "HandlerStatus")
                    {
                        if (int.TryParse(xmlhandler_status.Element("TowerLight").Value, out temp) == true)
                        {
                            handler_status.TowerLight = temp;
                        }

                        IEnumerable<XElement> xmlhandler_errors = xmlhandler_status.Elements("ErrorCode");

                        int tmp_error_code = 0;
                        handler_status.ErrorCode.Clear();

                        foreach (XElement error in xmlhandler_errors)
                        {
                            if (int.TryParse(error.Value, out tmp_error_code))
                            {
                                handler_status.ErrorCode.Add(tmp_error_code);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return handler_status;
        }

        public void ChangeProfile(string filename)
        {
            SendCommand(Commands.ChangeProfile, filename);
        }

        public string GetProfile()
        {
            string profile_name = string.Empty;
            HandlerStatus handler_status = new HandlerStatus();
            string result = SendCommand(Commands.GetProfile);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandler_profile = XElement.Parse(result);

                    if (xmlhandler_profile.Name.LocalName == "HandlerProfile")
                    {
                        profile_name = xmlhandler_profile.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return profile_name;
        }

        public void ResetHandler()
        {
            SendCommand(Commands.ResetHandler);
        }

        public void AutomaticMode(bool enabled)
        {
            SendCommand(Commands.AutomaticMode, enabled ? "1" : "0");
        }

        public void PurgingMode(bool enabled)
        {
            SendCommand(Commands.PurgingMode, enabled ? "1" : "0");
        }

        public enum TestSite
        {
            Site1,
            Site2
        }
        public enum TrayMapZone
        {
            Zone1,
            Zone2
        }
        public bool StartTrayMapSite(TestSite site, TrayMapZone zone)
        {
            bool bStatus = false;


            string result = "";
            
            switch (site)
            {
                case TestSite.Site1:

                    if (zone == TrayMapZone.Zone1)
                    {
                        result = SendCommand(Commands.StartTrayMapSiteA1);
                    }
                    else
                    {
                        result = SendCommand(Commands.StartTrayMapSiteA2);
                    }
                    break;

                case TestSite.Site2:

                    if (zone == TrayMapZone.Zone1)
                    {
                        result = SendCommand(Commands.StartTrayMapSiteB1);
                    }
                    else
                    {
                        result = SendCommand(Commands.StartTrayMapSiteB2);
                    }
                    break;
            }

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandler_status = XElement.Parse(result);

                    if (xmlhandler_status.Name.LocalName == "TrayMap")
                    {
                        bool.TryParse(xmlhandler_status.Element("Status").Value.ToString().Trim().ToUpper(), out bStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return bStatus;
        }

        protected string SendCommand(Commands cmd, string arguments = "")
        {
            string query_result = string.Empty;
            string command_string = string.Empty;

            try
            {
                if (Connect() == true)
                {
                    switch (cmd)
                    {
                        case Commands.StartOfLot:
                            command_string = "*LOT:STRT" + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.StartOfLotQuery:
                            command_string = "*LOT:STRT?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.StartOfLotSplitTestQuery:
                            command_string = "*LOT:STRT? SPLIT" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.EndOfLot:
                            command_string = "*LOT:END" + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.EndOfLotQuery:
                            command_string = "*LOT:END?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.LastTestedInTheLotQuery:
                            command_string = "*LOT:LAST?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.DutIDQuery:
                            command_string = "*DUT:ID?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.DutIDSet:
                            command_string = "*DUT:ID " + arguments + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.ContactForceQuery:
                            command_string = "*HAND:Force?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.MoveForward:
                            command_string = "*MOV:FWD " + arguments + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.MoveReverse:
                            command_string = "*MOV:REV " + arguments + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.StartOfTestQuery:
                            command_string = "*SOT?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.TestInProgress:
                            command_string = "*TIP" + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.EndOfTest:
                            command_string = "*EOT " + arguments + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.UtilizationPerHourQuery:
                            command_string = string.Format("*HAND:UPH? {0:yyyy-MM-dd}", DateTime.Now) + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.PickUpHeadTestYieldQuery:
                            command_string = "*HAND:YIELD?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.PickUpHeadTestYieldResetQuery:
                            command_string = "*HAND:YIELD? RST" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.HandlerIDQuery:
                            command_string = "*HAND:ID?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.HandlerStatusQuery:
                            command_string = "*HAND:STAT?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.ChangeProfile:
                            command_string = "*HAND:PROF " + arguments;
                            Transmit(command_string, false);
                            break;

                        case Commands.GetProfile:
                            command_string = "*HAND:PROF?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.ResetHandler:
                            command_string = "*HAND:RESET" + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.AutomaticMode:
                            command_string = "*HAND:AUTO " + arguments + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.PurgingMode:
                            command_string = "*HAND:PURGE " + arguments + Convert.ToChar(0x0D);
                            Transmit(command_string, false);
                            break;

                        case Commands.StartTrayMapSiteA1:
                            command_string = "*HAND:TrayMapSiteA1?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.StartTrayMapSiteA2:
                            command_string = "*HAND:TrayMapSiteA2?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.StartTrayMapSiteB1:
                            command_string = "*HAND:TrayMapSiteB1?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.StartTrayMapSiteB2:
                            command_string = "*HAND:TrayMapSiteB2?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;

                        case Commands.BarcodeIDQuery:    // only for Hontech
                            command_string = "*HAND:GET2DID?" + Convert.ToChar(0x0D);
                            query_result = Transmit(command_string, true);
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
            }
            finally
            {
                Disconnect();
            }

            return query_result;
        }

        public bool Connect()
        {
            Debug.WriteLine("Connecting to Handler.....");

            if (tcpclnt == null)
                tcpclnt = new TcpClient();

            try
            {
                var _connection_result = tcpclnt.BeginConnect(server_ip_address, 1818, null, null);
                // use the ipaddress as in the server program
                var connection_success = _connection_result.AsyncWaitHandle.WaitOne(100);

                if (connection_success)
                {
                    if (tcpclnt.Connected)
                    {
                        _connected = true;
                    }
                    else
                    {
                        _connected = false;
                    }
                }
                else
                {
                    _connected = false;
                    tcpclnt.EndConnect(_connection_result);
                }
            }
            catch { _connected = false; }

            return _connected;
        }

        public void Disconnect()
        {
            try
            {
                tcpclnt.Close();
            }
            catch { }
            finally
            {
                tcpclnt = null;
                _connected = false;
            }
        }

        private string Transmit(string msg, bool is_query = false)
        {
            StringBuilder query_result = new StringBuilder();

            try
            {
                if (_connected)
                {
                    responseEnd = false;
                    Array.Clear(responseBytes, 0, responseBytes.Length);

                    NetworkStream stm = tcpclnt.GetStream();

                    ASCIIEncoding asen = new ASCIIEncoding();
                    byte[] ba = asen.GetBytes(msg);

                    Debug.Write("Transmitting... " + msg + Environment.NewLine);

                    stm.Write(ba, 0, ba.Length);

                    if (is_query)
                    {
                        while (responseEnd == false)
                        {
                            if (msg.ToUpper().Contains(":UPH") ||
                            msg.ToUpper().Contains(":YIELD"))
                            {
                                stm.ReadTimeout = 3000;
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                stm.ReadTimeout = 300;
                            }

                            int bytesRead = stm.Read(responseBytes, 0, responseBytes.Length);

                            if (bytesRead > 0)
                            {
                                for (int i = 0; i < responseBytes.Length; i++)
                                {
                                    if (responseBytes[i] != 0)
                                    {
                                        query_result.Append(Convert.ToChar(responseBytes[i]));
                                    }
                                    else
                                    {
                                        responseEnd = true;
                                        break;
                                    }
                                }
                                Array.Clear(responseBytes, 0, responseBytes.Length);
                            }
                            else
                            {
                                break;
                            }

                        }
                    }
                    stm.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(Environment.MachineName + " " + this.GetType().Name + Environment.NewLine + 
                    MethodBase.GetCurrentMethod().Name + Environment.NewLine + 
                    string.Format("Message: {0}Stack Trace: {1}", ex.Message + Environment.NewLine, ex.StackTrace + Environment.NewLine));
            }
            return query_result.ToString();
        }
    }

    public class HontechHandler : ExistechHandler
    {
        public HontechHandler()
        {
            _test_site = 1;
            server_ip_address = "192.168.0.101";
        }

        public HontechHandler(int site)
        {
            switch (site)
            {
                case 0:
                    base.server_ip_address = "127.0.0.1";
                    break;

                case 1:
                default:
                    _test_site = 1;
                    server_ip_address = "192.168.0.101";
                    break;

                case 2:
                    _test_site = 2;
                    server_ip_address = "192.168.0.102";
                    break;

                case 3:
                    _test_site = 3;
                    server_ip_address = "192.168.0.103";
                    break;

                case 4:
                    _test_site = 4;
                    server_ip_address = "192.168.0.104";
                    break;
            }
        }

        ~HontechHandler()
        {
            Disconnect();
        }

        public override List<PickUpHeadTestYield> PickUpHeadTestYieldQuery(bool reset = false)
        {
            int temp_int = 0;
            double temp_double = 0;
            DateTime temp_datetime = DateTime.MinValue;
            List<PickUpHeadTestYield> handlertestyield = new List<PickUpHeadTestYield>();
            string result = string.Empty;

            if (reset == false)
            {
                result = SendCommand(Commands.PickUpHeadTestYieldQuery);
            }
            else
            {
                result = SendCommand(Commands.PickUpHeadTestYieldResetQuery);
            }

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlertestyield = XElement.Parse(result);

                    if (xmlhandlertestyield.Name.LocalName == "HandlerYield")
                    {
                        IEnumerable<XElement> testheads = xmlhandlertestyield.Elements("Head");

                        foreach (XElement testhead in testheads)
                        {
                            PickUpHeadTestYield testyield = new PickUpHeadTestYield();

                            if (int.TryParse(testhead.Attribute("ID").Value, out temp_int))
                            {
                                testyield.TestSite = temp_int;
                                testyield.PickUpHeadNo = temp_int;
                            }
                            if (double.TryParse(testhead.Element("Yield").Value, out temp_double))
                            {
                                testyield.TestYield = temp_double;
                            }
                            if (int.TryParse(testhead.Element("Count").Value, out temp_int))
                            {
                                testyield.TotalCount = temp_int;
                            }
                            handlertestyield.Add(testyield);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return handlertestyield;
        }

        public HandlerForce ContactForceQuery()
        {
            int temp_int = 1;
            Decimal temp_decimal = 0;
            HandlerForce handlerForce = new HandlerForce();
            string result = base.SendCommand(Commands.ContactForceQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "HandlerForce")
                    {
                        if(int.TryParse(xmlhandlerinfo.Element("Site").Value, out temp_int))
                        {
                            handlerForce.SiteNo = temp_int;
                        }
                        if (int.TryParse(xmlhandlerinfo.Element("Arm").Value, out temp_int))
                        {
                            handlerForce.ArmNo = temp_int;
                        }
                        if (Decimal.TryParse(xmlhandlerinfo.Element("PlungerForce").Value, out temp_decimal))
                        {
                            handlerForce.PlungerForce = temp_decimal;
                        }
                        if (Decimal.TryParse(xmlhandlerinfo.Element("EPValueVoltage").Value, out temp_decimal))
                        {
                            handlerForce.EPVoltage = temp_decimal;
                        }
                   }
                }
            }
            catch { }
            return handlerForce;
        }

        public string Get2DID()
        {
            string id = "";
            string result = base.SendCommand(Commands.BarcodeIDQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "Handler2DID")
                    {
                        id = xmlhandlerinfo.Element("BARCODE").Value.Trim();
                    }
                }
            }
            catch { }
            return id;
        }
    }

    public class Existech550Handler : HontechHandler
    {
        public Existech550Handler()
        {
            _test_site = 1;
            server_ip_address = "127.0.0.1";
        }

        public Existech550Handler(int site)
        {
            switch (site)
            {
                case 0:
                    _test_site = 1;
                    server_ip_address = "127.0.0.1";
                    break;

                case 1:
                default:
                    _test_site = 1;
                    server_ip_address = "192.168.0.101";
                    break;

                case 2:
                    _test_site = 2;
                    server_ip_address = "192.168.0.102";
                    break;

                case 3:
                    _test_site = 3;
                    server_ip_address = "192.168.0.103";
                    break;

                case 4:
                    _test_site = 4;
                    server_ip_address = "192.168.0.104";
                    break;
            }
        }

        ~Existech550Handler()
        {
            Disconnect();
        }

        public void GetID(out string module_id, out string mfg_id)
        {
            module_id = "";
            mfg_id = "";

            string result = base.SendCommand(Commands.DutIDQuery);

            try
            {
                if (result.Length > 0)
                {
                    XElement xmlhandlerinfo = XElement.Parse(result);

                    if (xmlhandlerinfo.Name.LocalName == "DUT")
                    {
                        module_id = xmlhandlerinfo.Element("ModuleID").Value.Trim();
                        mfg_id = xmlhandlerinfo.Element("MfgID").Value.Trim();
                    }
                }
            }
            catch { }
        }

        public void SetID(string module_id, string mfg_id)
        {
            module_id = "";
            mfg_id = "";

            base.SendCommand(Commands.DutIDSet, module_id + " " + mfg_id);
        }
    }

    public class HandlerID
    {
        public string SerialNo;
        public string SoftwareVersion;
    }

    public class HandlerStatus
    {
        public int TowerLight;
        public List<int> ErrorCode;
    }

    public class HandlerInfo
    {
        public int PlungerSettlingTime;
        public int StartOfTestDelay;
        public string PlungerType;
        public int TurretSpeed;
        public int NoOfTestSite;
        public string SoftwareVersion;
        public string SerialNo;
    }

    public class HandlerLotInfo
    {
        public DateTime Timestamp;
        public decimal PlungerForce;
        public int PlungerHeadNo;
        public decimal WorkingStroke;
        public int PickUpHeadNo;
        public Int64 BarcodeID;
        public string strBarcodeID;
    }

    public class ID
    {
        public int ModuleID;
        public int MfgID;
    }

    public class HandlerForce
    {
        public int SiteNo;
        public int ArmNo;
        public decimal PlungerForce;
        public decimal EPVoltage;
    }

    public class HandlerSOT
    {
        public int Status = 0;
        public int IndexMoved = 0;
        public int Site1 = 0;
        public int Site2 = 0;
    }

    public class HandlerUPH
    {
        public DateTime Timestamp;
        public int RunTime;
        public int ErrorTime;
        public int IdlingTime;    // idling
        public int InitTime;
        public int SetupTime;
    }

    public class PickUpHeadTestYield
    {
        public int TestSite;
        public int PickUpHeadNo;
        public double TestYield;
        public int TotalCount;
    }

    class XmlValidator
    {
        public static bool Validate(string xml)
        {
            try
            {
                new XmlDocument().LoadXml(xml);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

