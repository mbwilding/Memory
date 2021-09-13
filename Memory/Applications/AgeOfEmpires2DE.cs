using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Memory;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable FunctionNeverReturns
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace MemoryManipulation
{
    internal class AgeOfEmpires2DE
    {
        private readonly MainWindow _mainWindow;

        // Declare your variables
        public long _skirmishMapVisibility;

        public MemoryManage _memory;

        // Set your offsets
        public List<long> skirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };

        public AgeOfEmpires2DE(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
            Thread process = new(Process)
            {
                Priority = ThreadPriority.AboveNormal
            };
            process.Start();
        }

        private void Process()
        {
            _memory = new MemoryManage(_mainWindow, "AoE2DE_s", MemoryManage.AccessMode.PROCESS_ALL_ACCESS);

            while (true)
            {
                // Read values
                _skirmishMapVisibility = _memory.ReadInt(skirmishMapVisibilityOffsets);

                // Act upon values

                // UI
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => _mainWindow.SkirmishMapVisibilitySelection(_skirmishMapVisibility)));
            }
        }
    }
}
