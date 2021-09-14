using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using static System.Windows.Application;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable FunctionNeverReturns
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Memory
{
    internal class AgeOfEmpires2DE
    {
        public MemoryManage _memory;
        private readonly MainWindow _mainWindow;
        private bool AppRunning = true;

        // Update rates
        private const int _pollRateRead = 25;
        private const int _pollRateUi = 100;

        // Declare your variables
        public int _skirmishMapVisibility;
        private int _skirmishMapVisibilityPrev = -1;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        public List<long> skirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };

        public AgeOfEmpires2DE(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _memory = new MemoryManage(_mainWindow, "AoE2DE_s", MemoryManage.AccessMode.PROCESS_ALL_ACCESS);

            if (Current.MainWindow != null) Current.MainWindow.Closed += MainWindowOnClosed;

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
            _memory.Clean();
        }

        private void UiUpdate()
        {
            while (AppRunning)
            {
                if (!_memory.ProcessRunning)
                {
                    Current.Dispatcher.Invoke(
                        DispatcherPriority.DataBind,
                        new Action(() =>
                        {
                            if (_mainWindow.SkirmishMapVisibility.IsEnabled)
                                SkirmishMapVisibilityToggle(false);
                        }));
                    continue;
                }

                Current.Dispatcher.Invoke(
                    DispatcherPriority.DataBind,
                    new Action(() =>
                    {
                        if (!_mainWindow.SkirmishMapVisibility.IsEnabled)
                        {
                            SkirmishMapVisibilityToggle(true);
                        }
                        if (_skirmishMapVisibility != _skirmishMapVisibilityPrev)
                        {
                            Current.Dispatcher.Invoke(
                                DispatcherPriority.DataBind,
                                new Action(() => _mainWindow.SkirmishMapVisibility.SelectedIndex = _skirmishMapVisibility));
                        }
                    }));
                SetPrev();
                Thread.Sleep(_pollRateUi);
            }
        }

        public void SkirmishMapVisibilityToggle(bool state)
        {
            _mainWindow.SkirmishMapVisibility.IsEnabled = state;
            if (!state)
            {
                _mainWindow.SkirmishMapVisibility.SelectedIndex = -1;
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
