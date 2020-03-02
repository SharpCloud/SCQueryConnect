using SCQueryConnect.Interfaces;
using System;
using System.Windows;

namespace SCQueryConnect.Services
{
    public class MessageService : IMessageService
    {
        public Window Owner { get; set; }

        public MessageBoxResult Show(string messageBoxText)
        {
            return MessageBox.Show(messageBoxText);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return MessageBox.Show(messageBoxText, caption, button);
        }

        public bool? Show<TWindow>() where TWindow : Window, new()
        {
            var result = Show(() => new TWindow());
            return result;
        }

        public bool? Show<TWindow>(Func<TWindow> createWindow) where TWindow : Window
        {
            var result = Show(createWindow, out _);
            return result;
        }

        public bool? Show<TWindow>(Func<TWindow> createWindow, out TWindow window) where TWindow : Window
        {
            window = createWindow();
            window.Owner = Owner;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = window.ShowDialog();
            return result;
        }
    }
}
