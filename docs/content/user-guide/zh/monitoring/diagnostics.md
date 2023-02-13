# 诊断(Diagnostics)

Diagnostics 提供一组功能使我们能够很方便的可以记录在应用程序运行期间发生的关键性操作以及他们的执行时间等，使管理员可以查找特别是生产环境中出现问题所在的根本原因。

## 跟踪(Tracing)

CAP 对 .NET `DiagnosticSource` 提供了支持，监听器名称为 `CapDiagnosticListener`。

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

### 在 Skywalking APM 中追踪 CAP 事件

Skywalking 的 C# 客户端提供了对 CAP Diagnostics 的支持，你可以利用 [SkyAPM-dotnet](https://github.com/SkyAPM/SkyAPM-dotnet) 来实现在 Skywalking 中追踪事件。

尝试阅读Readme文档来在你的项目中集成它。

![](https://user-images.githubusercontent.com/8205994/71006463-51025980-2120-11ea-82dc-bffa5530d515.png)


![](https://user-images.githubusercontent.com/8205994/71006589-7b541700-2120-11ea-910b-7e0f2dfddce8.png)

### 其他 APM 的支持

目前还没有实现对除了 Skywalking 的其他APM的支持，如果你想在其他 APM 中实现对 CAP 诊断事件的支持，你可以参考这里的代码来实现它：

https://github.com/SkyAPM/SkyAPM-dotnet/tree/master/src/SkyApm.Diagnostics.CAP

## 度量(Metrics)

度量是指对于一个物体或是事件的某个性质给予一个数字，使其可以和其他物体或是事件的相同性质比较。度量可以是对一物理量（如长度、尺寸或容量等）的估计或测定，也可以是其他较抽象的特质。

CAP 7.0 对 `EventSource` 提供了支持，计数器名称为 `DotNetCore.CAP.EventCounter`。

CAP 提供了以下几个度量指标：

* 每秒发布速度
* 每秒消费速度
* 每秒调用订阅者速度
* 每秒执行订阅者平均耗时

### 使用 dotnet-counters 查看度量

[dotnet-counters](https://learn.microsoft.com/zh-cn/dotnet/core/diagnostics/dotnet-counters) 是一个性能监视工具，用于临时运行状况监视和初级性能调查。 它可以观察通过 EventCounter API 或 Meter API 发布的性能计数器值。 

使用以下命令来监视CAP中的度量指标：

```ps
dotnet-counters ps
dotnet-counters monitor --process-id=25496 --counters=DotNetCore.CAP.EventCounter
```

其中 process-id 为 CAP 所属的进程Id。

![img](/img/dotnet-counters.gif)

### 在 Dashboard 中查看度量

你可以配置 `x.UseDashboard()` 来开启仪表盘以图表的形式查看 Metrics 指标。 如下图：

![img](/img/dashboard-metrics.gif)


在 Realtime Metric Graph 中，时间轴会随着时间实时滚动从而可以看到发布和消费消息每秒的速率，同时我们可以看到消费者执行耗时以“打点”的方式在 Y1 轴上（Y0轴为速率，Y1轴为执行耗时）。

