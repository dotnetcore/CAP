using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    public interface ISubscriberExecutor
    {
        Task<OperateResult> ExecuteAsync(CapReceivedMessage message);
    }
}