# Transactions

Document from https://github.com/rebus-org/Rebus/wiki/Transactions

## Distributed transactions?

Out of the box, Rebus will NOT use DTC. This is a deliberate design choice, because it is our opinion that data consistency and resiliency against failures are are much better handled by being conscious about how those things work and what the consequences are, instead of relying on a black box to handle it.

Another thing is that distributed transactions are usually quite expensive, performance-wise, because of the communication overhead involved. Also, since DTC (as everyone else) is subject to the CAP theorem, it will have to give up availability (the 'A' in CAP) when a network partition occurs.

## Scenarios

