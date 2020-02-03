using System.Windows;

namespace SCQueryConnect.Converters
{
    public class ConnectionTypeVisibleConverter : ConnectionTypeVisibilityConverter
    {
        protected override Visibility MatchVisibility { get; } = Visibility.Visible;
    }
}
