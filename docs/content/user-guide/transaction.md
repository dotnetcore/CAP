# Transaction

For the processing of distributed transactions, this CAP library matches the "Asynchronous recovery events" scenario.

## Asynchronous recovery events

As known as the name "native message table", this is a classic solution, originally from EBay, and referenced links about it are at the end of this section. This is also one of the most popular solutions in the business development. 

Compared to TCC or 2pc/3pc, this solution is the simplest one for distributed transactions, and is decentralized. In TCC or 2PC solutions, the common transaction handlers synchronize the state among different services with a transaction coordinator, but it's not much required in this CAP solution. In addition, the deeper references of other conditions these services have, the more management complexity and stability risk may be increased in 2PC/TCC. Imagine that if we have 9 services committed successfully of all 10 whitch relied heavily, though the last one execute fail, should we roll back transactions of those 9 service? In fact, the cost is still very high. 

However, it's not mean that 2PC or TCC are at a disadvantage, each has its own suitability and matched scenarios, here won't introduce more.


> cn： [base-an-acid-alternative](http://www.cnblogs.com/savorboard/p/base-an-acid-alternative.html)
> 
> en： [Base: An Acid Alternative](http://queue.acm.org/detail.cfm?id=1394128)