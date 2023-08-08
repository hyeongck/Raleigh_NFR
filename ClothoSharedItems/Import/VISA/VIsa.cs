using ClothoSharedItems.Common;
using ClothoSharedItems.Import.GPIB;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClothoSharedItems.Import.VISA
{
    public partial class VIsa
    {
        private DevType currentVisa = DevType.NONE;
        private int sesn = 0;
        private Trace TRACE = (Trace)null;

        public VIsa(Trace trace = null)
        {
            this.TRACE = trace;
        }

        public bool Close(int vi)
        {
            try
            {
                if (vi >= 0)
                {
                    viClose(vi);
                    viClose(sesn);
                }
            }
            catch
            {
            }
            return true;
        }

        public void Clear(int vi)
        {
            if (vi >= 0)
            {
                viClear(vi);
            }
        }

        public bool Open(string resourceName, int timeout, ref int vi)
        {
            try
            {
                StringBuilder rsrcClass = new StringBuilder();
                vi = 0;
                if (timeout == 0) timeout = 30000;

                if (resourceName.CIvStartsWith("GPIB")) currentVisa = DevType.GPIB;
                else if (resourceName.CIvStartsWith("TCPIP")) currentVisa = DevType.TCPIP_INSTR;

                if (viOpenDefaultRM(ref sesn) < ViStatus.VI_SUCCESS)
                {
                    viClose(sesn);
                    return false;
                }

                if (viOpen(sesn, resourceName, 0, 0, ref vi) < ViStatus.VI_SUCCESS)
                {
                    viClose(sesn);
                    return false;
                }

                viGetAttribute(vi, ViAttr.VI_ATTR_RSRC_MANF_NAME, new StringBuilder());     // VISA Name
                viGetAttribute(vi, ViAttr.VI_ATTR_INTF_INST_NAME, new StringBuilder());     // TCPIP
                viGetAttribute(vi, ViAttr.VI_ATTR_RSRC_CLASS, rsrcClass);                   //
                viGetAttribute(vi, ViAttr.VI_ATTR_RSRC_NAME, new StringBuilder());

                if (resourceName.StartsWith("TCPIP"))
                {
                    viGetAttribute(vi, ViAttr.VI_ATTR_TCPIP_ADDR, new StringBuilder());
                    viGetAttribute(vi, ViAttr.VI_ATTR_TCPIP_HOSTNAME, new StringBuilder());
                    viSetAttribute(vi, ViAttr.VI_ATTR_TCPIP_NODELAY, (short)1);
                    viSetAttribute(vi, ViAttr.VI_ATTR_TCPIP_KEEPALIVE, (short)1);
                }

                viSetAttribute(vi, ViAttr.VI_ATTR_TMO_VALUE, timeout);
                if (rsrcClass.ToString().CIvEquals("SOCKET"))
                {
                    if (currentVisa == DevType.TCPIP_INSTR) currentVisa = DevType.TCPIP_SOCKET;

                    viGetAttribute(vi, ViAttr.VI_ATTR_TCPIP_PORT, new StringBuilder());
                    viSetAttribute(vi, ViAttr.VI_ATTR_TERMCHAR, 0xA);
                    viSetAttribute(vi, ViAttr.VI_ATTR_TERMCHAR_EN, (short)1);
                    viSetAttribute(vi, ViAttr.VI_ATTR_SUPPRESS_END_EN, (short)1);
                    viSetAttribute(vi, ViAttr.VI_ATTR_IO_PROT, (short)4);
                }
                else
                    viSetAttribute(vi, ViAttr.VI_ATTR_TERMCHAR_EN, (short)0);

                viSetAttribute(vi, ViAttr.VI_ATTR_SEND_END_EN, (short)1);
                viSetAttribute(vi, ViAttr.VI_ATTR_WR_BUF_OPER_MODE, (short)1);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool FindVISAResource(ref int vi, ref int retCount, ref List<string> rmList, string pattern)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                viOpenDefaultRM(ref sesn);
                var status = viFindRsrc(sesn, pattern, ref vi, ref retCount, builder);

                if (status == ViStatus.VI_SUCCESS && retCount > 0)
                {
                    rmList.Add(builder.ToString());

                    for (int i = 1; i < retCount; i++)
                    {
                        viFindNext(vi, builder);
                        rmList.Add(builder.ToString());
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Close(vi);
            }
        }

        public bool Write(int vi, string scpiCommand, bool isQuery = false)
        {
            lock (scpiLock)
            {
                try
                {
                    int retCnt = 0;

                    if (this.TRACE != null)
                        this.TRACE.Message(string.Format("VISA.{0}(\"{1}\")", isQuery ? "ask" : "write", scpiCommand.Replace("\n", "").Replace("\r", "")));

                    if (currentVisa == DevType.TCPIP_SOCKET)
                        scpiCommand = scpiCommand.CIvEndsWith("\n") ? scpiCommand : scpiCommand + "\n";

                    if (viWrite(vi, scpiCommand, scpiCommand.Length, ref retCnt) == ViStatus.VI_SUCCESS)
                        return true;
                }
                catch { }
                finally { }
                return false;
            }
        }

        public bool Read(int vi, ref string scpiReturn)
        {
            var status = ViStatus.VI_SUCCESS;
            StringBuilder stringBuilder = new StringBuilder(BUFFER_SIZE * 500);
            int i = 0;

            lock (scpiLock)
            {
                try
                {
                    int retCnt = 0;

                    DateTime dtStart = DateTime.Now;
                    do
                    {
                        StringBuilder readBuffer = new StringBuilder(BUFFER_SIZE);
                        status = viRead(vi, readBuffer, BUFFER_SIZE, ref retCnt);
                        stringBuilder.Append(readBuffer.ToString(0, retCnt));

                        if (status < ViStatus.VI_SUCCESS)
                        {
                            scpiReturn = status.ToString();
                            return false;
                        }

                        if (status == ViStatus.VI_SUCCESS || status == ViStatus.VI_SUCCESS_TERM_CHAR)
                        {
                            scpiReturn = stringBuilder.ToString(0, BUFFER_SIZE * i + retCnt).Replace("\n", "").Replace("\r", "");
                            return true;
                        }
                        i++;
                    } while ((DateTime.Now - dtStart).TotalSeconds <= 30);

                    //if (this.CheckError(vi))
                    return true;
                }
                catch
                {
                }
                finally
                {
                    if (this.TRACE != null)
                        this.TRACE.Message(string.Format("# {0}", scpiReturn));
                }
                return false;
            }
        }

        private const int BUFFER_SIZE = 1024;
        private byte[] m_byteBuffer = new byte[BUFFER_SIZE];

        public bool ReadByte(int vi, ref byte[] scpiReturn, double timeout = 30, bool isRetryOnError = false)
        {
            ByteStream tempStream = new ByteStream(BUFFER_SIZE * 500);
            var status = ViStatus.VI_SUCCESS;
            int i = 0;

            lock (scpiLock)
            {
                try
                {
                    int retCnt = 0;

                    DateTime dtStart = DateTime.Now;
                    do
                    {
                        status = viReadByte(vi, m_byteBuffer, BUFFER_SIZE, ref retCnt);

                        tempStream.WriteBytes((i++) * m_byteBuffer.Length, m_byteBuffer, retCnt);

                        if (status == ViStatus.VI_SUCCESS || status == ViStatus.VI_SUCCESS_TERM_CHAR)
                        {
                            scpiReturn = tempStream.Bytes;
                            return true;
                        }
                    } while ((DateTime.Now - dtStart).TotalSeconds <= timeout);
                }
                catch
                {
                }
                finally
                {
                    if (this.TRACE != null)
                        this.TRACE.Message(string.Format("# {0}", scpiReturn.JoinToString(",")));
                }
                return false;
            }
        }

        public bool Query(int vi, string scpiCommand, ref string scpiReturn)
        {
            lock (scpiLock)
            {
                try
                {
                    if (!Write(vi, scpiCommand, true)) return false;
                    if (Read(vi, ref scpiReturn)) return true;
                }
                catch
                {
                }
                finally
                {
                }
                return false;
            }
        }

        private bool CheckError(int vi)
        {
            lock (scpiLock)
            {
                try
                {
                    int retCnt = 0;
                    string buf = "SYST:ERR?";
                    StringBuilder readBuffer = new StringBuilder(1024);
                    viWrite(vi, buf, buf.Length, ref retCnt);
                    viRead(vi, readBuffer, 200, ref retCnt);
                    readBuffer.Length = retCnt;
                    if (this.TRACE != null)
                        this.TRACE.Message(string.Format("# Check Error: {0}", readBuffer.ToString().Replace("\n", "").Replace("\r", "")));
                    return readBuffer.ToString().CIvContains("No error");
                }
                catch
                {
                }
                finally
                {
                }
                return false;
            }
        }

        public bool SetTimeout(int vi, GpibTimeout tmo)
        {
            int timeout = 0;
            switch (tmo)
            {
                case GpibTimeout.None:
                    timeout = 0;
                    break;

                case GpibTimeout.T10us:
                case GpibTimeout.T30us:
                case GpibTimeout.T100us:
                case GpibTimeout.T300us:
                case GpibTimeout.T1ms:
                    timeout = 1;
                    break;

                case GpibTimeout.T3ms:
                    timeout = 3;
                    break;

                case GpibTimeout.T10ms:
                    timeout = 10;
                    break;

                case GpibTimeout.T30ms:
                    timeout = 30;
                    break;

                case GpibTimeout.T100ms:
                    timeout = 100;
                    break;

                case GpibTimeout.T300ms:
                    timeout = 300;
                    break;

                case GpibTimeout.T1s:
                    timeout = 1000;
                    break;

                case GpibTimeout.T3s:
                    timeout = 3000;
                    break;

                case GpibTimeout.T10s:
                    timeout = 10000;
                    break;

                case GpibTimeout.T30s:
                    timeout = 30000;
                    break;

                case GpibTimeout.T100s:
                    timeout = 100000;
                    break;

                case GpibTimeout.T300s:
                    timeout = 300000;
                    break;

                case GpibTimeout.T1000s:
                    timeout = 1000000;
                    break;
            }
            ViStatus vs = viSetAttribute(vi, ViAttr.VI_ATTR_TMO_VALUE, timeout);
            if (vs == ViStatus.VI_SUCCESS) return true;
            else return false;
        }

        public void GotoLocal(int vi)
        {
            viGpibControlREN(vi, (short)6);
        }

        public byte GetSBR(int vi)
        {
            string statusByte = "";
            if (Query(vi, "*STB?", ref statusByte))
                return statusByte.ToByte();
            else
                return 0;
        }

        public delegate ViStatus ViHndlr(
          int ViSession,
          int ViEventType,
          int ViEvent,
          IntPtr ViAddr);

        public delegate ViStatus ViEventHandler(
          int vi,
          ViEventType inEventType,
          int inContext,
          int inUserHandle);
    }
}