using System;
using System.Windows;
using MemoryManipulation;

namespace Memory
{
    public partial class MainWindow : Window
    {
        public void StatusText(string text)
        {
            Status.Content = text;
        }

        public void DetailsText(string text)
        {
            Details.Text = text;
        }

        public MainWindow()
        {
            InitializeComponent();

            // Start your thread here
            AoE2DE_s.Start(this);
        }
    }
}
