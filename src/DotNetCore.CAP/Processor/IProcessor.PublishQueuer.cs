using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class PublishQueuer : IProcessor
    {
        public static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);
        private readonly ILogger _logger;
        private readonly TimeSpan _pollingDelay;
        private readonly IServiceProvider _provider;
        private readonly IStateChanger _stateChanger;

        public PublishQueuer(
            ILogger<PublishQueuer> logger,
            IOptions<CapOptions> options,
            IStateChanger stateChanger,
            IServiceProvider provider)
        {
            _logger = logger;
            _stateChanger = stateChanger;
            _provider = provider;

            var capOptions = options.Value;
            _pollingDelay = TimeSpan.FromSeconds(capOptions.PollingDelay);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            _logger.LogDebug("Publish Queuer start calling.");
            using (var scope = _provider.CreateScope())
            {
                CapPublishedMessage sentMessage;
                var provider = scope.ServiceProvider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                while (
                    !context.IsStopping &&
                    (sentMessage = await connection.GetNextPublishedMessageToBeEnqueuedAsync()) != null)

                {
                    var state = new EnqueuedState();

                    using (var transaction = connection.CreateTransaction())
                    {
                        _stateChanger.ChangeState(sentMessage, state, transaction);
                        await transaction.CommitAsync();
                    }
                }
            }

            context.ThrowIfStopping();

            DefaultDispatcher.PulseEvent.Set();

            await WaitHandleEx.WaitAnyAsync(PulseEvent,
                context.CancellationToken.WaitHandle, _pollingDelay);
        }
    }
}