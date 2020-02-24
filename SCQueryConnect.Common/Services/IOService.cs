using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace SCQueryConnect.Common.Services
{
    public class IOService : IIOService
    {
        private const string ConnectionsFileV3 = "connections.json";
        private const string ConnectionsFileV3Backup = "connections.json.bak";
        
        public const string ConnectionsFileV4 = "connections_v4" + SaveDataExtension;
        public const string SaveDataExtension = ".scqc";

        public string OutputRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SharpCloudQueryConnect");

        public string V3ConnectionsPath => Path.Combine(OutputRoot, ConnectionsFileV3);
        public string V3ConnectionsBackupPath => Path.Combine(OutputRoot, ConnectionsFileV3Backup);
        public string V4ConnectionsPath => Path.Combine(OutputRoot, ConnectionsFileV4);

        public DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            return Directory.EnumerateFileSystemEntries(path);
        }

        public void AppendAllLinesToFile(string path, IEnumerable<string> contents)
        {
            File.AppendAllLines(path, contents);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void MoveFile(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public string ReadAllTextFromFile(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllTextToFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
    }
}
