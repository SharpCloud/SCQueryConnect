using SC.API.ComInterop.Models;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IRelationshipsBuilder
    {
        Task AddRelationshipsToStory(Story story);
    }
}
