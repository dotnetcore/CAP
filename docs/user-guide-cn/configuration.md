## 配置

CAP 使用 Microsoft.Extensions.DependencyInjection 进行配置的注入，你也可以依赖于 DI 从json文件中读取配置。

### Cap Options

你可以使用如下方式来配置 CAP 中的一些配置项，例如

```cs
services.AddCap(capOptions => {
    capOptions.FailedCallback = //...
});

```

`CapOptions`  提供了以下配置项：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
DefaultGroup | 订阅者所属的默认消费者组 | string | cap.queue+程序集名称
SuccessedMessageExpiredAfter | 成功的消息被删除的过期时间 | int | 3600 秒
FailedCallback| 执行失败消息时的回调函数，详情见下文 | Action | NULL
FailedRetryInterval | 失败重试间隔时间 | int | 60 秒
FailedRetryCount | 失败最大重试次数 | int | 50 次

CapOptions 提供了 `FailedCallback` 为处理失败的消息时的回调函数。当消息多次发送失败后，CAP会将消息状态标记为`Failed`，CAP有一个专门的处理者用来处理这种失败的消息，针对失败的消息会重新放入到队列中发送到MQ，在这之前如果`FailedCallback`具有值，那么将首先调用此回调函数来告诉客户端。

FailedCallback 的类型为 `Action<MessageType,string,string>`，第一个参数为消息类型（发送的还是接收的），第二个参数为消息的名称（name），第三个参数为消息的内容（content）。

### RabbitMQ Options

CAP 采用的是针对 CapOptions 进行扩展来实现RabbitMQ的配置功能，所以针对 RabbitMQ 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseRabbitMQ(rabbitMQOption=>{
        // rabbitmq options.
    });
});
```

`RabbitMQOptions` 提供了有关RabbitMQ相关的配置：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
HostName | 宿主地址 | string | localhost
UserName | 用户名 | string | guest
Password | 密码 | string | guest
VirtualHost | 虚拟主机 | string | /
Port | 端口号 | int | -1
TopicExchangeName | CAP默认Exchange名称 | string | cap.default.topic
RequestedConnectionTimeout | RabbitMQ连接超时时间 | int | 30,000 毫秒
SocketReadTimeout  | RabbitMQ消息读取超时时间 | int | 30,000 毫秒
SocketWriteTimeout | RabbitMQ消息写入超时时间 | int | 30,000 毫秒
QueueMessageExpires | 队列中消息自动删除时间 | int | (10天) 毫秒

### Kafka Options

CAP 采用的是针对 CapOptions 进行扩展来实现 Kafka 的配置功能，所以针对 Kafka 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseKafka(kafkaOption=>{
        // kafka options.
        // kafkaOptions.MainConfig.Add("", "");
    });
});
```

`KafkaOptions` 提供了有关 Kafka 相关的配置，由于Kafka的配置比较多，所以此处使用的是提供的 MainConfig 字典来支持进行自定义配置，你可以查看这里来获取对配置项的支持信息。

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)


### EntityFramework Options

如果使用的 Entityframework 来作为消息持久化存储的话，那么你可以在配置 CAP EntityFramework 配置项的时候来自定义一些配置。

```cs
services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>(efOption => 
    {
        // entityframework options.
    });
});

```

注意，如果你使用了 `UseEntityFramework` 的配置项，那么你不需要再次配置下面的章节几个针对不同数据库的配置，CAP 将会自动读取 DbContext 中使用的数据库相关配置信息。

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Schema | Cap表架构 | string | Cap  (SQL Server)
Schema | Cap表架构 | string | cap (PostgreSql)
TableNamePrefix | Cap表前缀 | string | cap (MySql)


### SqlServer Options

注意，如果你使用的是 EntityFramewrok，你用不到此配置项。

CAP 采用的是针对 CapOptions 进行扩展来实现 SqlServer 的配置功能，所以针对 SqlServer 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseSqlServer(sqlserverOptions => {
       // sqlserverOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Schema | Cap表架构 | string | Cap
ConnectionString | 数据库连接字符串 | string | null


### MySql Options

注意，如果你使用的是 EntityFramewrok，你用不到此配置项。

CAP 采用的是针对 CapOptions 进行扩展来实现 MySql 的配置功能，所以针对 MySql 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseMySql(mysqlOptions => {
       // mysqlOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
TableNamePrefix | Cap表名前缀 | string | cap 
ConnectionString | 数据库连接字符串 | string | null

### PostgreSql Options

注意，如果你使用的是 EntityFramewrok，你用不到此配置项。

CAP 采用的是针对 CapOptions 进行扩展来实现 PostgreSql 的配置功能，所以针对 PostgreSql 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UsePostgreSql(postgreOptions => {
       // postgreOptions.ConnectionString
    });
});

```

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Schema | Cap表名前缀 | string | cap 
ConnectionString | 数据库连接字符串 | string | null