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
                    // Disables UI Elements
                    if (SkirmishMapVisibilityCheck())
                        SkirmishMapVisibilityToggle(false);
                    continue;
                }

                // Enables UI Elements
                if (!SkirmishMapVisibilityCheck())
                    SkirmishMapVisibilityToggle(true);

                // Updates UI only when change detected
                if (SkirmishMapVisibility != _skirmishMapVisibilityPrev)
                    SkirmishMapVisibilityUpdate();

                SetPrev();
                Thread.Sleep(PollRateUi);
            }
        }

        // Set previous values
        private void SetPrev()
        {
            _skirmishMapVisibilityPrev = SkirmishMapVisibility;
        }

        // Finalizes threads when closed
        private void MainWindowOnClosed(object sender, EventArgs e)
        {
            _appRunning = false;
        }

        #region UiFunctions
        public bool SkirmishMapVisibilityCheck()
        {
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    state = _mainWindow.SkirmishMapVisibility.IsEnabled;
                }));
            return state;
        }

        public void SkirmishMapVisibilityToggle(bool state)
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _mainWindow.SkirmishMapVisibility.IsEnabled = state;
                    if (!state)
                    {
                        _mainWindow.SkirmishMapVisibility.SelectedIndex = -1;
                    }
                }));
        }

        public void SkirmishMapVisibilityUpdate()
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _mainWindow.SkirmishMapVisibility.SelectedIndex = SkirmishMapVisibility;
                }));
        }
        #endregion
    }
}
