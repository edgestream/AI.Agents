import { NextRequest, NextResponse } from "next/server";
import { isLocalAuth, getPostLogoutRedirectUri } from "@/app/lib/auth/config";
import { clearSessionCookie, decrypt, type SessionRef } from "@/app/lib/auth/session";
import { removeFullSession } from "@/app/lib/auth/serverSessionStore";

/**
 * GET /api/auth/logout
 *
 * Clears the local auth session cookie and optionally redirects the user
 * to the Entra ID logout endpoint.
 */
export async function GET(request: NextRequest) {
  if (!isLocalAuth()) {
    return NextResponse.json(
      { error: "Local auth is not enabled." },
      { status: 404 },
    );
  }

  // Remove the server-side session if we can identify it from the cookie
  const cookieHeader = request.cookies.get("__agui_auth")?.value;
  if (cookieHeader) {
    const ref = await decrypt<SessionRef>(cookieHeader);
    if (ref?.sessionId) removeFullSession(ref.sessionId);
  }

  const cookie = clearSessionCookie();
  const tenantId = process.env.ENTRA_TENANT_ID || process.env.AZURE_TENANT_ID;
  const postLogoutUri = encodeURIComponent(getPostLogoutRedirectUri());

  // Redirect to Entra ID logout to clear the SSO session
  const entraLogoutUrl = tenantId
    ? `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/logout?post_logout_redirect_uri=${postLogoutUri}`
    : "/";

  const response = NextResponse.redirect(entraLogoutUrl);
  response.headers.set("Set-Cookie", cookie);
  return response;
}
