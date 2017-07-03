# CAP
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/master.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP is a library to achieve eventually consistent in distributed architectures system like SOA,MicroService. 	It is lightweight,easy to use and efficiently.

## OverView

CAP is a library that used in an ASP.NET Core project,Of Course you can ues it in ASP.NET Core with .NET Framework.

You can think of CAP as an EventBus because it has all the features of EventBus, and CAP provides a easier way to handle the publishing and subscribing than EventBus.

CAP has the function of Message Presistence, and it makes messages reliability when your service is restarted or down. CAP provides a Producer Service based on Microsoft DI that integrates seamlessly with your business services and supports strong consistency transactions.

This is a diagram of the CAP working in the ASP.NET Core MicroService architecture:

![](http://images2015.cnblogs.com/blog/250417/201706/250417-20170630143600289-1065294295.png)

> The solid line in the figure represents the user code, and the dotted line represents the internal implementation of the CAP.

## Getting Started

### NuGet (Coming soon)

You can run the following command to install the CAP in your project.

If your Message Queue is using Kafka, you can：

```
PM> Install-Package DotNetCore.CAP.Kafka
```

or RabbitMQ：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
```

CAP provides EntityFramework as default database store extension ：

```
PM> Install-Package DotNetCore.CAP.EntityFrameworkCore
```

### Configuration

First,You need to config CAP in your Startup.cs：

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

### Publish

Inject `ICapProducerService` in your Controller, then use the `ICapProducerService` to send message

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
		//Specifies the message header and content to be sent
		await _producer.SendAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

		return Ok();
	}
}

```

### Subscribe

**Action Method**

Add Attribute on Action to subscribe message:

If you are using Kafka the Attribute is `[KafkaTopic()]`, and RabbitMQ is  `[RabbitMQTopic()]`

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

If your subscribe method is not in the Controller,then your subscribe class need to Inheritance `IConsumerService`: 

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

Then inject your  `ISubscriberService`  class in Startup.cs 

```cs
public void ConfigureServices(IServiceCollection services)
{
	services.AddTransient<ISubscriberService,SubscriberService>();
}
```

## Contribute

One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

### License

[MIT](https://github.com/dotnetcore/CAP/blob/master/LICENSE.txt)
