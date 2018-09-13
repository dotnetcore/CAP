﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DotNetCore.CAP.PostgreSql
{
    public class PostgreSqlPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly PostgreSqlOptions _options;

        public PostgreSqlPublisher(IServiceProvider provider) : base(provider)
        {
            _options = provider.GetService<PostgreSqlOptions>();
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override async Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction,
            CancellationToken cancel = default(CancellationToken))
        {
            if (NotUseTransaction)
            {
                using (var connection = InitDbConnection())
                {
                    await connection.ExecuteAsync(PrepareSql(), message);
                    return;
                }
            }

            var dbTrans = transaction.DbTransaction as IDbTransaction;
            if (dbTrans == null && transaction.DbTransaction is IDbContextTransaction dbContextTrans)
            {
                dbTrans = dbContextTrans.GetDbTransaction();
            }

            var conn = dbTrans?.Connection;
            await conn.ExecuteAsync(PrepareSql(), message, dbTrans);
        }

        #region private methods

        private string PrepareSql()
        {
            return
                $"INSERT INTO \"{_options.Schema}\".\"published\" (\"Id\",\"Name\",\"Content\",\"Retries\",\"Added\",\"ExpiresAt\",\"StatusName\")VALUES(@Id,@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
        }

        private IDbConnection InitDbConnection()
        {
            var conn = new NpgsqlConnection(_options.ConnectionString);
            conn.Open();
            return conn;
        }

        #endregion private methods
    }
}