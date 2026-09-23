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

After CAP starts, it initializes published-message and received-message storage. Physical names and schemas vary by provider and can be customized with provider options such as `TableNamePrefix` or collection-name settings. The storage-lock table is created only when `UseStorageLock` is enabled.

### Stored message data

Relational providers store the following logical fields. Exact database types, indexes, naming, and provider-specific fields vary by implementation. MongoDB stores corresponding documents.

Field | Description
:---|:---
Id | CAP message identifier (stored as a 64-bit integer by the built-in relational providers).
Version | Message version configured by CAP.
Name | Message topic or name.
Content | Serialized CAP message, including headers and payload.
Retries | Retry count.
Added | Time the message was stored.
ExpiresAt | Optional expiration time.
StatusName | Current message state.
Group | Consumer group; present on received messages.

The storage `Content` value serializes CAP's message object, which contains message headers and the payload value. It is not an additional wrapper with a MongoDB ObjectId. For provider-specific schemas and migrations, consult the relevant storage implementation documentation.

## Community-supported extensions

Thanks to the community for supporting CAP, the following is the implementation of community-supported storage

* SQLite ([@colinin](https://github.com/colinin)) ：https://github.com/colinin/DotNetCore.CAP.Sqlite   

* LiteDB ([@maikebing](https://github.com/maikebing)) ：https://github.com/maikebing/CAP.Extensions

* SQLite & Oracle ([@cocosip](https://github.com/cocosip)) ：https://github.com/cocosip/CAP-Extensions   
