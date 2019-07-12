<p align="center">
  <img height="140" src="https://cap.dotnetcore.xyz/img/logo.svg">
</p>

# CAP 　　　　　　　　　　　　　　　　　　　　　　[中文](https://github.com/dotnetcore/CAP/blob/master/README.zh-cn.md)
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/master.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/v8gfh6pe2u2laqoa/branch/master?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap/branch/master)
[![NuGet](https://img.shields.io/nuget/v/DotNetCore.CAP.svg)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![NuGet Preview](https://img.shields.io/nuget/vpre/DotNetCore.CAP.svg?label=nuget-pre)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![Member project of .NET Core Community](https://img.shields.io/badge/member%20project%20of-NCC-9e20c9.svg)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP is a library based on .Net standard, which is a solution to deal with distributed transactions, also has the function of EventBus, it is lightweight, easy to use, and efficiently.

In the process of building an SOA or MicroService system, we usually need to use the event to integrate each services. In the process, the simple use of message queue does not guarantee the reliability. CAP is adopted the local message table program integrated with the current database to solve the exception may occur in the process of the distributed system calling each other. It can ensure that the event messages are not lost in any case.

You can also use the CAP as an EventBus. The CAP provides a simpler way to implement event publishing and subscriptions. You do not need to inherit or implement any interface during the process of subscription and sending.

## Architecture overview

![cap.png](https://cap.dotnetcore.xyz/img/architecture-new.png)

> CAP implements the Outbox Pattern described in the [eShop ebook](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus).

## Getting Started

### NuGet

You can run the following command to install the CAP in your project.

```
PM> Install-Package DotNetCore.CAP
```

CAP supports RabbitMQ,Kafka and AzureService as message queue, select the packages you need to install:

```
PM> Install-Package DotNetCore.CAP.Kafka
PM> Install-Package DotNetCore.CAP.RabbitMQ
PM> Install-Package DotNetCore.CAP.AzureServiceBus
```

CAP supports SqlServer, MySql, PostgreSql，MongoDB as event log storage.

```
// select a database provider you are using, event log table will integrate into.

PM> Install-Package DotNetCore.CAP.SqlServer
PM> Install-Package DotNetCore.CAP.MySql
PM> Install-Package DotNetCore.CAP.PostgreSql
PM> Install-Package DotNetCore.CAP.MongoDB     //need MongoDB 4.0+ cluster
```

### Configuration

First,You need to config CAP in your Startup.cs：

```cs
public void ConfigureServices(IServiceCollection services)
{
    //......

    services.AddDbContext<AppDbContext>(); //Options, If you are using EF as the ORM
    services.AddSingleton<IMongoClient>(new MongoClient("")); //Options, If you are using MongoDB

    services.AddCap(x =>
    {
        // If you are using EF, you need to add the configuration：
        x.UseEntityFramework<AppDbContext>(); //Options, Notice: You don't need to config x.UseSqlServer(""") again! CAP can autodiscovery.

        // If you are using ADO.NET, choose to add configuration you needed：
        x.UseSqlServer("Your ConnectionStrings");
        x.UseMySql("Your ConnectionStrings");
        x.UsePostgreSql("Your ConnectionStrings");

        // If you are using MongoDB, you need to add the configuration：
        x.UseMongoDB("Your ConnectionStrings");  //MongoDB 4.0+ cluster

        // CAP support RabbitMQ,Kafka,AzureService as the MQ, choose to add configuration you needed：
        x.UseRabbitMQ("ConnectionString");
        x.UseKafka("ConnectionString");
        x.UseAzureServiceBus("ConnectionString");
    });
}

```

### Publish

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

### Subscribe

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

If your subscribe method is not in the Controller,then your subscribe class need to Inheritance `ICapSubscribe`:

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

Then inject your  `ISubscriberService`  class in Startup.cs

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

#### Subscribe Group

The concept of a subscription group is similar to that of a consumer group in Kafka. it is the same as the broadcast mode in the message queue, which is used to process the same message between multiple different microservice instances.

When CAP startup, it will use the current assembly name as the default group name, if multiple same group subscribers subscribe the same topic name, there is only one subscriber can receive the message.
Conversely, if subscribers are in different groups, they will all receive messages.

In the same application, you can specify the `Group` property to keep they are in different subscribe groups:

```C#

[CapSubscribe("xxx.services.show.time", Group = "group1" )]
public void ShowTime1(DateTime datetime)
{
}

[CapSubscribe("xxx.services.show.time", Group = "group2")]
public void ShowTime2(DateTime datetime)
{
}

```
`ShowTime1` and `ShowTime2` will be called at the same time.

BTW, You can specify the default group name in the configuration :

```C#
services.AddCap(x =>
{
    x.DefaultGroup = "default-group-name";  
});

```

### Dashboard

CAP v2.1+ provides the dashboard pages, you can easily view the sent and received messages. In addition, you can also view the  message status in real time on the dashboard.

In the distributed environment, the dashboard built-in integrated [Consul](http://consul.io) as a node discovery, while the realization of the gateway agent function, you can also easily view the node or other node data, It's like you are visiting local resources.

```c#
services.AddCap(x =>
{
    //...

    // Register Dashboard
    x.UseDashboard();

    // Register to Consul
    x.UseDiscovery(d =>
    {
        d.DiscoveryServerHostName = "localhost";
        d.DiscoveryServerPort = 8500;
        d.CurrentNodeHostName = "localhost";
        d.CurrentNodePort = 5800;
        d.NodeId = 1;
        d.NodeName = "CAP No.1 Node";
    });
});
```

The default dashboard address is :[http://localhost:xxx/cap](http://localhost:xxx/cap) , you can also change the `cap` suffix to others with `d.MatchPath` configuration options.

![dashboard](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220827302-189215107.png)

![received](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220934115-1107747665.png)

![subscibers](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220949193-884674167.png)

![nodes](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004221001880-1162918362.png)


## Contribute

One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

### License

[MIT](https://github.com/dotnetcore/CAP/blob/master/LICENSE.txt)
