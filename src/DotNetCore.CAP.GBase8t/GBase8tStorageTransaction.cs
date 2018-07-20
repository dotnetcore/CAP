// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using IBM.Data.Informix;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace DotNetCore.CAP.GBase8t
{
    public class GBase8tStorageTransaction : IStorageTransaction
    {
        private readonly IDbConnection _dbConnection;

        //private readonly IDbTransaction _dbTransaction;
        private readonly string _prefix;

        public GBase8tStorageTransaction(GBase8tStorageConnection connection)
        {
            var options = connection.Options;
            _prefix = options.Schema;
            
            _dbConnection = new IfxConnection(options.ConnectionString);
        }

        public void UpdateMessage(CapPublishedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE {_prefix}.published SET Retries = @Retries,Content= @Content,ExpiresAt = @ExpiresAt,StatusName=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public void UpdateMessage(CapReceivedMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sql =
                $"UPDATE {_prefix}.received SET Retries = @Retries,Content= @Content,ExpiresAt = @ExpiresAt,StatusName=@StatusName WHERE Id=@Id;";
            _dbConnection.Execute(sql, message);
        }

        public Task CommitAsync()
        {
            _dbConnection.Close();
            _dbConnection.Dispose();
            //_dbTransaction.Commit();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //_dbTransaction.Dispose();
            _dbConnection.Dispose();
        }
    }
}
