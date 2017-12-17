using Dapper;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlFetchedMessage : IFetchedMessage
    {
        private readonly string _connectionString = null;

        public MySqlFetchedMessage(int messageId, MessageType type, string connectionString)
        {
            MessageId = messageId;
            MessageType = type;

            _connectionString = connectionString;
        }

        public int MessageId { get; }

        public MessageType MessageType { get; }

        public void RemoveFromQueue()
        {
            // ignored
        }

        public void Requeue()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Execute("insert into `cap.queue`(`MessageId`,`MessageType`) values(@MessageId,@MessageType);"
                    , new {MessageId, MessageType });
            }
        }

        public void Dispose()
        {
            // ignored
        }
    }
}