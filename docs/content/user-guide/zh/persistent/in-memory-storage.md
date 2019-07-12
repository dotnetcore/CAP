# In-Memory Storage

内存消息的持久化存储常用于开发和测试环境，如果使用基于内存的存储则你会失去本地事务消息可靠性保证。

## 配置

如果要使用内存存储，你需要从 NuGet 安装以下扩展包：

```
Install-Package DotNetCore.CAP.InMemoryStorage
```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于内存的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseInMemoryStorage();
        // x.UseXXX ...
    });
}

```

内存中的发送成功消息，CAP 将会每 5分钟 进行一次清理。


## Publish with transaction

In-Memory 存储 **不支持** 事务方式发送消息。

