# General

CAP requires a storage medium with persistence capabilities to store event messages in databases or other NoSQL facilities. CAP uses this approach to protect against message loss in any environment or network issues. Reliability of messages is the cornerstone of distributed transactions, so messages must never be lost.

## Persistence

### Before Sent

Before the message enters the message queue, CAP persists the message in a local database table. This ensures that the message is not lost when the message queue is unavailable or a network error occurs.

To ensure the reliability of this mechanism, CAP uses the same database transactions as the business code to ensure that business operations and CAP messages are consistent during persistence. If any exception occurs during message persistence, the database will roll back.

### After Sent

After the message enters the message queue, CAP starts the persistence function of the message queue. Here's how CAP messages are persisted in RabbitMQ and Kafka.

For message persistence in RabbitMQ, CAP uses a consumer queue with message persistence, though exceptions may occur.

!!! info "Ready for Production?"
    By default, queues registered by CAP in RabbitMQ are persistent. For production use, we recommend that you start all consumers once to create persistent queues. This ensures all queues are created before messages are sent.

Since Kafka has built-in message persistence using files, it automatically ensures that messages are properly persisted without loss once they enter Kafka.

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

Table structure of **Lock** (Optional):

NAME | DESCRIPTION | TYPE
:---|:---|:---
Key | Lock Id | string
Instance | Acquired instance of lock | string
LastLockTime | Last acquired lock time | DateTime

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
