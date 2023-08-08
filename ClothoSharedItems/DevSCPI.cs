using ClothoSharedItems.Common;
using ClothoSharedItems.Import.GPIB;
using ClothoSharedItems.Import.VISA;
using System;
using System.Collections.Generic;

namespace ClothoSharedItems
{
    public abstract partial class DevSCPI : DevBase
    {
        public const double PING_TIMEOUT = 2.0;

        public const string NA_TCPIPAddress = "0.0.0.0";
        public const string NA_TCPIPPort = "0";
        public const int NA_GPIBAddress = 0;

        private int m_groupId;
        private bool m_isOpen;
        private string m_addrGPIB;
        private string m_addrTCPPort;
        private string m_addrTCPIP;
        private string m_visaAddress;
        private double m_timeoutTCP;
        private DevType m_chanType;
        private NetPort m_netPort;
        private OptionTCPIP m_optionTCPIP;
        private VIsa m_visaPort;
        private int m_vi = 0;
        private Dictionary<string, object> m_cmdsRegistered;

        public DevSCPI(string name, DevType devType, DevType chanType, string addrDefGPIB, string addrDefTCPIP, string addrDefTCPPort, string addrDefVISA = null, int gpibvi = 0)
            : base(name, devType & (DevType.GPIB | DevType.TCPIP_INSTR | DevType.TCPIP_SOCKET))
        {
            m_isOpen = false;
            m_timeoutTCP = 40.0;
            m_chanType = chanType;
            m_addrGPIB = addrDefGPIB;
            m_addrTCPIP = addrDefTCPIP;
            m_addrTCPPort = addrDefTCPPort;
            m_visaAddress = addrDefVISA;

            m_vi = gpibvi;
            m_visaPort = new VIsa();

            m_netPort = new NetPort();
            m_optionTCPIP = OptionTCPIP.AddNewLineToRequest | OptionTCPIP.DeleteLastCharFromResponse;
            m_cmdsRegistered = new Dictionary<string, object>();
        }

        public DevType DeviceType { get { return m_chanType; } set { m_chanType = value; } }

        public override bool Open(int groupId)
        {
            if (!m_isOpen)
            {
                try
                {
                    if ((m_chanType & DevType.GPIB) > 0)
                    {
                        if (!m_visaPort.Open(m_addrGPIB, 10000, ref m_vi))
                            return false;
                    }
                    else if (m_chanType == DevType.TCPIP_INSTR)
                    {
                        if (!NetPort.IsRespondedByPing(m_addrTCPIP, PING_TIMEOUT))
                            return false;

                        if (!m_visaPort.Open("TCPIP::" + m_addrTCPIP + "::INSTR", 10000, ref m_vi))
                            return false;
                    }
                    else if (m_chanType == DevType.TCPIP_SOCKET)
                    {
                        if (!NetPort.IsRespondedByPing(m_addrTCPIP, PING_TIMEOUT))
                        {
                            return false;
                        }

                        if (!m_netPort.Open(m_addrTCPIP, m_addrTCPPort))
                        {
                            return false;
                        }
                    }
                    else return false;

                    if (!IsRightDevice())
                        return false;

                    if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
                    {
                    }
                }
                catch
                {
                    if (m_isOpen)
                    {
                        m_isOpen = false;
                    }

                    if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
                    {
                        m_visaPort.Close(m_vi);
                    }
                    else if (m_chanType == DevType.TCPIP_SOCKET)
                    {
                        m_netPort.Dispose();
                    }

                    return false;
                }

                m_isOpen = true;
                m_groupId = groupId;
            }
            else if (m_groupId != groupId) return false;

            m_cmdsRegistered.Clear();

            Initialize();
            return true;
        }

        public override void Close(int groupId)
        {
            if (m_isOpen && (groupId == 0 || groupId == m_groupId))
            {
                if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
                    m_visaPort.Close(m_vi);
                else if (m_chanType == DevType.TCPIP_SOCKET)
                    m_netPort.Dispose();

                m_isOpen = false;
            }
        }

        public override bool IsOpen { get { return m_isOpen; } }

        public override bool IsOpenBy(int groupId)
        {
            return m_isOpen && (groupId == m_groupId);
        }

        public virtual void Write(string command)
        {
            if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
            {
                m_visaPort.Write(m_vi, command);
            }
            else if (m_chanType == DevType.TCPIP_SOCKET)
            {
                if ((m_optionTCPIP & OptionTCPIP.AddNewLineToRequest) > 0)
                    m_netPort.Write(command + "\n");
                else m_netPort.Write(command);
            }
        }

        public virtual string Read()
        {
            bool pResult = false;
            return Read(ref pResult);
        }

        public virtual string Read(ref bool pResult)
        {
            string result = "";

            if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
            {
                pResult = m_visaPort.Read(m_vi, ref result);
            }
            else if (m_chanType == DevType.TCPIP_SOCKET)
            {
                var rstString = m_netPort.Read(m_timeoutTCP);
                if (rstString != null && rstString.Length > 0)
                {
                    result = ((m_optionTCPIP & OptionTCPIP.DeleteLastCharFromResponse) > 0 ? rstString.Substring(0, rstString.Length - 1) : rstString);
                }

                pResult = (result.Length > 0);
            }

            return result;
        }

        public byte[] ReadByte(bool checkMoreBy100msWait = false)
        {
            byte[] result = new byte[] { };
            if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
            {
                m_visaPort.ReadByte(m_vi, ref result);
            }
            else if (m_chanType == DevType.TCPIP_SOCKET)
            {
                m_netPort.ReadByte(ref result, m_timeoutTCP, checkMoreBy100msWait);
            }

            return result;
        }

        public virtual string Query(string command, ref bool pResult)
        {
            Write(command); return Read(ref pResult);
        }

        public virtual string Query(string command)
        {
            Write(command);
            return Read();
        }

        public byte[] QueryByte(string command, bool checkMoreBy100msWait = true)
        {
            Write(command);
            return ReadByte(checkMoreBy100msWait);
        }

        public void Clear()
        {
            if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
                m_visaPort.Clear(m_vi);
            else
                Write("*CLS");
        }

        public virtual void Reset()
        {
            m_cmdsRegistered.Clear(); Write("*RST"); WaitComplete();
        }

        public void WaitComplete()
        {
            int flagRetry = 0;
            bool isPass = false;
            do
            {
                try
                {
                    Query("*OPC?", ref isPass);
                    if (isPass) break;
                }
                catch { flagRetry += 1; }
            }
            while (m_isOpen && (flagRetry < 3));
        }

        public byte GetSBR()
        {
            return m_visaPort.GetSBR(m_vi);
        }

        public void GotoLocal_GPIB()
        {
            if (m_chanType == DevType.GPIB)
            {
                m_visaPort.GotoLocal(m_vi);
            }
            else if (m_chanType == DevType.TCPIP_INSTR || m_chanType == DevType.TCPIP_SOCKET) { }
            else throw new NotImplementedException("Only available for GPIB");
        }

        public void ResetHistory()
        {
            m_cmdsRegistered.Clear();
        }

        public bool WriteIfUnregistered(string command, object value, bool doWrite = true)
        {
            object valueOld;
            if (!m_cmdsRegistered.TryGetValue(command, out valueOld) || !value.Equals(valueOld))
            {
                m_cmdsRegistered[command] = value;
                if (doWrite)
                    Write(command + " " + value.ToString());
                return true;
            }
            return false;
        }

        public abstract void Initialize();

        public abstract bool IsRightIDN(string idn);

        protected virtual bool IsRightDevice()
        {
            if (m_chanType == DevType.GPIB || m_chanType == DevType.TCPIP_INSTR)
            {
                string result = "";
                if (m_visaPort.Query(m_vi, "*IDN?", ref result))
                    return IsRightIDN(result);
                else
                    return false;
            }
            else
                return IsRightIDN(Query("*IDN?"));
        }

        public void SetPortType(DevType chanType)
        {
            m_chanType = (chanType & base.Type);
        }

        public string GPIBAddress { get { return m_addrGPIB; } set { m_addrGPIB = value; } }
        public string TCPIPPort { get { return m_addrTCPPort; } set { m_addrTCPPort = value; } }
        public string TCPIPAddress { get { return m_addrTCPIP; } set { m_addrTCPIP = value; } }
        public string VISAAddress { get { return m_visaAddress; } set { m_visaAddress = value; } }

        public void SetTCPIPTimeout(double timeout_sec)
        {
            m_timeoutTCP = timeout_sec.TrimMin(0.1);
        }

        public void SetTCPIPOption(OptionTCPIP option, bool set)
        {
            m_optionTCPIP = (set ? (m_optionTCPIP | option) : (m_optionTCPIP & (~option)));
        }

        public void SetGPIBTimeout(GpibTimeout value)
        {
            m_visaPort.SetTimeout(m_vi, value);
        }
    }
}