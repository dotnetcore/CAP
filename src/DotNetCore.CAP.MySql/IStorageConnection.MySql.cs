// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageConnection : IStorageConnection
    {
        private readonly CapOptions _capOptions;
        private readonly IOptions<MySqlOptions> _options;
        private readonly string _prefix;

        public MySqlStorageConnection(IOptions<MySqlOptions> options, IOptions<CapOptions> capOptions)
        {
            _options = options;
            _capOptions = capOptions.Value;
            _prefix = options.Value.TableNamePrefix;
        }

        public MySqlOptions Options => _options.Value;

        public IStorageTransaction CreateTransaction()
        {
            return new MySqlStorageTransaction(this);
        }

        public async Task<CapPublishedMessage> GetPublishedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.published` WHERE `Id`={id};";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapPublishedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapPublishedMessage>> GetPublishedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM `{_prefix}.published` WHERE `Retries`<{_capOptions.FailedRetryCount} AND `Version`='{_capOptions.Version}' AND `Added`<'{fourMinsAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";

            using (var connection = new MySqlConnection(Options.ConnectionString))
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
INSERT INTO `{_prefix}.received`(`Id`,`Version`,`Name`,`Group`,`Content`,`Retries`,`Added`,`ExpiresAt`,`StatusName`)
VALUES(@Id,'{_capOptions.Version}',@Name,@Group,@Content,@Retries,@Added,@ExpiresAt,@StatusName);";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                connection.Execute(sql, message);
            }
        }

        public async Task<CapReceivedMessage> GetReceivedMessageAsync(long id)
        {
            var sql = $@"SELECT * FROM `{_prefix}.received` WHERE Id={id};";
            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<CapReceivedMessage>(sql);
            }
        }

        public async Task<IEnumerable<CapReceivedMessage>> GetReceivedMessagesOfNeedRetry()
        {
            var fourMinsAgo = DateTime.Now.AddMinutes(-4).ToString("O");
            var sql =
                $"SELECT * FROM `{_prefix}.received` WHERE `Retries`<{_capOptions.FailedRetryCount} AND `Version`='{_capOptions.Version}' AND `Added`<'{fourMinsAgo}' AND (`StatusName` = '{StatusName.Failed}' OR `StatusName` = '{StatusName.Scheduled}') LIMIT 200;";
            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return await connection.QueryAsync<CapReceivedMessage>(sql);
            }
        }

        public bool ChangePublishedState(long messageId, string state)
        {
            var sql =
                $"UPDATE `{_prefix}.published` SET `Retries`=`Retries`+1,`ExpiresAt`=NULL,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }

        public bool ChangeReceivedState(long messageId, string state)
        {
            var sql =
                $"UPDATE `{_prefix}.received` SET `Retries`=`Retries`+1,`ExpiresAt`=NULL,`StatusName` = '{state}' WHERE `Id`={messageId}";

            using (var connection = new MySqlConnection(Options.ConnectionString))
            {
                return connection.Execute(sql) > 0;
            }
        }
    }
}