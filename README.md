SkyWalking C#/.NET instrument agent
==========

<img src="https://skywalkingtest.github.io/page-resources/3.0/skywalking.png" alt="Sky Walking logo" height="90px" align="right" />

[Apache SkyWalking](https://github.com/apache/incubator-skywalking) is an APM designed for microservices, cloud native and container-based (Docker, K8s, Mesos) architectures. **SkyWalking-netcore** provides the native support agent in C# and .NETStandard platform, with the helps from Apache SkyWalking committer team.

[![Twitter Follow](https://img.shields.io/twitter/follow/asfskywalking.svg?style=for-the-badge&label=Follow&logo=twitter)](https://twitter.com/AsfSkyWalking)

[![Build status](https://ci.appveyor.com/api/projects/status/fl6vucwfn1vu94dv/branch/master?svg=true)](https://ci.appveyor.com/project/wu-sheng/skywalking-csharp/branch/master)

# Supported
- This project currently supports apps targeting netstandard2.0 or higher.
- [Supported middlewares, frameworks and libraries.](docs/Supported-list.md)

# Features
A quick list of SkyWalking .NET Core Agent's capabilities
- Application Topology
- Distributed Tracing
- ASP.NET Core Diagnostics
- HttpClientFactory Diagnostics

# Getting Started

### Deploy SkyWalking Collector

#### Requirements
- SkyWalking Collector 5.0.0-beta or higher. [Download](https://github.com/apache/incubator-skywalking/releases)

### Install SkyWalking .NET Core Agent

You can run the following command to install the SkyWalking .NET Core Agent in your project.

```
PM> Install-Package SkyWalking.AspNetCore
```

### Configuration
First,You need to config SkyWalking in your Startup.cs：
```
public void ConfigureServices(IServiceCollection services)
{
    //......

    services.AddSkyWalking(option =>
    {
        // Application code is showed in sky-walking-ui
        option.ApplicationCode = "Your_ApplicationName";

        //Collector agent_gRPC/grpc service addresses.
        option.DirectServers = "localhost:11800";
        
    });
}
```

# Contributing
This section is in progress here: [Contributing to skywalking-netcore](/CONTIBUTING.md)

# Roadmap
- Expect to release 0.2 compatible in May. 2018
- Expect to release 0.3 compatible in June. 2018

# Contact Us
* Submit an issue
* [Gitter](https://gitter.im/openskywalking/Lobby) English
* QQ Group(cn): 392443393

# License
[Apache 2.0 License.](/LICENSE)
