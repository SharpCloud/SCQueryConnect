using SCQueryConnect.Models;

namespace SCQueryConnect.Interfaces
{
    public interface IBatchPublishHelper
    {
        string GetFolder(string queryName, string basePath);
        void PublishBatchFolder(PublishSettings settings);
    }
}
