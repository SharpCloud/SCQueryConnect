using SCQueryConnect.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCQueryConnect.Interfaces
{
    public interface IStoryAttributesHelper
    {
        Task<List<AttributeDesignations>> GetStoryAttributes();
        Task<List<AttributeMapping>> GetAttributeMappings();
    }
}
