import { cookies } from "next/headers";

/**
 * Shape of the data stored in the encrypted session cookie.
 */
export interface AuthSession {
  /** Entra ID access token */
  accessToken: string;
  /** Entra ID ID token */
  idToken: string;
  /** User object ID (oid claim) */
  principalId: string;
  /** User principal name or email (preferred_username / upn) */
  principalName: string;
}

const COOKIE_NAME = "__agui_auth";

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
 * Encrypts a JSON-serializable payload into a base64url string.
 * Layout: iv (12 bytes) ‖ ciphertext+tag (AES-GCM).
 */
async function encrypt(payload: AuthSession): Promise<string> {
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
export async function decrypt(encoded: string): Promise<AuthSession | null> {
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

/**
 * Reads the encrypted auth session from the request cookies.
 */
export async function getSession(): Promise<AuthSession | null> {
  const cookieStore = await cookies();
  const cookie = cookieStore.get(COOKIE_NAME);
  if (!cookie?.value) {
    return null;
  }
  return decrypt(cookie.value);
}

/**
 * Writes the auth session to an encrypted httpOnly cookie.
 * Returns the `Set-Cookie` header value.
 */
export async function createSessionCookie(session: AuthSession): Promise<string> {
  const value = await encrypt(session);
  // httpOnly, secure in production, sameSite=lax for OAuth redirect flow
  const parts = [
    `${COOKIE_NAME}=${value}`,
    "Path=/",
    "HttpOnly",
    "SameSite=Lax",
    `Max-Age=${60 * 60 * 8}`, // 8 hours
  ];
  if (process.env.NODE_ENV === "production") {
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
