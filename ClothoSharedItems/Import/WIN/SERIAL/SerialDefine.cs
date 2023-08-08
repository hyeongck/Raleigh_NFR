using System.Runtime.InteropServices;

namespace ClothoSharedItems.Import.WIN.SERIAL
{
    public delegate void ConfigureCommState(ref DCB dcb);

    public enum Parity { None, Odd, Even, Mark, Space }

    public enum StopBits { One, OnePointFive, Two }

    /// Defines the control setting for a serial communications device.
    [StructLayout(LayoutKind.Sequential)]
    public struct DCB
    {
        public int DCBlength;
        public uint BaudRate;
        public uint Flags;
        public ushort wReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public sbyte XonChar;
        public sbyte XoffChar;
        public sbyte ErrorChar;
        public sbyte EofChar;
        public sbyte EvtChar;
        public ushort wReserved1;
        public uint fBinary;
        public uint fParity;
        public uint fOutxCtsFlow;
        public uint fOutxDsrFlow;
        public uint fDtrControl;
        public uint fDsrSensitivity;
        public uint fTXContinueOnXoff;
        public uint fOutX;
        public uint fInX;
        public uint fErrorChar;
        public uint fNull;
        public uint fRtsControl;
        public uint fAbortOnError;
    }

    /// Contains the time-out parameters for a communications device.
    [StructLayout(LayoutKind.Sequential)]
    public struct COMMTIMEOUTS
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}