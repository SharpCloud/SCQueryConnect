using SCQueryConnect.Interfaces;
using System.Windows;

namespace SCQueryConnect.Services
{
    public class MessageService : IMessageService
    {
        public MessageBoxResult Show(string messageBoxText)
        {
            return MessageBox.Show(messageBoxText);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return MessageBox.Show(messageBoxText, caption, button);
        }
    }
}
