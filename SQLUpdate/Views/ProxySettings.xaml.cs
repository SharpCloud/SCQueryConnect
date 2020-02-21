using SCQueryConnect.Interfaces;
using System.Windows;

namespace SCQueryConnect.Views
{
    /// <summary>
    /// Interaction logic for ProxySettings.xaml
    /// </summary>
    public partial class ProxySettings : Window
    {
        private IProxyViewModel _viewModel;

        public ProxySettings(IProxyViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            tbProxy.Text = _viewModel.Proxy;
            chkAnnonymous.IsChecked = _viewModel.ProxyAnnonymous;
            tbUsername.Text = _viewModel.ProxyUserName;
            tbPassword.Password = _viewModel.ProxyPassword;
        }

        private void ClickOnOK(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbProxy.Text) && (bool)!chkAnnonymous.IsChecked)
            {
                if (string.IsNullOrEmpty(tbUsername.Text) || string.IsNullOrEmpty(tbPassword.Password))
                {
                    MessageBox.Show(
                        "You must provide a username and password if you are not using an anonymous proxy.",
                        "Proxy Server error");
                    return;
                }
            }

            _viewModel.Proxy = tbProxy.Text;
            _viewModel.ProxyAnnonymous = (bool)chkAnnonymous.IsChecked;
            _viewModel.ProxyUserName = tbUsername.Text;
            _viewModel.ProxyPassword = tbPassword.Password;
            Close();
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
