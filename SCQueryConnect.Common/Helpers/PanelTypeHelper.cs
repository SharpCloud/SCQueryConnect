using SC.API.ComInterop.Models;
using System;
using System.Linq;

namespace SCQueryConnect.Common.Helpers
{
    public static class PanelTypeHelper
    {
        public static string ValidTypes { get; }

        static PanelTypeHelper()
        {
            var validPanelValues = Enum.GetValues(typeof(Panel.PanelType))
                .Cast<Panel.PanelType>()
                .Select(t => t.ToString())
                .Where(t => string.Compare(t, "Undefined", StringComparison.OrdinalIgnoreCase) != 0);

            ValidTypes = string.Join(", ", validPanelValues);
        }
    }
}
