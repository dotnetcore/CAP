# General

CAP need to use storage media with persistence capabilities to store event messages, such as through databases or other NoSql facilities. CAP uses this approach to deal with the loss of messages in all environments or network anomalies. The reliability of messages is the cornerstone of distributed transactions, so messages cannot be lost under any circumstances.

## Persistent

### Before sent

Before the message enters the message queue, the CAP uses the local database table to persist the message, which ensures that the message is not lost when the message queue is abnormal or a network error occurs.

To ensure the reliability of this mechanism, CAP uses the same database transactions as the business code to ensure that business operations and CAP messages are consistent in the persistence process. That is to say, in the process of message persistence, the database will be rolled back when any one of the exceptions occurs.

###  After sent

After the message enters the message queue, the CAP will start the persistence function of the message queue. We need to explain how the CAP message is persisted in RabbitMQ and Kafka.

For message persistence in RabbitMQ, CAP uses a consumer queue with message persistence, but there may be exceptions here.

!!! info "Ready for production?"
    By default, queues registered by CAP in RabbitMQ are persistent. When used in a production environment, we recommend that you start all consumers once to create the queues with persistence, which ensures that all queues are created before the message is sent.

Since Kafka is born with message persistence using files, Kafka will ensure that messages are properly persisted without loss after the message enters Kafka.

## Storage

After the CAP started, two tables are generated into the persistent, by default the name is `Cap.Published` and `Cap.Received`.

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

When the CAP sends a message, it will store the original message object in a second package in the `Content` field. 

The following is the **Wapper Object** data structure of Content field.

NAME | DESCRIPTION | TYPE
:---|:---|:---
Id	| Message Id	| string
Timestamp |	Message created time |	string
Content |	Message content |	string
CallbackName |	Consumer callback topic name | string

The `Id` field is generate using the mongo [objectid algorithm](https://www.mongodb.com/blog/post/generating-globally-unique-identifiers-for-use-with-mongodb).