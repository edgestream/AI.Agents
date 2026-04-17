# Deployment

## Local validation

Build both container images with the current project layout:

```bash
docker compose build
```

Start the stack locally:

```bash
docker compose up -d
```

## Azure deployment with azd

The deployment now uses the strict naming pattern `<prefix>-agents-<environment>`.

- `rg-agents-<environment>`
- `log-agents-<environment>`
- `cae-agents-<environment>`
- `ca-agents-<environment>`

For the shared stage environment, the concrete names are:

- `rg-agents-stage`
- `log-agents-stage`
- `cae-agents-stage`
- `ca-agents-stage`

To provision or update those resources:

```powershell
docker login ghcr.io
azd env new Stage
azd up
```

## Post-merge manual checklist

1. Update any CI/CD workflow copies outside this repo to the renamed paths:
   - `src/Server/Dockerfile`
   - `src/Web/Dockerfile`
   - `tests/Server.Tests`
   - `tests/E2ETests`
   - `tests/samples/MealPlanner`
2. Create the GitHub environment named `stage` before enabling the CD workflow.
3. Add the required GitHub deployment secrets to `stage`:
   - `AZURE_OIDC_CLIENT_ID`
   - `AZURE_OIDC_TENANT_ID`
   - `AZURE_OIDC_SUBSCRIPTION_ID`
   - `FOUNDRY_PROJECT_ENDPOINT`
   - `FOUNDRY_MODEL`
4. Create or update the Azure AD / Entra app registration used by GitHub Actions OIDC and add a federated credential with subject `repo:edgestream/AI.Agents:environment:stage`.
5. Grant that GitHub deployment principal the required Azure role assignments. Because you plan to delete and redeploy the environment, the safest scope is subscription-level `Contributor`. If you later restrict scope, it still needs enough access to create the resource group and deploy Container Apps, Log Analytics, and related resources.
6. Create `appsettings.Stage.json` in the repo root or in your deployment automation input source before running `azd up`.
7. Create or update the Entra app registration used by Container Apps Easy Auth:
   - add the redirect URI `https://<stage-app-fqdn>/.auth/login/aad/callback` after the app exists
   - add delegated Microsoft Graph permission `User.Read`
   - grant admin consent
   - create a client secret
8. Set the azd environment values required for Easy Auth and token storage:
   - `ENTRA_CLIENT_ID`
   - `ENTRA_CLIENT_SECRET`
   - `ENTRA_TENANT_ID`
   - `TOKEN_STORE_SAS_URL` if you use the Easy Auth token store
   Keep the names distinct: `AZURE_OIDC_*` is the GitHub automation principal acquired through OpenID Connect, while `ENTRA_*` is the application identity exposed through ACA Easy Auth.
9. Grant the deployed Container App managed identity the `Azure AI User` role on the AI Foundry / Azure AI resource used by the backend.
10. Set `AGENTS_KEYVAULT` for local bootstrap automation if you want [scripts/Get-KeyVault-Environment.ps1](scripts/Get-KeyVault-Environment.ps1) to omit `-VaultName`.
11. Store the Entra app registration in Key Vault as:
   - `AGENTS-ENTRA-TENANT-ID`
   - `AGENTS-ENTRA-CLIENT-ID`
   - `AGENTS-ENTRA-CLIENT-SECRET`
   The bootstrap script mirrors those values into local `AZURE_*` variables for Azure SDK compatibility.
12. Authenticate to GHCR (`docker login ghcr.io`) on any machine or pipeline that will run `azd deploy` / `azd up` against the external registry.
13. Replace the published GHCR packages with the renamed images:
   - `ghcr.io/edgestream/agents-server:latest`
   - `ghcr.io/edgestream/agents-web:latest`
   The old `ai-agents-server` / `ai-agents-web` package names should no longer be used by rollout automation after the merge.
14. Rebuild and publish both container images before the first rollout from the renamed workflow paths.
15. Run the deployment (`azd up` or your equivalent rollout workflow) and verify the backend and frontend containers are both updated.

## Verification targets

- `docker compose build`
- `dotnet build Agents.slnx`
- `azd up` or an equivalent image rollout without path errors