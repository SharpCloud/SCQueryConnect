using SC.API.ComInterop.Models;
using SCQueryConnect.Common.Models;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces
{
    public interface IQueryConnectHelper
    {
        string AppNameOnly { get; }
        string AppName { get; }

        bool Validate(Story story, out string message);
        Task UpdateRelationships(IDbConnection connection, Story story, string sqlString);
        Task InitialiseDatabase(SharpCloudConfiguration config, string connectionString, DatabaseType dbType);
        Task UpdateSharpCloud(SharpCloudConfiguration config, UpdateSettings settings, CancellationToken ct);
    }
}
