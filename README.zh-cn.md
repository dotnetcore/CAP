<p align="right">
<a href="https://github.com/dotnetcore/CAP/blob/master/README.md">English</a>
</p>

# CAP 　　　　　　　　　　　　　　　　　　　　　　
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/master.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![NuGet](https://img.shields.io/nuget/vpre/DotNetCore.CAP.svg)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP 是一个在分布式系统（SOA、MicroService）中实现最终一致性的库，它具有轻量级、易使用、高性能等特点。

## 预览（OverView）

CAP 是在一个 ASP.NET Core 项目中使用的库，当然他可以用于 ASP.NET Core On .NET Framework 中。

你可以把 CAP 看成是一个 EventBus，因为它具有 EventBus 的所有功能，并且 CAP 提供了更加简化的方式来处理 EventBus 中的发布和订阅。

CAP 具有消息持久化的功能，当你的服务进行重启或者宕机时它可以保证消息的可靠性。CAP提供了基于Microsoft DI 的 Publisher Service 服务，它可以和你的业务服务进行无缝结合，并且支持强一致性的事务。

这是CAP集在ASP.NET Core 微服务架构中的一个示意图：

![](http://images2015.cnblogs.com/blog/250417/201707/250417-20170705175827128-1203291469.png)

> 图中实线部分代表用户代码，虚线部分代表CAP内部实现。

## Getting Started

### NuGet 

你可以运行以下下命令在你的项目中安装 CAP。

如果你的消息队列使用的是 Kafka 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.Kafka -Pre
```

如果你的消息队列使用的是 RabbitMQ 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ -Pre
```

CAP 默认提供了 Entity Framwork 作为数据库存储：

```
PM> Install-Package DotNetCore.CAP.EntityFrameworkCore -Pre
```

### Configuration

首先配置CAP到 Startup.cs 文件中，如下：

```cs
public void ConfigureServices(IServiceCollection services)
{
	......

    services.AddDbContext<AppDbContext>();

    services.AddCap()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddKafka(x => x.Servers = "localhost:9092");
}

public void Configure(IApplicationBuilder app)
{
	.....

    app.UseCap();
}

```

### 发布

在 Controller 中注入 `ICapPublisher` 然后使用 `ICapPublisher` 进行消息发送

```cs
public class PublishController : Controller
{
	private readonly ICapPublisher _publisher;

	public PublishController(ICapPublisher publisher)
	{
		_publisher = publisher;
	}


	[Route("~/checkAccount")]
	public async Task<IActionResult> PublishMessage()
	{
		//指定发送的消息头和内容
		await _publisher.PublishAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

		return Ok();
	}
}

```

### 订阅

**Action Method**

在 Action 上添加 CapSubscribeAttribute 来订阅相关消息。

```cs
public class PublishController : Controller
{
	private readonly ICapPublisher _publisher;

	public PublishController(ICapPublisher publisher)
	{
		_publisher = publisher;
	}


	[NoAction]
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

```cs

namespace xxx.Service
{
	public interface ISubscriberService
	{
		public void CheckReceivedMessage(Person person);
	}


	public class SubscriberService: ISubscriberService, ICapSubscribe
	{
		[KafkaTopic("xxx.services.account.check")]
		public void CheckReceivedMessage(Person person)
		{
			
		}
	}
}

```

然后在 Startup.cs 中的 `ConfigureServices()` 中注入你的  `ISubscriberService` 类

```cs
public void ConfigureServices(IServiceCollection services)
{
	services.AddTransient<ISubscriberService,SubscriberService>();
}
```

## 贡献

贡献的最简单的方法之一就是是参与讨论和讨论问题（issue）。你也可以通过提交的 Pull Request 代码变更作出贡献。

### License

MIT
