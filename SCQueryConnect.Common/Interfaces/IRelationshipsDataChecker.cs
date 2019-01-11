using System.Data;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IRelationshipsDataChecker
    {
        bool CheckDataIsOKRels(IDataReader reader);
    }
}
