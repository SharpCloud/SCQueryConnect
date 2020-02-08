﻿using SCQueryConnect.Common;
using SCQueryConnect.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SCQueryConnect.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        PasswordSecurity PublishPasswordSecurity { get; set; }
        PublishArchitecture PublishArchitecture { get; set; }
        string PublishTabHeader { get; set; }
        string UpdateSubtext { get; set; }
        string UpdateText { get; set; }
        int SelectedTabIndex { get; set; }
        ObservableCollection<QueryData> Connections { get; set; }
        QueryData QueryRootNode { get; }
        QueryData SelectedQueryData { get; set; }

        QueryData FindParent(QueryData queryData);
        void SelectUpdateTab();
        void CreateNewConnection(DatabaseType dbType);
        void CreateNewFolder();
        void MoveConnectionDown();
        void MoveConnectionUp();
        void CopyConnection();
        void DeleteConnection();
        void LoadAllConnections(bool migrate, string filePath);
        void SaveConnections(string saveFolderPath, string filename);
    }
}