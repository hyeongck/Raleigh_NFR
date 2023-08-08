using System;
using System.Runtime.InteropServices;

namespace ClothoSharedItems.Import.WIN
{
    public static class WinAPI
    {
        #region Hook-Related Function

        // Win32: SetWindowsHookEx()
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(HookType code, HookProc func, IntPtr hInstance, int threadID);

        // Win32: UnhookWindowsHookEx()
        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(IntPtr hhook);

        // Win32: CallNextHookEx()
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(IntPtr hhook, int code, IntPtr wParam, IntPtr lParam);

        #endregion Hook-Related Function

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}