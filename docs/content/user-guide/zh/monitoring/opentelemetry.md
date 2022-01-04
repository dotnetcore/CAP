# OpenTelemetry 

https://opentelemetry.io/

OpenTelemetry是工具、api和sdk的集合。 使用它来检测、生成、收集和导出遥测数据(度量、日志和跟踪)，以帮助您分析软件的性能和行为。

## 集成

You can find it [here](https://opentelemetry.io/docs/instrumentation/net/getting-started/) about how to use OpenTelemetry in console applications or ASP.NET Core, at here we mainly describe how to tracing CAP data to OpenTelemetry.

你可以在[这里](https://opentelemetry.io/docs/instrumentation/net/getting-started/)找到关于如何在控制台应用或ASP.NET Core 中使用OpenTelemetry。
在这里我们主要描述如何将CAP集成到OpenTelemetry中。

### 配置

安装CAP的OpenTelemetry包到项目中。

```C#
dotnet add package DotNetCore.Cap.OpenTelemetry
```

OpenTelemetry 的跟踪数据来自于[Diagnostics](diagnostics.md)发送的诊断数据，使用下面的配置行来启用收集数据。

```C#
services.AddCap(x =>
{
    //***
    x.UseOpenTelemetry();  // <-- Add this line
});

```

添加 CAP Instrumentation 到 OpenTelemetry的扩展配置中。

```C#
services.AddOpenTelemetryTracing((builder) => builder
    .AddAspNetCoreInstrumentation()
    .AddCapInstrumentation()    // <-- Add this line
    .AddZipkinExporter()
);
```

以下是CAP的跟踪数据在 Zipkin 中的一个示意图：

<img src="/img/opentelemetry.png">