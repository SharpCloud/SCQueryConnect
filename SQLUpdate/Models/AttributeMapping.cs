using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCQueryConnect.Models
{
    public class AttributeMapping : INotifyPropertyChanged
    {
        private bool _isBrokenMapping;

        public string SourceName { get; set; }
        public AttributeDesignations Target { get; set; }

        public bool IsBrokenMapping
        {
            get => _isBrokenMapping;

            set
            {
                if (_isBrokenMapping != value)
                {
                    _isBrokenMapping = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
