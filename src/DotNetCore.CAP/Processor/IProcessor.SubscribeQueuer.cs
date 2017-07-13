using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Processor.States;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class SubscribeQueuer : IProcessor
    {
        private ILogger _logger;
        private CapOptions _options;
        private IStateChanger _stateChanger;
        private IServiceProvider _provider;
        private TimeSpan _pollingDelay;

        internal static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        public SubscribeQueuer(
            ILogger<SubscribeQueuer> logger,
            IOptions<CapOptions> options,
            IStateChanger stateChanger,
            IServiceProvider provider)
        {
            _logger = logger;
            _options = options.Value;
            _stateChanger = stateChanger;
            _provider = provider;

            _pollingDelay = TimeSpan.FromSeconds(_options.PollingDelay);
        }

        public async Task ProcessAsync(ProcessingContext context)
        {
            using (var scope = _provider.CreateScope())
            {
                CapReceivedMessage message;
                var provider = scope.ServiceProvider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                while (
                    !context.IsStopping &&
                    (message = await connection.GetNextReceviedMessageToBeEnqueuedAsync()) != null)

                {
                    var state = new EnqueuedState();

                    using (var transaction = connection.CreateTransaction())
                    {
                        _stateChanger.ChangeState(message, state, transaction);
                        await transaction.CommitAsync();
                    }
                }
            }

            context.ThrowIfStopping();

            DefaultMessageProcessor.PulseEvent.Set();

            await WaitHandleEx.WaitAnyAsync(PulseEvent,
                context.CancellationToken.WaitHandle, _pollingDelay);
        }
    }
}
