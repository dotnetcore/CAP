// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Represents a connection to the storage.
    /// </summary>
    public interface IStorageConnection
    {
        //Sent messages

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapPublishedMessage> GetPublishedMessageAsync(long id);

        /// <summary>
        /// Returns executed failed messages.
        /// </summary>
        Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry();

        // Received messages

        /// <summary>
        /// Stores the message.
        /// </summary>
        /// <param name="message">The message to store.</param>
        void StoreReceivedMessage(CapReceivedMessage message);

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapReceivedMessage> GetReceivedMessageAsync(long id);

        /// <summary>
        /// Returns executed failed message.
        /// </summary>
        Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry();

        /// <summary>
        /// Creates and returns an <see cref="IStorageTransaction" />.
        /// </summary>
        IStorageTransaction CreateTransaction();

        /// <summary>
        /// Change specified message's state of published message
        /// </summary>
        /// <param name="messageId">Message id</param>
        /// <param name="state">State name</param>
        bool ChangePublishedState(long messageId, string state);

        /// <summary>
        /// Change specified message's state  of received message
        /// </summary>
        /// <param name="messageId">Message id</param>
        /// <param name="state">State name</param>
        bool ChangeReceivedState(long messageId, string state);
    }
}