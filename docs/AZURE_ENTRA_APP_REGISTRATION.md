# Azure Entra App Registration

This document covers the Microsoft Entra app registrations used by AI.Agents.

## Identity Split

The hosted deployment uses two different Entra identities:

- a GitHub automation principal used by `azure/login` through OpenID Connect
- a web app registration used by Azure Container Apps Easy Auth at runtime

Local auth reuses the runtime app registration. See [AZURE_ENTRA_LOCAL_AUTH.md](AZURE_ENTRA_LOCAL_AUTH.md).

## GitHub OIDC App Registration

Create or reuse an Entra app registration and service principal for GitHub automation.

Required federated credential:

- issuer: GitHub Actions OIDC
- audience: `api://AzureADTokenExchange`
- subject: `repo:edgestream/AI.Agents:environment:stage`

Because the shared stage environment can be deleted and recreated, assign that automation principal Azure access at subscription scope:

- `Contributor`

Expose its identifiers to GitHub as:

- `AZURE_OIDC_CLIENT_ID`
- `AZURE_OIDC_TENANT_ID`
- `AZURE_OIDC_SUBSCRIPTION_ID`

See [GITHUB_REPOSITORY.md](GITHUB_REPOSITORY.md) for the GitHub environment configuration.

## Container Apps Easy Auth App Registration

Create or reuse a web app registration for the hosted application.

Required configuration:

1. Add the hosted redirect URI:

```text
https://<stage-app-fqdn>/.auth/login/aad/callback
```

2. Create a client secret.
3. Add Microsoft Graph delegated permission `User.Read`.
4. Grant admin consent.

Store the values in the azd environment:

```powershell
azd env set ENTRA_CLIENT_ID <client-id>
azd env set ENTRA_CLIENT_SECRET <client-secret>
azd env set ENTRA_TENANT_ID <tenant-id>
```

## Local Auth Reuse

If you use the local auth flow in [AZURE_ENTRA_LOCAL_AUTH.md](AZURE_ENTRA_LOCAL_AUTH.md), add this redirect URI to the same app registration:

```text
http://localhost:3000/api/auth/callback
```

The local bootstrap script writes the same credential values into both `ENTRA_*` and `AZURE_*` environment variables because `DefaultAzureCredential` still expects the `AZURE_*` names.

## Key Vault Naming

If you store the runtime app registration in Azure Key Vault for local bootstrap automation, use these secret names:

- `AGENTS-ENTRA-TENANT-ID`
- `AGENTS-ENTRA-CLIENT-ID`
- `AGENTS-ENTRA-CLIENT-SECRET`

Optional convenience environment variable for the bootstrap script:

- `AGENTS_KEYVAULT`

Bootstrap script:

- `scripts/Get-KeyVault-Environment.ps1`

## Related Docs

- [AZURE_CONTAINER_APPS.md](AZURE_CONTAINER_APPS.md)
- [AZURE_ENTRA_LOCAL_AUTH.md](AZURE_ENTRA_LOCAL_AUTH.md)
- [GITHUB_REPOSITORY.md](GITHUB_REPOSITORY.md)