using Dapper;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlFetchedMessage : IFetchedMessage
    {
        private readonly MySqlOptions _options;
        private readonly string _processId;

        public MySqlFetchedMessage(int messageId, MessageType type, string processId, MySqlOptions options)
        {
            MessageId = messageId;
            MessageType = type;

            _processId = processId;
            _options = options;
        }

        public int MessageId { get; }

        public MessageType MessageType { get; }

        public void RemoveFromQueue()
        {
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                connection.Execute($"DELETE FROM `{_options.TableNamePrefix}.queue` WHERE `ProcessId`=@ProcessId"
                    , new { ProcessId = _processId });
            } 
        }

        public void Requeue()
        {
            using (var connection = new MySqlConnection(_options.ConnectionString))
            {
                connection.Execute($"UPDATE `{_options.TableNamePrefix}.queue` SET `ProcessId`=NULL WHERE `ProcessId`=@ProcessId"
                    , new { ProcessId = _processId });
            }
        }

        public void Dispose()
        {
            // ignored
        }
    }
}