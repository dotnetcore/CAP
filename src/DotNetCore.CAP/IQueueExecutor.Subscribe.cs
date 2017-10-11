using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP
{
	public class SubscribeQueueExecutor : IQueueExecutor
	{
		private readonly IConsumerInvokerFactory _consumerInvokerFactory;
		private readonly ILogger _logger;
		private readonly CapOptions _options;
		private readonly MethodMatcherCache _selector;
		private readonly IStateChanger _stateChanger;

		public SubscribeQueueExecutor(
			IStateChanger stateChanger,
			MethodMatcherCache selector,
			CapOptions options,
			IConsumerInvokerFactory consumerInvokerFactory,
			ILogger<BasePublishQueueExecutor> logger)
		{
			_selector = selector;
			_options = options;
			_consumerInvokerFactory = consumerInvokerFactory;
			_stateChanger = stateChanger;
			_logger = logger;
		}

		public async Task<OperateResult> ExecuteAsync(IStorageConnection connection, IFetchedMessage fetched)
		{
			//return await Task.FromResult(OperateResult.Success);
			var message = await connection.GetReceivedMessageAsync(fetched.MessageId);
			try
			{
				var sp = Stopwatch.StartNew();
				await _stateChanger.ChangeStateAsync(message, new ProcessingState(), connection);

				if (message.Retries > 0)
					_logger.JobRetrying(message.Retries);
				var result = await ExecuteSubscribeAsync(message);
				sp.Stop();

				IState newState;
				if (!result.Succeeded)
				{
					var shouldRetry = await UpdateMessageForRetryAsync(message, connection, result.Exception?.Message);
					if (shouldRetry)
					{
						newState = new ScheduledState();
						_logger.JobFailedWillRetry(result.Exception);
					}
					else
					{
						newState = new FailedState();
						_logger.JobFailed(result.Exception);
					}
				}
				else
				{
					newState = new SucceededState(_options.SucceedMessageExpiredAfter);
				}
				await _stateChanger.ChangeStateAsync(message, newState, connection);

				fetched.RemoveFromQueue();

				if (result.Succeeded)
					_logger.JobExecuted(sp.Elapsed.TotalSeconds);

				return OperateResult.Success;
			}
			catch (SubscriberNotFoundException ex)
			{
				_logger.LogError(ex.Message);

				await AddErrorReasonToContent(message, ex.Message, connection);

				await _stateChanger.ChangeStateAsync(message, new FailedState(), connection);

				fetched.RemoveFromQueue();

				return OperateResult.Failed(ex);
			}
			catch (Exception ex)
			{
				_logger.ExceptionOccuredWhileExecutingJob(message?.Name, ex);

				fetched.Requeue();

				return OperateResult.Failed(ex);
			}
		}

		protected virtual async Task<OperateResult> ExecuteSubscribeAsync(CapReceivedMessage receivedMessage)
		{
			try
			{
				var executeDescriptorGroup = _selector.GetTopicExector(receivedMessage.Name);

				if (!executeDescriptorGroup.ContainsKey(receivedMessage.Group))
				{
					var error = $"Topic:{receivedMessage.Name}, can not be found subscriber method.";
					throw new SubscriberNotFoundException(error);
				}

				// If there are multiple consumers in the same group, we will take the first
				var executeDescriptor = executeDescriptorGroup[receivedMessage.Group][0];
				var consumerContext = new ConsumerContext(executeDescriptor, receivedMessage.ToMessageContext());

				await _consumerInvokerFactory.CreateInvoker(consumerContext).InvokeAsync();

				return OperateResult.Success;
			}
			catch (Exception ex)
			{
				_logger.ConsumerMethodExecutingFailed($"Group:{receivedMessage.Group}, Topic:{receivedMessage.Name}",
					ex);

				return OperateResult.Failed(ex);
			}
		}

		private static async Task<bool> UpdateMessageForRetryAsync(CapReceivedMessage message, IStorageConnection connection, string exceptionMessage)
		{
			var retryBehavior = RetryBehavior.DefaultRetry;

			var retries = ++message.Retries;
			if (retries >= retryBehavior.RetryCount)
				return false;

			var due = message.Added.AddSeconds(retryBehavior.RetryIn(retries));
			message.ExpiresAt = due;

			await AddErrorReasonToContent(message, exceptionMessage, connection);

			return true;
		}

		public static Task AddErrorReasonToContent(CapReceivedMessage message, string description, IStorageConnection connection)
		{
			var exceptions = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("ExceptionMessage", description)
			};

			message.Content = Helper.AddJsonProperty(message.Content, exceptions);
			using (var transaction = connection.CreateTransaction())
			{
				transaction.UpdateMessage(message);
				transaction.CommitAsync();
			}
			return Task.CompletedTask;
		}
	}
}