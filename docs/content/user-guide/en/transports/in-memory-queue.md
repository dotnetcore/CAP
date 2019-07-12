# In-Memory Queue

In Memory Queue 为基于内存的消息队列，该扩展由 [社区](https://github.com/yang-xiaodong/Savorboard.CAP.InMemoryMessageQueue) 进行提供。

## 配置

要使用 In Memory Queue 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package Savorboard.CAP.InMemoryMessageQueue

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于内存的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseInMemoryMessageQueue();
        // x.UseXXX ...
    });
}

```