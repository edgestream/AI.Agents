# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY . .
RUN dotnet restore Agents.slnx
RUN find samples -name '*.csproj' -print -exec dotnet restore {} \;
RUN find samples -name '*.csproj' -print -exec dotnet build {} -c Release --no-restore \;
RUN dotnet publish samples/FakeNews/FakeNews.csproj \
    -c Release \
    --no-build \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8888
COPY --from=build /app/publish .
EXPOSE 8888
ENTRYPOINT ["dotnet", "FakeNews.dll"]
