# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json ./
COPY src/Directory.Build.props src/
COPY src/Directory.Packages.props src/
COPY src/Abstractions/Abstractions.csproj src/Abstractions/
COPY src/AGUI/AGUI.csproj src/AGUI/
COPY src/Microsoft/Microsoft.csproj src/Microsoft/
COPY src/MCP/MCP.csproj src/MCP/
COPY src/OAuth/OAuth.csproj src/OAuth/
COPY src/Server/Server.csproj src/Server/
RUN dotnet restore src/Server/Server.csproj
COPY . .
RUN dotnet publish src/Server/Server.csproj \
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
ENTRYPOINT ["dotnet", "AI.Agents.Server.dll"]
