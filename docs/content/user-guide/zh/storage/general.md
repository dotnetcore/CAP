# 基本

CAP 需要使用具有持久化功能的存储介质来存储事件消息，例如通过数据库或者其他NoSql设施。CAP 使用这种方式来应对一切环境或者网络异常导致消息丢失的情况，消息的可靠性是分布式事务的基石，所以在任何情况下消息都不能丢失。

## 持久化

### 发送前

在消息进入到消息队列之前，CAP使用本地数据库表对消息进行持久化，这样可以保证当消息队列出现异常或者网络错误时候消息是没有丢失的。

为了保证这种机制的可靠性，CAP使用和业务代码相同的数据库事务来保证业务操作和CAP的消息在持久化的过程中是强一致的。也就是说在进行消息持久化的过程中，任何一方发生异常情况数据库都会进行回滚操作。

###  发送后

消息进入到消息队列之后，CAP会启动消息队列的持久化功能，我们需要说明一下在 RabbitMQ 和 Kafka 中CAP的消息是如何持久化的。

针对于 RabbitMQ 中的消息持久化，CAP 使用的是具有消息持久化功能的消费者队列，但是这里面可能有例外情况，参加 2.2.1 章节。

由于 Kafka 天生设计的就是使用文件进行的消息持久化，在所以在消息进入到Kafka之后，Kafka会保证消息能够正确被持久化而不丢失。

## 消息存储

### 支持的存储

CAP 支持以下几种具有事务支持的数据库做为存储：

* [SQL Server](sqlserver.md)
* [MySQL](mysql.md)
* [PostgreSql](postgresql.md)
* [MongoDB](mongodb.md)
* [In-Memory Storage](in-memory-storage.md)

CAP 启动后会初始化已发布消息和已接收消息的存储。物理名称和结构因存储提供程序而异，并可通过 `TableNamePrefix` 或集合名称等提供程序选项自定义。只有启用 `UseStorageLock` 时才会创建存储锁表。

### 消息存储字段

关系型存储包含以下逻辑字段。具体数据库类型、索引、命名和提供程序专属字段因实现而异；MongoDB 保存对应的文档。

字段 | 说明
:---|:---
Id | CAP 消息标识符（内置关系型存储中为 64 位整数）。
Version | CAP 配置的消息版本。
Name | 消息主题或名称。
Content | 序列化后的 CAP 消息，包含标头和负载。
Retries | 重试次数。
Added | 消息存储时间。
ExpiresAt | 可选的过期时间。
StatusName | 当前消息状态。
Group | 消费者组，仅接收消息包含此字段。

存储的 `Content` 值是 CAP 消息对象的序列化结果，其中包含消息标头和负载。它不是额外嵌套的包装对象，消息编号也不是 MongoDB ObjectId。有关各提供程序的实际结构和迁移，请查看相应的存储文档。

## 社区支持的持久化

感谢社区对CAP的支持，以下是社区支持的持久化的实现

* SQLite ([@colinin](https://github.com/colinin)) ：https://github.com/colinin/DotNetCore.CAP.Sqlite   

* LiteDB ([@maikebing](https://github.com/maikebing)) ：https://github.com/maikebing/CAP.Extensions

* SQLite & Oracle ([@cocosip](https://github.com/cocosip)) ：https://github.com/cocosip/CAP-Extensions   

* SmartSql ([@xiangxiren](https://github.com/xiangxiren)) ：https://github.com/xiangxiren/SmartSql.CAP

* DM（达梦数据库）([@findersky](https://github.com/findersky)) ：https://github.com/findersky/CAP

* GaussDB ([@JASONPANZ](https://github.com/JASONPANZ)) ：https://github.com/JASONPANZ/CAP