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
	public class PublishQueuer : IProcessor
    {
		private ILogger _logger;
		private CapOptions _options;
		private IStateChanger _stateChanger;
		private IServiceProvider _provider;
		private TimeSpan _pollingDelay;

        public static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);

        public PublishQueuer(
			ILogger<PublishQueuer> logger,
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
                CapSentMessage sentMessage;
				var provider = scope.ServiceProvider;
				var connection = provider.GetRequiredService<IStorageConnection>();

				while (
					!context.IsStopping &&
					(sentMessage = await connection.GetNextSentMessageToBeEnqueuedAsync()) != null)

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
            
            DefaultMessageProcessor.PulseEvent.Set();

            await WaitHandleEx.WaitAnyAsync(PulseEvent,
                context.CancellationToken.WaitHandle, _pollingDelay);
		}
	}
}
