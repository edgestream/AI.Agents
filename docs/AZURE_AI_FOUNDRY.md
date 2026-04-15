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
./scripts/init-env.ps1 -VaultName <vault-name>
```

**3. Start the stack:**

```bash
docker compose up -d --build
```

Open **http://localhost:3000** once all containers are healthy.
