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
Options | Optional NATS.Client connection configuration; if omitted, the client library defaults are used. | `NATS.Client.Options` | Client defaults
Servers | Server URL or URLs. | string | `nats://127.0.0.1:4222`
ConnectionPoolSize  | Number of pooled connections. | int | 10
EnableSubscriberClientStreamAndSubjectCreation | Allow consumer clients to create streams and subjects. | bool | true
StreamOptions | 🆕 Stream configuration |  Action | NULL
ConsumerOptions | 🆕 Consumer configuration | Action | NULL
CustomHeadersBuilder | Custom subscribe headers |  See the blow | NULL
NormalizeStreamName | Converts a topic name to its stream name. | `Func<string, string>` | First segment before `.`

#### NATS Configuration Options

If you need additional native NATS configuration options, you can set them in the `Options` option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseNATS(natsOptions=>
    {
        // NATS options.
        natsOptions.Options = NATS.Client.ConnectionFactory.GetDefaultOptions();
        natsOptions.Options.Url = "nats://127.0.0.1:4222";
    });
});
```

`Options` is a NATS.Client `ConfigurationOptions`. You can find more details at this [link](http://nats-io.github.io/nats.net/class_n_a_t_s_1_1_client_1_1_options.html).

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
        new(DotNetCore.CAP.Messages.Headers.MessageName, e.Message.Subject)
    ];
});
```
