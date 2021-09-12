using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable PossibleNullReferenceException
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

        #region Enums
        public enum AccessMode
        {
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_ALL_ACCESS = 0x1F0FFF
        }
        #endregion

        private static IntPtr _processHandle;
        private static IntPtr _baseAddress;

        public MemoryManage(string processName, AccessMode accessMode)
        {
            bool message = false;
            Process process = null;
            while (process == null)
            {
                try { process = Process.GetProcessesByName(processName)[0]; }
                catch
                {
                    if (!message) Interface.Write("Please start the process.", ConsoleColor.Yellow);
                    message = true;
                }
            }

            try { _processHandle = OpenProcess((int)accessMode, false, process.Id); }
            catch { Interface.Failed("Program couldn't be hooked."); }
            try { _baseAddress = process.MainModule.BaseAddress; }
            catch { Interface.Failed("Program has access protection."); }

            Interface.Reset();
            Interface.Write("Successfully hooked to process.", ConsoleColor.Blue);
            Interface.Write(
                            "\nProcess:      " + process.MainModule.ModuleName
                            + "\nPath:         " + Path.GetDirectoryName(process.MainModule.FileName)
                            + "\nBase Address: " + _baseAddress.ToString("X")
                            + "\nEntry Point:  " + process.MainModule.EntryPointAddress.ToString("X")
                              , ConsoleColor.Green
            );
        }

        public long TraverseMemOffsets(List<long> offsets)
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

            for (ulong i = 1; i < ulong.MaxValue; i++)
            {
                var buffer = new byte[i];
                ReadProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out bytesRead);
                if (buffer[(i - 1)] == 0) return myString;
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
