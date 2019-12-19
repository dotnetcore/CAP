# Diagnostics

Diagnostics 提供一组功能使我们能够很方便的可以记录在应用程序运行期间发生的关键性操作以及他们的执行时间等，使管理员可以查找特别是生产环境中出现问题所在的根本原因。


## CAP 中的 Diagnostics

在 CAP 中，对 `DiagnosticSource` 提供了支持，监听器名称为 `CapDiagnosticListener`。

你可以在 `DotNetCore.CAP.Diagnostics.CapDiagnosticListenerNames` 类下面找到CAP已经定义的事件名称。

Diagnostics 提供对外提供的事件信息有：

* 消息持久化之前
* 消息持久化之后
* 消息持久化异常
* 消息向MQ发送之前
* 消息向MQ发送之后
* 消息向MQ发送异常
* 消息从MQ消费保存之前
* 消息从MQ消费保存之后
* 订阅者方法执行之前
* 订阅者方法执行之后
* 订阅者方法执行异常


## 在 Skywalking 中追踪 CAP 事件

Skywalking 的 C# 客户端提供了对 CAP Diagnostics 的支持，你可以利用 [SkyAPM-dotnet](https://github.com/SkyAPM/SkyAPM-dotnet) 来实现在 Skywalking 中追踪事件。

尝试阅读Readme文档来在你的项目中集成它。

![](https://user-images.githubusercontent.com/8205994/71006463-51025980-2120-11ea-82dc-bffa5530d515.png)


![](https://user-images.githubusercontent.com/8205994/71006589-7b541700-2120-11ea-910b-7e0f2dfddce8.png)

## 其他 APM 的支持

目前还没有实现对除了Skywalking的其他APM的支持，如果你想在其他 APM 中实现对 CAP 诊断事件的支持，你可以参考这里的代码来实现它：

https://github.com/SkyAPM/SkyAPM-dotnet/tree/master/src/SkyApm.Diagnostics.CAP