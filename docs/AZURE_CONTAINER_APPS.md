# Azure Container Apps

This document explains how to run AI.AGUI on Azure Container Apps with `azd`, how to use the deployed product, and what additional Microsoft Entra permissions are required for Microsoft Graph profile enrichment.

## Overview

AI.AGUI is deployed as a single Azure Container App with two containers in one replica:

- `frontend`: Next.js web UI exposed on port `3000`
- `backend`: ASP.NET Core AG-UI server exposed to the frontend over `http://localhost:8080`

The Azure deployment is defined in:

- `azure.yaml`
- `infra/main.bicep`
- `infra/main.bicepparam`
- `infra/resources.bicep`

The deployment uses Azure Container Apps built-in authentication with Microsoft Entra ID. The current auth configuration requests the following login scopes from Entra:

```text
openid offline_access profile https://graph.microsoft.com/User.Read
```

That extra `User.Read` scope is required so `/api/me` can enrich the signed-in user with Microsoft Graph profile data and photo.

## Prerequisites

- Azure subscription access with permission to provision resource groups and Azure Container Apps
- Azure CLI installed and logged in
- Azure Developer CLI (`azd`) installed
- An Azure AI Foundry project endpoint or Azure OpenAI configuration available in `appsettings.{Environment}.json`
- A Microsoft Entra app registration for the Container App authentication flow
- A client secret for that app registration

Optional but recommended:

- GitHub Container Registry access if you want to deploy custom image tags instead of `latest`

## Quickstart

By default, the infrastructure uses the published GHCR images configured in `infra/main.bicepparam`. You only need to override `BACKEND_IMAGE` or `FRONTEND_IMAGE` when you want to deploy a non-default tag.

### 1. Create the environment-specific app settings file

For a production-style deployment, create `appsettings.Production.json` at the repo root.

Minimal Foundry-based example:

```json
{
  "Foundry": {
    "Endpoint": "https://<ai-services-name>.services.ai.azure.com/api/projects/<project-name>"
  }
}
```

Alternative Azure OpenAI-based example:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "DeploymentName": "<deployment-name>",
    "ApiKey": "<api-key>"
  }
}
```

The `preprovision` hook in `azure.yaml` automatically loads `appsettings.{AZURE_ENV_NAME}.json` into the `APPSETTINGS_JSON` environment value before Bicep is applied.

### 2. Create or select an `azd` environment

```powershell
azd auth login
azd env new Production
```

If the environment already exists:

```powershell
azd env select Production
```

### 3. Set the required environment values

Set the Entra configuration used by Container Apps built-in auth:

```powershell
azd env set ENTRA_CLIENT_ID <app-registration-client-id>
azd env set ENTRA_CLIENT_SECRET <client-secret>
azd env set ENTRA_TENANT_ID <tenant-id>
```

If you want to override the default container tags:

```powershell
azd env set BACKEND_IMAGE ghcr.io/edgestream/ai-agui-server:<tag>
azd env set FRONTEND_IMAGE ghcr.io/edgestream/ai-agui-web:<tag>
```

If you prefer environment variables over `appsettings.{Environment}.json` for Azure OpenAI:

```powershell
azd env set AZURE_OPENAI_ENDPOINT https://<your-resource>.openai.azure.com/
azd env set AZURE_OPENAI_DEPLOYMENT_NAME <deployment-name>
azd env set AZURE_OPENAI_API_KEY <api-key>
```

### 4. Provision the infrastructure

Run this for the first deployment and any later infrastructure or auth-configuration changes:

```powershell
azd provision -e Production
```

This applies the Bicep in `infra/`, including:

- resource group
- Log Analytics workspace
- Container Apps environment
- single multi-container Azure Container App
- Container Apps auth configuration
- Entra client secret injection into the app secret store
- mounted `appsettings.Production.json` inside the backend container

### 5. Deploy application changes

Use `azd deploy` when you are only shipping application updates for the declared `backend` service.

```powershell
azd deploy -e Production
```

Use `azd up` when you want the full flow in one command:

```powershell
azd up -e Production
```

## When To Use `provision` vs `deploy`

In this repo, infrastructure changes live in Bicep, especially in `infra/resources.bicep`. That means you must run `azd provision` when you change:

- Container Apps authentication settings
- `loginParameters`
- ingress, secrets, probes, volumes, or container environment variables
- resource definitions or image references supplied through Bicep parameters

Run only `azd deploy` when you are redeploying application code for the declared `backend` service and the infrastructure definition has not changed.

The Microsoft Graph profile enrichment change required `azd provision` because the Container Apps auth config had to start requesting `https://graph.microsoft.com/User.Read`.

## Product Quickstart After Deployment

### 1. Get the application URL

Use the `APP_URI` output from `azd` or inspect the environment values:

```powershell
azd env get-values -e Production
```

Open the URL in a browser.

### 2. Sign in with Microsoft Entra ID

If Entra auth is configured, unauthenticated users are redirected to the Microsoft login page.

After sign-in:

- the frontend is served by the `frontend` container
- the AG-UI backend is served by the `backend` container
- the backend reads the authenticated user from Container Apps Easy Auth headers

### 3. Use the product

Typical verification flow:

1. Open the site.
2. Sign in.
3. Start a chat in the AG-UI interface.
4. Send a prompt to the configured backend agent.
5. Confirm the app returns a response.

### 4. Verify profile enrichment

The backend exposes `/api/me` for the signed-in user session. When everything is configured correctly, it returns:

- `authenticated = true`
- `displayName` from Microsoft Graph when available
- `email`
- `picture` as a data URL when the user has a Graph photo

If Microsoft Graph consent is missing, the product still works, but:

- display name can fall back to token claims
- profile photo is usually missing

## Microsoft Entra App Registration Setup

The Container App auth configuration expects an existing Microsoft Entra app registration.

Minimum setup:

1. Create or select a web app registration.
2. Add the redirect URI:

```text
https://<app-hostname>/.auth/login/aad/callback
```

3. Enable ID token issuance.
4. Create a client secret.
5. Store the client ID, client secret, and tenant ID in the `azd` environment:

```powershell
azd env set ENTRA_CLIENT_ID <client-id>
azd env set ENTRA_CLIENT_SECRET <client-secret>
azd env set ENTRA_TENANT_ID <tenant-id>
```

## Additional Microsoft Graph Permission Required

AI.AGUI now enriches the signed-in user via Microsoft Graph:

- `GET /me`
- `GET /me/photo/$value`

To make that work, the Entra app registration used by Container Apps auth must have the Microsoft Graph delegated permission `User.Read` and must be consented.

Requested scope:

```text
https://graph.microsoft.com/User.Read
```

Why it is needed:

- retrieve the richer Graph display name
- retrieve the user's profile photo
- keep the existing sign-in model based on delegated user access

## Exact Steps For The Entra Admin

Use an Entra admin account that is allowed to grant tenant-wide consent in your tenant. A Privileged Role Administrator is sufficient.

### Option A: Microsoft Entra admin center

1. Sign in to https://entra.microsoft.com.
2. Open `Identity` > `Applications` > `App registrations`.
3. Open the app registration used by AI.AGUI.
4. Open `API permissions`.
5. If `Microsoft Graph` delegated `User.Read` is missing:
   Select `Add a permission`.
6. Select `Microsoft Graph`.
7. Select `Delegated permissions`.
8. Search for `User.Read`.
9. Select `User.Read`.
10. Select `Add permissions`.
11. Back on `API permissions`, select `Grant admin consent for <tenant>`.
12. Confirm the consent action.

Expected final state:

- `Microsoft Graph`
- `Delegated`
- `User.Read`
- status shows granted for the tenant

### Option B: Azure CLI

These commands can be run by the Entra admin directly.

```powershell
$tenantId = '<tenant-id>'
$clientId = '<app-registration-client-id>'
$graphApiId = '00000003-0000-0000-c000-000000000000'
$userReadScopeId = 'e1fe6dd8-ba31-4d61-89e7-88639da4683d'

az login --tenant $tenantId
az ad app permission add --id $clientId --api $graphApiId --api-permissions "$userReadScopeId=Scope"
az ad app permission admin-consent --id $clientId
az ad app permission list --id $clientId --output json
az ad app permission list-grants --id $clientId --output json
```

### Option C: Repo helper script

An Entra admin can run the helper script in this repo:

```powershell
pwsh -File .\scripts\grant-graph-userread-admin-consent.ps1 -EnvironmentName Production
```

The script:

- reads `ENTRA_TENANT_ID`, `ENTRA_CLIENT_ID`, and `AZURE_SUBSCRIPTION_ID` from `.azure/Production/.env`
- ensures `User.Read` is configured on the app registration
- runs `admin-consent`
- prints the resulting permission state

## Apply The Auth Scope Change In Azure Container Apps

Granting the Graph permission in Entra is only one half of the change. The Container Apps auth configuration must also request that scope.

This repo already does that in `infra/resources.bicep` via:

```bicep
loginParameters: [
  'response_type=code id_token'
  'scope=openid offline_access profile https://graph.microsoft.com/User.Read'
]
```

After changing or merging this infrastructure definition, apply it with:

```powershell
azd provision -e Production
```

## Validation Checklist

After the Entra admin grants consent and the infrastructure is reprovisioned:

1. Open the site URL.
2. Sign in through Entra.
3. Confirm the app loads successfully after the auth redirect.
4. Confirm chat works.
5. Call `/api/me` in an authenticated session.
6. Verify that `displayName` comes from Graph.
7. Verify that `picture` is populated when the user has a profile photo.

## Troubleshooting

### Sign-in works but photo is missing

Possible causes:

- the user has no Microsoft Graph profile photo
- `User.Read` was added but tenant-wide consent was not granted
- the app registration used by Container Apps is not the one you updated

### Sign-in works but `displayName` still looks like UPN or email

Possible causes:

- admin consent is missing
- `azd provision` was not run after the `loginParameters` change
- the session token was issued before the new scope was requested

Try signing out and signing in again after `azd provision`.

### Entra admin consent command fails with insufficient privileges

The signed-in account does not have enough Microsoft Entra directory privilege to grant tenant-wide consent. Use a more privileged Entra admin account.

## References

- Azure Developer CLI Container Apps workflows: https://learn.microsoft.com/azure/developer/azure-developer-cli/container-apps-workflows
- Azure Container Apps authentication with Microsoft Entra ID: https://learn.microsoft.com/azure/container-apps/authentication-entra
- Microsoft guidance for requesting `openid offline_access profile https://graph.microsoft.com/User.Read`: https://learn.microsoft.com/azure/app-service/scenario-secure-app-access-microsoft-graph-as-user
- Microsoft Entra admin consent guidance: https://learn.microsoft.com/troubleshoot/entra/entra-id/app-integration/troubleshoot-consent-issues#perform-admin-consent