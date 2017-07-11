using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class EFStorageConnection : IStorageConnection
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

        public IStorageTransaction CreateTransaction()
        {
            return new EFStorageTransaction(this);
        }   

        public Task<CapSentMessage> GetSentMessageAsync(string id)
        {
            return _context.CapSentMessages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IFetchedMessage> FetchNextSentMessageAsync()
        {
            //            var sql = $@"
            //DELETE TOP (1)
            //FROM [{_options.Schema}].[{nameof(CapDbContext.CapSentMessages)}] WITH (readpast, updlock, rowlock)
            //OUTPUT DELETED.Id";

            var queueFirst = await _context.CapQueue.FirstOrDefaultAsync();
            if (queueFirst == null)
                return null;

            _context.CapQueue.Remove(queueFirst);

            var connection = _context.Database.GetDbConnection();
            var transaction = _context.Database.CurrentTransaction;
            transaction = transaction ?? await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            return new EFFetchedMessage(queueFirst.MessageId, connection, transaction);
        }

        public Task<CapSentMessage> GetNextSentMessageToBeEnqueuedAsync()
        {
            //            var sql = $@"
            //SELECT TOP (1) *
            //FROM [{_options.Schema}].[{nameof(CapDbContext.CapSentMessages)}] WITH (readpast)
            //WHERE (Due IS NULL OR Due < GETUTCDATE()) AND StateName = '{StatusName.Enqueued}'";

            //            var connection = _context.GetDbConnection();

            //            var message =  _context.CapSentMessages.FromSql(sql).FirstOrDefaultAsync();

            var message = _context.CapSentMessages.Where(x => x.StatusName == StatusName.Enqueued).FirstOrDefaultAsync();

            if (message != null)
            {
                _context.Attach(message);
            }

            return message;
        }

        public Task StoreReceivedMessageAsync(CapReceivedMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.LastRun = NormalizeDateTime(message.LastRun);

            _context.Add(message);
            return _context.SaveChangesAsync();
        }

        public Task<CapReceivedMessage> GetReceivedMessageAsync(string id)
        {
            return _context.CapReceivedMessages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<IFetchedMessage> FetchNextReceivedMessageAsync()
        {
            throw new NotImplementedException();
        }

        public Task<CapSentMessage> GetNextReceviedMessageToBeEnqueuedAsync()
        {
            throw new NotImplementedException();
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

        public void Dispose()
        {
        }
    }
}
