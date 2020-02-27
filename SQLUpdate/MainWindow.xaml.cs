using Microsoft.Win32;
using Newtonsoft.Json;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Interfaces.DataValidation;
using SCQueryConnect.Common.Models;
using SCQueryConnect.Controls;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Logging;
using SCQueryConnect.Models;
using SCQueryConnect.Services;
using SCQueryConnect.ViewModels;
using SCQueryConnect.Views;
using SQLUpdate.Views;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCQueryConnect
{
    // some useful links... https://www.microsoft.com/en-us/download/details.aspx?id=13255 for accessing the access db

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _startPoint;

        public string AppName
        {
            get { return _qcHelper.AppName; }
        }
        
        
        private readonly IBatchPublishHelper _batchPublishHelper;
        private readonly IQueryConnectHelper _qcHelper;
        private readonly IConnectionStringHelper _connectionStringHelper;
        private readonly IExcelWriter _excelWriter;
        private readonly MultiDestinationLogger _logger;
        private readonly IIOService _ioService;
        private readonly IMainViewModel _mainViewModel;
        private readonly IMessageService _messageService;
        private readonly IPasswordStorage _passwordStorage;
        private readonly IProxyViewModel _proxyViewModel;
        private readonly ISharpCloudApiFactory _sharpCloudApiFactory;
        private readonly RichTextBoxLoggingDestination _storyLoggingDestination;
        private readonly RichTextBoxLoggingDestination _folderLoggingDestination;

        public MainWindow(
            IBatchPublishHelper batchPublishHelper,
            IConnectionStringHelper connectionStringHelper,
            IExcelWriter excelWriter,
            IIOService ioService,
            IMainViewModel mainViewModel,
            IMessageService messageService,
            IPasswordStorage passwordStorage,
            IProxyViewModel proxyViewModel,
            ISharpCloudApiFactory sharpCloudApiFactory,
            ILog logger,
            IQueryConnectHelper qcHelper)
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            DataContext = this;

            _batchPublishHelper = batchPublishHelper;
            _connectionStringHelper = connectionStringHelper;
            _excelWriter = excelWriter;
            _ioService = ioService;
            _mainViewModel = mainViewModel;
            _messageService = messageService;
            _passwordStorage = passwordStorage;
            _proxyViewModel = proxyViewModel;
            _sharpCloudApiFactory = sharpCloudApiFactory;

            _folderLoggingDestination = new RichTextBoxLoggingDestination(FolderUpdateLogOutput);
            _storyLoggingDestination = new RichTextBoxLoggingDestination(StoryUpdateLogOutput);

            _logger = (MultiDestinationLogger) logger;
            _logger.SetPersistentDestination(_folderLoggingDestination, _storyLoggingDestination);

            _qcHelper = qcHelper;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainViewModel.LoadApplicationState();
            }
            catch (JsonReaderException ex)
            {
                var msg = $"Error occurred reading saved connections at {_ioService.V4ConnectionsPath}" +
                          Environment.NewLine +
                          Environment.NewLine +
                          "Click OK to exit QueryConnect" +
                          Environment.NewLine +
                          Environment.NewLine +
                          ex.Message;

                _messageService.Show(msg);
                throw;
            }

            Password.Password = _passwordStorage.LoadPassword(PasswordStorage.Password);

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(SelectAllOnTabFocus));

            EventManager.RegisterClassHandler(
                typeof(PasswordBox),
                GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(SelectAllOnTabFocus));

            var splashScreen = new SplashScreen("Images/splash.jpg");
            splashScreen.Show(true);
        }

        private void SelectAllOnTabFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Tab))
            {
                if (sender is TextBox textBox && !textBox.IsReadOnly)
                {
                    textBox.SelectAll();
                }
                else if (sender is PasswordBox passwordBox)
                {
                    passwordBox.SelectAll();
                }
            }
        }

        private void RewriteDataSourceClick(object sender, RoutedEventArgs e)
        {
            RewriteDataSource(_mainViewModel.SelectedQueryData);
        }

        private void RewriteDataSource(QueryData queryData)
        {
            var key = queryData.ConnectionType == DatabaseType.Excel
                ? DatabaseStrings.ExcelFileKey
                : DatabaseStrings.DataSourceKey;

            var filePath = _connectionStringHelper.GetVariable(
                queryData.FormattedConnectionString,
                key);

            try
            {
                _excelWriter.RewriteExcelFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not rewrite Excel data source! {ex.Message}");
            }
        }

        private async void TestConnectionClick(object sender, RoutedEventArgs e)
        {
            await TestConnection(_mainViewModel.SelectedQueryData);
        }

        private async Task TestConnection(QueryData queryData)
        {
            if (queryData.ConnectionType == DatabaseType.SharpCloudExcel)
            {
                try
                {
                    await _qcHelper.InitialiseDatabase(
                        _mainViewModel.GetApiConfiguration(),
                        queryData.FormattedConnectionString,
                        queryData.ConnectionType);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not connect to SharpCloud! {ex.Message}");
                    return;
                }
            }

            try
            {
                using (IDbConnection connection = _mainViewModel.GetDb(queryData))
                {
                    connection.Open();
                    MessageBox.Show("Hooray! It looks like it's worked!");
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not connect to database! " + ex.Message);
            }
        }

        private void ReviewConnectionClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ConnectionInfo(_mainViewModel.SelectedQueryData)
            {
                Owner = this
            };
            dlg.ShowDialog();
        }

        private void ProcessRunException(Exception e)
        {
            switch (e)
            {
                case InvalidOperationException ex when ex.Message.Contains(Constants.AccessDBEngineErrorMessage):
                    var msgbox = new DatabaseErrorMessage { Owner = this };
                    msgbox.ShowDialog();
                    break;

                default:
                    MessageBox.Show($"There was an error: {e.Message}");
                    break;
            }
        }

        private async void QueryEditorSqlPreviewClick(object sender, EventArgs e)
        {
            await PreviewSql();
        }

        private async Task PreviewSql()
        {
            try
            {
                await _mainViewModel.PreviewSql();
            }
            catch (Exception e)
            {
                ProcessRunException(e);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            _passwordStorage.SavePassword(PasswordStorage.Password, Password.Password);
            _mainViewModel.SaveApplicationState();
        }

        private void ViewExisting(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                var path = _batchPublishHelper.GetOrCreateOutputFolder(
                    queryData.Name);

                Process.Start(path);
            }
        }

        private void NewConnectionClick(object sender, RoutedEventArgs e)
        {
            var newWnd = new SelectDatabaseType
            {
                Owner = this
            };

            if (newWnd.ShowDialog() != true)
            {
                return;
            }

            _mainViewModel.CreateNewConnection(newWnd.SelectedButton);
        }

        private void NewQueryFolderClick(object sender, RoutedEventArgs e)
        {
            _mainViewModel.CreateNewFolder();
        }

        private void MoveConnectionDown(object sender, RoutedEventArgs e)
        {
            _mainViewModel.MoveConnectionDown();
        }

        private void MoveConnectionUp(object sender, RoutedEventArgs e)
        {
            _mainViewModel.MoveConnectionUp();
        }

        private void CopyConnectionClick(object sender, RoutedEventArgs e)
        {
            _mainViewModel.CopyConnection();
        }

        private void DeleteConnectionClick(object sender, RoutedEventArgs e)
        {
            _mainViewModel.DeleteConnection();
        }

        private async void TreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _mainViewModel.SelectedQueryData = e.NewValue as QueryData;

            if (_mainViewModel.SelectedQueryData == null)
            {
                return;
            }

            if (_mainViewModel.SelectedQueryData.IsFolder)
            {
                await _folderLoggingDestination.SetLogText(
                    _mainViewModel.SelectedQueryData.LogData);
            }
            else
            {
                await _storyLoggingDestination.SetLogText(
                    _mainViewModel.SelectedQueryData.LogData);
            }
        }

        private void BrowseForDataSourceClick(object sender, RoutedEventArgs e)
        {
            var ord = new OpenFileDialog();

            switch (_mainViewModel.SelectedQueryData.ConnectionType)
            {
                case DatabaseType.Access:
                    ord.Filter = "Access Database Files (*.accdb;*.mdb)|*.accdb;*.mdb";
                    break;

                case DatabaseType.Excel:
                case DatabaseType.SharpCloudExcel:
                    ord.Filter = "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx";
                    break;
            }

            if (ord.ShowDialog() == true)
            {
                _mainViewModel.SelectedQueryData.FileName = ord.FileName;
            }
        }

        private async void SelectStoryClick(object sender, RoutedEventArgs e)
        {
            var api = _sharpCloudApiFactory.CreateSharpCloudApi(
                _mainViewModel.Username,
                Password.Password,
                _mainViewModel.Url,
                _proxyViewModel.Proxy,
                _proxyViewModel.ProxyAnonymous,
                _proxyViewModel.ProxyUserName,
                _proxyViewModel.ProxyPassword);

            if (api == null)
            {
                await _logger.Log(InvalidCredentialsException.LoginFailed);
                return;
            }

            var sel = new SelectStory(api, false, _mainViewModel.Username);

            bool? dialogResult = sel.ShowDialog();
            if (dialogResult == true)
            {
                var story = sel.SelectedStoryLites.First();
                _mainViewModel.SelectedQueryData.StoryId = story.Id;
            }
        }

        private void ViewStoryClick(object sender, RoutedEventArgs e)
        {
            Process.Start($"{_mainViewModel.Url}/html/#/story/{_mainViewModel.SelectedQueryData.StoryId}");
        }

        private void App_Directory_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
        }

        private void Proxy_OnClick(object sender, RoutedEventArgs e)
        {
            var proxy = new ProxySettings(_proxyViewModel);
            proxy.ShowDialog();
        }

        private void StorySourceSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SourceStorySettings(_mainViewModel.SelectedQueryData)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }

        private void QC_Data_Folder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", _ioService.OutputRoot);
        }

        private void PublishBatchFolderClick(object sender, RoutedEventArgs e)
        {
            _mainViewModel.PublishBatchFolder();
        }

        private void QueryItemTreePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
        }

        private void QueryItemTreeMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(this);
                var diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (e.OriginalSource is FrameworkElement element &&
                        element.DataContext is QueryData item)
                    {
                        var dragData = new DataObject(item);
                        DragDrop.DoDragDrop(element, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        private void QueryItemTreeDragEnter(object sender, DragEventArgs e)
        {
            if (GetConfigFileDataPath(e.Data) != null)
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else if (e.Data.GetData(typeof(QueryData)) is QueryData source &&
                     e.OriginalSource is FrameworkElement fe &&
                     fe.DataContext is QueryData dropTarget &&
                     source != dropTarget)
            {
                if (dropTarget.IsFolder)
                {
                    dropTarget.DragInto = true;
                }
                else
                {
                    var mousePos = e.GetPosition(this);
                    var diff = _startPoint - mousePos;

                    if (diff.Y > 0)
                    {
                        dropTarget.DragAbove = true;
                    }
                    else if (diff.Y < 0)
                    {
                        dropTarget.DragBelow = true;
                    }
                }

                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void QueryItemTreeDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(QueryData)) is QueryData source &&
                e.OriginalSource is FrameworkElement fe)
            {
                var dropTarget = fe.DataContext as QueryData;

                if (source == dropTarget ||
                    _mainViewModel.FindParent(dropTarget) == source)
                {
                    ResetDragDropHighlight(dropTarget);
                    return;
                }

                var index = 0;
                var originalSourceParent = _mainViewModel.FindParent(source);
                originalSourceParent.Connections.Remove(source);
                QueryData updatedSourceParent;

                if (dropTarget != null)
                {
                    ResetDragDropHighlight(dropTarget);

                    if (dropTarget.IsFolder)
                    {
                        updatedSourceParent = dropTarget;
                    }
                    else
                    {
                        var dropParent = _mainViewModel.FindParent(dropTarget) ??
                                         _mainViewModel.QueryRootNode;

                        index = dropParent.Connections.IndexOf(dropTarget);
                        updatedSourceParent = dropParent;
                    }
                }
                else
                {
                    index = _mainViewModel.QueryRootNode.Connections.Count;
                    updatedSourceParent = _mainViewModel.QueryRootNode;
                }

                var mousePos = e.GetPosition(this);

                if (mousePos.Y > _startPoint.Y)
                {
                    index++;
                }

                if (index > updatedSourceParent.Connections.Count)
                {
                    updatedSourceParent.Connections.Add(source);
                }
                else
                {
                    updatedSourceParent.Connections.Insert(index, source);
                }

                updatedSourceParent.IsExpanded = true;
            }
            else if (GetConfigFileDataPath(e.Data) is string path)
            {
                _mainViewModel.ImportConnections(path);
            }
        }

        private static string GetConfigFileDataPath(IDataObject obj)
        {
            string path = null;

            if (obj is DataObject dataObject &&
                dataObject.ContainsFileDropList())
            {
                var dropList = dataObject.GetFileDropList();

                path = dropList.OfType<string>().FirstOrDefault(s =>
                    string.Equals(
                        Path.GetExtension(s),
                        ".json",
                        StringComparison.OrdinalIgnoreCase));
            }

            return path;
        }

        private async void RunQueryDataClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                await _mainViewModel.RunQueryData(queryData);
            }
        }

        private void QueryItemTreeDragLeave(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                queryData.DragAbove = false;
                queryData.DragBelow = false;
                queryData.DragInto = false;
            }
        }

        private static void ResetDragDropHighlight(QueryData queryData)
        {
            queryData.DragAbove = false;
            queryData.DragBelow = false;
            queryData.DragInto = false;
        }

        private async void CancelStoryUpdate(object sender, RoutedEventArgs e)
        {
            await _mainViewModel.CancelStoryUpdate();
        }

        private void PasswordOnPasswordChanged(object sender, RoutedEventArgs e)
        {
            _passwordStorage.SavePassword(PasswordStorage.Password, Password.Password);
        }

        private void ExportQueryDataClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.DataContext is QueryData queryData)
            {
                _mainViewModel.ExportQueryDataClick(queryData);
            }
        }

        private void BuildRelationshipsHelpClick(object sender, RoutedEventArgs e)
        {
            ShowHelpWindow<BuildRelationshipsHelp>();
        }

        private void UnpublishUnmatchedItemsHelpClick(object sender, RoutedEventArgs e)
        {
            ShowHelpWindow<UnpublishUnmatchedItemsHelp>();
        }

        private void EditCustomAttributeMappingClick(object sender, RoutedEventArgs e)
        {
            var editor = new AttributeMappingEditor
            {
                Owner = this
            };

            editor.ShowDialog();
        }

        private void ShowHelpWindow<T>() where T : Window, new()
        {
            var help = new T
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            help.Show();
        }

        private async void MainWindowOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                switch (_mainViewModel.SelectedTabIndex)
                {
                    case MainViewModel.QueriesTabIndex:
                        await PreviewSql();
                        break;

                    case MainViewModel.FolderTabIndex:
                    case MainViewModel.UpdateStoryTabIndex:
                        await _mainViewModel.RunQueryData(_mainViewModel.SelectedQueryData);
                        break;
                }
            }
        }
    }
}
