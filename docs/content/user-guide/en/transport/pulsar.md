# Apache Pulsar

[Apache Pulsar](https://pulsar.apache.org/) is a cloud-native, distributed messaging and streaming platform originally created at Yahoo! and now a top-level Apache Software Foundation project.

Pulsar can be used in CAP as a message transporter. 

## Configuration

To use Pulsar as a transporter, you need to install the following package from NuGet:

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
EnableClientLog | Enable Pulsar client logs through the CAP logger. | bool | false
TlsOptions | TLS and authentication settings. See the properties below. | `TlsOptions` | null

`TlsOptions` exposes these settings; unless specified otherwise, values come from the Pulsar client library defaults:

| Property | Description | Type |
| :--- | :--- | :--- |
| `UseTls` | Whether TLS is enabled. | bool |
| `TlsHostnameVerificationEnable` | Verify the broker hostname against its certificate. | bool |
| `TlsAllowInsecureConnection` | Allow an insecure TLS connection. | bool |
| `TlsTrustCertificate` | Trust certificate used to validate the broker. | `X509Certificate2` |
| `Authentication` | Pulsar client authentication configuration. | `Authentication` |
| `TlsProtocols` | TLS protocol versions accepted by the client. | `SslProtocols` |

The current CAP Pulsar connection factory does not read `TlsOptions.UseTls`; TLS behavior is determined by the Pulsar service URL. The other listed settings are passed to the Pulsar client when `TlsOptions` is supplied.
