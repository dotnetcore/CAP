using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using Dapper;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFFetchedMessage : IFetchedMessage
    {
        private readonly IDbConnection _connection;
        private readonly IDbContextTransaction _transaction;
        private readonly Timer _timer;
        private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(1);
        private readonly object _lockObject = new object();

        public EFFetchedMessage(string messageId,
            int type,
            IDbConnection connection,
            IDbContextTransaction transaction)
        {
            MessageId = messageId;
            Type = type;
            _connection = connection;
            _transaction = transaction;
            _timer = new Timer(ExecuteKeepAliveQuery, null, KeepAliveInterval, KeepAliveInterval);
        }

        public string MessageId { get; }

        public int Type { get; }

        public void RemoveFromQueue()
        {
            lock (_lockObject)
            {
                _transaction.Commit();
            }
        }

        public void Requeue()
        {
            lock (_lockObject)
            {
                _transaction.Rollback();
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _timer?.Dispose();
                _transaction.Dispose();
                _connection.Dispose();
            }
        }

        private void ExecuteKeepAliveQuery(object obj)
        {
            lock (_lockObject)
            {
                try
                {
                    _connection?.Execute("SELECT 1", _transaction.GetDbTransaction());
                }
                catch
                {
                }
            }
        }
    }
}
