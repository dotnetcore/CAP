# Introduction

CAP is an EventBus and a solution for solving distributed transaction problems in microservices or SOA systems. It helps you create a microservices system that is scalable, reliable, and easy to modify.

In Microsoft's [eShop](https://github.com/dotnet/eShop) microservices sample project, CAP is recommended as the EventBus for production environments.

!!! question "What is EventBus?"

    An EventBus is a mechanism that allows different components to communicate with each other without knowing each other. A component can send an Event to the EventBus without knowing who will pick it up or how many others will. Components can also listen to Events on an EventBus without knowing who sent them. This way, components can communicate without depending on each other. Also, it's very easy to substitute a component – as long as the new component understands the events being sent and received, other components will never know about the substitution.

Compared to other service buses or event buses, CAP has its own characteristics. It does not require users to implement or inherit any interface when sending or processing messages, providing very high flexibility. We believe that convention is greater than configuration, so CAP is very simple to use, very friendly to beginners, and lightweight.

CAP is modular in design and highly scalable. You have many options to choose from, including message queues, storage, serialization, and more. Many system elements can be replaced with custom implementations.

## Related videos

[Video: bilibili Tutorial](https://www.bilibili.com/video/av31582401/)

[Video: Youtube Tutorial](https://youtu.be/K1e4e0eddNE)

[Video: Youtube Tutorial - @CodeOpinion](https://www.youtube.com/watch?v=dnhPzILvgeo) 

[Video: Tencent Tutorial](https://www.cnblogs.com/savorboard/p/7243609.html)

## Related articles

[Article: Introduction and how to use](http://www.cnblogs.com/savorboard/p/cap.html)

[Article: New features in version 7.0](https://www.cnblogs.com/savorboard/p/cap-7-0.html)

[Article: New features in version 6.0](https://www.cnblogs.com/savorboard/p/cap-6-0.html)

[Article: New features in version 5.0](https://www.cnblogs.com/savorboard/p/cap-5-0.html)

[Article: New features in version 3.0](https://www.cnblogs.com/savorboard/p/cap-3-0.html)

[Article: New features in version 2.6](https://www.cnblogs.com/savorboard/p/cap-2-6.html)

[Article: New features in version 2.5](https://www.cnblogs.com/savorboard/p/cap-2-5.html)

[Article: New features in version 2.4](http://www.cnblogs.com/savorboard/p/cap-2-4.html)

[Article: New features in version 2.3](http://www.cnblogs.com/savorboard/p/cap-2-3.html)

[Article: .NET Core Community The first thousand-star project was born: CAP](https://www.cnblogs.com/forerunner/p/ncc-cap-with-over-thousand-stars.html)
