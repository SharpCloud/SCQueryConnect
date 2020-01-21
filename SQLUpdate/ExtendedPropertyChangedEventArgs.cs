using System.ComponentModel;

namespace SCQueryConnect
{
    public class ExtendedPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public object OldValue { get; }
        public object NewValue { get; }

        public ExtendedPropertyChangedEventArgs(
            object oldValue,
            object newValue,
            string propertyName)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
