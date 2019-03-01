### 介绍

CAP 是一个遵循 .NET Standard 标准库的C#库，用来处理分布式事务以及提供EventBus的功能，她具有轻量级，高性能，易使用等特点。

目前 CAP 使用的是 .NET Standard 1.6 的标准进行开发，目前最新预览版本已经支持 .NET Standard 2.0.

### 应用场景

CAP 的应用场景主要有以下两个：

* 1. 分布式事务中的最终一致性（异步确保）的方案。

分布式事务是在分布式系统中不可避免的一个硬性需求，而目前的分布式事务的解决方案也无外乎就那么几种，在了解 CAP 的分布式事务方案前，可以阅读以下 [这篇文章](http://www.infoq.com/cn/articles/solution-of-distributed-system-transaction-consistency)。

CAP 没有采用两阶段提交（2PC）这种事务机制，而是采用的 本地消息表+MQ 这种经典的实现方式，这种方式又叫做 异步确保。

* 2. 具有高可用性的 EventBus。

CAP 实现了 EventBus 中的发布/订阅，它具有 EventBus 的所有功能。也就是说你可以像使用 EventBus 一样来使用 CAP，另外 CAP 的 EventBus 是具有高可用性的，这是什么意思呢？

CAP 借助于本地消息表来对 EventBus 中的消息进行了持久化，这样可以保证 EventBus 发出的消息是可靠的，当消息队列出现宕机或者连接失败的情况时，消息也不会丢失。

### Quick Start

* **引用 NuGet 包**

使用一下命令来引用CAP的NuGet包：

```
PM> Install-Package DotNetCore.CAP
```

根据使用的不同类型的消息队列，来引入不同的扩展包：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
PM> Install-Package DotNetCore.CAP.Kafka
```

根据使用的不同类型的数据库，来引入不同的扩展包：

```
PM> Install-Package DotNetCore.CAP.SqlServer
PM> Install-Package DotNetCore.CAP.MySql
PM> Install-Package DotNetCore.CAP.PostgreSql
PM> Install-Package DotNetCore.CAP.MongoDB
```

* **启动配置**

在 ASP.NET Core 程序中，你可以在 `Startup.cs` 文件 `ConfigureServices()` 中配置 CAP 使用到的服务：

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