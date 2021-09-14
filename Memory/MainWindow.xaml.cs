﻿using System;
using System.Windows;
using System.Windows.Input;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly AgeOfEmpires2DE _ageOfEmpires2De;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            Title = "Debug";
#else
            Title = Randomizer.Run();
#endif
            // Instantiate your objects here
            _ageOfEmpires2De = new AgeOfEmpires2DE(this);
        }

#region UiControls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        public void StatusText(string text)
        {
            Status.Content = text;
        }

        public void DetailsText(string text)
        {
            Details.Text = text;
        }

        private void SkirmishMapVisibility_DropDownClosed(object sender, EventArgs e)
        {
            if (_ageOfEmpires2De._skirmishMapVisibility != SkirmishMapVisibility.SelectedIndex)
            {
                _ageOfEmpires2De._memory.WriteInt(_ageOfEmpires2De.skirmishMapVisibilityOffsets,
                    SkirmishMapVisibility.SelectedIndex);
            }
        }
#endregion
    }
}
