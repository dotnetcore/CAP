﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.SqlServer.Diagnostics;
using DotNetCore.CAP.Transport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class SqlServerCapTransaction : CapTransactionBase
    {
        private readonly DiagnosticProcessorObserver _diagnosticProcessor;

        public SqlServerCapTransaction(
            IDispatcher dispatcher,
            DiagnosticProcessorObserver diagnosticProcessor) : base(dispatcher)
        {
            _diagnosticProcessor = diagnosticProcessor;
        }

        protected override void AddToSent(MediumMessage msg)
        {
            if (DbTransaction is NoopTransaction)
            {
                base.AddToSent(msg);
                return;
            }

            var dbTransaction = DbTransaction as IDbTransaction
                                ?? throw new ArgumentNullException(nameof(DbTransaction));

            var transactionKey = ((SqlConnection)dbTransaction.Connection).ClientConnectionId;
            if (_diagnosticProcessor.BufferList.TryGetValue(transactionKey, out var list))
            {
                list.Add(msg);
            }
            else
            {
                var msgList = new List<MediumMessage>(1) { msg };
                _diagnosticProcessor.BufferList.TryAdd(transactionKey, msgList);
            }
        }

        public override void Commit()
        {
            switch (DbTransaction)
            {
                case NoopTransaction _:
                    Flush();
                    break;
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
            }
        }

        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            switch (DbTransaction)
            {
                case NoopTransaction _:
                    Flush();
                    break;
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    await Task.CompletedTask;
                    break;
            }
        }

        public override void Rollback()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    break;
            }
        }

        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    await Task.CompletedTask;
                    break;
            }
        }

        public override void Dispose()
        {
            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Dispose();
                    break;
            }

            DbTransaction = null;
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

        /// <summary>
        /// Start the CAP transaction
        /// </summary>
        /// <param name="dbConnection">The <see cref="IDbConnection" />.</param>
        /// <param name="publisher">The <see cref="ICapPublisher" />.</param>
        /// <param name="autoCommit">Whether the transaction is automatically committed when the message is published</param>
        /// <returns>The <see cref="ICapTransaction" /> object.</returns>
        public static IDbTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();

            var dbTransaction = dbConnection.BeginTransaction();
            publisher.Transaction.Value = publisher.ServiceProvider.GetService<ICapTransaction>();
            var capTransaction = publisher.Transaction.Value.Begin(dbTransaction, autoCommit);
            return (IDbTransaction)capTransaction.DbTransaction;
        }
    }
}