using System.Windows;

namespace SCQueryConnect.Converters
{
    public class ConnectionTypeCollapsedConverter : ConnectionTypeVisibilityConverter
    {
        protected override Visibility MatchVisibility { get; } = Visibility.Collapsed;
    }
}
