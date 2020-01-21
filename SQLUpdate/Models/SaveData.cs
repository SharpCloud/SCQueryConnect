using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SCQueryConnect.Models
{
    [DataContract]
    internal class SaveData
    {
        [DataMember] public IList<QueryData> Connections { get; set; }
        [DataMember] public IList<Solution> Solutions { get; set; }
    }
}
