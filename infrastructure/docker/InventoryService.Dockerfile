FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/EdaDemo.Contracts/ EdaDemo.Contracts/
COPY src/EdaDemo.Messaging/ EdaDemo.Messaging/
COPY src/InventoryService/ InventoryService/
RUN dotnet restore InventoryService/InventoryService.csproj
RUN dotnet publish InventoryService/InventoryService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryService.dll"]
