using SCQueryConnect.Common.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UIRelationshipsDataChecker : RelationshipsDataChecker
    {
        private readonly TextBlock _relationshipErrorText;

        public UIRelationshipsDataChecker(TextBlock relationshipErrorText)
        {
            _relationshipErrorText = relationshipErrorText;
        }

        protected override void ProcessDataValidity(bool isOk)
        {
            _relationshipErrorText.Visibility = isOk ? Visibility.Collapsed : Visibility.Visible;
            base.ProcessDataValidity(isOk);
        }
    }
}
