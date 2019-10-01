using SCQueryConnect;
using System.Windows;

namespace SQLUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Bootstrapper.Start();

            var window = Bootstrapper.Resolve<MainWindow>();

            window.Closed += (s, a) =>
            {
                Bootstrapper.Stop();
            };

            window.Show();
        }
    }
}
