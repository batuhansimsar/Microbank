FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["src/BuildingBlocks/Common/Common.csproj", "BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "BuildingBlocks/Common.Logging/"]
COPY ["src/BuildingBlocks/EventBus.MassTransit/EventBus.MassTransit.csproj", "BuildingBlocks/EventBus.MassTransit/"]

# Copy Notification project
COPY ["src/Services/Notification/Notification.API/Notification.API.csproj", "Services/Notification/Notification.API/"]

# Restore dependencies
RUN dotnet restore "Services/Notification/Notification.API/Notification.API.csproj"

# Copy all source code
COPY src/BuildingBlocks/Common/ BuildingBlocks/Common/
COPY src/BuildingBlocks/Common.Logging/ BuildingBlocks/Common.Logging/
COPY src/BuildingBlocks/EventBus.MassTransit/ BuildingBlocks/EventBus.MassTransit/
COPY src/Services/Notification/Notification.API/ Services/Notification/Notification.API/

# Build
WORKDIR "/src/Services/Notification/Notification.API"
RUN dotnet build "Notification.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Notification.API.dll"]

