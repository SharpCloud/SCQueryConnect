using SCQueryConnect.ViewModels;

namespace SCQueryConnect.Models
{
    public class PublishSettings
    {
        public QueryData Data { get; set; }
        public ProxyViewModel ProxyViewModel { get; set; }
        public string BasePath { get; set; }
        public string Username { get; set; }
        public string SharpCloudUrl { get; set; }
        public PasswordSecurity PasswordSecurity { get; set; }
        public PublishArchitecture PublishArchitecture { get; set; }
    }
}
