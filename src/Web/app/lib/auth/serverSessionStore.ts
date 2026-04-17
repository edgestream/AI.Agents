/**
 * Server-side session store. Stores the full Entra ID session (access token,
 * ID token, principal info) in memory, keyed by a random session ID.
 *
 * The browser cookie holds only the encrypted session ID so the total cookie
 * size stays well under the 4 KB browser limit. Raw Azure AD JWTs are
 * typically 5–6 KB when encrypted, which causes Chrome to silently drop the
 * cookie entirely.
 *
 * TTL matches COOKIE_MAX_AGE (8 hours). In-memory only: sessions are lost on
 * server restart (intentional — this is a local dev auth mode).
 *
 * IMPORTANT: This module must only be used in Node.js runtime contexts
 * (route handlers, Node.js middleware). It will NOT work in Edge runtime.
 */

/** Full Entra ID session — stored server-side, never serialised into the cookie. */
export interface FullSession {
  accessToken: string;
  idToken: string;
  principalId: string;
  principalName: string;
}

interface SessionEntry {
  session: FullSession;
  expiresAt: number;
}

/** 8 hours in ms — must match COOKIE_MAX_AGE in session.ts. */
const TTL_MS = 8 * 60 * 60 * 1000;

const store = new Map<string, SessionEntry>();

export function storeFullSession(sessionId: string, session: FullSession): void {
  store.set(sessionId, { session, expiresAt: Date.now() + TTL_MS });
  // Prune expired entries periodically to avoid unbounded memory growth
  if (store.size % 50 === 0) prune();
}

export function getFullSession(sessionId: string): FullSession | null {
  const entry = store.get(sessionId);
  if (!entry) return null;
  if (Date.now() > entry.expiresAt) {
    store.delete(sessionId);
    return null;
  }
  return entry.session;
}

export function removeFullSession(sessionId: string): void {
  store.delete(sessionId);
}

function prune(): void {
  const now = Date.now();
  for (const [key, entry] of store) {
    if (now > entry.expiresAt) store.delete(key);
  }
}
