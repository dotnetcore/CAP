using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents a connection to the storage.
    /// </summary>
    public interface IStorageConnection : IDisposable
    {
        //Sent messages

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapPublishedMessage> GetPublishedMessageAsync(int id);

        /// <summary>
        /// Fetches the next message to be executed.
        /// </summary>
        Task<IFetchedMessage> FetchNextMessageAsync();

        /// <summary>
        /// Returns the next message to be enqueued.
        /// </summary>
        Task<CapPublishedMessage> GetNextPublishedMessageToBeEnqueuedAsync();

        /// <summary>
        /// Returns executed failed messages.
        /// </summary>
        Task<IEnumerable<CapPublishedMessage>> GetFailedPublishedMessages();

        // Received messages

        /// <summary>
        /// Stores the message.
        /// </summary>
        /// <param name="message">The message to store.</param>
        Task StoreReceivedMessageAsync(CapReceivedMessage message);

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapReceivedMessage> GetReceivedMessageAsync(int id);

        /// <summary>
        /// Returns the next message to be enqueued.
        /// </summary>
        Task<CapReceivedMessage> GetNextReceivedMessageToBeEnqueuedAsync();

        /// <summary>
        /// Returns executed failed message.
        /// </summary>
        Task<IEnumerable<CapReceivedMessage>> GetFailedReceivedMessages();

        /// <summary>
        /// Creates and returns an <see cref="IStorageTransaction" />.
        /// </summary>
        IStorageTransaction CreateTransaction();

        /// <summary>
        /// Change specified message's state of published message
        /// </summary>
        /// <param name="messageId">Message id</param>
        /// <param name="state">State name</param>
        bool ChangePublishedState(int messageId, string state);

        /// <summary>
        /// Change specified message's state  of received message
        /// </summary>
        /// <param name="messageId">Message id</param>
        /// <param name="state">State name</param>
        bool ChangeReceivedState(int messageId, string state);
    }
}