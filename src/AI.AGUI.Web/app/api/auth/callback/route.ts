import { NextRequest, NextResponse } from "next/server";
import { isLocalAuth } from "@/app/lib/auth/config";
import { acquireTokenByCode } from "@/app/lib/auth/msal";
import { createSessionCookie, type AuthSession } from "@/app/lib/auth/session";

/**
 * GET /api/auth/callback
 *
 * Handles the redirect from Entra ID after the user has authenticated.
 * Exchanges the authorization code for tokens, stores them in an encrypted
 * session cookie, and redirects the user back to the application root.
 */
export async function GET(request: NextRequest) {
  if (!isLocalAuth()) {
    return NextResponse.json(
      { error: "Local auth is not enabled." },
      { status: 404 },
    );
  }

  const code = request.nextUrl.searchParams.get("code");
  if (!code) {
    const errorDescription = request.nextUrl.searchParams.get("error_description") || "Unknown error";
    return NextResponse.json(
      { error: "Authorization code not found", detail: errorDescription },
      { status: 400 },
    );
  }

  try {
    const result = await acquireTokenByCode(code);

    if (!result) {
      return NextResponse.json({ error: "Token acquisition failed" }, { status: 500 });
    }

    // Extract claims from the ID token
    const account = result.account;
    const principalId = account?.localAccountId ?? account?.homeAccountId ?? "";
    const principalName =
      (result.idTokenClaims as Record<string, unknown>)?.preferred_username as string
      ?? (result.idTokenClaims as Record<string, unknown>)?.upn as string
      ?? account?.username
      ?? "";

    const session: AuthSession = {
      accessToken: result.accessToken,
      idToken: result.idToken,
      principalId,
      principalName,
    };

    const cookie = await createSessionCookie(session);
    const response = NextResponse.redirect(new URL("/", request.url));
    response.headers.set("Set-Cookie", cookie);
    return response;
  } catch (error) {
    console.error("Token acquisition failed:", error);
    return NextResponse.json(
      { error: "Failed to complete authentication", detail: String(error) },
      { status: 500 },
    );
  }
}
