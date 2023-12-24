# OpenTelemetry 

https://opentelemetry.io/

OpenTelemetry is a collection of tools, APIs, and SDKs. Use it to instrument, generate, collect, and export telemetry data (metrics, logs, and traces) to help you analyze your software’s performance and behavior.

## Integration

You can find it [here](https://opentelemetry.io/docs/instrumentation/net/getting-started/) about how to use OpenTelemetry in console applications or ASP.NET Core, at here we mainly describe how to tracing CAP data to OpenTelemetry.

### Configuration

Install the CAP OpenTelemetry package into the project.

```C#
dotnet add package DotNetCore.Cap.OpenTelemetry
```

The OpenTelemetry data comes from [diagnostics](diagnostics.md), add the instrumentation of CAP to the configuration of OpenTelemetry.

```C#
services.AddOpenTelemetryTracing((builder) => builder
    .AddAspNetCoreInstrumentation()
    .AddCapInstrumentation()    // <-- Add this line
    .AddZipkinExporter()
);
```

If you don't use a framework that does this automatically for you (like aspnetcore), make sure you enable a listener, for example:

```C#
ActivitySource.AddActivityListener(new ActivityListener()
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Start"),
    ActivityStopped = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Stop")
});
```
Here is a diagram of CAP's tracking data in Zipkin:

<img src="/img/opentelemetry.png">

### Context Propagation
CAP supports [Context
Propagation](https://opentelemetry.io/docs/instrumentation/js/propagation/) by
injecting `traceparent` and `baggage` headers when sending messages and
restoring the context from those headers when receiving messages.

CAP uses the configured Propagators.DefaultTextMapPropagator propagator, which
is usually set to both TraceContextPropagator and BaggagePropagator [by the
dotnet OpenTelemetry
SDK](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Sdk.cs#L21)
but can be set in your your client program. For example, to opt out of the
Baggage propagation, you can call:

```C#
OpenTelemetry.Sdk.SetDefaultTextMapPropagator(
    new TraceContextPropagator());
```

See the [dotnet OpenTelemetry.Api
readme](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/README.md?plain=1#L455)
for more details.

