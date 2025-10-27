# Idempotence

Idempotence (which you can read a formal definition of on [Wikipedia](https://en.wikipedia.org/wiki/Idempotence)) in messaging systems means that a message redelivery can be handled without resulting in an unintended state.

## Delivery Guarantees[^1]

[^1]: The chapter refers to the [Delivery guarantees](https://github.com/rebus-org/Rebus/wiki/Delivery-guarantees) of rebus, which I think is described very good.

Before discussing idempotency, let's discuss message delivery guarantees on the consumer side.

Since CAP doesn't use MS DTC or other 2PC (Two-Phase Commit) distributed transaction mechanisms, there is an inherent limitation: messages are delivered at least once. Specifically, in a message-based system, there are three possibilities:

* Exactly Once (*)  
* At Most Once 
* At Least Once  

Exactly Once has a (*) next to it because, in the general case, it is simply not possible.

### At Most Once

The At Most Once delivery guarantee ensures that you receive all messages either once or not at all.

This type of delivery guarantee can arise from your messaging system and your code performing actions in the following order:

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

In the best case scenario, this works well – your messages will be received, work transactions will be committed, and you will be happy.

However, things can fail – especially if you do a lot of work. For example, consider what happens if anything fails after step (1), and then – when you try to execute step (4)/(2) (i.e., put the message back into the queue) – the network becomes temporarily unavailable, the message broker restarts, or the host machine reboots due to a system update.

This might be acceptable if that's what you want, but most things in CAP revolve around the concept of DURABLE messages – messages whose contents are as important as the data in your database.

### At Least Once

The At Least Once delivery guarantee ensures that you receive all messages one or more times if something fails.

This requires a slight change in the order of execution and requires that the message queue system supports transactions, either through the traditional begin-commit-rollback protocol (MSMQ does this) or through a receive-ack-nack protocol (RabbitMQ, Azure Service Bus, etc. do this).

Consider this approach:

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

If the "lease" grabbed in step (1) has an appropriate timeout associated with it, then we are guaranteed that no matter how wrong things go, we will only actually remove the message from the queue (step 4/2) if we have successfully committed our "work transaction".

### What is a "Work Transaction"?

It depends on what you're doing 😄 Maybe it's a transaction in a relational database (which traditionally have good support for this), maybe it's a transaction in a document database that supports transactions (like RavenDB or PostgreSQL), or maybe it's a conceptual transaction representing the work you perform as a consequence of handling a message, e.g., updating documents in MongoDB, moving files in the file system, or modifying in-memory data structures.

The fact that the "work transaction" is conceptual makes it impossible to support Exactly Once delivery – it's simply not generally possible to commit or roll back a "work transaction" and a "queue transaction" (the protocol with the message queue system) atomically and consistently.

## Idempotence in CAP

In CAP, the **At Least Once** delivery guarantee is used.

Since CAP uses a temporary storage medium (database table), At Most Once could theoretically be achieved, but to strictly guarantee that messages are not lost, we do not provide related functions or configurations.

### Why We Don't Provide (Achieve) Idempotency

1. Message successfully written, but Consumer method execution failed.  

    There are many reasons why the Consumer method might fail. Without knowing the specific scenario, it's unclear whether retrying blindly or not retrying is the correct choice.
    For example, if the consumer is a debit service and the debit execution succeeds but fails to write the debit log, CAP will consider the consumer failed and retry. If the client doesn't guarantee idempotency, the framework will retry, inevitably leading to serious consequences like multiple debits.

2. Consumer method execution succeeded, but the same message is received again.  

    This scenario is also possible. If the Consumer has already executed successfully but for some reason (e.g., broker recovery), the same message is received again, CAP will treat it as a new message. Message will be executed again by the Consumer. Because it is a new message, CAP cannot ensure idempotency at this point.

3. Current data storage mode cannot guarantee idempotency.  

    Since the CAP message table for successfully consumed messages is deleted after 1 hour, historical messages cannot be verified for idempotency. If the broker has been maintained or manually processed some messages for some reason, there's no way to verify if they were already processed.

4. Industry practices.

    Many event-driven frameworks require users to ensure idempotent operations, such as ENode, RocketMQ, etc.

From an implementation perspective, CAP could provide some less stringent idempotency, but strict idempotency cannot be guaranteed.

### Naturally Idempotent Message Processing

Generally, the best way to handle message redeliveries is to make the processing of each message naturally idempotent.

Natural idempotence occurs when processing a message consists of calling an idempotent method on a domain object, like:

```
obj.MarkAsDeleted();
```

or

```
obj.UpdatePeriod(message.NewPeriod);
```

You can use `INSERT ON DUPLICATE KEY UPDATE` provided by the database to achieve this easily.

### Explicitly Handling Redeliveries

Another way to make message processing idempotent is to explicitly track IDs of processed messages and then handle redeliveries in your code.

Assuming you track message IDs using an `IMessageTracker` that uses the same transactional data store as the rest of your work, your code might look like this:

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

    // Do the actual work here
    // ...

    // Record that this message has been processed
    await _messageTracker.MarkAsProcessed(message.Id);
}
```

For the `IMessageTracker` implementation, you can use a message ID storage system like Redis or a database with a corresponding processing state.