using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces
{
    public interface ILog
    {
        Task Clear();
        Task Log(string text);
    }
}
