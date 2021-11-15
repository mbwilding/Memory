using System;

namespace Memory
{
    internal class Program
    {
        [STAThread]
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            App app = new()
            {
                StartupUri = new Uri("/MainWindow.xaml", UriKind.Relative)
            };
            app.Run();
        }
    }
}
