using System.Threading.Tasks;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Test.FakeInMemoryQueue
{
    internal sealed class InMemoryConsumerClientFactory : IConsumerClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly InMemoryQueue _queue;

        public InMemoryConsumerClientFactory(ILoggerFactory loggerFactory, InMemoryQueue queue)
        {
            _loggerFactory = loggerFactory;
            _queue = queue;
        }

        public Task<IConsumerClient> CreateAsync(string groupName, byte groupConcurrent)
        {
            var logger = _loggerFactory.CreateLogger(typeof(InMemoryConsumerClient));
            var client = new InMemoryConsumerClient(logger, _queue, groupName, groupConcurrent);
            return Task.FromResult<IConsumerClient>(client);
        }
    }
}