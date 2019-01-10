using SCQueryConnect.Common.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UIDataChecker : DataChecker
    {
        private readonly TextBlock _errorText;

        public UIDataChecker(TextBlock errorText)
        {
            _errorText = errorText;
        }

        protected override void ProcessDataValidity(bool isOk)
        {
            _errorText.Visibility = isOk ? Visibility.Collapsed : Visibility.Visible;
            base.ProcessDataValidity(isOk);
        }
    }
}
