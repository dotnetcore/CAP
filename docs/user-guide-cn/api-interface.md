CAP 的 API 接口只有一个，就是 `ICapPublisher` 接口，你可以从 DI 容器中获取到该接口的实例进行调用。

### 发布/发送

你可以使用 `ICapPublisher` 接口中的 `Publish<T>` 或者 `PublishAsync<T>` 方法来发送消息：

```c#
public class PublishController : Controller
{
    private readonly ICapPublisher _capBus;

    public PublishController(ICapPublisher capPublisher)
    {
        _capBus = capPublisher;
    }
    
    //不使用事务
    [Route("~/without/transaction")]
    public IActionResult WithoutTransaction()
    {
        _capBus.Publish("xxx.services.show.time", DateTime.Now);
	
        return Ok();
    }

    //Ado.Net 中使用事务，自动提交
    [Route("~/adonet/transaction")]
    public IActionResult AdonetWithTransaction()
    {
        using (var connection = new MySqlConnection(ConnectionString))
        {
            using (var transaction = connection.BeginTransaction(_capBus, autoCommit: true))
            {
                //业务代码

                _capBus.Publish("xxx.services.show.time", DateTime.Now);
            }
        }
        return Ok();
    }

    //EntityFramework 中使用事务，自动提交
    [Route("~/ef/transaction")]
    public IActionResult EntityFrameworkWithTransaction([FromServices]AppDbContext dbContext)
    {
        using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
        {
            //业务代码

            _capBus.Publish("xxx.services.show.time", DateTime.Now);
        }
        return Ok();
    }
}

```

下面是PublishAsync这个接口的签名：

**`PublishAsync<T>(string name,T object)`**

默认情况下，在调用此方法的时候 CAP 将在内部创建事务，然后将消息写入到 `Cap.Published` 这个消息表。

#### 消息补偿

有时候当发送一条消息出去之后，希望有一个回调可以获得消费方法的通知，用来补偿发送方做的业务操作，那么可以使用下面这个重载。

**`PublishAsync<T>(string name,T object, string callBackName)`**

这个重载中 `callbackName` 是一个回调的订阅方法名称，当消费端处理完成消息之后CAP会把消费者的处理结果返回并且调用指定的订阅方法。

> 在一些需要业务补偿的场景中，我们可以利用此特性进行一些还原的补偿操作。例如：电商系统中的付款操作，订单在进行支付调用支付服务的过程中如果发生异常，那么支付服务可以通过返回一个结果来告诉调用方此次业务失败，调用方将支付状态标记为失败。 调用方通过订阅 `callbackName`(订阅参数为消费方方法的返回值) 即可接收到支付服务消费者方法的返回结果，从而进行补偿的业务处理。

下面是使用方法：

```C#

// 发送方
_capBus.Publish("xxx.services.show.time",DaateTime.Now,"callback-show-execute-time");

[CapSubscribe("callback-show-execute-time")]   //对应发送的 callbackName
public void ShowPublishTimeAndReturnExecuteTime(DateTime time)
{
    Console.WriteLine(time); // 这是订阅方返回的时间
}

//--------------------------------------------------------------------------------

//订阅方
[CapSubscribe("xxx.services.show.time")]
public DateTime ShowPublishTimeAndReturnExecuteTime(DateTime time)
{
    Console.WriteLine(time); // 这是发送的时间

    return DateTime.Now; // 这是消费者返回的时间，CAP会取该方法的返回值用来传递到发送方的回调订阅里面
}

```

#### 事务

事务在 CAP 具有重要作用，它是保证消息可靠性的一个基石。 在发送一条消息到消息队列的过程中，如果不使用事务，我们是没有办法保证我们的业务代码在执行成功后消息已经成功的发送到了消息队列，或者是消息成功的发送到了消息队列，但是业务代码确执行失败。

这里的失败原因可能是多种多样的，比如连接异常，网络故障等等。

*只有业务代码和CAP的Publish代码必须在同一个事务中，才能够保证业务代码和消息代码同时成功或者失败。*

以下是两种使用事务进行Publish的代码：

* EntityFramework

```c#
using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false)
{
    //业务代码

    _capBus.Publish("xxx.services.show.time", DateTime.Now);

    trans.Commit();
}
```

在不使用自动提交的时候，你的业务代码可以位于 Publish 之前或者之后，只需要保证在同一个事务。 

当使用自动提交时候，需要确保 `_capBus.Publish` 位于代码的最后。

其中，发送的内容会序列化为Json存储到消息表中。

* Dapper

```c#
using (var connection = new MySqlConnection(ConnectionString))
{
    using (var transaction = connection.BeginTransaction(_capBus, autoCommit: false))
    {
        //your business code
        connection.Execute("insert into test(name) values('test')", transaction: (IDbTransaction)transaction.DbTransaction);

        _capBus.Publish("sample.rabbitmq.mysql", DateTime.Now);

        transaction.Commit();
    }
}
```

### 订阅/消费

**注意：框架无法做到100%确保消息只执行一次，所以在一些关键场景消息端在方法实现的过程中自己保证幂等性。**

使用 `CapSubscribeAttribute` 来订阅 CAP 发布出去的消息。

```
[CapSubscribe("xxx.services.bar")]
public void BarMessageProcessor()
{
    
}

```

这里，你也可以使用多个 `CapSubscribe[""]` 来同时订阅多个不同的消息 :

```
[CapSubscribe("xxx.services.bar")]
[CapSubscribe("xxx.services.foo")]
public void BarAndFooMessageProcessor()
{
    
}

```

其中，`xxx.services.bar` 为订阅的消息名称，内部实现上，这个名称在不同的消息队列具有不同的代表。 在 Kafka 中，这个名称即为 Topic Name。 在RabbitMQ 中，为 RouteKey。

> RabbitMQ 中的 RouteKey 支持绑定键表达式写法，有两种主要的绑定键：
> 
> \*（星号）可以代替一个单词.
> 
> \# (井号) 可以代替0个或多个单词.
> 
> 比如在下面这个图中(P为发送者，X为RabbitMQ中的Exchange，C为消费者，Q为队列)
> 
> ![](http://images2017.cnblogs.com/blog/250417/201708/250417-20170807093230268-283915002.png)
> 
> 在这个示例中，我们将发送一条关于动物描述的消息，也就是说 Name(routeKey) 字段中的内容包含 3 个单词。第一个单词是描述速度的（celerity），第二个单词是描述颜色的（colour），第三个是描述哪种动物的（species），它们组合起来类似：“<celerity>.<colour>.<species>”。
> 
> 然后在使用 `CapSubscribe` 绑定的时候，Q1绑定为 `CapSubscribe["*.orange.*"]`, Q2 绑定为  `CapSubscribe["*.*.rabbit"]` 和 `[CapSubscribe["lazy.#]`。
> 
> 那么，当发送一个名为 "quick.orange.rabbit" 消息的时候，这两个队列将会同时收到该消息。同样名为 `lazy.orange.elephant`的消息也会被同时收到。另外，名为 "quick.orange.fox" 的消息将仅会被发送到Q1队列，名为 "lazy.brown.fox" 的消息仅会被发送到Q2。"lazy.pink.rabbit" 仅会被发送到Q2一次，即使它被绑定了2次。"quick.brown.fox" 没有匹配到任何绑定的队列，所以它将会被丢弃。
> 
> 另外一种情况，如果你违反约定，比如使用 4个单词进行组合，例如 "quick.orange.male.rabbit"，那么它将匹配不到任何的队列，消息将会被丢弃。
> 
> 但是，假如你的消息名为 "lazy.orange.male.rabbit"，那么他们将会被发送到Q2，因为 #（井号）可以匹配 0 或者多个单词。


在 CAP 中，我们把每一个拥有 `CapSubscribe[]`标记的方法叫做**订阅者**，你可以把订阅者进行分组。

**组(Group)**，是订阅者的一个集合，每一组可以有一个或者多个消费者，但是一个订阅者只能属于某一个组。同一个组内的订阅者订阅的消息只能被消费一次。

如果你在订阅的时候没有指定组，CAP会将订阅者设置到一个默认的组，默认的组名称为 `cap.queue.{程序集名称}`。

以下是使用组进行订阅的示例：

```c#
[CapSubscribe("xxx.services.foo", Group = "moduleA")]
public void FooMessageProcessor()
{
    
}

```

#### 例外情况

这里有几种情况可能需要知道：

**① 消息发布的时候订阅方还未启动**

Kafka:

当 Kafka 中，发布的消息存储于持久化的日志文件中，所以消息不会丢失，当订阅者所在的程序启动的时候会消费掉这些消息。

RabbitMQ：

在 RabbitMQ 中，应用程序**首次启动**会创建具有持久化的 Exchange 和 Queue，CAP 会针对每一个订阅者Group会新建一个消费者队列，**由于首次启动时候订阅者未启动的所以是没有队列的，消息无法进行持久化，这个时候生产者发的消息会丢失**。

针对RabbitMQ的消息丢失的问题，有两种解决方式：

i. 部署应用程序之前，在RabbitMQ中手动创建具有durable特性的Exchange和Queue，默认情况他们的名字分别是(cap.default.topic, cap.default.group)。

ii. 提前运行一遍所有实例，让Exchange和Queue初始化。

我们建议采用第 ii 种方案，因为很容易做到。