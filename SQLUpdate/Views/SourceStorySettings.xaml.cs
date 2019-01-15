using System;
using System.Text;
using System.Windows;

namespace SCQueryConnect.Views
{
    public partial class SourceStorySettings : Window
    {
        private readonly QueryData _queryData;

        public SourceStorySettings(QueryData queryData)
        {
            InitializeComponent();

            _queryData = queryData;
            Server.Text = _queryData.SourceStoryServer;
            UserName.Text = _queryData.SourceStoryUserName;
            Password.Password = Encoding.Default.GetString(
                Convert.FromBase64String(_queryData.SourceStoryPassword ?? string.Empty));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _queryData.SourceStoryServer = Server.Text;
            _queryData.SourceStoryUserName = UserName.Text;
            _queryData.SourceStoryPassword =
                Convert.ToBase64String(Encoding.Default.GetBytes(Password.Password));

            DialogResult = true;
        }
    }
}
