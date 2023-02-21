# Apache Kafka®

[Apache Kafka®](https://kafka.apache.org/)  is an open-source stream-processing software platform developed by LinkedIn and donated to the Apache Software Foundation, written in Scala and Java.

Kafka® can be used in CAP as a message transporter. 

## Configuration

To use Kafka transporter, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.Kafka

```

Then you can add configuration items to the `ConfigureServices` method of `Startup.cs`.

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

The Kafka configuration parameters provided directly by the CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Servers | Broker server address | string | 
ConnectionPoolSize | connection pool size | int | 10
CustomHeaders | Custom subscribe headers |  Func<> |  N/A

#### CustomHeaders Options

When the message sent from a heterogeneous system, because of the CAP needs to define additional headers, so an exception will occur at this time. By providing this parameter to set the custom headersn to make the subscriber works.

You can find the description of heterogeneous system integration [here](../../cap/messaging#heterogeneous-system-integration).

Sometimes, if you want to get additional context information from Broker, you can also add it through this option. For example, add information such as Offset or Partition.

Example：

```C#
x.UseKafka(opt =>
{
    //...

    opt.CustomHeaders = kafkaResult => new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("my.kafka.offset", kafkaResult.Offset.ToString()),
        new KeyValuePair<string, string>("my.kafka.partition", kafkaResult.Partition.ToString())
    };
});
```

Then you can get the header you added by this way:

```C#
[CapSubscribe("sample.kafka.postgrsql")]
public void HeadersTest(DateTime value, [FromCap]CapHeader header)
{
    var offset = header["my.kafka.offset"];
    var partition = header["my.kafka.partition"];
}
```


#### Kafka MainConfig Options

If you need **more** native Kakfa related configuration options, you can set them in the `MainConfig` configuration option:

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

`MainConfig` is a configuration dictionary, you can find a list of supported configuration options through the following link.

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)
