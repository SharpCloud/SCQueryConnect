using SCQueryConnect.Interfaces;
using System.Collections.ObjectModel;

namespace SCQueryConnect.Models
{
    public class QueryBatch : IQueryItem
    {
        public string Name { get; set; }
        public ObservableCollection<QueryData> Connections { get; set; }
    }
}
