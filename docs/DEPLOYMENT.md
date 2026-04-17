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

The Bicep templates now default to the `ai-agents` resource prefix:

- `rg-ai-agents-<environment>`
- `log-ai-agents-<environment>`
- `cae-ai-agents-<environment>`
- `ca-ai-agents`

To provision or update those resources:

```powershell
docker login ghcr.io
azd up
```

If you must keep targeting legacy `ai-agui`-named Azure resources during migration, set the override before deploying:

```powershell
azd env set RESOURCE_PREFIX ai-agui
azd up
```

## Post-merge manual checklist

1. Update any CI/CD workflow copies outside this repo to the renamed paths:
   - `src/Server/Dockerfile`
   - `src/Web/Dockerfile`
   - `tests/Server.Tests`
   - `tests/E2ETests`
   - `tests/samples/MealPlanner`
2. Decide whether production should move to the new `ai-agents` Azure resource names or continue using the legacy `ai-agui` names temporarily via `RESOURCE_PREFIX=ai-agui`.
3. If you use the disabled GitHub workflow templates in this repo, re-sync their environment values for `RESOURCE_GROUP` and `APP` before enabling them.
4. Prefer `AI_AGENTS_KEY_VAULT` for local bootstrap automation. The script still falls back to `AGUI_KEY_VAULT` during migration.
5. Optionally rename the Key Vault secrets from `AGUI-AZURE-*` to `AI-AGENTS-AZURE-*`. The script supports both names, so this can happen after the merge.
6. Authenticate to GHCR (`docker login ghcr.io`) on any machine or pipeline that will run `azd deploy` / `azd up` against the external registry.
7. Replace the published GHCR packages with the renamed images:
   - `ghcr.io/edgestream/agents-server:latest`
   - `ghcr.io/edgestream/agents-web:latest`
   The old `ai-agents-server` / `ai-agents-web` package names should no longer be used by rollout automation after the merge.
8. Rebuild and publish both container images before the first rollout from the renamed workflow paths.
9. Run the deployment (`azd up` or your equivalent rollout workflow) and verify the backend and frontend containers are both updated.

## Verification targets

- `docker compose build`
- `dotnet build Agents.slnx`
- `azd up` or an equivalent image rollout without path errors