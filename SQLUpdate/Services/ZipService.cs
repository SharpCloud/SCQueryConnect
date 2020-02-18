using SCQueryConnect.Interfaces;
using System.IO.Compression;

namespace SCQueryConnect.Services
{
    public class ZipService : IZipService
    {
        public void ExtractZipFileToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
        }
    }
}
