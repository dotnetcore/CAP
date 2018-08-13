// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using MySql.Data.MySqlClient;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageTransaction : IStorageTransaction
    {
        private readonly IDbConnection _dbConnection;

        private readonly string _prefix;

        public MySqlStorageTransaction(MySqlStorageConnection connection)
        {
            var options = connection.Options;
            _prefix = options.TableNamePrefix;

            _dbConnection = new MySqlConnection(options.ConnectionString);
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE `{_prefix}.published` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE `{_prefix}.received` SET `Retries` = @Retries,`Content`= @Content,`ExpiresAt` = @ExpiresAt,`StatusName`=@StatusName WHERE `Id`=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public Task CommitAsync()
        {
            _dbConnection.Close();
            _dbConnection.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _dbConnection.Dispose();
        }
    }
}