using SCQueryConnect;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SQLUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILog _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Bootstrapper.Start();

            _logger = CreateErrorLogger();
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            var window = Bootstrapper.Resolve<MainWindow>();

            window.Closed += (s, a) =>
            {
                Bootstrapper.Stop();
            };

            window.Show();
        }

        private ILog CreateErrorLogger()
        {
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpCloudQueryConnect");
            var logfile = localPath + "/SCQueryConnect.log";
            return new ConsoleLogger(logfile);
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = $"{e.Exception.Message} {e.Exception.StackTrace}";
            _logger.Log(message);
        }
    }
}
