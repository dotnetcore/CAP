# Azure Service Bus

Microsoft Azure Service Bus is a fully managed enterprise integration message broker. Service Bus is most commonly used to decouple applications and services from each other, and is a reliable and secure platform for asynchronous data and state transfer. 

CAP supports Azure Service Bus as a message transporter.

## Configuration

!!! warning "Requirement"
    For the Service Bus pricing layer, CAP requires "standard" or "advanced" to support Topic functionality.

To use Azure Service Bus as a message transport, you need to install the following extensions from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.AzureServiceBus
```

Then you can add memory-based configuration items to the `ConfigureServices` method of `Startup.cs`.


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

The AzureServiceBus configuration options provided directly by the CAP are as follows:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
ConnectionString | Endpoint address | string | 
TopicPath | Topic entity path | string | cap
ManagementTokenProvider | Token provider | ITokenProvider | null