using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Mock
{
    public class MockSubscriberExecutor : ISubscriberExecutor
    {
        public Task<OperateResult> ExecuteAsync(CapReceivedMessage message)
        {
            return Task.FromResult(OperateResult.Success);
        }
    }
}