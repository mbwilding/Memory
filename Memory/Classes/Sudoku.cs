using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Local
// ReSharper disable once CheckNamespace

namespace Memory
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class Sudoku
    {
        private static Startupinfo _si;
        private static Processinfo _pi;
        private static SecurityAttributes _dummySa; // = new SecurityAttributes();
        private static IntPtr _hProc, _hToken, _hDupToken, _pEnvBlock;

        public static void RunWithTokenOf(string processName, bool ofActiveSessionOnly, string exeToRun, string arguments, string workingDir = "")
        {
            var piDs = new List<int>();
            foreach (var p in Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(processName)))
                switch (ofActiveSessionOnly)
                {
                    case true:
                    {
                        if (p.SessionId == WTSGetActiveConsoleSessionId())
                            piDs.Add(p.Id);
                        break;
                    }
                    default:
                        piDs.Add(p.Id);
                        break;
                }
            
            switch (piDs.Count)
            {
                case 0:
                    GoComplain(ComplainReason.CantGetPid, processName);
                    return;
                default:
                    RunWithTokenOf(piDs[0], exeToRun, arguments, workingDir);
                    break;
            }
        }

        public static void RunWithTokenOf(int processId, string exeToRun, string arguments, string workingDir = "")
        {
            _throw = true;
            try
            {
                #region Process ExeToRun, Arguments and WorkingDir

                // If ExeToRun is not absolute path, then let it be
                exeToRun = Environment.ExpandEnvironmentVariables(exeToRun);
                switch (exeToRun.Contains("\\"))
                {
                    case false:
                    {
                        var split = Environment.ExpandEnvironmentVariables("%path%").Split(';');
                        foreach (var path in split)
                        {
                            var guess = path + "\\" + exeToRun;
                            if (!File.Exists(guess)) continue;
                            exeToRun = guess;
                            break;
                        }

                        break;
                    }
                }

                if (!File.Exists(exeToRun)) GoComplain(ComplainReason.FileNotFound, exeToRun);
                // If WorkingDir not exist, let it be the dir of ExeToRun
                // ExeToRun no dir? Impossible, as I would GoComplain() already
                workingDir = Environment.ExpandEnvironmentVariables(workingDir);
                if (!Directory.Exists(workingDir)) workingDir = Path.GetDirectoryName(exeToRun);
                // If arguments exist, CmdLine must include ExeToRun as well
                arguments = Environment.ExpandEnvironmentVariables(arguments);
                string cmdLine = null;
                if (arguments != "")
                {
                    if (exeToRun.Contains(" "))
                        cmdLine = "\"" + exeToRun + "\" " + arguments;
                    else
                        cmdLine = exeToRun + " " + arguments;
                }

                #endregion

                //Log += "Running as user: " + Environment.UserName; 
                // Set privileges of current process
                const string privs = "SeDebugPrivilege";
                var obj = "OpenProcessToken";
                if (!OpenProcessToken(GetCurrentProcess(), TokenAllAccess, out _hToken))
                    GoComplain(ComplainReason.ApiCallFailed, obj);
                foreach (var priv in privs.Split(','))
                {
                    obj = "LookupPrivilegeValue (" + priv + ")";
                    if (!LookupPrivilegeValue("", priv, out var luid)) GoComplain(ComplainReason.ApiCallFailed, obj);
                    obj = "AdjustTokenPrivileges (" + priv + ")";
                    var tp = new TokenPrivileges {PrivilegeCount = 1, Luid = luid, Attrs = SePrivilegeEnabled};

                    if (!(AdjustTokenPrivileges(_hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)
                          & (Marshal.GetLastWin32Error() == 0)))
                    {
                        GoComplain(ComplainReason.ApiCallFailed, obj);
                    }
                }

                CloseHandle(_hToken);
                // Open process by PID
                obj = "OpenProcess (PID: " + processId + ")";
                _hProc = OpenProcess(ProcessAccessFlags.All, false, processId);
                if (_hProc == null) GoComplain(ComplainReason.ApiCallFailed, obj);

                obj = "OpenProcessToken (TOKEN_DUPLICATE | TOKEN_QUERY)";
                if (!OpenProcessToken(_hProc, TokenDuplicate | TokenQuery, out _hToken))
                    GoComplain(ComplainReason.ApiCallFailed, obj);

                obj = "DuplicateTokenEx (TOKEN_ALL_ACCESS)";
                if (!DuplicateTokenEx(_hToken, TokenAllAccess, ref _dummySa,
                    SecurityImpersonationLevel.SecurityIdentification,
                    TokenType.TokenPrimary, out _hDupToken))
                    GoComplain(ComplainReason.ApiCallFailed, obj);

                // Create environment block
                obj = "CreateEnvironmentBlock";
                if (!CreateEnvironmentBlock(out _pEnvBlock, _hToken, true))
                    GoComplain(ComplainReason.ApiCallFailed, obj);

                //Log += obj + ": OK!\n";
                // Create process with the token we "stole" ^^
                var dwCreationFlags = NormalPriorityClass | CreateNewConsole | CreateUnicodeEnvironment;
                _si = new Startupinfo();
                _si.cb = Marshal.SizeOf(_si);
                _si.lpDesktop = "winsta0\\default";
                _pi = new Processinfo();

                if (!CreateProcessWithTokenW(_hDupToken, LogonFlags.WithProfile, exeToRun, cmdLine,
                    dwCreationFlags, _pEnvBlock, workingDir, ref _si, out _pi))
                {
                    obj = "CreateProcessAsUserW";
                    if (CreateProcessAsUserW(_hDupToken, exeToRun, cmdLine, ref _dummySa, ref _dummySa,
                        false, dwCreationFlags, _pEnvBlock, workingDir, ref _si, out _pi))
                    {
                        //Log += obj + ": Done! New process created successfully!";
                    }
                    else
                    {
                        switch (Marshal.GetLastWin32Error())
                        {
                            case 3:
                                GoComplain(ComplainReason.FileNotFound, exeToRun);
                                break;
                            case 193:
                                GoComplain(ComplainReason.FileNotExe, exeToRun);
                                break;
                            default:
                                GoComplain(ComplainReason.ApiCallFailed, obj);
                                break;
                        }
                    }
                }

                CleanUp();
                //EndLog();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void CleanUp()
        {
            CloseHandle(_si.hStdError);
            _si.hStdError = IntPtr.Zero;
            CloseHandle(_si.hStdInput);
            _si.hStdInput = IntPtr.Zero;
            CloseHandle(_si.hStdOutput);
            _si.hStdOutput = IntPtr.Zero;
            CloseHandle(_pi.hThread);
            _pi.hThread = IntPtr.Zero;
            CloseHandle(_pi.hProcess);
            _pi.hThread = IntPtr.Zero;
            DestroyEnvironmentBlock(_pEnvBlock);
            _pEnvBlock = IntPtr.Zero;
            CloseHandle(_hDupToken);
            _hDupToken = IntPtr.Zero;
            CloseHandle(_hToken);
            _hToken = IntPtr.Zero;
        }

        #region WIN32 API
        private static string WinApiLastErrMsg()
        {
            var err = Marshal.GetLastWin32Error();
            return new Win32Exception(err).Message + " (Error code: " + err + ")";
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            bool disableAllPrivileges,
            [MarshalAs(UnmanagedType.Struct)] ref TokenPrivileges newState,
            uint dummy, IntPtr dummy2, IntPtr dummy3);

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenPrivileges
        {
            internal uint PrivilegeCount;
            internal Luid Luid;
            internal uint Attrs;
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct Luid
        {
            private readonly int LowPart;
            private readonly uint HighPart;
        }

        private const uint SePrivilegeEnabled = 0x00000002;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
            out Luid lpLuid);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int processId
        );

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF
            //Terminate = 0x00000001,
            //CreateThread = 0x00000002,
            //VirtualMemoryOperation = 0x00000008,
            //VirtualMemoryRead = 0x00000010,
            //VirtualMemoryWrite = 0x00000020,
            //DuplicateHandle = 0x00000040,
            //CreateProcess = 0x000000080,
            //SetQuota = 0x00000100,
            //SetInformation = 0x00000200,
            //QueryInformation = 0x00000400,
            //QueryLimitedInformation = 0x00001000,
            //Synchronize = 0x00100000
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr processHandle,
            uint desiredAccess, out IntPtr tokenHandle);

        private const uint StandardRightsRequired = 0x000F0000;

        //private const uint StandardRightsRead = 0x00020000;
        private const uint TokenAssignPrimary = 0x0001;
        private const uint TokenDuplicate = 0x0002;
        private const uint TokenImpersonate = 0x0004;
        private const uint TokenQuery = 0x0008;
        private const uint TokenQuerySource = 0x0010;
        private const uint TokenAdjustPrivileges = 0x0020;
        private const uint TokenAdjustGroups = 0x0040;
        private const uint TokenAdjustDefault = 0x0080;

        private const uint TokenAdjustSessionid = 0x0100;
        //private const uint TokenRead = StandardRightsRead | TokenQuery;

        private const uint TokenAllAccess = StandardRightsRequired | TokenAssignPrimary |
                                            TokenDuplicate | TokenImpersonate | TokenQuery | TokenQuerySource |
                                            TokenAdjustPrivileges | TokenAdjustGroups | TokenAdjustDefault |
                                            TokenAdjustSessionid;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetTokenInformation(IntPtr tokenHandle, uint tokenInformationClass,
            ref uint tokenInformation, uint tokenInformationLength);

        private const uint TokenInformationClassTokenSessionId = 12;

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUserW(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SecurityAttributes lpProcessAttributes,
            ref SecurityAttributes lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref Startupinfo lpStartupInfo,
            out Processinfo lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct SecurityAttributes
        {
            private readonly int nLength;
            private readonly IntPtr lpSecurityDescriptor;
            private readonly int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Startupinfo
        {
            public int cb;
            private readonly string lpReserved;
            public string lpDesktop;
            private readonly string lpTitle;
            private readonly int dwX;
            private readonly int dwY;
            private readonly int dwXSize;
            private readonly int dwYSize;
            private readonly int dwXCountChars;
            private readonly int dwYCountChars;
            private readonly int dwFillAttribute;
            private readonly int dwFlags;
            private readonly short wShowWindow;
            private readonly short cbReserved2;
            private readonly IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Processinfo
        {
            public readonly IntPtr hProcess;
            public IntPtr hThread;
            private readonly int dwProcessId;
            private readonly int dwThreadId;
        }

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            LogonFlags dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref Startupinfo lpStartupInfo,
            out Processinfo lpProcessInformation);

        private enum LogonFlags
        {
            WithProfile = 1,
            NetCredentialsOnly
        }

        private const uint NormalPriorityClass = 0x00000020;
        private const uint CreateNewConsole = 0x00000010;
        private const uint CreateUnicodeEnvironment = 0x00000400;

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            ref SecurityAttributes lpTokenAttributes,
            SecurityImpersonationLevel impersonationLevel,
            TokenType tokenType,
            out IntPtr phNewToken);

        private enum SecurityImpersonationLevel
        {
            //SecurityAnonymous = 0,
            SecurityIdentification = 1
            //SecurityImpersonation = 2,
            //SecurityDelegation = 3
        }

        private enum TokenType
        {
            TokenPrimary = 1
            //TokenImpersonation
        }

        #endregion

        #region Complain System

        public enum ComplainReason
        {
            FileNotFound,
            FileNotExe,
            CantGetPid,
            ApiCallFailed
        }

        public delegate void ComplainHandler(ComplainReason reason, string obj);

        public static event ComplainHandler Complain;

        private static bool _throw;

        private static void GoComplain(ComplainReason reason, string obj)
        {
            switch (reason)
            {
                case ComplainReason.FileNotFound:
                    break;
                case ComplainReason.FileNotExe:
                    break;
                case ComplainReason.CantGetPid:
                    break;
                case ComplainReason.ApiCallFailed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }

            Complain?.Invoke(reason, obj);
            switch (_throw)
            {
                case false:
                    return;
                default:
                    _throw = false;
                    throw new Exception();
            }
        }

        #endregion
    }
}