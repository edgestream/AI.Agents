# Kubernetes Deployment

This directory contains a minimal Kustomize base for running AI.Agents on a Kubernetes cluster with the already-built container images.

The base creates:

- a namespace named `ai-agents`
- a backend deployment and service on port `8080`
- a frontend deployment and service on port `3000`
- an ingress that routes traffic to the frontend service

The backend and frontend images default to:

- `ghcr.io/edgestream/agents-server:latest`
- `ghcr.io/edgestream/agents-web:latest`

Update the `images` block in `kustomization.yaml` if you want to pin a different tag.

## Required Secrets

The backend will not start until these secrets exist in the `ai-agents` namespace:

- `ai-agents-backend-appsettings`
  - must contain a file key named `appsettings.Production.json`
- `ai-agents-backend-azure-identity`
  - must contain `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, and `AZURE_CLIENT_SECRET`

Example manifests are provided in:

- `backend-appsettings-secret.example.yaml`
- `backend-azure-identity-secret.example.yaml`

The frontend auth secret is optional. Without it, the web app still runs in anonymous local-auth mode.

- `ai-agents-frontend-auth`
  - optional
  - useful when you want interactive Entra sign-in in front of the cluster ingress

An example is provided in `frontend-auth-secret.example.yaml`.

## Apply Order

Create the namespace first so secrets can be added before the workloads are scheduled:

```bash
kubectl apply -f deploy/k8s/namespace.yaml
```

Create the required secrets in that namespace, either from the example manifests or directly from local files. For example:

```bash
kubectl create secret generic ai-agents-backend-appsettings \
  --namespace ai-agents \
  --from-file=appsettings.Production.json=appsettings.Production.json

kubectl create secret generic ai-agents-backend-azure-identity \
  --namespace ai-agents \
  --from-literal=AZURE_TENANT_ID=<tenant-id> \
  --from-literal=AZURE_CLIENT_ID=<client-id> \
  --from-literal=AZURE_CLIENT_SECRET=<client-secret>
```

If you want frontend sign-in enabled, add the optional frontend auth secret too.

Apply the base:

```bash
kubectl apply -k deploy/k8s
```

## Ingress Notes

The ingress is intentionally hostless so it can work on a cluster with a default ingress controller without needing an immediate DNS choice.

On a shared cluster, you will usually want to patch in:

- a specific host name
- an `ingressClassName`
- TLS configuration

If your ingress controller is not nginx, the nginx buffering annotation in `ingress.yaml` can be removed.

## Runtime Notes

- The backend mounts `/run/secrets/appsettings.Production.json` because the server explicitly loads that path when `ASPNETCORE_ENVIRONMENT=Production`.
- The frontend talks to the backend through the in-cluster service URL `http://ai-agents-backend:8080`.
- The frontend readiness probe uses `/api/me` so it exercises the Next.js server without requiring a signed-in user.