using SCQueryConnect.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCQueryConnect.Models
{
    public class QueryBatch : IQueryItem
    {
        public const string RootId = "RootId";

        private bool _isExpanded;
        private bool _isSelected;
        private string _id;
        private string _name = "New Folder";
        private string _description;
        private ObservableCollection<IQueryItem> _connections = new ObservableCollection<IQueryItem>();
        private QueryBatch _parentFolder;

        public bool IsExpanded
        {
            get => _isExpanded;
            
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Id
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    _id = Guid.NewGuid().ToString();
                }

                return _id;
            }

            set => _id = value;
        }

        public string Name
        {
            get => _name;

            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;

            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<IQueryItem> Connections
        {
            get => _connections;

            set
            {
                if (_connections != value)
                {
                    _connections = value;
                    OnPropertyChanged();
                }
            }
        }

        public QueryBatch ParentFolder
        {
            get => _parentFolder;

            set
            {
                _parentFolder = value;
                ParentFolderId = _parentFolder.Id;
            }
        }

        public string ParentFolderId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
