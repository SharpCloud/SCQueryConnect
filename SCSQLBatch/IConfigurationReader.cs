namespace SCSQLBatch
{
    public interface IConfigurationReader
    {
        string Get(string appSettingsKey);
    }
}
