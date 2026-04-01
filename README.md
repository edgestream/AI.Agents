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

## Deploy to Azure (Container Apps)

The entire stack can be provisioned and deployed to Azure Container Apps with a single command using the [Azure Developer CLI (`azd`)](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview).

### One-time prerequisites

| Prerequisite | Install |
|---|---|
| Azure subscription | [Free account](https://azure.microsoft.com/free/) |
| Azure Developer CLI | `winget install Microsoft.Azd` / `brew install azd` / [other options](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) |
| Azure CLI | `winget install Microsoft.AzureCLI` / `brew install azure-cli` |
| GHCR pull token | GitHub → Settings → Developer settings → Personal access tokens → `read:packages` scope |

Log in to Azure:

```bash
azd auth login
```

### Provision and deploy

Set your Azure OpenAI credentials and GHCR pull token, then spin up the whole stack:

```bash
azd env new dev
azd env set AZURE_OPENAI_ENDPOINT   "https://<your-resource>.openai.azure.com/"
azd env set AZURE_OPENAI_DEPLOYMENT_NAME "<your-deployment>"
azd env set GHCR_USERNAME "<your-github-username>"
azd env set GHCR_TOKEN   "<your-ghcr-pat>"
azd up
```

`azd up` will:
1. Create the resource group `rg-ai-web-dev` in your chosen region
2. Provision a Log Analytics workspace, Container Apps Environment, and two Container Apps
3. Build the Docker images from the local Dockerfiles and push them to GHCR
4. Deploy the images to the provisioned Container Apps

The frontend URL is printed at the end of `azd up`.

### Tear down

```bash
azd down
```

### Overriding image tags (CD pipeline)

To deploy a specific image (e.g. after CI pushes `sha-abc1234`):

```bash
azd env set BACKEND_IMAGE  "ghcr.io/edgestream/ai-web-aguiserver:sha-abc1234"
azd env set FRONTEND_IMAGE "ghcr.io/edgestream/ai-web-aguichat:sha-abc1234"
azd deploy
```

### Resource naming

All provisioned resources follow the `<abbreviation>-ai-web-<env>` convention:

| Resource | Name |
|---|---|
| Resource Group | `rg-ai-web-<env>` |
| Log Analytics Workspace | `log-ai-web-<env>` |
| Container Apps Environment | `cae-ai-web-<env>` |
| Backend Container App | `ca-ai-web-backend` |
| Frontend Container App | `ca-ai-web-frontend` |

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