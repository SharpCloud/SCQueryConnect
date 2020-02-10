namespace SCQueryConnect.Interfaces
{
    public interface IPasswordStorage
    {
        string LoadPassword(string key);
        void SavePassword(string key, string password);
    }
}
