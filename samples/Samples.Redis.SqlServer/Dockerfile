#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["samples/Samples.Redis.SqlServer/Samples.Redis.SqlServer.csproj", "samples/Samples.Redis.SqlServer/"]
COPY ["src/DotNetCore.CAP.RedisStreams/DotNetCore.CAP.RedisStreams.csproj", "src/DotNetCore.CAP.RedisStreams/"]
COPY ["src/DotNetCore.CAP/DotNetCore.CAP.csproj", "src/DotNetCore.CAP/"]
COPY ["src/DotNetCore.CAP.SqlServer/DotNetCore.CAP.SqlServer.csproj", "src/DotNetCore.CAP.SqlServer/"]
RUN dotnet restore "samples/Samples.Redis.SqlServer/Samples.Redis.SqlServer.csproj"
COPY . .
WORKDIR "/src/samples/Samples.Redis.SqlServer"
RUN dotnet build "Samples.Redis.SqlServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Samples.Redis.SqlServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Samples.Redis.SqlServer.dll"]