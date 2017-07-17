using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class DefaultDispatcher : IDispatcher
    {
        private readonly IQueueExecutorFactory _queueExecutorFactory;
        private readonly IServiceProvider _provider;
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _cts;
        private readonly TimeSpan _pollingDelay;

        internal static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        public DefaultDispatcher(
               IServiceProvider provider,
               IQueueExecutorFactory queueExecutorFactory,
               IOptions<CapOptions> capOptions,
               ILogger<DefaultDispatcher> logger)
        {
            _logger = logger;
            _queueExecutorFactory = queueExecutorFactory;
            _provider = provider;
            _cts = new CancellationTokenSource();
            _pollingDelay = TimeSpan.FromSeconds(capOptions.Value.PollingDelay);
        }

        public bool Waiting { get; private set; }

        public Task ProcessAsync(ProcessingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.ThrowIfStopping();

            return ProcessCoreAsync(context);
        }

        public async Task ProcessCoreAsync(ProcessingContext context)
        {
            try
            {
                _logger.LogInformation("BaseMessageJobProcessor processing ...");

                var worked = await Step(context);

                context.ThrowIfStopping();

                Waiting = true;

                if (!worked)
                {
                    var token = GetTokenToWaitOn(context);
                    await WaitHandleEx.WaitAnyAsync(PulseEvent, token.WaitHandle, _pollingDelay);
                }
            }
            finally
            {
                Waiting = false;
            }
        }

        protected virtual CancellationToken GetTokenToWaitOn(ProcessingContext context)
        {
            return context.CancellationToken;
        }

        private async Task<bool> Step(ProcessingContext context)
        {
            var fetched = default(IFetchedMessage);
            using (var scopedContext = context.CreateScope())
            {
                var provider = scopedContext.Provider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                if ((fetched = await connection.FetchNextMessageAsync()) != null)
                {
                    using (fetched)
                    {
                        var queueExecutor = _queueExecutorFactory.GetInstance(fetched.MessageType);
                        await queueExecutor.ExecuteAsync(connection, fetched);
                    }
                }
            }
            return fetched != null;
        }
    }
}