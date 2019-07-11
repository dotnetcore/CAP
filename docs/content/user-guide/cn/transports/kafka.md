# Kafka


### Kafka Options

CAP 采用的是针对 CapOptions 进行扩展来实现 Kafka 的配置功能，所以针对 Kafka 的配置用法如下：

```cs
services.AddCap(capOptions => {
    capOptions.UseKafka(kafkaOption=>{
        // kafka options.
        // kafkaOptions.MainConfig.Add("", "");
    });
});
```

`KafkaOptions` 提供了有关 Kafka 相关的配置，由于Kafka的配置比较多，所以此处使用的是提供的 MainConfig 字典来支持进行自定义配置，你可以查看这里来获取对配置项的支持信息。

[https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md)

