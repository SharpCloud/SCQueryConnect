namespace SCQueryConnect.Interfaces
{
    public interface IProxyViewModel
    {
        string Proxy { get; set; }
        bool ProxyAnnonymous { get; set; }
        string ProxyUserName { get; set; }
        string ProxyPassword { get; set; }
    }
}
