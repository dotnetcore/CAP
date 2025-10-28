# Redis Streams

[Redis](https://redis.io/) is an open-source, BSD-licensed, in-memory data structure store used as a database, cache, and message broker.

[Redis Streams](https://redis.io/topics/streams-intro) is a new data type introduced in Redis 5.0 that models a log data structure in an abstract way using an append-only data structure.

Redis Streams can be used in CAP as a message transporter. 

## Configuration

To use Redis Streams as a transporter, you need to install the following package from NuGet:

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

Redis Streams configuration parameters provided by CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Configuration | redis connection configuration (StackExchange.Redis) | ConfigurationOptions | ConfigurationOptions
StreamEntriesCount | number of entries returned from a stream while reading | uint | 10
ConnectionPoolSize  | number of connections pool | uint | 10
OnConsumeError      | callback function that will be invoked when an error occurred during message consumption. | Func<ConsumeErrorContext, Task> | null
#### Redis Configuration Options

If you need additional native Redis configuration options, you can set them in the `Configuration` option:

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

`Configuration` is a StackExchange.Redis `ConfigurationOptions`. You can find more details at this [link](https://stackexchange.github.io/StackExchange.Redis/Configuration).

### Streams Cleanup Notes

Since Redis Streams does not support deleting all messages that have been acknowledged by all groups (see [Redis issue](https://github.com/redis/redis/issues/5774)), you should consider using a script to periodically delete old messages.
