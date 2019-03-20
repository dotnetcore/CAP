// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DotNetCore.CAP.Processor
{
	public class Dispatcher : IDispatcher, IDisposable
	{
		private readonly CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly ISubscriberExecutor _executor;
		private readonly ILogger<Dispatcher> _logger;
		private readonly CapOptions _capOptions;

		private readonly BlockingCollection<CapPublishedMessage> _publishedMessageQueue =
			new BlockingCollection<CapPublishedMessage>(new ConcurrentQueue<CapPublishedMessage>());

		private readonly BlockingCollection<CapReceivedMessage> _receivedMessageQueue =
			new BlockingCollection<CapReceivedMessage>(new ConcurrentQueue<CapReceivedMessage>());

		private readonly ConcurrentBag<BlockingCollection<CapReceivedMessage>> _receivedMessageQueueList =
		   new ConcurrentBag<BlockingCollection<CapReceivedMessage>>();

		private readonly IPublishMessageSender _sender;

		public Dispatcher(ILogger<Dispatcher> logger,
			IPublishMessageSender sender,
			ISubscriberExecutor executor,
			CapOptions capOptions)
		{
			_logger = logger;
			_sender = sender;
			_executor = executor;
			_capOptions = capOptions;
			for (int i = 1; i <= _capOptions.ConsumerCount; i++)
			{
				var receivedMessageQueue = new BlockingCollection<CapReceivedMessage>(new ConcurrentQueue<CapReceivedMessage>());
				_receivedMessageQueueList.Add(receivedMessageQueue);
				Task.Factory.StartNew(_ => DoProcessing(receivedMessageQueue), receivedMessageQueue);
			}
			Task.Factory.StartNew(Sending);
			//Task.Factory.StartNew(Processing);
		}

		public void EnqueueToPublish(CapPublishedMessage message)
		{
			_publishedMessageQueue.Add(message);
		}

		public void EnqueueToExecute(CapReceivedMessage message)
		{
			int next = new Random().Next(0, _capOptions.ConsumerCount - 1);
			_receivedMessageQueueList.ToList()[next].Add(message);
		}

		public void Dispose()
		{
			_cts.Cancel();
		}

		private void Sending()
		{
			try
			{
				while (!_publishedMessageQueue.IsCompleted)
				{
					if (_publishedMessageQueue.TryTake(out var message, 100, _cts.Token))
					{
						try
						{
							_sender.SendAsync(message);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Topic:{message.Name}, Id:{message.Id}");
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				// expected
			}
		}
		private void DoProcessing(BlockingCollection<CapReceivedMessage> receivedMessageQueue)
		{
			try
			{
				foreach (var message in receivedMessageQueue.GetConsumingEnumerable(_cts.Token))
				{
					_executor.ExecuteAsync(message);
				}
			}
			catch (OperationCanceledException)
			{
				// expected
			}
		}
		private void Processing()
		{
			try
			{
				foreach (var message in _receivedMessageQueue.GetConsumingEnumerable(_cts.Token))
				{
					_executor.ExecuteAsync(message);
				}
			}
			catch (OperationCanceledException)
			{
				// expected
			}
		}
	}
}
