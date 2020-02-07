using System.Windows;
using SCQueryConnect.Models;

namespace SCQueryConnect.Views
{
    /// <summary>
    /// Interaction logic for ConnectionInfo.xaml
    /// </summary>
    public partial class ConnectionInfo : Window
    {
        public ConnectionInfo(QueryData queryData)
        {
            InitializeComponent();
            
            txt.Text =
                $"Internal Connection Type:\n" +
                $"{queryData.GetBatchDBType}\n" +
                $"\n" +
                $"Connection String:\n" +
                $"{queryData.FormattedConnectionString}";
        }
    }
}
