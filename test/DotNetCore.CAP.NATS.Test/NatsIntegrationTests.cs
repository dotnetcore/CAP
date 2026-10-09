// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Xunit;

namespace DotNetCore.CAP.NATS.Test;

public class NatsIntegrationTests
{
    [NatsIntegrationFact]
    public async Task PublishPreservesBytesHeadersAndDeduplicatesMessageId()
    {
        await using var scope = new NatsTestScope();
        var nativeOptions = NatsOpts.Default with { Url = "nats://127.0.0.1:1" };
        scope.Options.Options = nativeOptions;
        var received = new ConcurrentQueue<TransportMessage>();
        await scope.StartAsync("group", async (client, message, sender) =>
        {
            received.Enqueue(message);
            await client.CommitAsync(sender);
        });
        await scope.PublishAsync("same-id");
        await scope.PublishAsync("same-id");
        await scope.PublishAsync("empty", Array.Empty<byte>());
        await NatsTestScope.UntilAsync(() => received.Count == 2);
        var messages = received.ToArray();
        Assert.Equal(new byte[] { 0, 1, 128, 255 }, messages[0].Body.ToArray());
        Assert.Empty(messages[1].Body.ToArray());
        Assert.Equal("same-id", messages[0].GetId());
        Assert.Equal(scope.Topic, messages[0].GetName());
        Assert.Equal("group", messages[0].GetGroup());
        Assert.Equal("value", messages[0].Headers["test-header"]);
        var stream = await scope.JetStream.GetStreamAsync(scope.Stream);
        Assert.Equal(2L, stream.Info.State.Messages);
        Assert.Equal("nats://127.0.0.1:1", nativeOptions.Url);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task SameGroupCompetesAndDifferentGroupReceivesEveryMessage()
    {
        await using var scope = new NatsTestScope();
        var first = new ConcurrentBag<string>();
        var second = new ConcurrentBag<string>();
        var fanout = new ConcurrentBag<string>();
        await scope.StartAsync("shared.group", async (client, message, sender) =>
        {
            first.Add(message.GetId());
            await client.CommitAsync(sender);
        });
        await scope.StartAsync("shared.group", async (client, message, sender) =>
        {
            second.Add(message.GetId());
            await client.CommitAsync(sender);
        });
        await scope.StartAsync("other.group", async (client, message, sender) =>
        {
            fanout.Add(message.GetId());
            await client.CommitAsync(sender);
        });
        for (var i = 0; i < 40; i++) await scope.PublishAsync(i.ToString());
        await NatsTestScope.UntilAsync(() => first.Count + second.Count == 40 && fanout.Count == 40);
        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.Equal(40, first.Concat(second).Distinct().Count());
        Assert.Equal(40, fanout.Distinct().Count());
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task ConcurrentStartupUsesOneDurableConsumer()
    {
        await using var scope = new NatsTestScope();
        var received = new ConcurrentBag<string>();
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => scope.StartAsync("shared", async (client, message, sender) =>
        {
            received.Add(message.GetId());
            await client.CommitAsync(sender);
        })));
        for (var i = 0; i < 20; i++) await scope.PublishAsync(i.ToString());
        await NatsTestScope.UntilAsync(() => received.Count == 20);
        Assert.Equal(20, received.Distinct().Count());
        var stream = await scope.JetStream.GetStreamAsync(scope.Stream);
        Assert.Equal(1, stream.Info.State.ConsumerCount);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task NakAndAckWaitRedeliverMessages()
    {
        await using var scope = new NatsTestScope();
        scope.Options.ConsumerOptions = opts => opts with { AckWait = TimeSpan.FromMilliseconds(200) };
        var deliveries = new ConcurrentDictionary<string, int>();
        await scope.StartAsync("group", async (client, message, sender) =>
        {
            var count = deliveries.AddOrUpdate(message.GetId(), 1, (_, value) => value + 1);
            if (count > 1) await client.CommitAsync(sender);
            else if (message.GetId() == "nak") await client.RejectAsync(sender);
        });
        await scope.PublishAsync("nak");
        await scope.PublishAsync("timeout");
        await NatsTestScope.UntilAsync(() => deliveries.GetValueOrDefault("nak") >= 2 && deliveries.GetValueOrDefault("timeout") >= 2);
        await NatsTestScope.UntilAsync(async () =>
            (await scope.JetStream.GetPushConsumerAsync(scope.Stream, Helper.Normalized("group-" + scope.Topic))).Info.NumAckPending == 0);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task CustomHeadersAcceptHeaderlessForeignMessagesAndRecoverAfterException()
    {
        await using var scope = new NatsTestScope();
        var attempts = 0;
        scope.Options.CustomHeadersBuilder = (msg, _) =>
        {
            if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("test header error");
            return new List<KeyValuePair<string, string>>
            {
                new(Headers.MessageId, "foreign"), new(Headers.MessageName, msg.Subject)
            };
        };
        var received = new TaskCompletionSource<TransportMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await scope.StartAsync("group", async (client, message, sender) =>
        {
            await client.CommitAsync(sender);
            received.TrySetResult(message);
        }, concurrency: 1);
        var ack = await scope.JetStream.PublishAsync(scope.Topic, new byte[] { 255, 0 }, NatsRawSerializer<byte[]>.Default);
        ack.EnsureSuccess();
        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("foreign", result.GetId());
        Assert.Equal(scope.Topic, result.GetName());
        Assert.Equal(new byte[] { 255, 0 }, result.Body.ToArray());
        Assert.True(attempts >= 2);
        Assert.Contains(scope.Logs, log => log.Reason?.Contains("test header error") == true);
    }

    [NatsIntegrationFact]
    public async Task ConcurrencyIsBoundedAcrossTopicsAndCancellationDrainsCallbacks()
    {
        await using var scope = new NatsTestScope();
        var active = 0;
        var maximum = 0;
        var completed = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = await scope.StartAsync("group", async (consumer, _, sender) =>
        {
            var current = Interlocked.Increment(ref active);
            int previous;
            do { previous = maximum; } while (current > previous && Interlocked.CompareExchange(ref maximum, current, previous) != previous);
            await release.Task;
            await consumer.CommitAsync(sender);
            Interlocked.Decrement(ref active);
            Interlocked.Increment(ref completed);
        }, concurrency: 2, topics: new[] { scope.Topic, scope.Stream + ".second" });
        for (var i = 0; i < 8; i++) await scope.PublishAsync(i.ToString(), topic: i % 2 == 0 ? scope.Topic : scope.Stream + ".second");
        await NatsTestScope.UntilAsync(() => Volatile.Read(ref active) == 2);
        using var cancellation = new CancellationTokenSource();
        var listening = client.ListeningAsync(TimeSpan.FromSeconds(1), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listening);
        var disposal = client.DisposeAsync().AsTask();
        Assert.False(disposal.IsCompleted);
        release.TrySetResult();
        await disposal.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, maximum);
        Assert.Equal(2, completed);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task StreamSubjectsAndExistingSettingsArePreserved()
    {
        await using var scope = new NatsTestScope();
        await scope.JetStream.CreateStreamAsync(new StreamConfig(scope.Stream, new[] { scope.Stream + ".original" })
        {
            Storage = StreamConfigStorage.Memory,
            MaxMsgs = 1234,
            Description = "existing stream"
        });
        var first = await scope.CreateAsync("one");
        var second = await scope.CreateAsync("two");
        await Task.WhenAll(first.FetchTopicsAsync(new[] { scope.Topic }), second.FetchTopicsAsync(new[] { scope.Stream + ".second" }));
        var stream = await scope.JetStream.GetStreamAsync(scope.Stream);
        Assert.Equal(1234, stream.Info.Config.MaxMsgs);
        Assert.Equal("existing stream", stream.Info.Config.Description);
        Assert.Equal(new[] { scope.Topic, scope.Stream + ".original", scope.Stream + ".second" }.Order(), stream.Info.Config.Subjects!.Order());
    }

    [NatsIntegrationFact]
    public async Task DisabledStreamCreationDoesNotModifyStream()
    {
        await using var scope = new NatsTestScope();
        await scope.JetStream.CreateStreamAsync(new StreamConfig(scope.Stream, new[] { scope.Topic }) { Storage = StreamConfigStorage.Memory });
        scope.Options.EnableSubscriberClientStreamAndSubjectCreation = false;
        var client = await scope.CreateAsync("group");
        await client.FetchTopicsAsync(new[] { scope.Stream + ".other" });
        var stream = await scope.JetStream.GetStreamAsync(scope.Stream);
        Assert.Equal(new[] { scope.Topic }, stream.Info.Config.Subjects);
    }

    [NatsIntegrationFact]
    public async Task NewConsumerStartsWithNewMessagesButRestartKeepsPendingMessages()
    {
        await using var scope = new NatsTestScope();
        scope.Options.ConsumerOptions = opts => opts with { AckWait = TimeSpan.FromMilliseconds(200) };
        var setup = await scope.CreateAsync("group");
        await setup.FetchTopicsAsync(new[] { scope.Topic });
        await scope.PublishAsync("before-subscribe");
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = await scope.StartAsync("group", (_, _, _) => { received.TrySetResult(); return Task.CompletedTask; });
        await scope.PublishAsync("pending");
        await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var durable = Helper.Normalized("group-" + scope.Topic);
        var before = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(1, before.Info.NumAckPending);
        await first.DisposeAsync();
        var resumed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await scope.StartAsync("group", async (client, message, sender) =>
        {
            await client.CommitAsync(sender);
            resumed.TrySetResult(message.GetId());
        });
        Assert.Equal("pending", await resumed.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        var after = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(before.Info.Created, after.Info.Created);
        Assert.Equal(before.Info.Config.DeliverSubject, after.Info.Config.DeliverSubject);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task OldAndNewClientsShareDurableAndCanRollBackWithoutRecreatingIt()
    {
        await using var scope = new NatsTestScope();
        var setup = await scope.CreateAsync("group");
        await setup.FetchTopicsAsync(new[] { scope.Topic });
        using var oldConnection = new global::NATS.Client.ConnectionFactory().CreateConnection(scope.Options.Servers);
        var oldJs = oldConnection.CreateJetStreamContext();
        var durable = Helper.Normalized("group-" + scope.Topic);
        var config = global::NATS.Client.JetStream.ConsumerConfiguration.Builder()
            .WithDurable(durable).WithDeliverPolicy(global::NATS.Client.JetStream.DeliverPolicy.New)
            .WithAckPolicy(global::NATS.Client.JetStream.AckPolicy.Explicit).WithAckWait(30000).Build();
        var subscriptionOptions = global::NATS.Client.JetStream.PushSubscribeOptions.Builder()
            .WithStream(scope.Stream).WithConfiguration(config).Build();
        var oldMessages = new ConcurrentBag<string>();
        var newMessages = new ConcurrentBag<string>();
        var oldSubscription = oldJs.PushSubscribeAsync(scope.Topic, "group", (_, args) =>
        {
            oldMessages.Add(args.Message.Header[Headers.MessageId]);
            args.Message.Ack();
        }, false, subscriptionOptions);
        oldConnection.Flush();
        var before = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        var upgraded = await scope.StartAsync("group", async (client, message, sender) =>
        {
            newMessages.Add(message.GetId());
            await client.CommitAsync(sender);
        });
        for (var i = 0; i < 40; i++) await scope.PublishAsync(i.ToString());
        await NatsTestScope.UntilAsync(() => oldMessages.Count + newMessages.Count == 40);
        Assert.NotEmpty(oldMessages);
        Assert.NotEmpty(newMessages);
        Assert.Equal(40, oldMessages.Concat(newMessages).Distinct().Count());
        await upgraded.DisposeAsync();
        await scope.PublishAsync("rollback");
        await NatsTestScope.UntilAsync(() => oldMessages.Contains("rollback"));
        oldSubscription.Unsubscribe();
        var after = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(before.Info.Created, after.Info.Created);
        Assert.Equal(before.Info.Config.DeliverSubject, after.Info.Config.DeliverSubject);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task UpgradeRedeliversOldClientPendingMessageAndPreservesAckFloor()
    {
        await using var scope = new NatsTestScope();
        var setup = await scope.CreateAsync("group");
        await setup.FetchTopicsAsync(new[] { scope.Topic });
        var durable = Helper.Normalized("group-" + scope.Topic);
        var oldReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var oldConnection = new global::NATS.Client.ConnectionFactory().CreateConnection(scope.Options.Servers))
        {
            var config = global::NATS.Client.JetStream.ConsumerConfiguration.Builder()
                .WithDurable(durable).WithDeliverPolicy(global::NATS.Client.JetStream.DeliverPolicy.New)
                .WithAckPolicy(global::NATS.Client.JetStream.AckPolicy.Explicit).WithAckWait(1000).Build();
            var subscriptionOptions = global::NATS.Client.JetStream.PushSubscribeOptions.Builder()
                .WithStream(scope.Stream).WithConfiguration(config).Build();
            var subscription = oldConnection.CreateJetStreamContext().PushSubscribeAsync(scope.Topic, "group", (_, args) =>
            {
                if (args.Message.Header[Headers.MessageId] == "acked") args.Message.Ack();
                else oldReceived.TrySetResult();
            }, false, subscriptionOptions);
            oldConnection.Flush();
            await scope.PublishAsync("acked");
            await NatsTestScope.UntilAsync(async () =>
                (await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable)).Info.AckFloor.StreamSeq == 1);
            await scope.PublishAsync("pending");
            await oldReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            subscription.Unsubscribe();
        }
        var before = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(1, before.Info.NumAckPending);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await scope.StartAsync("group", async (client, message, sender) =>
        {
            await client.CommitAsync(sender);
            received.TrySetResult(message.GetId());
        });
        Assert.Equal("pending", await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        await NatsTestScope.UntilAsync(async () =>
            (await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable)).Info.AckFloor.StreamSeq == 2);
        var after = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(before.Info.Created, after.Info.Created);
        Assert.Equal(before.Info.Config.DeliverSubject, after.Info.Config.DeliverSubject);
        Assert.Empty(scope.Logs);
    }

    [NatsIntegrationFact]
    public async Task IncompatibleExistingConsumerFailsWithoutBeingRecreated()
    {
        await using var scope = new NatsTestScope();
        var setup = await scope.CreateAsync("group");
        await setup.FetchTopicsAsync(new[] { scope.Topic });
        var durable = Helper.Normalized("group-" + scope.Topic);
        var original = await scope.JetStream.CreatePushConsumerAsync(scope.Stream, new NatsJSPushConsumerOpts
        {
            DurableName = durable, DeliverSubject = scope.JetStream.NewBaseInbox(),
            FilterSubject = scope.Topic, DeliverGroup = "wrong-group"
        });
        var client = await scope.CreateAsync("group");
        await Assert.ThrowsAsync<BrokerConnectionException>(() => client.SubscribeAsync(new[] { scope.Topic }));
        var after = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
        Assert.Equal(original.Info.Created, after.Info.Created);
        Assert.Equal("wrong-group", after.Info.Config.DeliverGroup);
    }

    [NatsIntegrationFact]
    public async Task PublishToMissingStreamReturnsCapFailure()
    {
        await using var scope = new NatsTestScope();
        var result = await scope.Transport.SendAsync(new TransportMessage(new Dictionary<string, string?>
        {
            [Headers.MessageId] = "missing", [Headers.MessageName] = scope.Topic
        }, Array.Empty<byte>()));
        Assert.False(result.Succeeded);
    }

    [NatsIntegrationFact]
    public async Task PoolReusesConnectionsAndDisposesReturnsAfterShutdown()
    {
        await using var scope = new NatsTestScope();
        var first = await scope.Pool.RentConnectionAsync();
        Assert.True(await scope.Pool.ReturnAsync(first));
        var second = await scope.Pool.RentConnectionAsync();
        Assert.Same(first, second);
        await scope.Pool.DisposeAsync();
        Assert.False(await scope.Pool.ReturnAsync(second));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => scope.Pool.RentConnectionAsync().AsTask());
        var failed = await scope.Transport.SendAsync(new TransportMessage(new Dictionary<string, string?>
        {
            [Headers.MessageId] = "after-disposal", [Headers.MessageName] = scope.Topic
        }, Array.Empty<byte>()));
        Assert.False(failed.Succeeded);
        Assert.IsType<PublisherSentFailedException>(failed.Exception);
    }
}

internal sealed class NatsIntegrationFactAttribute : FactAttribute
{
    public NatsIntegrationFactAttribute()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR_BUILD_ID")))
            Skip = "Requires a NATS server with JetStream enabled; AppVeyor does not provision one.";
    }
}
