using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCQueryConnect.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private const int FolderTabIndex = 3;
        private const int UpdateStoryTabIndex = 2;

        private PasswordSecurity _publishPasswordSecurity;
        private PublishArchitecture _publishArchitecture;
        private QueryData _selectedQueryData;
        private int _lastSelectedConnectionIndex;
        private int _lastSelectedFolderIndex = FolderTabIndex;
        private int _selectedTabIndex;
        private string _publishTabHeader;
        private string _updateMessage;

        public PasswordSecurity PublishPasswordSecurity
        {
            get => _publishPasswordSecurity;

            set
            {
                if (_publishPasswordSecurity != value)
                {
                    _publishPasswordSecurity = value;
                    OnPropertyChanged();
                }
            }
        }

        public PublishArchitecture PublishArchitecture
        {
            get => _publishArchitecture;

            set
            {
                if (_publishArchitecture != value)
                {
                    _publishArchitecture = value;
                    OnPropertyChanged();
                }
            }
        }

        public QueryData SelectedQueryData
        {
            get => _selectedQueryData;

            set
            {
                if (_selectedQueryData != value)
                {
                    _selectedQueryData = value;
                    OnPropertyChanged();

                    if (_selectedQueryData != null)
                    {
                        if (_selectedQueryData.IsFolder)
                        {
                            PublishTabHeader = "2. Publish";
                            SelectedTabIndex = _lastSelectedFolderIndex;
                        }
                        else
                        {
                            PublishTabHeader = "4. Publish";
                            SelectedTabIndex = _lastSelectedConnectionIndex;
                        }
                    }
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;

            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();

                    if (SelectedQueryData == null)
                    {
                        return;
                    }

                    if (SelectedQueryData.IsFolder)
                    {
                        _lastSelectedFolderIndex = _selectedTabIndex;
                    }
                    else
                    {
                        _lastSelectedConnectionIndex = _selectedTabIndex;
                    }
                }
            }
        }

        public string PublishTabHeader
        {
            get => _publishTabHeader;
            
            set
            {
                if (_publishTabHeader != value)
                {
                    _publishTabHeader = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdateMessage
        {
            get => _updateMessage;

            set
            {
                if (_updateMessage != value)
                {
                    _updateMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SelectUpdateTab()
        {
            SelectedTabIndex = SelectedQueryData.IsFolder
                ? FolderTabIndex
                : UpdateStoryTabIndex;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
