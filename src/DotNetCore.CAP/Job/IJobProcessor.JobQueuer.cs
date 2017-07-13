using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job.States;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Job
{
	public class JobQueuer : IJobProcessor
    {
		private ILogger _logger;
		private CapOptions _options;
		private IStateChanger _stateChanger;
		private IServiceProvider _provider;
		private TimeSpan _pollingDelay;

		public JobQueuer(
			ILogger<JobQueuer> logger,
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
               // CapReceivedMessage receivedMessage;
				var provider = scope.ServiceProvider;
				var connection = provider.GetRequiredService<IStorageConnection>();

				while (
					!context.IsStopping &&
					(sentMessage = await connection.GetNextSentMessageToBeEnqueuedAsync()) != null)

                {
                    System.Diagnostics.Debug.WriteLine("JobQueuer 执行 内部循环:  " + DateTime.Now);
                    var state = new EnqueuedState();

					using (var transaction = connection.CreateTransaction())
					{
						_stateChanger.ChangeState(sentMessage, state, transaction);
						await transaction.CommitAsync();
					}
				}
			}

            System.Diagnostics.Debug.WriteLine("JobQueuer 执行:  " + DateTime.Now);
            context.ThrowIfStopping();
            
            WaitHandleEx.SentPulseEvent.Set();
			await WaitHandleEx.WaitAnyAsync(WaitHandleEx.QueuePulseEvent,
                context.CancellationToken.WaitHandle, _pollingDelay);
		}
	}
}
