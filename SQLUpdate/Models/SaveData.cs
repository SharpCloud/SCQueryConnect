using System.Collections.Generic;

namespace SCQueryConnect.Models
{
    public class SaveData
    {
        public List<QueryData> Connections { get; set; }
        public int LastSelectedConnectionIndex { get; set; }
        public int LastSelectedFolderIndex { get; set; }
        public int SelectedQueryTabIndex { get; set; }
        public int SelectedTabIndex { get; set; }
    }
}
