# RabbitMQ

RabbitMQ is an open-source message-broker software that originally implemented the Advanced Message Queuing Protocol and has since been extended with a plug-in architecture to support Streaming Text Oriented Messaging Protocol, Message Queuing Telemetry Transport, and other protocols.

RabbitMQ can be used in CAP as a message transporter. 

!!! warning "Notes"
    When using RabbitMQ, the consumer integrated with the CAP application will automatically create a persistent queue after it is started for the first time. Subsequent messages will be normally transmitted to the queue and consumed.
    However, if you have never started the consumer, the queue will not be created. In this case, if you publish messages first, RabbitMQ Exchange will discard the messages received directly until the consumer is started and the queue is created.

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
QueueOptions  | Change Options for created queue | QueueRabbitOptions  |  { Durable=true, Exclusive=false, AutoDelete=false }
ConnectionFactoryOptions  |  RabbitMQClient native connection options | ConnectionFactory | N/A
CustomHeadersBuilder  | Custom subscribe headers |  See the blow |  N/A
PublishConfirms | Enable [publish confirms](https://www.rabbitmq.com/confirms.html#publisher-confirms) | bool | false
BasicQosOptions | Specify [Qos](https://www.rabbitmq.com/consumer-prefetch.html) of message prefetch | BasicQos | N/A

#### ConnectionFactory Option

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

#### CustomHeadersBuilder Option

When the message sent from the RabbitMQ management console or a heterogeneous system, because of the CAP needs to define additional headers, so an exception will occur at this time. By providing this parameter to set the custom headersn to make the subscriber works.

You can find the description of [Header Information](../cap/messaging.md#heterogeneous-system-integration) here.

Example：

```cs
x.UseRabbitMQ(aa =>
{
    aa.CustomHeadersBuilder = (msg, sp) =>
    [
        new(DotNetCore.CAP.Messages.Headers.MessageId, sp.GetRequiredService<ISnowflakeId>().NextId().ToString()),
        new(DotNetCore.CAP.Messages.Headers.MessageName, msg.RoutingKey)
    ];
});
```

#### How to connect cluster

using comma split connection string, like this:

```
x=> x.UseRabbitMQ("localhost:5672,localhost:5673,localhost:5674")
```