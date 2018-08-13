using System.Data;
using System.Diagnostics;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class MongoDBCapTransaction : CapTransactionBase
    {
        public MongoDBCapTransaction(IDispatcher dispatcher)
            : base(dispatcher)
        {
        }

        public override void Commit()
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session)
            {
                session.CommitTransaction();
            }

            Flush();
        }

        public override void Rollback()
        {
            Debug.Assert(DbTransaction != null);

            if (DbTransaction is IClientSessionHandle session)
            {
                session.AbortTransaction();
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
            IClientSessionHandle dbTransaction, bool autoCommit = false)
        {
            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        } 
    }
}
