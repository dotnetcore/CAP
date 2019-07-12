# Diagnostics

Diagnostics 提供一组功能使我们能够很方便的可以记录在应用程序运行期间发生的关键性操作以及他们的执行时间等，使管理员可以查找特别是生产环境中出现问题所在的根本原因。


## CAP 中的 Diagnostics

在 CAP 中，对 `DiagnosticSource` 提供了支持，监听器名称为 `CapDiagnosticListener`。

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

相关涉及到的对象，你可以在 `DotNetCore.CAP.Diagnostics` 命名空间下看到。