using SC.API.ComInterop;

namespace SCQueryConnect.Common.Interfaces
{
    public interface ISharpCloudApiFactory
    {
        SharpCloudApi CreateSharpCloudApi(
            string username,
            string password,
            string url,
            string proxyURL,
            bool useDefaultProxyCredentials,
            string proxyUsername,
            string proxyPassword);
    }
}
