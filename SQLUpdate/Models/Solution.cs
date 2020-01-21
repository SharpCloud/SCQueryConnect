using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCQueryConnect.Models
{
    public class Solution : INotifyPropertyChanged
    {
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(
            object oldValue,
            object newValue,
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs(
                oldValue,
                newValue,
                propertyName));
        }
    }
}
