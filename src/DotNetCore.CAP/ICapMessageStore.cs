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
        /// Change <see cref="CapSentMessage"/> model status name.
        /// </summary>
        /// <param name="message">The type of <see cref="CapSentMessage"/>.</param>
        /// <param name="statusName">The status name.</param>
        /// <returns></returns>
        Task<OperateResult> ChangeSentMessageStateAsync(CapSentMessage message, string statusName, bool autoSaveChanges = true);

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
        /// Change <see cref="CapReceivedMessage"/> model status name.
        /// </summary>
        /// <param name="message">The type of <see cref="CapReceivedMessage"/>.</param>
        /// <param name="statusName">The status name.</param>
        /// <returns></returns>
        Task<OperateResult> ChangeReceivedMessageStateAsync(CapReceivedMessage message, string statusName, bool autoSaveChanges = true);

        /// <summary>
        /// Fetches the next message to be executed.
        /// </summary>
        Task<CapReceivedMessage> GetNextReceivedMessageToBeExcuted();

        /// <summary>
        /// Updates a message in a store as an asynchronous operation.
        /// </summary>
        /// <param name="message">The message to update in the store.</param>
        Task<OperateResult> UpdateReceivedMessageAsync(CapReceivedMessage message);
    }
}