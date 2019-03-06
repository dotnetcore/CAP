CAP 封装了在 ASP.NET Core 中的使用依赖注入来获取 Publisher （`ICapPublisher`）的接口。而启动方式类似于 “中间件” 的形式，通过在 Startup.cs 配置 `ConfigureServices` 和 `Configure` 进行启动。

### 消息表

当系统引入CAP之后并首次启动后，CAP会在客户端生成 2 个表，分别是 Cap.Published, Cap.Received 。注意表名可能在不同的数据库具有不同的大小写区分，如果你在运行项目的时候没有显式的指定数据库生成架构(Schema)或者表名前缀(TableNamePrefix)的话，默认情况下就是以上的名字。

**Cap.Published**：这个表主要是用来存储 CAP 发送到MQ(Message Queue)的客户端消息，也就是说你使用 `ICapPublisher` 接口 Publish 的消息内容。

**Cap.Received**：这个表主要是用来存储 CAP 接收到 MQ(Message Queue) 的客户端订阅的消息，也就是使用 `CapSubscribe[]` 订阅的那些消息。

> 2.2 版本以前：
> **Cap.Queue**： 这个表主要是CAP内部用来处理发送和接收消息的一个临时表，通常情况下，如果系统不出现问题，这个表将是空的。

`Published` 和 `Received` 表具有 StatusName 字段，这个字段用来标识当前消息的状态。目前共有 `Scheduled`，`Successed`，`Failed` 等几个状态。

> 在 2.2 版本以前的所有状态为：`Scheduled`，`Enqueued`，`Processing`，`Successed`，`Failed` 

CAP 在处理消息的过程中会依次从`Scheduled` 到 `Successed` 来改变这些消息状态的值。如果是状态值为 `Successed`，代表该消息已经成功的发送到了 MQ 中。如果为 Failed 则代表消息发送失败。

CAP 2.2 以上版本中会针对 `Scheduled`,`Failed` 状态的消息 CAP 会于消息持久化过后 4 分钟后开始进行重试，重试的间隔默认为 60 秒，你可以在 `CapOptions` 中配置的`FailedRetryInterval` 来调整默认间隔时间。

> 2.2 版本以前， CAP 会对状态为 `Failed` 的消息默认进行 100 次重试。

### 消息格式

CAP 采用 JSON 格式进行消息传输，以下是消息的对象模型：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | 消息编号 | int
Version | 消息版本 | string
Name | 消息名称 | string
Content | 内容 | string
Group | 所属消费组 | string
Added |　创建时间 | DateTime
ExpiresAt | 过期时间 | DateTime
Retries | 重试次数 | int
StatusName | 状态 | string

>对于 Cap.Received 中的消息，会多一个 `Group` 字段来标记所属的消费者组。

对于消息内容 Content 属性里面的内容CAP 使用 Message 对象进行了一次二次包装。一下为Message对象的信息

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | CAP生成的消息编号 | string 
Timestamp | 消息创建时间 | string
Content | 内容 | string
CallbackName | 回调的订阅者名称 | string

其中 Id 字段，CAP 采用的 MongoDB 中的 ObjectId 分布式Id生成算法生成。

### EventBus 

EventBus 采用 发布-订阅 风格进行组件之间的通讯，它不需要显式在组件中进行注册。

![](http://images2017.cnblogs.com/blog/250417/201708/250417-20170804153901240-1774287236.png)

上图是EventBus的一个Event的流程，关于 EventBus 的更多信息就不在这里介绍了...

在 CAP 中，为什么说 CAP 实现了 EventBus 中的全部特性，因为 EventBus 具有的两个大功能就是发布和订阅， 在 CAP 中 使用了另外一种优雅的方式来实现的，另外一个 CAP 提供的强大功能就是消息的持久化，以及在任何异常情况下消息的可靠性，这是EventBus不具有的功能。

![](https://camo.githubusercontent.com/452505edb71d41f2c1bd18907275b76291621e46/687474703a2f2f696d61676573323031352e636e626c6f67732e636f6d2f626c6f672f3235303431372f3230313730372f3235303431372d32303137303730353137353832373132382d313230333239313436392e706e67)

CAP 里面发送一个消息可以看做是一个 “Event”，一个使用了CAP的ASP.NET Core 应用程序既可以进行发送也可以进行订阅接收。


### 重试

重试在整个CAP架构设计中具有重要作用，CAP 中会针对发送失败或者执行失败的消息进行重试。在整个 CAP 的设计过程中有以下几处采用的重试策略。

**1、 发送重试**

在消息发送过程中，当出现 Broker 宕机或者连接失败的情况亦或者出现异常的情况下，这个时候 CAP 会对发送的重试，第一次重试次数为 3，4分钟后以后每分钟重试一次，进行次数 +1，当总次数达到50次后，CAP将不对其进行重试。

> 你可以在 `CapOptions` 中设置`FailedRetryCount`来调整默认重试的总次数。

当失败总次数达到默认失败总次数后，就不会进行重试了，你可以在 Dashboard 中查看消息失败的原因，然后进行人工重试处理。

**2、 消费重试**

当 Consumer 接收到消息时，会执行消费者方法，在执行消费者方法出现异常时，会进行重试。这个重试策略和上面的 `发送重试` 是相同的。

### 数据清理

数据库消息表中具有一个 `ExpiresAt` 字段表示消息的过期时间，当消息发送成功或者消费成功后，CAP会将消息状态为 `Successed` 的 `ExpiresAt` 设置为 **1小时** 后过期，会将消息状态为 `Failed` 的 `ExpiresAt` 设置为 **15天** 后过期。

CAP 默认情况下会每隔一个小时将消息表的数据进行清理删除，避免数据量过多导致性能的降低。清理规则为 ExpiresAt 不为空并且小于当前时间的数据。 也就是说状态为`Failed`的消息（正常情况他们已经被重试了 50 次），如果你15天没有人工介入处理，同样会被清理掉。