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
ConnectionPoolSize | 用户名 | int | 10


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
