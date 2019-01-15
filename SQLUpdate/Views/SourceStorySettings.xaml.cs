using System.Windows;

namespace SCQueryConnect.Views
{
    public partial class SourceStorySettings : Window
    {
        public SourceStorySettings()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
