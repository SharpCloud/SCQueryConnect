using System.Windows.Input;

namespace SCQueryConnect.Interfaces
{
    public interface IActionCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}
