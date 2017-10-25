using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Consumer method executor.
    /// </summary>
    public interface ISubscriberExecutor
    {
        /// <summary>
        /// Execute the consumer method.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        Task<OperateResult> ExecuteAsync(CapReceivedMessage receivedMessage);
    }
}
