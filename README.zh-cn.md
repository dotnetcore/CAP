# CAP 　　　　　　　　　　　　　　　　　　　　　　[English](https://github.com/dotnetcore/CAP/blob/develop/README.md)
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/develop.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![NuGet](https://img.shields.io/nuget/v/DotNetCore.CAP.svg)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![NuGet Preview](https://img.shields.io/nuget/vpre/DotNetCore.CAP.svg?label=nuget-pre)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![Member project of .NET China Foundation](https://img.shields.io/badge/member_project_of-.NET_CHINA-red.svg?style=flat&colorB=9E20C8)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP 是一个基于 .NET Standard 的 C# 库，它是一种处理分布式事务的解决方案，同样具有 EventBus 的功能，它具有轻量级、易使用、高性能等特点。

你可以在这里[CAP Wiki](https://github.com/dotnetcore/CAP/wiki)看到更多详细资料。

## 预览（OverView）

在我们构建 SOA 或者 微服务系统的过程中，我们通常需要使用事件来对各个服务进行集成，在这过程中简单的使用消息队列并不能保证数据的最终一致性，
CAP 采用的是和当前数据库集成的本地消息表的方案来解决在分布式系统互相调用的各个环节可能出现的异常，它能够保证任何情况下事件消息都是不会丢失的。

你同样可以把 CAP 当做 EventBus 来使用，CAP提供了一种更加简单的方式来实现事件消息的发布和订阅，在订阅以及发布的过程中，你不需要继承或实现任何接口。

这是CAP集在ASP.NET Core 微服务架构中的一个示意图：

![](http://images2015.cnblogs.com/blog/250417/201707/250417-20170705175827128-1203291469.png)

> 图中实线部分代表用户代码，虚线部分代表CAP内部实现。

## Getting Started

### NuGet 

你可以运行以下下命令在你的项目中安装 CAP。

```
PM> Install-Package DotNetCore.CAP
```

如果你的消息队列使用的是 Kafka 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.Kafka
```

如果你的消息队列使用的是 RabbitMQ 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
```

CAP 提供了 Sql Server, MySql, PostgreSQL 的扩展作为数据库存储：

```
// 按需选择安装你正在使用的数据库
PM> Install-Package DotNetCore.CAP.SqlServer
PM> Install-Package DotNetCore.CAP.MySql
PM> Install-Package DotNetCore.CAP.PostgreSql
```

### Configuration

首先配置CAP到 Startup.cs 文件中，如下：

```c#
public void ConfigureServices(IServiceCollection services)
{
    ......

    services.AddDbContext<AppDbContext>();

    services.AddCap(x =>
    {
        // 如果你的 SqlServer 使用的 EF 进行数据操作，你需要添加如下配置：
        // 注意: 你不需要再次配置 x.UseSqlServer(""")
        x.UseEntityFramework<AppDbContext>();
		
        // 如果你使用的Dapper，你需要添加如下配置：
        x.UseSqlServer("数据库连接字符串");

        // 如果你使用的 RabbitMQ 作为MQ，你需要添加如下配置：
        x.UseRabbitMQ("localhost");

        //如果你使用的 Kafka 作为MQ，你需要添加如下配置：
        x.UseKafka("localhost");
    });
}

public void Configure(IApplicationBuilder app)
{
    .....

    app.UseCap();
}

```

### 发布

在 Controller 中注入 `ICapPublisher` 然后使用 `ICapPublisher` 进行消息发送

```c#
public class PublishController : Controller
{
    [Route("~/checkAccountWithTrans")]
    public async Task<IActionResult> PublishMessageWithTransaction([FromServices]AppDbContext dbContext, [FromServices]ICapPublisher publisher)
    {
        using (var trans = dbContext.Database.BeginTransaction())
        {
            // 此处填写你的业务代码

            //如果你使用的是EF，CAP会自动发现当前环境中的事务，所以你不必显式传递事务参数。
            //由于本地事务, 当前数据库的业务操作和发布事件日志之间将实现原子性。
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
                // 此处填写你的业务代码，通常情况下，你可以将业务代码使用一个委托传递进来进行封装该区域代码。

                publisher.Publish("xxx.services.account.check", new Person { Name = "Foo", Age = 11 }, sqlTransaction);

                sqlTransaction.Commit();
            }
        }
        return Ok();
    }
}

```

### 订阅

**Action Method**

在 Action 上添加 CapSubscribeAttribute 来订阅相关消息。

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

如果你的订阅方法没有位于 Controller 中，则你订阅的类需要继承 `ICapSubscribe`：

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

然后在 Startup.cs 中的 `ConfigureServices()` 中注入你的  `ISubscriberService` 类

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<ISubscriberService,SubscriberService>();
}
```

### Dashboard

CAP 2.1+ 以上版本中提供了仪表盘（Dashboard）功能，你可以很方便的查看发出和接收到的消息。除此之外，你还可以在仪表盘中实时查看发送或者接收到的消息。 

在分布式环境中，仪表盘内置集成了 [Consul](http://consul.io) 作为节点的注册发现，同时实现了网关代理功能，你同样可以方便的查看本节点或者其他节点的数据，它就像你访问本地资源一样。

```c#
services.AddCap(x =>
{
    //...
    
    // 注册 Dashboard
    x.UseDashboard();
    
    // 注册节点到 Consul
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

## 贡献

贡献的最简单的方法之一就是是参与讨论和讨论问题（issue）。你也可以通过提交的 Pull Request 代码变更作出贡献。

### License

MIT
