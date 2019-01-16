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

            try
            {
                var uri = new Uri(path, UriKind.Absolute);
                output = path;
            }
            catch (UriFormatException)
            {
                var dllPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(dllPath);
                output = Path.Combine(dir, path);
            }

            return output;
        }
    }
}
