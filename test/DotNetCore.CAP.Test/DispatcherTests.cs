using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Test.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DotNetCore.CAP.Test;

public class DispatcherTests 
{
    private readonly ILogger<Dispatcher> _logger;
    private readonly ISubscribeExecutor _executor;
    private readonly IDataStorage _storage;

    public DispatcherTests()
    {
        _logger = Substitute.For<ILogger<Dispatcher>>();
        _executor = Substitute.For<ISubscribeExecutor>();
        _storage = Substitute.For<IDataStorage>();
    }

    [Fact]
    public async Task EnqueueToPublish_ShouldInvokeSend_WhenParallelSendDisabled()
    {
        // Arrange
        var sender = new TestThreadSafeMessageSender();
        var options = Options.Create(new CapOptions
        {
            EnableSubscriberParallelExecute = true,
            EnablePublishParallelSend = false,
            SubscriberParallelExecuteThreadCount = 2,
            SubscriberParallelExecuteBufferFactor = 2
        });
        
        var dispatcher = new Dispatcher(_logger, sender, options, _executor, _storage);

        using var cts = new CancellationTokenSource();
        var messageId = "testId";
        
        // Act
        await dispatcher.Start(cts.Token);
        await dispatcher.EnqueueToPublish(CreateTestMessage(messageId));
        await cts.CancelAsync();
        
        // Assert
        sender.Count.Should().Be(1);
        sender.ReceivedMessages.First().DbId.Should().Be(messageId);
    }
    
    [Fact]
    public async Task EnqueueToPublish_ShouldBeThreadSafe_WhenParallelSendDisabled()
    {
        // Arrange
        var sender = new TestThreadSafeMessageSender();
        var options = Options.Create(new CapOptions
        {
            EnableSubscriberParallelExecute = true,
            EnablePublishParallelSend = false,
            SubscriberParallelExecuteThreadCount = 2,
            SubscriberParallelExecuteBufferFactor = 2
        });
        var dispatcher = new Dispatcher(_logger, sender, options, _executor, _storage);

        using var cts = new CancellationTokenSource();
        var messages = Enumerable.Range(1, 100)
            .Select(i => CreateTestMessage(i.ToString()))
            .ToArray();

        // Act
        await dispatcher.Start(cts.Token);

        var tasks = messages
            .Select(msg => Task.Run(() => dispatcher.EnqueueToPublish(msg), CancellationToken.None));
        await Task.WhenAll(tasks);
        await cts.CancelAsync();

        // Assert
        sender.Count.Should().Be(100);
        var receivedMessages = sender.ReceivedMessages.Select(m => m.DbId).Order().ToList();
        var expected = messages.Select(m => m.DbId).Order().ToList();
        expected.Should().Equal(receivedMessages);
    }

    [Fact]
    public async Task EnqueueToScheduler_ShouldBeThreadSafe_WhenDelayLessThenMinute()
    {
        // Arrange
        var sender = new TestThreadSafeMessageSender();
        var options = Options.Create(new CapOptions
        {
            EnableSubscriberParallelExecute = true,
            EnablePublishParallelSend = false,
            SubscriberParallelExecuteThreadCount = 2,
            SubscriberParallelExecuteBufferFactor = 2
        });
        var dispatcher = new Dispatcher(_logger, sender, options, _executor, _storage);

        using var cts = new CancellationTokenSource();
        var messages = Enumerable.Range(1, 10000)
            .Select(i => CreateTestMessage(i.ToString()))
            .ToArray();

        // Act
        await dispatcher.Start(cts.Token);
        var dateTime = DateTime.Now.AddSeconds(1);
        await Parallel.ForEachAsync(messages, CancellationToken.None,
            async (m, ct) => { await dispatcher.EnqueueToScheduler(m, dateTime); });

        await Task.Delay(1500, CancellationToken.None);

        await cts.CancelAsync();

        // Assert
        sender.Count.Should().Be(10000);

        var receivedMessages = sender.ReceivedMessages.Select(m => m.DbId).Order().ToList();
        var expected = messages.Select(m => m.DbId).Order().ToList();
        expected.Should().Equal(receivedMessages);
    }

    [Fact]
    public async Task EnqueueToScheduler_ShouldSendMessagesInCorrectOrder_WhenEarlierMessageIsSentLater()
    {
        // Arrange
        var sender = new TestThreadSafeMessageSender();
        var options = Options.Create(new CapOptions
        {
            EnableSubscriberParallelExecute = true,
            EnablePublishParallelSend = false,
            SubscriberParallelExecuteThreadCount = 2,
            SubscriberParallelExecuteBufferFactor = 2
        });
        var dispatcher = new Dispatcher(_logger, sender, options, _executor, _storage);

        using var cts = new CancellationTokenSource();
        var messages = Enumerable.Range(1, 3)
            .Select(i => CreateTestMessage(i.ToString()))
            .ToArray();

        // Act
        await dispatcher.Start(cts.Token);
        var dateTime = DateTime.Now;
        
        await dispatcher.EnqueueToScheduler(messages[0], dateTime.AddSeconds(1));
        await dispatcher.EnqueueToScheduler(messages[1], dateTime.AddMilliseconds(200));
        await dispatcher.EnqueueToScheduler(messages[2], dateTime.AddMilliseconds(100));

        await Task.Delay(1200, CancellationToken.None);
        await cts.CancelAsync();
        
        // Assert
        sender.ReceivedMessages.Select(m => m.DbId).Should().Equal(["3", "2", "1"]);
    }
    

    private MediumMessage CreateTestMessage(string id = "1")
    {
        return new MediumMessage()
        {
            DbId = id,
            Origin = new Message(
                headers: new Dictionary<string, string>()
                {
                    { "cap-msg-id", id }
                },
                value: new MessageValue("test@test.com", "User"))
        };
    }
}