# GitHub Repository

This document covers the GitHub-side CI/CD configuration for AI.Agents.

## Workflow Files

The repository uses these workflow files:

- `.github/workflows/ci.yml`
- `.github/workflows/cd.yml`

The CI workflow builds images, runs tests, and publishes the container tags. The CD workflow updates the hosted stage Container App to the matching image SHA tags.

## GitHub Environment

Create the GitHub environment named `stage` before enabling the CD workflow.

Environment secrets required by the CD workflow:

- `AZURE_OIDC_CLIENT_ID`
- `AZURE_OIDC_TENANT_ID`
- `AZURE_OIDC_SUBSCRIPTION_ID`

The Azure side of that OIDC identity is described in [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md).

## CI Secrets

The CI workflow also needs the runtime app registration values for the local stack and live tests:

- `ENTRA_CLIENT_ID`
- `ENTRA_CLIENT_SECRET`
- `ENTRA_TENANT_ID`
- `FOUNDRY_PROJECT_ENDPOINT`
- `FOUNDRY_MODEL`

CI mirrors the `ENTRA_*` secrets into `AZURE_*` environment variables for Azure SDK consumers inside the test stack.

## GitHub Container Registry

Published package names:

- `ghcr.io/edgestream/agents-server`
- `ghcr.io/edgestream/agents-web`

The current hosted Stage rollout assumes these packages are public. The Container App deployment no longer injects registry credentials for GHCR.

The CD workflow updates the hosted app to `sha-<7>` tags derived from CI. On fresh developer or automation machines outside GitHub Actions, authenticate with:

```powershell
docker login ghcr.io
```

## Repository Path Expectations

If you maintain workflow copies or related automation outside this repository, update them to the current layout:

- `src/Server/Dockerfile`
- `src/Web/Dockerfile`
- `tests/Server.Tests`
- `tests/E2ETests`
- `tests/samples/MealPlanner`

## Related Docs

- [AZURE_CONTAINER_APPS.md](AZURE_CONTAINER_APPS.md)
- [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md)
- [AZURE_AI_FOUNDRY.md](AZURE_AI_FOUNDRY.md)