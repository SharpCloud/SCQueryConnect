using System.Collections.Generic;
using System.IO;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IIOService
    {
        string OutputRoot { get; }
        string V3ConnectionsPath { get; }
        string V3ConnectionsBackupPath { get; }
        string V4ConnectionsPath { get; }

        DirectoryInfo CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> EnumerateFileSystemEntries(string path);

        void AppendAllLinesToFile(string path, IEnumerable<string> contents);
        void DeleteFile(string path);
        bool FileExists(string path);
        void MoveFile(string sourceFileName, string destFileName);
        string ReadAllTextFromFile(string path);
        void WriteAllTextToFile(string path, string contents);
    }
}
