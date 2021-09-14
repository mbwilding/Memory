using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable StringLiteralTypo
// ReSharper disable once CheckNamespace

namespace Memory
{

    // REQUIRES PUBLISHING TO SINGLE FILE

    public static class Randomizer
    {
        public static readonly Random Rand = new();

        public static string Run()
        {
            Process process = Process.GetCurrentProcess();
            string tempPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (process.MainModule != null)
            {
                if (Path.GetDirectoryName(process.MainModule.FileName) == tempPath)
                {
                    return Path.GetFileNameWithoutExtension(process.MainModule.FileName);
                }

                string newExePath = tempPath + @"\" + RandomStr(Rand.Next(5, 13)) + ".exe";
                string processPath = string.Empty;
                if (process.MainModule != null) processPath = process.MainModule.FileName;

                if (processPath != null)
                {
                    if (File.Exists(newExePath))
                    {
                        File.Delete(newExePath);
                    }
                    File.Copy(processPath, newExePath);
                    Process.Start(newExePath);
                    Environment.Exit(0);
                }
            }
            return string.Empty;
        }

        public static void Clean()
        {
            Process process = Process.GetCurrentProcess();
            if (process.MainModule == null) return;

            string exe = process.MainModule.FileName;
            Process.Start(new ProcessStartInfo()
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + exe + "\"",
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
    }
}
