# Configuration

By default, you specify configurations when registering CAP services in the DI container for an ASP.NET Core project.

```c#
services.AddCap(config =>
{
    // config.XXX
});
```

`services` is an `IServiceCollection` interface, which can be found in the `Microsoft.Extensions.DependencyInjection` package.

## Minimum Configuration Required

You must configure at least one transport and one storage. If you want to get started quickly, you can use the following configuration:

```C#
services.AddCap(capOptions => 
{
     capOptions.UseInMemoryQueue();  // Requires the Savorboard.CAP.InMemoryMessageQueue NuGet package.
     capOptions.UseInMemoryStorage();
});
```

For transport and storage configuration options provided by specific components, see the [Transports](../transport/general.md) and [Storage](../storage/general.md) sections.

## Configuration in Subscribers

Subscribers use the `[CapSubscribe]` attribute to mark themselves as subscribers. They can be located in an ASP.NET Core Controller or Service.

When you declare `[CapSubscribe]`, you can change the behavior of the subscriber by specifying the following parameters.

### Name

> string, required

Subscribe to messages by specifying the `Name` parameter. This corresponds to the name specified when publishing the message through `_cap.Publish("Name")`.

This name corresponds to different items in different message brokers:

- In RabbitMQ, it corresponds to the Routing Key.
- In Kafka, it corresponds to the Topic.
- In Azure Service Bus, it corresponds to the Subject.
- In NATS, it corresponds to the Subject.
- In Redis Streams, it corresponds to the Stream.

### Group

> string, optional

Specify the `Group` parameter to place subscribers within a separate consumer group, a concept similar to consumer groups in Kafka. If this parameter is not specified, the current assembly name (`DefaultGroupName`) is used as the default.

Subscribers with the same `Name` but set to **different** groups will all receive messages. Conversely, if subscribers with the same `Name` are set to the **same** group, only one will receive the message.

It also makes sense for subscribers with different `Names` to be set to **different** groups; they can have independent threads for execution. Conversely, if subscribers with different `Names` are set to the **same** group, they will share consumption threads.

Group corresponds to different items in different message brokers:

- In RabbitMQ, it corresponds to Queue.
- In Kafka, it corresponds to Consumer Group.
- In Azure Service Bus, it corresponds to Subscription Name.
- In NATS, it corresponds to Queue Group.
- In Redis Streams, it corresponds to Consumer Group.

###  GroupConcurrent

> byte, optional

Set the parallelism of concurrent execution for subscribers by specifying the value of the `GroupConcurrent` parameter. Concurrent execution means that it needs to run on an independent thread, so if you do not specify the `Group` parameter, CAP will automatically create a Group using the value of `Name`.

!!! Note
    If you have multiple subscribers configured with the same Group and have also set the `GroupConcurrent` value for them, the degree of parallelism is the sum of the values in the group.  
    This setting applies only to new messages; retried messages are not subject to the concurrency limit.

## Custom Configuration

The `CapOptions` class is used to store configuration information. By default, all options have default values. Sometimes you may need to customize them.

#### DefaultGroupName

> Default: cap.queue.{assembly name}

The default consumer group name. It corresponds to different names in different transports. You can customize this value to customize the names in different transports for easy viewing.

!!! info "Mapping"
    Maps to [Queue Names](https://www.rabbitmq.com/queues.html#names) in RabbitMQ.  
    Maps to [Consumer Group Id](http://kafka.apache.org/documentation/#group.id) in Apache Kafka.  
    Maps to Subscription Name in Azure Service Bus.  
    Maps to [Queue Group Name](https://docs.nats.io/nats-concepts/queue) in NATS.
    Maps to [Consumer Group](https://redis.io/topics/streams-intro#creating-a-consumer-group) in Redis Streams.

#### GroupNamePrefix

> Default: Null

Add unified prefixes to consumer group names. https://github.com/dotnetcore/CAP/pull/780

#### TopicNamePrefix

> Default: Null

Add unified prefixes to topic/queue names. https://github.com/dotnetcore/CAP/pull/780

#### Version

> Default: v1

Used to specify a version for a message to isolate messages of different versions across services. This is useful for A/B testing or multi-service version scenarios. The following are application scenarios that require versioning:

!!! info "Business Iteration and Backward Compatibility"
    Due to rapid iteration of business logic, the message data structure may change during service integration. Sometimes we add or modify data structures to accommodate new requirements. If you have a brand new system, this is not a problem. However, if your system is already deployed to production and serving customers, new features can become incompatible with old data structures when released online, which can cause serious issues. To work around this problem, you would need to clear all message queues and persistent messages before restarting the application, which is obviously unacceptable for production environments.

!!! info "Multiple Server Versions"
    Sometimes, the server needs to provide multiple sets of interfaces to support different versions of the client application. The data structures for the same interface interactions between different app versions and the server may differ, so the server typically provides different routing addresses to accommodate different client versions.

!!! info "Different Instances Using the Same Storage Table/Collection"
    If you want multiple service instances to share the same database, you can isolate database tables for different instances by specifying different table names. This can be achieved through CAP configuration by setting different table name prefixes.

> Check out the blog to learn more about the Version feature: https://www.cnblogs.com/savorboard/p/cap-2-4.html

#### FailedRetryInterval

> Default: 60 sec

During the message sending process, if message transmission fails, CAP will retry sending. This configuration option specifies the interval between each retry attempt.

During the message consumption process, if the consumer method fails, CAP will retry execution. This configuration option specifies the interval between each retry attempt.

!!! WARNING "Retry & Interval"
    By default, if a failure occurs during send or consume operations, retry will begin after **4 minutes** (FallbackWindowLookbackSeconds) to avoid potential issues caused by message state delays.    
    Send and consume failures are retried 3 times immediately. After the initial 3 attempts, retries follow a polling schedule, at which point the FailedRetryInterval configuration takes effect.

!!! WARNING "Multi-instance Concurrent Retries"
    Version 7.1.0 introduced database-based distributed locks to solve the problem of concurrent database fetches during retry operations across multiple instances. You must explicitly set `UseStorageLock` to true to enable this.

#### UseStorageLock

> Default: false

If set to true, we will use a database-based distributed lock to handle concurrent data fetches by retry processes across multiple instances. This will generate the cap.lock table in the database.

#### CollectorCleaningInterval

> Default: 300 sec

The interval at which the collector deletes expired messages.

#### SchedulerBatchSize

> Default: 1000

Maximum number of delayed or queued messages fetched per scheduler cycle.

#### ConsumerThreadCount

> Default: 1

Number of consumer threads. When this value is greater than 1, the order of message execution cannot be guaranteed.

#### FailedRetryCount

> Default: 50

Maximum number of retries. When this count is reached, retries will stop. You can modify this parameter to set the maximum retry attempts.

#### FallbackWindowLookbackSeconds

> Default: 240 sec

Configures the retry processor to pick up messages with `Scheduled` or `Failed` status within the lookback time window.

#### FailedThresholdCallback

> Default: NULL

Type: `Action<FailedInfo>`

Failure threshold callback. This action is invoked when retry attempts reach the value set by `FailedRetryCount`. You can use this callback to receive notifications and take manual intervention. For example, send an email or notification. 

#### SucceedMessageExpiredAfter

> Default: 24*3600 sec (1 day)

Expiration time (in seconds) for successfully sent or consumed messages. When a message is sent or consumed successfully, it will be removed from the database after `SucceedMessageExpiredAfter` seconds. You can set the expiration time by modifying this value.

#### FailedMessageExpiredAfter

> Default: 15*24*3600 sec (15 days)

Expiration time (in seconds) for failed messages. When a message fails to send or consume, it will be removed from the database after `FailedMessageExpiredAfter` seconds. You can set the expiration time by modifying this value.

#### [Removed] UseDispatchingPerGroup 

> Default: false

> Removed in version 8.2, now default behavior

If multiple consumers are within the same group, each consumer group pushes received messages to its own dispatching pipeline channel. Each channel has a thread count set to the `ConsumerThreadCount` value.

#### [Obsolete] EnableConsumerPrefetch

> Default: false (Before version 7.0, the default was true)

This option has been renamed to `EnableSubscriberParallelExecute`. Please use the new option instead.

#### EnableSubscriberParallelExecute

> Default: false

If set to `true`, CAP will prefetch a batch of messages from the broker and buffer them, then execute the subscriber method. After execution completes, it fetches the next batch for processing.

!!! note "Precautions"
    Setting this to true may cause issues. If the subscriber method executes slowly and takes a long time, the retry thread may pick up messages that have not yet been executed. The retry thread picks up messages from 4 minutes ago (FallbackWindowLookbackSeconds) by default. If the consumer side has more than 4 minutes (FallbackWindowLookbackSeconds) of message backlog, those messages will be picked up again and executed again.

#### SubscriberParallelExecuteThreadCount

> Default: `Environment.ProcessorCount`

Specifies the number of threads for parallel task execution when `EnableSubscriberParallelExecute` is enabled.

#### SubscriberParallelExecuteBufferFactor

> Default: 1

Multiplier used to determine the buffered capacity size during parallel subscriber execution when `EnableSubscriberParallelExecute` is enabled. The buffer capacity is calculated by multiplying this factor with `SubscriberParallelExecuteThreadCount`, which represents the number of threads allocated for parallel processing.

#### EnablePublishParallelSend

> Default: false (In versions 7.2 <= Version < 8.1, the default is true)

By default, sent messages are placed into a single in-memory channel and then processed linearly.
If set to true, message sending tasks will be processed in parallel by the .NET thread pool, which will greatly improve sending performance.
