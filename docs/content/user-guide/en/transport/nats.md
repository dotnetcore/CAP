# NATS

[NATS](https://nats.io/) is a simple, secure and performant communications system for digital systems, services and devices. NATS is part of the Cloud Native Computing Foundation (CNCF).

!!! warning
    Since version 5.2+, CAP features are implemented based on [JetStream](https://docs.nats.io/nats-concepts/jetstream), so JetStream must be explicitly enabled on the server.

    **You must enable JetStream by specifying the `--jetstream` parameter when starting the NATS server to use CAP properly.**

## Configuration

To use NATS as a transporter, you need to install the following package from NuGet:

```powershell

PM> Install-Package DotNetCore.CAP.NATS

```

Then you can add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(capOptions =>
    {
        capOptions.UseNATS(natsOptions=>{
            //NATS Options
        });
    });
}

```

#### NATS Options

NATS configuration parameters provided directly by the CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Options | Native NATS.NET connection configuration. Servers takes precedence over Options.Url. | `NATS.Client.Core.NatsOpts` | Client defaults
Servers | Server URL or URLs. | string | `nats://127.0.0.1:4222`
ConnectionPoolSize  | Maximum number of idle publisher connections retained. | int | 10
EnableSubscriberClientStreamAndSubjectCreation | Allow consumer clients to create streams and subjects. | bool | true
StreamOptions | Stream configuration | `Action<StreamConfig>` | NULL
ConsumerOptions | New durable push consumer configuration | `Func<NatsJSPushConsumerOpts, NatsJSPushConsumerOpts>` | NULL
CustomHeadersBuilder | Custom subscribe headers |  `INatsJSMsg<byte[]>`; see below | NULL
NormalizeStreamName | Converts a topic name to its stream name. | `Func<string, string>` | First segment before `.`

#### NATS Configuration Options

If you need additional native NATS configuration options, you can set them in the `Options` option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseNATS(natsOptions=>
    {
        natsOptions.Servers = "nats://127.0.0.1:4222";
        natsOptions.Options = NATS.Client.Core.NatsOpts.Default with { Name = "CAP" };
    });
});
```

`Options` uses [NATS.NET NatsOpts](https://nats-io.github.io/nats.net/api/NATS.Client.Core.NatsOpts.html). CAP consumer connections use a five-second connect timeout, disable echo and client reconnection, and report failures to CAP for recovery. Consumer buffers use bounded channels with wait-on-full behavior; `Options.SubPendingChannelCapacity` configures capacity for both new and reused consumers.

#### Custom Headers Builder Option

When messages are sent from a heterogeneous system, CAP requires additional headers to be defined. By providing this parameter, you can set custom headers to ensure the subscriber works correctly.

You can find the description of [Header Information](../cap/messaging.md#heterogeneous-system-integration) here.

Example:

```cs
x.UseNATS(aa =>
{
    aa.CustomHeadersBuilder = (e, sp) =>
    [
        new(DotNetCore.CAP.Messages.Headers.MessageId, sp.GetRequiredService<ISnowflakeId>().NextId().ToString()),
        new(DotNetCore.CAP.Messages.Headers.MessageName, e.Subject)
    ];
});
```

#### Migrating from NATS.Client 1.x

The transport now uses NATS.NET 3.3.0 (`NATS.Client.JetStream` and its Core dependency). `UseNATS("nats://...")` remains unchanged. Applications using native client types must update their configuration:

| Previous API | New API |
| --- | --- |
| `NATS.Client.Options` | `NATS.Client.Core.NatsOpts` |
| Stream configuration builder | `StreamConfig` with writable properties |
| Consumer configuration builder | Return a new `NatsJSPushConsumerOpts` using `with` |
| `MsgHandlerEventArgs.Message` | `INatsJSMsg<byte[]>` directly: `Subject`, `Headers`, `Data`, `Metadata` |
| `IConnectionPool.RentConnection()` / `Return()` | Await `RentConnectionAsync()` / `ReturnAsync()`; native connections are `INatsConnection` |
| Synchronous connection-pool disposal | `IAsyncDisposable.DisposeAsync()` |

```csharp
services.AddCap(cap => cap.UseNATS(nats =>
{
    nats.Servers = "nats://127.0.0.1:4222";
    nats.Options = NATS.Client.Core.NatsOpts.Default with { Name = "CAP" };
    nats.StreamOptions = stream =>
    {
        stream.MaxMsgs = 100_000;
    };
    nats.ConsumerOptions = consumer => consumer with
    {
        AckWait = TimeSpan.FromSeconds(60),
        MaxAckPending = 1_000
    };
}));
```

CAP continues to use durable queue push consumers: instances in the same CAP group compete for messages, while different groups receive their own copy. New consumers start with new messages, explicit acknowledgments and a 30-second acknowledgment timeout. CAP acknowledges after storing and enqueueing a received message; business-method retries remain managed by CAP. Delivery is at least once, so applications must tolerate redelivery.

Existing consumers are looked up by the previous durable naming rule and reuse their server-side delivery subject and acknowledgment progress. `ConsumerOptions` configures creation; it does not overwrite existing consumer settings. Incompatible topic filters, queue groups or acknowledgment policies fail explicitly without deleting the consumer. Keep group names, topics, stream normalization and any custom durable name unchanged during rolling upgrades and rollbacks.

For local subscription settings, prefer connection-level `Options.SubPendingChannelCapacity`. In NATS.NET 3.3.0, `ConsumerOptions.SubOpts` only applies when creating a consumer; consumers obtained with `GetPushConsumerAsync` use connection defaults. A configured `NotificationHandler` is also passed when consuming an existing consumer.

Stream updates preserve existing settings and merge subjects before applying `StreamOptions`. For streams managed externally, disable `EnableSubscriberClientStreamAndSubjectCreation`; CAP will still create or reuse its consumers. Concurrent updates from separate processes are not transactional; centrally provision subjects when multiple applications manage the same stream.
