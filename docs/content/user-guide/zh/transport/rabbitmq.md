# RabbitMQ

RabbitMQ是实现了高级消息队列协议（AMQP）的开源消息代理软件（亦称面向消息的中间件）。RabbitMQ 服务器是用 Erlang 语言编写的，而聚类和故障转移是构建在开源的通讯平台框架上的。所有主要的编程语言均有与代理接口通讯的客户端库。

CAP 支持使用 RabbitMQ 作为消息传输器。

!!! warning "注意事项" 
    在使用RabbitMQ时，集成了CAP的消费者应用在启动过一次后会自动创建持久化的队列，后续消息会正常传递到队列中并消费。
    如果你从来没有启动过消费者，则队列不会被自动创建，此时如果先行发布消息，在此时间段的消息 RabbitMQ Exchange 收到后会直接丢弃。

## 配置

要使用 RabbitMQ 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.RabbitMQ

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于 RabbitMQ 的配置项。

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

CAP 直接对外提供的 RabbitMQ 配置参数如下：

配置项 | 描述 | 类型 | 默认值
:---|:---|---|:---
HostName | 宿主地址，如果要配置集群可以使用逗号分隔，例如 `192.168.1.111,192.168.1.112` | string | localhost
UserName | 用户名 | string | guest
Password | 密码 | string | guest
VirtualHost | 虚拟主机 | string | /
Port | 端口号 | int | -1
ExchangeName | CAP默认Exchange名称 | string | cap.default.topic
QueueArguments  | 队列额外参数 x-arguments | QueueArgumentsOptions  |  N/A
QueueOptions  | 更改已创建队列的选项 | QueueRabbitOptions  |  { Durable=true, Exclusive=false, AutoDelete=false }
ConnectionFactoryOptions  |  RabbitMQClient原生参数 | ConnectionFactory | N/A
CustomHeadersBuilder  | 订阅者自定义头信息 |  见下文 |  N/A
PublishConfirms | 是否启用[发布确认](https://www.rabbitmq.com/confirms.html#publisher-confirms) | bool | false
BasicQosOptions | 指定消费的[Qos](https://www.rabbitmq.com/consumer-prefetch.html) | BasicQos | N/A

#### ConnectionFactory Option

如果你需要 **更多** 原生 `ConnectionFactory` 相关的配置项，可以通过 `ConnectionFactoryOptions` 配置项进行设定：

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

当需要从异构系统或者直接接收从RabbitMQ 控制台发送的消息时，由于 CAP 需要定义额外的头信息才能正常订阅，所以此时会出现异常。通过提供此参数来进行自定义头信息的设置来使订阅者正常工作。

你可以在这里找到有关 [头信息](../cap/messaging.md#异构系统集成) 的说明。

用法如下：

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


#### 如何连接 RabbitMQ 集群？

使用逗号分隔连接字符串即可，如下：

```
x=> x.UseRabbitMQ("localhost:5672,localhost:5673,localhost:5674")
```
