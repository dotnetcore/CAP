// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.InMemoryStorage
{
    public class InMemoryStorageTransaction : IStorageTransaction
    {
        private readonly InMemoryStorageConnection _connection;

        public InMemoryStorageTransaction(InMemoryStorageConnection connection)
        {
            _connection = connection;
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var msg = _connection.PublishedMessages.FirstOrDefault(x => message.Id == x.Id);
            if (msg == null) return;
            msg.Retries = message.Retries;
            msg.Content = message.Content;
            msg.ExpiresAt = message.ExpiresAt;
            msg.StatusName = message.StatusName;
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            var msg = _connection.ReceivedMessages.FirstOrDefault(x => message.Id == x.Id);
            if (msg == null) return;
            msg.Retries = message.Retries;
            msg.Content = message.Content;
            msg.ExpiresAt = message.ExpiresAt;
            msg.StatusName = message.StatusName;
        }

        public Task CommitAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}