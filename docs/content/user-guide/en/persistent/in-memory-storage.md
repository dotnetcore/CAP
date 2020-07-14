# In-Memory Storage

In-memory storage is often used in development and test environments, and if you use memory-based storage you lose the reliability of local transaction messages.

## Configuration

To use in-memory storage, you need to install following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.InMemoryStorage
```

Next, add configuration items to the `ConfigureServices` method of `Startup.cs`.

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

 CAP will clean **every 5 minutes** Successful messages in memory.

## Publish with transaction

In-Memory Storage **does not support** Transaction mode to send messages.
