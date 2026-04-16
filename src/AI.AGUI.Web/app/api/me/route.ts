import { NextRequest, NextResponse } from "next/server";

const debugLogLevelValues = new Set(["debug", "trace"]);

function isDebugLoggingEnabled(): boolean {
  return debugLogLevelValues.has((process.env.LOG_LEVEL ?? "").trim().toLowerCase());
}

function logHeaders(route: string, label: string, headers: Headers | HeadersInit): void {
  if (!isDebugLoggingEnabled()) {
    return;
  }

  const normalizedHeaders = headers instanceof Headers
    ? Object.fromEntries(headers.entries())
    : headers;

  console.info("[easy-auth]", JSON.stringify({
    route,
    label,
    headers: normalizedHeaders,
  }));
}

/**
 * User info response shape matching the backend /api/me endpoint.
 */
export interface UserInfo {
  authenticated: boolean;
  userId?: string;
  displayName?: string;
  email?: string;
  picture?: string;
}

/**
 * GET /api/me - Proxies user info from backend or returns info from Easy Auth headers.
 * 
 * In development: Returns anonymous user.
 * In production (ACA with Easy Auth): Extracts identity from X-MS-* headers
 * and forwards to backend for validation.
 */
export async function GET(request: NextRequest) {
  const principalId = request.headers.get("X-MS-CLIENT-PRINCIPAL-ID");
  const principalName = request.headers.get("X-MS-CLIENT-PRINCIPAL-NAME");
  const accessToken = request.headers.get("X-MS-TOKEN-AAD-ACCESS-TOKEN");
  const idToken = request.headers.get("X-MS-TOKEN-AAD-ID-TOKEN");

  logHeaders("/api/me", "incoming request headers", request.headers);

  // If we have Easy Auth headers, forward to backend
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

      logHeaders("/api/me", "forwarded backend headers", backendHeaders);

      const response = await fetch(`${backendUrl}/api/me`, {
        headers: backendHeaders,
      });

      if (response.ok) {
        const data = await response.json();
        return NextResponse.json(data);
      }
    } catch (error) {
      console.error("Failed to fetch user info from backend:", error);
    }

    // Fallback: return info from headers directly
    return NextResponse.json({
      authenticated: true,
      userId: principalId || undefined,
      displayName: principalName || undefined,
      email: principalName?.includes("@") ? principalName : undefined,
    } satisfies UserInfo);
  }

  // No Easy Auth headers - return anonymous
  return NextResponse.json({
    authenticated: false,
  } satisfies UserInfo);
}
