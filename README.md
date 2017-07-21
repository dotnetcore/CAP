<p align="right">
    <a href="https://github.com/dotnetcore/CAP/blob/develop/README.zh-cn.md">中文</a>
</p>

# CAP 　　　　　　　　　　　　　　　　　　　　　　
[![Travis branch](https://img.shields.io/travis/dotnetcore/CAP/develop.svg?label=travis-ci)](https://travis-ci.org/dotnetcore/CAP)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4mpe0tbu7n126vyw?svg=true)](https://ci.appveyor.com/project/yuleyule66/cap)
[![NuGet](https://img.shields.io/nuget/v/DotNetCore.CAP.svg)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![NuGet Preview](https://img.shields.io/nuget/vpre/DotNetCore.CAP.svg?label=nuget-pre)](https://www.nuget.org/packages/DotNetCore.CAP/)
[![Member project of .NET China Foundation](https://img.shields.io/badge/member_project_of-.NET_CHINA-red.svg?style=flat&colorB=9E20C8)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)

CAP is a .Net Standard library to achieve eventually consistent in distributed architectures system like SOA,MicroService. 	It is lightweight,easy to use and efficiently.

## OverView

CAP is a library that used in an ASP.NET Core project, Of Course you can ues it in ASP.NET Core with .NET Framework.

You can think of CAP as an EventBus because it has all the features of EventBus, and CAP provides a easier way to handle the publishing and subscribing than EventBus.

CAP has the function of Message Presistence, and it makes messages reliability when your service is restarted or down. CAP provides a Publish Service based on Microsoft DI that integrates seamlessly with your business services and supports strong consistency transactions.

This is a diagram of the CAP working in the ASP.NET Core MicroService architecture:

![](http://images2015.cnblogs.com/blog/250417/201707/250417-20170705175827128-1203291469.png)

> The solid line in the figure represents the user code, and the dotted line represents the internal implementation of the CAP.

## Getting Started

### NuGet

You can run the following command to install the CAP in your project.

```
PM> Install-Package DotNetCore.CAP
```

If your Message Queue is using Kafka, you can：

```
PM> Install-Package DotNetCore.CAP.Kafka
```

If your Message Queue is using RabbitMQ, you can：

```
PM> Install-Package DotNetCore.CAP.RabbitMQ
```

CAP provides EntityFramework as default database store extension (The MySQL version is under development)：

```
PM> Install-Package DotNetCore.CAP.SqlServer
```

### Configuration

First,You need to config CAP in your Startup.cs：

```cs
public void ConfigureServices(IServiceCollection services)
{
	......

	services.AddDbContext<AppDbContext>();

	services.AddCap(x =>
	{
		// If your SqlServer is using EF for data operations, you need to add the following configuration：
		// Notice: You don't need to config x.UseSqlServer(""") again!
		x.UseEntityFramework<AppDbContext>();
		
		// If you are using Dapper,you need to add the config：
		x.UseSqlServer("Your ConnectionStrings");

		// If your Message Queue is using RabbitMQ you need to add the config：
		x.UseRabbitMQ("localhost");

		// If your Message Queue is using Kafka you need to add the config：
		x.UseKafka("localhost");
	});
}

public void Configure(IApplicationBuilder app)
{
	.....

    app.UseCap();
}

```

### Publish

Inject `ICapPublisher` in your Controller, then use the `ICapPublisher` to send message

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
		// Specifies the message header and content to be sent
		await _publisher.PublishAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

		return Ok();
	}

	[Route("~/checkAccountWithTrans")]
	public async Task<IActionResult> PublishMessageWithTransaction([FromServices]AppDbContext dbContext)
	{
		 using (var trans = dbContext.Database.BeginTransaction())
		 {
			await _publisher.PublishAsync("xxx.services.account.check", new Person { Name = "Foo", Age = 11 });

			trans.Commit();
		 }
		return Ok();
	}
}

```

### Subscribe

**Action Method**

Add the Attribute `[CapSubscribe()]` on Action to subscribe message:

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

If your subscribe method is not in the Controller,then your subscribe class need to Inheritance `ICapSubscribe`: 

```cs

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
