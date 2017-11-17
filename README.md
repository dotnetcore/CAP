# CAP 　　　　　　　　　　　　　　　　　　　　　　[中文](https://github.com/dotnetcore/CAP/blob/develop/README.zh-cn.md)
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/develop.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![NuGet](https://img.shields.io/nuget/v/DotNetCore.CAP.svg)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![NuGet Preview](https://img.shields.io/nuget/vpre/DotNetCore.CAP.svg?label=nuget-pre)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![Member project of .NET China Foundation](https://img.shields.io/badge/member_project_of-.NET_CHINA-red.svg?style=flat&colorB=9E20C8)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP is a library based on .Net standard, which is a solution to deal with distributed transactions, also has the function of EventBus, it is lightweight, easy to use, and efficiently.

## OverView

In the process of building an SOA or MicroService system, we usually need to use the event to integrate each services. In the process, the simple use of message queue does not guarantee the reliability. CAP is adopted the local message table program integrated with the current database to solve the exception may occur in the process of the distributed system calling each other. It can ensure that the event messages are not lost in any case.

You can also use the CAP as an EventBus. The CAP provides a simpler way to implement event publishing and subscriptions. You do not need to inherit or implement any interface during the process of subscription and sending.

This is a diagram of the CAP working in the ASP.NET Core MicroService architecture:

![](http://images2015.cnblogs.com/blog/250417/201707/250417-20170705175827128-1203291469.png)

> The solid line in the figure represents the user code, and the dotted line represents the internal implementation of the CAP.

## Getting Started

### NuGet

You can run the following command to install the CAP in your project.

```
PM> Install-Package DotNetCore.CAP
```

If you want use Kafka to send integrating event, installing by:

```
PM> Install-Package DotNetCore.CAP.Kafka
```

If you want use RabbitMQ to send integrating event, installing by:

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
```

CAP supports SqlServer, MySql, PostgreSql as event log storage. 

```

// select a database provider you are using, event log table will integrate into.

PM> Install-Package DotNetCore.CAP.SqlServer
PM> Install-Package DotNetCore.CAP.MySql
PM> Install-Package DotNetCore.CAP.PostgreSql
```

### Configuration

First,You need to config CAP in your Startup.cs：

```cs
public void ConfigureServices(IServiceCollection services)
{
    //......

    services.AddDbContext<AppDbContext>();

    services.AddCap(x =>
    {
        // If you are using EF, you need to add the following configuration：
        // Notice: You don't need to config x.UseSqlServer(""") again! CAP can autodiscovery.
        x.UseEntityFramework<AppDbContext>();

        // If you are using ado.net,you need to add the configuration：
        x.UseSqlServer("Your ConnectionStrings");
        x.UseMySql("Your ConnectionStrings");
        x.UsePostgreSql("Your ConnectionStrings");
		
        // If you are using RabbitMQ, you need to add the configuration：
        x.UseRabbitMQ("localhost");

        // If you are using Kafka, you need to add the configuration：
        x.UseKafka("localhost");
    });
}

public void Configure(IApplicationBuilder app)
{
    //.....

    app.UseCap();
}

```

### Publish

Inject `ICapPublisher` in your Controller, then use the `ICapPublisher` to send message

```c#
public class PublishController : Controller
{
    [Route("~/publishWithTransactionUsingEF")]
    public async Task<IActionResult> PublishMessageWithTransactionUsingEF([FromServices]AppDbContext dbContext, [FromServices]ICapPublisher publisher)
    {
        using (var trans = dbContext.Database.BeginTransaction())
        {
            // your business code

            //If you are using EF, CAP will automatic discovery current environment transaction, so you do not need to explicit pass parameters.
            //Achieving atomicity between original database operation and the publish event log thanks to a local transaction.
            await publisher.PublishAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

            trans.Commit();
        }
        return Ok();
    }

    [Route("~/publishWithTransactionUsingAdonet")]
    public async Task<IActionResult> PublishMessageWithTransactionUsingAdonet([FromServices]ICapPublisher publisher)
    {
        var connectionString = "";
        using (var sqlConnection = new SqlConnection(connectionString))
        {
            sqlConnection.Open();
            using (var sqlTransaction = sqlConnection.BeginTransaction())
            {
                // your business code

                publisher.Publish("xxx.services.account.check", new Person { Name = "Foo", Age = 11 }, sqlTransaction);

                sqlTransaction.Commit();
            }
        }
        return Ok();
    }
}

```

### Subscribe

**Action Method**

Add the Attribute `[CapSubscribe()]` on Action to subscribe message:

```c#
public class PublishController : Controller
{
    [CapSubscribe("xxx.services.account.check")]
    public async Task CheckReceivedMessage(Person person)
    {
        Console.WriteLine(person.Name);
        Console.WriteLine(person.Age);     
        return Task.CompletedTask;
    }
}

```

**Service Method**

If your subscribe method is not in the Controller,then your subscribe class need to Inheritance `ICapSubscribe`: 

```c#

namespace xxx.Service
{
    public interface ISubscriberService
    {
        public void CheckReceivedMessage(Person person);
    }


    public class SubscriberService: ISubscriberService, ICapSubscribe
    {
        [CapSubscribe("xxx.services.account.check")]
        public void CheckReceivedMessage(Person person)
        {
        }
    }
}

```

Then inject your  `ISubscriberService`  class in Startup.cs 

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<ISubscriberService,SubscriberService>();
}
```

### Dashboard

CAP 2.1 and above provides the dashboard pages, you can easily view the sent and received messages. In addition, you can also view the  message status in real time on the dashboard.

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

![dashboard](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220827302-189215107.png)

![received](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220934115-1107747665.png)

![subscibers](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004220949193-884674167.png)

![nodes](http://images2017.cnblogs.com/blog/250417/201710/250417-20171004221001880-1162918362.png)


## Contribute

One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

### License

[MIT](https://github.com/dotnetcore/CAP/blob/master/LICENSE.txt)
