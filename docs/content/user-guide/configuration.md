# Configuration

CAP uses Microsoft.Extensions.DependencyInjection for configuration injection. 

## CAP Configs

You can use the following methods to configure some configuration items in the CAP, for example:

```cs
services.AddCap(capOptions => {
    capOptions.FailedCallback = //...
});

```

`CapOptions` provides the following configuration items:：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
DefaultGroup | Default consumer group to which the subscriber belongs | string | cap.queue+{assembly name}
SuccessedMessageExpiredAfter | Expiration date after successful message was deleted | int | 3600 seconds
FailedCallback|Callback function when the failed message is executed. See below for details | Action | NULL
FailedRetryInterval | Failed Retry Interval | int | 60 seconds
FailedRetryCount | Failed RetryCount | int | 50th

CapOptions provides a callback function for `FailedCallback` to handle failed messages. When the message fails to be sent multiple times, the CAP will mark the message state as `Failed`. The CAP has a special handler to handle this failed message. The failed message will be put back into the queue and sent to MQ. Prior to this, if `FailedCallback` has a value, this callback function will be called first to tell the client.

The type of FailedCallback is `Action<MessageType,string,string>`. The first parameter is the message type (send or receive), the second parameter is the name of the message, and the third parameter is the content of the message.

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

### Kafka Configs

CAP adopts Kafka's configuration function to expand CapOptions, so the configuration usage for Kafka is as follows:

```cs
services.AddCap(capOptions => {
    capOptions.UseKafka(kafkaOption=>{
        // kafka options.
        // kafkaOptions.MainConfig.Add("", "");
    });
});
```

`KafkaOptions` provides Kafka-related configurations. Because Kafka has more configurations, the MainConfig dictionary provided here is used to support custom configurations. You can check here to get support information for configuration items.

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)


### EntityFramework Configs

If you are using Entityframework as a message persistence store, then you can customize some configuration when configuring the CAP EntityFramework configuration item.

```cs
services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>(efOption => 
    {
        // entityframework options.
    });
});

```

Note that if you use the `UseEntityFramework` configuration item, then you do not need to reconfigure the following sections for several different database configurations. The CAP will automatically read the database configuration information used in DbContext.

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
Schema | Cap table schema | string | Cap (SQL Server)
Schema | Cap table schema | string | cap (PostgreSql)
TableNamePrefix | Cap table name prefix | string | cap (MySql)

### SqlServer Configs

Note that if you are using EntityFramewrok, you do not use this configuration item.

CAP adopts the configuration function of SqlServer for extending CapOptions. Therefore, the configuration usage of SqlServer is as follows:

```cs
services.AddCap(capOptions => {
    capOptions.UseSqlServer(sqlserverOptions => {
       // sqlserverOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
Schema | Cap Table Schema | string | Cap
ConnectionString | Database connection string | string | null

### MySql Configs

Note that if you are using EntityFramewrok, you do not use this configuration item.

CAP uses the configuration function for MySql that extends for CapOptions, so the configuration usage for MySql is as follows:

```cs
services.AddCap(capOptions => {
    capOptions.UseMySql(mysqlOptions => {
       // mysqlOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
TableNamePrefix | Cap table name prefix | string | cap
ConnectionString | Database connection string | string | null

### PostgreSql Configs

Note that if you are using EntityFramewrok, you do not use this configuration item.

CAP uses PostgreSql configuration functions for CapOptions extensions, so the configuration usage for PostgreSql is as follows:

```c#
services.AddCap(capOptions => {
    capOptions.UsePostgreSql(postgreOptions => {
       // postgreOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:------
Schema | Cap table name prefix | string | cap
ConnectionString | Database connection string | string | null