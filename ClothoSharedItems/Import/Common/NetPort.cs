using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClothoSharedItems.Common
{
    public sealed class NetPort : IDisposable
    {
        private TcpClient m_tcpClient;
        private NetworkStream m_netStream;
        private byte[] m_rcvData = new byte[1024 * 1000];
        private bool m_isAlive = false;
        public bool isAlive { get { return m_isAlive; } set { m_isAlive = value; } }

        public bool Open(string ipAddress, string port)
        {
            try
            {
                m_tcpClient = new TcpClient(ipAddress, port.ToInt32());
                m_netStream = m_tcpClient.GetStream();
                m_isAlive = true;
                return true;
            }
            catch
            {
                m_tcpClient = null;
                m_netStream = null;
            }
            return false;
        }

        ~NetPort()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_tcpClient != null)
            {
                m_isAlive = false;
                m_netStream.Close();
                m_tcpClient.Close();
            }
        }

        public string Read(double timeout)
        {
            DateTime dtStart = DateTime.Now;
            do
            {
                if (!m_isAlive) break;
                if (!m_netStream.CanRead) break;

                if (m_netStream.DataAvailable)
                {
                    int rcvLen = m_netStream.Read(m_rcvData, 0, m_rcvData.Length);
                    if (rcvLen > 0)
                    {
                        return System.Text.Encoding.ASCII.GetString(m_rcvData, 0, rcvLen);
                    }
                }
                else ClothoSharedItems.Common.Waiter.Doze();
            } while ((DateTime.Now - dtStart).TotalSeconds <= timeout);
            return "";
        }

        public byte[] ReadByte(ref byte[] result, double timeout, bool checkMoreBy100msWait = false)
        {
            DateTime dtStart = DateTime.Now;
            do
            {
                if (!m_isAlive) break;
                if (!m_netStream.CanRead) break;

                if (m_netStream.DataAvailable)
                {
                    int rcvLenSum = 0;
                    do
                    {
                        int rcvLen = m_netStream.Read(m_rcvData, rcvLenSum, m_rcvData.Length - rcvLenSum);
                        if (rcvLen > 0)
                        {
                            rcvLenSum += rcvLen;
                            if (!checkMoreBy100msWait || rcvLenSum >= m_rcvData.Length) break;
                        }
                        System.Threading.Thread.Sleep(100);
                    } while (m_netStream.DataAvailable);

                    result = m_rcvData.SubArray(0, rcvLenSum);
                    return result;
                }
                else ClothoSharedItems.Common.Waiter.Doze();
            } while ((DateTime.Now - dtStart).TotalSeconds <= timeout);
            return result;
        }

        public void Write(string data)
        {
            if (m_isAlive && m_netStream.CanWrite)
            {
                if (m_netStream.DataAvailable)
                    m_netStream.Read(m_rcvData, 0, m_rcvData.Length);

                byte[] wrtData = System.Text.Encoding.ASCII.GetBytes(data);
                m_netStream.Write(wrtData, 0, wrtData.Length);
                m_netStream.Flush();
            }
        }

        public string Query(string data, double timeout)
        {
            Write(data);
            return Read(timeout);
        }

        public byte[] QueryByte(string data, double timeout)
        {
            byte[] result = new byte[] { };
            this.Write(data);
            this.ReadByte(ref result, timeout);
            return result;
        }

        public static bool IsRespondedByPing(string addressIP, double timeout)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            PingReply reply = pingSender.Send(addressIP, (int)(1000 * timeout), data, options);
            return (reply.Status == IPStatus.Success);
        }

        private Thread m_scanThread = null;
        private bool m_flagScan = false;
        private List<string> ipAll = new List<string>();

        public void GetIPlist(ref List<string> ipList, List<byte[]> pattern = null, bool stopifnonFilter = false)
        {
            const int PING_TIMEOUT = 500;
            const int PING_RETRY_COUNT = 10;
            const int THREAD_DEAD_COUNTOUT = 10;
            ipAll.Clear();

            if (m_flagScan)
            {
                m_flagScan = false;
                m_scanThread?.Join();
            }
            else
            {
                m_scanThread?.Join();

                var addrsHost = new List<byte[]>();
                foreach (var ipAddr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                        addrsHost.Add(ipAddr.GetAddressBytes());
                }

                if (addrsHost.Count > 0)
                {
                    var pingBytes = Encoding.ASCII.GetBytes("0123456789");
                    var pingOptions = new PingOptions() { DontFragment = true };

                    if (stopifnonFilter && pattern != null)
                    {
                        if (!addrsHost.Any(t => pattern.Any(v => v[0] == t[0] && v[1] == t[1])))
                            return;
                    }

                    var addrRef = addrsHost.FirstOrDefault(addr => addr[0] == 192 && addr[1] == 168) ?? addrsHost.First();

                    var ipTargets = new List<string>();
                    for (int iAddr = 2; iAddr <= 255; iAddr++)
                    {
                        if (iAddr == addrRef[3]) continue;
                        ipTargets.Add($"{addrRef[0]}.{addrRef[1]}.{addrRef[2]}.{iAddr}");
                    }

                    var nPingSum = 0;
                    var nPingSumMax = ipTargets.Count * PING_RETRY_COUNT;

                    m_flagScan = true;

                    var tasksActivated = new List<Task>();
                    foreach (var ipTarget in ipTargets)
                    {
                        tasksActivated.Add(Task.Factory.StartNew(() =>
                        {
                            long timeOfFailed = 0;
                            int leftTry = PING_RETRY_COUNT;

                            while (m_flagScan && leftTry > 0)
                            {
                                try
                                {
                                    using (Ping pingSender = new Ping())
                                    {
                                        while (m_flagScan && leftTry > 0)
                                        {
                                            leftTry--;
                                            Interlocked.Add(ref nPingSum, 1);
                                            var reply = pingSender.Send(ipTarget, PING_TIMEOUT, pingBytes, pingOptions);

                                            if (reply.Status == IPStatus.Success)
                                            {
                                                ipAll.Add(reply.Address.ToString());
                                                Interlocked.Add(ref nPingSum, leftTry);
                                                return;
                                            }
                                            else timeOfFailed += PING_TIMEOUT;

                                            if (m_flagScan) Thread.Sleep(250);
                                        }
                                    }
                                }
                                catch { }// (Exception ex) { this.Text = $"[{count++}] {ex.ToString()}"; }
                            }
                        }, TaskCreationOptions.LongRunning));
                    }

                    (m_scanThread = new Thread(() =>
                    {
                        var nAfterEnd = 0;
                        while (true)
                        {
                            foreach (var taskCompleted in tasksActivated.Where(t => t.IsCompleted).ToArray())
                                tasksActivated.Remove(taskCompleted);

                            var isComplete = tasksActivated.Count == 0;

                            if (isComplete || nAfterEnd >= THREAD_DEAD_COUNTOUT)
                            {
                                if (!isComplete)
                                {
                                    foreach (var task in tasksActivated)
                                    {
                                        try { task.Dispose(); } catch { }
                                    }
                                }

                                m_flagScan = false;
                                m_scanThread = null;

                                break;
                            }
                            else if (m_flagScan)
                            {
                                if (nPingSum >= nPingSumMax)
                                    m_flagScan = false;
                            }
                            else nAfterEnd++;

                            Thread.Sleep(100);
                        }
                    }
                    )).Start();
                }
            }

            ipList.AddRange(ipAll);
        }
    }
}