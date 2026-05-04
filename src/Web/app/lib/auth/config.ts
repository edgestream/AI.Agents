import type { NextRequest } from "next/server";

/**
 * Authentication mode configuration.
 *
 * - `"local"` – the Next.js app drives an MSAL authorization-code flow on
 *   localhost and stores the resulting Entra tokens in an encrypted cookie.
 *   Route handlers resolve the local session directly so downstream handlers
 *   and the backend see the same `X-MS-*` contract as ACA Easy Auth.
 *
 * - `"aca"` – Azure Container Apps Easy Auth is responsible for
 *   authentication; the Next.js app trusts the `X-MS-*` headers provided by
 *   the ingress.
 *
 * - `"oidc"` – an external ingress auth proxy such as oauth2-proxy performs
 *   OIDC sign-in; the Next.js app trusts the forwarded `X-Auth-Request-*`
 *   headers provided by the proxy.
 *
 * `local` is the safe default for developer workstations. Azure-hosted
 * deployments must set `AUTH_MODE=aca` or `AUTH_MODE=oidc` explicitly.
 */

export type AuthMode = "local" | "aca" | "oidc";

type AuthRequest = Pick<NextRequest, "headers" | "nextUrl">;

const DEFAULT_REDIRECT_URI = "http://localhost:3000/api/auth/callback";
const CALLBACK_PATH = "/api/auth/callback";
const LOOPBACK_HOSTNAMES = new Set(["localhost", "127.0.0.1", "::1", "[::1]"]);

function getFirstHeaderValue(value: string | null): string | null {
  if (!value) {
    return null;
  }

  return value.split(",", 1)[0]?.trim() || null;
}

function getHostname(value: string): string {
  if (value.startsWith("[")) {
    const end = value.indexOf("]");
    return end >= 0 ? value.slice(1, end) : value;
  }

  const lastColon = value.lastIndexOf(":");
  if (lastColon >= 0 && value.indexOf(":") === lastColon) {
    return value.slice(0, lastColon);
  }

  return value;
}

function getRequestHost(request: AuthRequest): string | null {
  return getFirstHeaderValue(request.headers.get("x-forwarded-host"))
    ?? getFirstHeaderValue(request.headers.get("host"));
}

function getRequestOrigin(request?: AuthRequest): string | null {
  if (!request) {
    return null;
  }

  const host = getRequestHost(request);
  if (!host) {
    return request.nextUrl.origin;
  }

  const hostname = getHostname(host);
  const forwardedProto = getFirstHeaderValue(request.headers.get("x-forwarded-proto"));
  const protocol = forwardedProto
    ?? (LOOPBACK_HOSTNAMES.has(hostname) ? "http" : request.nextUrl.protocol.replace(/:$/, ""));

  return new URL(`${protocol}://${host}`).origin;
}

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
  const authMode = process.env.AUTH_MODE?.toLowerCase();

  if (authMode === "aca" || authMode === "oidc") {
    return authMode;
  }

  return "local";
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
 * Interactive sign-in is available once either ENTRA_* or AZURE_* app-registration
 * settings and the session secret are configured.
 */
export function canSignIn(): boolean {
  if (getAuthMode() !== "local") {
    return true;
  }

  return hasLocalAuthAppRegistration() && Boolean(process.env.AUTH_SESSION_SECRET);
}

/**
 * Redirect URI used by the MSAL authorization-code flow.
 * Defaults to `http://localhost:3000/api/auth/callback`.
 */
function getConfiguredRedirectUri(): string {
  return process.env.AUTH_REDIRECT_URI ?? DEFAULT_REDIRECT_URI;
}

/**
 * Returns `true` when the active request is using a loopback origin such as
 * localhost or 127.0.0.1. That happens for native local runs and for
 * kubectl port-forward workflows against a remote pod.
 */
function shouldUseRequestOrigin(request?: AuthRequest): request is AuthRequest {
  if (!request) {
    return false;
  }

  if (!process.env.AUTH_REDIRECT_URI) {
    return true;
  }

  const host = getRequestHost(request);
  if (host) {
    return LOOPBACK_HOSTNAMES.has(getHostname(host));
  }

  return LOOPBACK_HOSTNAMES.has(request.nextUrl.hostname);
}

/**
 * Redirect URI used by the MSAL authorization-code flow.
 * Uses the live loopback request origin for localhost / port-forward sessions,
 * otherwise falls back to the hosted AUTH_REDIRECT_URI value.
 */
export function getRedirectUri(request?: AuthRequest): string {
  if (shouldUseRequestOrigin(request)) {
    return new URL(CALLBACK_PATH, getRequestOrigin(request) ?? request.nextUrl.origin).toString();
  }

  return getConfiguredRedirectUri();
}

/**
 * Application root derived from the effective redirect URI.
 */
export function getAppRootUri(request?: AuthRequest): string {
  if (shouldUseRequestOrigin(request)) {
    return new URL("/", getRequestOrigin(request) ?? request.nextUrl.origin).toString();
  }

  return new URL("/", getConfiguredRedirectUri()).toString();
}

/**
 * Post-logout redirect URI.
 * Defaults to the application root derived from `AUTH_REDIRECT_URI`.
 */
export function getPostLogoutRedirectUri(request?: AuthRequest): string {
  if (shouldUseRequestOrigin(request)) {
    return new URL("/", getRequestOrigin(request) ?? request.nextUrl.origin).toString();
  }

  return process.env.AUTH_POST_LOGOUT_REDIRECT_URI ?? getAppRootUri();
}
