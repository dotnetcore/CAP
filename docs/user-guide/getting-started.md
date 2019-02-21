# Getting Stared

## Usage 

#### 1. Distributed transaction alternative solution in micro-service base on eventually consistency

A distributed transaction is a very complex process with a lot of moving parts that can fail. Also, if these parts run on different machines or even in different data centers, the process of committing a transaction could become very long and unreliable.

This could seriously affect the user experience and overall system bandwidth. So one of the best ways to solve the problem of distributed transactions is to avoid them completely.

Usually, a microservice is designed in such way as to be independent and useful on its own. It should be able to solve some atomic business task.

If we could split our system in such microservices, there’s a good chance we wouldn’t need to implement transactions between them at all. 

By far, one of the most feasible models of handling consistency across microservices is eventual consistency. This model doesn’t enforce distributed ACID transactions across microservices. Instead, it proposes to use some mechanisms of ensuring that the system would be eventually consistent at some point in the future.

CAP ia an alternative solution without transactions, it comply the eventually consistency and implement base on message queue. 

#### 2. EventBus with Outbox pattern

CAP is an event bus that implements the Outbox pattern, Outbox is an infrastructure feature which simulates the reliability of distributed transactions without requiring use of the Distributed Transaction Coordinator(DTC).

The outbox feature can be used instead of the DTC to mimic the same level of consistency without using distributed transactions.

!!! Tip "CAP implements the Outbox Pattern described in the [eShop ebook](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus)"
    <img src="https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/media/image24.png">

    > Atomicity when publishing events to the event bus with a worker microservice

## Quick Start

### NuGet Package

Use the following command to reference the CAP NuGet package:

```
PM> Install-Package DotNetCore.CAP
```

According to the different types of message queues used, different extension packages are introduced:

``` text
PM> Install-Package DotNetCore.CAP.RabbitMQ
PM> Install-Package DotNetCore.CAP.Kafka
PM> Install-Package DotNetCore.CAP.AzureServiceBus
```

According to the different types of databases used, different extension packages are introduced:

``` text
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
