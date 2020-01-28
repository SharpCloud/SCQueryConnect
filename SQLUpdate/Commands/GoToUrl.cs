using System;
using System.Diagnostics;
using System.Windows.Input;

namespace SCQueryConnect.Commands
{
    public class GoToUrl : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Process.Start((string)parameter);
        }

        #pragma warning disable 67
        public event EventHandler CanExecuteChanged;
        #pragma warning restore 67
    }
}
