using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Test.FakeInMemoryQueue
{
    internal class FakeInMemoryQueueTransport : ITransport
    {
        private readonly InMemoryQueue _queue;
        private readonly ILogger _logger;

        public FakeInMemoryQueueTransport(InMemoryQueue queue, ILogger<FakeInMemoryQueueTransport> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public BrokerAddress BrokerAddress { get; } = new BrokerAddress("InMemory", string.Empty);

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            try
            {
                _queue.Send(message.GetName(), message);

                _logger.LogDebug($"Event message [{message.GetName()}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return Task.FromResult(OperateResult.Failed(wrapperEx));
            }
        }
    }
}