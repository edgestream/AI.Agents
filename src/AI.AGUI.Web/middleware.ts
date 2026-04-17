import { NextRequest, NextResponse } from "next/server";

/**
 * Next.js middleware for auth-related routes.
 *
 * In ACA (Azure Container Apps) mode, Easy Auth headers are already injected
 * by the platform — no middleware work needed.
 *
 * In local auth mode, session resolution is done directly inside the route
 * handlers (`/api/me`, `/api/copilotkit`) by calling `resolveLocalSession`.
 * Middleware cannot do it here because middleware runs in a separate Edge
 * worker that does not share in-memory state with route handlers.
 */
export function middleware(_request: NextRequest) {
  return NextResponse.next();
}

/**
 * Only run the middleware on API routes that consume auth headers.
 */
export const config = {
  matcher: ["/api/me", "/api/copilotkit"],
};
