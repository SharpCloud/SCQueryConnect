using System.Windows;

namespace SCQueryConnect.Interfaces
{
    public interface IMessageService
    {
        MessageBoxResult Show(string messageBoxText);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button);
    }
}
