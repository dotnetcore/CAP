using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.EntityFrameworkCore
{
    public class CapPublisher : ICapPublisher
    {
        private readonly SqlServerOptions _options;
        private readonly IServiceProvider _provider;
        private readonly DbContext _dbContext;

        public CapPublisher(SqlServerOptions options, IServiceProvider provider)
        {
            _options = options;
            _provider = provider;
            _dbContext = (DbContext)_provider.GetService(_options.DbContextType);
        }

        public async Task PublishAsync(string topic, string content)
        {
            var connection = _dbContext.Database.GetDbConnection();
            var transaction = _dbContext.Database.CurrentTransaction;
            transaction = transaction ?? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var dbTransaction = transaction.GetDbTransaction();

            var message = new CapSentMessage
            {
                KeyName = topic,
                Content = content,
                StatusName = StatusName.Scheduled
            };

            var sql = "INSERT INTO [cap].[CapSentMessages] ([Id],[Added],[Content],[KeyName],[ExpiresAt],[Retries],[StatusName])VALUES(@Id,@Added,@Content,@KeyName,@ExpiresAt,@Retries,@StatusName)";
            await connection.ExecuteAsync(sql, message, transaction: dbTransaction);

            PublishQueuer.PulseEvent.Set();
        }

        public Task PublishAsync<T>(string topic, T contentObj)
        {
            throw new NotImplementedException();
        }
    }
}
