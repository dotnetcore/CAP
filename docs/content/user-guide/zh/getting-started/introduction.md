# 介绍

CAP 是一个EventBus，同时也是一个在微服务或者SOA系统中解决分布式事务问题的一个框架。它有助于创建可扩展，可靠并且易于更改的微服务系统。

在微软的 [eShopOnContainer](https://github.com/dotnet-architecture/eShopOnContainers) 微服务示例项目中，推荐使用 CAP 作为生产环境可用的 EventBus。

!!! question "什么是 EventBus？"

    事件总线是一种机制，它允许不同的组件彼此通信而不彼此了解。 组件可以将事件发送到Eventbus，而无需知道是谁来接听或有多少其他人来接听。 组件也可以侦听Eventbus上的事件，而无需知道谁发送了事件。 这样，组件可以相互通信而无需相互依赖。 同样，很容易替换一个组件。 只要新组件了解正在发送和接收的事件，其他组件就永远不会知道.

相对于其他的 Service Bus 或者 Event Bus， CAP 拥有自己的特色，它不要求使用者发送消息或者处理消息的时候实现或者继承任何接口，拥有非常高的灵活性。我们一直坚信约定大于配置，所以CAP使用起来非常简单，对于新手非常友好，并且拥有轻量级。

CAP 采用模块化设计，具有高度的可扩展性。你有许多选项可以选择，包括消息队列，存储，序列化方式等，系统的许多元素内容可以替换为自定义实现。


## 相关视频

[Video: bilibili 教程](https://www.bilibili.com/video/av31582401/)

[Video: Youtube 教程](https://youtu.be/K1e4e0eddNE)

[Video: 腾讯视频教程](https://www.cnblogs.com/savorboard/p/7243609.html)

## 相关文章

[Article: CAP 介绍及使用](http://www.cnblogs.com/savorboard/p/cap.html)

[Article: CAP 5.0 版本中的新特性](https://www.cnblogs.com/savorboard/p/cap-5-0.html)

[Article: CAP 3.0 版本中的新特性](https://www.cnblogs.com/savorboard/p/cap-3-0.html)

[Article: CAP 2.6 版本中的新特性](https://www.cnblogs.com/savorboard/p/cap-2-6.html)

[Article: CAP 2.5 版本中的新特性](https://www.cnblogs.com/savorboard/p/cap-2-5.html)

[Article: CAP 2.4 版本中的新特性](http://www.cnblogs.com/savorboard/p/cap-2-4.html)

[Article: CAP 2.3 版本中的新特性用](http://www.cnblogs.com/savorboard/p/cap-2-3.html)

[Article: .NET Core Community 首个千星项目诞生：CAP](https://www.cnblogs.com/forerunner/p/ncc-cap-with-over-thousand-stars.html)
