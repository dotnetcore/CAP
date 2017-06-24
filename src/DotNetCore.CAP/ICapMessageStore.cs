using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides an abstraction for a store which manages consistent message.
    /// </summary>
    /// <typeparam name="ConsistencyMessage"></typeparam>
    public interface ICapMessageStore
    {
        /// <summary>
        ///  Creates a new message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to create in the store.</param>
        Task<OperateResult> StoreSentMessageAsync(CapSentMessage message);

        /// <summary>
        /// Fetches the next message to be executed.
        /// </summary>
        /// <returns></returns>
        Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync();

        /// <summary>
        /// Updates a message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to update in the store.</param>
        Task<OperateResult> UpdateSentMessageAsync(CapSentMessage message);

        /// <summary>
        /// Deletes a message from the store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to delete in the store.</param>
        Task<OperateResult> RemoveSentMessageAsync(CapSentMessage message);

        /// <summary>
        /// Creates a new message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<OperateResult> StoreReceivedMessageAsync(CapReceivedMessage message);

        /// <summary>
        /// Updates a message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to update in the store.</param>
        Task<OperateResult> UpdateReceivedMessageAsync(CapReceivedMessage message);
    }
}