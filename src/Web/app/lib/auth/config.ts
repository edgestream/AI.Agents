/**
 * Authentication mode configuration.
 *
 * - `"local"` – the Next.js app drives an MSAL authorization-code flow on
 *   localhost and stores the resulting Entra tokens in an encrypted cookie.
 *   A Next.js middleware then injects the standard `X-MS-*` headers so
 *   downstream route handlers and the backend see the same contract as ACA
 *   Easy Auth.
 *
 * - `"aca"` – Azure Container Apps Easy Auth is responsible for
 *   authentication; the Next.js app trusts the `X-MS-*` headers provided by
 *   the ingress.
 *
 * `local` is the safe default for developer workstations. Azure-hosted
 * deployments must set `AUTH_MODE=aca` explicitly.
 */

export type AuthMode = "local" | "aca";

function hasLocalAuthAppRegistration(): boolean {
  return Boolean(
    (process.env.ENTRA_CLIENT_ID || process.env.AZURE_CLIENT_ID)
    && (process.env.ENTRA_CLIENT_SECRET || process.env.AZURE_CLIENT_SECRET)
    && (process.env.ENTRA_TENANT_ID || process.env.AZURE_TENANT_ID),
  );
}

/**
 * Returns the current authentication mode.
 */
export function getAuthMode(): AuthMode {
  return process.env.AUTH_MODE === "aca" ? "aca" : "local";
}

/**
 * Returns `true` when the app is running in local Entra auth mode.
 */
export function isLocalAuth(): boolean {
  return getAuthMode() === "local";
}

/**
 * Returns whether interactive sign-in is configured for the current process.
 *
 * Local mode allows anonymous usage without Entra app-registration settings.
 * Interactive sign-in is only available once both the app registration and the
 * session secret are configured.
 */
export function canSignIn(): boolean {
  if (getAuthMode() === "aca") {
    return true;
  }

  return hasLocalAuthAppRegistration() && Boolean(process.env.AUTH_SESSION_SECRET);
}

/**
 * Redirect URI used by the MSAL authorization-code flow.
 * Defaults to `http://localhost:3000/api/auth/callback`.
 */
export function getRedirectUri(): string {
  return process.env.AUTH_REDIRECT_URI ?? "http://localhost:3000/api/auth/callback";
}

/**
 * Post-logout redirect URI.
 * Defaults to `http://localhost:3000`.
 */
export function getPostLogoutRedirectUri(): string {
  return process.env.AUTH_POST_LOGOUT_REDIRECT_URI ?? "http://localhost:3000";
}
