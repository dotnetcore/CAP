# NATS

[NATS](https://nats.io/)是一个简单、安全、高性能的数字系统、服务和设备通信系统。NATS 是 CNCF 的一部分。

!!! warning
    自 CAP 5.2+ 的版本已经基于 [JetStream](https://docs.nats.io/nats-concepts/jetstream) 实现相关功能，所以需要在服务端显式启用。
    
    **你需要在 NATS Server 启动时候指定 `--jetstream` 参数来启用 JetSteram 相关功能，才能正常使用CAP.**

## 配置

要使用NATS 传输器，你需要安装下面的NuGet包：

```powershell

PM> Install-Package DotNetCore.CAP.NATS

```

你可以通过在 `Startup.cs` 文件中配置 `ConfigureServices` 来添加配置：

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

#### NATS 配置

CAP 直接提供的关于 NATS 的配置参数：


NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Options | NATS 客户端配置 | Options | Options
Servers | 服务器Urls地址 | string | NULL
ConnectionPoolSize  | 连接池数量 | uint | 10
DeliverPolicy | 消费消息的策略点（⚠️在8.1.0版本移除，使用`ConsumerOptions`替代。） | enum | DeliverPolicy.New
StreamOptions | 🆕 Stream 配置项 |  Action | NULL
ConsumerOptions | 🆕 Consumer 配置项 | Action | NULL
CustomHeadersBuilder | 订阅者自定义头信息 |  见下文 |  N/A

#### NATS ConfigurationOptions

如果你需要 **更多** 原生相关的配置项，可以通过 `Options` 配置项进行设定：

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

`Options` 是 NATS.Client 客户端提供的配置， 你可以在这个[链接](http://nats-io.github.io/nats.net/class_n_a_t_s_1_1_client_1_1_options.html)找到更多详细信息。

#### CustomHeadersBuilder Option

当需要从异构系统或者直接接收从 NATS JetStream 发送的消息时，由于 CAP 需要定义额外的头信息才能正常订阅，所以此时会出现异常。通过提供此参数来进行自定义头信息的设置来使订阅者正常工作。

你可以在这里找到有关 [头信息](../cap/messaging.md#异构系统集成) 的说明。

用法如下：

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