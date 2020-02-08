using SCQueryConnect.Models;

namespace SCQueryConnect.Interfaces
{
    public interface IBatchPublishHelper
    {
        string GetOrCreateOutputFolder(string queryName, string basePath);
        void PublishBatchFolder(PublishSettings settings);
    }
}
