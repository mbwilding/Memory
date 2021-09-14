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
            string tempPath = Path.GetTempPath();
            if (process.MainModule != null)
            {
                string currentPath = Path.GetDirectoryName(process.MainModule.FileName) + @"\";
                if (currentPath == tempPath)
                {
                    return Path.GetFileNameWithoutExtension(process.MainModule.FileName);
                }

                string newExePath = tempPath + RandomStr(Rand.Next(5, 13)) + ".exe";
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

        public static string RandomStr(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Rand.Next(s.Length)]).ToArray());
        }

/*Process process = Process.GetCurrentProcess();
string currentPath = Path.GetDirectoryName(process.MainModule.FileName) + @"\";
string newExePath = Path.GetTempPath() + "Random" + ".exe";
File.Copy(process.MainModule.FileName, newExePath);
Process newProcess = new Process();
newProcess.StartInfo.Arguments = "secondRun";
newProcess.Start();*/
    }
}
