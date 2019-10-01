using SCQueryConnect.Common.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UIRelationshipsDataChecker : RelationshipsDataChecker
    {
        internal TextBlock RelationshipErrorText { private get; set; }

        protected override void ProcessDataValidity(bool isOk)
        {
            RelationshipErrorText.Visibility = isOk ? Visibility.Collapsed : Visibility.Visible;
            base.ProcessDataValidity(isOk);
        }
    }
}
