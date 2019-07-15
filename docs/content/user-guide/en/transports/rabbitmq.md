# RabbitMQ

RabbitMQ is an open-source message-broker software that originally implemented the Advanced Message Queuing Protocol and has since been extended with a plug-in architecture to support Streaming Text Oriented Messaging Protocol, Message Queuing Telemetry Transport, and other protocols.

CAP has supported RabbitMQ as message transporter. 

## Configuration

To use RabbitMQ transporter, you need to install the following extensions from NuGet:

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

The RabbitMQ configuration parameters provided directly by the CAP are as follows:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
HostName | Broker host address | string | localhost
UserName | Broker user name | string | guest
Password | Broker password | string | guest
VirtualHost | Broker virtual host | string | /
Port | Port | int | -1
TopicExchangeName | Default exchange name of cap created | string | cap.default.topic
QueueMessageExpires |  Message expries after to delete, in milliseconds | int | (10 days) milliseconds

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