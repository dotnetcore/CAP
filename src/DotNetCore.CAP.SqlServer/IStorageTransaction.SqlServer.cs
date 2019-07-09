// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerStorageTransaction : IStorageTransaction
    {
        private readonly IDbConnection _dbConnection;
        private readonly string _schema;

        public SqlServerStorageTransaction(SqlServerStorageConnection connection)
        {
            var options = connection.Options;
            _schema = options.Schema;

            _dbConnection = new SqlConnection(options.ConnectionString);
            _dbConnection.Open();
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE [{_schema}].[Published] SET [Retries] = @Retries,[Content] = @Content,[ExpiresAt] = @ExpiresAt,[StatusName]=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE [{_schema}].[Received] SET [Retries] = @Retries,[Content] = @Content,[ExpiresAt] = @ExpiresAt,[StatusName]=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public Task CommitAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _dbConnection.Dispose();
        }
    }
}