using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static System.Windows.Application;

// ReSharper disable UnusedMember.Global
// ReSharper disable once CheckNamespace

namespace Memory
{
    public class MemoryManage
    {
        #region Native Imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesWritten);
        #endregion

        #region Enums
        public enum AccessMode
        {
            Read = 0x0010,
            Write = 0x0020,
            All = 0x1F0FFF
        }
        #endregion

        private readonly UiControls _ui;
        private readonly string _procExe;
        private readonly AccessMode _accessMode;
        private Process _proc;
        private IntPtr _procHandle;
        private IntPtr _baseAddress;

        public bool ProcessRunning { get; private set; }

        public MemoryManage(MainWindow mainWindow, string procExe, AccessMode accessMode)
        {
            _procExe = procExe;
            _accessMode = accessMode;
            _ui = new UiControls(mainWindow, Current);

            Thread process = new(FindProcess)
            {
                Priority = ThreadPriority.Normal,
            };
            process.Start();
        }

        public void Clean()
        {
            CloseHandle(_procHandle);
        }

        private void ProcessExit(object sender, EventArgs e)
        {
            ProcessRunning = false;
            CheckProcess();
        }

        private void CheckProcess()
        {
            if (!ProcessRunning || _proc == null)
            {
                FindProcess();
            }
        }

        private void FindProcess()
        {
            do
            {
                Process[] processes = Process.GetProcessesByName(_procExe);
                if (processes.Length == 0)
                {
                    ProcessRunning = false;
                    _ui.Status("Please start the process.");
                    Thread.Sleep(500);
                    continue;
                }
                _proc = processes[0];
                ProcessRunning = true;
            } while (!ProcessRunning);

            _procHandle = OpenProcess((int)_accessMode, false, _proc.Id);
            if (_procHandle == IntPtr.Zero)
            {
                _ui.Status("Program couldn't be hooked.");
            }

            if (_proc.StartTime > DateTime.Now - TimeSpan.FromMilliseconds(1000))
            {
                Thread.Sleep(100);
            }

            if (_proc.MainModule == null) return;
            _baseAddress = _proc.MainModule.BaseAddress;
            if (_baseAddress == IntPtr.Zero)
            {
                _ui.Status("Program has access protection.");
            }

            _proc.EnableRaisingEvents = true;
            _proc.Exited += ProcessExit;

            _ui.Status("Running.");
            _ui.Details(
                "Process:      " + _proc.MainModule.ModuleName
                                 + "\nBase Address: " + _baseAddress.ToString("X")
                                 + "\nEntry Point:  " + _proc.MainModule.EntryPointAddress.ToString("X")
            );
        }

        private long TraverseOffsets(List<long> offsets)
        {
            long address = _baseAddress.ToInt64();
            byte[] buffer = new byte[sizeof(ulong)];
            for (int i = 0; i < offsets.Count; i++)
            {
                address += offsets[i];
                if (i == offsets.Count - 1) break;
                ReadProcessMemory(_procHandle, (IntPtr)address, buffer, buffer.Length, out _);
                address = BitConverter.ToInt64(buffer, 0);
            }
            return address;
        }

        #region MemFuncs
        public int ReadInt(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(int)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        public uint ReadUInt(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(uint)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public long ReadInt64(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(long)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToInt64(buffer, 0);
        }

        public ulong ReadUInt64(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public bool WriteInt(List<long> offsets, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public bool WriteInt64(List<long> offsets, long value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public float ReadFloat(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(float)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        public bool WriteFloat(List<long> offsets, float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public double ReadDouble(List<long> offsets)
        {
            byte[] buffer = new byte[sizeof(double)];
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return BitConverter.ToDouble(buffer, 0);
        }

        public bool WriteDouble(List<long> offsets, double value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            long offset = TraverseOffsets(offsets);
            return WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        public string ReadString(List<long> offsets)
        {
            string myString = string.Empty;

            for (ulong i = 1; i < ulong.MaxValue; i++)
            {
                byte[] buffer = new byte[i];
                long offset = TraverseOffsets(offsets);
                ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
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
            return WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }
        #endregion
    }
}