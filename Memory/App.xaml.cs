using System.Windows;

namespace Memory
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
#if !DEBUG
            Randomizer.Clean();
#endif
        }
    }
}
