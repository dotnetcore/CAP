// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.NATS;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Xunit;

namespace DotNetCore.CAP.NATS.Test;

internal sealed class NatsTestScope : IAsyncDisposable
{
    private readonly ConcurrentBag<NATSConsumerClient> _clients = new();
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();

    public NatsTestScope(string? server = null)
    {
        Stream = "cap_test_" + Guid.NewGuid().ToString("N");
        Options = new NATSOptions { Servers = server ?? Environment.GetEnvironmentVariable("CAP_NATS_TEST_URL") ?? "nats://127.0.0.1:4222" };
        Connection = new NatsConnection(new NatsOpts { Url = Options.Servers, PublishTimeoutOnDisconnected = true });
        JetStream = new NatsJSContext(Connection);
        Pool = new ConnectionPool(NullLoggerFactory.Instance, Microsoft.Extensions.Options.Options.Create(Options));
        Transport = new NATSTransport(NullLogger<NATSTransport>.Instance, Pool);
    }

    public string Stream { get; }
    public string Topic => Stream + ".event";
    public NATSOptions Options { get; }
    public NatsConnection Connection { get; }
    public NatsJSContext JetStream { get; }
    public ConnectionPool Pool { get; }
    public NATSTransport Transport { get; }
    public ConcurrentQueue<LogMessageEventArgs> Logs { get; } = new();

    public async Task<NATSConsumerClient> StartAsync(string group,
        Func<NATSConsumerClient, TransportMessage, object?, Task> callback, byte concurrency = 0,
        string[]? topics = null)
    {
        var client = await CreateAsync(group, concurrency);
        client.OnMessageCallback = (message, sender) => callback(client, message, sender);
        await client.FetchTopicsAsync(topics ?? new[] { Topic });
        await client.SubscribeAsync(topics ?? new[] { Topic });
        return client;
    }

    public async Task<NATSConsumerClient> CreateAsync(string group, byte concurrency = 0)
    {
        var factory = new NATSConsumerClientFactory(Microsoft.Extensions.Options.Options.Create(Options), _services);
        var client = (NATSConsumerClient)await factory.CreateAsync(group, concurrency);
        _clients.Add(client);
        client.OnLogCallback = Logs.Enqueue;
        return client;
    }

    public async Task PublishAsync(string id, byte[]? body = null, string? topic = null)
    {
        var result = await Transport.SendAsync(new TransportMessage(new Dictionary<string, string?>
        {
            [Headers.MessageId] = id,
            [Headers.MessageName] = topic ?? Topic,
            ["test-header"] = "value"
        }, body ?? new byte[] { 0, 1, 128, 255 }));
        Assert.True(result.Succeeded, result.Exception?.ToString());
    }

    public static async Task UntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!await condition().WaitAsync(TimeSpan.FromSeconds(10)))
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the NATS test condition.");
            await Task.Delay(20);
        }
    }

    public static Task UntilAsync(Func<bool> condition) => UntilAsync(() => Task.FromResult(condition()));

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients) await client.DisposeAsync();
        try
        {
            await JetStream.DeleteStreamAsync(Stream);
        }
        catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10059)
        {
        }
        await Pool.DisposeAsync();
        await Connection.DisposeAsync();
        await _services.DisposeAsync();
    }
}
