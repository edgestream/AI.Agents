import { type NextRequest } from "next/server";
import { isLocalAuth } from "./config";
import { decrypt, type SessionRef } from "./session";
import { getFullSession, type FullSession } from "./serverSessionStore";

const COOKIE_NAME = "__agui_auth";

/**
 * Reads the local-auth session from the encrypted cookie in the request.
 *
 * Used by route handlers (`/api/me`, `/api/copilotkit`) to resolve the
 * server-side session when running in `AUTH_MODE=local`. Route handlers share
 * module-level memory with the commit handler that stored the session, so
 * the `serverSessionStore` Map is accessible here.
 *
 * Middleware runs in a separate worker and cannot share this state — that is
 * why header injection is done in route handlers instead of middleware.
 *
 * Returns `null` if not in local auth mode, cookie is absent, or session has
 * expired.
 */
export async function resolveLocalSession(
  request: NextRequest,
): Promise<FullSession | null> {
  if (!isLocalAuth()) return null;

  const cookie = request.cookies.get(COOKIE_NAME);
  if (!cookie?.value) return null;

  const ref = await decrypt<SessionRef>(cookie.value);
  if (!ref?.sessionId) return null;

  return getFullSession(ref.sessionId);
}
