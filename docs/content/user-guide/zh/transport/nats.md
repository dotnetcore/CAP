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
Options | NATS.NET 原生连接配置；Servers 优先于 Options.Url。 | `NATS.Client.Core.NatsOpts` | 客户端默认值
Servers | NATS 服务端 URL | string | `nats://127.0.0.1:4222`
ConnectionPoolSize  | 发布端最多缓存的空闲连接数 | int | 10
EnableSubscriberClientStreamAndSubjectCreation | 是否允许消费者客户端创建 Stream 和 Subject | bool | true
StreamOptions | Stream 配置项 | `Action<StreamConfig>` | NULL
ConsumerOptions | 新建 durable push consumer 配置 | `Func<NatsJSPushConsumerOpts, NatsJSPushConsumerOpts>` | NULL
CustomHeadersBuilder | 订阅者自定义头信息 | `INatsJSMsg<byte[]>`；见下文 |  N/A
NormalizeStreamName | 将主题名称转换为 Stream 名称 | `Func<string, string>` | 取第一个 `.` 之前的部分

#### NATS ConfigurationOptions

如果你需要 **更多** 原生相关的配置项，可以通过 `Options` 配置项进行设定：

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseNATS(natsOptions=>
    {
        natsOptions.Servers = "nats://127.0.0.1:4222";
        natsOptions.Options = NATS.Client.Core.NatsOpts.Default with { Name = "CAP" };
    });
});
```

`Options` 使用 [NATS.NET NatsOpts](https://nats-io.github.io/nats.net/api/NATS.Client.Core.NatsOpts.html)。CAP 消费连接采用 5 秒连接超时，禁用 echo 和客户端重连，将连接故障交给 CAP 恢复。消费缓冲区采用满载时等待的有界通道；`Options.SubPendingChannelCapacity` 对新建和复用的 consumer 均生效。

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
        new(DotNetCore.CAP.Messages.Headers.MessageName, e.Subject)
    ];
});
```

#### 从 NATS.Client 1.x 迁移

传输器现已使用 NATS.NET 3.3.0（`NATS.Client.JetStream` 及其 Core 依赖）。`UseNATS("nats://...")` 用法不变；使用原生客户端类型的配置需要迁移：

| 原 API | 新 API |
| --- | --- |
| `NATS.Client.Options` | `NATS.Client.Core.NatsOpts` |
| Stream 配置 Builder | 使用可写属性的 `StreamConfig` |
| Consumer 配置 Builder | 通过 `with` 返回新的 `NatsJSPushConsumerOpts` |
| `MsgHandlerEventArgs.Message` | 直接使用 `INatsJSMsg<byte[]>` 的 `Subject`、`Headers`、`Data`、`Metadata` |
| `IConnectionPool.RentConnection()` / `Return()` | await `RentConnectionAsync()` / `ReturnAsync()`；原生连接改为 `INatsConnection` |
| 同步释放连接池 | `IAsyncDisposable.DisposeAsync()` |

```csharp
services.AddCap(cap => cap.UseNATS(nats =>
{
    nats.Servers = "nats://127.0.0.1:4222";
    nats.Options = NATS.Client.Core.NatsOpts.Default with { Name = "CAP" };
    nats.StreamOptions = stream =>
    {
        stream.MaxMsgs = 100_000;
    };
    nats.ConsumerOptions = consumer => consumer with
    {
        AckWait = TimeSpan.FromSeconds(60),
        MaxAckPending = 1_000
    };
}));
```

CAP 继续使用 durable queue push consumer：相同 CAP Group 的实例竞争消费，不同 Group 各自收到一份消息。新 consumer 默认仅接收创建后的消息，采用显式确认和 30 秒确认超时。CAP 在接收消息完成存储、入队后 ACK，业务订阅方法的重试仍由 CAP 管理。投递语义为至少一次，应用需要兼容重投。

升级时按照原 durable 命名规则查找已有 consumer，复用服务端保存的投递 subject 和确认进度。`ConsumerOptions` 配置新建行为，不覆盖已有 consumer 的配置。主题过滤、投递组或确认策略不兼容时明确报错，不删除重建 consumer。滚动升级及回滚时，请保持 Group、Topic、Stream 名称转换规则和自定义 durable 名称不变。

本地订阅缓冲区优先通过连接配置 `Options.SubPendingChannelCapacity` 调整。NATS.NET 3.3.0 的 `ConsumerOptions.SubOpts` 仅在创建 consumer 时生效；通过 `GetPushConsumerAsync` 获取的 consumer 使用连接默认值。配置的 `NotificationHandler` 也会传给已有 consumer 的消费循环。

更新 Stream 时保留已有配置并合并 subjects，再应用 `StreamOptions`。如果 Stream 由外部管理，请关闭 `EnableSubscriberClientStreamAndSubjectCreation`；CAP 仍会创建或复用 consumer。不同进程之间的 Stream 并发更新不是事务操作，多应用共用同一 Stream 时建议集中预配置 subjects。
