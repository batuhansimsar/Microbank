FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["src/BuildingBlocks/Common/Common.csproj", "BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "BuildingBlocks/Common.Logging/"]
COPY ["src/BuildingBlocks/EventBus.RabbitMQ/EventBus.RabbitMQ.csproj", "BuildingBlocks/EventBus.RabbitMQ/"]

# Copy Account projects
COPY ["src/Services/Account/Account.Domain/Account.Domain.csproj", "Services/Account/Account.Domain/"]
COPY ["src/Services/Account/Account.API/Account.API.csproj", "Services/Account/Account.API/"]

# Restore dependencies
RUN dotnet restore "Services/Account/Account.API/Account.API.csproj"

# Copy all source code
COPY src/BuildingBlocks/Common/ BuildingBlocks/Common/
COPY src/BuildingBlocks/Common.Logging/ BuildingBlocks/Common.Logging/
COPY src/BuildingBlocks/EventBus.RabbitMQ/ BuildingBlocks/EventBus.RabbitMQ/
COPY src/Services/Account/Account.Domain/ Services/Account/Account.Domain/
COPY src/Services/Account/Account.API/ Services/Account/Account.API/

# Build
WORKDIR "/src/Services/Account/Account.API"
RUN dotnet build "Account.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Account.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Account.API.dll"]

