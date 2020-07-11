# In-Memory Queue

In Memory Queue is a memory-based message queue provided by [Community](https://github.com/yang-xiaodong/Savorboard.CAP.InMemoryMessageQueue).

## Configuration

To use In Memory Queue as a message transporter, you need to install the following package from NuGet:

```powershell
PM> Install-Package Savorboard.CAP.InMemoryMessageQueue

```

Next, add configuration options to the `ConfigureServices` method of `Startup.cs`:

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