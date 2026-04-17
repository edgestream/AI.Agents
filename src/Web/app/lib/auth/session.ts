import { cookies } from "next/headers";
import { type FullSession, getFullSession } from "./serverSessionStore";

/**
 * Shape of the data stored in the encrypted session cookie.
 * Contains only a reference (session ID) to the full session stored
 * server-side in serverSessionStore. This keeps the cookie under 4 KB.
 */
export interface SessionRef {
  sessionId: string;
}

/**
 * Full shape of the authenticated session (access token, ID token, principal).
 * Stored server-side in serverSessionStore — never serialised into the cookie.
 * @deprecated Use FullSession from serverSessionStore directly.
 */
export type AuthSession = FullSession;

export const COOKIE_NAME = "__agui_auth";

/**
 * Derives a 256-bit AES-GCM key from the `AUTH_SESSION_SECRET` env var
 * using PBKDF2 (Web Crypto API — works in both Node.js and Edge runtimes).
 */
async function deriveKey(): Promise<CryptoKey> {
  const secret = process.env.AUTH_SESSION_SECRET;
  if (!secret) {
    throw new Error("AUTH_SESSION_SECRET environment variable is required for local auth mode");
  }

  const encoder = new TextEncoder();
  const keyMaterial = await crypto.subtle.importKey(
    "raw",
    encoder.encode(secret),
    "PBKDF2",
    false,
    ["deriveKey"],
  );

  return crypto.subtle.deriveKey(
    {
      name: "PBKDF2",
      salt: encoder.encode("agui-local-auth"),
      iterations: 100_000,
      hash: "SHA-256",
    },
    keyMaterial,
    { name: "AES-GCM", length: 256 },
    false,
    ["encrypt", "decrypt"],
  );
}

/** Encode Uint8Array → base64url string. */
function toBase64Url(bytes: Uint8Array): string {
  let binary = "";
  for (const b of bytes) binary += String.fromCharCode(b);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

/** Decode base64url string → Uint8Array. */
function fromBase64Url(encoded: string): Uint8Array {
  const padded = encoded.replace(/-/g, "+").replace(/_/g, "/");
  const binary = atob(padded);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

/**
 * Encrypts a JSON-serialisable payload into a base64url string.
 * Layout: iv (12 bytes) ‖ ciphertext+tag (AES-GCM).
 */
export async function encrypt<T = AuthSession>(payload: T): Promise<string> {
  const key = await deriveKey();
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const data = new TextEncoder().encode(JSON.stringify(payload));

  const encrypted = await crypto.subtle.encrypt({ name: "AES-GCM", iv }, key, data);

  const combined = new Uint8Array(iv.length + encrypted.byteLength);
  combined.set(iv);
  combined.set(new Uint8Array(encrypted), iv.length);

  return toBase64Url(combined);
}

/**
 * Decrypts a base64url string into the original payload.
 * Returns `null` if decryption fails (e.g. tampered cookie).
 */
export async function decrypt<T = AuthSession>(encoded: string): Promise<T | null> {
  try {
    const key = await deriveKey();
    const combined = fromBase64Url(encoded);
    const iv = combined.slice(0, 12);
    const data = combined.slice(12);

    const decrypted = await crypto.subtle.decrypt({ name: "AES-GCM", iv }, key, data);
    return JSON.parse(new TextDecoder().decode(decrypted)) as T;
  } catch {
    return null;
  }
}

/**
 * Sets the encrypted session cookie imperatively using next/headers cookies().
 * This is the most reliable approach in Next.js App Router route handlers because
 * Next.js manages the Set-Cookie header at the framework level.
 */
export async function setSessionCookieImperative(session: AuthSession): Promise<void> {
  const value = await encrypt(session);
  const cookieStore = await cookies();
  cookieStore.set(COOKIE_NAME, value, {
    path: "/",
    httpOnly: true,
    sameSite: "lax",
    maxAge: COOKIE_MAX_AGE,
    secure: isCookieSecure(),
  });
}

/**
 * Reads the encrypted auth session from the request cookies.
 * Decrypts the session reference from the cookie, then looks up the full
 * session in the server-side session store.
 */
export async function getSession(): Promise<AuthSession | null> {
  const cookieStore = await cookies();
  const cookie = cookieStore.get(COOKIE_NAME);
  if (!cookie?.value) {
    return null;
  }
  const ref = await decrypt<SessionRef>(cookie.value);
  if (!ref?.sessionId) {
    return null;
  }
  return getFullSession(ref.sessionId);
}

export const COOKIE_MAX_AGE = 60 * 60 * 8; // 8 hours

/**
 * Returns whether the session cookie should have the Secure flag.
 * Disabled when AUTH_COOKIE_SECURE=false (e.g. local HTTP dev).
 */
export function isCookieSecure(): boolean {
  return process.env.NODE_ENV === "production" && process.env.AUTH_COOKIE_SECURE !== "false";
}

/**
 * Sets the encrypted session cookie on a NextResponse using the type-safe
 * `response.cookies.set()` API (preferred over raw Set-Cookie headers in
 * Next.js App Router, which can silently drop headers on redirects).
 */
export async function setSessionCookie(
  response: { cookies: { set: (name: string, value: string, options: Record<string, unknown>) => void } },
  session: AuthSession,
): Promise<void> {
  const value = await encrypt(session);
  response.cookies.set(COOKIE_NAME, value, {
    path: "/",
    httpOnly: true,
    sameSite: "lax",
    maxAge: COOKIE_MAX_AGE,
    secure: isCookieSecure(),
  });
}

/**
 * Writes the auth session to an encrypted httpOnly cookie.
 * Returns the `Set-Cookie` header value.
 * @deprecated Prefer {@link setSessionCookie} which uses response.cookies.set().
 */
export async function createSessionCookie(session: AuthSession): Promise<string> {
  const value = await encrypt(session);
  const parts = [
    `${COOKIE_NAME}=${value}`,
    "Path=/",
    "HttpOnly",
    "SameSite=Lax",
    `Max-Age=${COOKIE_MAX_AGE}`,
  ];
  if (isCookieSecure()) {
    parts.push("Secure");
  }
  return parts.join("; ");
}

/**
 * Returns a `Set-Cookie` header value that clears the session cookie.
 */
export function clearSessionCookie(): string {
  return `${COOKIE_NAME}=; Path=/; HttpOnly; SameSite=Lax; Max-Age=0`;
}
