using System;
using System.Windows;

namespace SCQueryConnect.Interfaces
{
    public interface IMessageService
    {
        Window Owner { get; set; }

        MessageBoxResult Show(string messageBoxText);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button);
        bool? Show<TWindow>() where TWindow : Window, new();
        bool? Show<TWindow>(Func<TWindow> createWindow) where TWindow : Window;
        bool? Show<TWindow>(Func<TWindow> createWindow, out TWindow window) where TWindow : Window;
    }
}
