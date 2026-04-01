# AI.Web
> Conversational [AG-UI](https://docs.ag-ui.com/introduction) web interface and backend services

![image](https://mintcdn.com/tawkitai/-0mlsyK2_Ht4cjV3/images/ag-ui-overview-with-partners-dark.png?w=1650&fit=max&auto=format&n=-0mlsyK2_Ht4cjV3&q=85&s=e581c90e6ee93decb9151cb92355c435)

## Quickstart

The backend requires Azure OpenAI credentials. Copy the provided template and fill in your values:

```.env
AZURE_OPENAI_ENDPOINT=https://<your-resource>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=<your-deployment>
AZURE_OPENAI_API_KEY=<your-api-key>
```

Start the services:

```bash
docker compose up
```

Open the frontend at: http://localhost:3000

Stop the services:

```bash
docker compose down
```

## Build

### Run locally (without docker)

The backend requires Azure OpenAI credentials. Use the [.NET Secret Manager](https://learn.microsoft.com/aspnet/core/security/app-secrets)
to store them outside the repository:

```bash
cd src/AI.Web.AGUIServer
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "<your-deployment>"
```

Secrets are stored in your user profile and loaded automatically when
`ASPNETCORE_ENVIRONMENT` is `Development` (the default for `dotnet run`).

```bash
dotnet run
```

The backend listens on http://localhost:8000.

The frontend reads `BACKEND_URL` from the environment. It defaults to
`http://localhost:8000/`, which matches the backend's local address above.

```bash
cd src/AI.Web.AGUIChat
npm ci
npm run dev
```

To override it, create `src/AI.Web.AGUIChat/.env.local`:

```bash
BACKEND_URL=http://localhost:8000/
```

### Running tests

```bash
dotnet test
```

### Running E2E tests

E2E tests use [Playwright](https://playwright.dev/dotnet/) and live in `tests/AI.Web.E2ETests`. The backend is started automatically by the test assembly setup via `WebApplicationFactory<Program>` with `FakeChatClient` — no Azure credentials and no extra backend container are needed. Only the Next.js frontend must be running externally.

#### 1. Build and install browsers (once)

```bash
dotnet build tests/AI.Web.E2ETests
pwsh tests/AI.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

#### 2. Start the frontend

**Option A – Docker (recommended for CI):**

```bash
docker compose -f docker-compose.e2e.yml up --build -d
```

**Option B – locally with npm:**

```bash
cd src/AI.Web.AGUIChat
BACKEND_URL=http://localhost:8080/ npm run dev
```

#### 3. Run the tests

```bash
# Title / input visibility tests (no backend required):
dotnet test tests/AI.Web.E2ETests

# Full send-message test (requires frontend to be running):
E2E_LIVE_TEST=true dotnet test tests/AI.Web.E2ETests
```

#### 4. Tear down the frontend (if using Docker)

```bash
docker compose -f docker-compose.e2e.yml down
```

#### Configuration

| Environment variable | Default                  | Description                                                                 |
|----------------------|--------------------------|-----------------------------------------------------------------------------|
| `E2E_BASE_URL`       | `http://localhost:3000`  | Base URL of the running frontend.                                           |
| `E2E_LIVE_TEST`      | *(unset)*                | Set to `true` to run the send-message test (requires a running frontend).   |