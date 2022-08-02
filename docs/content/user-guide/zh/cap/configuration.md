# 配置

默认情况下，你在向IoC容器中注册CAP服务的时候指定配置。

```c#
services.AddCap(config=> {
    // config.XXX 
});
```

其中 `services` 代表的是 `IServiceCollection` 接口对象，它位于 `Microsoft.Extensions.DependencyInjection` 下面。 

如果你不想使用微软的IoC容器，那么你可以查看 [ASP.NET Core 这里的文档](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2#default-service-container-replacement) 来了解如何替换默认的容器实现。

## 什么是最低配置？

最简单的回答就是，至少你要配置一个消息队列和一个事件存储，如果你想快速开始你可以使用下面的配置：

```C#
services.AddCap(config => 
{
     config.UseInMemoryMessageQueue();
     config.UseInMemoryStorage();
});
```

有关具体的消息队列的配置和存储的配置，你可以查看 Transports 章节和 Persistent 章节中具体组件提供的配置项。

## CAP 中的自定义配置

在 `AddCap` 中 `CapOptions` 对象是用来存储配置相关信息，默认情况下它们都具有一些默认值，有些时候你可能需要自定义。

#### DefaultGroupName

默认值：cap.queue.{程序集名称}

默认的消费者组的名字，在不同的 Transports 中对应不同的名字，可以通过自定义此值来自定义不同 Transports 中的名字，以便于查看。

!!! info "Mapping"
    在 RabbitMQ 中映射到 [Queue Names](https://www.rabbitmq.com/queues.html#names)。  
    在 Apache Kafka 中映射到 [Consumer Group Id](http://kafka.apache.org/documentation/#group.id)。  
    在 Azure Service Bus 中映射到 Subscription Name。  
    在 NATS 中映射到 [Queue Group Name](https://docs.nats.io/nats-concepts/queue).
    在 Redis Streams 中映射到 [Consumer Group](https://redis.io/topics/streams-intro#creating-a-consumer-group).


#### GroupNamePrefix

默认值：Null

为订阅 Group 统一添加前缀。 https://github.com/dotnetcore/CAP/pull/780

#### TopicNamePrefix

默认值： Null

为 Topic 统一添加前缀。 https://github.com/dotnetcore/CAP/pull/780

#### Version

默认值：v1

这是在CAP v2.4 版本中引入的新配置项，用于给消息指定版本来隔离不同版本服务的消息，常用于A/B测试或者多服务版本的场景。以下是其应用场景：

!!! info "业务快速迭代，需要向前兼容"
    由于业务的快速迭代，在各个服务集成的过程中，消息的数据结构并不是固定不变的，有些时候我们为了适应新引入的需求，会添加或者修改一些数据结构。如果你是一套全新的系统这没有什么问题，但是如果你的系统已经部署到生产环境了并且正在服务客户，这就会导致新的功能在上线的时候和旧的数据结构发生不兼容，那么这些改变可能会导致出现严重的问题，要想解决这个问题，只能把消息队列和持久化的消息全部清空，然后才能启动应用程序，这对于生产环境来说显然是致命的。

!!! info "多个版本的服务端"
    有些时候，App的服务端需要提供多套接口，来支持不同版本的App，这些不同版本的App相同的接口和服务端交互的数据结构可能是不一样的，所以通常情况下服务端提供不用的路由地址来适配不同版本的App调用。

!!! info "不同实例，使用相同的持久化表/集合"
    希望多个不同实例的程序可以公用相同的数据库，在 2.4 之前的版本，我们可以通过指定不同的表名来隔离不同实例的数据库表，即在CAP配置的时候通过配置不同的表名前缀来实现。

> 查看博客来了解更多关于 Version 的信息： https://www.cnblogs.com/savorboard/p/cap-2-4.html


#### FailedRetryInterval

默认值：60 秒

在消息发送的时候，如果发送失败，CAP将会对消息进行重试，此配置项用来配置每次重试的间隔时间。

在消息消费的过程中，如果消费失败，CAP将会对消息进行重试消费，此配置项用来配置每次重试的间隔时间。

!!! WARNING "重试 & 间隔"
    在默认情况下，重试将在发送和消费消息失败的 **4分钟后** 开始，这是为了避免设置消息状态延迟导致可能出现的问题。  
    发送和消费消息的过程中失败会立即重试 3 次，在 3 次以后将进入重试轮询，此时 FailedRetryInterval 配置才会生效。

#### ConsumerThreadCount 

默认值：1

消费者线程并行处理消息的线程数，当这个值大于1时，将不能保证消息执行的顺序。

#### CollectorCleaningInterval

默认值：300 秒

收集器删除已经过期消息的时间间隔。

#### FailedRetryCount

默认值：50

重试的最大次数。当达到此设置值时，将不会再继续重试，通过改变此参数来设置重试的最大次数。

#### FailedThresholdCallback

默认值：NULL

类型：`Action<FailedInfo>`

重试阈值的失败回调。当重试达到 FailedRetryCount 设置的值的时候，将调用此 Action 回调，你可以通过指定此回调来接收失败达到最大的通知，以做出人工介入。例如发送邮件或者短信。

#### SucceedMessageExpiredAfter

默认值：24*3600 秒（1天后）

成功消息的过期时间（秒）。 当消息发送或者消费成功时候，在时间达到 `SucceedMessageExpiredAfter` 秒时候将会从 Persistent 中删除，你可以通过指定此值来设置过期的时间。

#### FailedMessageExpiredAfter

默认值：15*24*3600 秒（15天后）

失败消息的过期时间（秒）。 当消息发送或者消费失败时候，在时间达到 `FailedMessageExpiredAfter` 秒时候将会从 Persistent 中删除，你可以通过指定此值来设置过期的时间。

#### UseDispatchingPerGroup

> 默认值: false

默认情况下，CAP会将所有消费者组的消息都先放置到内存同一个Channel中，然后线性处理。
如果设置为 true，则每个消费者组都会根据 `ConsumerThreadCount` 设置的值创建单独的线程进行处理。
