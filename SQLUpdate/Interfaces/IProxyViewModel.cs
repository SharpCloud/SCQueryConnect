namespace SCQueryConnect.Interfaces
{
    public interface IProxyViewModel
    {
        string Proxy { get; set; }
        bool ProxyAnonymous { get; set; }
        string ProxyUserName { get; set; }
        string ProxyPassword { get; set; }
    }
}
