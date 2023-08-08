using System.Runtime.InteropServices;
using System.Text;

namespace ClothoSharedItems.Import.VISA
{
    public partial class VIsa
    {
        public static object scpiLock = new object();
        public const short VI_FIND_BUFLEN = 256;
        public const short VI_NULL = 0;
        public const short VI_TRUE = 1;
        public const short VI_FALSE = 0;
        public const short VI_INTF_GPIB = 1;
        public const short VI_INTF_VXI = 2;
        public const short VI_INTF_GPIB_VXI = 3;
        public const short VI_INTF_ASRL = 4;
        public const short VI_INTF_TCPIP = 6;
        public const short VI_INTF_USB = 7;
        public const short VI_PROT_NORMAL = 1;
        public const short VI_PROT_FDC = 2;
        public const short VI_PROT_HS488 = 3;
        public const short VI_PROT_4882_STRS = 4;
        public const short VI_PROT_USBTMC_VENDOR = 5;
        public const short VI_FDC_NORMAL = 1;
        public const short VI_FDC_STREAM = 2;
        public const short VI_LOCAL_SPACE = 0;
        public const short VI_A16_SPACE = 1;
        public const short VI_A24_SPACE = 2;
        public const short VI_A32_SPACE = 3;
        public const short VI_OPAQUE_SPACE = -1;
        public const short VI_UNKNOWN_LA = -1;
        public const short VI_UNKNOWN_SLOT = -1;
        public const short VI_UNKNOWN_LEVEL = -1;
        public const short VI_QUEUE = 1;
        public const short VI_HNDLR = 2;
        public const short VI_SUSPEND_HNDLR = 4;
        public const short VI_ALL_MECH = -1;
        public const short VI_TRIG_ALL = -2;
        public const short VI_TRIG_SW = -1;
        public const short VI_TRIG_TTL0 = 0;
        public const short VI_TRIG_TTL1 = 1;
        public const short VI_TRIG_TTL2 = 2;
        public const short VI_TRIG_TTL3 = 3;
        public const short VI_TRIG_TTL4 = 4;
        public const short VI_TRIG_TTL5 = 5;
        public const short VI_TRIG_TTL6 = 6;
        public const short VI_TRIG_TTL7 = 7;
        public const short VI_TRIG_ECL0 = 8;
        public const short VI_TRIG_ECL1 = 9;
        public const short VI_TRIG_PANEL_IN = 27;
        public const short VI_TRIG_PANEL_OUT = 28;
        public const short VI_TRIG_PROT_DEFAULT = 0;
        public const short VI_TRIG_PROT_ON = 1;
        public const short VI_TRIG_PROT_OFF = 2;
        public const short VI_TRIG_PROT_SYNC = 5;
        public const short VI_READ_BUF = 1;
        public const short VI_WRITE_BUF = 2;
        public const short VI_READ_BUF_DISCARD = 4;
        public const short VI_WRITE_BUF_DISCARD = 8;
        public const short VI_IO_IN_BUF = 16;
        public const short VI_IO_OUT_BUF = 32;
        public const short VI_IO_IN_BUF_DISCARD = 64;
        public const short VI_IO_OUT_BUF_DISCARD = 128;
        public const short VI_FLUSH_ON_ACCESS = 1;
        public const short VI_FLUSH_WHEN_FULL = 2;
        public const short VI_FLUSH_DISABLE = 3;
        public const short VI_NMAPPED = 1;
        public const short VI_USE_OPERS = 2;
        public const short VI_DEREF_ADDR = 3;
        public const int VI_TMO_IMMEDIATE = 0;
        public const int VI_TMO_INFINITE = -1;
        public const short VI_NO_LOCK = 0;
        public const short VI_EXCLUSIVE_LOCK = 1;
        public const short VI_SHARED_LOCK = 2;
        public const short VI_LOAD_CONFIG = 4;
        public const short VI_NO_SEC_ADDR = -1;
        public const short VI_ASRL_PAR_NONE = 0;
        public const short VI_ASRL_PAR_ODD = 1;
        public const short VI_ASRL_PAR_EVEN = 2;
        public const short VI_ASRL_PAR_MARK = 3;
        public const short VI_ASRL_PAR_SPACE = 4;
        public const short VI_ASRL_STOP_ONE = 10;
        public const short VI_ASRL_STOP_ONE5 = 15;
        public const short VI_ASRL_STOP_TWO = 20;
        public const short VI_ASRL_FLOW_NONE = 0;
        public const short VI_ASRL_FLOW_XON_XOFF = 1;
        public const short VI_ASRL_FLOW_RTS_CTS = 2;
        public const short VI_ASRL_FLOW_DTR_DSR = 4;
        public const short VI_ASRL_END_NONE = 0;
        public const short VI_ASRL_END_LAST_BIT = 1;
        public const short VI_ASRL_END_TERMCHAR = 2;
        public const short VI_ASRL_END_BREAK = 3;
        public const short VI_STATE_ASSERTED = 1;
        public const short VI_STATE_UNASSERTED = 0;
        public const short VI_STATE_UNKNOWN = -1;
        public const short VI_BIG_ENDIAN = 0;
        public const short VI_LITTLE_ENDIAN = 1;
        public const short VI_DATA_PRIV = 0;
        public const short VI_DATA_NPRIV = 1;
        public const short VI_PROG_PRIV = 2;
        public const short VI_PROG_NPRIV = 3;
        public const short VI_BLCK_PRIV = 4;
        public const short VI_BLCK_NPRIV = 5;
        public const short VI_D64_PRIV = 6;
        public const short VI_D64_NPRIV = 7;
        public const short VI_WIDTH_8 = 1;
        public const short VI_WIDTH_16 = 2;
        public const short VI_WIDTH_32 = 4;
        public const short VI_GPIB_REN_DEASSERT = 0;
        public const short VI_GPIB_REN_ASSERT = 1;
        public const short VI_GPIB_REN_DEASSERT_GTL = 2;
        public const short VI_GPIB_REN_ASSERT_ADDRESS = 3;
        public const short VI_GPIB_REN_ASSERT_LLO = 4;
        public const short VI_GPIB_REN_ASSERT_ADDRESS_LLO = 5;
        public const short VI_GPIB_REN_ADDRESS_GTL = 6;
        public const short VI_GPIB_ATN_DEASSERT = 0;
        public const short VI_GPIB_ATN_ASSERT = 1;
        public const short VI_GPIB_ATN_DEASSERT_HANDSHAKE = 2;
        public const short VI_GPIB_ATN_ASSERT_IMMEDIATE = 3;
        public const short VI_GPIB_HS488_DISABLED = 0;
        public const short VI_GPIB_HS488_NIMPL = -1;
        public const short VI_GPIB_UNADDRESSED = 0;
        public const short VI_GPIB_TALKER = 1;
        public const short VI_GPIB_LISTENER = 2;
        public const short VI_VXI_CMD16 = 512;
        public const short VI_VXI_CMD16_RESP16 = 514;
        public const short VI_VXI_RESP16 = 2;
        public const short VI_VXI_CMD32 = 1024;
        public const short VI_VXI_CMD32_RESP16 = 1026;
        public const short VI_VXI_CMD32_RESP32 = 1028;
        public const short VI_VXI_RESP32 = 4;
        public const short VI_ASSERT_SIGNAL = -1;
        public const short VI_ASSERT_USE_ASSIGNED = 0;
        public const short VI_ASSERT_IRQ1 = 1;
        public const short VI_ASSERT_IRQ2 = 2;
        public const short VI_ASSERT_IRQ3 = 3;
        public const short VI_ASSERT_IRQ4 = 4;
        public const short VI_ASSERT_IRQ5 = 5;
        public const short VI_ASSERT_IRQ6 = 6;
        public const short VI_ASSERT_IRQ7 = 7;
        public const short VI_UTIL_ASSERT_SYSRESET = 1;
        public const short VI_UTIL_ASSERT_SYSFAIL = 2;
        public const short VI_UTIL_DEASSERT_SYSFAIL = 3;
        public const short VI_VXI_CLASS_MEMORY = 0;
        public const short VI_VXI_CLASS_EXTENDED = 1;
        public const short VI_VXI_CLASS_MESSAGE = 2;
        public const short VI_VXI_CLASS_REGISTER = 3;
        public const short VI_VXI_CLASS_OTHER = 4;
        public const int VI_ERROR_INV_SESSION = -1073807346;
        public const int VI_INFINITE = -1;
        public const short VI_NORMAL = 1;
        public const short VI_FDC = 2;
        public const short VI_HS488 = 3;
        public const short VI_ASRL488 = 4;
        public const short VI_ASRL_IN_BUF = 16;
        public const short VI_ASRL_OUT_BUF = 32;
        public const short VI_ASRL_IN_BUF_DISCARD = 64;
        public const short VI_ASRL_OUT_BUF_DISCARD = 128;
        public const int VI_SPEC_VERSION = 4194304;
        private const string visaDll = "visa32.dll";

        [DllImport(visaDll, EntryPoint = "#141", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viOpenDefaultRM(ref int sesn);

        [DllImport(visaDll, EntryPoint = "#128", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetDefaultRM(ref int sesn);

        [DllImport(visaDll, EntryPoint = "#129", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viFindRsrc(
          int sesn,
          string expr,
          ref int vi,
          ref int retCount,
          StringBuilder desc);

        [DllImport(visaDll, EntryPoint = "#130", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viFindNext(int vi, StringBuilder desc);

        [DllImport(visaDll, EntryPoint = "#146", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viParseRsrc(
          int sesn,
          string desc,
          ref short intfType,
          ref short intfNum);

        [DllImport(visaDll, EntryPoint = "#147", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viParseRsrcEx(
          int sesn,
          string desc,
          ref short intfType,
          ref short intfNum,
          StringBuilder rsrcClass,
          StringBuilder expandedUnaliasedName,
          StringBuilder aliasIfExists);

        [DllImport(visaDll, EntryPoint = "#131", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viOpen(
          int sesn,
          string viDesc,
          int mode,
          int timeout,
          ref int vi);

        [DllImport(visaDll, EntryPoint = "#132", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viClose(int vi);

        [DllImport(visaDll, EntryPoint = "#133", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetAttribute(
          int vi,
          ViAttr attrName,
          ref byte attrValue);

        [DllImport(visaDll, EntryPoint = "#133", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetAttribute(
          int vi,
          ViAttr attrName,
          ref short attrValue);

        [DllImport(visaDll, EntryPoint = "#133", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetAttribute(
          int vi,
          ViAttr attrName,
          ref int attrValue);

        [DllImport(visaDll, EntryPoint = "#133", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetAttribute(
          int vi,
          ViAttr attrName,
          ref long attrValue);

        [DllImport(visaDll, EntryPoint = "#133", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGetAttribute(
          int vi,
          ViAttr attrName,
          StringBuilder attrValue);

        [DllImport(visaDll, EntryPoint = "#134", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viSetAttribute(int vi, ViAttr attrName, byte attrValue);

        [DllImport(visaDll, EntryPoint = "#134", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viSetAttribute(int vi, ViAttr attrName, short attrValue);

        [DllImport(visaDll, EntryPoint = "#134", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viSetAttribute(int vi, ViAttr attrName, int attrValue);

        [DllImport(visaDll, EntryPoint = "#142", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viStatusDesc(int vi, int status, StringBuilder desc);

        [DllImport(visaDll, EntryPoint = "#143", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int viTerminate(int vi, short degree, int jobId);

        [DllImport(visaDll, EntryPoint = "#144", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viLock(
          int vi,
          int lockType,
          int timeout,
          string requestedKey,
          StringBuilder accessKey);

        [DllImport(visaDll, EntryPoint = "#145", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUnlock(int vi);

        [DllImport(visaDll, EntryPoint = "#135", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viEnableEvent(
          int vi,
          ViEventType eventType,
          short mechanism,
          int context);

        [DllImport(visaDll, EntryPoint = "#136", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viDisableEvent(
          int vi,
          ViEventType eventType,
          short mechanism);

        [DllImport(visaDll, EntryPoint = "#137", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viDiscardEvents(
          int vi,
          ViEventType eventType,
          short mechanism);

        [DllImport(visaDll, EntryPoint = "#138", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viWaitOnEvent(
          int vi,
          ViEventType inEventType,
          int timeout,
          out ViEventType outEventType,
          out int outEventContext);

        [DllImport(visaDll, EntryPoint = "#139", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viInstallHandler(
          int vi,
          ViEventType inEventType,
          VIsa.ViEventHandler handler,
          int userhandle);

        [DllImport(visaDll, EntryPoint = "#140", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUninstallHandler(
          int vi,
          ViEventType inEventType,
          VIsa.ViEventHandler handler,
          int userhandle);

        [DllImport(visaDll, EntryPoint = "#256", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viReadByte(
          int vi,
          byte[] buffer,
          int count,
          ref int retCount);

        [DllImport(visaDll, EntryPoint = "#256", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int viRead(int vi, StringBuilder readBuffer, uint cnt, out uint retCnt);

        [DllImport(visaDll, EntryPoint = "#256", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viRead(
          int vi,
          StringBuilder readBuffer,
          int cnt,
          ref int retCnt);

        [DllImport(visaDll, EntryPoint = "#277", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viReadAsync(
          int vi,
          byte[] buffer,
          int count,
          ref int jobId);

        [DllImport(visaDll, EntryPoint = "#219", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viReadToFile(
          int vi,
          string filename,
          uint count,
          ref uint retCount);

        [DllImport(visaDll, EntryPoint = "#257", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viWriteByte(
          int vi,
          byte[] buffer,
          int count,
          ref int retCount);

        [DllImport(visaDll, EntryPoint = "#257", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viWrite(int vi, string buf, int cnt, ref int retCnt);

        [DllImport(visaDll, EntryPoint = "#278", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viWriteAsync(
          int vi,
          byte[] buffer,
          int count,
          ref int jobId);

        [DllImport(visaDll, EntryPoint = "#218", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viWriteFromFile(
          int vi,
          string filename,
          uint count,
          ref uint retCount);

        [DllImport(visaDll, EntryPoint = "#258", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viAssertTrigger(int vi, short protocol);

        [DllImport(visaDll, EntryPoint = "#259", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viReadSTB(int vi, ref short status);

        [DllImport(visaDll, EntryPoint = "#260", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viClear(int vi);

        [DllImport(visaDll, EntryPoint = "#267", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viSetBuf(int vi, short mask, int bufSize);

        [DllImport(visaDll, EntryPoint = "#268", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viFlush(int vi, short mask);

        [DllImport(visaDll, EntryPoint = "#202", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viBufWrite(
          int vi,
          byte[] buffer,
          int count,
          ref int retCount);

        [DllImport(visaDll, EntryPoint = "#203", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viBufRead(
          int vi,
          byte[] buffer,
          int count,
          ref int retCount);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, int[] arr);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, short[] arr);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, float[] arr);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, double[] arr);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, byte[] arr);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, string arg);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, int arg);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, short arg);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, double arg);

        [DllImport(visaDll, EntryPoint = "#269", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viPrintf(int vi, string writeFmt, byte arg);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(int vi, StringBuilder buffer, string writeFmt);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          int[] arr);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          short[] arr);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          float[] arr);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          double[] arr);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          byte[] arr);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          string arg);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          int arg);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          short arg);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          double arg);

        [DllImport(visaDll, EntryPoint = "#204", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSPrintf(
          int vi,
          StringBuilder buffer,
          string writeFmt,
          byte arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, int[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, short[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, float[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, double[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, byte[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int count,
          int[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int count,
          short[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int count,
          float[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int count,
          double[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int count,
          byte[] arr);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, StringBuilder arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(
          int vi,
          string readFmt,
          ref int stringSize,
          StringBuilder arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, ref int arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, ref short arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, ref float arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, ref double arg);

        [DllImport(visaDll, EntryPoint = "#271", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viScanf(int vi, string readFmt, ref byte arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(int vi, string buffer, string readFmt);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          int[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          short[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          float[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          double[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          byte[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int count,
          int[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int count,
          short[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int count,
          float[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int count,
          double[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int count,
          byte[] arr);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          StringBuilder arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int stringSize,
          StringBuilder arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref int arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref short arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref float arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref double arg);

        [DllImport(visaDll, EntryPoint = "#206", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern ViStatus viSScanf(
          int vi,
          string buffer,
          string readFmt,
          ref byte arg);

        [DllImport(visaDll, EntryPoint = "#273", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viIn8(int vi, short accSpace, int offset, ref byte val8);

        [DllImport(visaDll, EntryPoint = "#274", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viOut8(int vi, short accSpace, int offset, byte val8);

        [DllImport(visaDll, EntryPoint = "#261", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viIn16(
          int vi,
          short accSpace,
          int offset,
          ref short val16);

        [DllImport(visaDll, EntryPoint = "#262", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viOut16(int vi, short accSpace, int offset, short val16);

        [DllImport(visaDll, EntryPoint = "#281", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viIn32(
          int vi,
          short accSpace,
          int offset,
          ref int val32);

        [DllImport(visaDll, EntryPoint = "#282", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viOut32(int vi, short accSpace, int offset, int val32);

        [DllImport(visaDll, EntryPoint = "#283", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveIn8(
          int vi,
          short accSpace,
          int offset,
          int length,
          byte[] buf8);

        [DllImport(visaDll, EntryPoint = "#284", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveOut8(
          int vi,
          short accSpace,
          int offset,
          int length,
          byte[] buf8);

        [DllImport(visaDll, EntryPoint = "#285", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveIn16(
          int vi,
          short accSpace,
          int offset,
          int length,
          short[] buf16);

        [DllImport(visaDll, EntryPoint = "#286", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveOut16(
          int vi,
          short accSpace,
          int offset,
          int length,
          short[] buf16);

        [DllImport(visaDll, EntryPoint = "#287", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveIn32(
          int vi,
          short accSpace,
          int offset,
          int length,
          int[] buf32);

        [DllImport(visaDll, EntryPoint = "#288", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveOut32(
          int vi,
          short accSpace,
          int offset,
          int length,
          int[] buf32);

        [DllImport(visaDll, EntryPoint = "#200", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMove(
          int vi,
          short srcSpace,
          int srcOffset,
          short srcWidth,
          short destSpace,
          int destOffset,
          short destWidth,
          int srcLength);

        [DllImport(visaDll, EntryPoint = "#201", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMoveAsync(
          int vi,
          short srcSpace,
          int srcOffset,
          short srcWidth,
          short destSpace,
          int destOffset,
          short destWidth,
          int srcLength,
          ref int jobId);

        [DllImport(visaDll, EntryPoint = "#263", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMapAddress(
          int vi,
          short mapSpace,
          int mapOffset,
          int mapSize,
          short accMode,
          int suggested,
          ref int address);

        [DllImport(visaDll, EntryPoint = "#264", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUnmapAddress(int vi);

        [DllImport(visaDll, EntryPoint = "#275", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPeek8(int vi, int address, ref byte val8);

        [DllImport(visaDll, EntryPoint = "#276", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPoke8(int vi, int address, byte val8);

        [DllImport(visaDll, EntryPoint = "#265", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPeek16(int vi, int address, ref short val16);

        [DllImport(visaDll, EntryPoint = "#266", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPoke16(int vi, int address, short val16);

        [DllImport(visaDll, EntryPoint = "#289", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPeek32(int vi, int address, ref int val32);

        [DllImport(visaDll, EntryPoint = "#290", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void viPoke32(int vi, int address, int val32);

        [DllImport(visaDll, EntryPoint = "#291", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMemAlloc(int vi, int memSize, ref int offset);

        [DllImport(visaDll, EntryPoint = "#292", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMemFree(int vi, int offset);

        [DllImport(visaDll, EntryPoint = "#208", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGpibControlREN(int vi, short mode);

        [DllImport(visaDll, EntryPoint = "#210", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGpibControlATN(int vi, short mode);

        [DllImport(visaDll, EntryPoint = "#211", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGpibSendIFC(int vi);

        [DllImport(visaDll, EntryPoint = "#212", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGpibCommand(
          int vi,
          string buffer,
          int count,
          ref int retCount);

        [DllImport(visaDll, EntryPoint = "#213", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viGpibPassControl(int vi, short primAddr, short secAddr);

        [DllImport(visaDll, EntryPoint = "#209", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viVxiCommandQuery(
          int vi,
          short mode,
          int devCmd,
          ref int devResponse);

        [DllImport(visaDll, EntryPoint = "#214", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viAssertUtilSignal(int vi, short line);

        [DllImport(visaDll, EntryPoint = "#215", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viAssertIntrSignal(int vi, short mode, int statusID);

        [DllImport(visaDll, EntryPoint = "#216", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viMapTrigger(
          int vi,
          short trigSrc,
          short trigDest,
          short mode);

        [DllImport(visaDll, EntryPoint = "#217", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUnmapTrigger(int vi, short trigSrc, short trigDest);

        [DllImport(visaDll, EntryPoint = "#293", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUsbControlOut(
          int vi,
          short bmRequestType,
          short bRequest,
          short wValue,
          short wIndex,
          short wLength,
          byte[] buf);

        [DllImport(visaDll, EntryPoint = "#294", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ViStatus viUsbControlIn(
          int vi,
          short bmRequestType,
          short bRequest,
          short wValue,
          short wIndex,
          short wLength,
          byte[] buf,
          ref short retCnt);
    }
}