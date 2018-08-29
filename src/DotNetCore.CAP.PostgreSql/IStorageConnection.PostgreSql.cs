// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;

        public PostgreSqlStorageConnection(PostgreSqlOptions options, CapOptions capOptions)
        {
            _capOptions = capOptions;
            Options = options;
        }

        public PostgreSqlOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new PostgreSqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            var sql = $"SELECT * FROM \"{Options.Schema}\".\"published\" WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"published\" WHERE \"Retries\"<{_capOptions.FailedRetryCount} AND \"Added\"<'{fourMinsAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public void StoreReceivedMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"INSERT INTO \"{Options.Schema}\".\"received\"(\"Id\",\"Name\",\"Group\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Id,@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName) RETURNING \"Id\";";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                connection.Execute(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            var sql = $"SELECT * FROM \"{Options.Schema}\".\"received\" WHERE \"Id\"={id} FOR UPDATE SKIP LOCKED";
            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O"); 
            var sql =
                $"SELECT * FROM \"{Options.Schema}\".\"received\" WHERE \"Retries\"<{_capOptions.FailedRetryCount} AND \"Added\"<'{fourMinsAgo}' AND (\"StatusName\"='{StatusName.Failed}' OR \"StatusName\"='{StatusName.Scheduled}') LIMIT 200;";
            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public void Dispose()
        {
        }

        public bool ChangePublishedState(long messageId, string state)
        {
            var sql =
                $"UPDATE \"{Options.Schema}\".\"published\" SET \"Retries\"=\"Retries\"+1,\"ExpiresAt\"=NULL,\"StatusName\" = '{state}' WHERE \"Id\"={messageId}";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(long messageId, string state)
        {
            var sql =
                $"UPDATE \"{Options.Schema}\".\"received\" SET \"Retries\"=\"Retries\"+1,\"ExpiresAt\"=NULL,\"StatusName\" = '{state}' WHERE \"Id\"={messageId}";

            using (var connection = new NpgsqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }
    }
}