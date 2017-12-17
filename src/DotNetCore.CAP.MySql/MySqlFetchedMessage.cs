using Dapper;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlFetchedMessage : IFetchedMessage
    {
        private readonly MySqlOptions _options;

        public MySqlFetchedMessage(int messageId, MessageType type, MySqlOptions options)
        {
            MessageId = messageId;
            MessageType = type;

            _options = options;
        }

        public int MessageId { get; }

        public MessageType MessageType { get; }

        public void RemoveFromQueue()
        {
            // ignored
        }

        public void Requeue()
        {
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                connection.Execute($"insert into `{_options.TableNamePrefix}.queue`(`MessageId`,`MessageType`) values(@MessageId,@MessageType);"
                    , new {MessageId, MessageType });
            }
        }

        public void Dispose()
        {
            // ignored
        }
    }
}