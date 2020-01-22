using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        TabItem SelectedParentTab { get; set; }
        Visibility ConnectionsVisibility { get; set; }
        Visibility SolutionsVisibility { get; set; }
    }
}
