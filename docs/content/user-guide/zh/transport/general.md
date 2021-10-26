# 运输器

通过运输将数据从一个地方移动到另一个地方-在采集程序和管道之间，管道与实体数据库之间，甚至在管道与外部系统之间。

## 支持的运输器

CAP 支持以下几种运输方式：

* [RabbitMQ](rabbitmq.md)
* [Kafka](kafka.md)
* [Azure Service Bus](azure-service-bus.md)
* [Amazon SQS](aws-sqs.md)
* [NATS](nats.md)
* [In-Memory Queue](in-memory-queue.md)
* [Redis Streams](redis-streams.md)
* [Apache Pulsar](pulsar.md)

## 怎么选择运输器

 🏳‍🌈  | RabbitMQ | Kafka | Azure Service Bus | In-Memory
:--   |   :--:    | :--: | :--:               | :--:
**定位** | 可靠消息传输 | 实时数据处理 | 云 | 内存型，测试
**分布式**   | ✔   | ✔    | ✔ |❌
**持久化** | ✔ | ✔ | ✔ | ❌
**性能**  |  Medium  |  High | Medium | High


> `Azure Service Bus` vs `RabbitMQ` :  
> http://geekswithblogs.net/michaelstephenson/archive/2012/08/12/150399.aspx

>`Kafka` vs `RabbitMQ` :   
> https://stackoverflow.com/questions/42151544/is-there-any-reason-to-use-rabbitmq-over-kafka

## 社区支持的运输器

感谢社区对CAP的支持，以下是社区支持的运输器实现

* ActiveMQ (@[Lukas Zhang](https://github.com/lukazh/Lukaz.CAP.ActiveMQ)): https://github.com/lukazh

* RedisMQ ([@木木](https://github.com/difudotnet)): https://github.com/difudotnet/CAP.RedisMQ.Extensions

* ZeroMQ ([@maikebing](https://github.com/maikebing)): https://github.com/maikebing/CAP.Extensions/tree/master/src/DotNetCore.CAP.ZeroMQ