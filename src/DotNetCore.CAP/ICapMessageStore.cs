using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides an abstraction for a store which manages CAP message.
    /// </summary>
    public interface ICapMessageStore
    {
        /// <summary>
        ///  Creates a new message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to create in the store.</param>
        Task<OperateResult> StoreSentMessageAsync(CapSentMessage message);
    }
}