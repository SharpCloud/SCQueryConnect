using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SCQueryConnect.Common.Services
{
    public class CDataLicenceService : ICDataLicenceService
    {
        private readonly IDictionary<DatabaseType, string> _licenses;

        public CDataLicenceService()
        {
            _licenses = ReadLicenceData();
        }

        public string GetLicence(DatabaseType dbType)
        {
            var success = _licenses.TryGetValue(dbType, out var key);
            return success ? key : null;
        }

        private IDictionary<DatabaseType, string> ReadLicenceData()
        {
            var data = ReadLicenceText();

            var allKeys = data
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());

            var stringDictionary = allKeys
                .Select(str => str.Split('='))
                .ToDictionary(kvp => kvp[0], kvp => kvp[1], StringComparer.OrdinalIgnoreCase);

            var licenses = new Dictionary<DatabaseType, string>
            {
                [DatabaseType.Access] = stringDictionary["Access"],
                [DatabaseType.Excel] = stringDictionary["Excel"],
                [DatabaseType.SharePointList] = stringDictionary["SharePoint"]
            };

            return licenses;
        }

        private string ReadLicenceText()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SCQueryConnect.Common.CDataLicences.CDataLicences.txt";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    var result = reader.ReadToEnd();
                    return result;
                }
            }
        }
    }
}
