# Apache Pulsar

[Apache Pulsar](https://pulsar.apache.org/) 是一个用于服务器到服务器的消息系统，具有多租户、高性能等优势。 Pulsar 最初由 Yahoo 开发，目前由 Apache 软件基金会管理。

CAP 支持使用 Apache Pulsar 作为消息传输器。

## Configuration

要使用 Pulsar 作为消息传输器，你需要从 NuGet 安装以下扩展包：

```shell

Install-Package DotNetCore.CAP.Pulsar

```

然后，你可以在 `Startup.cs` 的 `ConfigureServices` 方法中添加基于 Pulsar 的配置项。

```csharp

public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddCap(x =>
    {
        x.UsePulsar(opt => {
            //Pulsar Options
        });
        // x.UseXXX ...
    });
}

```

#### Pulsar Options

CAP 直接对外提供的 Pulsar 配置参数如下：

NAME | DESCRIPTION | TYPE | DEFAULT
:---|:---|---|:---
ServiceUrl | Broker 地址 | string | 
EnableClientLog | 是否通过 CAP 日志记录 Pulsar 客户端日志 | bool | false
TlsOptions | TLS 和身份验证配置，详见下表 | `TlsOptions` | null

`TlsOptions` 提供以下设置；未另行说明时，默认值来自 Pulsar 客户端库：

| 属性 | 说明 | 类型 |
| :--- | :--- | :--- |
| `UseTls` | 是否启用 TLS。 | bool |
| `TlsHostnameVerificationEnable` | 是否根据证书验证 Broker 主机名。 | bool |
| `TlsAllowInsecureConnection` | 是否允许不安全的 TLS 连接。 | bool |
| `TlsTrustCertificate` | 用于验证 Broker 的受信任证书。 | `X509Certificate2` |
| `Authentication` | Pulsar 客户端身份验证配置。 | `Authentication` |
| `TlsProtocols` | 客户端接受的 TLS 协议版本。 | `SslProtocols` |

当前 CAP Pulsar 连接工厂不会读取 `TlsOptions.UseTls`；TLS 行为由 Pulsar 服务 URL 决定。提供 `TlsOptions` 时，表中其余设置会传递给 Pulsar 客户端。
