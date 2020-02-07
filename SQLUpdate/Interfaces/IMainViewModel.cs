using SCQueryConnect.Models;
using System.ComponentModel;

namespace SCQueryConnect.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        PasswordSecurity PublishPasswordSecurity { get; set; }
        PublishArchitecture PublishArchitecture { get; set; }
        string PublishTabHeader { get; set; }
        string UpdateMessage { get; set; }
        int SelectedTabIndex { get; set; }
        QueryData SelectedQueryData { get; set; }

        void SelectUpdateTab();
    }
}
