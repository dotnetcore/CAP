using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Mocker
{
    public class MockSubscriberExecutor : ISubscriberExecutor
    {
        public Task<OperateResult> ExecuteAsync(CapReceivedMessage message)
        {
            return Task.FromResult(OperateResult.Success);
        }
    }
}