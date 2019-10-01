using SCQueryConnect.Common.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UIDataChecker : DataChecker
    {
        internal TextBlock ErrorText { private get; set; }

        protected override void ProcessDataValidity(bool isOk)
        {
            ErrorText.Visibility = isOk ? Visibility.Collapsed : Visibility.Visible;
            base.ProcessDataValidity(isOk);
        }
    }
}
