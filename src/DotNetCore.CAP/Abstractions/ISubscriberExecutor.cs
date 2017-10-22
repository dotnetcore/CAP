using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    public interface ISubscriberExecutor
    {
        Task<OperateResult> ExecuteAsync(CapReceivedMessage receivedMessage);
    }
}
