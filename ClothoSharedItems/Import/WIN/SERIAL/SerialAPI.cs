using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClothoSharedItems.Import.WIN.SERIAL
{
    public static class SerialAPI
    {
        // Used to get a handle to the serial port so that we can read/write to it.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(string fileName,
           [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
           [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
           IntPtr securityAttributes,
           [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
           int flags,
           IntPtr template);

        // Used to close the handle to the serial port.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        // Used to get the state of the serial port so that we can configure it.
        [DllImport("kernel32.dll")]
        public static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);

        // Used to configure the serial port.
        [DllImport("kernel32.dll")]
        public static extern bool SetCommState(IntPtr hFile, [In] ref DCB lpDCB);

        // Used to set the connection timeouts on our serial connection.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS lpCommTimeouts);

        // Used to read bytes from the serial connection.
        [DllImport("kernel32.dll")]
        public static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer,
           int nNumberOfBytesToRead, out int lpNumberOfBytesRead, int lpOverlapped);

        // Used to write bytes to the serial connection.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer,
            int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, int lpOverlapped);

        // Used to flush the I/O buffers.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool PurgeComm(IntPtr hFile, int dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushFileBuffers(IntPtr hFile);

        [DllImport("kernel32.dll")]
        public static extern bool SetCommMask(IntPtr hFile, int dwEvtMask);
    }
}