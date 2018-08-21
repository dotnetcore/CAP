using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MySqlCapTransaction : CapTransactionBase
    {
        public MySqlCapTransaction(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        public override void Commit()
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Commit();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Commit();
                    break;
            }

            Flush();
        }

        public override void Rollback()
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case IDbTransaction dbTransaction:
                    dbTransaction.Rollback();
                    break;
                case IDbContextTransaction dbContextTransaction:
                    dbContextTransaction.Rollback();
                    break;
            }
        }

        public override void Dispose()
        {
            (DbTransaction as IDbTransaction)?.Dispose();
        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IDbContextTransaction dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static IDbContextTransaction BeginAndJoinToTransaction(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction();
            var capTrans = publisher.Transaction.Begin(trans, autoCommit);
            return new CapEFDbTransaction(capTrans);
        }
    }
}