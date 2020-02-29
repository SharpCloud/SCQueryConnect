namespace SCQueryConnect.Interfaces
{
    public interface ICommandsViewModel
    {
        IActionCommand NewQueryFolder { get; }
        IActionCommand MoveConnectionDown { get; }
        IActionCommand MoveConnectionUp { get; }
        IActionCommand CopyConnection { get; }
        IActionCommand DeleteConnection { get; }
        IActionCommand PublishBatchFolder { get; }
        IActionCommand CancelStoryUpdate { get; }
        IActionCommand TestConnection { get; }
        IActionCommand RunQueryData { get; }
    }
}
