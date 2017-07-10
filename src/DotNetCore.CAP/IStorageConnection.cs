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
        /// Stores the message.
        /// </summary>
        /// <param name="message">The message to store.</param>
        Task StoreSentMessageAsync(CapSentMessage message);

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapSentMessage> GetSentMessageAsync(string id);

        /// <summary>
        /// Fetches the next message to be executed.
        /// </summary>
        Task<IFetchedMessage> FetchNextSentMessageAsync();

        /// <summary>
        /// Returns the next message to be enqueued.
        /// </summary>
        Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync();

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
        Task<CapReceivedMessage> GetReceivedMessageAsync(string id);

        /// <summary>
        /// Fetches the next message to be executed.
        /// </summary>
        Task<IFetchedMessage> FetchNextReceivedMessageAsync();

        /// <summary>
        /// Returns the next message to be enqueued.
        /// </summary>
        Task<CapSentMessage> GetNextReceviedMessageToBeEnqueuedAsync();
         
        //-----------------------------------------

        /// <summary>
        /// Creates and returns an <see cref="IStorageTransaction"/>.
        /// </summary>
        IStorageTransaction CreateTransaction();
    }
}
