import { NextRequest, NextResponse } from "next/server";
import { canSignIn, getAuthMode, type AuthMode } from "@/app/lib/auth/config";
import { getOidcProxyIdentity } from "@/app/lib/auth/oidcProxy";
import { resolveLocalSession } from "@/app/lib/auth/resolveLocalSession";

/**
 * User info response shape matching the backend /api/me endpoint.
 */
export interface UserInfo {
  authenticated: boolean;
  userId?: string;
  displayName?: string;
  email?: string;
  picture?: string;
  tenantId?: string;
  domain?: string;
  /** Indicates the active auth mode (`"local"`, `"aca"`, or `"oidc"`). */
  authMode?: AuthMode;
  /** Whether the current process is configured to offer an interactive sign-in flow. */
  canSignIn?: boolean;
}

function getDomainFromEmail(email: string | undefined): string | undefined {
  if (!email) {
    return undefined;
  }

  const atIndex = email.indexOf("@");
  if (atIndex < 0 || atIndex === email.length - 1) {
    return undefined;
  }

  return email.slice(atIndex + 1);
}

function getTenantIdFromIdToken(idToken: string | null): string | undefined {
  if (!idToken) {
    return undefined;
  }

  const jwtParts = idToken.split(".");
  if (jwtParts.length < 2) {
    return undefined;
  }

  try {
    const payload = JSON.parse(Buffer.from(jwtParts[1], "base64url").toString("utf8")) as { tid?: unknown };
    return typeof payload.tid === "string" ? payload.tid : undefined;
  } catch {
    return undefined;
  }
}

/**
 * GET /api/me - Proxies user info from backend or returns info from auth headers.
 *
 * In local auth mode the route handler resolves the encrypted session cookie
 * directly so the backend still sees the same `X-MS-*` contract as ACA.
 *
 * In development without auth: Returns anonymous user.
 * In production:
 * - ACA mode trusts the platform `X-MS-*` headers and forwards them to backend.
 * - OIDC mode trusts ingress-proxy `X-Auth-Request-*` headers and returns a
 *   frontend-authenticated user shape without Graph enrichment.
 */
export async function GET(request: NextRequest) {
  const authMode = getAuthMode();
  const signInEnabled = canSignIn();

  if (authMode === "oidc") {
    const oidcIdentity = getOidcProxyIdentity(request);

    if (
      oidcIdentity.userId
      || oidcIdentity.principalName
      || oidcIdentity.accessToken
      || oidcIdentity.authorization
    ) {
      return NextResponse.json({
        authenticated: true,
        userId: oidcIdentity.userId,
        email: oidcIdentity.email,
        domain: getDomainFromEmail(oidcIdentity.email),
        authMode,
        canSignIn: signInEnabled,
      } satisfies UserInfo);
    }

    return NextResponse.json({
      authenticated: false,
      authMode,
      canSignIn: signInEnabled,
    } satisfies UserInfo);
  }

  let principalId = request.headers.get("X-MS-CLIENT-PRINCIPAL-ID");
  let principalName = request.headers.get("X-MS-CLIENT-PRINCIPAL-NAME");
  let accessToken = request.headers.get("X-MS-TOKEN-AAD-ACCESS-TOKEN");
  let idToken = request.headers.get("X-MS-TOKEN-AAD-ID-TOKEN");

  // In local auth mode, route handlers resolve the session directly because the
  // Edge middleware worker cannot access the in-memory server-side session store.
  if (authMode === "local" && !principalId && !principalName) {
    const localSession = await resolveLocalSession(request);
    if (localSession) {
      principalId = localSession.principalId;
      principalName = localSession.principalName;
      accessToken = localSession.accessToken;
      idToken = localSession.idToken;
    }
  }
  const tenantId = getTenantIdFromIdToken(idToken);

  // If we have Easy Auth headers (or locally resolved session headers), forward to backend
  if (principalId || principalName || accessToken || idToken) {
    const backendUrl = (process.env.BACKEND_URL || "http://localhost:8000").replace(/\/+$/, "");
    try {
      const backendHeaders: HeadersInit = {};

      if (principalId) {
        backendHeaders["X-MS-CLIENT-PRINCIPAL-ID"] = principalId;
      }
      if (principalName) {
        backendHeaders["X-MS-CLIENT-PRINCIPAL-NAME"] = principalName;
      }
      if (accessToken) {
        backendHeaders["X-MS-TOKEN-AAD-ACCESS-TOKEN"] = accessToken;
      }
      if (idToken) {
        backendHeaders["X-MS-TOKEN-AAD-ID-TOKEN"] = idToken;
      }

      const response = await fetch(`${backendUrl}/api/me`, {
        headers: backendHeaders,
      });

      if (response.ok) {
        const data: UserInfo = await response.json();
        return NextResponse.json({
          ...data,
          tenantId,
          domain: getDomainFromEmail(data.email),
          authMode,
          canSignIn: signInEnabled,
        } satisfies UserInfo);
      }
    } catch (error) {
      console.error("Failed to fetch user info from backend:", error);
    }

    const fallbackEmail = principalName?.includes("@") ? principalName : undefined;

    // Fallback: return info from headers directly.
    // displayName is intentionally omitted — it must come from Graph via the backend.
    // principalName is a UPN / email, not a human-readable display name.
    return NextResponse.json({
      authenticated: true,
      userId: principalId || undefined,
      email: fallbackEmail,
      tenantId,
      domain: getDomainFromEmail(fallbackEmail),
      authMode,
      canSignIn: signInEnabled,
    } satisfies UserInfo);
  }

  // No auth headers - return anonymous with auth mode info
  return NextResponse.json({
    authenticated: false,
    authMode,
    canSignIn: signInEnabled,
  } satisfies UserInfo);
}
