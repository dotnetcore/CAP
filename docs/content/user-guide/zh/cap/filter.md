# 过滤器

从 5.1.0 版本后，我们引入了对订阅者过滤器的支持，以使在某些场景（如事务处理，日志记录等）中变得容易。

## 自定义过滤器

### 添加过滤器

创建一个过滤器类，并继承 `SubscribeFilter` 抽象类。

```C#
public class MyCapFilter: SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        // 订阅方法执行前
    }

    public override Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        // 订阅方法执行后
    }

    public override Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        // 订阅方法执行异常
    }
}
```

在一些场景中，如果想终止订阅者方法执行，可以在 `OnSubscribeExecutingAsync` 中抛出异常，并且在 `OnSubscribeExceptionAsync` 中选择忽略该异常。

通过在 `ExceptionContext` 中设置 `context.ExceptionHandled = true` 来忽略异常。

```C#
public override Task OnSubscribeExceptionAsync(ExceptionContext context)
{
    context.ExceptionHandled = true;
}
```

### 配置过滤器

```C#
services.AddCap(opt =>
{
    // ***
}.AddSubscribeFilter<MyCapFilter>();
```

目前， 我们还不支持同时添加多个过滤器。

!!! WARNING "过滤器中使用 AsyncLocal 的问题"
    我们不建议在过滤器中使用AsyncLocal，因为过滤器的生命周期为Scoped，所以直接定义临时变量即可在整个执行周期内共享变量值。
    然后，如果由于一些你无法控制的原因要使用，由于AsyncLocal的设计问题，则可将异步过滤器作为同步使用，也就是继承的方法构造中不添加 async 关键字。
