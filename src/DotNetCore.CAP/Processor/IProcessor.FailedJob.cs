using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class FailedJobProcessor : IProcessor
    {
        private readonly CapOptions _options;
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;
        private readonly IStateChanger _stateChanger;

        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private readonly TimeSpan _waitingInterval;

        public FailedJobProcessor(
            IOptions<CapOptions> options,
            ILogger<FailedJobProcessor> logger,
            IServiceProvider provider,
            IStateChanger stateChanger)
        {
            _options = options.Value;
            _logger = logger;
            _provider = provider;
            _stateChanger = stateChanger;
            _waitingInterval = _options.FailedMessageWaitingInterval;
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (var scope = _provider.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                await Task.WhenAll(
                    ProcessPublishedAsync(connection, context),
                    ProcessReceivededAsync(connection, context));

                DefaultDispatcher.PulseEvent.Set();

                await context.WaitAsync(_waitingInterval);
            }
        }

        private async Task ProcessPublishedAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await connection.GetFailedPublishedMessages();
            foreach (var message in messages)
            {
                using (var transaction = connection.CreateTransaction())
                {
                    _stateChanger.ChangeState(message, new EnqueuedState(), transaction);
                    await transaction.CommitAsync();
                }
                context.ThrowIfStopping();
                await context.WaitAsync(_delay);
            }
        }

        private async Task ProcessReceivededAsync(IStorageConnection connection, ProcessingContext context)
        {
            var messages = await connection.GetFailedReceviedMessages();
            foreach (var message in messages)
            {
                using (var transaction = connection.CreateTransaction())
                {
                    _stateChanger.ChangeState(message, new EnqueuedState(), transaction);
                    await transaction.CommitAsync();
                }
                context.ThrowIfStopping();
                await context.WaitAsync(_delay);
            }
        }
    }
}