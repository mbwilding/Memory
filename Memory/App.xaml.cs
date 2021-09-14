using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
#if !DEBUG
            Process process = Process.GetCurrentProcess();
            string exe = process.MainModule.FileName;

            Process.Start(new ProcessStartInfo()
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + exe + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });
#endif
        }
    }
}
