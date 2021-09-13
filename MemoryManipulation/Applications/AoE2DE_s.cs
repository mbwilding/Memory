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
            long skirmishMapVisibility = memory.TraverseMemOffsets(skirmishMapVisibilityOffsets);

            while (true)
            {
                // Do something with the returned data
                var test = memory.ReadInt(skirmishMapVisibility);
                Console.Title = test switch
                {
                    0 => Interface.AppName + " | Map Visibility: Normal",
                    1 => Interface.AppName + " | Map Visibility: Explored",
                    2 => Interface.AppName + " | Map Visibility: All Visible",
                    _ => Interface.AppName
                };
            }
        }
    }
}
