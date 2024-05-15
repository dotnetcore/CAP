# NATS

[NATS](https://nats.io/) is a simple, secure and performant communications system for digital systems, services and devices. NATS is part of the Cloud Native Computing Foundation (CNCF).

!!! warning
    Since version 5.2+, CAP's relevant features have been implemented based on [JetStream](https://docs.nats.io/nats-concepts/jetstream), so it needs to be explicitly enabled on the server.

    **You need to enable JetStream by specifying the `--jetstream` parameter when starting the NATS Server in order to use CAP properly.**

## Configuration

To use NATS transporter, you need to install the following package from NuGet:

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
Options | NATS client configuration | Options | Options
Servers | Server url/urls used to connect to the NATs server. | string | NULL
ConnectionPoolSize  | number of connections pool | uint | 10
DeliverPolicy | The point in the stream to receive messages from (⚠️ Removed from version 8.1.0, use `ConsumerOptions` instead.) | enum | DeliverPolicy.New
StreamOptions | 🆕 Stream configuration |  Action | NULL
ConsumerOptions | 🆕 Consumer configuration | Action | NULL
CustomHeadersBuilder | Custom subscribe headers |  See the blow | NULL

#### NATS ConfigurationOptions

If you need **more** native NATS related configuration options, you can set them in the `Options` option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseNATS(natsOptions=>
    {
        // NATS options.
        natsOptions.Options.Url="";
    });
});
```

`Options` is a NATS.Client ConfigurationOptions , you can find more details through this [link](http://nats-io.github.io/nats.net/class_n_a_t_s_1_1_client_1_1_options.html)

#### CustomHeadersBuilder Option

When the message sent from a heterogeneous system, because of the CAP needs to define additional headers, so an exception will occur at this time. By providing this parameter to set the custom headersn to make the subscriber works.

You can find the description of [Header Information](../cap/messaging.md#heterogeneous-system-integration) here.

Example：

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
