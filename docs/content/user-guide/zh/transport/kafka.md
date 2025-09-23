# Apache Kafka®

[Apache Kafka®](https://kafka.apache.org/) 是一个开源流处理软件平台，由 LinkedIn 开发并捐赠给 Apache Software Foundation，用 Scala 和 Java 编写。
 
CAP 支持使用 Apache Kafka® 作为消息传输器。

## Configuration

要使用 Kafka 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.Kafka

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于 Kafka 的配置项。

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

CAP 直接对外提供的 Kafka 配置参数如下：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Servers | Broker 地址 | string | 
MainConfig | librdkafka 的配置参数 | Dictionary<string, string> | 见下
ConnectionPoolSize | 用户名 | int | 10
CustomHeadersBuilder | 设置自定义头 | Function | 见下
RetriableErrorCodes |  ConsumeException 异常时的重试错误码集合  | IList<ErrorCode> |  见代码
TopicOptions | 配置 NumPartitions 和 ReplicationFactor | KafkaTopicOptions |  -1

#### Kafka MainConfig Options

如果你需要 **更多** 原生 Kakfa 相关的配置项，可以通过 `MainConfig` 配置项进行设定：

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

MainConfig 为配置字典，你可以通过以下链接找到其支持的配置项列表。

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)

要禁止 CAP 自动创建主题，可以关闭该功能：

```csharp
services.AddCap(capOptions =>
{
    capOptions.UseKafka(kafkaOption =>
    {
        kafkaOption.MainConfig.Add("allow.auto.create.topics", "false");
    });
});
```

#### CustomHeadersBuilder Options

有关 `CustomHeadersBuilder` 的说明：

如果你想在消费消息的时候，通过从 `CapHeader` 获取 Kafka 中例如 Offset 或者 Partition 等信息，你可以通过自定义此函数来实现这一点。

例如以下代码为你展示了如何进行设置额外的参数到 `CapHeader` 中:

```C#
x.UseKafka(opt =>
{
    //...

    opt.CustomHeadersBuilder = (kafkaResult,sp) => new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("my.kafka.offset", kafkaResult.Offset.ToString()),
        new KeyValuePair<string, string>("my.kafka.partition", kafkaResult.Partition.ToString())
    };
});
```

然后你可以通过这个方式来获取你添加的头信息:

```C#
[CapSubscribe("sample.kafka.postgrsql")]
public void HeadersTest(DateTime value, [FromCap]CapHeader header)
{
    var offset = header["my.kafka.offset"];
    var partition = header["my.kafka.partition"];
}
```
