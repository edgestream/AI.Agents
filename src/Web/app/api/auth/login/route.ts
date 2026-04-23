import { NextRequest, NextResponse } from "next/server";
import { canSignIn, getRedirectUri, isLocalAuth } from "@/app/lib/auth/config";
import { getAuthCodeUrl } from "@/app/lib/auth/msal";

/**
 * GET /api/auth/login
 *
 * Initiates the Entra ID sign-in flow by redirecting the user to the
 * Microsoft authorization endpoint. Only active in local auth mode.
 */
export async function GET(request: NextRequest) {
  if (!isLocalAuth()) {
    return NextResponse.json(
      { error: "Local auth is disabled because AUTH_MODE=aca." },
      { status: 404 },
    );
  }

  if (!canSignIn()) {
    return NextResponse.json(
      { error: "Local sign-in is not configured for this process." },
      { status: 404 },
    );
  }

  try {
    const authUrl = await getAuthCodeUrl(getRedirectUri(request));
    return NextResponse.redirect(authUrl);
  } catch (error) {
    console.error("Failed to start local authentication:", error);
    return NextResponse.json(
      {
        error: "Local auth is not configured for this process.",
        detail: String(error),
      },
      { status: 500 },
    );
  }
}
