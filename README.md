# AI.Web
> Conversational [AG-UI](https://docs.ag-ui.com/introduction) web interface and backend services

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
docker compose up
```

Open the frontend at http://localhost:3000.

Stop the services:

```bash
docker compose down
```

## Development

### Running local

Copy `appsettings.Development.json` from the repository root into `src/AI.Web.AGUIServer/` then start the backend:

```bash
dotnet run --project src/AI.Web.AGUIServer
```

Open another console and start the frontend:

```bash
cd src/AI.Web.AGUIChat
npm run dev
```

### Running tests

Tests are grouped into categories to make it easy to run only what you need:

| Category        | Description                                                                |
|-----------------|----------------------------------------------------------------------------|
| *(default)*     | Unit and fake-backed integration tests — no external services required.    |
| `Live`          | Tests that require an internet connection or configured Azure credentials. |
| `Integration`   | Tests that require a service container (e.g. running Next.js frontend).    |

Run tests which don't need an internet connection or external services:

```bash
# Run all default tests:
dotnet test --filter "TestCategory!=Integration&TestCategory!=Live"
```

End-to-end tests use [Playwright](https://playwright.dev/dotnet/). We have to install a headless browser first (once):

```bash
tests/AI.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

This tests are **black-box** tests - no server is started or managed by the test project. The full stack (frontend + backend) must be running before executing the tests:

```bash
# Against the local stack:
dotnet test tests/AI.Web.E2ETests

# Against a remote environment:
E2E_BASE_URL=https://staging.example.com dotnet test tests/AI.Web.E2ETests
```

When a test fails a trace zip is automatically saved next to the test assembly and you can open it in the Playwright Trace Viewer:

```bash
npx playwright show-trace tests/AI.Web.E2ETests/bin/Debug/net10.0/traces/ChatPage_CanSendMessage_ReceivesResponse.zip
```
