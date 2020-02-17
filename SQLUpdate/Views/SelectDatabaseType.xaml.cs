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
                new Database(DatabaseType.Excel, "Excel Spreadsheet"),
                new Database(DatabaseType.Access, "Access Database"),
                new Database(DatabaseType.SharePointList, "SharePoint List"),
                new Database(DatabaseType.Sql, "SQL Server Connection"),
                new Database(DatabaseType.Odbc, "ODBC Database Connection"),
                new Database(DatabaseType.Ado, "Generic ADO/OLDEB Connection"),
                new Database(DatabaseType.SharpCloudExcel, "SharpCloud (Excel)")
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
