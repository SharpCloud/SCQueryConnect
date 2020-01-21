using SCQueryConnect.Interfaces;
using System.Windows;

namespace SCQueryConnect.Helpers
{
    public class MessageService : IMessageService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
