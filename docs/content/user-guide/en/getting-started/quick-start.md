# Quick Start

Learn how to build a microservices event bus architecture using CAP. This offers advantages over directly integrating message queues and provides many out-of-the-box features.

## Installation

```powershell
PM> Install-Package DotNetCore.CAP
```

## Integrated in ASP.NET Core

For a quick start, we use memory-based event storage and message transport.

```powershell
PM> Install-Package DotNetCore.CAP.InMemoryStorage
PM> Install-Package Savorboard.CAP.InMemoryMessageQueue
```

In `Startup.cs`, add the following configuration:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(x =>
    {
        x.UseInMemoryStorage();
        x.UseInMemoryMessageQueue();
    });
}
```

## Publishing a Message

```c#
public class PublishController : Controller
{
    [Route("~/send")]
    public IActionResult SendMessage([FromServices] ICapPublisher capBus)
    {
        capBus.Publish("test.show.time", DateTime.Now);

        return Ok();
    }
}
```

### Publishing a Delayed Message

```c#
public class PublishController : Controller
{
    [Route("~/send/delay")]
    public IActionResult SendDelayMessage([FromServices] ICapPublisher capBus)
    {
        capBus.PublishDelay(TimeSpan.FromSeconds(100), "test.show.time", DateTime.Now);

        return Ok();
    }
}
```

### Publishing with Extra Headers

```c#
var header = new Dictionary<string, string>()
{
    ["my.header.first"] = "first",
    ["my.header.second"] = "second"
};

capBus.Publish("test.show.time", DateTime.Now, header);
```

## Processing a Message

```C#
public class ConsumerController : Controller
{
    [NonAction]
    [CapSubscribe("test.show.time")]
    public void ReceiveMessage(DateTime time)
    {
        Console.WriteLine("message time is: " + time);
    }
}
```

### Processing with Extra Headers

```c#
[CapSubscribe("test.show.time")]
public void ReceiveMessage(DateTime time, [FromCap] CapHeader header)
{
    Console.WriteLine("message time is: " + time);
    Console.WriteLine("message first header: " + header["my.header.first"]);
    Console.WriteLine("message second header: " + header["my.header.second"]);
}
```

## Summary

One of the most powerful advantages of asynchronous messaging over direct message queue integration is reliability. Failures in one part of the system don't propagate or cause the entire system to crash. Messages are stored inside CAP to ensure message reliability, and strategies such as retries are used to achieve eventual consistency of data between services.