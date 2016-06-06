using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SCQueryConnect
{
    [DataContract]
    public class QueryData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int ConnectionType { get; set; }
        [DataMember]
        public string ConnectionsString { get; set; }
        [DataMember]
        public string QueryString { get; set; }
        [DataMember]
        public string StoryId { get; set; }
        [DataMember]
        public string QueryStringRels { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public QueryData(string name="")
        {
            Name = name;
            QueryStringRels = "SELECT ITEM1, ITEM2, COMMENT, TAGS FROM RELTABLE";

        }
    }
}
