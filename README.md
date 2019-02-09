SkyWalking C#/.NET instrument agent
==========

[Apache SkyWalking](https://github.com/apache/incubator-skywalking) is an APM designed for microservices, cloud native and container-based (Docker, K8s, Mesos) architectures. **SkyWalking-netcore** provides the native support agent in C# and .NETStandard platform, with the helps from Apache SkyWalking committer team.

[![Twitter Follow](https://img.shields.io/twitter/follow/asfskywalking.svg?style=for-the-badge&label=Follow&logo=twitter)](https://twitter.com/AsfSkyWalking)

[![Build status](https://ci.appveyor.com/api/projects/status/fl6vucwfn1vu94dv/branch/master?svg=true)](https://ci.appveyor.com/project/wu-sheng/skywalking-csharp/branch/master)

# Supported
- This project currently supports apps targeting netcoreapp2.1 or higher.
- [Supported middlewares, frameworks and libraries.](docs/Supported-list.md)

# Features
A quick list of SkyWalking .NET Core Agent's capabilities
- Application Topology
- Distributed Tracing
- ASP.NET Core Diagnostics
- HttpClient Diagnostics
- EntityFrameworkCore Diagnostics

# Getting Started

## Deploy SkyWalking Collector

#### Requirements
- SkyWalking Collector 5.0.0-beta or higher. See SkyWalking backend deploy [docs](https://github.com/apache/incubator-skywalking/blob/5.x/docs/en/Deploy-backend-in-standalone-mode.md).
- SkyWalking 6 backend is compatible too. The deployment doc is [here](https://github.com/apache/incubator-skywalking/blob/master/docs/en/setup/backend/backend-ui-setup.md). If you are new user, recommand you to read the 
[whole official documents](https://github.com/apache/incubator-skywalking/blob/master/docs/README.md)

## Install SkyWalking .NET Core Agent

You can run the following command to install the SkyWalking .NET Core Agent in your computer.

```
// install SkyWalking DotNet CLI
dotnet tool install -g SkyWalking.DotNet.CLI
```
On windows, run as Administrator
```
dotnet skywalking install
```

On macOS/Linux
```
sudo dotnet skywalking install
```

## How to use
Set the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` and `DOTNET_ADDITIONAL_DEPS` environment variables to support the activation of the SkyWalking .NET Core Agent. 

- Add the assembly name of `SkyWalking.Agent.AspNetCore` to the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` environment variable.
- On Windows, set the `DOTNET_ADDITIONAL_DEPS` environment variable to `%PROGRAMFILES%\dotnet\x64\additionalDeps\skywalking.agent.aspnetcore`. On macOS/Linux, set the `DOTNET_ADDITIONAL_DEPS` environment variable to `/usr/local/share/dotnet/x64/additionalDeps/skywalking.agent.aspnetcore`.

### Examples
- On windows

```
dotnet new mvc -n sampleapp
cd sampleapp

// enable SkyWalking.Agent.AspNetCore
set ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SkyWalking.Agent.AspNetCore
set DOTNET_ADDITIONAL_DEPS=%PROGRAMFILES%\dotnet\x64\additionalDeps\skywalking.agent.aspnetcore

// set Application_Code
set SKYWALKING__APPLICATIONCODE=sample_app

dotnet run
```

- On macOS/Linux

```
dotnet new mvc -n sampleapp
cd sampleapp

// enable SkyWalking.Agent.AspNetCore
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SkyWalking.Agent.AspNetCore
export DOTNET_ADDITIONAL_DEPS=/usr/local/share/dotnet/x64/additionalDeps/skywalking.agent.aspnetcore

// set Application_Code
export SKYWALKING__APPLICATIONCODE=sample_app

dotnet run
```

## Configuration
Use `dotnet skywalking config [your_application_code] [your_collector_server]` to generate config file. Example

```
dotnet skywalking config sample_app 192.168.0.1:11800
```

# Roadmap
[What are we going to do next?](/docs/roadmap.md)

# Contributing
This section is in progress here: [Contributing to skywalking-netcore](/CONTIBUTING.md)

# Contact Us
* Submit an issue
* [Gitter](https://gitter.im/openskywalking/Lobby) English
* QQ Group(cn): 392443393

# License
[Apache 2.0 License.](/LICENSE)
