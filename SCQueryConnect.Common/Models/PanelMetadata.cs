using SC.API.ComInterop.Models;

namespace SCQueryConnect.Common.Models
{
    internal class PanelMetadata
    {
        public string ItemExternalId { get; set; }
        public string Title { get; set; }
        public Panel.PanelType PanelType { get; set; }
        public string Data { get; set; }
    }
}
