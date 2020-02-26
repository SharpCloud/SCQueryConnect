using SCQueryConnect.Common;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SCQueryConnect.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        PasswordSecurity PublishPasswordSecurity { get; set; }
        PublishArchitecture PublishArchitecture { get; set; }
        bool CanCancelUpdate { get; set; }
        string PublishTabHeader { get; set; }
        string UpdateSubtext { get; set; }
        string UpdateText { get; set; }
        string Url { get; set; }
        string Username { get; set; }
        int SelectedQueryTabIndex { get; set; }
        TabItem SelectedQueryTabItem { get; set; }
        int SelectedTabIndex { get; set; }
        ObservableCollection<QueryData> Connections { get; set; }
        QueryData QueryRootNode { get; }
        QueryData SelectedQueryData { get; set; }

        QueryData FindParent(QueryData queryData);
        void CreateNewConnection(DatabaseType dbType);
        void CreateNewFolder();
        void MoveConnectionDown();
        void MoveConnectionUp();
        void CopyConnection();
        void DeleteConnection();
        void LoadApplicationState();
        void SaveApplicationState();
        void ExportQueryDataClick(QueryData queryData);
        void ImportConnections(string filePath);
        SharpCloudConfiguration GetApiConfiguration();
        IDbConnection GetDb(QueryData queryData);
        Task PreviewSql();
        Task RunQueryData(QueryData queryData);
        Task CancelStoryUpdate();
        void PublishBatchFolder();
    }
}
