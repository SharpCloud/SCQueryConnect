using System.Collections.Generic;
using System.IO;

namespace SCQueryConnect.Interfaces
{
    public interface IIOService
    {
        string OutputRoot { get; }
        string V3ConnectionsPath { get; }
        string V3ConnectionsBackupPath { get; }
        string V4ConnectionsPath { get; }

        string PromptForExportPath(string defaultFileName);

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
