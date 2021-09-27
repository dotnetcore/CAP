# Introduction

CAP is an EventBus and a solution for solving distributed transaction problems in microservices or SOA systems. It helps create a microservices system that is scalable, reliable, and easy to change.

In Microsoft's [eShopOnContainer](https://github.com/dotnet-architecture/eShopOnContainers) microservices sample project, it is recommended to use CAP as the EventBus in the production environment.


!!! question "What is EventBus？"

    An Eventbus is a mechanism that allows different components to communicate with each other without knowing about each other. A component can send an Event to the Eventbus without knowing who will pick it up or how many others will pick it up. Components can also listen to Events on an Eventbus, without knowing who sent the Events. That way, components can communicate without depending on each other. Also, it is very easy to substitute a component. As long as the new component understands events that are being sent and received, other components will never know about the substitution.

Compared to other Services Bus or Event Bus, CAP has its own characteristics. It does not require users to implement or inherit any interface when sending messages or processing messages. It has very high flexibility. We have always believed that the appointment is greater than the configuration, so the CAP is very simple to use, very friendly to the novice, and lightweight.

CAP is modular in design and highly scalable. You have many options to choose from, including message queues, storage, serialization, etc. Many elements of the system can be replaced with custom implementations.

## Related videos

[Video: bilibili Tutorial](https://www.bilibili.com/video/av31582401/)

[Video: Youtube Tutorial](https://youtu.be/K1e4e0eddNE)

[Video: Tencent Tutorial](https://www.cnblogs.com/savorboard/p/7243609.html)

## Related articles

[Article: Introduction and how to use](http://www.cnblogs.com/savorboard/p/cap.html)

[Article: New features in version 5.0](https://www.cnblogs.com/savorboard/p/cap-5-0.html)

[Article: New features in version 3.0](https://www.cnblogs.com/savorboard/p/cap-3-0.html)

[Article: New features in version 2.6](https://www.cnblogs.com/savorboard/p/cap-2-6.html)

[Article: New features in version 2.5](https://www.cnblogs.com/savorboard/p/cap-2-5.html)

[Article: New features in version 2.4](http://www.cnblogs.com/savorboard/p/cap-2-4.html)

[Article: New features in version 2.3](http://www.cnblogs.com/savorboard/p/cap-2-3.html)

[Article: .NET Core Community The first thousand-star project was born: CAP](https://www.cnblogs.com/forerunner/p/ncc-cap-with-over-thousand-stars.html)
