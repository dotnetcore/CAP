using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFStorageTransaction
        : IStorageTransaction, IDisposable
    {
        private EFStorageConnection _connection;

        public EFStorageTransaction(EFStorageConnection connection)
        {
            _connection = connection;
        }

        public void UpdateMessage(CapSentMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // NOOP. EF will detect changes.
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // NOOP. EF will detect changes.
        }

        public void EnqueueMessage(CapSentMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _connection.Context.Add(new CapQueue
            {
                MessageId = message.Id,
                Type = 0
            });
        }

        public void EnqueueMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _connection.Context.Add(new CapQueue
            {
                MessageId = message.Id,
                Type = 1
            });
        }


        public Task CommitAsync()
        {
            return _connection.Context.SaveChangesAsync();
        }

        public void Dispose()
        {
        }
    }
}
