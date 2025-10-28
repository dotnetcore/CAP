# Azure Service Bus

Microsoft Azure Service Bus is a fully managed enterprise integration message broker. Service Bus is most commonly used to decouple applications and services, and is a reliable and secure platform for asynchronous data and state transfer.

Azure Service Bus can be used in CAP as a message transporter.

## Configuration

!!! warning "Requirement"
    For the Service Bus pricing tier, CAP requires "Standard" or "Premium" to support Topic functionality.

To use Azure Service Bus as a message transporter, you need to install the following package from NuGet:

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

#### Azure Service Bus Options

The Azure Service Bus configuration options provided by CAP:

| NAME                                 | DESCRIPTION                                                                                                                                                           | TYPE                                                                   | DEFAULT                          |
| :----------------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | :------------------------------- |
| ConnectionString                     | Endpoint address                                                                                                                                                      | string                                                                 |                                  |
| TopicPath                            | Topic entity path                                                                                                                                                     | string                                                                 | cap                              |
| EnableSessions                       | Enable [Service bus sessions](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions)                                                          | bool                                                                   | false                            |
| MaxConcurrentSessions                | The maximum number of concurrent sessions that the processor can handle. Not applicable when EnableSessions is false.                                                 | int                                                                    | 8                                |
| SessionIdleTimeout                   | The maximum time to wait for a new message before the session is closed. If not specified, 60 seconds will be used by Azure Service Bus.                              | TimeSpan                                                               | null                             |
| SubscriptionAutoDeleteOnIdle         | Automatically delete subscription after a certain idle interval.                                                                                                      | TimeSpan                                                               | TimeSpan.MaxValue                |
| SubscriptionMessageLockDuration      | The amount of time the message is locked by a given receiver so that no other receiver receives the same message.                                                     | TimeSpan                                                               | 60 seconds                       |
| SubscriptionDefaultMessageTimeToLive | The default message time to live value for a subscription. This is the duration after which the message expires.                                                      | TimeSpan                                                               | TimeSpan.MaxValue                |
| SubscriptionMaxDeliveryCount         | The maximum number of times a message is delivered to the subscription before it is dead-lettered.                                                                    | int                                                                    | 10                               |
| MaxAutoLockRenewalDuration           | The maximum duration within which the lock will be renewed automatically. This value should be greater than the longest message lock duration.                        | TimeSpan                                                               | 5 minutes                        |
| ManagementTokenProvider              | Token provider                                                                                                                                                        | ITokenProvider                                                         | null                             |
| AutoCompleteMessages                 | Gets a value that indicates whether the processor should automatically complete messages after the message handler has completed processing                           | bool                                                                   | false                            |
| CustomHeadersBuilder                 | Adds custom and/or mandatory Headers for incoming messages from heterogeneous systems.                                                                                | `Func<Message, IServiceProvider, List<KeyValuePair<string, string>>>?` | null                             |
| Namespace                            | Namespace of Servicebus , Needs to be set when using with TokenCredential Property                                                                                    | string                                                                 | null                             |
| DefaultCorrelationHeaders            | Adds additional correlation properties to all [correlation filters](https://learn.microsoft.com/en-us/azure/service-bus-messaging/topic-filters#correlation-filters). | IDictionary<string, string>                                            | Dictionary<string, string>.Empty |
| SQLFilters                           | Custom SQL Filters by name and expression on Topic Subscribtion                                                                                                       | List<KeyValuePair<string, string>>                                     | null                             |

#### Sessions

When sessions are enabled (see the `EnableSessions` option above), every message sent will have a session ID. To control the session ID, include an extra header with the name `AzureServiceBusHeaders.SessionId` when publishing events:

```C#
ICapPublisher capBus = ...;
string yourEventName = ...;
YourEventType yourEvent = ...;

Dictionary<string, string> extraHeaders = new Dictionary<string, string>();
extraHeaders.Add(AzureServiceBusHeaders.SessionId, <your-session-id>);

capBus.Publish(yourEventName, yourEvent, extraHeaders);
```

If no session ID header is present, the message ID will be used as the session ID.

#### Heterogeneous Systems

Sometimes you might want to listen to a message published by an external system. In this case, you need to add a set of two mandatory headers for CAP compatibility, as shown below:

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

#### SQL Filters

You can set SQL filters on the subscription level to get desired messages without having custom logic on the business side. For more information, see [Azure Service Bus SQL Filters](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-sql-filter).

`SQLFilters` is a list of `KeyValuePair<string, string>`, where the key is the filter name and the value is the SQL expression.

```C#
c.UseAzureServiceBus(asb =>
{
    asb.ConnectionString = ...
    asb.SQLFilters = new List<KeyValuePair<string, string>> {
            
            new KeyValuePair<string,string>("IOTFilter","FromIOTHub='true'"),  // The message will be handled if ApplicationProperties contains IOTFilter and value is true
            new KeyValuePair<string,string>("SequenceFilter","sys.enqueuedSequenceNumber >= 300")
        };
});
```
