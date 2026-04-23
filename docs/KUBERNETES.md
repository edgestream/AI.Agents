# Kubernetes

The directory `deploy/k8s` contains a minimal Kustomize base for running AI.Agents on a Kubernetes cluster.

The base is pinned to the `development` namespace.

The base creates:

- a backend deployment and service on port `8080`
- a frontend deployment and service on port `3000`
- an ingress that routes traffic to the frontend service

## Required Secrets

The backend will not start until these secrets exist:

- `agents-backend-appsettings`
  - must contain a file key named `appsettings.Production.json`
- `agents-backend-azure-identity`
  - must contain `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, and `AZURE_CLIENT_SECRET`

Example manifests are provided in:

- `backend-appsettings-secret.example.yaml`
- `backend-azure-identity-secret.example.yaml`

The frontend auth secret is optional. Without it, the web app still runs in anonymous local-auth mode.

- `agents-frontend-auth`
  - optional
  - useful when you want interactive Entra sign-in in front of the cluster ingress

An example is provided in `frontend-auth-secret.example.yaml`.

If you enable `agents-frontend-auth`, the frontend uses this repo's built-in Entra authorization-code flow behind your Kubernetes ingress. Ingress-hosted requests still use the deployed `AUTH_REDIRECT_URI`, but loopback requests such as `kubectl port-forward` now use the active localhost origin instead of bouncing through the ingress host. The shared Entra app registration must therefore contain a **Web** redirect URI for each hosted ingress host and for each localhost port you actually use.

Keep these redirect URI entries on the same app registration when you reuse it across environments:

- `http://localhost:3000/api/auth/callback` for local development
- `https://<your-host>/api/auth/callback` for each Kubernetes ingress host you configure in `AUTH_REDIRECT_URI`

If you use a different local port while port-forwarding, register that exact localhost callback URI too, or forward the service to `3000` so the default localhost registration continues to match.

Set `AUTH_POST_LOGOUT_REDIRECT_URI` to `https://<your-host>/` if you want an explicit logout landing page. If it is omitted, the frontend now derives the app root from `AUTH_REDIRECT_URI` so hosted sign-in and sign-out do not fall back to `localhost`.

If the same app registration is also used by Azure Container Apps Easy Auth, keep those `https://<aca-host>/.auth/login/aad/callback` entries too.

If the value sent by the frontend and the app registration differ, Microsoft Entra returns `AADSTS50011`.

## Creating the development namespace

Create the `development` namespace first so the required secrets exist before the workloads are scheduled:

```bash
kubectl create namespace development
kubectl config set-context --current --namespace=development
```

## Creating secrets

Create the required secrets by copying the example manifests:

```bash
cp deploy/k8s/backend-appsettings-secret.example.yaml deploy/k8s/backend-appsettings-secret.yaml
cp deploy/k8s/backend-azure-identity-secret.example.yaml deploy/k8s/backend-azure-identity-secret.yaml
cp deploy/k8s/frontend-auth-secret.example.yaml deploy/k8s/frontend-auth-secret.yaml
```

Edit the secret settings and apply them to the `development` namespace:

```bash
kubectl apply -n development -f deploy/k8s/backend-appsettings-secret.yaml
kubectl apply -n development -f deploy/k8s/backend-azure-identity-secret.yaml
kubectl apply -n development -f deploy/k8s/frontend-auth-secret.yaml
```

Or you may create them directly from local configuration files:

```bash
kubectl create secret generic agents-backend-appsettings \
  -n development \
  --from-file=appsettings.Production.json=appsettings.Production.json
```

```bash
kubectl create secret generic agents-backend-azure-identity \
  -n development \
  --from-literal=AZURE_TENANT_ID=<tenant-id> \
  --from-literal=AZURE_CLIENT_ID=<client-id> \
  --from-literal=AZURE_CLIENT_SECRET=<client-secret>
```

If you want frontend sign-in enabled, add the optional frontend auth secret too.

Before you apply `frontend-auth-secret.yaml`, add the matching Kubernetes callback URI to the shared app registration in **Authentication** -> **Web**. If you use Azure CLI, include every existing redirect URI in the update command because `az ad app update --web-redirect-uris` replaces the full list.

Example:

```powershell
az ad app update --id <client-id> \
  --web-redirect-uris \
    http://localhost:3000/api/auth/callback \
    https://<your-host>/api/auth/callback
```

## Apply the base

```bash
kubectl apply -k deploy/k8s
```

The base declares the Service selector labels directly in the workload manifests, and `deploy/k8s/kustomization.yaml` sets `namespace: development`, so `kubectl apply -k deploy/k8s` always deploys these resources into the `development` namespace.

## Ingress Notes

The ingress is intentionally hostless so it can work on a cluster with a default ingress controller without needing an immediate DNS choice.

On a shared cluster, you will usually want to patch in:

- a specific host name
- an `ingressClassName`
- TLS configuration

If your ingress controller is not nginx, the nginx buffering annotation in `ingress.yaml` can be removed.

## Runtime Notes

- The backend mounts `/run/secrets/appsettings.Production.json` because the server explicitly loads that path when `ASPNETCORE_ENVIRONMENT=Production`.
- The frontend talks to the backend through the in-cluster service URL `http://agents-backend:8080`.
- The frontend readiness probe uses `/api/me` so it exercises the Next.js server without requiring a signed-in user.