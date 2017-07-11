using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Job.States;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Job
{
	public class JobQueuer : IJobProcessor
    {
		private ILogger _logger;
		private JobsOptions _options;
		private IStateChanger _stateChanger;
		private IServiceProvider _provider;

		internal static readonly AutoResetEvent PulseEvent = new AutoResetEvent(true);
		private TimeSpan _pollingDelay;

		public JobQueuer(
			ILogger<JobQueuer> logger,
			JobsOptions options,
			IStateChanger stateChanger,
			IServiceProvider provider)
		{
			_logger = logger;
			_options = options;
			_stateChanger = stateChanger;
			_provider = provider;

			_pollingDelay = TimeSpan.FromSeconds(_options.PollingDelay);
		}

		public async Task ProcessAsync(ProcessingContext context)
		{
			using (var scope = _provider.CreateScope())
			{
                CapSentMessage sentMessage;
                CapReceivedMessage receivedMessage;
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

			DelayedJobProcessor.PulseEvent.Set();
			await WaitHandleEx.WaitAnyAsync(PulseEvent, context.CancellationToken.WaitHandle, _pollingDelay);
		}
	}
}
