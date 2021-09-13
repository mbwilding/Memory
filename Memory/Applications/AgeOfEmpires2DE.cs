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
        public MemoryManage _memory;
        private readonly MainWindow _mainWindow;

        // Update rates
        private readonly int _pollRateRead = 25;
        private readonly int _pollRateUi = 100;

        private bool AppRunning = true;

        // Declare your variables
        public long _skirmishMapVisibility;
        private long _skirmishMapVisibilityPrev = -1;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        public List<long> skirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };

        public AgeOfEmpires2DE(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _memory = new MemoryManage(_mainWindow, "AoE2DE_s", MemoryManage.AccessMode.PROCESS_ALL_ACCESS);

            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Closed += MainWindowOnClosed;

            Thread processThread = new(Process) { Priority = ThreadPriority.Highest };
            processThread.Start();
            Thread uiThread = new(UiUpdate) { Priority = ThreadPriority.AboveNormal };
            uiThread.Start();
        }

        private void Process()
        {
            while (AppRunning)
            {
                if (!_memory.ProcessRunning) continue;

                // Read values
                _skirmishMapVisibility = _memory.ReadInt(skirmishMapVisibilityOffsets);

                // TODO Act upon values here

                Thread.Sleep(_pollRateRead);
            }
        }

        private void UiUpdate()
        {
            while (AppRunning)
            {
                if (!_memory.ProcessRunning) continue;

                if (_skirmishMapVisibility != _skirmishMapVisibilityPrev)
                {
                    Application.Current.Dispatcher.Invoke(
                        DispatcherPriority.Background,
                        new Action(() => _mainWindow.SkirmishMapVisibilitySelection(_skirmishMapVisibility)));
                }
                SetPrev();
                Thread.Sleep(_pollRateUi);
            }
        }

        private void SetPrev()
        {
            _skirmishMapVisibilityPrev = _skirmishMapVisibility;
        }

        private void MainWindowOnClosed(object sender, EventArgs e)
        {
            AppRunning = false;
        }
    }
}
