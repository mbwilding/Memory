using System.Collections.Generic;
using System.Threading;
using Memory;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable FunctionNeverReturns
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace MemoryManipulation
{
    internal class AoE2DE_s
    {
        private static MainWindow _mainWindow;

        public static void Start(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Thread thread = new(Process);
            thread.Start();
        }

        private static void Process()
        {
            MemoryManage memory = new(_mainWindow, "notepad", MemoryManage.AccessMode.PROCESS_VM_READ);

            // Set your offsets here
            List<long> skirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };

            while (true)
            {
                // Do something with the returned data
                var skirmishMapVisibility = memory.ReadInt(skirmishMapVisibilityOffsets);
                //skirmishMapVisibility
            }
        }
    }
}
