using System;
using System.IO;
using System.Reflection;

namespace SCQueryConnect.Common.Helpers
{
    public class PathHelper
    {
        public string GetAbsolutePath(string path)
        {
            string output;

            var isAbsolute =
                Path.IsPathRooted(path) &&
                !string.IsNullOrWhiteSpace(path) &&
                !Path.GetPathRoot(path).Equals(
                    Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal);

            if (!isAbsolute)
            {
                var dllPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(dllPath);
                output = Path.Combine(dir, path);
            }
            else
            {
                output = path;
            }

            return output;
        }
    }
}
