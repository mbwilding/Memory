using System.Windows;
using System.Windows.Input;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly AssaultCube _ac;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate your objects here
            _ac = new AssaultCube(this);
        }

        #region AppControls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        #endregion

        #region UiControls

        private void AmmoMagFrozen_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _ac.AmmoMagFrozen = AmmoMagFrozen.IsChecked == true;
        }

        private void AmmoBagFrozen_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _ac.AmmoBagFrozen = AmmoBagFrozen.IsChecked == true;
        }

        #endregion
    }
}
