using System;

namespace Memory
{
    class Program
    {
        [STAThread]
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
#if !DEBUG
            Randomizer.Run();
#endif

            App a = new App();
            a.StartupUri = new Uri("/MainWindow.xaml", UriKind.Relative);
            a.Run();
        }
    }
}
