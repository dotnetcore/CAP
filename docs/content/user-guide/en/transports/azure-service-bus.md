# Azure Service Bus

Microsoft Azure Service Bus is a fully managed enterprise integration message broker. Service Bus is most commonly used to decouple applications and services from each other, and is a reliable and secure platform for asynchronous data and state transfer. 

CAP supports Azure Service Bus as a message transporter.

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

CAP 直接对外提供的 Kafka 配置参数如下：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
ConnectionString | Endpoint 地址 | string | 
TopicPath | Topic entity path | string | cap
ManagementTokenProvider | Token提供 | ITokenProvider | null