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
        public MemoryManage Memory;
        private readonly MainWindow _mainWindow;

        // Update rates
        private const int PollRateIdle = 200;
        private const int PollRateRead = 25;
        private const int PollRateUi = 100;
        private const int PollRateFreeze = 50;

        // Declare your variables
        public int AmmoMag;
        public bool AmmoMagFrozen;
        public int AmmoBag;
        public bool AmmoBagFrozen;

        // Set your offsets (Obtained via Cheat Engine, comparing pointer maps)
        public List<long> AmmoMagOffsets = new() { 0x17B0B8, 0x140 };
        public List<long> AmmoBagOffsets = new() { 0x17B0B8, 0x11C };

        public AssaultCube(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Memory = new MemoryManage(_mainWindow, "ac_client", MemoryManage.AccessMode.All);

            Task.Run(Process);
            Task.Run(Ui);
            Task.Run(Freeze);
        }

        private Task Process()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
                {
                    Task.Delay(PollRateIdle);
                    continue;
                }

                // Read values
                if (!AmmoMagFrozen)
                    AmmoMag = Memory.Read<int>(AmmoMagOffsets);
                if (!AmmoBagFrozen)
                    AmmoBag = Memory.Read<int>(AmmoBagOffsets);

                // TODO Act upon values here

                Task.Delay(PollRateRead);
            }
            Memory.Clean();

            return Task.CompletedTask;
        }

        private Task Ui()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
                {
                    UiState(false);

                    Task.Delay(PollRateIdle);
                    continue;
                }
                else
                {
                    UiState(true);
                }

                UiUpdate();

                Task.Delay(PollRateUi);
            }

            return Task.CompletedTask;
        }

        private Task Freeze()
        {
            while (Memory.AppRunning)
            {
                if (!Memory.ProcessRunning)
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
        }

        private void UiUpdate()
        {
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!AmmoMagFrozen)
                        _mainWindow.AmmoMag.Text = AmmoMag.ToString();
                    if (!AmmoBagFrozen)
                        _mainWindow.AmmoBag.Text = AmmoBag.ToString();
                }));
        }

        public void AmmoMagFreeze()
        {
            if (!AmmoMagFrozen) return;

            int value = AmmoMag;
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!int.TryParse(_mainWindow.AmmoMag.Text, out value)) return;
                    state = true;
                }));
            if (state) Memory.Write(AmmoMagOffsets, value);
        }

        private void AmmoBagFreeze()
        {
            if (!AmmoBagFrozen) return;

            int value = AmmoBag;
            bool state = false;
            Current?.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    if (!int.TryParse(_mainWindow.AmmoBag.Text, out value)) return;
                    state = true;
                }));
            if (state) Memory.Write(AmmoBagOffsets, value);
        }

        #endregion
    }
}
