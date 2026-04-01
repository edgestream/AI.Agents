# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj src/AI.Web.AGUIServer/
RUN dotnet restore src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj
COPY . .
RUN dotnet publish src/AI.Web.AGUIServer/AI.Web.AGUIServer.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AI.Web.AGUIServer.dll"]
