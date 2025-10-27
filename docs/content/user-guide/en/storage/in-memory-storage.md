# In-Memory Storage

In-memory storage is commonly used in development and test environments. However, if you use memory-based storage, you lose the reliability guarantee of local transaction messages.

## Configuration

To use in-memory storage, you need to install the following package from NuGet:

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

CAP will clean successful messages from memory every 5 minutes.

## Publish with transaction

In-memory storage does **not support** transactional message publishing.
