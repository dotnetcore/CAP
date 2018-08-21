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
            (DbTransaction as IClientSessionHandle)?.Dispose();
        }
    }

    public static class CapTransactionExtensions
    {
        public static ICapTransaction Begin(this ICapTransaction transaction,
            IClientSessionHandle dbTransaction, bool autoCommit = false)
        {
            if (!dbTransaction.IsInTransaction)
            {
                dbTransaction.StartTransaction();
            }

            transaction.DbTransaction = dbTransaction;
            transaction.AutoCommit = autoCommit;

            return transaction;
        }

        public static IClientSessionHandle BeginAndJoinToTransaction(this IClientSessionHandle clientSessionHandle,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var capTrans = publisher.Transaction.Begin(clientSessionHandle, autoCommit);
            return new CapMongoDbClientSessionHandle(capTrans);
        }

        public static IClientSessionHandle StartAndJoinToTransaction(this IMongoClient client,
            ICapPublisher publisher, bool autoCommit = false)
        {
            var clientSessionHandle = client.StartSession();
            return BeginAndJoinToTransaction(clientSessionHandle, publisher, autoCommit);
        }
    }
}
