﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using static System.Windows.Application;

// ReSharper disable IdentifierTypo
// ReSharper disable CheckNamespace

namespace Memory
{
    internal class AgeOfEmpires2De
    {
        public MemoryManage Memory;
        private readonly MainWindow _mainWindow;

        // Update rates
        private const int PollRateIdle = 200;
        private const int PollRateRead = 25;
        private const int PollRateUi = 100;
        private const int PollRateFreeze = 50;


        // Declare your variables
        public int SkirmishMapVisibility;
        private int _skirmishMapVisibilityPrev = -1;
        public bool SkirmishMapVisibilityFreezed;
        public int SkirmishRelics;
        private int _skirmishRelicsPrev = -1;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        public List<long> SkirmishMapVisibilityOffsets = new() { 0x3165DE8, 0x258, 0x10, 0x100, 0x3C };
        public List<long> SkirmishRelicsOffsets = new() { 0x34FF940, 0x8, 0x80C };

        public AgeOfEmpires2De(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Memory = new MemoryManage(_mainWindow, "AoE2DE_s", MemoryManage.AccessMode.All);

            Thread threadProcess = new(Process) { Priority = ThreadPriority.Highest };
            threadProcess.Start();
            Thread threadUi = new(Ui) { Priority = ThreadPriority.Normal };
            threadUi.Start();
            Thread threadFreeze = new(Freeze) { Priority = ThreadPriority.AboveNormal };
            threadFreeze.Start();
        }

        private void Process()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
                {
                    Thread.Sleep(PollRateIdle);
                    continue;
                }

                // Read values
                if (!SkirmishMapVisibilityFreezed)
                    SkirmishMapVisibility = Memory.ReadInt(SkirmishMapVisibilityOffsets);
                SkirmishRelics = Memory.ReadInt(SkirmishRelicsOffsets);

                // TODO Act upon values here

                Thread.Sleep(PollRateRead);
            }
            Memory.Clean();
        }

        private void Ui()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
                {
                    // Disables UI Elements
                    if (SkirmishMapVisibilityCheck())
                        SkirmishMapVisibilityToggle(false);
                    Thread.Sleep(PollRateIdle);
                    continue;
                }

                // Enables UI Elements
                if (!SkirmishMapVisibilityCheck())
                    SkirmishMapVisibilityToggle(true);

                // Updates UI only when change detected
                if (SkirmishMapVisibility != _skirmishMapVisibilityPrev)
                    SkirmishMapVisibilityUpdate();
                if (SkirmishRelics != _skirmishRelicsPrev)
                    SkirmishRelicsUpdate();

                SetPrev();
                Thread.Sleep(PollRateUi);
            }
        }

        private void Freeze()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
                {
                    Thread.Sleep(PollRateIdle);
                    continue;
                }

                SkirmishMapVisibilityFreeze();

                Thread.Sleep(PollRateFreeze);
            }
        }

        // Set previous values
        private void SetPrev()
        {
            _skirmishMapVisibilityPrev = SkirmishMapVisibility;
            _skirmishRelicsPrev = SkirmishRelics;
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

        public void SkirmishMapVisibility_DropDownClosed()
        {
            // Called from UI
            if (SkirmishMapVisibility != _mainWindow.SkirmishMapVisibility.SelectedIndex)
            {
                Memory.WriteInt(SkirmishMapVisibilityOffsets, _mainWindow.SkirmishMapVisibility.SelectedIndex);
            }
        }

        public void SkirmishMapVisibilityFreeze()
        {
            int value = SkirmishMapVisibility;
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (_mainWindow.SkirmishMapVisibilityFreeze.IsChecked != true) return;
                    value = _mainWindow.SkirmishMapVisibility.SelectedIndex;
                    state = true;
                }));
            if (state) Memory.WriteInt(SkirmishMapVisibilityOffsets, value);
        }

        public void SkirmishRelicsUpdate()
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _mainWindow.Relics.Content = SkirmishRelics;
                }));
        }
        #endregion
    }
}
