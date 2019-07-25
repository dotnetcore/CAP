// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.SqlServer
{
    public class SqlServerPublisher : CapPublisherBase, ICallbackPublisher
    {
        private readonly SqlServerOptions _options;

        public SqlServerPublisher(IServiceProvider provider) : base(provider)
        {
            _options = ServiceProvider.GetService<IOptions<SqlServerOptions>>().Value;
        }

        public async Task PublishCallbackAsync(CapPublishedMessage message)
        {
            await PublishAsyncInternal(message);
        }

        protected override async Task ExecuteAsync(CapPublishedMessage message, ICapTransaction transaction = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (transaction == null)
            {
                using (var connection = new SqlConnection(_options.ConnectionString))
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
                $"INSERT INTO {_options.Schema}.[Published] ([Id],[Version],[Name],[Content],[Retries],[Added],[ExpiresAt],[StatusName])VALUES(@Id,'{_options.Version}',@Name,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";
        }

        #endregion private methods
    }
}