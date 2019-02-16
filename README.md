SkyAPM C#/.NET instrument agent
==========

<img src="https://skyapmtest.github.io/page-resources/SkyAPM/skyapm.png" alt="Sky Walking logo" height="90px" align="right" />

[Apache SkyWalking](https://github.com/apache/incubator-skywalking) is an APM designed for microservices, cloud native and container-based (Docker, K8s, Mesos) architectures. **SkyAPM-dotnet** provides the native support agent in C# and .NETStandard platform, with the helps from Apache SkyWalking committer team.

[![issues](https://img.shields.io/github/issues-raw/skyapm/skyapm-dotnet.svg?style=flat-square)](https://github.com/SkyAPM/SkyAPM-dotnet/issues)
[![pulls](https://img.shields.io/github/issues-pr-raw/skyapm/skyapm-dotnet.svg?style=flat-square)](https://github.com/SkyAPM/SkyAPM-dotnet/pulls)
[![releases](https://img.shields.io/github/release/skyapm/skyapm-dotnet.svg?style=flat-square)](https://github.com/SkyAPM/SkyAPM-dotnet/releases)
[![Gitter](https://img.shields.io/gitter/room/openskywalking/lobby.svg?style=flat-square)](https://gitter.im/openskywalking/Lobby)
[![Twitter Follow](https://img.shields.io/twitter/follow/asfskywalking.svg?style=flat-square&label=Follow&logo=twitter)](https://twitter.com/AsfSkyWalking)

## CI Build Status

| Platform | Build Server | Master Status  |
|--------- |------------- |---------|
| AppVeyor |  Windows/Linux |[![Build status](https://ci.appveyor.com/api/projects/status/fl6vucwfn1vu94dv/branch/master?svg=true)](https://ci.appveyor.com/project/wu-sheng/skywalking-csharp/branch/master)|

## Nuget Packages

| Package Name |  NuGet | MyGet | Downloads 
|--------------|  ------- |  ------- |  ---- 
| SkyAPM.Agent.AspNetCore | [![nuget](https://img.shields.io/nuget/v/SkyAPM.Agent.AspNetCore.svg?style=flat-square)](https://www.nuget.org/packages/SkyAPM.Agent.AspNetCore) | [![myget](https://img.shields.io/myget/skyapm-dotnet/v/SkyAPM.Agent.AspNetCore.svg?style=flat-square)](https://www.myget.org/feed/skyapm-dotnet/package/nuget/SkyAPM.Agent.AspNetCore) | [![stats](https://img.shields.io/nuget/dt/SkyAPM.Agent.AspNetCore.svg?style=flat-square)](https://www.nuget.org/stats/packages/SkyAPM.Agent.AspNetCore?groupby=Version) 
| SkyAPM.Agent.AspNet | [![nuget](https://img.shields.io/nuget/v/SkyAPM.Agent.AspNet.svg?style=flat-square)](https://www.nuget.org/packages/SkyAPM.Agent.AspNet) | [![myget](https://img.shields.io/myget/skyapm-dotnet/v/SkyAPM.Agent.AspNet.svg?style=flat-square)](https://www.myget.org/feed/skyapm-dotnet/package/nuget/SkyAPM.Agent.AspNet) | [![](https://img.shields.io/nuget/dt/SkyAPM.Agent.AspNet.svg?style=flat-square)](https://www.nuget.org/stats/packages/SkyAPM.Agent.AspNet?groupby=Version)  

> MyGet feed URL https://www.myget.org/F/skyapm-dotnet/api/v3/index.json

# Supported
- This project currently supports apps targeting netcoreapp2.0/netframework4.6.1 or higher.
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

You can run the following command to install the SkyWalking .NET Core Agent in your project.

```
dotnet add package SkyAPM.Agent.AspNetCore
```

## How to use
Set the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` environment variable to support the activation of the SkyAPM .NET Core Agent. 

- Add the assembly name of `SkyAPM.Agent.AspNetCore` to the `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` environment variable.

### Examples
- On windows

```
dotnet new mvc -n sampleapp
cd sampleapp

dotnet add package SkyAPM.Agent.AspNetCore

// enable SkyAPM.Agent.AspNetCore
set ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SkyAPM.Agent.AspNetCore

// set service_name
set SKYWALKING__SERVICENAME=sample_app

dotnet run
```

- On macOS/Linux

```
dotnet new mvc -n sampleapp
cd sampleapp

dotnet add package SkyAPM.Agent.AspNetCore

// enable SkyAPM.Agent.AspNetCore
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=SkyAPM.Agent.AspNetCore

// set service_name
export SKYWALKING__SERVICENAME=sample_app

dotnet run
```

## Configuration

Install `SkyAPM.DotNet.CLI`

```
dotnet tool install -g SkyAPM.DotNet.CLI
```

Use `dotnet skywalking config [your_service_name] [your_servers]` to generate config file. 

Example

```
dotnet skyapm config sample_app 192.168.0.1:11800
```

# Roadmap
[What are we going to do next?](/docs/roadmap.md)

# Contributing
This section is in progress here: [Contributing to SkyAPM-dotnet](/CONTIBUTING.md)

# Contact Us
* Submit an issue
* [Gitter](https://gitter.im/openskywalking/Lobby) English
* QQ Group(cn): 392443393

# License
[Apache 2.0 License.](/LICENSE)
