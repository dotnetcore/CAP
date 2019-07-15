# Apache Kafka®

[Apache Kafka®](https://kafka.apache.org/)  is an open-source stream-processing software platform developed by LinkedIn and donated to the Apache Software Foundation, written in Scala and Java.

CAP has supported Kafka® as message transporter. 

## Configuration

To use Kafka transporter, you need to install the following extensions from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.Kafka

```

Then you can add memory-based configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UseKafka(opt=>{
            //KafkaOptions
        });
        // x.UseXXX ...
    });
}

```

#### Kafka Options

The Kafka configuration parameters provided directly by the CAP are as follows:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Servers | Broker server address | string | 
ConnectionPoolSize | connection pool size | int | 10

#### Kafka MainConfig Options

If you need **more** native Kakfa related configuration items, you can set it with the `MainConfig` configuration option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseKafka(kafkaOption=>
    {
        // kafka options.
        // kafkaOptions.MainConfig.Add("", "");
    });
});
```

`MainConfig` is a configuration dictionary, you can find a list of supported configuration items through the following link.

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)
