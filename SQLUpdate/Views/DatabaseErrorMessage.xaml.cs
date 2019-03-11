using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Helpers;
using System.Diagnostics;
using System.Windows;

namespace SCQueryConnect.Views
{
    public partial class DatabaseErrorMessage : Window
    {
        private readonly string _alternateArchitectureLink;

        public string AlternateArchitecture { get; }

        public DatabaseErrorMessage()
        {
            InitializeComponent();

            var _architectureDetector = new ArchitectureDetector();

            if (_architectureDetector.Is32Bit)
            {
                AlternateArchitecture = "64";
                _alternateArchitectureLink = "https://sharpcloudonpremupdate.blob.core.windows.net/apidemos/sharpcloudSQLUpdate/publish.htm";
            }
            else
            {
                AlternateArchitecture = "32";
                _alternateArchitectureLink = "https://sharpcloudonpremupdate.blob.core.windows.net/apidemos/sharpcloudSQLUpdatex86/publish.htm";
            }

            DataContext = this;
        }

        private void AccessDatabaseEngineHyperlink_Click(object sender, RoutedEventArgs e)
        {
            UrlHelper.GoToAccessDatabaseEngine();
        }

        private void QueryConnectHyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_alternateArchitectureLink);
        }
    }
}
