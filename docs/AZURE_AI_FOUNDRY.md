# Azure AI Foundry

## Prerequisites

- Docker Desktop running
- Azure CLI logged in (`az login`)
- The shared `.env` file from the team secret store

## Setup

**1. Create `appsettings.Development.json` at the repository root:**

```json
{
  "Foundry": {
    "ProjectEndpoint": "https://<ai-services-name>.services.ai.azure.com/api/projects/<project-name>",
    "Model": "<model-deployment-name>"
  }
}
```

**2. Place the `.env` file at the repository root** (never commit it — it's in `.gitignore`):

```dotenv
AZURE_TENANT_ID=<tenant-id>
AZURE_CLIENT_ID=<client-id>
AZURE_CLIENT_SECRET=<client-secret>
```

**3. Start the stack:**

```bash
docker compose up -d --build
```

Open **http://localhost:3000** once all containers are healthy.

## Operations

### Rotate the client secret (expires annually)

```bash
az ad app credential reset --id <AZURE_CLIENT_ID> --years 1 --query "{clientSecret:password}" -o tsv
```

Distribute the new secret to the team via the shared secret store.

### Re-assign the Foundry role

```bash
az role assignment create \
  --assignee <AZURE_CLIENT_ID> \
  --role "Azure AI Developer" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.CognitiveServices/accounts/<ai-services-name>"
```
