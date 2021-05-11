# Redis Streams

[Redis](https://redis.io/) is an open source (BSD licensed), in-memory data structure store, used as a database, cache, and message broker.

[Redis Stream](https://redis.io/topics/streams-intro) is a new data type introduced with Redis 5.0, which models a log data structure in a more abstract way with an append only data structure.

Redis Streams can be used in CAP as a message transporter. 

## Configuration

To use Redis Streams transporter, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.RedisStreams

```

Then you can add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(capOptions =>
    {
        capOptions.UseRedis(redisOptions=>{
            //redisOptions
        });
    });
}

```

#### Redis Streams Options

Redis configuration parameters provided directly by the CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Configuration | redis connection configuration (StackExchange.Redis) | ConfigurationOptions | ConfigurationOptions
StreamEntriesCount | number of entries returned from a stream while reading | uint | 10
ConnectionPoolSize  | number of connections pool | uint | 10

#### Redis ConfigurationOptions

If you need **more** native Redis related configuration options, you can set them in the `Configuration` option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseRedis(redisOptions=>
    {
        // redis options.
        redisOptions.Configuration.EndPoints.Add(IPAddress.Loopback, 0);
    });
});
```

`Configuration` is a StackExchange.Redis ConfigurationOptions , you can find more details through this [link](https://stackexchange.github.io/StackExchange.Redis/Configuration)
