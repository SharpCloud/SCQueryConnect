using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCQueryConnect.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SelectDatabaseType : Window
    {
        public QueryData.DbType SelectedButton { get; set; }

        public SelectDatabaseType()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button.Content.ToString().Contains("SQL"))
                SelectedButton = QueryData.DbType.SQL;
            if (button.Content.ToString().Contains("ODBC"))
                SelectedButton = QueryData.DbType.ODBC;
            if (button.Content.ToString().Contains("ADO"))
                SelectedButton = QueryData.DbType.ADO;
            if (button.Content.ToString().Contains("Excel"))
                SelectedButton = QueryData.DbType.Excel;
            if (button.Content.ToString().Contains("Access"))
                SelectedButton = QueryData.DbType.Access;
            if (button.Content.ToString().Contains("SharePoint"))
                SelectedButton = QueryData.DbType.SharepointList;

            DialogResult = true;
        }
    }
}
