using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesWritten);
        #endregion

        #region Enums
        public enum AccessMode
        {
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_ALL_ACCESS = 0x1F0FFF
        }
        #endregion

        private readonly MainWindow _mainWindow;
        private readonly string _procExe;
        private readonly AccessMode _accessMode;
        private Process _process;
        private IntPtr _processHandle;
        private IntPtr _baseAddress;

        public bool ProcessRunning { get; private set; }

        public MemoryManage(MainWindow mainWindow, string procExe, AccessMode accessMode)
        {
            _procExe = procExe;
            _mainWindow = mainWindow;
            _accessMode = accessMode;

            Thread process = new(FindProcess)
            {
                Priority = ThreadPriority.Normal,
            };
            process.Start();
        }

        private void ProcessExit(object sender, EventArgs e)
        {
            ProcessRunning = false;
            CheckProcess();
        }

        private void CheckProcess()
        {
            if (!ProcessRunning || _process == null)
            {
                FindProcess();
            }
        }

        private void FindProcess()
        {
            do
            {
                try
                {
                    _process = Process.GetProcessesByName(_procExe)[0];
                    if (_process.Responding)
                    {
                        ProcessRunning = true;
                    }
                }
                catch
                {
                    Application.Current.Dispatcher.Invoke(
                        DispatcherPriority.Background,
                        new Action(() =>
                        {
                            _mainWindow.StatusText("Please start the process.");
                            _mainWindow.DetailsText(string.Empty);
                        }));
                    ProcessRunning = false;
                }
                Thread.Sleep(500);
            } while (!ProcessRunning);

            try
            {
                _processHandle = OpenProcess((int)_accessMode, false, _process.Id);
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Background,
                    new Action(() => _mainWindow.StatusText("Program couldn't be hooked.")));
            }

            try
            {
                _baseAddress = _process.MainModule.BaseAddress;
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Background,
                    new Action(() => _mainWindow.StatusText("Program has access protection.")));
            }
            try
            {
                _process.EnableRaisingEvents = true;
                _process.Exited += ProcessExit;
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Background,
                    new Action(() => _mainWindow.StatusText("Error assigning exit handler.")));
            }

            Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    _mainWindow.StatusText("Running.");
                    _mainWindow.DetailsText(
                                             "Process:      " + _process.MainModule.ModuleName
                                         + "\nBase Address: " + _baseAddress.ToString("X")
                                         + "\nEntry Point:  " + _process.MainModule.EntryPointAddress.ToString("X")
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

        #region Public
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
        #endregion
    }
}