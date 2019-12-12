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

        public IConsumerClient Create(string groupId)
        {
            var logger = _loggerFactory.CreateLogger(typeof(InMemoryConsumerClient));
            return new InMemoryConsumerClient(logger, _queue, groupId);
        }
    }
}