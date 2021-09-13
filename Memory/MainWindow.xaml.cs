using System;
using System.Windows;
using MemoryManipulation;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly AgeOfEmpires2DE _ageOfEmpires2De;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate your objects here
            _ageOfEmpires2De = new AgeOfEmpires2DE(this);
        }

        #region UiControls
        public void StatusText(string text)
        {
            Status.Content = text;
        }

        public void DetailsText(string text)
        {
            Details.Text = text;
        }

        public void SkirmishMapVisibilitySelection(long value)
        {
            SkirmishMapVisibility.SelectedIndex = Convert.ToInt16(value);
        }

        private void SkirmishMapVisibility_DropDownClosed(object sender, EventArgs e)
        {
            if (_ageOfEmpires2De._skirmishMapVisibility != SkirmishMapVisibility.SelectedIndex)
            {
                _ageOfEmpires2De._memory.WriteInt(_ageOfEmpires2De.skirmishMapVisibilityOffsets, SkirmishMapVisibility.SelectedIndex);
            }
        }
        #endregion
    }
}
