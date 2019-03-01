# Design

## Motivation

With the popularity of microservices architecture, more and more people are trying to use microservices to architect their systems. In this we encounter problems such as distributed transactions. To solve these problems, I did not find simplicity and Easy to use solution, so I decided to create such a library to solve this problem.

The original CAP was to solve the transaction problems in the distributed system. She used asynchronous to ensure that this weak consistency transaction mechanism achieved the eventual consistency of the distributed transaction. For more information, see section 6.

Now in addition to solving distributed transaction problems, CAP's other important function is to use it as an EventBus. It has all of the features of EventBus and provides a more simplified way to handle publish/subscribe in EventBus.

## Persistence

The CAP relies on the local database for persistence of messages. The CAP uses this method to deal with situations in which all messages are lost due to environmental or network anomalies. The reliability of messages is the cornerstone of distributed transactions, so messages cannot be lost under any circumstances.

There are two types of persistence for messages:

**1 Persistence before the message enters the message queue**

Before the message enters the message queue, the CAP uses the local database table to persist the message. This ensures that the message is not lost when the message queue is abnormal or the network error occurs.

In order to ensure the reliability of this mechanism, CAP uses database transactions with the same business code to ensure that business operations and CAP messages are strongly consistent throughout the persistence process. That is to say, in the process of message persistence, the database of any abnormal situation will be rolled back.

**2 Persistence after messages enter the message queue**
 
After the message enters the message queue, the CAP starts the persistence function of the message queue. We need to explain how the message of the CAP in RabbitMQ and Kafka is persistent.

For message persistence in RabbitMQ, CAP uses a consumer queue with message persistence, but there may be exceptions to this and take part in 2.2.1.

Since Kafka is inherently designed to persist messages using files, Kafka ensures that messages are correctly persisted without loss after the message enters Kafka.

## Communication Data Streams

The flow of messages in the CAP is roughly as follows:

 >2.2 version before

![](http://images2017.cnblogs.com/blog/250417/201708/250417-20170803174645928-1813351415.png)

> "P" represents the sender of the message (producer). "C" stands for message consumer (subscriber).

**After version 2.2**

In the 2.2 and later versions, we adjusted the flow of some messages. We removed the Queue table in the database and used the memory queue instead. For details, see: [Improve the implementation mechanism of queue mode](https://github.com/dotnetcore/CAP/issues/96)
 
## Consistency

The CAP uses the ultimate consistency as a consistent solution. This solution follows the CAP theory. The following is the description of the CAP theory.

C (consistent) consistency refers to the atomicity of data. It is guaranteed by transactions in a classic database. When a transaction completes, the data will be in a consistent state regardless of success or rollback. In a distributed environment, consistency is Indicates whether the data of multiple nodes is consistent;

A (availability) service is always available, when the user sends a request, the service can return the result within a certain time;

P (Partition Tolerance) In distributed applications, the system may not operate due to some distributed reasons. The good partition tolerance makes the application a distributed system but it seems to be a functioning whole.

According to ["CAP" distributed theory](https://en.wikipedia.org/wiki/CAP_theorem), in a distributed system, we often reluctantly give up strong consensus support for availability and partition fault tolerance, and instead pursue Ultimate consistency. In most business scenarios, we can accept short-term inconsistencies.

Section 6 will introduce this further.