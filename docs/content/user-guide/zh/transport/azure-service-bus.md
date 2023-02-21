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

| NAME                    | DESCRIPTION                                                                                                  | TYPE                                                 | DEFAULT |
| :---------------------- | :----------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- | :------ |
| ConnectionString        | Endpoint 地址                                                                                                | string                                               |
| EnableSessions          | Enable [Service bus sessions](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions) | bool                                                 | false   |
| TopicPath               | Topic entity path                                                                                            | string                                               | cap     |
| ManagementTokenProvider | Token provider                                                                                               | ITokenProvider                                       | null    |
| AutoCompleteMessages    | 获取一个值，该值指示处理器是否应在消息处理程序完成处理后自动完成消息                                         | bool                                                 | false   |
| CustomHeaders           | 为来自异构系统的传入消息添加自定义头                                                                         | `Func<Message, List<KeyValuePair<string, string>>>?` | null    |
| Namespace               | Servicebus 的命名空间，与 TokenCredential 属性一起使用时需要设置                                             | string                                               | null    |
| SQLFilters              | 根据名称和表达式自定义 SQL 过滤器                                                                              | List<KeyValuePair<string, string>>                   | null    |

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
    asb.CustomHeaders = message => new List<KeyValuePair<string, string>>()
    {
        new(DotNetCore.CAP.Messages.Headers.MessageId,
            SnowflakeId.Default().NextId().ToString()),
        new(DotNetCore.CAP.Messages.Headers.MessageName, message.Label)
    };
});
```

> 重要提示：如果消息中已存在同名（Key）的标头，则不会添加自定义标头。 
