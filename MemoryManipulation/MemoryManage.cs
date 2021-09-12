using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable InconsistentNaming
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

        //private const string _processName = "AoE2DE_s";
        private const string _processName = "BinaryDifference";
        private readonly IntPtr _processHandle;
        private readonly IntPtr _baseAddress;

        private readonly List<long> MapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };
        public long MapVisibility;
        private readonly List<long> ToolTipSizeOffsets = new() { 0x034FFB70, 0x90, 0xC48, 0x240 };
        public long ToolTipSize;

        private readonly List<long> BDOffset = new() { 0x0001F488, 0x40, 0x30, 0x10, 0x50, 0xA0, 0x0, 0x5E };
        public long BD;
            

        public MemoryManage()
        {
            var process = Process.GetProcessesByName(_processName)[0];
            _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
            if (process.MainModule != null) _baseAddress = process.MainModule.BaseAddress;

            //MapVisibility = TraverseMemOffsets(MapVisibilityOffsets);
            //ToolTipSize = TraverseMemOffsets(ToolTipSizeOffsets);
            BD = TraverseMemOffsets(BDOffset);
        }

        private long TraverseMemOffsets(List<long> offsets)
        {
            long valueCurrent = _baseAddress.ToInt64();
            long valuePrevious = valueCurrent;

            IntPtr bytesRead;
            byte[] buffer = new byte[sizeof(ulong)];

            foreach (var offset in offsets)
            {
                valueCurrent += offset;
                valuePrevious = valueCurrent;
                ReadProcessMemory(_processHandle, (IntPtr)valueCurrent, buffer, buffer.Length, out bytesRead);
                valueCurrent = BitConverter.ToInt64(buffer, 0);
            }
            return valuePrevious;
        }

        public long ReadInt(long address)
        {
            IntPtr bytesRead;
            byte[] buffer = new byte[sizeof(int)];

            ReadProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesRead);
            var value = BitConverter.ToInt32(buffer, 0);

            return value;
        }

        public bool WriteInt(long address, int value)
        {
            IntPtr bytesWritten;
            byte[] buffer = BitConverter.GetBytes(value);

            return WriteProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesWritten);
        }

        public float ReadFloat(long address)
        {
            IntPtr bytesRead;
            byte[] buffer = new byte[sizeof(float)];

            ReadProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesRead);
            var value = BitConverter.ToSingle(buffer, 0);

            return value;
        }

        public bool WriteFloat(long address, float value)
        {
            IntPtr bytesWritten;
            byte[] buffer = BitConverter.GetBytes(value);

            return WriteProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesWritten);
        }

        public string ReadString(long address)
        {
            IntPtr bytesRead = IntPtr.Zero;
            var myString = string.Empty;

            for (int i = 1; i < 50; i++)
            {
                var buffer = new byte[i];
                ReadProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesRead);
                if (buffer[(i - 1)] == 0)
                {
                    return myString;
                }
                myString = Encoding.UTF8.GetString(buffer);
            }
            return myString;
        }

        public bool WriteString(long address, string value)
        {
            IntPtr bytesWritten;
            byte[] newString = Encoding.UTF8.GetBytes(value);
            byte[] buffer = new byte[ReadString(address).Length];

            Array.Copy(newString, buffer, buffer.Length);

            return WriteProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesWritten);
        }
    }
}
