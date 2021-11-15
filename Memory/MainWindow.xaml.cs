using System;
using System.Windows;
using System.Windows.Input;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly AgeOfEmpires2De _aoe2;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate your objects here
            _aoe2 = new AgeOfEmpires2De(this);
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
            _aoe2.SkirmishMapVisibility_DropDownClosed();
        }

        private void SkirmishMapVisibilityFreeze_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool state = SkirmishMapVisibilityFreeze.IsChecked == true;
            _aoe2.SkirmishMapVisibilityFreezed = state;
        }

        #endregion
    }
}
