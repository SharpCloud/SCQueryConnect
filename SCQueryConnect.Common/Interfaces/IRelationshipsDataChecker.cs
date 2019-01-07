using System.Data.Common;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IRelationshipsDataChecker
    {
        bool CheckDataIsOKRels(DbDataReader reader);
    }
}
