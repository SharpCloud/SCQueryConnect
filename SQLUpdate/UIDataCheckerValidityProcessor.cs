using SCQueryConnect.Common.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UIDataCheckerValidityProcessor : IDataCheckerValidityProcessor
    {
        private readonly TextBlock _errorText;

        public UIDataCheckerValidityProcessor(TextBlock errorText)
        {
            _errorText = errorText;
        }

        void IDataCheckerValidityProcessor.ProcessDataValidity(bool isOk)
        {
            _errorText.Visibility = isOk ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
