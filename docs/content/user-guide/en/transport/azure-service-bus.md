# Azure Service Bus

Microsoft Azure Service Bus is a fully managed enterprise integration message broker. Service Bus is most commonly used to decouple applications and services from each other, and is a reliable and secure platform for asynchronous data and state transfer. 

Azure services can be used in CAP as a message transporter. 

## Configuration

!!! warning "Requirement"
    For the Service Bus pricing layer, CAP requires "standard" or "advanced" to support Topic functionality.

To use Azure Service Bus as a message transport, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.AzureServiceBus
```
Next, add configuration items to the `ConfigureServices` method of `Startup.cs`:

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

The AzureServiceBus configuration options provided directly by the CAP:

| NAME                     | DESCRIPTION                                                                                                  | TYPE | DEFAULT |
|:-------------------------|:-------------------------------------------------------------------------------------------------------------|---|:--- |
| ConnectionString         | Endpoint address                                                                                             | string |
| EnableSessions           | Enable [Service bus sessions](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions) | bool | false |
| TopicPath                | Topic entity path                                                                                            | string | cap |
| ManagementTokenProvider  | Token provider                                                                                               | ITokenProvider | null |
| CustomHeaders            | Adds custom and/or mandatory Headers for incoming messages from heterogeneous systems.                       | Func<Message, List<KeyValuePair<string, string>>>? | null | 
#### Sessions

When sessions are enabled (see `EnableSessions` option above), every message sent will have a session id. To control the session id, include
an extra header with name `AzureServiceBusHeaders.SessionId` when publishing events:

```csharp
ICapPublisher capBus = ...;
string yourEventName = ...;
YourEventType yourEvent = ...;

Dictionary<string, string> extraHeaders = new Dictionary<string, string>();
extraHeaders.Add(AzureServiceBusHeaders.SessionId, <your-session-id>);

capBus.Publish(yourEventName, yourEvent, extraHeaders);
```

If no session id header is present, the message id will be used as the session id.

#### Heterogeneous Systems

Sometimes you might want to listen to a message that was published by an external system. In this case, you need to add a set of two mandatory headers for CAP compatibility as shown below.

```csharp
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

> Important: If a header with the same name (key) already exists in the message, the Custom Header won't be added.
