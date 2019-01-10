namespace SCQueryConnect.Common.Models
{
    public class SharpCloudConfiguration
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string ProxyUrl { get; set; }
        public bool UseDefaultProxyCredentials { get; set; }
        public string ProxyUserName { get; set; }
        public string ProxyPassword { get; set; }
    }
}
