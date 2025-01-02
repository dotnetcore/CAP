# Configuration

By default, you can specify configuration when you register CAP services into the DI container for ASP.NET Core project.

```c#
services.AddCap(config=> 
{
    // config.XXX 
});
```
`services` is `IServiceCollection` interface, which can be found in the `Microsoft.Extensions.DependencyInjection` package.

## What is minimum configuration required for CAP

you have to configure at least a transport and a storage. If you want to get started quickly you can use the following configuration:

```C#
services.AddCap(capOptions => 
{
     capOptions.UseInMemoryQueue();  //Required Savorboard.CAP.InMemoryMessageQueue nuget package.
     capOptions.UseInmemoryStorage();
});
```

For specific transport and storage configuration, you can take a look at the configuration options provided by the specific components in the [Transports](../transport/general.md) section and the [Persistent](../storage/general.md) section.

## Configuration in Subscribers

Subscribers use the `[CapSubscribe]` attribute to mark themselves as subscribers. They can be located in an ASP.NET Core Controller or Service.

When you declare `[CapSubscribe]`, you can change the behavior of the subscriber by specifying the following parameters.

### Name

> string, required

Subscribe to messages by specifying the `Name` parameter, which corresponds to the name specified when publishing the message through _cap.Publish("Name").

This name corresponds to different items in different Brokers:

- In RabbitMQ, it corresponds to the Routing Key.
- In Kafka, it corresponds to the Topic.
- In AzureServiceBus, it corresponds to the Subject.
- In NATS, it corresponds to the Subject.
- In RedisStreams, it corresponds to the Stream.

### Group

> string, optional

Specify the `Group` parameter to place subscribers within a separate consumer group, a concept similar to consumer groups in Kafka. If this parameter is not specified, the current assembly name (`DefaultGroupName`) is used as the default.

Subscribers with the same `Name` but set to **different** groups will all receive messages. Conversely, if subscribers with the same `Name` are set to the **same** group, only one will receive the message.

It also makes sense for subscribers with different `Names` to be set to **different** groups; they can have independent threads for execution. Conversely, if subscribers with different `Names` are set to the **same** group, they will share consumption threads.

Group corresponds to different items in different Brokers:

- In RabbitMQ, it corresponds to Queue.
- In Kafka, it corresponds to Consumer Group.
- In AzureServiceBus, it corresponds to Subscription Name.
- In NATS, it corresponds to Queue Group.
- In RedisStreams, it corresponds to Consumer Group.

###  GroupConcurrent

> byte, optional

Set the parallelism of concurrent execution for subscribers by specifying the value of the `GroupConcurrent` parameter. Concurrent execution means that it needs to be on an independent thread, so if you do not specify the `Group` parameter, CAP will automatically create a Group using the value of `Name`.

!!! Note
    If you have multiple subscribers configured with the same Group and have also set the `GroupConcurrent` value for them, Then the degree of parallelism is the sum of the values in the group.  
    This setting applies only to new messages; retried messages are not subject to the concurrency limit.

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

It is used to specify a version of a message to isolate messages of different versions of the service. It is often used in A/B testing or multi-service version scenarios. Following are application scenarios that needs versioning:

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
    By default if failure occurs on send or consume, retry will start after **4 minutes** (FallbackWindowLookbackSeconds) in order to avoid possible problems caused by setting message state delays.    
    Failures in the process of sending and consuming messages will be retried 3 times immediately, and will be retried polling after 3 times, at which point the FailedRetryInterval configuration will take effect.

!!! WARNING "Multi-instance concurrent retries"
    We introduced database-based distributed locks in version 7.1.0 to solve the problem of retrying concurrent fetches from the database under multiple instances, you need to explicitly configure `UseStorageLock` to true.

#### UseStorageLock

> Default: false

If set to true, we will use a database-based distributed lock to solve the problem of concurrent fetches data by retry processes with multiple instances. This will generate the cap.lock table in the database.

#### CollectorCleaningInterval

> Default: 300 sec

The interval of the collector processor deletes expired messages.

#### ConsumerThreadCount 

> Default: 1

Number of consumer threads, when this value is greater than 1, the order of message execution cannot be guaranteed.

#### FailedRetryCount

> Default: 50

Maximum number of retries. When this value is reached, retry will stop and the maximum number of retries will be modified by setting this parameter.

#### FallbackWindowLookbackSeconds

> Default: 240 sec

Configure the retry processor to pick up the fallback window lookback time for `Scheduled` or `Failed` status messages.

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

#### [Removed] UseDispatchingPerGroup 

> Default: false

> Removed in version 8.2, already default behavior

If `true` then all consumers within the same group pushes received messages to own dispatching pipeline channel. Each channel has set thread count to `ConsumerThreadCount` value.

#### [Obsolete] EnableConsumerPrefetch

> Default: false， Before version 7.0 the default behavior is true

Renamed to `EnableSubscriberParallelExecute` option, Please use the new option.

#### EnableSubscriberParallelExecute

> Default: false

If `true`, CAP will prefetch some message from the broker as buffered, then execute the subscriber method. After the execution is done, it will fetch the next batch for execution.

!!! note "Precautions"
    Setting it to true may cause some problems. When the subscription method executes too slowly and takes too long, it will cause the retry thread to pick up messages that have not yet been executed. The retry thread picks up messages from 4 minutes (FallbackWindowLookbackSeconds) ago by default , that is to say, if the message backlog of more than 4 minutes (FallbackWindowLookbackSeconds) on the consumer side will be picked up again and executed again

#### SubscriberParallelExecuteThreadCount

> Default: `Environment.ProcessorCount`

With the `EnableSubscriberParallelExecute` option enabled, specify the number of parallel task execution threads.

#### SubscriberParallelExecuteBufferFactor

> Default: 1

With the `EnableSubscriberParallelExecute` option enabled, multiplier used to determine the buffered capacity size in subscriber parallel execution. The buffer capacity is computed by multiplying this factor with the value of `SubscriberParallelExecuteThreadCount`, which represents the number of threads allocated for parallel processing.

#### EnablePublishParallelSend

> Default: false， The (7.2 <= Version < 8.1) the default behavior is true

By default, messages sent are first placed into the Channel in memory and then processed linearly.
If set to true, the task of sending messages will be processed in parallel by the .NET thread pool, which will greatly increase the speed of sending.