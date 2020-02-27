using SCQueryConnect.Interfaces;
using System.Threading.Tasks;
using System.Windows;

namespace SCQueryConnect.Views
{
    /// <summary>
    /// Interaction logic for AttributeMappingEditor.xaml
    /// </summary>
    public partial class AttributeMappingEditor : Window
    {
        public AttributeMappingEditor()
        {
            InitializeComponent();
        }

        private void AttributeMappingEditorLoaded(object sender, RoutedEventArgs e)
        {
            if (Content is FrameworkElement fe &&
                fe.DataContext is IAttributeMappingEditorViewModel vm)
            {
                // Do not run on UI thread to maintain responsive UI
                Task.Run(() => vm.InitialiseEditor(null));
            }
        }
    }
}
