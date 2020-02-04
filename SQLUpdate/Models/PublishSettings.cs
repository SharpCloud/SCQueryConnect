using SCQueryConnect.ViewModels;
using System.Windows.Controls;

namespace SCQueryConnect.Models
{
    public class PublishSettings
    {
        public QueryData Data { get; set; }
        public bool Is32Bit { get; set; }
        public PasswordBox Password { get; set; }
        public ProxyViewModel ProxyViewModel { get; set; }
        public string BasePath { get; set; }
        public string Username { get; set; }
        public string SharpCloudUrl { get; set; }
        public PasswordSecurity PasswordSecurity { get; set; }
    }
}
