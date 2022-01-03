using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using static System.Windows.Application;

// ReSharper disable IdentifierTypo
// ReSharper disable CheckNamespace

namespace Memory
{
    // Assault Cube v1.3.0.0

    internal class AssaultCube
    {
        private readonly MemoryManage _memory;
        private readonly MainWindow _mainWindow;
        private bool _uiState;

        // Update rates
        private const int PollRateIdle = 200;
        private const int PollRateRead = 25;
        private const int PollRateUi = 100;
        private const int PollRateFreeze = 50;

        // Declare your variables
        private int _ammoMag;
        public bool AmmoMagFrozen;
        private int _ammoBag;
        public bool AmmoBagFrozen;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        private readonly List<long> _ammoMagOffsets = new() { 0x17B0B8, 0x140 };
        private readonly List<long> _ammoBagOffsets = new() { 0x17B0B8, 0x11C };

        public AssaultCube(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _memory = new MemoryManage(_mainWindow, "ac_client", MemoryManage.AccessMode.All);

            Task.Run(Process);
            Task.Run(Ui);
            Task.Run(Freeze);
        }

        private Task Process()
        {
            while (_memory.AppRunning)
            {
                if (!_memory.ProcessRunning)
                {
                    Task.Delay(PollRateIdle);
                    continue;
                }

                // Read values
                if (!AmmoMagFrozen)
                    _ammoMag = _memory.Read<int>(_ammoMagOffsets);
                if (!AmmoBagFrozen)
                    _ammoBag = _memory.Read<int>(_ammoBagOffsets);

                // TODO Act upon values here

                Task.Delay(PollRateRead);
            }
            _memory.Clean();

            return Task.CompletedTask;
        }

        private Task Ui()
        {
            while (_memory.AppRunning)
            {
                if (!_memory.ProcessRunning)
                {
                    if (_uiState) UiState(false);

                    Task.Delay(PollRateIdle);
                    continue;
                }
                if (!_uiState) UiState(true);
                
                UiUpdate();

                Task.Delay(PollRateUi);
            }

            return Task.CompletedTask;
        }

        private Task Freeze()
        {
            while (_memory.AppRunning)
            {
                if (!_memory.ProcessRunning)
                {
                    Task.Delay(PollRateIdle);
                    continue;
                }

                AmmoMagFreeze();
                AmmoBagFreeze();

                Task.Delay(PollRateFreeze);
            }

            return Task.CompletedTask;
        }

        #region UiFunctions

        private void UiState(bool state)
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _mainWindow.AmmoMag.IsEnabled = state;
                    _mainWindow.AmmoBag.IsEnabled = state;
                }));
            _uiState = state;
        }

        private void UiUpdate()
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!AmmoMagFrozen)
                        _mainWindow.AmmoMag.Text = _ammoMag.ToString();
                    if (!AmmoBagFrozen)
                        _mainWindow.AmmoBag.Text = _ammoBag.ToString();
                }));
        }

        private void AmmoMagFreeze()
        {
            if (!AmmoMagFrozen) return;

            int value = _ammoMag;
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!int.TryParse(_mainWindow.AmmoMag.Text, out value)) return;
                    state = true;
                }));
            if (state) _memory.Write(_ammoMagOffsets, value);
        }

        private void AmmoBagFreeze()
        {
            if (!AmmoBagFrozen) return;

            int value = _ammoBag;
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!int.TryParse(_mainWindow.AmmoBag.Text, out value)) return;
                    state = true;
                }));
            if (state) _memory.Write(_ammoBagOffsets, value);
        }

        #endregion
    }
}
