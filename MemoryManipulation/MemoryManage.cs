using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable NotAccessedVariable

namespace MemoryManipulation
{
    internal class MemoryManage
    {
        #region Native Imports
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);
        #endregion

        //private const int PROCESS_VM_READ = 0x0010;
        //private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        private readonly string ProcessName = "AoE2DE_s";
        private readonly Process Process;
        private readonly IntPtr ProcessHandle;
        private readonly IntPtr BaseAddress;

        public List<long> MapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };
        public long MapVisibility;

        public MemoryManage()
        {
            Process = Process.GetProcessesByName(ProcessName)[0];
            ProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, false, Process.Id);
            if (Process.MainModule != null) BaseAddress = Process.MainModule.BaseAddress;

            MapVisibility = TraverseMemOffsets(MapVisibilityOffsets);
        }

        private long TraverseMemOffsets(List<long> offsets)
        {
            long valueCurrent = BaseAddress.ToInt64();
            long valuePrevious = valueCurrent;

            IntPtr bytesRead;
            byte[] buffer = new byte[sizeof(ulong)];

            foreach (var offset in offsets)
            {
                valueCurrent += offset;
                valuePrevious = valueCurrent;
                ReadProcessMemory(ProcessHandle, (IntPtr)valueCurrent, buffer, buffer.Length, out bytesRead);
                valueCurrent = BitConverter.ToInt64(buffer, 0);
            }
            return valuePrevious;
        }

        public long Read(long address)
        {
            IntPtr bytesRead;
            byte[] buffer = new byte[sizeof(ulong)];

            ReadProcessMemory(ProcessHandle, (IntPtr)address, buffer, buffer.Length, out bytesRead);
            var value = BitConverter.ToInt32(buffer, 0);

            return value;
        }

        public void Write(long address, int value)
        {
            IntPtr bytesWritten;
            byte[] buffer = BitConverter.GetBytes(value);

            WriteProcessMemory(ProcessHandle, (IntPtr)address, buffer, buffer.Length, out bytesWritten);
        }
    }
}
