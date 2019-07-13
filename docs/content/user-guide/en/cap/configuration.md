# Configuration

By default, you can specify the configuration when you register the CAP service into the IoC container for ASP.NET Core project.

```c#
services.AddCap(config=> 
{
    // config.XXX 
});
```

The `services` is `IServiceCollection` interface，which is under the `Microsoft.Extensions.DependencyInjection`.

If you don't want to use Microsoft's IoC container, you can view ASP.NET Core documentation [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2#default-service-container-replacement) to learn how to replace the default container implementation.

## What is the minimum configuration?

The simplest answer is that at least you have to configure a transport and a storage. If you want to get started quickly you can use the following configuration:

```C#
services.AddCap(capOptions => 
{
     capOptions.UseInMemoryQueue();
     capOptions.UseInmemoryStorage();
});
```

For specific transport and storage configuration, you can view the configuration items provided by the specific components in the [Transports](../transports/general.md) section and the [Persistent](../persistent/general.md) section.

## Custom configuration

The `CapOptions` is used to store configuration information. By default they have the default values, and sometimes you may need to customize them.

#### DefaultGroup

> Default: cap.queue.{assembly name}

The default consumer group name, corresponding to different names in different Transports, you can customize this value to customize the names in Transports for easy viewing.

!!! info "Mapping"
    Map to [Queue Names](https://www.rabbitmq.com/queues.html#names) in RabbitMQ.  
    Map to Topic Name in Apache Kafka.  
    Map to Subscription Name in Azure Service Bus.  

#### Version

> Default: v1

This is a new configuration item introduced in the CAP v2.4 version. It is used to specify a version of a message to isolate messages of different versions of the service. It is often used in A/B testing or multi-service version scenarios. The following is its application scenario:

!!! info "Business Iterative and compatible"
    Due to the rapid iteration of services, the data structure of the message is not fixed during each service integration process. Sometimes we add or modify certain data structures to accommodate the newly introduced requirements. If you're a brand new system, there's no problem, but if your system is deployed to a production environment and serves customers, this will cause new features to be incompatible with the old data structure when they go online, and then these changes can cause serious problems. To work around this issue, you can only clean up message queues and persistent messages before starting the application, which is obviously fatal for production environments.

!!! info "Multiple versions of the server"
    Sometimes, the server's server needs to provide multiple sets of interfaces to support different versions of the app. The data structures of the same interface and server interaction of these different versions of the app may be different, so usually the server does not provide the same. Routing addresses to adapt to different versions of App calls.

!!! info "Using the same persistent table/collection in different instance"
    If you want multiple different instance services to use the same database, in versions prior to 2.4, we could isolate database tables for different instances by specifying different table names. That is to say, when configuring the CAP, it is implemented by configuring different table name prefixes.

> Check out the blog to learn more about Version feature: https://www.cnblogs.com/savorboard/p/cap-2-4.html

#### FailedRetryInterval

> Default: 60 sec

In the process of message message sent to transport failed, the CAP will be retry to sent. This configuration item is used to configure the interval between each retry.

In the process of message consumption failed, the CAP will retry to execute. This configuration item is used to configure the interval between each retry.

!!! WARNING "Retry & Interval"
    By default, retry will start after **4 minutes** of failure to send or consume, in order to avoid possible problems caused by setting message state delays.    
    Failures in the process of sending and consuming messages will be retried 3 times immediately, and will be retried polling after 3 times, at which point the FailedRetryInterval configuration will take effect.

#### FailedRetryCount

> Default: 50

Maximum number of retries. When this value is reached, retry will stop and the maximum number of retries will be modified by setting this parameter.

#### FailedThresholdCallback

> Default: NULL

Type: `Action<MessageType, string, string>`

>
T1 : Message Type  
T2 : Message Name  
T3 : Message Content

Failure threshold callback. This action is called when the retry reaches the value set by `FailedRetryCount`, and you can receive the notification by specifying this parameter to make a manual intervention. For example, send an email or notify.

#### SucceedMessageExpiredAfter

> Default: 24*3600 sec (1 days)

The expiration time (in seconds) of the success message. When the message is sent or consumed successfully, it will be removed from persistent when the time reaches `SucceedMessageExpiredAfter` seconds. You can set the expiration time by specifying this value.