# NATS

[NATS](https://nats.io/) is a simple, secure and performant communications system for digital systems, services and devices. NATS is part of the Cloud Native Computing Foundation (CNCF).

!!! warning
    Versions of CAP below 5.2.0 are implement based on Request/Response mode, and now we are based on JetStream implementation.
    see https://github.com/dotnetcore/CAP/issues/983 for more information.

## Configuration

To use NATS transporter, you need to install the following package from NuGet:

```powershell

PM> Install-Package DotNetCore.CAP.NATS

```

Then you can add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    services.AddCap(capOptions =>
    {
        capOptions.UseNATS(natsOptions=>{
            //NATS Options
        });
    });
}

```

#### NATS Options

NATS configuration parameters provided directly by the CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
Options | NATS client configuration | Options | Options
Servers | Server url/urls used to connect to the NATs server. | string | NULL
ConnectionPoolSize  | number of connections pool | uint | 10

#### NATS ConfigurationOptions

If you need **more** native NATS related configuration options, you can set them in the `Options` option:

```csharp
services.AddCap(capOptions => 
{
    capOptions.UseNATS(natsOptions=>
    {
        // NATS options.
        natsOptions.Options.Url="";
    });
});
```

`Options` is a NATS.Client ConfigurationOptions , you can find more details through this [link](http://nats-io.github.io/nats.net/class_n_a_t_s_1_1_client_1_1_options.html)
