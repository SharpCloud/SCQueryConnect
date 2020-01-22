using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCQueryConnect.Models
{
    public class Solution : INotifyPropertyChanged
    {
        private ObservableCollection<string> _connectionIds = new ObservableCollection<string>();
        private string _name = "Solution";

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;

            set
            {
                if (_name != value)
                {
                    var oldValue = _name;
                    _name = value;
                    OnPropertyChanged(oldValue, _name);
                }
            }
        }

        public ObservableCollection<string> ConnectionIds
        {
            get => _connectionIds;

            set
            {
                if (_connectionIds != value)
                {
                    _connectionIds = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            object oldValue = null,
            object newValue = null,
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs(
                oldValue,
                newValue,
                propertyName));
        }
    }
}
