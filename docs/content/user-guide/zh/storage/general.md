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

在 CAP 启动后，会向持久化介质中生成两个表，默认情况下名称为：`Cap.Published` `Cap.Received`。

### 存储格式

**Published** 表结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | Message Id | int
Version | Message Version | string
Name | Topic Name | string
Content | Json Content | string
Added | Added Time | DateTime
ExpiresAt | Expire time | DateTime
Retries | Retry times | int
StatusName | Status Name | string

**Received** 表结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id | Message Id | int
Version | Message Version | string
Name | Topic Name | string
Group | Group Name | string
Content | Json Content | string
Added | Added Time | DateTime
ExpiresAt | Expire time | DateTime
Retries | Retry times | int
StatusName | Status Name | string

### 包装器对象

CAP 在进行消息发送到时候，会对原始消息对象进行一个二次包装存储到 `Content` 字段中，以下为包装 Content 的 Message 对象数据结构：

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id	| CAP生成的消息编号	| string
Timestamp |	消息创建时间 |	string
Content |	内容 |	string
CallbackName |	回调的订阅者名称 | string

其中 Id 字段，CAP 采用的 MongoDB 中的 ObjectId 分布式Id生成算法生成。

## 社区支持的持久化

感谢社区对CAP的支持，以下是社区支持的持久化的实现

* SQLite ([@colinin](https://github.com/colinin)) ：https://github.com/colinin/DotNetCore.CAP.Sqlite   

* LiteDB ([@maikebing](https://github.com/maikebing)) ：https://github.com/maikebing/CAP.Extensions

* SQLite & Oracle ([@cocosip](https://github.com/cocosip)) ：https://github.com/cocosip/CAP-Extensions   

* SmartSql ([@xiangxiren](https://github.com/xiangxiren)) ：https://github.com/xiangxiren/SmartSql.CAP   
