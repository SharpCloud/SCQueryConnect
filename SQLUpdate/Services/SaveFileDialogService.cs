using Microsoft.Win32;
using SCQueryConnect.Common.Services;
using SCQueryConnect.Interfaces;

namespace SCQueryConnect.Services
{
    public class SaveFileDialogService : ISaveFileDialogService
    {
        public string PromptForExportPath(string defaultFileName)
        {
            if (string.IsNullOrWhiteSpace(defaultFileName))
            {
                return string.Empty;
            }

            var dlg = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = $"SharpCloud QueryConnect Files (*{IOService.SaveDataExtension})|*{IOService.SaveDataExtension}"
            };

            var result = dlg.ShowDialog();

            var filename = result == true
                ? dlg.FileName
                : string.Empty;

            return filename;
        }
    }
}
