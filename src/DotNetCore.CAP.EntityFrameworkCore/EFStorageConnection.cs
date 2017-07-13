using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFStorageConnection : IStorageConnection
    {
        private readonly CapDbContext _context;
        private readonly SqlServerOptions _options;

        public EFStorageConnection(
            CapDbContext context,
            IOptions<SqlServerOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public CapDbContext Context => _context;

        public SqlServerOptions Options => _options;

        public IStorageTransaction CreateTransaction()
        {
            return new EFStorageTransaction(this);
        }

        public Task<CapSentMessage> GetSentMessageAsync(string id)
        {
            return _context.CapSentMessages.FirstOrDefaultAsync(x => x.Id == id);
        }


        public Task<IFetchedMessage> FetchNextMessageAsync()
        {
            var sql = $@"
DELETE TOP (1)
FROM [{_options.Schema}].[{nameof(CapDbContext.CapQueue)}] WITH (readpast, updlock, rowlock)
OUTPUT DELETED.MessageId,DELETED.[Type];";

            return FetchNextMessageCoreAsync(sql);
        }


        public async Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync()
        {
            var sql = $@"
SELECT TOP (1) *
FROM [{_options.Schema}].[{nameof(CapDbContext.CapSentMessages)}] WITH (readpast)
WHERE StatusName = '{StatusName.Scheduled}'";

            try
            {
                var connection = _context.GetDbConnection();
                var message = (await connection.QueryAsync<CapSentMessage>(sql)).FirstOrDefault();

                if (message != null)
                {
                    _context.Attach(message);
                }

                return message;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // CapReceviedMessage

        public Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _context.Add(message);
            return _context.SaveChangesAsync();
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(string id)
        {
            return _context.CapReceivedMessages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<CapReceivedMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            var sql = $@"
SELECT TOP (1) *
FROM [{_options.Schema}].[{nameof(CapDbContext.CapReceivedMessages)}] WITH (readpast)
WHERE StateName = '{StatusName.Enqueued}'";

            var connection = _context.GetDbConnection();
            var message = (await connection.QueryAsync<CapReceivedMessage>(sql)).FirstOrDefault();

            if (message != null)
            {
                _context.Attach(message);
            }

            return message;
        }

        public void Dispose()
        {
        }

        private async Task<IFetchedMessage> FetchNextMessageCoreAsync(string sql, object args = null)
        {
            FetchedMessage fetchedJob = null;
            var connection = _context.GetDbConnection();
            var transaction = _context.Database.CurrentTransaction;
            transaction = transaction ?? await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                fetchedJob =
                    (await connection.QueryAsync<FetchedMessage>(sql, args, transaction.GetDbTransaction()))
                    .FirstOrDefault();
            }
            catch (SqlException)
            {
                transaction.Dispose();
                throw;
            }

            if (fetchedJob == null)
            {
                transaction.Rollback();
                transaction.Dispose();
                return null;
            }

            return new EFFetchedMessage(
                fetchedJob.MessageId,
                fetchedJob.Type,
                connection,
                transaction);
        }
    }
}
