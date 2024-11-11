# Azure Service Bus

Azure 服务总线是一种多租户云消息服务，可用于在应用程序和服务之间发送信息。 异步操作可实现灵活的中转消息传送、结构化的先进先出 (FIFO) 消息传送以及发布/订阅功能。

CAP 支持使用 Azure Service Bus 作为消息传输器。

## Configuration

!!! warning "必须条件"
    针对 Service Bus 的定价, CAP 要求使用  “标准” 或者 “高级” 以支持 Topic 功能。

要使用 Azure Service Bus 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.AzureServiceBus

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于内存的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseAzureServiceBus(opt=>
        {
            //AzureServiceBusOptions
        });
        // x.UseXXX ...
    });
}

```

#### AzureServiceBus Options

CAP 直接对外提供的 Azure Service Bus 配置参数如下：

| 名称                                 | 描述                                                                                                                                       | 类型                                                                   | 默认值                           |
| :----------------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | :------------------------------- |
| ConnectionString                     | 终端地址                                                                                                                                   | string                                                                 |                                  |
| TopicPath                            | 主题实体路径                                                                                                                               | string                                                                 | cap                              |
| EnableSessions                       | 启用 [Service Bus 会话](https://docs.microsoft.com/zh-cn/azure/service-bus-messaging/message-sessions)                                     | bool                                                                   | false                            |
| MaxConcurrentSessions                | 处理器可处理的最大并发会话数。当 EnableSessions 为 false 时不适用。                                                                        | int                                                                    | 8                                |
| SessionIdleTimeout                   | 在会话关闭前等待新消息的最长时间。如果未指定，Azure Service Bus 将使用 60 秒。                                                             | TimeSpan                                                               | null                             |
| SubscriptionAutoDeleteOnIdle         | 在特定空闲间隔后自动删除订阅。                                                                                                             | TimeSpan                                                               | TimeSpan.MaxValue                |
| SubscriptionMessageLockDuration      | 给定接收器锁定消息的时间，以防止其他接收器接收相同的消息。                                                                                 | TimeSpan                                                               | 60 秒                            |
| SubscriptionDefaultMessageTimeToLive | 订阅的默认消息生存时间值。这是消息到期前的持续时间。                                                                                       | TimeSpan                                                               | TimeSpan.MaxValue                |
| SubscriptionMaxDeliveryCount         | 消息在被传递给订阅后进入死信队列之前的最大传递次数。                                                                                       | int                                                                    | 10                               |
| MaxAutoLockRenewalDuration           | 锁自动续订的最长持续时间。该值应大于最长的消息锁定持续时间。                                                                               | TimeSpan                                                               | 5 分钟                           |
| ManagementTokenProvider              | 令牌提供程序                                                                                                                               | ITokenProvider                                                         | null                             |
| AutoCompleteMessages                 | 获取一个值，该值指示在消息处理程序完成处理后，处理器是否应自动完成消息。                                                                   | bool                                                                   | false                            |
| CustomHeadersBuilder                 | 为来自异构系统的传入消息添加自定义和/或强制性标头。                                                                                        | `Func<Message, IServiceProvider, List<KeyValuePair<string, string>>>?` | null                             |
| Namespace                            | Servicebus 的命名空间，在使用 TokenCredential 属性时需要设置。                                                                             | string                                                                 | null                             |
| DefaultCorrelationHeaders            | 将附加的关联属性添加到所有 [关联筛选器](https://learn.microsoft.com/zh-cn/azure/service-bus-messaging/topic-filters#correlation-filters)。 | IDictionary<string, string>                                            | Dictionary<string, string>.Empty |
| SQLFilters                           | 在主题订阅上按名称和表达式定义的自定义 SQL 筛选器。                                                                                        | List<KeyValuePair<string, string>>                                     | null                             |

#### Sessions

当使用 `EnableSessions` 选项启用 sessions 后，每个发送的消息都会具有一个 session id。 要控制 seesion id 你可以在发送消息时在消息头中使用 `AzureServiceBusHeaders.SessionId` 携带它。


```C#
ICapPublisher capBus = ...;
string yourEventName = ...;
YourEventType yourEvent = ...;

Dictionary<string, string> extraHeaders = new Dictionary<string, string>();
extraHeaders.Add(AzureServiceBusHeaders.SessionId, <your-session-id>);

capBus.Publish(yourEventName, yourEvent, extraHeaders);
```

如果头中没有 session id , 那么消息 Id 仍然使用的 Message Id.


#### Heterogeneous Systems

有时您可能想接收由外部系统发布的消息。 在这种情况下，您需要添加一组两个强制标头以实现 CAP 兼容性，如下所示。

```C#
c.UseAzureServiceBus(asb =>
{
    asb.ConnectionString = ...
    asb.CustomHeadersBuilder = (msg, sp) =>
    [
        new(DotNetCore.CAP.Messages.Headers.MessageId, sp.GetRequiredService<ISnowflakeId>().NextId().ToString()),
        new(DotNetCore.CAP.Messages.Headers.MessageName, msg.RoutingKey)
    ];
});
```

> 重要提示：如果消息中已存在同名（Key）的标头，则不会添加自定义标头。 
