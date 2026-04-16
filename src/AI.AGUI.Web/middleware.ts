import { NextRequest, NextResponse } from "next/server";

/**
 * Next.js middleware that, when `AUTH_MODE=local`, reads the encrypted session
 * cookie and injects the standard `X-MS-*` Easy Auth headers into matched
 * requests so that route handlers and the backend see the same contract as
 * Azure Container Apps Easy Auth.
 *
 * Routes that are matched by this middleware:
 * - `/api/me`
 * - `/api/copilotkit`
 */

const COOKIE_NAME = "__agui_auth";

interface AuthSession {
  accessToken: string;
  idToken: string;
  principalId: string;
  principalName: string;
}

async function deriveKey(): Promise<CryptoKey> {
  const secret = process.env.AUTH_SESSION_SECRET;
  if (!secret) return await crypto.subtle.importKey("raw", new Uint8Array(32), "AES-GCM", false, ["decrypt"]);

  const encoder = new TextEncoder();
  const keyMaterial = await crypto.subtle.importKey("raw", encoder.encode(secret), "PBKDF2", false, ["deriveKey"]);
  return crypto.subtle.deriveKey(
    { name: "PBKDF2", salt: encoder.encode("agui-local-auth"), iterations: 100_000, hash: "SHA-256" },
    keyMaterial,
    { name: "AES-GCM", length: 256 },
    false,
    ["decrypt"],
  );
}

function fromBase64Url(encoded: string): Uint8Array {
  const padded = encoded.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(padded);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

async function decryptSession(encoded: string): Promise<AuthSession | null> {
  try {
    const key = await deriveKey();
    const combined = fromBase64Url(encoded);
    const iv = combined.slice(0, 12);
    const data = combined.slice(12);
    const decrypted = await crypto.subtle.decrypt({ name: "AES-GCM", iv }, key, data);
    return JSON.parse(new TextDecoder().decode(decrypted)) as AuthSession;
  } catch {
    return null;
  }
}

export async function middleware(request: NextRequest) {
  if (process.env.AUTH_MODE !== "local") {
    return NextResponse.next();
  }

  const cookie = request.cookies.get(COOKIE_NAME);
  if (!cookie?.value) {
    return NextResponse.next();
  }

  const session = await decryptSession(cookie.value);
  if (!session) {
    return NextResponse.next();
  }

  // Clone request headers and inject the Easy Auth headers from the session
  const requestHeaders = new Headers(request.headers);

  if (session.principalId) {
    requestHeaders.set("X-MS-CLIENT-PRINCIPAL-ID", session.principalId);
  }
  if (session.principalName) {
    requestHeaders.set("X-MS-CLIENT-PRINCIPAL-NAME", session.principalName);
  }
  if (session.accessToken) {
    requestHeaders.set("X-MS-TOKEN-AAD-ACCESS-TOKEN", session.accessToken);
  }
  if (session.idToken) {
    requestHeaders.set("X-MS-TOKEN-AAD-ID-TOKEN", session.idToken);
  }

  return NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });
}

/**
 * Only run the middleware on API routes that consume auth headers.
 */
export const config = {
  matcher: ["/api/me", "/api/copilotkit"],
};
