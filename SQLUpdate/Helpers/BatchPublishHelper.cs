using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly IIOService _ioService;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;

        public BatchPublishHelper(
            IConnectionStringHelper connectionStringHelper,
            IEncryptionHelper encryptionHelper,
            IIOService ioService,
            IMessageService messageService,
            IPasswordStorage passwordStorage)
        {
            _connectionStringHelper = connectionStringHelper;
            _encryptionHelper = encryptionHelper;
            _ioService = ioService;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
        }

        public string GetBatchRunStartMessage(string name) => $"- Running '{name}'...";

        public string GetOrCreateOutputFolder(string queryName)
        {
            var folder = Path.Combine(_ioService.OutputRoot, "data");
            _ioService.CreateDirectory(folder);
            folder = Path.Combine(folder, queryName);
            _ioService.CreateDirectory(folder);
            return folder;
        }

        public void PublishBatchFolder(PublishSettings settings)
        {
            try
            {
                var outputFolder = GetOrCreateOutputFolder(settings.Data.Name);

                var notEmpty = _ioService.EnumerateFileSystemEntries(outputFolder).Any();
                if (notEmpty)
                {
                    var result = _messageService.Show(
                        $"A folder named '{settings.Data.Name}' already exist at this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                _ioService.DeleteDirectory(outputFolder, true);
                GetOrCreateOutputFolder(settings.Data.Name);

                PublishAllBatchFolders(settings.Data, string.Empty, null, settings);
                Process.Start(outputFolder);
            }
            catch (Exception ex)
            {
                _messageService.Show($"Sorry, we were unable to complete the process\r\rError: {ex.Message}");
            }
        }

        private void PublishAllBatchFolders(
            QueryData queryData,
            string parentPath,
            StringBuilder parentStringBuilder,
            PublishSettings settings)
        {
            var message = GetBatchRunStartMessage(queryData.Name);

            if (queryData.IsFolder)
            {
                var subPath = Path.Combine(parentPath, queryData.Name);
                var filename = $"{queryData.Name}.bat";
                var outputFolder = GetOrCreateOutputFolder(subPath);
                var batchFilePath = Path.Combine(outputFolder, filename);
                
                parentStringBuilder?.AppendLine($"echo {message}");
                parentStringBuilder?.AppendLine($"call \"{batchFilePath}\"");

                var localStringBuilder = new StringBuilder();
                localStringBuilder.AppendLine("@echo off");

                foreach (var c in queryData.Connections)
                {
                    PublishAllBatchFolders(c, subPath, localStringBuilder, settings);
                }

                var content = localStringBuilder.ToString();
                _ioService.WriteAllTextToFile(batchFilePath, content);
            }
            else
            {
                GetOrCreateOutputFolder(parentPath);

                var fullPath = GenerateBatchExe(
                    queryData.ConnectionsString,
                    parentPath,
                    queryData,
                    settings);

                var suffix = GetFileSuffix(settings);
                var filename = $"SCSQLBatch{suffix}.exe";

                parentStringBuilder?.AppendLine($"echo {message}");
                parentStringBuilder?.AppendLine($"\"{Path.Combine(fullPath, filename)}\"");
            }
        }

        private string GenerateBatchExe(
            string connectionString,
            string sequenceName,
            QueryData queryData,
            PublishSettings settings)
        {
            var suffix = GetFileSuffix(settings);
            var zipfile = $"SCSQLBatch{suffix}.zip";

            var path = Path.Combine(sequenceName, queryData.Name);
            var outputFolder = GetOrCreateOutputFolder(path);

            try
            {
                var configFilename = Path.Combine(outputFolder, $"SCSQLBatch{suffix}.exe.config");

                if (_ioService.FileExists(configFilename))
                {
                    var result = _messageService.Show(
                        "Config files already exist in this location, Do you want to replace?",
                        "WARNING",
                        MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return null;
                }

                if (connectionString.Contains("\""))
                {
                    _messageService.Show(
                        "Your connection string and/or query string contains '\"', which will automatically be replaced with '");
                }

                try
                {
                    _ioService.DeleteFile($"{outputFolder}/Autofac.dll");
                    _ioService.DeleteFile($"{outputFolder}/Newtonsoft.Json.dll");
                    _ioService.DeleteFile($"{outputFolder}/SC.Framework.dll");
                    _ioService.DeleteFile($"{outputFolder}/SC.API.ComInterop.dll");
                    _ioService.DeleteFile($"{outputFolder}/SC.Api.dll");
                    _ioService.DeleteFile($"{outputFolder}/SC.SharedModels.dll");
                    _ioService.DeleteFile($"{outputFolder}/SCSQLBatch{suffix}.exe");
                    _ioService.DeleteFile($"{outputFolder}/SCSQLBatch{suffix}.exe.config");
                    _ioService.DeleteFile($"{outputFolder}/SCSQLBatch.zip");
                    _ioService.DeleteFile($"{outputFolder}/SCQueryConnect.Common.dll");

                    _ioService.ExtractZipFileToDirectory(zipfile, outputFolder);
                }
                catch (Exception e)
                {
                    _messageService.Show($"Sorry, we were unable to complete the process\r\rError: {e.Message}");
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

                var passwordBytes = GetPasswordBytes(
                    settings.PasswordSecurity,
                    PasswordStorage.Password,
                    out var entropy);

                var proxyPasswordBytes = GetPasswordBytes(
                    settings.PasswordSecurity,
                    PasswordStorage.ProxyPassword,
                    out var proxyEntropy);

                var content = _ioService.ReadAllTextFromFile("BatchConfigTemplate.xml");

                var appSettings = new List<string>
                {
                    GetAppSettingText(Constants.BatchUserIdKey, settings.Username),
                    GetAppSettingText(Constants.BatchUrlKey, settings.SharpCloudUrl),
                    GetAppSettingText(Constants.BatchStoryIdKey, queryData.StoryId),
                    GetAppSettingText(Constants.BatchDBTypeKey, queryData.GetBatchDBType),
                    GetAppSettingText(Constants.BatchConnectionStringKey, Sanitize(formattedConnection)),
                    GetAppSettingText(Constants.BatchQueryStringKey, Sanitize(queryData.QueryString)),
                    GetAppSettingText(Constants.BatchQueryStringRelsKey, Sanitize(queryData.QueryStringRels)),
                    GetAppSettingText(Constants.BatchQueryStringPanelsKey, Sanitize(queryData.QueryStringPanels)),
                    GetAppSettingText(Constants.BatchQueryStringResourceUrlsKey , Sanitize(queryData.QueryStringResourceUrls)),
                    "<!-- Add a path to log file if required - leave blank for no logging -->",
                    GetAppSettingText(Constants.BatchLogFileKey, "Logfile.txt"),
                    GetAppSettingText(Constants.BatchBuildRelationshipsKey, queryData.BuildRelationships.ToString()),
                    GetAppSettingText(Constants.BatchUnpublishItemsKey, queryData.UnpublishItems.ToString()),
                    GetAppSettingText(Constants.BatchProxyKey, settings.ProxyViewModel.Proxy),
                    GetAppSettingText(Constants.BatchProxyAnonymousKey, settings.ProxyViewModel.ProxyAnnonymous.ToString()),
                    GetAppSettingText(Constants.BatchProxyUsernameKey, settings.ProxyViewModel.ProxyUserName),
                };

                string[] passwords;
                if (settings.PasswordSecurity == PasswordSecurity.Base64)
                {
                    passwords = new[]
                    {
                        GetAppSettingText(Constants.BatchPassword64Key, Convert.ToBase64String(passwordBytes)),
                        GetAppSettingText(Constants.BatchProxyPassword64Key, Convert.ToBase64String(entropy))
                    };
                }
                else
                {
                    passwords = new[]
                    {
                        GetAppSettingText(Constants.BatchPasswordDpapiKey, Convert.ToBase64String(passwordBytes)),
                        GetAppSettingText(Constants.BatchPasswordDpapiEntropyKey, Convert.ToBase64String(entropy)),
                        GetAppSettingText(Constants.BatchProxyPasswordDpapiKey, Convert.ToBase64String(proxyPasswordBytes)),
                        GetAppSettingText(Constants.BatchProxyPasswordDpapiEntropyKey, Convert.ToBase64String(proxyEntropy))
                    };
                }

                appSettings.AddRange(passwords);
                var indented = appSettings.Select(s => $"    {s}");
                var appSettingsString = string.Join(Environment.NewLine, indented);
                var configText = content.Replace("{appSettings}", appSettingsString);
                _ioService.WriteAllTextToFile(configFilename, configText);

                // update the Logfile
                var logfile = $"{outputFolder}Logfile.txt";
                var contentNotes = new List<string>
                {
                    "----------------------------------------------------------------------",
                    GetIs32Bit(settings)
                        ? $"32 bit (x86) Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}"
                        : $"64 bit Batch files created at {DateTime.Now:dd MMM yyyy HH:mm}",
                    "----------------------------------------------------------------------"
                };
                _ioService.AppendAllLinesToFile(logfile, contentNotes);
                return outputFolder;
            }
            catch (Exception exception)
            {
                _messageService.Show(exception.Message);
            }

            return null;
        }

        private byte[] GetPasswordBytes(
            PasswordSecurity security,
            string key,
            out byte[] entropy)
        {
            byte[] passwordBytes;

            if (security == PasswordSecurity.DpapiUser)
            {
                passwordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(
                        _passwordStorage.LoadPassword(key)),
                    out entropy,
                    DataProtectionScope.CurrentUser);
            }
            else if (security == PasswordSecurity.DpapiMachine)
            {
                passwordBytes = _encryptionHelper.Encrypt(
                    _encryptionHelper.TextEncoding.GetBytes(
                        _passwordStorage.LoadPassword(key)),
                    out entropy,
                    DataProtectionScope.LocalMachine);
            }
            else
            {
                passwordBytes = _encryptionHelper.TextEncoding.GetBytes(
                    _passwordStorage.LoadPassword(key));

                entropy = new byte[0];
            }

            return passwordBytes;
        }

        private static bool GetIs32Bit(PublishSettings settings)
        {
            switch (settings.PublishArchitecture)
            {
                case PublishArchitecture.X64:
                    return false;

                case PublishArchitecture.X32:
                    return true;

                default:
                    return IntPtr.Size == 4;
            }
        }

        private static string GetFileSuffix(PublishSettings settings)
        {
            var is32Bit = GetIs32Bit(settings);
            var suffix = is32Bit ? "x86" : string.Empty;
            return suffix;
        }

        private static string GetAppSettingText(string key, string value)
            => $"<add key=\"{key}\" value=\"{value}\" />";

        private static string Sanitize(string input) => input == null
            ? string.Empty
            : input.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
    }
}
