# Apache Kafka

[Apache Kafka](https://kafka.apache.org/) is an open-source event streaming platform developed by LinkedIn and donated to the Apache Software Foundation. It is written in Scala and Java.

Kafka can be used in CAP as a message transporter. 

## Configuration

To use Kafka as a transporter, you need to install the following package from NuGet:

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
MainConfig | librdkafka configuration parameters | Dictionary<string, string> | See below
ConnectionPoolSize | connection pool size | int | 10
CustomHeadersBuilder | Custom subscribe headers |  Func<> |  N/A
RetriableErrorCodes | Retriable error codes when ConsumeException  | IList<ErrorCode> |  See code
TopicOptions | The configuraiton of NumPartitions and ReplicationFactor | KafkaTopicOptions |  -1

#### Kafka Main Configuration Options

If you need additional native Kafka configuration options, you can set them in the `MainConfig` configuration option:

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

`MainConfig` is a configuration dictionary. You can find a list of supported configuration options at the following link:

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)

To prevent CAP from creating topics automatically, disable topic auto creation:

```csharp
services.AddCap(capOptions =>
{
    capOptions.UseKafka(kafkaOption =>
    {
        kafkaOption.MainConfig.Add("allow.auto.create.topics", "false");
    });
});
```

#### Custom Headers Builder Options

When messages are sent from a heterogeneous system, CAP requires additional headers to be defined. By providing this parameter, you can set custom headers to ensure the subscriber works correctly.

You can find the description of heterogeneous system integration [here](../cap/messaging.md#heterogeneous-system-integration).

Sometimes, if you want to add additional context information from the broker to messages, you can also do this through this option. For example, you can add information such as offset or partition.

Example:

```C#
x.UseKafka(opt =>
{
    //...

    opt.CustomHeadersBuilder = (kafkaResult, sp) => new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("my.kafka.offset", kafkaResult.Offset.ToString()),
        new KeyValuePair<string, string>("my.kafka.partition", kafkaResult.Partition.ToString())
    };
});
```

Then you can retrieve the headers you added like this:

```C#
[CapSubscribe("sample.kafka.postgrsql")]
public void HeadersTest(DateTime value, [FromCap]CapHeader header)
{
    var offset = header["my.kafka.offset"];
    var partition = header["my.kafka.partition"];
}
```

