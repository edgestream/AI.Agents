# Azure AI Foundry

This document covers the Azure AI Foundry and Azure OpenAI configuration used by AI.Agents.

## Prerequisites

- Docker Desktop running
- Azure CLI logged in (`az login`)
- `Key Vault Secrets User` role on the shared dev vault if you use the bootstrap script

## Local Setup

### 1. Create `appsettings.Development.json`

Start from `appsettings.json.example` and add your AI configuration in the repository root.

Minimal Foundry example:

```json
{
  "Foundry": {
    "Endpoint": "https://<ai-services-name>.services.ai.azure.com/api/projects/<project-name>"
  }
}
```

Alternative Azure OpenAI example:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "<deployment-name>",
    "ApiKey": "<api-key>"
  }
}
```

### 2. Pull Entra credentials from Key Vault into `.env`

Never commit `.env`.

```powershell
# Optional: set AGENTS_KEYVAULT once so the script can omit -VaultName.
$env:AGENTS_KEYVAULT = '<vault-name>'

./scripts/Get-KeyVault-Environment.ps1 -VaultName <vault-name>
```

### 3. Start the local stack

```bash
docker compose up -d --build
```

Open `http://localhost:3000` once all containers are healthy.

## Hosted Configuration

The hosted Container Apps environment loads `appsettings.Stage.json` during `azd provision` and `azd up`.

Minimal Foundry example:

```json
{
  "Foundry": {
    "Endpoint": "https://<ai-services-name>.services.ai.azure.com/api/projects/<project-name>"
  }
}
```

If you prefer Azure OpenAI environment variables over a mounted settings file, set:

```powershell
azd env set AZURE_OPENAI_ENDPOINT https://<your-resource>.openai.azure.com/
azd env set AZURE_OPENAI_DEPLOYMENT_NAME <deployment-name>
azd env set AZURE_OPENAI_API_KEY <api-key>
```

See [AZURE_CONTAINER_APPS.md](AZURE_CONTAINER_APPS.md) for the actual rollout flow.

## Role Assignment

The deployed Container App managed identity must have the `Azure AI User` role on the Azure AI Foundry or Azure AI resource used by the backend. Without that role, hosted requests to the model endpoint fail even when the app is otherwise configured correctly.

## Related Docs

- [AZURE_CONTAINER_APPS.md](AZURE_CONTAINER_APPS.md)
- [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md)
- [AZURE_ENTRA_LOCAL_AUTH.md](AZURE_ENTRA_LOCAL_AUTH.md)
