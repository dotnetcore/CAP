# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Build.props", "src/"]
COPY ["samples/Samples.Redis.SqlServer/Samples.Redis.SqlServer.csproj", "samples/Samples.Redis.SqlServer/"]
COPY ["src/DotNetCore.CAP.RedisStreams/DotNetCore.CAP.RedisStreams.csproj", "src/DotNetCore.CAP.RedisStreams/"]
COPY ["src/DotNetCore.CAP/DotNetCore.CAP.csproj", "src/DotNetCore.CAP/"]
COPY ["src/DotNetCore.CAP.SqlServer/DotNetCore.CAP.SqlServer.csproj", "src/DotNetCore.CAP.SqlServer/"]
RUN dotnet restore "./samples/Samples.Redis.SqlServer/Samples.Redis.SqlServer.csproj"
COPY . .
WORKDIR "/src/samples/Samples.Redis.SqlServer"
RUN dotnet build "./Samples.Redis.SqlServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Samples.Redis.SqlServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Samples.Redis.SqlServer.dll"]