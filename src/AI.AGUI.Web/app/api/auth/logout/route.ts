import { NextResponse } from "next/server";
import { isLocalAuth, getPostLogoutRedirectUri } from "@/app/lib/auth/config";
import { clearSessionCookie } from "@/app/lib/auth/session";

/**
 * GET /api/auth/logout
 *
 * Clears the local auth session cookie and optionally redirects the user
 * to the Entra ID logout endpoint.
 */
export async function GET() {
  if (!isLocalAuth()) {
    return NextResponse.json(
      { error: "Local auth is not enabled." },
      { status: 404 },
    );
  }

  const cookie = clearSessionCookie();
  const tenantId = process.env.AZURE_AD_TENANT_ID;
  const postLogoutUri = encodeURIComponent(getPostLogoutRedirectUri());

  // Redirect to Entra ID logout to clear the SSO session
  const entraLogoutUrl = tenantId
    ? `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/logout?post_logout_redirect_uri=${postLogoutUri}`
    : "/";

  const response = NextResponse.redirect(entraLogoutUrl);
  response.headers.set("Set-Cookie", cookie);
  return response;
}
