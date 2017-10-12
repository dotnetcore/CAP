using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Internal
{
    public interface ISubscriberExecutor
    {
        Task<OperateResult> ExecuteAsync(CapReceivedMessage receivedMessage);
    }
}
