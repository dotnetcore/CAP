# Getting Stared

## Usage Scenarios

The usage scenarios of CAP mainly include the following two:

**1. The scheme of eventual consistency in distributed transactions**
In the process of building an SOA or MicroService system, we usually need to use the event to integrate each services. In the process, the simple use of message queue does not guarantee the reliability. CAP is adopted the local message table program integrated with the current database to solve the exception may occur in the process of the distributed system calling each other. It can ensure that the event messages are not lost in any case.
<br><br>
Distributed transactions are an inevitable requirement in a distributed system, and the current solution for distributed transactions is nothing more than just a few. Before understanding the CAP's distributed transaction scenarios, you can read the following [Articles] (http://www.infoq.com/en/articles/solution-of-distributed-system-transaction-consistency).
<br><br>
The CAP does not use the two-phase commit (2PC) transaction mechanism, but uses the classical message implementation of the local message table + MQ, which is also called asynchronous guarantee.

**2. Highly usable EventBus**
You can also use the CAP as an EventBus. The CAP provides a simpler way to implement event publishing and subscriptions. You do not need to inherit or implement any interface during the process of subscription and sending.
<br><br>
CAP implements the publish and subscribe method of EventBus, which has all the features of EventBus. This means that you can use CAPs just like EventBus. In addition, CAP's EventBus is highly available. What does this mean?
<br><br>
The CAP uses the local message table to persist the messages in the EventBus. This ensures that the messages sent by the EventBus are reliable. When the message queue fails or fails, the messages are not lost.

## Quick Start

### NuGet Package

Use the following command to reference the CAP NuGet package:

```
PM> Install-Package DotNetCore.CAP
```

According to the different types of message queues used, different extension packages are introduced:

```
PM> Install-Package DotNetCore.CAP.RabbitMQ

PM> Install-Package DotNetCore.CAP.Kafka
```

According to the different types of databases used, different extension packages are introduced:

```
PM> Install-Package DotNetCore.CAP.SqlServer

PM> Install-Package DotNetCore.CAP.MySql

PM> Install-Package DotNetCore.CAP.PostgreSql

PM> Install-Package DotNetCore.CAP.MongoDB
```

### Startup Configuration

In an ASP.NET Core program, you can configure the services used by the CAP in the `Startup.cs` file `ConfigureServices()`:

```c#
public void ConfigureServices(IServiceCollection services)
{
    //......

    services.AddDbContext<AppDbContext>(); //Options, If you are using EF as the ORM
    services.AddSingleton<IMongoClient>(new MongoClient("")); //Options, If you are using MongoDB

    services.AddCap(x =>
    {
        // If you are using EF, you need to add the configuration：
        //Options, Notice: You don't need to config x.UseSqlServer(""") again! CAP can autodiscovery.
        x.UseEntityFramework<AppDbContext>(); 

        // If you are using Ado.Net, you need to add the configuration：
        x.UseSqlServer("Your ConnectionStrings");
        x.UseMySql("Your ConnectionStrings");
        x.UsePostgreSql("Your ConnectionStrings");

        // If you are using MongoDB, you need to add the configuration：
        x.UseMongoDB("Your ConnectionStrings");  //MongoDB 4.0+ cluster

        // If you are using RabbitMQ, you need to add the configuration：
        x.UseRabbitMQ("localhost");

        // If you are using Kafka, you need to add the configuration：
        x.UseKafka("localhost");
    });
}
```

### Usage

#### Publish

Inject `ICapPublisher` in your Controller, then use the `ICapPublisher` to send message

```c#
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
                //your business logic code

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
            //your business logic code

            _capBus.Publish("xxx.services.show.time", DateTime.Now);
        }

        return Ok();
    }
}

```

#### Subscribe

**In Controller Action**

Add the Attribute `[CapSubscribe()]` on Action to subscribe message:

```c#
public class PublishController : Controller
{
    [CapSubscribe("xxx.services.show.time")]
    public void CheckReceivedMessage(DateTime datetime)
    {
        Console.WriteLine(datetime);
    }
}

```

**In Business Logic Service**

If your subscribe method is not in the Controller, the service class you need to Inheritance `ICapSubscribe`:

```c#

namespace BusinessCode.Service
{
    public interface ISubscriberService
    {
        public void CheckReceivedMessage(DateTime datetime);
    }

    public class SubscriberService: ISubscriberService, ICapSubscribe
    {
        [CapSubscribe("xxx.services.show.time")]
        public void CheckReceivedMessage(DateTime datetime)
        {
        }
    }
}

```

Then inject your  `ISubscriberService`  class in `Startup.cs`

```c#
public void ConfigureServices(IServiceCollection services)
{
    //Note: The injection of services needs before of `services.AddCap()`
    services.AddTransient<ISubscriberService,SubscriberService>();

    services.AddCap(x=>
    {
        //...
    });
}
```
