// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Data;
using DotNetCore.CAP.Internal;

namespace DotNetCore.CAP
{
    public static class TransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbTransaction dbTransaction, bool autoCommit = false)
        {

            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static IDbTransaction JoinToTransaction(this IDbTransaction dbTransaction,
            ICapPublisher publisher, bool autoCommit = false)
        {
            dbTransaction = new RelationDbTransaction(publisher.Transaction.Begin(dbTransaction, autoCommit));
            return dbTransaction;
        }

        public static IDbTransaction BeginAndJoinToTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }

            var dbTransaction = dbConnection.BeginTransaction();
            return dbTransaction.JoinToTransaction(publisher, autoCommit);
        }
    }
}