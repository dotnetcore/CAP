# RabbitMQ


## RabbitMQ Configs

The CAP uses the CapOptions extension to implement the RabbitMQ configuration function. Therefore, the configuration of the RabbitMQ is used as follows:

```cs
services.AddCap(capOptions => {
    capOptions.UseRabbitMQ(rabbitMQOption=>{
        // rabbitmq options.
    });
});
```
`RabbitMQOptions` provides related RabbitMQ configuration:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
HostName | Host Address | string | localhost
UserName | username | string | guest
Password | Password | string | guest
VirtualHost | Virtual Host | string | /
Port | Port number | int | -1
TopicExchangeName | CAP Default Exchange Name | string | cap.default.topic
RequestedConnectionTimeout | RabbitMQ Connection Timeout | int | 30,000 milliseconds
SocketReadTimeout | RabbitMQ message read timeout | int | 30,000 milliseconds
SocketWriteTimeout | RabbitMQ message write timeout | int | 30,000 milliseconds
QueueMessageExpires | Automatic deletion of messages in queue | int | (10 days) ms