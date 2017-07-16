using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class SqlServerStorageTransaction : IStorageTransaction, IDisposable
    {
        private readonly SqlServerStorageConnection _connection;
        private readonly SqlServerOptions _options;
        private readonly string _schema;

        private IDbTransaction _dbTransaction;
        private IDbConnection _dbConnection;

        public SqlServerStorageTransaction(SqlServerStorageConnection connection)
        {
            _connection = connection;
            _options = _connection.Options;
            _schema = _options.Schema;

            _dbConnection = new SqlConnection(_options.ConnectionString);
            _dbTransaction = _dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"UPDATE [{_schema}].[Published] SET [ExpiresAt] = @ExpiresAt,[StatusName]=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"UPDATE [{_schema}].[Received] SET [ExpiresAt] = @ExpiresAt,[StatusName]=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public void EnqueueMessage(CapPublishedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"INSERT INTO [{_schema}].[Queue] values(@MessageId,@MessageType);";
            _dbConnection.Execute(sql, new CapQueue { MessageId = message.Id, MessageType = MessageType.Publish });
        }

        public void EnqueueMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"INSERT INTO [{_schema}].[Queue] values(@MessageId,@MessageType);";
            _dbConnection.Execute(sql, new CapQueue { MessageId = message.Id, MessageType = MessageType.Subscribe });
        }

        public Task CommitAsync()
        {
            _dbTransaction.Commit();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _dbTransaction.Dispose();
            _dbConnection.Dispose();
        }
    }
}