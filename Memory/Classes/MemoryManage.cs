using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleNullReferenceException
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedVariable

// ReSharper disable once CheckNamespace
namespace Memory
{
    public class MemoryManage
    {
        #region Native Imports
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesWritten);
        #endregion

        #region Enums
        public enum AccessMode
        {
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_ALL_ACCESS = 0x1F0FFF
        }
        #endregion

        private readonly IntPtr _processHandle;
        private readonly IntPtr _baseAddress;

        public MemoryManage(MainWindow mainWindow, string procExe, AccessMode accessMode)
        {
            bool message = false;
            Process process = null;
            while (process == null)
            {
                try { process = Process.GetProcessesByName(procExe)[0]; }
                catch
                {
                    if (!message)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => mainWindow.StatusText("Please start the process.")));
                        message = true;
                    }
                    Thread.Sleep(500);
                }
            }

            try
            {
                _processHandle = OpenProcess((int) accessMode, false, process.Id);
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => mainWindow.StatusText("Program couldn't be hooked.")));
            }

            try
            {
                _baseAddress = process.MainModule.BaseAddress;
            }
            catch
            {
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => mainWindow.StatusText("Program has access protection.")));
            }
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    mainWindow.StatusText("Successfully hooked to process.");
                    mainWindow.DetailsText(
                            "Process:      " + process.MainModule.ModuleName
                        + "\nBase Address: " + _baseAddress.ToString("X")
                        + "\nEntry Point:  " + process.MainModule.EntryPointAddress.ToString("X")
                        );
                }));
        }

        private long TraverseOffsets(List<long> offsets)
        {
            long address = _baseAddress.ToInt64();
            byte[] buffer = new byte[sizeof(ulong)];
            for (int i = 0; i < offsets.Count; i++)
            {
                address += offsets[i];
                if (i == offsets.Count - 1) break;
                ReadProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, out _);
                address = BitConverter.ToInt64(buffer, 0);
            }
            return address;
        }

        public long ReadInt(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(int)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            int value = BitConverter.ToInt32(buffer, 0);
            return value;
        }

        public bool WriteInt(List<long> offsets, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public float ReadFloat(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(float)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            float value = BitConverter.ToSingle(buffer, 0);
            return value;
        }

        public bool WriteFloat(List<long> offsets, float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public string ReadString(List<long> offsets)
        {
            string myString = string.Empty;

            for (ulong i = 1; i < ulong.MaxValue; i++)
            {
                byte[] buffer = new byte[i];
                long offset = TraverseOffsets(offsets);
                ReadProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
                if (buffer[(i - 1)] == 0) return myString;
                myString = Encoding.UTF8.GetString(buffer);
            }
            return myString;
        }

        public bool WriteString(List<long> offsets, string value)
        {
            byte[] newString = Encoding.UTF8.GetBytes(value);
            byte[] buffer = new byte[ReadString(offsets).Length];
            Array.Copy(newString, buffer, buffer.Length);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_processHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }
    }
}