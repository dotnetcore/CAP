﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlStorageTransaction : IStorageTransaction
    {
        private readonly IDbConnection _dbConnection;

        private readonly IDbTransaction _dbTransaction;
        private readonly string _schema;

        public PostgreSqlStorageTransaction(PostgreSqlStorageConnection connection)
        {
            var options = connection.Options;
            _schema = options.Schema;

            _dbConnection = new NpgsqlConnection(options.ConnectionString);
            _dbConnection.Open();
            _dbTransaction = _dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $@"UPDATE ""{_schema}"".""published"" SET ""Retries""=@Retries,""Content""= @Content,""ExpiresAt""=@ExpiresAt,""StatusName""=@StatusName WHERE ""Id""=@Id;";
            _dbConnection.Execute(sql, message, _dbTransaction);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $@"UPDATE ""{_schema}"".""received"" SET ""Retries""=@Retries,""Content""= @Content,""ExpiresAt""=@ExpiresAt,""StatusName""=@StatusName WHERE ""Id""=@Id;";
            _dbConnection.Execute(sql, message, _dbTransaction);
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