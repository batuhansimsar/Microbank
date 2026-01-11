FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries  
COPY ["src/BuildingBlocks/Common/Common.csproj", "BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "BuildingBlocks/Common.Logging/"]
COPY ["src/BuildingBlocks/EventBus.MassTransit/EventBus.MassTransit.csproj", "BuildingBlocks/EventBus.MassTransit/"]

# Copy Transfer projects
COPY ["src/Services/Transfer/Transfer.Domain/Transfer.Domain.csproj", "Services/Transfer/Transfer.Domain/"]
COPY ["src/Services/Transfer/Transfer.API/Transfer.API.csproj", "Services/Transfer/Transfer.API/"]

# Restore dependencies
RUN dotnet restore "Services/Transfer/Transfer.API/Transfer.API.csproj"

# Copy all source code
COPY src/BuildingBlocks/Common/ BuildingBlocks/Common/
COPY src/BuildingBlocks/Common.Logging/ BuildingBlocks/Common.Logging/
COPY src/BuildingBlocks/EventBus.MassTransit/ BuildingBlocks/EventBus.MassTransit/
COPY src/Services/Transfer/Transfer.Domain/ Services/Transfer/Transfer.Domain/
COPY src/Services/Transfer/Transfer.API/ Services/Transfer/Transfer.API/

# Build
WORKDIR "/src/Services/Transfer/Transfer.API"
RUN dotnet build "Transfer.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Transfer.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Transfer.API.dll"]

