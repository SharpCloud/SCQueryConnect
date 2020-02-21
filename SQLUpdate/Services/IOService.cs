﻿using Microsoft.Win32;
using SCQueryConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SCQueryConnect.Services
{
    public class IOService : IIOService
    {
        private const string SaveDataExtension = ".scqc";
        private const string ConnectionsFileV3 = "connections.json";
        private const string ConnectionsFileV3Backup = "connections.json.bak";
        
        public const string ConnectionsFileV4 = "connections_v4" + SaveDataExtension;

        public string OutputRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SharpCloudQueryConnect");

        public string V3ConnectionsPath => Path.Combine(OutputRoot, ConnectionsFileV3);
        public string V3ConnectionsBackupPath => Path.Combine(OutputRoot, ConnectionsFileV3Backup);
        public string V4ConnectionsPath => Path.Combine(OutputRoot, ConnectionsFileV4);

        public string PromptForExportPath(string defaultFileName)
        {
            if (string.IsNullOrWhiteSpace(defaultFileName))
            {
                return string.Empty;
            }

            var dlg = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = $"SharpCloud QueryConnect Files (*{SaveDataExtension})|*{SaveDataExtension}"
            };

            return dlg.FileName;
        }

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

        public string ReadAllTextFromFile(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllTextToFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public void ExtractZipFileToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
        }
    }
}
