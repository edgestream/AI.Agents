# Azure Entra Local Auth

The web app supports an optional **local authentication mode** that lets developers sign in with Microsoft Entra ID on `localhost`, without deploying to Azure Container Apps (ACA).

When enabled, the Next.js app drives a standard OAuth 2.0 authorization-code flow via MSAL, stores the resulting tokens in an encrypted session cookie, and injects the same `X-MS-*` Easy Auth headers that ACA would provide. The backend, Graph enrichment, and all auth-sensitive UI work identically in both modes.

When the local Entra variables are omitted, the app still starts in anonymous local mode. In that state the Sign in UI stays hidden, and backend Foundry calls can still authenticate through `DefaultAzureCredential` sources such as `az login`.

The repo root `.env` can remain the single source of truth for the local app registration. Interactive local sign-in accepts the canonical `ENTRA_*` variables or the mirrored `AZURE_*` values used by Azure SDKs.

## Prerequisites

These prerequisites apply when you want interactive local sign-in. They are not required for anonymous local usage.

| Item | Notes |
|---|---|
| **Existing Entra app registration** | The same app registration used for ACA Easy Auth (recommended in your root `.env` as `ENTRA_CLIENT_ID` / `ENTRA_CLIENT_SECRET` / `ENTRA_TENANT_ID`). You must add the exact localhost callback URI you use during development as a **Web** redirect URI in the Azure portal, for example `http://localhost:3000/api/auth/callback`. |
| **`User.Read` delegated permission** | Must be explicitly added as a **Microsoft Graph → Delegated → User.Read** permission in the app registration and granted admin consent. Without it, Graph profile enrichment (display name, photo) will fail silently. |
| **`Azure AI User` role on AI Foundry** | The app registration's service principal must have the **Azure AI User** role on the AI Foundry resource (account scope). In local mode the backend authenticates via `EnvironmentCredential`, so the same Entra app credentials must also be available under the `AZURE_CLIENT_ID` / `AZURE_CLIENT_SECRET` / `AZURE_TENANT_ID` compatibility names. Without that role assignment all LLM calls fail with a `lacks the required data action` error. |
| **Node.js 20+** | Required by the Next.js app. |

## Reusing the Existing App Registration

The repo root `.env` should treat the Entra app registration as the canonical local auth identity and mirror the same values into `AZURE_*` for Azure SDK compatibility:

```env
ENTRA_TENANT_ID=<your-tenant-id>
ENTRA_CLIENT_ID=<your-client-id>
ENTRA_CLIENT_SECRET=<your-client-secret>

# Azure SDK compatibility mirror for DefaultAzureCredential / EnvironmentCredential
AZURE_TENANT_ID=<same-as-ENTRA_TENANT_ID>
AZURE_CLIENT_ID=<same-as-ENTRA_CLIENT_ID>
AZURE_CLIENT_SECRET=<same-as-ENTRA_CLIENT_SECRET>
```

[scripts/Get-KeyVault-Environment.ps1](../scripts/Get-KeyVault-Environment.ps1) now writes both sets automatically. If you maintain the file manually, keep the values identical. For the hosted app registration and GitHub OIDC setup, see [AZURE_ENTRA_APP_REGISTRATION.md](AZURE_ENTRA_APP_REGISTRATION.md). The only one-time portal step is adding a redirect URI:

If you use the bootstrap script, set `AGENTS_KEYVAULT` or pass `-VaultName`, and store the app registration in Key Vault as `AGENTS-ENTRA-TENANT-ID`, `AGENTS-ENTRA-CLIENT-ID`, and `AGENTS-ENTRA-CLIENT-SECRET`.

1. Open the [Azure portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) and find your app.
2. Go to **Authentication** → **Web** → **Redirect URIs**.
3. Add the localhost callback URI you use for sign-in, such as `http://localhost:3000/api/auth/callback`, and **Save**.

If you access the frontend through `kubectl port-forward`, the local auth flow now uses the live browser origin for loopback requests. In practice that means the callback stays on `localhost` instead of bouncing to the hosted ingress URL, but the exact localhost port must still be registered in the app registration. Using `kubectl -n development port-forward svc/agents-frontend 3000:3000` keeps the default `http://localhost:3000/api/auth/callback` registration valid.

## Environment Variables

The root `.env` is the **single source of truth** for all local development credentials. It is already read by `docker-compose.yml`, and a symlink at `src/Web/.env` makes the same file available to `npm run dev`.

Add the following to the root `.env` (alongside the existing credentials):

```env
# Local Entra auth is the default. Set AUTH_MODE=aca only for hosted / Easy Auth behavior.
# AUTH_MODE=local

# Required only when you want interactive local sign-in.
# A random secret used to encrypt the session cookie (min. 32 characters)
AUTH_SESSION_SECRET=<random-secret-string>

# Canonical app registration variables:
# ENTRA_TENANT_ID=...
# ENTRA_CLIENT_ID=...
# ENTRA_CLIENT_SECRET=...

# Azure SDK compatibility mirror for local backend / dotnet test:
# AZURE_TENANT_ID=...
# AZURE_CLIENT_ID=...
# AZURE_CLIENT_SECRET=...
```

> **Tip:** Generate `AUTH_SESSION_SECRET` with `node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"` or `openssl rand -base64 32`.

### Symlinking for `npm run dev`

Next.js only loads `.env*` files from its own project directory (`src/Web/`). A symlink keeps things in sync without duplicating secrets:

```powershell
# Run once from the repo root (requires Developer Mode or an elevated terminal on Windows)
New-Item -ItemType SymbolicLink -Path src\Web\.env -Target ..\..\.env
```

After that, `docker compose up` and `npm run dev` both read the same file. Do **not** create a separate `.env.local` — it would shadow the symlink and become a second source of truth.

## How It Works

```
Browser                  Next.js Middleware           Route Handler / Backend
  │                            │                            │
  ├── GET /api/auth/login ────►│                            │
  │   ◄── 302 → Entra ID ─────┤                            │
  │                            │                            │
  │── (user signs in at        │                            │
  │    Entra ID)               │                            │
  │                            │                            │
  ├── GET /api/auth/callback?code=… ──────────────────────►│
  │   ◄── Set-Cookie + 302 → / ────────────────────────────┤
  │                            │                            │
  ├── GET /api/me ────────────►│                            │
  │                            ├── decrypt cookie           │
  │                            ├── inject X-MS-* headers ──►│
  │   ◄── user profile ────────┤◄──────────────────────────┤
```

1. **`/api/auth/login`** — Redirects the browser to the Entra ID authorization endpoint.
2. **Entra ID** — The user authenticates and consents.
3. **`/api/auth/callback`** — Exchanges the authorization code for tokens via MSAL and stores them in an encrypted `httpOnly` cookie.
4. **Next.js middleware** — On subsequent requests to `/api/me` and `/api/copilotkit`, the middleware decrypts the cookie and injects `X-MS-CLIENT-PRINCIPAL-ID`, `X-MS-CLIENT-PRINCIPAL-NAME`, `X-MS-TOKEN-AAD-ACCESS-TOKEN`, and `X-MS-TOKEN-AAD-ID-TOKEN` headers.
5. **Route handlers & backend** — See the exact same header contract as ACA Easy Auth. No backend changes are needed.

## Sign In / Sign Out

| Action | Local mode | ACA mode |
|---|---|---|
| Sign in | `GET /api/auth/login` | `GET /.auth/login/aad` |
| Sign out | `GET /api/auth/logout` | `GET /.auth/logout` |

The UI components (`UserMenu`, `UserAvatar`) automatically select the correct URL based on the `authMode` field returned by `/api/me`. Local processes default to `local`; hosted ACA deployments must set `AUTH_MODE=aca` explicitly. In local mode the Sign in action is only shown when either the `ENTRA_*` or `AZURE_*` app-registration variables and `AUTH_SESSION_SECRET` are present.

## Security Notes

- The session cookie is encrypted with AES-256-GCM. The key is derived from `AUTH_SESSION_SECRET` via PBKDF2.
- The cookie is `httpOnly`, `SameSite=Lax`, and `Secure` in production.
- **This mode is intended for local development only.** In production, continue to use ACA Easy Auth.
- The cookie has an 8-hour lifetime. After that the user must sign in again.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `AUTH_SESSION_SECRET environment variable is required` | Missing env var | Add `AUTH_SESSION_SECRET` to your `.env` / `.env.local` |
| `ENTRA_CLIENT_ID, ENTRA_CLIENT_SECRET, and ENTRA_TENANT_ID are required` | Missing app registration env vars | Add all three for browser sign-in, or provide the mirrored `AZURE_*` values from the same app registration in the root `.env` |
| You want anonymous local usage only | No local sign-in configuration | Leave the Entra variables unset, sign in stays hidden, and use `az login` for backend `DefaultAzureCredential` access |
| Sign in redirects but callback fails with "invalid_grant" | Authorization code reuse or clock skew | Clear cookies and try again |
| Callback fails with "redirect_uri mismatch" | Redirect URI not registered | Add `http://localhost:3000/api/auth/callback` to the app registration |
| Profile shows anonymous after sign-in | Middleware not matching the route | Ensure `AUTH_MODE` is unset or set to `local`, then restart the dev server |
| Graph enrichment returns no photo/display name | Access token lacks `User.Read` scope | Re-consent or update the app registration's API permissions |