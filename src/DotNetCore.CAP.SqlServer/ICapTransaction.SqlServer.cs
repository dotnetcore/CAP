// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.SqlServer.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class SqlServerCapTransaction : CapTransactionBase
    {
        private readonly DiagnosticProcessorObserver _diagnosticProcessor;

        public SqlServerCapTransaction(IDispatcher dispatcher,
            DiagnosticProcessorObserver diagnosticProcessor) : base(dispatcher)
        {
            _diagnosticProcessor = diagnosticProcessor;
        }

        protected override void AddToSent(CapPublishedMessage msg)
        {
            var transactionKey = ((SqlConnection)((IDbTransaction)DbTransaction).Connection).ClientConnectionId;
            if (_diagnosticProcessor.BufferList.TryGetValue(transactionKey, out var list))
            {
                list.Add(msg);
            }
            else
            {
                var msgList = new List<CapPublishedMessage>(1) { msg };
                _diagnosticProcessor.BufferList.TryAdd(transactionKey, msgList);
            }
        }

        public override void Commit()
        {
            throw new NotImplementedException();
        }

        public override void Rollback()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {

        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static IDbTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }

            var dbTransaction = dbConnection.BeginTransaction();
            var capTransaction = publisher.Transaction.Begin(dbTransaction, autoCommit);
            return (IDbTransaction)capTransaction.DbTransaction;
        }

        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbContextTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction();
            var capTrans = publisher.Transaction.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }
    }
}