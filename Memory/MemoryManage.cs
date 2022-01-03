using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool is32Bit);

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
        private bool _is32Bit;
        public bool AppRunning = true;

        public bool ProcessRunning { get; private set; }

        public MemoryManage(MainWindow mainWindow, string procExe, AccessMode accessMode)
        {
            _procExe = procExe;
            _accessMode = accessMode;
            _ui = new UiControls(mainWindow, Current);

            if (Current.MainWindow != null) Current.MainWindow.Closed += MainWindowOnClosed;

            Task.Run(FindProcess);
        }

        // Finalizes threads when closed
        private void MainWindowOnClosed(object sender, EventArgs e)
        {
            AppRunning = false;
        }

        public void Clean()
        {
            CloseHandle(_procHandle);
        }

        private void ProcessExit(object sender, EventArgs e)
        {
            ProcessRunning = false;
            _ui.Details(string.Empty);
            Task.Run(FindProcess);
        }

        private Task FindProcess()
        {
            do
            {
                if (!AppRunning) return Task.CompletedTask;

                Process[] processes = Process.GetProcessesByName(_procExe);
                if (processes.Length == 0)
                {
                    ProcessRunning = false;
                    _ui.Status("Please start the process.");
                    Task.Delay(500);
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
                Task.Delay(100);
            }

            if (_proc.MainModule == null) return Task.CompletedTask;
            _baseAddress = _proc.MainModule.BaseAddress;
            if (_baseAddress == IntPtr.Zero)
            {
                _ui.Status("Program has access protection.");
            }

            _is32Bit = ArchitectureCheck(_procHandle);

            _proc.EnableRaisingEvents = true;
            _proc.Exited += ProcessExit;

            _ui.Status("Running.");
            _ui.Details
            (
              "Process:      " + _proc.MainModule.ModuleName
                 + "\nBase Address: " + _baseAddress.ToString("X")
                 + "\nEntry Point:  " + _proc.MainModule.EntryPointAddress.ToString("X")
            );

            return Task.CompletedTask;
        }

        private bool ArchitectureCheck(IntPtr handle)
        {
            IsWow64Process(handle, out bool is32Bit);
            return is32Bit;
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
                address = _is32Bit ? BitConverter.ToInt32(buffer, 0) : BitConverter.ToInt64(buffer, 0);
            }
            return address;
        }

        #region MemFuncs

        public T Read<T>(List<long> offsets) where T : IConvertible
        {
            return (T)ConvertData<T>(offsets);
        }

        public void Write<T>(List<long> offsets, T value) where T : IConvertible
        {
            byte[] buffer;

            if (typeof(T) != typeof(string))
            {
                buffer = BitConverter.GetBytes((dynamic) value);
            }
            else
            {
                byte[] newString = Encoding.UTF8.GetBytes((dynamic) value);
                buffer = new byte[Read<string>(offsets).Length];
                Array.Copy(newString, buffer, buffer.Length);
            }

            TraverseWrite(offsets, buffer);
        }

        private byte[] TraverseRead(List<long> offsets, byte[] buffer)
        {
            long offset = TraverseOffsets(offsets);
            ReadProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
            return buffer;
        }

        private void TraverseWrite(List<long> offsets, byte[] buffer)
        {
            long offset = TraverseOffsets(offsets);
            WriteProcessMemory(_procHandle, (IntPtr)offset, buffer, buffer.Length, out _);
        }

        private object ConvertData<T>(List<long> offsets)
        {
            if (typeof(T) != typeof(string))
            {
                var value = TraverseRead(offsets, new byte[Marshal.SizeOf(typeof(T))]);

                if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
                    return value[0];
                if (typeof(T) == typeof(short))
                    return BitConverter.ToInt16(value, 0);
                if (typeof(T) == typeof(ushort))
                    return BitConverter.ToUInt16(value, 0);
                if (typeof(T) == typeof(int))
                    return BitConverter.ToInt32(value, 0);
                if (typeof(T) == typeof(uint))
                    return BitConverter.ToUInt32(value, 0);
                if (typeof(T) == typeof(long))
                    return BitConverter.ToInt64(value, 0);
                if (typeof(T) == typeof(ulong))
                    return BitConverter.ToInt64(value, 0);
                if (typeof(T) == typeof(float))
                    return BitConverter.ToSingle(value, 0);
                if (typeof(T) == typeof(double))
                    return BitConverter.ToDouble(value, 0);
                if (typeof(T) == typeof(bool))
                    return BitConverter.ToBoolean(value, 0);
                if (typeof(T) == typeof(char))
                    return BitConverter.ToChar(value, 0);
                if (typeof(T) == typeof(Half))
                    return BitConverter.ToHalf(value, 0);
            }
            else
            {
                string myString = string.Empty;

                for (ulong i = 1; i < ulong.MaxValue; i++)
                {
                    byte[] buffer = new byte[i];
                    buffer = TraverseRead(offsets, buffer);
                    if (buffer[i - 1] == 0) return myString;
                    myString = Encoding.UTF8.GetString(buffer);
                }
                return myString;
            }

            return default;
        }

        #endregion
    }
}