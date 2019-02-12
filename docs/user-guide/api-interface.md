# Interfaces

CAP only has one interface,It is `ICapPublisher`, You can get its instance from the DI container and then call it.

## Publish & Send

You can use the `Publish<T>` or `PublishAsync<T>`  methods defined in the `ICapPublisher` interface to send the event messages.

```c# hl_lines="19 33"
public class PublishController : Controller
{
    private readonly ICapPublisher _capBus;

    public PublishController(ICapPublisher capPublisher)
    {
        _capBus = capPublisher;
    }

    [Route("~/adonet/transaction")]
    public IActionResult AdonetWithTransaction()
    {
        using (var connection = new MySqlConnection(ConnectionString))
        {
            using (var transaction = connection.BeginTransaction(_capBus, autoCommit: true))
            {
                //your business code

                _capBus.Publish("xxx.services.show.time", DateTime.Now);
            }
        }

        return Ok();
    }

    [Route("~/ef/transaction")]
    public IActionResult EntityFrameworkWithTransaction([FromServices]AppDbContext dbContext)
    {
        using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
        {
            //your business code

            _capBus.Publish("xxx.services.show.time", DateTime.Now);
        }

        return Ok();
    }
}
```
The following is the signature of the of the PublishAsync method

```c#
PublishAsync<T>(string name, T object)
```

By default,when this method(PublishAsync<T>) is called,CAP will create a transaction internally,
and then write messages into the `Cap.Published` message table.

In some situations,you may need a callback when a message is sent out, you can use the follwing
overload of the `PublishAsync<T>` method:

```c#
PublishAsync<T>(string name, T object, string callBackName)
```

In this overload method, `callbackName` is the callback name of the subscription method,when the consumption-side finished processing messages,CAP will return the processed result and also call the  specified subscription method

### Transactions

Transaction plays a very import role in CAP, It is a main factor to ensure the reliability of messaging. 

In the process of sending a message to the message queue without transaction we can not ensure that messages are sent to the message queue successfully after we finish dealing the business logic,or messages are send to the message queque successfully but our bussiness logic is failed.

There is a variety of reasons that causing failure,eg:connection errors,network errors,etc.

!!! note
    Only by putting the business logic and logic in the Publish of CAP in the same transaction so that we can enssure both them to be success or fail

The following two blocks of code snippet demonstrate how to use transactions in EntityFramework and dapper when publishing messages.


####  EntityFramework

```c#
using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: false)
{
    // Your business logic。

    _capBus.Publish("xxx.services.show.time", DateTime.Now);

    trans.Commit();
}

```
When you set the `autoCommit: false`, you can put your business logic before or after the Publish logic,the only thing you need to do is to ensure that they are in the same transaction.

If you set the `autoCommit: true`, you need publish message `_capBus.Publish` at the last.

During the course,the message content will be serialized as json and stored in the message table.

#### Dapper

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

## Subscribe & Consume

!!! warning
    The businsess logics in the subscription side should be keep idempotent.

    You can view more details in this [ISSUE](https://github.com/dotnetcore/CAP/issues/29#issuecomment-451841287).

Use `CapSubscribe[""]` to decorate a method so that it can  subscribe messages published by CAP.

```c#
[CapSubscribe("xxx.services.bar")]
public void BarMessageProcessor()
{
}
```
You can also use multiple `CapSubscribe[""]` to decorate a method so that you can subscribe messages from different sources accordingly.

```c#
[CapSubscribe("xxx.services.bar")]
[CapSubscribe("xxx.services.foo")]
public void BarAndFooMessageProcessor()
{
}
```
`xxx.services.bar` is the name of the message to be subscribed.And it has different name in different message queque Clients.for example,in kafka the name is called  Topic Name and in RAbbitMQ it is called RouteKey.

In RabbitMQ you can use regular expression in RouteKey:
<blockquote>
  <p> <cite title="Source Title">\*</cite> (Asterisk) stands for  a single word.</p>
  <p> <cite title="Source Title">#</cite> (hash sign) standards for zero or more words.</p>
  <p class="small">See the following picture(P for Publisher,X for Exchange,C for consumer and Q for Queue)</p>
  <p><image src='../../img/rabbitmq-route.png'></image></p>
  <p class="small">In this example, we're going to send messages which all describe animals. The messages will be sent with a routing key that consists of three words (two dots). The first word in the routing key will describe a celerity, second a colour and third a species: "<celerity>.<colour>.<species>".</p>
  <p class="small">We created three bindings: Q1 is bound with binding key "*.orange.*" and Q2 with "*.*.rabbit" and "lazy.#".</p>
  <p class="small">These bindings can be summarised as:</p>
  <p class="small">Q1 is interested in all the orange animals.Q2 wants to hear everything about rabbits, and everything about lazy animals.A message with a routing key set to "quick.orange.rabbit" will be delivered to both queues. Message "lazy.orange.elephant" also will go to both of them. On the other hand "quick.orange.fox" will only go to the first queue, and "lazy.brown.fox" only to the second. "lazy.pink.rabbit" will be delivered to the second queue only once, even though it matches two bindings. "quick.brown.fox" doesn't match any binding so it will be discarded.</p>
  <p class="small">What happens if we break our contract and send a message with one or four words, like "orange" or "quick.orange.male.rabbit"? Well, these messages won't match any bindings and will be lost.</p>
  <p class="small">On the other hand "lazy.orange.male.rabbit", even though it has four words, will match the last binding and will be delivered to the second queue.</p>
</blockquote>

In CAP, we called a method decorated by `CapSubscribe[]` a **subscriber**, you can group different subscribers.

**Group** is a collection of subscribers,each group can have one or multiple consumers,but a subscriber can only belongs to a certain group(you can not put a subscriber into multiple groups).Messages subscribed by members in a certain group can only be consumed once.

If you do not specify any group when subscribing,CAP will put the subscriber into a default group named `cap.default.group`

the following is a demo shows how to use group when subscribing.

```c#
[CapSubscribe("xxx.services.foo", Group = "moduleA")]
public void FooMessageProcessor()
{
    
}

```

### Exceptional case

The following situations you shoud be aware of.

**① the subscription side has not started yet when publishing a message**

#### Kafka

In Kafka,published messages stored in the Persistent log files,so messages will not lost.when the subscription side started,it can still consume the message.


#### RabbitMQ

In RabbitMQ, the application will create Persistent Exchange and Queue at the **first start**, CAP will create a new consumer queue for each consumer group,**because the application started but the subscription side hasn's start yet so there has no queue,thus the message can not be persited,and the published messages will lost**

There are two ways to solve this `message lost` issue in RamitMQ:

* Before the deployment of your application,you can create durable Exchange and Queue in RabbitMQ by hand,the default names them are (cap.default.topic, cap.default.group).

* Run all instances in advance to ensure that both Exchange and Queue are initialized.

It is highly recommanded that users adopt the second way,because it is easier to achieve.
