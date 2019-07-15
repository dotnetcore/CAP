# Transaction

## Distributed transactions?

CAP does not directly provide out-of-the-box MS DTC or 2PC-based distributed transactions, instead we provide a solution that can be used to solve problems encountered in distributed transactions.

In a distributed environment, using 2PC or DTC-based distributed transactions can be very expensive due to the overhead involved in communication, as is performance. In addition, since distributed transactions based on 2PC or DTC are also subject to the **CAP theorem**, it will have to give up availability (A in CAP) when network partitioning occurs.

> A distributed transaction is a very complex process with a lot of moving parts that can fail. Also, if these parts run on different machines or even in different data centers, the process of committing a transaction could become very long and unreliable.

> This could seriously affect the user experience and overall system bandwidth. So **one of the best ways to solve the problem of distributed transactions is to avoid them completely**.[^1]
 
For the processing of distributed transactions, CAP uses the "Eventual Consistency and Compensation" scheme.

### Eventual Consistency and Compensation [^1]

[^1]: This chapter is quoted from: https://www.baeldung.com/transactions-across-microservices

By far, one of the most feasible models of handling consistency across microservices is [eventual consistency](https://en.wikipedia.org/wiki/Eventual_consistency).

This model doesn’t enforce distributed ACID transactions across microservices. Instead, it proposes to use some mechanisms of ensuring that the system would be eventually consistent at some point in the future.

#### A Case for Eventual Consistency

For example, suppose we need to solve the following task:

* register a user profile  
* do some automated background check that the user can actually access the system

The second task is to ensure, for example, that this user wasn’t banned from our servers for some reason.

But it could take time, and we’d like to extract it to a separate microservice. It wouldn’t be reasonable to keep the user waiting for so long just to know that she was registered successfully.

**One way to solve it would be with a message-driven approach including compensation**. Let’s consider the following architecture:

* the user microservice tasked with registering a user profile  
* the validation microservice tasked with doing a background check  
* the messaging platform that supports persistent queues  

The messaging platform could ensure that the messages sent by the microservices are persisted. Then they would be delivered at a later time if the receiver weren’t currently available

#### Happy Scenario

In this architecture, a happy scenario would be:

* the user microservice registers a user, saving information about her in its local database
* the user microservice marks this user with a flag. It could signify that this user hasn’t yet been validated and doesn’t have access to full system functionality
* a confirmation of registration is sent to the user with a warning that not all functionality of the system is accessible right away
* the user microservice sends a message to the validation microservice to do the background check of a user
* the validation microservice runs the background check and sends a message to the user microservice with the results of the check
* if the results are positive, the user microservice unblocks the user
* if the results are negative, the user microservice deletes the user account

After we’ve gone through all these steps, the system should be in a consistent state. However, for some period of time, the user entity appeared to be in an incomplete state.

The last step, when the user microservice removes the invalid account, is a compensation phase.

#### Failure Scenarios

Now let’s consider some failure scenarios:

* if the validation microservice is not accessible, then the messaging platform with its persistent queue functionality ensures that the validation microservice would receive this message at some later time
* suppose the messaging platform fails, then the user microservice tries to send the message again at some later time, for example, by scheduled batch-processing of all users that were not yet validated
* if the validation microservice receives the message, validates the user but can’t send the answer back due to the messaging platform failure, the validation microservice also retries sending the message at some later time
* if one of the messages got lost, or some other failure happened, the user microservice finds all non-validated users by scheduled batch-processing and sends requests for validation again

Even if some of the messages were issued multiple times, this wouldn’t affect the consistency of the data in the microservices’ databases.

**By carefully considering all possible failure scenarios, we can ensure that our system would satisfy the conditions of eventual consistency. At the same time, we wouldn’t need to deal with the costly distributed transactions.**

But we have to be aware that ensuring eventual consistency is a complex task. It doesn’t have a single solution for all cases.