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
# Install curl (healthcheck) and the Azure CLI (enables AzureCliCredential via DefaultAzureCredential
# when the developer's ~/.azure token cache is bind-mounted at runtime).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates lsb-release gnupg \
    && curl -sLS https://packages.microsoft.com/keys/microsoft.asc \
       | gpg --dearmor > /etc/apt/keyrings/microsoft.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/microsoft.gpg] \
       https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" \
       > /etc/apt/sources.list.d/azure-cli.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends azure-cli \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AI.Web.AGUIServer.dll"]
