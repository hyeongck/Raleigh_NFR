using ClothoSharedItems.Import.WIN.SERIAL;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClothoSharedItems.Common
{
    public sealed class ComPort : IDisposable
    {
        // source from 'http://channel9.msdn.com/Forums/TechOff/528962-Stumbling-on-Basic-Serial-Port-IO'

        private const int BUFFER_SIZE = 65536;

        //private bool m_disposed = false;
        private byte m_endChar = (byte)'\r';

        private IntPtr m_handle = IntPtr.Zero;
        private byte[] m_buffer = new byte[BUFFER_SIZE];
        private StringBuilder m_sbRead = new StringBuilder();

        ~ComPort()
        {
            Dispose();
        }

        public bool Open(string portName, ConfigureCommState configureDCB, COMMTIMEOUTS timeouts)
        {
            m_handle = SerialAPI.CreateFile(portName, FileAccess.ReadWrite, FileShare.None,
                        IntPtr.Zero, FileMode.Open, 0 /*0x00000080 | 0x40000000*/ , IntPtr.Zero);
            if (m_handle == IntPtr.Zero) return false;

            //if (!SerialFunc.PurgeComm(m_handle, 0x000C) ||
            //    !SerialFunc.SetCommMask(m_handle, 0x0178)) { //EV_RXCHAR
            //    Dispose();
            //    return false;
            //}

            DCB dcb = new DCB();
            if (SerialAPI.GetCommState(m_handle, ref dcb))
            {
                if (configureDCB != null)
                    configureDCB(ref dcb);
                if (SerialAPI.SetCommState(m_handle, ref dcb))
                {
                    if (SerialAPI.SetCommTimeouts(m_handle, ref timeouts))
                        return true;
                }
            }

            Dispose();
            return false;
        }

        public void Dispose()
        {
            if (m_handle != IntPtr.Zero)
            {
                //this.Flush(true);
                SerialAPI.CloseHandle(m_handle);
                m_handle = IntPtr.Zero;
            }
        }

        //public bool Flush(bool abortRTx = false)
        //{
        //    const int PURGE_TXABORT = 0x0001;
        //    const int PURGE_RXABORT = 0x0002;
        //    const int PURGE_TXCLEAR = 0x0004; // output buffer
        //    const int PURGE_RXCLEAR = 0x0008; // input buffer

        //    int dwFlag = PURGE_RXCLEAR | PURGE_TXCLEAR;
        //    if (abortRTx) dwFlag |= PURGE_TXABORT | PURGE_RXABORT;
        //    return SerialFunc.PurgeComm(m_handle, dwFlag);
        //}

        public int Read(byte[] data, double timeout)
        {
            int bytesRead = -1;
            DateTime dtStart = DateTime.Now;
            do
            {
                if (!SerialAPI.ReadFile(m_handle, data, data.Length, out bytesRead, 0))
                    return -1;

                if (bytesRead > 0) break;
                else ClothoSharedItems.Common.Waiter.Doze();
            } while ((DateTime.Now - dtStart).TotalSeconds < timeout);

            return bytesRead;
        }

        public int Write(byte[] data)
        {
            if (data == null) return 0;

            int bytesWritten;
            SerialAPI.PurgeComm(m_handle, 0x000C);
            if (SerialAPI.WriteFile(m_handle, data, data.Length, out bytesWritten, 0))
            {
                //SerialFunc.FlushFileBuffers(m_handle);
                return bytesWritten;
            }
            return -1;
        }

        public string Read(double timeout)
        {
            m_sbRead.Clear();

            int nRead;
            DateTime dtStart = DateTime.Now;
            do
            {
                if (!SerialAPI.ReadFile(m_handle, m_buffer, m_buffer.Length, out nRead, 0))
                    return null;

                if (nRead > 0)
                {
                    for (int i = 0; i < nRead; i++)
                    {
                        if (m_buffer[i] == m_endChar)
                        {
                            m_sbRead.Append(ASCIIEncoding.ASCII.GetString(m_buffer, 0, i));
                            return m_sbRead.ToString();
                        }
                    }
                    m_sbRead.Append(ASCIIEncoding.ASCII.GetString(m_buffer, 0, nRead));
                    //if (m_buffer[nRead - 1] == m_endChar) {
                    //    m_sbRead.Append(ASCIIEncoding.ASCII.GetString(m_buffer, 0, nRead - 1));
                    //    break;
                    //} else {
                    //    m_sbRead.Append(ASCIIEncoding.ASCII.GetString(m_buffer, 0, nRead));
                    //}
                }
                else ClothoSharedItems.Common.Waiter.Doze();
            } while ((DateTime.Now - dtStart).TotalSeconds < timeout);

            return m_sbRead.ToString();
        }

        public void Write(string data)
        {
            if (data != null && data.Length > 0)
            {
                int nWrite;
                byte[] byteData = ASCIIEncoding.ASCII.GetBytes(data);
                SerialAPI.PurgeComm(m_handle, 0x000C);
                SerialAPI.WriteFile(m_handle, byteData, byteData.Length, out nWrite, 0);
                //SerialFunc.FlushFileBuffers(m_handle);
            }
        }

        public string Query(string data, double timeout)
        {
            //SerialFunc.PurgeComm(m_handle, 0x000C);
            Write(data);
            return Read(timeout);
        }

        public void Clear()
        {
            SerialAPI.PurgeComm(m_handle, 0x000C);
        }

        public static string[] GetComportNames(Predicate<string> ifMatched)
        {
            List<string> m_comList = new List<string>();
            using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            {
                if (regKey != null)
                {
                    foreach (string idn in regKey.GetValueNames())
                    {
                        if (ifMatched != null && !ifMatched(idn))
                            continue;

                        string portName = (string)regKey.GetValue(idn);
                        if (!portName.CIvStartsWith("COM")) continue;

                        m_comList.Insert(0, portName);
                    }
                }
            }

            return m_comList.ToArray();
        }

        public static string[] GetDiagPortNames()
        {
            return ComPort.GetComportNames(idn => idn.CIvContains("\\LG") && idn.CIvContains("DIAG"));
        }

        public static List<int> GetDiagPortIDs()
        {
            const string KEYNAME_SERIALCOMM = @"HARDWARE\DEVICEMAP\SERIALCOMM";

            var ports = new List<int>();

            using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(KEYNAME_SERIALCOMM))
            {
                if (regKey != null)
                {
                    foreach (string idn in regKey.GetValueNames())
                    {
                        if (!idn.CIvContainsAllOf("\\LG", "DIAG") &&
                        !idn.CIvContains("QCUSB"))
                            continue;
                        //if (!idn.CIvContainsAnyOf(PORTIDS_VALID)) continue;

                        string portName = (string)regKey.GetValue(idn);
                        if (portName.CIvStartsWith("COM"))
                            ports.Add(portName.Substring(3).ToInt32());
                    }
                }
            }
            return ports;
        }
    }
}