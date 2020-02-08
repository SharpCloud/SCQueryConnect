using System.Collections.Generic;
using System.IO;

namespace SCQueryConnect.Interfaces
{
    public interface IIOService
    {
        DirectoryInfo CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> EnumerateFileSystemEntries(string path);

        void AppendAllLinesToFile(string path, IEnumerable<string> contents);
        void DeleteFile(string path);
        bool FileExists(string path);
        string ReadAllTextFromFile(string path);
        void WriteAllTextToFile(string path, string contents);

        void ExtractZipFileToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
    }
}
