# Idempotence

Imdempotence (which you may read a formal definition of on [Wikipedia](https://en.wikipedia.org/wiki/Idempotence), when we are talking about messaging, is when a message redelivery can be handled without ending up in an unintended state.

## Delivery guarantees[^1]

[^1]: The chapter refers to the [Delivery guarantees](https://github.com/rebus-org/Rebus/wiki/Delivery-guarantees) of rebus, which I think is described very good.

Before we talk about idempotency, let's talk about the delivery of messages on the consumer side.

Since CAP is not a used MS DTC or other type of 2PC distributed transaction mechanism, there is a problem that at least the message is strictly delivered once. Specifically, in a message-based system, there are three possibilities:

* Exactly Once(*)  
* At Most Once 
* At Least Once  

Exactly once has a (*) next to it, because in the general case, it is simply not possible.

### At Most Once

The At Most Once delivery guarantee covers the case when you are guaranteed to receive all messages either once, or maybe not at all.

This type of delivery guarantee can arise from your messaging system and your code performing its actions in the following order:


```
1. Remove message from queue
2. Start work transaction
3. Handle message (your code)
4. Success?
    Yes:
        1. Commit work transaction
    No: 
        1. Roll back work transaction
        2. Put message back into the queue
```

In the sunshine scenario, this is all well and good – your messages will be received, and work transactions will be committed, and you will be happy.

However, the sun does not always shine, and stuff tends to fail – especially if you do enough stuff. Consider e.g. what would happen if anything fails after having performed step (1), and then – when you try to execute step (4)/(2) (i.e. put the message back into the queue) – the network was temporarily unavailable, or the message broker restarted, or the host machine decided to reboot because it had installed an update.

This can be OK if it's what you want, but most things in CAP revolve around the concept of DURABLE messages, i.e. messages whose contents is just as important as the data in your database.

### At Least Once

This delivery guarantee covers the case when you are guaranteed to receive all messages either once, or maybe more times if something has failed.

It requires a slight change to the order we are executing our steps in, and it requires that the message queue system supports transactions, either in the form of the traditional begin-commit-rollback protocol (MSMQ does this), or in the form of a receive-ack-nack protocol (RabbitMQ, Azure Service Bus, etc. do this).

Check this out – if we do this:

```
1. Grab lease on message in queue
2. Start work transaction
3. Handle message (your code)
4. Success?
    Yes: 
        1. Commit work transaction
        2. Delete message from queue
    No: 
        1. Roll back work transaction
        2. Release lease on message
```

and the "lease" we grabbed on the message in step (1) is associated with an appropriate timeout, then we are guaranteed that no matter how wrong things go, we will only actually remove the message from the queue (i.e. execute step (4)/(2)) if we have successfully committed our "work transaction".

### What is a "work transaction"?

It depends on what you're doing 😄 maybe it's a transaction in a relational database (which traditionally have pretty good support in this regard), maybe it's a transaction in a document database that happens to support transaction (like RavenDB or Postgres), or maybe it's a conceptual transaction in the form of whichever work you happen to carry out as a consequence of handling a message, e.g. update a bunch of documents in MongoDB, move some files around in the file system, or mutate some obscure in-mem data structure.

The fact that the "work transaction" is just a conceptual thing is what makes it impossible to support the aforementioned Exactly Once delivery guarantee – it's just not generally possible to commit or roll back a "work transaction" and a "queue transaction" (which is what we could call the protocol carried out with the message queue systems) atomically and consistently.

## Idempotence at CAP

In the CAP, the delivery guarantees we use is **At Least Once**.

Since we have a temporary storage medium (database table), we may be able to do At Most Once, but in order to strictly guarantee that the message will not be lost, we do not provide related functions or configurations.

### Why are we not providing(achieving) idempotency ?

1. The message was successfully written, but the execution of the Consumer method failed.  

    There are a lot of reasons why the Consumer method fails. I don't know if the specific scene is blindly retrying or not retrying is an incorrect choice.
    For example, if the consumer is debiting service, if the execution of the debit is successful, but fails to write the debit log, the CAP will judge that the consumer failed to execute and try again. If the client does not guarantee idempotency, the framework will retry it, which will inevitably lead to serious consequences for multiple debits.

2. The implementation of the Consumer method succeeded, but received the same message.  

    The scenario is also possible here. If the Consumer has been successfully executed at the beginning, but for some reason, such as the Broker recovery, and received the same message, the CAP will consider this a new after receiving the Broker message. The message will be executed again by the Consumer. Because it is a new message, the CAP cannot be idempotent at this time.

3. The current data storage mode can not be idempotent.  

    Since the table of the CAP message is deleted after 1 hour for the successfully consumed message, if the historical message cannot be idempotent. Historically, if the broker has maintained or manually processed some messages for some reason.

4. Industry practices.

    Many event-driven frameworks require users to ensure idempotent operations, such as ENode, RocketMQ, etc...

From an implementation point of view, CAP can do some less stringent idempotence, but strict idempotent cannot.

### Naturally idempotent message processing

Generally, the best way to deal with message redeliveries is to make the processing of each message naturally idempotent.

Natural idempotence arises when the processing of a message consists of calling an idempotent method on a domain object, like

```
obj.MarkAsDeleted();

```

or

```
obj.UpdatePeriod(message.NewPeriod);
```

You can use the `INSERT ON DUPLICATE KEY UPDATE` provided by the database to easily done.

### Explicitly handling redeliveries

Another way of making message processing idempotent, is to simply track IDs of processed messages explicitly, and then make your code handle a redelivery.

Assuming that you are keeping track of message IDs by using an `IMessageTracker` that uses the same transactional data store as the rest of your work, your code might look somewhat like this:

```c#
readonly IMessageTracker _messageTracker;

public SomeMessageHandler(IMessageTracker messageTracker)
{
    _messageTracker = messageTracker;
}

[CapSubscribe]
public async Task Handle(SomeMessage message) 
{
    if (await _messageTracker.HasProcessed(message.Id))
    {
        return;
    }

    // do the work here
    // ...

    // remember that this message has been processed
    await _messageTracker.MarkAsProcessed(messageId);
}
```

As for the implementation of `IMessageTracker`, you can use a storage message Id such as Redis or a database and the corresponding processing state.