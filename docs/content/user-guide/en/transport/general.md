# Transports

Transports move data from one place to another – between acquisition programs and pipelines, between pipelines and the entity database, and even between pipelines and external systems.

## Supported transports

CAP supports several transport methods:

* [RabbitMQ](rabbitmq.md)
* [Kafka](kafka.md)
* [Azure Service Bus](azure-service-bus.md)
* [Amazon SQS](aws-sqs.md)
* [NATS](nats.md)
* [In-Memory Queue](in-memory-queue.md)
* [Redis Streams](redis-streams.md)
* [Apache Pulsar](pulsar.md)

## How to select a transport

 🏳‍🌈  | RabbitMQ | Kafka | Azure Service Bus | In-Memory
:--   |   :--:    | :--: | :--:               | :--  :
**Positioning** | Reliable message transmission | Real time data processing | Cloud | In-Memory, testing
**Distributed**   | ✔   | ✔    | ✔ |❌
**Persistence** | ✔ | ✔ | ✔ | ❌
**Performance**  |  Medium  |  High | Medium | High


> `Azure Service Bus` vs `RabbitMQ` :  
> http://geekswithblogs.net/michaelstephenson/archive/2012/08/12/150399.aspx

>`Kafka` vs `RabbitMQ` :   
> https://stackoverflow.com/questions/42151544/is-there-any-reason-to-use-rabbitmq-over-kafka

## Community-supported transport extensions

Thanks to the community for supporting CAP, the following is the implementation of community-supported transport

* ActiveMQ (@[Lukas Zhang](https://github.com/lukazh/Lukaz.CAP.ActiveMQ)): https://github.com/lukazh

* RedisMQ  ([@木木](https://github.com/difudotnet)) https://github.com/difudotnet/CAP.RedisMQ.Extensions

* ZeroMQ ([@maikebing](https://github.com/maikebing))： https://github.com/maikebing/CAP.Extensions



