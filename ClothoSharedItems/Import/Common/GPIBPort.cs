using ClothoSharedItems.Import.GPIB;
using ClothoSharedItems.Import.VISA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClothoSharedItems.Common
{
    public sealed class GPIBPort
    {
        private const int BUFFER_SIZE = 1024;

        private static readonly char[] _TRIMCHARS = new char[] { '\n', '\r' };//, ' ' };
        private static readonly byte[] _TRIMCHARS_BYTE = new byte[] { 0x0A, 0x0D };//, ' ' };

        private static int m_idBd = 0;
        private int m_idUd = 0;
        private byte[] m_byteBuffer = new byte[BUFFER_SIZE];
        private static int vi = 0;

        private static bool? m_is32os = null;

        private static bool _32DllAvailable
        {
            get
            {
                if (m_is32os == null)
                {
                    try
                    {
                        if ((GpibAPI32.ThreadIbsta() & GpibStatus.Error) == 0)
                            m_is32os = true;
                        else

                            m_is32os = true;
                    }
                    catch { m_is32os = false; }
                }
                return m_is32os.Value;
            }
        }

        private static bool UpdateBoardID()
        {
            for (int ibd = 0; ibd <= 2; ibd++)
            {
                if (_32DllAvailable)
                {
                    var rsp = GpibAPI32.ibfind("GPIB" + ibd);
                    if (rsp != -1 && (GpibAPI32.ThreadIbsta() & GpibStatus.Error) == 0)
                    {
                        m_idBd = ibd;
                        GpibAPI32.ibsic(rsp);
                        return true;
                    }
                }
                else
                {
                    var rsp = GpibAPI64.ibfind("GPIB" + ibd);
                    if (rsp != -1 && (GpibAPI64.ThreadIbsta() & GpibStatus.Error) == 0)
                    {
                        m_idBd = ibd;
                        GpibAPI64.ibsic(rsp);
                        return true;
                    }
                }
            }
            return false;
        }

        public static Dictionary<int, string> GetDeviceIDNs()
        {
            var dicIDNs = new Dictionary<int, string>();
            var visa = new VIsa();
            int retCount = 0;
            List<string> rmList = new List<string>();
            string pattern = "GPIB?*::?*INSTR";
            try
            {
                visa.FindVISAResource(ref vi, ref retCount, ref rmList, pattern);

                if (rmList.Count > 0)
                {
                    for (int i = 0; i < rmList.Count; i++)
                    {
                        Regex regex = new Regex(@"GPIB(?<IBD>\d)::(?<PAD>\d+)::.*INSTR", RegexOptions.IgnoreCase);
                        var isMatch = regex.Match(rmList[i]);
                        if (i == 0) m_idBd = isMatch.Groups["IBD"].Value.ToInt32();
                        try
                        {
                            if (visa.Open(rmList[i], 3000, ref vi))
                            {
                                string result = "";
                                if (visa.Query(vi, "*IDN?", ref result))
                                {
                                    dicIDNs[isMatch.Groups["PAD"].Value.ToInt32()] = result;
                                    visa.Clear(vi);
                                }
                            }
                        }
                        finally { visa.Close(vi); }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.GetType().ToString() + ": \n  - " + ex.Message + "\n" + ex.StackTrace);
            }

            return dicIDNs;
        }

        public static List<int> GetDeviceAddrs()
        {
            var tList = GetDeviceIDNs();
            var addresses = new List<int>();
            addresses.AddRange(tList.Keys);
            return addresses;
        }

        public int IBCNT { get { return _32DllAvailable ? GpibAPI32.ThreadIbcnt() : GpibAPI64.ThreadIbcnt(); } }
        public GpibError IBERR { get { return _32DllAvailable ? GpibAPI32.ThreadIberr() : GpibAPI64.ThreadIberr(); } }
        public GpibStatus IBSTA { get { return _32DllAvailable ? GpibAPI32.ThreadIbsta() : GpibAPI64.ThreadIbsta(); } }

        public bool Open(int pad, int sad = 0, GpibTimeout tmo = GpibTimeout.T10s)
        {
            short listen;

            if (_32DllAvailable)
            {
                if ((m_idUd = GpibAPI32.ibdev(m_idBd, pad, sad, (int)tmo, 1, 0)) >= 0)
                {
                    if ((GpibAPI32.ibln(m_idUd, pad, sad, out listen) & GpibStatus.Error) == GpibStatus.None && listen != 0)
                        return true;
                }
            }
            else
            {
                if ((m_idUd = GpibAPI64.ibdev(m_idBd, pad, sad, (int)tmo, 1, 0)) >= 0)
                {
                    if ((GpibAPI64.ibln(m_idUd, pad, sad, out listen) & GpibStatus.Error) == GpibStatus.None && listen != 0)
                        return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            if (_32DllAvailable)
                GpibAPI32.ibclr(m_idUd);
            else GpibAPI64.ibclr(m_idUd);
        }

        ~GPIBPort()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Write(string command, ref GpibStatus errStatus)
        {
            GpibStatus status = _32DllAvailable ? GpibAPI32.ibwrt(m_idUd, command, command.Length) : GpibAPI64.ibwrt(m_idUd, command, command.Length);
            errStatus = status;
            if ((status & GpibStatus.Error) > 0)
            {
                return false;
            }
            return true;
        }

        public void Write(params string[] commands)
        {
            foreach (string cmd in commands) this.Write(cmd);
        }

        public string Read(double timeout = 10, bool isRetryOnError = false)
        {
            ByteStream tempStream = new ByteStream(1024 * 1000);
            int i = 0;
            int cnt = 0;
            GpibStatus status = GpibStatus.None;
            DateTime dtStart = DateTime.Now;

            do
            {
                status = _32DllAvailable ? GpibAPI32.ibrd(m_idUd, m_byteBuffer, m_byteBuffer.Length) : GpibAPI64.ibrd(m_idUd, m_byteBuffer, m_byteBuffer.Length);

                try
                {
                    if ((status & GpibStatus.Error) > 0)
                        throw new IOException(string.Format("IO error '{0}' occurs during reading from GPIB", status));
                }
                catch (IOException) when (isRetryOnError) { Waiter.Wait(3.0); }

                cnt = IBCNT;
                tempStream.WriteBytes((i++) * m_byteBuffer.Length, m_byteBuffer, cnt);

                if ((status & GpibStatus.End) > 0)
                {
                    return getReadString(tempStream);
                }
            } while ((DateTime.Now - dtStart).TotalSeconds <= timeout);

            return null;
        }

        public byte[] ReadByte(ref byte[] result, double timeout = 20, bool isRetryOnError = false)
        {
            ByteStream tempStream = new ByteStream(1024 * 1000);
            int i = 0;
            int cnt = 0;
            GpibStatus status = GpibStatus.None;
            DateTime dtStart = DateTime.Now;
            do
            {
                status = _32DllAvailable ? GpibAPI32.ibrd(m_idUd, m_byteBuffer, m_byteBuffer.Length) : GpibAPI64.ibrd(m_idUd, m_byteBuffer, m_byteBuffer.Length);

                try
                {
                    if ((status & GpibStatus.Error) > 0)
                        throw new IOException(string.Format("IO error '{0}' occurs during reading from GPIB", status));
                }
                catch (IOException) when (isRetryOnError) { Waiter.Wait(3.0); }

                cnt = IBCNT;
                tempStream.WriteBytes((i++) * m_byteBuffer.Length, m_byteBuffer, cnt);

                if ((status & GpibStatus.End) > 0)
                {
                    result = tempStream.Bytes;
                    return result;
                }
            } while ((DateTime.Now - dtStart).TotalSeconds <= timeout);

            return result;
        }

        public static byte[] ConcatByteArrays(params byte[][] arrays)
        {
            return arrays.SelectMany(x => x).ToArray();
        }

        public byte GetSBR()
        {
            byte spr = 0;
            GpibStatus status = _32DllAvailable ? GpibAPI32.ibrsp(m_idUd, ref spr) : GpibAPI64.ibrsp(m_idUd, ref spr);
            if ((status & GpibStatus.Error) != GpibStatus.None)
                throw new IOException(string.Format("IO error '{0}' occurs during serial pooling on GPIB", status));
            return spr;
        }

        public bool SetTimeout(GpibTimeout tmo)
        {
            GpibStatus status = GpibStatus.None;
            try
            {
                status = (_32DllAvailable ? GpibAPI32.ibtmo(m_idUd, (int)tmo) : GpibAPI64.ibconfig(m_idUd, (int)Gpib64Option.IbcTMO, (int)tmo));
            }
            catch
            {
                return false;
            }

            if ((status & GpibStatus.Error) != GpibStatus.None)
                return false;
            else
                return true;
        }

        public void GotoLocal()
        {
            if (((_32DllAvailable ? GpibAPI32.ibloc(m_idUd) : GpibAPI64.ibloc(m_idUd)) & GpibStatus.Error) != GpibStatus.None)
            {
            }
        }

        public string Query(string command)
        {
            GpibStatus errorcode = GpibStatus.None;
            if (this.Write(command, ref errorcode)) return this.Read(10, false);
            else return null;
        }

        public byte[] QueryByte(string command)
        {
            byte[] result = new byte[] { };
            this.Write(command);
            this.ReadByte(ref result);
            return result;
        }

        private string getReadString(ByteStream byteStream)
        {
            int len = byteStream.Length;
            while (len > 0)
            {
                byte cAtEnd = byteStream[len - 1];
                if (Array.TrueForAll(_TRIMCHARS_BYTE, c => (c != cAtEnd))) break;
                len--;
            }
            return System.Text.Encoding.UTF8.GetString(byteStream.Bytes, 0, len);
        }
    }
}