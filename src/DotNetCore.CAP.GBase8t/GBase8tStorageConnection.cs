// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using IBM.Data.Informix;

namespace DotNetCore.CAP.GBase8t
{
    public class GBase8tStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;
        private readonly string _prefix;

        public GBase8tStorageConnection(GBase8tOptions options, CapOptions capOptions)
        {
            _capOptions = capOptions;
            Options = options;
            _prefix = Options.Schema;
        }

        public GBase8tOptions Options { get; }

        public IStorageTransaction CreateTransaction()
        {
            return new GBase8tStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM {_prefix}.published WHERE Id={id};";

            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT skip 0 first 200 * FROM {_prefix}.published WHERE Retries<{_capOptions.FailedRetryCount} AND Added<'{fourMinsAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}');";

            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<int> StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql = $@"
INSERT INTO {_prefix}.received (Name,Group,Content,Retries,Added,ExpiresAt,StatusName)
VALUES(@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);select serialv-1 from sysmaster:sysptnhdr p,systables t where p.partnum=t.partnum and t.tabname='received';";

            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return await connection.ExecuteScalarAsync<int>(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(int id)
        {
            var sql = $@"SELECT * FROM {_prefix}.received WHERE Id={id};";
            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT skip 0 first 200 * FROM {_prefix}.received WHERE Retries<{_capOptions.FailedRetryCount} AND Added<'{fourMinsAgo}' AND (StatusName = '{StatusName.Failed}' OR StatusName = '{StatusName.Scheduled}');";
            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangePublishedState(int messageId, string state)
        {
            var sql =
                $"UPDATE {_prefix}.published SET Retries=Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(int messageId, string state)
        {
            var sql =
                $"UPDATE {_prefix}.received SET Retries =Retries+1,ExpiresAt=NULL,StatusName = '{state}' WHERE Id={messageId}";

            using (var connection = new IfxConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public void Dispose()
        {
        }
    }
}
