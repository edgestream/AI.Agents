# Azure AI Foundry

## Prerequisites

- Docker Desktop running
- Azure CLI logged in (`az login`)
- `Key Vault Secrets User` role on the shared dev vault (request from a team owner)

## Setup

**1. Create `appsettings.Development.json` at the repository root:**

```json
{
  "Foundry": {
    "Endpoint": "https://<ai-services-name>.services.ai.azure.com/api/projects/<project-name>"
  }
}
```

**2. Pull credentials from Key Vault into `.env`** (never commit it — it's in `.gitignore`):

```powershell
# Optional: set AI_AGENTS_KEY_VAULT once so the script can omit -VaultName.
$env:AI_AGENTS_KEY_VAULT = '<vault-name>'

./scripts/init-env.ps1 -VaultName <vault-name>
```

**3. Start the stack:**

```bash
docker compose up -d --build
```

Open **http://localhost:3000** once all containers are healthy.

## Azure deployment

The infrastructure now defaults to `ai-agents` resource names. During migration you can still target the legacy `ai-agui` resource names by setting `RESOURCE_PREFIX=ai-agui` in the azd environment before running `azd up`.

Because the backend service is published through GHCR, authenticate to GitHub Container Registry before `azd deploy` / `azd up` if you are pushing from a fresh machine.

```powershell
# Optional when publishing to GHCR from a fresh machine
docker login ghcr.io

# Optional migration override when reusing pre-rename Azure resources
azd env set RESOURCE_PREFIX ai-agui

azd up
```

If you omit `RESOURCE_PREFIX`, azd provisions the renamed resources such as `rg-ai-agents-<environment>` and `ca-ai-agents`. The combined Container App still runs both `backend` and `frontend` containers; `azd` packages the backend image from source and the frontend image continues to be managed through the `frontendImage` infrastructure parameter and rollout workflow.
