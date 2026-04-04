# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj src/AI.Web.AGUIServer/
COPY src/AI.Client/AI.Client.csproj src/AI.Client/
COPY src/AI.MCP.Client/AI.MCP.Client.csproj src/AI.MCP.Client/
RUN dotnet restore src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj
COPY . .
RUN dotnet publish src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AI.Web.AGUIServer.dll"]
