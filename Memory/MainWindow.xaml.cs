using System;
using System.Windows;
using System.Windows.Input;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly AgeOfEmpires2De _ageOfEmpires2De;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            Title = "Debug";
#else
            Title = Randomizer.Run();
#endif
            // Instantiate your objects here
            _ageOfEmpires2De = new AgeOfEmpires2De(this);
        }

        #region AppControls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        #endregion

        #region UiControls
        private void SkirmishMapVisibility_DropDownClosed(object sender, EventArgs e)
        {
            if (_ageOfEmpires2De.SkirmishMapVisibility != SkirmishMapVisibility.SelectedIndex)
            {
                _ageOfEmpires2De.Memory.WriteInt(_ageOfEmpires2De.SkirmishMapVisibilityOffsets,
                    SkirmishMapVisibility.SelectedIndex);
            }
        }
        #endregion
    }
}
