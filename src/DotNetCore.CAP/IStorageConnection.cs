using System;
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
        Task<CapReceivedMessage> GetNextReceviedMessageToBeEnqueuedAsync();

        //-----------------------------------------

        /// <summary>
        /// Creates and returns an <see cref="IStorageTransaction"/>.
        /// </summary>
        IStorageTransaction CreateTransaction();
    }
}