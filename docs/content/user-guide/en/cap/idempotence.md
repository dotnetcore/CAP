# Idempotence

Idempotence (which you may read a formal definition of on [Wikipedia](https://en.wikipedia.org/wiki/Idempotence) when we are talking about messaging, is when message redelivery can be handled without ending up in an unintended state.

## Delivery guarantees[^1]

[^1]: The chapter refers to the [Delivery guarantees](https://github.com/rebus-org/Rebus/wiki/Delivery-guarantees) of rebus, which I think is described very well.

Before we talk about idempotency, let's talk about the delivery of messages on the consumer side.

Since CAP doesn't use MS DTC or another type of 2PC distributed transaction mechanism, there is a problem that the message is strictly delivered at least once. Specifically, in a message-based system, there are three possibilities:

* Exactly Once(*)  
* At Most Once 
* At Least Once  

Exactly once has a (*) next to it, because in the general case, it is simply not possible.

### At Most Once

The At Most Once delivery guarantee covers the case when you are guaranteed to receive all messages either once, or maybe not at all.

This type of delivery guarantee can arise from your messaging system and your code performing its actions in the following order:


```
1. Remove a message from the queue
2. Start work transaction
3. Handle message (your code)
4. Success?
    Yes:
        1. Commit work transaction
    No: 
        1. Rollback work transaction
        2. Put the message back into the queue
```

In the best-case scenario, this is all well and good – your messages will be received, work transactions will be committed, and you will be happy.

However, the sun does not always shine, and stuff tends to fail – especially if you do a lot of stuff. Consider e.g. what would happen if anything fails after having performed step (1), and then – when you try to execute step (4)/(2) (i.e. put the message back into the queue) – the network was temporarily unavailable, or the message broker restarted, or the host machine decided to reboot because it had installed an update.

This can be OK if it's what you want, but most things in CAP revolve around the concept of DURABLE messages, i.e. messages whose contents are just as important as the data in your database.

### At Least Once

This delivery guarantee covers the case when you are guaranteed to receive all messages either once, or maybe more times if something has failed.

It requires a slight change to the order we are executing our steps in, and it requires that the message queue system supports transactions, either in the form of the traditional begin-commit-rollback protocol (MSMQ does this) or in the form of a receive-ack-nack protocol (RabbitMQ, Azure Service Bus, etc. do this).

Check this out – if we do this:

```
1. Grab lease on message in the queue
2. Start work transaction
3. Handle message (your code)
4. Success?
    Yes: 
        1. Commit work transaction
        2. Delete message from the queue
    No: 
        1. Rollback work transaction
        2. Release lease on a message
```

and the "lease" we grabbed on the message in step (1) is associated with an appropriate timeout, then we are guaranteed that no matter how wrong things go, we will only actually remove the message from the queue (i.e. execute step (4)/(2)) if we have successfully committed our "work transaction".

### What is a "work transaction"?

It depends on what you're doing 😄 maybe it's a transaction in a relational database (which traditionally has pretty good support in this regard), maybe it's a transaction in a document database that happens to support transaction (like RavenDB or Postgres), or maybe it's a conceptual transaction in the form of whichever work you happen to carry out as a consequence of handling a message, e.g. update a bunch of documents in MongoDB, move some files around in the file system, or mutate some obscure in-mem data structure.

The fact that the "work transaction" is just a conceptual thing is what makes it impossible to support the aforementioned Exactly Once delivery guarantee – it's just not generally possible to commit or roll back a "work transaction" and a "queue transaction" (which is what we could call the protocol carried out with the message queue systems) atomically and consistently.

## Idempotence at CAP

In CAP,  **At Least Once**  delivery guarantee is used.

Since we have a temporary storage medium (database table), we may be able to do At Most Once, but in order to strictly guarantee that the message will not be lost, we do not provide related functions or configurations.

### Why are we not providing(achieving) idempotency?

1. The message was successfully written, but the execution of the Consumer method failed.  

    There are a lot of reasons why the Consumer method fails. I don't know if the specific scene is blindly retrying or not retrying is an incorrect choice.
    For example, if the consumer is debiting service, if the execution of the debit is successful, but fails to write the debit log, 
