// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;

        public SqlServerStorageConnection(
            IOptions<SqlServerOptions> options, 
            IOptions<CapOptions> capOptions)
        {
            _capOptions = capOptions.Value;
            Options = options.Value;
        }

        public SqlServerOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new SqlServerStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM [{Options.Schema}].[Published] WITH (readpast) WHERE Id={id}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT TOP (200) * FROM [{Options.Schema}].[Published] WITH (readpast) WHERE Retries<{_capOptions.FailedRetryCount} AND Version='{_capOptions.Version}' AND Added<'{fourMinsAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}')";

            using (var connection = new SqlConnection(Options.ConnectionString))
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

            var sql = $@"
INSERT INTO [{Options.Schema}].[Received]([Id],[Version],[Name],[Group],[Content],[Retries],[Added],[ExpiresAt],[StatusName])
VALUES(@Id,'{_capOptions.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                connection.Execute(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM [{Options.Schema}].[Received] WITH (readpast) WHERE Id={id}";
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT TOP (200) * FROM [{Options.Schema}].[Received] WITH (readpast) WHERE Retries<{_capOptions.FailedRetryCount} AND Version='{_capOptions.Version}' AND Added<'{fourMinsAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}')";
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangePublishedState(long messageId, string state)
        {
            var sql =
                $"UPDATE [{Options.Schema}].[Published] SET Retries=Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(long messageId, string state)
        {
            var sql =
                $"UPDATE [{Options.Schema}].[Received] SET Retries=Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }
    }
}