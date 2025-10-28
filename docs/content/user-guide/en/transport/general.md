# Transports

Transports move data between different parts of the system – between message producers and message brokers, between message brokers and the entity database, and even between message brokers and external systems.

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

## Selecting a Transport

 Feature | RabbitMQ | Kafka | Azure Service Bus | In-Memory
:--   |   :--:    | :--: | :--:               | :--  :
**Use Case** | Reliable message transmission | Real-time data processing | Cloud integration | Testing and development
**Distributed**   | ✔   | ✔    | ✔ |❌
**Persistence** | ✔ | ✔ | ✔ | ❌
**Performance**  |  Medium  |  High | Medium | High


For more comparisons:

- [Azure Service Bus vs RabbitMQ](http://geekswithblogs.net/michaelstephenson/archive/2012/08/12/150399.aspx)
- [Kafka vs RabbitMQ](https://stackoverflow.com/questions/42151544/is-there-any-reason-to-use-rabbitmq-over-kafka)

Thanks to the community for contributing to CAP! The following transport extensions are community-supported:

* ActiveMQ ([@Lukas Zhang](https://github.com/lukazh/Lukaz.CAP.ActiveMQ)): https://github.com/lukazh

* RedisMQ ([@木木](https://github.com/difudotnet)): https://github.com/difudotnet/CAP.RedisMQ.Extensions

* ZeroMQ ([@maikebing](https://github.com/maikebing)): https://github.com/maikebing/CAP.Extensions

* MQTT ([@john jiang](https://github.com/jinzaz)): https://github.com/jinzaz/jinzaz.CAP.MQTT



