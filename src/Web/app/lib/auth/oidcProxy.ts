import type { NextRequest } from "next/server";

export interface OidcProxyIdentity {
  userId?: string;
  principalName?: string;
  email?: string;
  accessToken?: string;
  authorization?: string;
}

function getEmailLikeValue(...values: Array<string | null | undefined>): string | undefined {
  for (const value of values) {
    if (value?.includes("@")) {
      return value;
    }
  }

  return undefined;
}

/**
 * Reads the standard oauth2-proxy authentication headers forwarded by ingress.
 */
export function getOidcProxyIdentity(request: Pick<NextRequest, "headers">): OidcProxyIdentity {
  const userId = request.headers.get("X-Auth-Request-User") ?? undefined;
  const preferredUsername = request.headers.get("X-Auth-Request-Preferred-Username") ?? undefined;
  const email = request.headers.get("X-Auth-Request-Email") ?? undefined;
  const accessToken = request.headers.get("X-Auth-Request-Access-Token") ?? undefined;
  const authorizationHeader = request.headers.get("Authorization") ?? undefined;

  return {
    userId,
    principalName: preferredUsername ?? email ?? userId,
    email: getEmailLikeValue(email, preferredUsername, userId),
    accessToken,
    authorization: authorizationHeader ?? (accessToken ? `Bearer ${accessToken}` : undefined),
  };
}
