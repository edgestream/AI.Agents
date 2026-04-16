import { NextRequest, NextResponse } from "next/server";
import { getAuthMode, type AuthMode } from "@/app/lib/auth/config";

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
  /** Indicates the active auth mode (`"local"` or `"aca"`). */
  authMode?: AuthMode;
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
 * GET /api/me - Proxies user info from backend or returns info from Easy Auth headers.
 * 
 * In local auth mode the Next.js middleware injects X-MS-* headers from the
 * encrypted session cookie, so this handler works identically for both modes.
 *
 * In development without auth: Returns anonymous user.
 * In production (ACA with Easy Auth): Extracts identity from X-MS-* headers
 * and forwards to backend for validation.
 */
export async function GET(request: NextRequest) {
  const authMode = getAuthMode();
  const principalId = request.headers.get("X-MS-CLIENT-PRINCIPAL-ID");
  const principalName = request.headers.get("X-MS-CLIENT-PRINCIPAL-NAME");
  const accessToken = request.headers.get("X-MS-TOKEN-AAD-ACCESS-TOKEN");
  const idToken = request.headers.get("X-MS-TOKEN-AAD-ID-TOKEN");
  const tenantId = getTenantIdFromIdToken(idToken);

  // If we have Easy Auth headers (or local-auth injected headers), forward to backend
  if (principalId || principalName || accessToken || idToken) {
    const backendUrl = process.env.BACKEND_URL || "http://localhost:8080";
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
        } satisfies UserInfo);
      }
    } catch (error) {
      console.error("Failed to fetch user info from backend:", error);
    }

    const fallbackEmail = principalName?.includes("@") ? principalName : undefined;

    // Fallback: return info from headers directly
    return NextResponse.json({
      authenticated: true,
      userId: principalId || undefined,
      displayName: principalName || undefined,
      email: fallbackEmail,
      tenantId,
      domain: getDomainFromEmail(fallbackEmail),
      authMode,
    } satisfies UserInfo);
  }

  // No auth headers - return anonymous with auth mode info
  return NextResponse.json({
    authenticated: false,
    authMode,
  } satisfies UserInfo);
}
