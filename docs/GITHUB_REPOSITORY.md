# GitHub Repository

This document covers the GitHub-side CI/CD configuration for AI.Agents.

## Workflow Files

The repository uses these workflow files:

- `.github/workflows/ci.yml`
- `.github/workflows/cd.yml`

The CI workflow builds images, runs tests, and publishes the container tags. The CD workflow now applies `deploy/k8s` and rolls the `development` namespace deployments to the matching image SHA tags.

## GitHub Environment

Create the GitHub environment named `development` before enabling the CD workflow.

Environment secrets required by the CD workflow:

- `KUBE_CONFIG`

Store the full kubeconfig content for the deployment identity in that secret. The Kubernetes-side service account and RBAC setup is described in [KUBERNETES.md](KUBERNETES.md).

If you use environment protection rules, attach them to `development` so the rollout secret stays isolated to the deployment environment.

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

The current Kubernetes rollout assumes these packages are pullable by the cluster. If your GHCR packages are private, configure image pull credentials or a pull secret in the cluster before enabling CD.

The CD workflow updates the `agents-backend` and `agents-frontend` deployments to `sha-<7>` tags derived from CI. On fresh developer or automation machines outside GitHub Actions, authenticate with:

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