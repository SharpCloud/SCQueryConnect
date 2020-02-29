using SCQueryConnect.Commands;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;

namespace SCQueryConnect.ViewModels
{
    public class CommandsViewModel : ICommandsViewModel
    {
        public IActionCommand NewQueryFolder { get; }
        public IActionCommand MoveConnectionDown { get; }
        public IActionCommand MoveConnectionUp { get; }
        public IActionCommand CopyConnection { get; }
        public IActionCommand DeleteConnection { get; }
        public IActionCommand PublishBatchFolder { get; }
        public IActionCommand CancelStoryUpdate { get; }
        public IActionCommand TestConnection { get; }
        public IActionCommand RunQueryData { get; }

        public CommandsViewModel(IMainViewModel mainViewModel)
        {
            NewQueryFolder = new ActionCommand<QueryData>(
                qd => mainViewModel.CreateNewFolder(),
                qd => true);

            MoveConnectionDown = new ActionCommand<QueryData>(
                qd => mainViewModel.MoveConnectionDown(),
                qd => true);

            MoveConnectionUp = new ActionCommand<QueryData>(
                qd => mainViewModel.MoveConnectionUp(),
                qd => true);

            CopyConnection = new ActionCommand<QueryData>(
                qd => mainViewModel.CopyConnection(),
                qd => true);

            DeleteConnection = new ActionCommand<QueryData>(
                qd => mainViewModel.DeleteConnection(),
                qd => true);

            PublishBatchFolder = new ActionCommand<QueryData>(
                qd => mainViewModel.PublishBatchFolder(),
                qd => true);

            CancelStoryUpdate = new ActionCommand<QueryData>(
                qd => mainViewModel.CancelStoryUpdate(),
                qd => true);

            TestConnection = new ActionCommand<QueryData>(
                qd => mainViewModel.TestConnection(qd),
                qd => true);

            RunQueryData = new ActionCommand<QueryData>(
                qd => mainViewModel.RunQueryData(qd),
                qd => true);
        }
    }
}
