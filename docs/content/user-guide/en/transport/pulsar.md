# Apache Pulsar

[Apache Pulsar](https://pulsar.apache.org/) is a cloud-native, distributed messaging and streaming platform originally created at Yahoo! and now a top-level Apache Software Foundation project.

Pulsar can be used in CAP as a message transporter. 

## Configuration

To use Pulsar transporter, you need to install the following package from NuGet:

```powershell
PM> Install-Package DotNetCore.CAP.Pulsar

```

Then you can add configuration items to the `ConfigureServices` method of `Startup.cs`.

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UsePulsar(opt => {
            //Pulsar options
        });
        // x.UseXXX ...
    });
}

```

#### Pulsar Options

The Pulsar configuration parameters provided directly by the CAP:

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
ServiceUrl | Broker server address | string | 
TlsOptions | Tls configuration | object | 