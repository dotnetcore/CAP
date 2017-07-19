using System.Threading.Tasks;

namespace DotNetCore.CAP
{
    public interface IQueueExecutor
    {
        Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage message);
    }
}