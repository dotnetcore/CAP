# Filter

Subscriber filters are similar to ASP.NET MVC filters and are mainly used to perform additional work before and after the subscriber method executes, such as transaction management or logging.

## Creating a Subscriber Filter

### Create Filter

Create a new filter class that inherits from the `SubscribeFilter` abstract class.

```C#
public class MyCapFilter : SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        // Execute before the subscriber method runs
    }

    public override Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        // Execute after the subscriber method completes
    }

    public override Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        // Handle exceptions during subscriber method execution
    }
}
```

In some scenarios, if you want to terminate the subscriber method execution, you can throw an exception in `OnSubscribeExecutingAsync`, and choose to handle the exception in `OnSubscribeExceptionAsync`.

To ignore exceptions, set `context.ExceptionHandled = true` in `ExceptionContext`:

```C#
public override Task OnSubscribeExceptionAsync(ExceptionContext context)
{
    context.ExceptionHandled = true;
}
```

### Registering a Filter

Use `AddSubscribeFilter<>` to register a filter.

```C#
services.AddCap(opt =>
{
    // ...
}).AddSubscribeFilter<MyCapFilter>();
```

Currently, multiple filters are not supported.
