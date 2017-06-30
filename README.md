# CAP
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/master.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP is a library to achieve eventually consistent in distributed architectures system like SOA,MicroService. It is lightweight,easy to use and efficiently

## OverView

CAP is a library that used in an ASP.NET Core project,Of Course you can ues it in ASP.NET Core with .NET Framework 

You can think of CAP as an EventBus because it has all the features of EventBus, and CAP provides a easier way to handle the publishing and subscribing in EventBus.

CAP has the function of message persistence, and it guarantees the reliability of the message when your service is restarted or down. CAP provides a Microsoft Pro-based Producer Service service that integrates seamlessly with your business services and supports strong consistency transactions.

This is a diagram of the CAP work in the ASP.NET Core MicroService architecture:

![](http://images2015.cnblogs.com/blog/250417/201706/250417-20170630143600289-1065294295.png)

> The solid line in the figure represents the user code, and the dotted line represents the internal implementation of the CAP.

## Getting Started

### NuGet (Coming soon)

你可以运行以下下命令在你的项目中安装 CAP。

如果你的消息队列使用的是 Kafka 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.Kafka
```

如果你的消息队列使用的是 RabbitMQ 的话，你可以：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
```

CAP 默认提供了 Entity Framwork 作为数据库存储：

```
PM> Install-Package DotNetCore.CAP.EntityFrameworkCore
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
            .AddKafka(x => x.Servers = "localhost:9453");
}

public void Configure(IApplicationBuilder app)
{
	.....

    app.UseCap();
}

```

### 发布

在 Controller 中注入 `ICapProducerService` 然后使用 `ICapProducerService` 进行消息发送

```cs
public class PublishController : Controller
{
	private readonly ICapProducerService _producer;

	public PublishController(ICapProducerService producer)
	{
		_producer = producer;
	}


	[Route("~/checkAccount")]
	public async Task<IActionResult> PublishMessage()
	{
		//指定发送的消息头和内容
		await _producer.SendAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

		return Ok();
	}
}

```

### 订阅

**Action Method**

在Action上添加 Attribute 来订阅相关消息。

如果你使用的是 Kafak 则使用 `[KafkaTopic()]`, 如果是 RabbitMQ 则使用 `[RabbitMQTopic()]`

```cs
public class PublishController : Controller
{
	private readonly ICapProducerService _producer;

	public PublishController(ICapProducerService producer)
	{
		_producer = producer;
	}


	[NoAction]
	[KafkaTopic("xxx.services.account.check")]
	public async Task CheckReceivedMessage(Person person)
	{
		Console.WriteLine(person.Name);
		Console.WriteLine(person.Age);     
		return Task.CompletedTask;
	}
}

```

**Service Method**

如果你的订阅方法没有位于 Controller 中，则你订阅的类需要继承 `IConsumerService`：

```cs

namespace xxx.Service
{
	public interface ISubscriberService
	{
		public void CheckReceivedMessage(Person person);
	}


	public class SubscriberService: ISubscriberService, IConsumerService
	{
		[KafkaTopic("xxx.services.account.check")]
		public void CheckReceivedMessage(Person person)
		{
			
		}
	}
}

```

then inject your  `ISubscriberService`  class in Startup.cs 

```cs
public void ConfigureServices(IServiceCollection services)
{
	services.AddTransient<ISubscriberService,SubscriberService>();
}
```

## Contribute

One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

### License

MIT
