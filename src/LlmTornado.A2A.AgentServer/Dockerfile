# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create output directory for mounted files
RUN mkdir -p /app/output && chmod 755 /app/output

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["LlmTornado.A2A.AgentServer/LlmTornado.A2A.AgentServer.csproj", "LlmTornado.A2A.AgentServer/"]
RUN dotnet restore "./LlmTornado.A2A.AgentServer/LlmTornado.A2A.AgentServer.csproj"
COPY . .
WORKDIR "/src/LlmTornado.A2A.AgentServer"
RUN dotnet build "./LlmTornado.A2A.AgentServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LlmTornado.A2A.AgentServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

#install latest updates for linux
RUN apt-get update && apt-get install -y curl jq

ENTRYPOINT ["dotnet", "LlmTornado.A2A.AgentServer.dll"]