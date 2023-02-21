# Filter

Subscriber filters are similar to ASP.NET MVC filters and are mainly used to process additional work before and after the subscriber method is executed. Such as transaction management or logging, etc.

## Create subscribe filter

### Create Filter

Create a new filter class and inherit the `SubscribeFilter` abstract class.

```C#
public class MyCapFilter: SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        // before subscribe method exectuing
    }

    public override Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        // after subscribe method executed
    }

    public override Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        // subscribe method execution exception
    }
}
```

In some scenarios, if you want to terminate the subscriber method execution, you can throw an exception in `OnSubscribeExecutingAsync`, and choose to ignore the exception in `OnSubscribeExceptionAsync`.

To ignore exceptions, you can setting `context.ExceptionHandled = true` in `ExceptionContext`


```C#
public override Task OnSubscribeExceptionAsync(ExceptionContext context)
{
    context.ExceptionHandled = true;
}
```

### Configuration Filter

Use `AddSubscribeFilter<>` to add a filter.

```C#
services.AddCap(opt =>
{
    // ***
}.AddSubscribeFilter<MyCapFilter>();
```

Currently, we do not support adding multiple filters.
