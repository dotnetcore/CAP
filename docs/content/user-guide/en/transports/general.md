# Transports

Transports move data from one place to another – between acquisition programs and pipelines, between pipelines and the entity database, and even between pipelines and external systems.

## Supported transports

CAP supports several transport methods:

* [RabbitMQ](rabbitmq.md)
* [Kafka](kafka.md)
* [Azure Service Bus](azure-service-bus.md)
* [In-Memory Queue](in-memory-queue.md)

## How to select a transport

 🏳‍🌈  | RabbitMQ | Kafka | Azure Service Bus | In-Memory
:--   |   :--:    | :--: | :--:               | :--  :
**定位** | 可靠消息传输 | 实时数据处理 | 云 | 内存型，测试
**分布式**   | ✔   | ✔    | ✔ |❌
**持久化** | ✔ | ✔ | ✔ | ❌
**性能**  |  Medium  |  High | Medium | High


> `Azure Service Bus` vs `RabbitMQ` :  
> http://geekswithblogs.net/michaelstephenson/archive/2012/08/12/150399.aspx

>`Kafka` vs `RabbitMQ` :   
> https://stackoverflow.com/questions/42151544/is-there-any-reason-to-use-rabbitmq-over-kafka

