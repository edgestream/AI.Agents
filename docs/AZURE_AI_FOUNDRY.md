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
# Optional: set AGENTS_KEYVAULT once so the script can omit -VaultName.
$env:AGENTS_KEYVAULT = '<vault-name>'

./scripts/Get-KeyVault-Environment.ps1 -VaultName <vault-name>
```

**3. Start the stack:**

```bash
docker compose up -d --build
```

Open **http://localhost:3000** once all containers are healthy.

## Azure deployment

The Azure deployment uses the strict naming pattern `<prefix>-agents-<environment>`. For the shared stage environment this means:

- `rg-agents-stage`
- `log-agents-stage`
- `cae-agents-stage`
- `ca-agents-stage`

Because the backend service is published through GHCR, authenticate to GitHub Container Registry before `azd deploy` / `azd up` if you are pushing from a fresh machine.

```powershell
# Optional when publishing to GHCR from a fresh machine
docker login ghcr.io

# Create or select the stage environment
azd env new Stage
azd up
```

Make sure `appsettings.Stage.json` exists in the repo root before running `azd up`. The combined Container App still runs both `backend` and `frontend` containers; `azd` packages the backend image from source and the frontend image continues to be managed through the `frontendImage` infrastructure parameter and rollout workflow.
