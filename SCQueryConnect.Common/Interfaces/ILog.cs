using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces
{
    public interface ILog
    {
        Task Log(string text);
    }
}
