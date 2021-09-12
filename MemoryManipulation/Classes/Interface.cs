using System;

namespace MemoryManipulation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]

    public class Interface
    {
        private const string Banner =
        @"
    ███╗   ███╗███████╗███╗   ███╗ ██████╗ ██████╗ ██╗   ██╗
    ████╗ ████║██╔════╝████╗ ████║██╔═══██╗██╔══██╗╚██╗ ██╔╝
    ██╔████╔██║█████╗  ██╔████╔██║██║   ██║██████╔╝ ╚████╔╝ 
    ██║╚██╔╝██║██╔══╝  ██║╚██╔╝██║██║   ██║██╔══██╗  ╚██╔╝  
    ██║ ╚═╝ ██║███████╗██║ ╚═╝ ██║╚██████╔╝██║  ██║   ██║   
    ╚═╝     ╚═╝╚══════╝╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   
        ";

        public static string AppName = "Memory Manipulation";

        public static void Setup()
        {
            Console.Title = AppName;
            Console.SetWindowSize(64, 15);
            Write(Banner, ConsoleColor.Magenta);
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Reset()
        {
            Console.Clear();
            Setup();
        }

        public static void Failed(string message)
        {
            Reset();
            Write(message + "\nPress any key to exit.", ConsoleColor.Red);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
