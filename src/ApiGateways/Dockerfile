FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "BuildingBlocks/Common.Logging/"]

# Copy Gateway project
COPY ["src/ApiGateways/Gateway.API/Gateway.API.csproj", "ApiGateways/Gateway.API/"]

# Restore dependencies
RUN dotnet restore "ApiGateways/Gateway.API/Gateway.API.csproj"

# Copy all source code
COPY src/BuildingBlocks/Common.Logging/ BuildingBlocks/Common.Logging/
COPY src/ApiGateways/Gateway.API/ ApiGateways/Gateway.API/

# Build
WORKDIR "/src/ApiGateways/Gateway.API"
RUN dotnet build "Gateway.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Gateway.API.dll"]
