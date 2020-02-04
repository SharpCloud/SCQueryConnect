﻿using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace SCQueryConnect.Helpers
{
    public class BatchPublishHelper : IBatchPublishHelper
    {
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IEncryptionHelper _encryptionHelper;

        public BatchPublishHelper(
            IConnectionStringHelper connectionStringHelper,
            IEncryptionHelper encryptionHelper)
        {
            _connectionStringHelper = connectionStringHelper;
            _encryptionHelper = encryptionHelper;
        }

        public string GetFolder(string queryName, string basePath)
        {
            var folder = $"{basePath}/data";
            Directory.CreateDirectory(folder);
            folder += "/" + queryName;
            Directory.CreateDirectory(folder);
            return folder;
        }

        public void PublishBatchFolder(PublishSettings settings)
        {
            try
            {
                var outputFolder = GetFolder(settings.Data.Name, settings.BasePath);

                var notEmpty = Directory.EnumerateFileSystemEntries(outputFolder).Any();
                if (notEmpty)
                {
                    var result = MessageBox.Show(
                        $"A folder named '{settings.Data.Name}' already exist at this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                Directory.Delete(outputFolder, true);
                GetFolder(settings.Data.Name, settings.BasePath);

                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                PublishAllBatchFolders(settings.Data, string.Empty, sb, settings);
                WriteBatchFile(settings.Data.Name, settings.Data.Name, sb.ToString(), settings);
                Process.Start(outputFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {ex.Message}");
            }
        }

        private void PublishAllBatchFolders(
            QueryData queryData,
            string parentPath,
            StringBuilder parentStringBuilder,
            PublishSettings settings)
        {
            if (queryData.IsFolder)
            {
                var subPath = Path.Combine(parentPath, queryData.Name);
                var localStringBuilder = new StringBuilder();
                localStringBuilder.AppendLine("@echo off");

                parentStringBuilder.AppendLine($"echo Running: {queryData.Name}");

                foreach (var c in queryData.Connections)
                {
                    PublishAllBatchFolders(c, subPath, localStringBuilder, settings);

                    var batchFolderRoot = GetFolder(subPath, settings.BasePath);
                    var batchPath = Path.Combine(batchFolderRoot, c.Name, c.Name);
                    parentStringBuilder.AppendLine($"call \"{batchPath}.bat\"");
                }

                var content = localStringBuilder.ToString();
                WriteBatchFile(subPath, queryData.Name, content, settings);
            }
            else
            {
                GetFolder(parentPath, settings.BasePath);

                var fullPath = GenerateBatchExe(
                    queryData.ConnectionsString,
                    parentPath,
                    queryData,
                    settings);

                var suffix = GetFileSuffix(settings.Is32Bit);
                var filename = $"SCSQLBatch{suffix}.exe";
                parentStringBuilder.AppendLine($"echo Running: {queryData.Name}");
                parentStringBuilder.AppendLine($"\"{Path.Combine(fullPath, filename)}\"");
            }
        }

        private void WriteBatchFile(
            string path,
            string filename,
            string content,
            PublishSettings settings)
        {
            var sequenceFolder = GetFolder(path, settings.BasePath);
            var batchPath = Path.Combine(sequenceFolder, $"{filename}.bat");
            File.WriteAllText(batchPath, content);
        }

        private string GenerateBatchExe(
            string connectionString,
            string sequenceName,
            QueryData queryData,
            PublishSettings settings)
        {
            var suffix = GetFileSuffix(settings.Is32Bit);
            var zipfile = $"SCSQLBatch{suffix}.zip";

            var path = Path.Combine(sequenceName, queryData.Name);
            var outputFolder = GetFolder(path, settings.BasePath);

            try
            {
                var configFilename = outputFolder + $"/SCSQLBatch{suffix}.exe.config";

                if (File.Exists(configFilename))
                {
                    if (MessageBox.Show("Config files already exist in this location, Do you want to replace?", "WARNING", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return null;
                }

                if (connectionString.Contains("\""))
                    MessageBox.Show(
                        "Your connection string and/or query string contains '\"', which will automatically be replaced with '");
                try
                {
                    File.Delete($"{outputFolder}/Autofac.dll");
                    File.Delete($"{outputFolder}/Newtonsoft.Json.dll");
                    File.Delete($"{outputFolder}/SC.Framework.dll");
                    File.Delete($"{outputFolder}/SC.API.ComInterop.dll");
                    File.Delete($"{outputFolder}/SC.Api.dll");
                    File.Delete($"{outputFolder}/SC.SharedModels.dll");
                    File.Delete($"{outputFolder}/SCSQLBatch{suffix}.exe");
                    File.Delete($"{outputFolder}/SCSQLBatch{suffix}.exe.config");
                    File.Delete($"{outputFolder}/SCSQLBatch.zip");
                    File.Delete($"{outputFolder}/SCQueryConnect.Common.dll");

                    ZipFile.ExtractToDirectory(zipfile, outputFolder);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Sorry, we were unable to complete the process\r\rError: {e.Message}");
                    return null;
                }

                // set up the config

                // Remove data source if type is SharpCloud; a temp file will
                // be used, so an overwrite prompt will not appear

                var formattedConnection = queryData.GetBatchDBType == DatabaseStrings.SharpCloudExcel
                    ? _connectionStringHelper.SetDataSource(
                        queryData.FormattedConnectionString,
                        string.Empty)
                    : queryData.FormattedConnectionString;

                var passwordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(settings.Password.Password),
                    out var entropy,
                    DataProtectionScope.LocalMachine);

                var proxyPasswordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(settings.ProxyViewModel.ProxyPassword),
                    out var proxyEntropy,
                    DataProtectionScope.LocalMachine);

                var content = File.ReadAllText(configFilename);

                content = ReplaceConfigSetting(content, "USERID", settings.Username);
                content = ReplaceConfigSetting(content, "PASSWORD_DPAPI", Convert.ToBase64String(passwordBytes));
                content = ReplaceConfigSetting(content, "PASSWORD_DPAPI_ENTROPY", Convert.ToBase64String(entropy));
                content = ReplaceConfigSetting(content, "https://my.sharpcloud.com", settings.SharpCloudUrl);
                content = ReplaceConfigSetting(content, "00000000-0000-0000-0000-000000000000", queryData.StoryId);
                content = ReplaceConfigSetting(content, "SQL", queryData.GetBatchDBType);
                content = ReplaceConfigSetting(content, "CONNECTIONSTRING", formattedConnection.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYSTRING", queryData.QueryString.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "QUERYRELSSTRING", queryData.QueryStringRels.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'"));
                content = ReplaceConfigSetting(content, "LOGFILE", $"Logfile.txt");
                content = ReplaceConfigSetting(content, "BUILDRELATIONSHIPS", queryData.BuildRelationships.ToString());
                content = ReplaceConfigSetting(content, "UNPUBLISHITEMS", queryData.UnpublishItems.ToString());
                content = ReplaceConfigSetting(content, "PROXYADDRESS", settings.ProxyViewModel.Proxy);
                content = ReplaceConfigSetting(content, "PROXYANONYMOUS", settings.ProxyViewModel.ProxyAnnonymous.ToString());
                content = ReplaceConfigSetting(content, "PROXYUSERNAME", settings.ProxyViewModel.ProxyUserName);
                content = ReplaceConfigSetting(content, "PROXYPWORD_DPAPI", Convert.ToBase64String(proxyPasswordBytes));
                content = ReplaceConfigSetting(content, "PROXYPWORD_DPAPI_ENTROPY", Convert.ToBase64String(proxyEntropy));

                File.WriteAllText(configFilename, content);

                // update the Logfile
                var logfile = $"{outputFolder}Logfile.txt";
                var contentNotes = new List<string>();
                contentNotes.Add($"----------------------------------------------------------------------");
                contentNotes.Add(settings.Is32Bit
                    ? $"32 bit (x86) Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}"
                    : $"64 bit Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}");
                contentNotes.Add($"----------------------------------------------------------------------");

                File.AppendAllLines(logfile, contentNotes);
                return outputFolder;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            return null;
        }

        private static string ReplaceConfigSetting(
            string configText,
            string oldValue,
            string newValue)
        {
            var updated = configText.Replace($"\"{oldValue}\"", $"\"{newValue}\"");
            return updated;
        }

        private static string GetFileSuffix(bool b32bit) => b32bit ? "x86" : string.Empty;
    }
}
