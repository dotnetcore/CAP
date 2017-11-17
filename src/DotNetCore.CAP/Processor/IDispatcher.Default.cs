using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Processor
{
    public class DefaultDispatcher : IDispatcher
    {
        internal static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        private readonly TimeSpan _pollingDelay;
        private readonly IQueueExecutorFactory _queueExecutorFactory;

        public DefaultDispatcher(IQueueExecutorFactory queueExecutorFactory,
            IOptions<CapOptions> capOptions)
        {
            _queueExecutorFactory = queueExecutorFactory;
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
            IFetchedMessage fetched;
            using (var scopedContext = context.CreateScope())
            {
                var provider = scopedContext.Provider;
                var connection = provider.GetRequiredService<IStorageConnection>();

                if ((fetched = await connection.FetchNextMessageAsync()) != null)
                    using (fetched)
                    {
                        var queueExecutor = _queueExecutorFactory.GetInstance(fetched.MessageType);
                        await queueExecutor.ExecuteAsync(connection, fetched);
                    }
            }
            return fetched != null;
        }
    }
}