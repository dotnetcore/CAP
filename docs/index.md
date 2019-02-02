# CAP

CAP is a library based on .Net standard, which is a solution to deal with distributed transactions, also has the function of EventBus, it is lightweight, easy to use, and efficiently.

## Introduction

In the process of building an SOA or MicroService system, we usually need to use the event to integrate each services. In the process, the simple use of message queue does not guarantee the reliability. CAP is adopted the local message table program integrated with the current database to solve the exception may occur in the process of the distributed system calling each other. It can ensure that the event messages are not lost in any case.

You can also use the CAP as an EventBus. The CAP provides a simpler way to implement event publishing and subscriptions. You do not need to inherit or implement any interface during the process of subscription and sending.

This is a diagram of the CAP working in the ASP.NET Core MicroService architecture:

![cap.png](img/architecture.png)

!!! note
    CAP implements the Outbox Pattern described in the [eShop ebook](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus).

## Contributing

One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

If you have any question or problems, please report them on the CAP repository:

<a href="https://github.com/dotnetcore/cap/issues/new"><button class="btn btn-primary btn-lg" type="submit"><i class="fab fa-github fa-2x"></i> Report Issue</button></a>
<a href="https://github.com/dotnetcore/cap/issues"><button class="btn btn-primary btn-lg" type="submit"> Active Issues <i class="fab fa-github fa-2x"></i></button></a>

## License

CAP is licensed under the [MIT license](https://github.com/dotnetcore/CAP/blob/master/LICENSE.txt).
