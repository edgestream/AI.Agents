# Azure Container Apps

This document covers the Azure Container Apps deployment surface for AI.Agents.

## Deployment Surface

The Azure Container Apps deployment is defined in:

- `azure.yaml`
- `infra/main.bicep`
- `infra/main.bicepparam`
- `infra/resources.bicep`
- `src/Server/Dockerfile`
- `.github/workflows/cd.yml`

The deployed product is a single Container App with two containers in one replica:

- `frontend` serves the Next.js UI on port `3000`
- `backend` serves the ASP.NET Core API on port `8080`

The current Stage rollout assumes the GHCR packages are public. The Container App does not configure registry credentials for `ghcr.io`, so private packages will fail to pull until registry auth support is added back intentionally.

Runtime authentication is handled by Container Apps built-in auth. See [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md) for the Entra app registrations and permissions.

## Stage Environment

The shared hosted environment currently uses:

- azd environment name: `Stage`
- settings file: `appsettings.Stage.json`
- naming pattern: `<prefix>-agents-<environment>`

Concrete stage resource names:

- `rg-agents-stage`
- `log-agents-stage`
- `cae-agents-stage`
- `ca-agents-stage`

## First Provision

### 1. Create the stage settings file

Create `appsettings.Stage.json` in the repository root. For the Foundry or Azure OpenAI payload, see [AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md).

### 2. Create or select the azd environment

```powershell
azd auth login
azd env new Stage
```

If it already exists:

```powershell
azd env select Stage
```

### 3. Set the Container Apps environment values

Required runtime auth values:

```powershell
azd env set ENTRA_CLIENT_ID <app-registration-client-id>
azd env set ENTRA_CLIENT_SECRET <client-secret>
azd env set ENTRA_TENANT_ID <tenant-id>
```

Optional token store:

```powershell
azd env set TOKEN_STORE_SAS_URL <full-container-sas-url>
```

The value must be a full container SAS URL in this format:

```text
https://<storage-account>.blob.core.windows.net/<container>?<sas-query-string>
```

Example PowerShell:

```powershell
$accountName = '<storage-account>'
$resourceGroup = '<storage-resource-group>'
$containerName = '<private-container>'
$key = (az storage account keys list -g $resourceGroup -n $accountName --query '[0].value' -o tsv).Trim()
$sas = (az storage container generate-sas --account-name $accountName --account-key $key --name $containerName --permissions rwdl --expiry ((Get-Date).ToUniversalTime().AddYears(1).ToString('yyyy-MM-ddTHH:mmZ')) --https-only -o tsv).Trim()
$sasUrl = "https://$accountName.blob.core.windows.net/$containerName`?$sas"
azd env set TOKEN_STORE_SAS_URL $sasUrl
```

In practice, set `TOKEN_STORE_SAS_URL` for the hosted Stage environment if you want ACA Easy Auth to persist provider tokens and forward `X-MS-TOKEN-AAD-ACCESS-TOKEN` to the backend. Without a token store, the app can still authenticate users, but Graph-backed profile enrichment such as profile photo retrieval is skipped because no Graph access token is forwarded.

Optional image overrides:

```powershell
azd env set BACKEND_IMAGE ghcr.io/edgestream/agents-server:<tag>
azd env set FRONTEND_IMAGE ghcr.io/edgestream/agents-web:<tag>
```

### 4. Authenticate to GHCR locally when you need to push or inspect images

```powershell
docker login ghcr.io
```

### 5. Provision and deploy

```powershell
azd up -e Stage
```

After the first successful deployment, capture the hosted `APP_URI` from the azd output and make sure the runtime Entra app registration includes this callback URI:

```text
https://<stage-app-fqdn>/.auth/login/aad/callback
```

See [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md) for the app registration details.

If you add or change the token store after users have already signed in, sign out and sign in again so a fresh Easy Auth session is created with persisted provider tokens.

## Image Model

- `backend` is packaged from source by `azd` using `src/Server/Dockerfile`
- `frontend` is supplied through the `FRONTEND_IMAGE` Bicep parameter and updated by the CD workflow
- default images are:
  - `ghcr.io/edgestream/agents-server:latest`
  - `ghcr.io/edgestream/agents-web:latest`

## Provision vs Deploy

Use `azd provision -e Stage` when Bicep, auth configuration, secrets, mounted settings, or container references change.

```powershell
azd provision -e Stage
```

Use `azd deploy -e Stage` when only the declared backend service needs to be repackaged from source.

```powershell
azd deploy -e Stage
```

Use `azd up -e Stage` for the full flow in one command.

The GitHub CD workflow updates both frontend and backend containers directly by `sha-<7>` image tags after CI publishes images. See [GITHUB_REPOSITORY.md](GITHUB_REPOSITORY.md).

If you manually change `FRONTEND_IMAGE` in the `Stage` azd environment, verify the live image afterwards:

```powershell
az containerapp show --name ca-agents-stage --resource-group rg-agents-stage --query "properties.template.containers[?name=='frontend'].image | [0]" -o tsv
```

During the Stage rollout, a direct `az containerapp update --container-name frontend --image <tag>` was needed once to force the live frontend to the expected tag after a manual image publish.

## Verification

After deployment:

1. Inspect the environment values:

```powershell
azd env get-values -e Stage
```

2. Open the `APP_URI` in a browser.
3. Sign in through Container Apps Easy Auth.
4. Confirm the chat UI loads and can reach the backend.
5. Confirm `/api/me` returns an authenticated user profile.

## Token Store

If `TOKEN_STORE_SAS_URL` is set, Container Apps Easy Auth uses a blob-backed token store so sign-in tokens survive restarts and scale events. Leave it unset only when you accept ephemeral tokens for simpler dev or test environments.

## User Profile Contract

Treat the backend `/api/me` response as the only source of truth for user profile presentation.

- `authenticated: true` does not guarantee `displayName` or `picture`
- `displayName` must come from backend Graph enrichment, not from `principalName`, email, or decoded JWT claims
- if Graph enrichment is unavailable, the UI stays authenticated but does not invent a human-readable name

## Related Docs

- [AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md)
- [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md)
- [GITHUB_REPOSITORY.md](GITHUB_REPOSITORY.md)