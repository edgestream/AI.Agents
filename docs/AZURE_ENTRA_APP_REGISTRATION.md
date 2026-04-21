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

1. After the first successful Stage deployment, add the hosted redirect URI that matches the deployed Container App hostname:

```text
https://<stage-app-fqdn>/.auth/login/aad/callback
```

2. If you also use local auth, add this localhost redirect URI as a separate entry on the same app registration:

```text
http://localhost:3000/api/auth/callback
```

3. Enable `ID tokens` under `Authentication` -> `Implicit grant and hybrid flows`.
4. Create a client secret.
5. Add Microsoft Graph delegated permission `User.Read`.
6. Grant admin consent if your tenant policy requires it. Running `az ad app permission admin-consent` requires tenant-admin rights and returns `Authorization_RequestDenied` for non-admin accounts.

Because the Container Apps Easy Auth configuration in [infra/resources.bicep](../infra/resources.bicep) requests `response_type=code id_token` to obtain a usable Graph token, disabling ID token issuance breaks sign-in with `AADSTS700054`.

CLI example for the runtime app registration:

```powershell
az ad app update --id <client-id> \
	--web-redirect-uris \
		https://<stage-app-fqdn>/.auth/login/aad/callback \
		http://localhost:3000/api/auth/callback \
	--enable-id-token-issuance true
```

Store the values in the azd environment:

```powershell
azd env set ENTRA_CLIENT_ID <client-id>
azd env set ENTRA_CLIENT_SECRET <client-secret>
azd env set ENTRA_TENANT_ID <tenant-id>
```

## Local Auth and Kubernetes Reuse

If you reuse the runtime app registration for the local auth flow in [AZURE_ENTRA_LOCAL_AUTH.md](AZURE_ENTRA_LOCAL_AUTH.md) or for the optional Kubernetes frontend auth secret in [KUBERNETES.md](KUBERNETES.md), keep separate redirect URI entries for each deployed host:

- `http://localhost:3000/api/auth/callback`
- `https://<k8s-ingress-host>/api/auth/callback` for each Kubernetes ingress host that sets `AUTH_REDIRECT_URI`
- any Azure Container Apps Easy Auth callback such as `https://<stage-app-fqdn>/.auth/login/aad/callback`

The redirect URI sent by the app must exactly match one of those registered entries or sign-in fails with `AADSTS50011`.

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