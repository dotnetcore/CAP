using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP
{
    /// <summary>
    /// GaussDB 的 CAP 事务包装器，负责在数据库事务提交后刷新待发布消息。
    /// </summary>
    public class GaussDBCapTransaction : CapTransactionBase
    {
        /// <summary>
        /// 初始化 GaussDB CAP 事务包装器。
        /// </summary>
        /// <param name="dispatcher">消息调度器，用于事务提交后刷新消息队列。</param>
        public GaussDBCapTransaction(IDispatcher dispatcher) : base(dispatcher)
        {
        }

        /// <summary>
        /// 提交数据库事务并在成功后刷新 CAP 发布队列，确保业务数据与消息一致。
        /// </summary>
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

            // 数据库事务提交成功后，才刷新 CAP 发布队列，确保业务数据与消息一致。
            Flush();
        }

        /// <summary>
        /// 异步提交数据库事务并在成功后刷新 CAP 发布队列。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public override async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case DbTransaction dbTransaction:
                    await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }

            // 数据库事务提交成功后，才刷新 CAP 发布队列，确保业务数据与消息一致。
            await FlushAsync();
        }

        /// <summary>
        /// 回滚数据库事务，不发送任何 CAP 消息。
        /// </summary>
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

        /// <summary>
        /// 异步回滚数据库事务，不发送任何 CAP 消息。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public override async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(DbTransaction != null);

            switch (DbTransaction)
            {
                case DbTransaction dbTransaction:
                    await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IDbContextTransaction dbContextTransaction:
                    await dbContextTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>
    /// GaussDB ADO.NET 与 EF Core 事务接入 CAP 的扩展方法。
    /// </summary>
    public static class CapTransactionExtensions
    {
        /// <summary>
        /// 基于 ADO.NET 连接开启 CAP 事务。
        /// </summary>
        /// <param name="dbConnection">业务数据库连接。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        public static ICapTransaction BeginTransaction(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false)
        {
            return BeginTransaction(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit);
        }

        /// <summary>
        /// 基于 ADO.NET 连接和指定隔离级别开启 CAP 事务。
        /// </summary>
        /// <param name="dbConnection">业务数据库连接。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <param name="isolationLevel">数据库事务隔离级别。</param>
        public static ICapTransaction BeginTransaction(this IDbConnection dbConnection,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false)
        {
            if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();
            var dbTransaction = dbConnection.BeginTransaction(isolationLevel);

            publisher.Transaction = ActivatorUtilities.CreateInstance<GaussDBCapTransaction>(publisher.ServiceProvider);
            publisher.Transaction.DbTransaction = dbTransaction;
            publisher.Transaction.AutoCommit = autoCommit;

            return publisher.Transaction;
        }

        /// <summary>
        /// 基于 ADO.NET 连接异步开启 CAP 事务。
        /// </summary>
        /// <param name="dbConnection">业务数据库连接。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public static ValueTask<ICapTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
            ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            return BeginTransactionAsync(dbConnection, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
        }

        /// <summary>
        /// 基于 ADO.NET 连接和指定隔离级别异步开启 CAP 事务。
        /// </summary>
        /// <param name="dbConnection">业务数据库连接。</param>
        /// <param name="isolationLevel">数据库事务隔离级别。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public static ValueTask<ICapTransaction> BeginTransactionAsync(this IDbConnection dbConnection,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            if (dbConnection.State == ConnectionState.Closed) ((DbConnection)dbConnection).OpenAsync(cancellationToken).GetAwaiter().GetResult();
            var dbTransaction = ((DbConnection)dbConnection).BeginTransactionAsync(isolationLevel, cancellationToken).AsTask().GetAwaiter().GetResult();

            publisher.Transaction = ActivatorUtilities.CreateInstance<GaussDBCapTransaction>(publisher.ServiceProvider);
            publisher.Transaction.DbTransaction = dbTransaction;
            publisher.Transaction.AutoCommit = autoCommit;

            return ValueTask.FromResult(publisher.Transaction);
        }

        /// <summary>
        /// 基于 EF Core DatabaseFacade 开启 CAP 事务。
        /// </summary>
        /// <param name="database">EF Core 数据库门面。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <returns>EF Core 事务包装对象。</returns>
        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false)
        {
            return BeginTransaction(database, IsolationLevel.Unspecified, publisher, autoCommit);
        }

        /// <summary>
        /// 基于 EF Core DatabaseFacade 和指定隔离级别开启 CAP 事务。
        /// </summary>
        /// <param name="database">EF Core 数据库门面。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="isolationLevel">数据库事务隔离级别。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <returns>EF Core 事务包装对象。</returns>
        public static IDbContextTransaction BeginTransaction(this DatabaseFacade database,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false)
        {
            var trans = database.BeginTransaction(isolationLevel);
            publisher.Transaction = ActivatorUtilities.CreateInstance<GaussDBCapTransaction>(publisher.ServiceProvider);
            publisher.Transaction.DbTransaction = trans;
            publisher.Transaction.AutoCommit = autoCommit;
            return new CapEFDbTransaction(publisher.Transaction);
        }

        /// <summary>
        /// 基于 EF Core DatabaseFacade 异步开启 CAP 事务。
        /// </summary>
        /// <param name="database">EF Core 数据库门面。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>EF Core 事务包装对象。</returns>
        public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
            ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            return BeginTransactionAsync(database, IsolationLevel.Unspecified, publisher, autoCommit, cancellationToken);
        }

        /// <summary>
        /// 基于 EF Core DatabaseFacade 和指定隔离级别异步开启 CAP 事务。
        /// </summary>
        /// <param name="database">EF Core 数据库门面。</param>
        /// <param name="publisher">CAP 发布器。</param>
        /// <param name="isolationLevel">数据库事务隔离级别。</param>
        /// <param name="autoCommit">发布消息后是否自动提交事务。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>EF Core 事务包装对象。</returns>
        public static Task<IDbContextTransaction> BeginTransactionAsync(this DatabaseFacade database,
            IsolationLevel isolationLevel, ICapPublisher publisher, bool autoCommit = false, CancellationToken cancellationToken = default)
        {
            var trans = database.BeginTransactionAsync(isolationLevel, cancellationToken).GetAwaiter().GetResult();
            publisher.Transaction = ActivatorUtilities.CreateInstance<GaussDBCapTransaction>(publisher.ServiceProvider);
            publisher.Transaction.DbTransaction = trans;
            publisher.Transaction.AutoCommit = autoCommit;
            return Task.FromResult<IDbContextTransaction>(new CapEFDbTransaction(publisher.Transaction));
        }
    }
}
