FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries
COPY ["src/BuildingBlocks/Common/Common.csproj", "BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "BuildingBlocks/Common.Logging/"]

# Copy service project
COPY ["src/Services/Identity/Identity.API/Identity.API.csproj", "Services/Identity/Identity.API/"]

# Restore dependencies
RUN dotnet restore "Services/Identity/Identity.API/Identity.API.csproj"

# Copy all source code
COPY src/BuildingBlocks/Common/ BuildingBlocks/Common/
COPY src/BuildingBlocks/Common.Logging/ BuildingBlocks/Common.Logging/
COPY src/Services/Identity/Identity.API/ Services/Identity/Identity.API/

# Build
WORKDIR "/src/Services/Identity/Identity.API"
RUN dotnet build "Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Identity.API.dll"]

