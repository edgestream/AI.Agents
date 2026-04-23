import { NextRequest, NextResponse } from "next/server";
import { isLocalAuth, getAppRootUri } from "@/app/lib/auth/config";
import { retrieveAndDeleteNonce } from "@/app/lib/auth/nonceStore";
import { storeFullSession } from "@/app/lib/auth/serverSessionStore";
import { COOKIE_NAME, COOKIE_MAX_AGE, isCookieSecure, encrypt, type SessionRef } from "@/app/lib/auth/session";

/**
 * GET /api/auth/commit?nonce=
 *
 * Second leg of the local-auth two-hop flow. Retrieves the full session from
 * the nonce store, saves it in the server-side session store (to avoid the
 * 4 KB browser cookie size limit — Azure AD JWTs are ~5–6 KB encrypted), and
 * sets a small encrypted cookie that contains only a session ID reference.
 */
export async function GET(request: NextRequest) {
  if (!isLocalAuth()) {
    return NextResponse.json({ error: "Local auth is not enabled." }, { status: 404 });
  }

  const nonce = request.nextUrl.searchParams.get("nonce");
  if (!nonce) {
    return NextResponse.json({ error: "Missing nonce" }, { status: 400 });
  }

  const session = retrieveAndDeleteNonce(nonce);
  if (!session) {
    return NextResponse.json({ error: "Invalid or expired nonce" }, { status: 400 });
  }

  // Store full session server-side; only put a tiny session ID in the cookie.
  const sessionId = crypto.randomUUID();
  storeFullSession(sessionId, session);

  const sessionRef: SessionRef = { sessionId };
  const value = await encrypt<SessionRef>(sessionRef);
  const cookieString = [
    `${COOKIE_NAME}=${value}`,
    "Path=/",
    "HttpOnly",
    "SameSite=Lax",
    `Max-Age=${COOKIE_MAX_AGE}`,
    ...(isCookieSecure() ? ["Secure"] : []),
  ].join("; ");

  const appRoot = getAppRootUri(request);
  const safeAppRoot = encodeURI(appRoot);
  const html = `<!DOCTYPE html><html><head><meta http-equiv="refresh" content="0; url=${safeAppRoot}"><title>Signing in\u2026</title></head><body></body></html>`;
  return new Response(html, {
    status: 200,
    headers: {
      "Content-Type": "text/html; charset=utf-8",
      "Set-Cookie": cookieString,
      "Cache-Control": "no-store",
    },
  });
}

