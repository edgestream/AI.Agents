import type { AuthSession } from "./session";

/**
 * In-memory nonce store for the local auth two-hop flow.
 *
 * Chrome blocks Set-Cookie on cross-site responses (e.g. an Entra HTTPS
 * redirect back to HTTP localhost) unless the cookie has the `Secure` flag —
 * which is impossible over plain HTTP. The workaround is:
 *
 *  1. The /api/auth/callback handler stores the session under a one-time nonce
 *     and returns an HTML page.
 *  2. That page makes a same-site fetch POST to /api/auth/commit with the nonce.
 *  3. /api/auth/commit retrieves and deletes the session, sets the cookie
 *     (first-party same-site context — Chrome accepts this), and returns 200.
 *  4. The page navigates to /.
 *
 * Nonces expire after 5 minutes and are single-use.
 */

interface NonceEntry {
  session: AuthSession;
  expiresAt: number;
}

const TTL_MS = 5 * 60 * 1000; // 5 minutes

// Module-level singleton — shared across all requests within the same process.
const store = new Map<string, NonceEntry>();

export function storeNonce(nonce: string, session: AuthSession): void {
  store.set(nonce, { session, expiresAt: Date.now() + TTL_MS });
}

/**
 * Retrieves and **deletes** the session associated with the nonce (one-time use).
 * Returns null if the nonce is unknown or expired.
 */
export function retrieveAndDeleteNonce(nonce: string): AuthSession | null {
  const entry = store.get(nonce);
  store.delete(nonce);
  if (!entry || Date.now() > entry.expiresAt) return null;
  return entry.session;
}
