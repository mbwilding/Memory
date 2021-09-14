using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using static System.Windows.Application;

// ReSharper disable CheckNamespace

namespace Memory
{
    internal class AgeOfEmpires2De
    {
        public MemoryManage Memory;
        private readonly MainWindow _mainWindow;
        private bool _appRunning = true;

        // Update rates
        private const int PollRateRead = 25;
        private const int PollRateUi = 100;

        // Declare your variables
        public int SkirmishMapVisibility;
        private int _skirmishMapVisibilityPrev = -1;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        public List<long> SkirmishMapVisibilityOffsets = new() { 0x03165DE8, 0x258, 0x10, 0x100, 0x3C };

        public AgeOfEmpires2De(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Memory = new MemoryManage(_mainWindow, "AoE2DE_s", MemoryManage.AccessMode.All);

            if (Current.MainWindow != null) Current.MainWindow.Closed += MainWindowOnClosed;

            Thread threadProcess = new(Process) { Priority = ThreadPriority.Highest };
            threadProcess.Start();
            Thread threadUi = new(Ui) { Priority = ThreadPriority.AboveNormal };
            threadUi.Start();
        }

        private void Process()
        {
            while (_appRunning)
            {
                if (!Memory.ProcessRunning) continue;

                // Read values
                SkirmishMapVisibility = Memory.ReadInt(SkirmishMapVisibilityOffsets);

                // TODO Act upon values here

                Thread.Sleep(PollRateRead);
            }
            Memory.Clean();
        }

        private void Ui()
        {
            while (_appRunning)
            {
                if (!Memory.ProcessRunning)
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
                        if (SkirmishMapVisibility != _skirmishMapVisibilityPrev)
                        {
                            Current.Dispatcher.Invoke(
                                DispatcherPriority.DataBind,
                                new Action(() => _mainWindow.SkirmishMapVisibility.SelectedIndex = SkirmishMapVisibility));
                        }
                    }));
                SetPrev();
                Thread.Sleep(PollRateUi);
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
            _skirmishMapVisibilityPrev = SkirmishMapVisibility;
        }

        private void MainWindowOnClosed(object sender, EventArgs e)
        {
            _appRunning = false;
        }
    }
}
