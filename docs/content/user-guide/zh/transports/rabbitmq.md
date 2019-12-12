# RabbitMQ

RabbitMQ是实现了高级消息队列协议（AMQP）的开源消息代理软件（亦称面向消息的中间件）。RabbitMQ 服务器是用 Erlang 语言编写的，而聚类和故障转移是构建在开源的通讯平台框架上的。所有主要的编程语言均有与代理接口通讯的客户端库。

CAP 支持使用 RabbitMQ 作为消息传输器。

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

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
HostName | 宿主地址 | string | localhost
UserName | 用户名 | string | guest
Password | 密码 | string | guest
VirtualHost | 虚拟主机 | string | /
Port | 端口号 | int | -1
TopicExchangeName | CAP默认Exchange名称 | string | cap.default.topic
QueueMessageExpires | 队列中消息自动删除时间 | int | (10天) 毫秒

#### ConnectionFactory Options

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
