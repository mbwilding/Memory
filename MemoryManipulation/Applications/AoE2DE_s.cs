using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable FunctionNeverReturns
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace MemoryManipulation
{
    internal class AoE2DE_s
    {
        public static void Start()
        {
            Thread thread = new(Process);
            thread.Start();
        }

        private static void Process()
        {
            MemoryManage memory = new("AoE2DE_s", MemoryManage.AccessMode.PROCESS_VM_READ);

            // Set your offsets here
            List<long> skirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };
            var skirmishMapVisibility = memory.TraverseMemOffsets(skirmishMapVisibilityOffsets);

            while (true)
            {
                // Do something with the returned data
                var test = memory.ReadInt(skirmishMapVisibility);
                switch (test)
                {
                    case 0:
                        Console.Title = Interface.AppName + " | Map Visibility: Normal";
                        break;
                    case 1:
                        Console.Title = Interface.AppName + " | Map Visibility: Explored";
                        break;
                    case 2:
                        Console.Title = Interface.AppName + " | Map Visibility: All Visible";
                        break;
                    default:
                        Console.Title = Interface.AppName;
                        break;
                }
            }
        }
    }
}
