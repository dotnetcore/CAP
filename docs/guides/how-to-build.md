# How to build project
This document helps people to compile and build the project.

## Build Project
**Because we are using Git submodule, we recommend don't use `GitHub` tag or release page to download source codes for compiling.**

### Build from GitHub
- Prepare git and .NET Core SDK.
- `git clone https://github.com/SkyAPM/SkyAPM-dotnet.git`
- `cd SkyAPM-dotnet/`
- Switch to the tag by using `git checkout [tagname]` (Optional, switch if want to build a release from source codes)
- `git submodule init`
- `git submodule update`
- Run `dotnet restore`
- Run `dotnet build src/SkyApm.Transport.Grpc.Protocol`
- Run `dotnet build skyapm-dotnet.sln`