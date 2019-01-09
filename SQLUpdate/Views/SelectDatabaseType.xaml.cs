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
        public IList<DatabaseType> DatabaseTypes { get; }
        public QueryData.DbType SelectedButton { get; set; }

        public SelectDatabaseType()
        {
            InitializeComponent();

            DatabaseTypes = new[]
            {
                new DatabaseType(QueryData.DbType.Excel, "Excel Spreadsheet"),
                new DatabaseType(QueryData.DbType.Access, "Access Database"),
                new DatabaseType(QueryData.DbType.SharepointList, "SharePoint List"),
                new DatabaseType(QueryData.DbType.SQL, "SQL Server Connection"),
                new DatabaseType(QueryData.DbType.ODBC, "ODBC Database Connection"),
                new DatabaseType(QueryData.DbType.ADO, "Generic ADO/OLDEB Connection"),
                new DatabaseType(QueryData.DbType.SharpCloud, "SharpCloud")
            };

            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var type = button.DataContext as DatabaseType;
            SelectedButton = type.DBType;
            DialogResult = true;
        }
    }
}
