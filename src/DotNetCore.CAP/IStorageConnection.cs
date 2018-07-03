// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        /// Returns executed failed messages.
        /// </summary>
        Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry();

        // Received messages

        /// <summary>
        /// Stores the message.
        /// </summary>
        /// <param name="message">The message to store.</param>
        Task<int> StoreReceivedMessageAsync(CapReceivedMessage message);

        /// <summary>
        /// Returns the message with the given id.
        /// </summary>
        /// <param name="id">The message's id.</param>
        Task<CapReceivedMessage> GetReceivedMessageAsync(int id);

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
        bool ChangePublishedState(int messageId, string state);

        /// <summary>
        /// Change specified message's state  of received message
        /// </summary>
        /// <param name="messageId">Message id</param>
        /// <param name="state">State name</param>
        bool ChangeReceivedState(int messageId, string state);

        /// <summary>
        ///  Requeue specified fail received message to retry
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        bool ReceivedRequeue(int messageId);

        /// <summary>
        ///  Requeue specified fail published message to retry
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        bool PublishedRequeue(int messageId);
    }
}