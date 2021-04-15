# Azure Service Bus

Azure 服务总线是一种多租户云消息服务，可用于在应用程序和服务之间发送信息。 异步操作可实现灵活的中转消息传送、结构化的先进先出 (FIFO) 消息传送以及发布/订阅功能。

CAP 支持使用 Azure Service Bus 作为消息传输器。

## Configuration

!!! warning "必要条件"
    针对 Service Bus 定价层, CAP 要求使用 “标准” 或者 “高级” 以支持 Topic 功能。

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

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
ConnectionString | Endpoint 地址 | string | 
TopicPath | Topic entity path | string | cap
EnableSessions | 启用 [Service bus sessions](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions) | bool | false 
ManagementTokenProvider | Token提供 | ITokenProvider | null

#### Sessions

当使用 `EnableSessions` 选项启用 sessions 后，每个发送的消息都会具有一个 session id。 要控制 seesion id 你可以在发送消息时在消息头中使用 `AzureServiceBusHeaders.SessionId` 携带它。


```csharp
ICapPublisher capBus = ...;
string yourEventName = ...;
YourEventType yourEvent = ...;

Dictionary<string, string> extraHeaders = new Dictionary<string, string>();
extraHeaders.Add(AzureServiceBusHeaders.SessionId, <your-session-id>);

capBus.Publish(yourEventName, yourEvent, extraHeaders);
```

如果头中没有 session id , 那么消息 Id 仍然使用的 Message Id.
