using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageTransaction : IStorageTransaction
    {
        private readonly IDbConnection _dbConnection;

        private readonly IDbTransaction _dbTransaction;
        private readonly string _prefix;

        public MySqlStorageTransaction(MySqlStorageConnection connection)
        {
            var options = connection.Options;
            _prefix = options.TableNamePrefix;

            _dbConnection = new MySqlConnection(options.ConnectionString);
            _dbConnection.Open();
            _dbTransaction = _dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql =
                $"UPDATE `{_prefix}.published` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
            _dbConnection.Execute(sql, message, _dbTransaction);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql =
                $"UPDATE `{_prefix}.received` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
            _dbConnection.Execute(sql, message, _dbTransaction);
        }

        public void EnqueueMessage(CapPublishedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"INSERT INTO `{_prefix}.queue` values(@MessageId,@MessageType);";
            _dbConnection.Execute(sql, new CapQueue {MessageId = message.Id, MessageType = MessageType.Publish},
                _dbTransaction);
        }

        public void EnqueueMessage(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var sql = $"INSERT INTO `{_prefix}.queue` values(@MessageId,@MessageType);";
            _dbConnection.Execute(sql, new CapQueue {MessageId = message.Id, MessageType = MessageType.Subscribe},
                _dbTransaction);
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