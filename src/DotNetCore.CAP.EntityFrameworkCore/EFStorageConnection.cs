using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MR.AspNetCore.Jobs.Models;
using MR.AspNetCore.Jobs.Server;
using MR.AspNetCore.Jobs.Server.States;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFStorageConnection<TContext> : IStorageConnection where TContext : DbContext
    {
        private readonly CapDbContext _context;
        private readonly EFOptions _options;

        public EFStorageConnection(
            CapDbContext context,
            IOptions<EFOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public CapDbContext Context => _context;

        public EFOptions Options => _options;



        public Task StoreCronJobAsync(CronJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _context.Add(job);
            return _context.SaveChangesAsync();
        }

        public Task AttachCronJobAsync(CronJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            _context.Attach(job);
            return Task.FromResult(true);
        }

        public Task UpdateCronJobAsync(CronJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            return _context.SaveChangesAsync();
        }

        public Task<CronJob[]> GetCronJobsAsync()
        {
            return _context.CronJobs.ToArrayAsync();
        }

        public async Task RemoveCronJobAsync(string name)
        {
            var cronJob = await _context.CronJobs.FirstOrDefaultAsync(j => j.Name == name);
            if (cronJob != null)
            {
                _context.Remove(cronJob);
                await _context.SaveChangesAsync();
            }
        }

        public IStorageTransaction CreateTransaction()
        {
            return new EFStorageTransaction(this);
        }

        public void Dispose()
        {
        }

        private DateTime? NormalizeDateTime(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return dateTime;
            if (dateTime == DateTime.MinValue)
            {
                return new DateTime(1754, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return dateTime;
        }

      

        public Task StoreSentMessageAsync(CapSentMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.LastRun = NormalizeDateTime(message.LastRun);

            _context.Add(message);
            return _context.SaveChangesAsync();
        }

        public Task<CapSentMessage> GetSentMessageAsync(string id)
        {
            return _context.CapSentMessages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<IFetchedJob> FetchNextJobAsync()
        {
            
        }

        public async Task<Job> GetNextJobToBeEnqueuedAsync()
        {
            var sql = $@"
SELECT TOP (1) *
FROM [{_options.Schema}].[{nameof(JobsDbContext.Jobs)}] WITH (readpast)
WHERE (Due IS NULL OR Due < GETUTCDATE()) AND StateName = '{ScheduledState.StateName}'";

            var connection = _context.GetDbConnection();

            var job = (await connection.QueryAsync<Job>(sql)).FirstOrDefault();

            if (job != null)
            {
                _context.Attach(job);
            }

            return job;
        }

        public Task<IFetchedMessage> FetchNextSentMessageAsync()
        {
            var sql = $@"
DELETE TOP (1)
FROM [{_options.Schema}].[{nameof(CapDbContext.CapSentMessages)}] WITH (readpast, updlock, rowlock)
OUTPUT DELETED.Id";

            //return FetchNextDelayedMessageCoreAsync(sql);
            throw new NotImplementedException();
        }

        //private async Task<IFetchedMessage> FetchNextDelayedMessageCoreAsync(string sql, object args = null)
        //{
        //    FetchedMessage fetchedJob = null;
        //    var connection = _context.Database.GetDbConnection();
        //    var transaction = _context.Database.CurrentTransaction;
        //    transaction = transaction ?? await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        //    try
        //    {
        //        fetchedJob =
        //            (await _context...QueryAsync<FetchedMessage>(sql, args, transaction.GetDbTransaction()))
        //            .FirstOrDefault();
        //    }
        //    catch (SqlException)
        //    {
        //        transaction.Dispose();
        //        throw;
        //    }

        //    if (fetchedJob == null)
        //    {
        //        transaction.Rollback();
        //        transaction.Dispose();
        //        return null;
        //    }

        //    return new SqlServerFetchedJob(
        //        fetchedJob.JobId,
        //        connection,
        //        transaction);
        //}

        public Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync()
        {
            throw new NotImplementedException();
        }

        public Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IFetchedMessage> FetchNextReceivedMessageAsync()
        {
            throw new NotImplementedException();
        }

        public Task<CapSentMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            throw new NotImplementedException();
        }
    }
}
