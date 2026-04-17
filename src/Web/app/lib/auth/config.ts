/**
 * Authentication mode configuration.
 *
 * - `"local"` – the Next.js app drives an MSAL authorization-code flow on
 *   localhost and stores the resulting Entra tokens in an encrypted cookie.
 *   A Next.js middleware then injects the standard `X-MS-*` headers so
 *   downstream route handlers and the backend see the same contract as ACA
 *   Easy Auth.
 *
 * - `"aca"` (default) – Azure Container Apps Easy Auth is responsible for
 *   authentication; the Next.js app trusts the `X-MS-*` headers provided by
 *   the ingress.
 *
 * Set `AUTH_MODE=local` in `.env.local` to enable the local flow.
 */

export type AuthMode = "local" | "aca";

/**
 * Returns the current authentication mode.
 */
export function getAuthMode(): AuthMode {
  return process.env.AUTH_MODE === "local" ? "local" : "aca";
}

/**
 * Returns `true` when the app is running in local Entra auth mode.
 */
export function isLocalAuth(): boolean {
  return getAuthMode() === "local";
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
