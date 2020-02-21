using SCQueryConnect.Interfaces;

namespace SCQueryConnect.ViewModels
{
    public class ProxyViewModel : IProxyViewModel
    {
        public string Proxy { get; set; }
        public bool ProxyAnnonymous { get; set; }
        public string ProxyUserName { get; set; }
        public string ProxyPassword { get; set; }
    }
}
