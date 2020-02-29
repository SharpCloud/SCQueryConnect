using SCQueryConnect.Interfaces;
using System;

namespace SCQueryConnect.Commands
{
    public class ActionCommand<T> : IActionCommand
    {
        private readonly Action<T> _action;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public ActionCommand(
            Action<T> action,
            Func<T, bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute((T) parameter);
        }

        public void Execute(object parameter)
        {
            _action((T) parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }
}
