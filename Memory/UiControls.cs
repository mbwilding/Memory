using System;
using System.Windows.Threading;

// ReSharper disable once CheckNamespace

namespace Memory
{
    public class UiControls
    {
        private readonly MainWindow _window;
        private readonly System.Windows.Application _app;

        public UiControls(MainWindow window, System.Windows.Application app)
        {
            _window = window;
            _app = app;
        }

        public void Status(string message)
        {
            _app.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _window.Status.Content = message;
                }));
        }

        public void Details(string message)
        {
            _app.Dispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(() =>
                {
                    _window.Details.Text = message;
                }));
        }
    }
}
