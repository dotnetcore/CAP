# General

CAP needs to use storage media with persistence capabilities to store event messages in databases or other NoSql facilities. CAP uses this approach to deal with loss of messages in all environments or network anomalies. Reliability of messages is the cornerstone of distributed transactions, so messages cannot be lost under any circumstances.

## Persistence

### Before sent

Before message enters the message queue, CAP uses the local database table to persist the message, which ensures that the message is not lost when the message queue is abnormal or a network error occurs.

To ensure the reliability of this mechanism, CAP uses the same database transactions as the business code to ensure that business operations and CAP messages are consistent in the persistence process. That is to say, in the process of message persistence, the database will be rolled back when any one of the exceptions occurs.

###  After sent

After the message enters the message queue, CAP will start the persistence function of the message queue. We need to explain how CAP message is persisted in RabbitMQ and Kafka.

For message persistence in RabbitMQ, CAP uses a consumer queue with message persistence, but there may be exceptions here.

!!! info "Ready for production?"
    By default, queues registered by CAP in RabbitMQ are persistent. When used in a production environment, we recommend that you start all consumers once to create the queues with persistence, which ensures that all queues are created before the message is sent.

Since Kafka is born with message persistence using files, Kafka will ensure that messages are properly persisted without loss after the message enters Kafka.

## Storage

### Supported storages

CAP supports the following types of transaction-enabled databases for storage:

* [SQL Server](sqlserver.md)
* [MySQL](mysql.md)
* [PostgreSql](postgresql.md)
* [MongoDB](mongodb.md)
* [In-Memory Storage](in-memory-storage.md)

After CAP is started, two tables are generated in used storage, by default the name is `Cap.Published` and `Cap.Received`.

### Storage Data Structure

Table structure of **Published** :

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
 
Table structure of **Received** :

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
 
### Wapper Object

When CAP sends a message, it will store original message object in a second package in the `Content` field. 

The following is the **Wapper Object** data structure of Content field.

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id	| Message Id	| string
Timestamp |	Message created time |	string
Content |	Message content |	string
CallbackName |	Consumer callback topic name | string

The `Id` field is generate using the mongo [objectid algorithm](https://www.mongodb.com/blog/post/generating-globally-unique-identifiers-for-use-with-mongodb).


## Community-supported extensions

Thanks to the community for supporting CAP, the following is the implementation of community-supported storage

* SQLite ([@colinin](https://github.com/colinin)) ：https://github.com/colinin/DotNetCore.CAP.Sqlite   

* LiteDB ([@maikebing](https://github.com/maikebing)) ：https://github.com/maikebing/CAP.Extensions

* SQLite & Oracle ([@cocosip](https://github.com/cocosip)) ：https://github.com/cocosip/CAP-Extensions   
