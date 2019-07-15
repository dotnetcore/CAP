# In-Memory Storage

Persistent storage of memory messages is often used in development and test environments, and if you use memory-based storage you lose the reliability of local transaction messages.

## Configuration

To use in-memory storage, you need to install the following extensions from NuGet:

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

The successful message in memory, the CAP will be cleaned **every 5 minutes**.

## Publish with transaction

In-Memory Storage **Not supported** Transaction mode to send messages.
