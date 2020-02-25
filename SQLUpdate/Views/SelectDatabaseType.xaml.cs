using SCQueryConnect.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SelectDatabaseType : Window
    {
        public IList<Database> DatabaseTypes { get; }
        public DatabaseType SelectedButton { get; set; }

        public SelectDatabaseType()
        {
            InitializeComponent();

            DatabaseTypes = new[]
            {
                new Database(DatabaseType.Excel, "Excel Spreadsheet", "../Images/Connections/Excel.png"),
                new Database(DatabaseType.Access, "Access Database", "../Images/Connections/Access.png"),
                new Database(DatabaseType.SharePointList, "SharePoint List", "../Images/Connections/SharePoint.png"),
                new Database(DatabaseType.Sql, "SQL Server Connection", "../Images/Connections/SqlServer.png"),
                new Database(DatabaseType.Odbc, "ODBC Database Connection", "../Images/Connections/ODBC.png"),
                new Database(DatabaseType.Ado, "Generic ADO/OLEDB Connection", "../Images/Connections/ADO.png"),
                new Database(DatabaseType.SharpCloudExcel, "SharpCloud (Excel)", "../Images/Connections/SharpCloud.png")
            };

            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var type = button.DataContext as Database;
            SelectedButton = type.DBType;
            DialogResult = true;
        }
    }
}
