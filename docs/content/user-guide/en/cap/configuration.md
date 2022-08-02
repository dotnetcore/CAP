# Configuration

By default, you can specify configuration when you register CAP services into the IoC container for ASP.NET Core project.

```c#
services.AddCap(config=> 
{
    // config.XXX 
});
```

`services` is `IServiceCollection` interface, which can be found in the `Microsoft.Extensions.DependencyInjection` package.

If you don't want to use Microsoft's IoC container, you can take a look at ASP.NET Core documentation [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2#default-service-container-replacement) to learn how to replace the default container implementation.

## what is minimum configuration required for CAP

you have to configure at least a transport and a storage. If you want to get started quickly you can use the following configuration:

```C#
services.AddCap(capOptions => 
{
     capOptions.UseInMemoryQueue();
     capOptions.UseInmemoryStorage();
});
```

For specific transport and storage configuration, you can take a look at the configuration options provided by the specific components in the [Transports](../transport/general.md) section and the [Persistent](../storage/general.md) section.

## Custom configuration

The `CapOptions` is used to store configuration information. By default they have default values, sometimes you may need to customize them.

#### DefaultGroupName

> Default: cap.queue.{assembly name}

The default consumer group name, corresponds to different names in different Transports, you can customize this value to customize the names in Transports for easy viewing.

!!! info "Mapping"
    Map to [Queue Names](https://www.rabbitmq.com/queues.html#names) in RabbitMQ.  
    Map to [Consumer Group Id](http://kafka.apache.org/documentation/#group.id) in Apache Kafka.  
    Map to Subscription Name in Azure Service Bus.  
    Map to [Queue Group Name](https://docs.nats.io/nats-concepts/queue) in NATS.
    Map to [Consumer Group](https://redis.io/topics/streams-intro#creating-a-consumer-group) in Redis Streams.

#### GroupNamePrefix

> Default: Null

Add unified prefixes for consumer group.  https://github.com/dotnetcore/CAP/pull/780

#### TopicNamePrefix

> Default: Null

Add unified prefixes for topic/queue name.  https://github.com/dotnetcore/CAP/pull/780

#### Versioning

> Default: v1

This is a new configuration option introduced in the CAP v2.4 version. It is used to specify a version of a message to isolate messages of different versions of the service. It is often used in A/B testing or multi-service version scenarios. Following are application scenarios that needs versioning:

!!! info "Business Iterative and compatible"
    Due to the rapid iteration of services, the data structure of the message is not fixed during each service integration process. Sometimes we add or modify certain data structures to accommodate the newly introduced requirements. If you have a brand new system, there's no problem, but if your system is already deployed to a production environment and serves customers, this will cause new features to be incompatible with the old data structure when they go online, and then these changes can cause serious problems. To work around this issue, you can only clean up message queues and persistent messages before starting the application, which is obviously not acceptable for production environments.

!!! info "Multiple versions of the server"
    Sometimes, the server's server needs to provide multiple sets of interfaces to support different versions of the app. Data structures of the same interface and server interaction of these different versions of the app may be different, so usually server does not provide the same routing addresses to adapt to different versions of App calls.

!!! info "Using the same persistent table/collection in different instance"
    If you want multiple different instance services to use the same database, in versions prior to 2.4, we could isolate database tables for different instances by specifying different table names. After version 2.4 this can be achived through CAP configuration, by configuring different table name prefixes.

> Check out the blog to learn more about the Versioning feature: https://www.cnblogs.com/savorboard/p/cap-2-4.html

#### FailedRetryInterval

> Default: 60 sec

During the message sending process if message transport fails, CAP will try to send the message again. This configuration option is used to configure the interval between each retry.

During the message sending process if consumption method fails, CAP will try to execute the method again. This configuration option is used to configure the interval between each retry.

!!! WARNING "Retry & Interval"
    By default if failure occurs on send or consume, retry will start after **4 minutes** in order to avoid possible problems caused by setting message state delays.    
    Failures in the process of sending and consuming messages will be retried 3 times immediately, and will be retried polling after 3 times, at which point the FailedRetryInterval configuration will take effect.

#### CollectorCleaningInterval

> Default: 300 sec

The interval of the collector processor deletes expired messages.

#### ConsumerThreadCount 

> Default: 1

Number of consumer threads, when this value is greater than 1, the order of message execution cannot be guaranteed.

#### FailedRetryCount

> Default: 50

Maximum number of retries. When this value is reached, retry will stop and the maximum number of retries will be modified by setting this parameter.

#### FailedThresholdCallback

> Default: NULL

Type: `Action<FailedInfo>`

Failure threshold callback. This action is called when the retry reaches the value set by `FailedRetryCount`, you can receive notification by specifying this parameter to make a manual intervention. For example, send an email or notification. 

#### SucceedMessageExpiredAfter

> Default: 24*3600 sec (1 days)

The expiration time (in seconds) of the success message. When the message is sent or consumed successfully, it will be removed from database storage when the time reaches `SucceedMessageExpiredAfter` seconds. You can set the expiration time by specifying this value.

#### FailedMessageExpiredAfter

> Default: 15*24*3600 sec(15 days)

The expiration time (in seconds) of the failed message. When the message is sent or consumed failed, it will be removed from database storage when the time reaches `FailedMessageExpiredAfter` seconds. You can set the expiration time by specifying this value.

#### UseDispatchingPerGroup

> Default: false

If `true` then all consumers within the same group pushes received messages to own dispatching pipeline channel. Each channel has set thread count to `ConsumerThreadCount` value.