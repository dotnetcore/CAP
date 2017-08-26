using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Dashboard;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents a persisted storage.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Initializes the storage. For example, making sure a database is created and migrations are applied.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken);

        IMonitoringApi GetMonitoringApi();

        IStorageConnection GetConnection();
    }
}