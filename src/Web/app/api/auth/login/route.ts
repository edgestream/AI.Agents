import { NextResponse } from "next/server";
import { isLocalAuth } from "@/app/lib/auth/config";
import { getAuthCodeUrl } from "@/app/lib/auth/msal";

/**
 * GET /api/auth/login
 *
 * Initiates the Entra ID sign-in flow by redirecting the user to the
 * Microsoft authorization endpoint. Only active in local auth mode.
 */
export async function GET() {
  if (!isLocalAuth()) {
    return NextResponse.json(
      { error: "Local auth is not enabled. Set AUTH_MODE=local to use this endpoint." },
      { status: 404 },
    );
  }

  const authUrl = await getAuthCodeUrl();
  return NextResponse.redirect(authUrl);
}
