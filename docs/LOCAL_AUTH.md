# Local Entra Auth Mode

The web app supports an optional **local authentication mode** that lets developers sign in with Microsoft Entra ID on `localhost`, without deploying to Azure Container Apps (ACA).

When enabled, the Next.js app drives a standard OAuth 2.0 authorization-code flow via MSAL, stores the resulting tokens in an encrypted session cookie, and injects the same `X-MS-*` Easy Auth headers that ACA would provide. The backend, Graph enrichment, and all auth-sensitive UI work identically in both modes.

## Prerequisites

| Item | Notes |
|---|---|
| **Existing Entra app registration** | The same app registration used for ACA Easy Auth (already in your root `.env` as `AZURE_CLIENT_ID` / `AZURE_CLIENT_SECRET` / `AZURE_TENANT_ID`). You must add `http://localhost:3000/api/auth/callback` as a **Web** redirect URI in the Azure portal. |
| **`User.Read` delegated permission** | Must be explicitly added as a **Microsoft Graph → Delegated → User.Read** permission in the app registration and granted admin consent. Without it, Graph profile enrichment (display name, photo) will fail silently. |
| **`Azure AI User` role on AI Foundry** | The app registration's service principal must have the **Azure AI User** role on the AI Foundry resource (account scope). In local mode the backend authenticates via `EnvironmentCredential` (the `AZURE_CLIENT_ID`/`AZURE_CLIENT_SECRET` env vars), so the role must be assigned to that service principal — not to your personal user account. Without it all LLM calls fail with a `lacks the required data action` error. |
| **Node.js 20+** | Required by the Next.js app. |

## Reusing the existing app registration

The repo root `.env` already contains the Entra app credentials used by `docker-compose.yml` for the backend:

```env
AZURE_TENANT_ID=<your-tenant-id>
AZURE_CLIENT_ID=<your-client-id>
AZURE_CLIENT_SECRET=<your-client-secret>
```

Local auth reuses the **same values**. The only one-time step is adding a redirect URI:

1. Open the [Azure portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) and find your app.
2. Go to **Authentication** → **Web** → **Redirect URIs**.
3. Add `http://localhost:3000/api/auth/callback` and **Save**.

## Environment variables

The root `.env` is the **single source of truth** for all local development credentials. It is already read by `docker-compose.yml`, and a symlink at `src/AI.AGUI.Web/.env` makes the same file available to `npm run dev`.

Add the following to the root `.env` (alongside the existing credentials):

```env
# Enable local Entra auth (omit or set to "aca" for production / ACA behavior)
AUTH_MODE=local

# A random secret used to encrypt the session cookie (min. 32 characters)
AUTH_SESSION_SECRET=<random-secret-string>

# These should already be present:
# AZURE_TENANT_ID=...
# AZURE_CLIENT_ID=...
# AZURE_CLIENT_SECRET=...
```

> **Tip:** Generate `AUTH_SESSION_SECRET` with `node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"` or `openssl rand -base64 32`.

### Symlinking for `npm run dev`

Next.js only loads `.env*` files from its own project directory (`src/AI.AGUI.Web/`). A symlink keeps things in sync without duplicating secrets:

```powershell
# Run once from the repo root (requires Developer Mode or an elevated terminal on Windows)
New-Item -ItemType SymbolicLink -Path src\AI.AGUI.Web\.env -Target ..\..\.env
```

After that, `docker compose up` and `npm run dev` both read the same file. Do **not** create a separate `.env.local` — it would shadow the symlink and become a second source of truth.

## How it works

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

## Sign in / sign out

| Action | Local mode | ACA mode |
|---|---|---|
| Sign in | `GET /api/auth/login` | `GET /.auth/login/aad` |
| Sign out | `GET /api/auth/logout` | `GET /.auth/logout` |

The UI components (`UserMenu`, `UserAvatar`) automatically select the correct URL based on the `authMode` field returned by `/api/me`.

## Security notes

- The session cookie is encrypted with AES-256-GCM. The key is derived from `AUTH_SESSION_SECRET` via PBKDF2.
- The cookie is `httpOnly`, `SameSite=Lax`, and `Secure` in production.
- **This mode is intended for local development only.** In production, continue to use ACA Easy Auth.
- The cookie has an 8-hour lifetime. After that the user must sign in again.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `AUTH_SESSION_SECRET environment variable is required` | Missing env var | Add `AUTH_SESSION_SECRET` to your `.env` / `.env.local` |
| `AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID are required` | Missing app registration env vars | Add all three (they should already be in the root `.env`) |
| Sign in redirects but callback fails with "invalid_grant" | Authorization code reuse or clock skew | Clear cookies and try again |
| Callback fails with "redirect_uri mismatch" | Redirect URI not registered | Add `http://localhost:3000/api/auth/callback` to the app registration |
| Profile shows anonymous after sign-in | Middleware not matching the route | Ensure `AUTH_MODE=local` is set and restart the dev server |
| Graph enrichment returns no photo/display name | Access token lacks `User.Read` scope | Re-consent or update the app registration's API permissions |
