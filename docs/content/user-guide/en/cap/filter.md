# Filter

Subscriber filters are similar to ASP.NET MVC filters and are mainly used to process additional work before and after the subscriber method is executed. Such as transaction management or logging, etc.

## Create subscribe filter

### Create Filter

Create a new filter class and inherit the `SubscribeFilter` abstract class.

```C#
public class MyCapFilter: SubscribeFilter
{
    public override void OnSubscribeExecuting(ExecutingContext context)
    {
        // before subscribe method exectuing
    }

    public override void OnSubscribeExecuted(ExecutedContext context)
    {
        // after subscribe method executed
    }

    public override void OnSubscribeException(ExceptionContext context)
    {
        // subscribe method execution exception
    }
}
```

In some scenarios, if you want to terminate the subscriber method execution, you can throw an exception in `OnSubscribeExecuting`, and choose to ignore the exception in `OnSubscribeException`.

To ignore exceptions, you can setting `context.ExceptionHandled = true` in `ExceptionContext`


```C#
public override void OnSubscribeException(ExceptionContext context)
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
