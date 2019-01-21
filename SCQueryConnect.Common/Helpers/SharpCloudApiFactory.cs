using SC.API.ComInterop;
using SCQueryConnect.Common.Interfaces;

namespace SCQueryConnect.Common.Helpers
{
    public class SharpCloudApiFactory : ISharpCloudApiFactory
    {
        public SharpCloudApi CreateSharpCloudApi(
            string username,
            string password,
            string url,
            string proxyUrl,
            bool useDefaultProxyCredentials,
            string proxyUsername,
            string proxyPassword)
        {
            var isValid = SharpCloudApi.UsernamePasswordIsValid(
                username,
                password,
                url,
                proxyUrl,
                useDefaultProxyCredentials,
                proxyUsername,
                proxyPassword);

            if (!isValid)
            {
                return null;
            }

            var api = new SharpCloudApi(
                username,
                password,
                url,
                proxyUrl,
                useDefaultProxyCredentials,
                proxyUsername,
                proxyPassword);

            return api;
        }
    }
}
