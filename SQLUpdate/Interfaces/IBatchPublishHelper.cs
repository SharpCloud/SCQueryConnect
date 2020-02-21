using SCQueryConnect.Models;

namespace SCQueryConnect.Interfaces
{
    public interface IBatchPublishHelper
    {
        string GetBatchRunStartMessage(string name);
        string GetOrCreateOutputFolder(string queryName);
        void PublishBatchFolder(PublishSettings settings);
    }
}
