# AI.Agents

> Conversational [AG-UI](https://docs.ag-ui.com/introduction) web interface and backend server

![image](https://mintcdn.com/tawkitai/-0mlsyK2_Ht4cjV3/images/ag-ui-overview-with-partners-dark.png?w=1650&fit=max&auto=format&n=-0mlsyK2_Ht4cjV3&q=85&s=e581c90e6ee93decb9151cb92355c435)

## Quickstart

Copy the application settings example into `appsettings.Development.json` in the repository root and fill in your Azure OpenAI credentials:

```appsettings.Development.json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "<your-deployment-name>",
    "ApiKey": "<your-api-key>"
  }
}
```

Start the services:

```bash
docker compose up --build
```

Open the frontend at http://localhost:3000.

Stop the services:

```bash
docker compose down
```

## Development

### Running local

For a native backend run, create `src/Server/appsettings.Development.json` or use user secrets, then start the backend:

```bash
dotnet run --project src/Server
```

The repository-root `appsettings.Development.json` is still used by `docker compose` via the mounted secret file.

Open another console and start the frontend:

```bash
cd src/Web
npm run dev
```

### Running tests

Tests are grouped into categories to make it easy to run only what you need:

| Category        | Description                                                                |
|-----------------|----------------------------------------------------------------------------|
| *(default)*     | Unit and fake-backed integration tests — no external services required.    |
| `ExternalDependency` | Tests that require a running stack, internet access, or other external systems. |
| `Live`          | Tests that call real websites or cloud services instead of local fakes.    |

Run tests which don't need an internet connection or external services:

```bash
# Run all default tests:
dotnet test --filter "TestCategory!=ExternalDependency&TestCategory!=Live"
```

End-to-end tests use [Playwright](https://playwright.dev/dotnet/). We have to install a headless browser first (once):

```bash
tests/E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

These tests are **black-box** tests - no server is started or managed by the test project. The full stack (frontend + backend) must be running before executing the tests:

```bash
# Against the local stack:
dotnet test tests/E2ETests

# Against a remote environment:
E2E_BASE_URL=https://staging.example.com dotnet test tests/E2ETests
```

When a test fails a trace zip is automatically saved next to the test assembly and you can open it in the Playwright Trace Viewer:

```bash
npx playwright show-trace tests/E2ETests/bin/Debug/net10.0/traces/ChatPage_CanSendMessage_ReceivesResponse.zip
```

## Deployment

Deployment documentation is split by service:

- [docs/AZURE_CONTAINER_APPS.md](docs/AZURE_CONTAINER_APPS.md)
- [docs/AZURE_AI_FOUNDRY.md](docs/AZURE_AI_FOUNDRY.md)
- [docs/AZURE_ENTRA_APP_REGISTRATION.md](docs/AZURE_ENTRA_APP_REGISTRATION.md)
- [docs/GITHUB_REPOSITORY.md](docs/GITHUB_REPOSITORY.md)
