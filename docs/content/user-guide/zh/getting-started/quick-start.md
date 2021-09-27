# 快速开始

了解如何使用 CAP 构建微服务事件总线架构，它比直接集成消息队列提供了哪些优势，它提供了哪些开箱即用的功能。

## 安装

```powershell
PM> Install-Package DotNetCore.CAP
```

## 在 Asp.Net Core 中集成

以便于快速启动，我们使用基于内存的事件存储和消息队列。

```powershell
PM> Install-Package DotNetCore.CAP.InMemoryStorage
PM> Install-Package Savorboard.CAP.InMemoryMessageQueue
```

在 `Startup.cs` 中，添加以下配置：

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

## 发送消息

```c#
public class PublishController : Controller
{
    [Route("~/send")]
    public IActionResult SendMessage([FromServices]ICapPublisher capBus)
    {
        capBus.Publish("test.show.time", DateTime.Now);

        return Ok();
    }
}
```

## 处理消息

```C#
public class ConsumerController : Controller
{
    [NonAction]
    [CapSubscribe("test.show.time")]
    public void ReceiveMessage(DateTime time)
    {
        Console.WriteLine("message time is:" + time);
    }
}
```

## 带有头信息的消息

### 发送包含头信息的消息

```c#
var header = new Dictionary<string, string>()
{
    ["my.header.first"] = "first",
    ["my.header.second"] = "second"
};

capBus.Publish("test.show.time", DateTime.Now, header);

```

### 处理包含头信息的消息

```c#
[CapSubscribe("test.show.time")]
public void ReceiveMessage(DateTime time, [FromCap]CapHeader header)
{
    Console.WriteLine("message time is:" + time);
    Console.WriteLine("message firset header :" + header["my.header.first"]);
    Console.WriteLine("message second header :" + header["my.header.second"]);
}

```


## 摘要

相对于直接集成消息队列，异步消息传递最强大的优势之一是可靠性，系统的一个部分中的故障不会传播，也不会导致整个系统崩溃。 在 CAP 内部会将消息进行存储，以保证消息的可靠性，并配合重试等策略以达到各个服务之间的数据最终一致性。