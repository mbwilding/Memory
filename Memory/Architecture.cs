using System;
using System.Runtime.InteropServices;

namespace Memory
{
    internal static class Architecture
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool is32Bit);

        internal static bool Check(IntPtr handle)
        {
            IsWow64Process(handle, out bool is32Bit);
            return is32Bit;
        }
    }
}
