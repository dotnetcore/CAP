using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Persistence
{
    public interface IStorageInitializer
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        string GetPublishedTableName();

        string GetReceivedTableName();
    }
}
