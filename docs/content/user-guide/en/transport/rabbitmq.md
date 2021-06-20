# RabbitMQ

RabbitMQ is an open-source message-broker software that originally implemented the Advanced Message Queuing Protocol and has since been extended with a plug-in architecture to support Streaming Text Oriented Messaging Protocol, Message Queuing Telemetry Transport, and other protocols.

RabbitMQ can be used in CAP as a message transporter. 

## Configuration

To use RabbitMQ transporter, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.RabbitMQ

```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseRabbitMQ(opt=>
        {
            //RabbitMQOptions
        });
        // x.UseXXX ...
    });
}

```

#### RabbitMQ Options

The RabbitMQ configuration parameters provided directly by CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
HostName | Broker host address | string | localhost
UserName | Broker user name | string | guest
Password | Broker password | string | guest
VirtualHost | Broker virtual host | string | /
Port | Port | int | -1
ExchangeName | Default exchange name | string | cap.default.topic
QueueArguments  | Extra queue `x-arguments` | QueueArgumentsOptions  |  N/A
ConnectionFactoryOptions  |  RabbitMQClient native connection options | ConnectionFactory | N/A
CustomHeaders  | Custom subscribe headers |  Func<BasicDeliverEventArgs, List<KeyValuePair<string, string>>> |  N/A

#### ConnectionFactory Options

If you need **more** native `ConnectionFactory` configuration options, you can set it by 'ConnectionFactoryOptions' option:

```csharp

services.AddCap(x =>
{
    x.UseRabbitMQ(o =>
    {
        o.HostName = "localhost";
        o.ConnectionFactoryOptions = opt => { 
            //rabbitmq client ConnectionFactory config
        };
    });
});

```

#### CustomHeaders Options

When the message sent from the RabbitMQ management console or a heterogeneous system, because of the CAP needs to define additional headers, so an exception will occur at this time. By providing this parameter to set the custom headersn to make the subscriber works.

You can find the description of [Header Information](../cap/messaging#heterogeneous-system-integration) here.

Example：

```cs
x.UseRabbitMQ(aa =>
{
    aa.CustomHeaders = e => new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>(Headers.MessageId, SnowflakeId.Default().NextId().ToString()),
        new KeyValuePair<string, string>(Headers.MessageName, e.RoutingKey),
    };
});
```

#### How to connect cluster

using comma split connection string, like this:

```
x=> x.UseRabbitMQ("localhost:5672,localhost:5673,localhost:5674")
```