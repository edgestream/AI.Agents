# Local Entra Auth Mode

The web app supports an optional **local authentication mode** that lets developers sign in with Microsoft Entra ID on `localhost`, without deploying to Azure Container Apps (ACA).

When enabled, the Next.js app drives a standard OAuth 2.0 authorization-code flow via MSAL, stores the resulting tokens in an encrypted session cookie, and injects the same `X-MS-*` Easy Auth headers that ACA would provide. The backend, Graph enrichment, and all auth-sensitive UI work identically in both modes.

## Prerequisites

| Item | Notes |
|---|---|
| **Azure AD app registration** | Must be configured with a **Web** redirect URI of `http://localhost:3000/api/auth/callback`. The app needs `User.Read` delegated permission for Graph profile enrichment. |
| **Client secret** | Create one under *Certificates & secrets* in the app registration. |
| **Node.js 20+** | Required by the Next.js app. |

## Environment variables

Create a `.env.local` file in `src/AI.AGUI.Web/` with the following variables:

```env
# Enable local Entra auth (omit or set to "aca" for production behavior)
AUTH_MODE=local

# Azure AD app registration
AZURE_AD_CLIENT_ID=<your-client-id>
AZURE_AD_CLIENT_SECRET=<your-client-secret>
AZURE_AD_TENANT_ID=<your-tenant-id>

# A random secret used to encrypt the session cookie (min. 32 characters)
AUTH_SESSION_SECRET=<random-secret-string>

# Optional: override redirect URIs (defaults shown)
# AUTH_REDIRECT_URI=http://localhost:3000/api/auth/callback
# AUTH_POST_LOGOUT_REDIRECT_URI=http://localhost:3000
```

> **Tip:** Generate `AUTH_SESSION_SECRET` with `openssl rand -base64 32` or `node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"`.

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

- The session cookie is encrypted with AES-256-GCM. The key is derived from `AUTH_SESSION_SECRET` via `scrypt`.
- The cookie is `httpOnly`, `SameSite=Lax`, and `Secure` in production.
- **This mode is intended for local development only.** In production, continue to use ACA Easy Auth.
- The cookie has an 8-hour lifetime. After that the user must sign in again.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `AUTH_SESSION_SECRET environment variable is required` | Missing env var | Add `AUTH_SESSION_SECRET` to `.env.local` |
| `AZURE_AD_CLIENT_ID, AZURE_AD_CLIENT_SECRET, and AZURE_AD_TENANT_ID are required` | Missing app registration env vars | Add all three to `.env.local` |
| Sign in redirects but callback fails with "invalid_grant" | Authorization code reuse or clock skew | Clear cookies and try again |
| Profile shows anonymous after sign-in | Middleware not matching the route | Ensure `AUTH_MODE=local` is set and restart the dev server |
| Graph enrichment returns no photo/display name | Access token lacks `User.Read` scope | Re-consent or update the app registration's API permissions |
