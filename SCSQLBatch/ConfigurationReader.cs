using System.Configuration;

namespace SCSQLBatch
{
    public class ConfigurationReader : IConfigurationReader
    {
        public string Get(string appSettingsKey)
        {
            return ConfigurationManager.AppSettings[appSettingsKey];
        }
    }
}
