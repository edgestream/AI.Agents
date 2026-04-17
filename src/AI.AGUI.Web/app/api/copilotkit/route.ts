import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";
import { resolveLocalSession } from "@/app/lib/auth/resolveLocalSession";

const serviceAdapter = new ExperimentalEmptyAdapter();

/**
 * Creates an HttpAgent with forwarded authentication headers.
 */
async function createAuthenticatedAgent(request: NextRequest): Promise<HttpAgent> {
  const backendUrl = (process.env.BACKEND_URL || "http://localhost:8000").replace(/\/+$/, "");
  const authHeaders: Record<string, string> = {};

  let principalId = request.headers.get("X-MS-CLIENT-PRINCIPAL-ID");
  let principalName = request.headers.get("X-MS-CLIENT-PRINCIPAL-NAME");
  let accessToken = request.headers.get("X-MS-TOKEN-AAD-ACCESS-TOKEN");
  let idToken = request.headers.get("X-MS-TOKEN-AAD-ID-TOKEN");

  // In local auth mode, resolve session directly (middleware can't share in-memory state)
  if (!principalId && !principalName) {
    const localSession = await resolveLocalSession(request);
    if (localSession) {
      principalId = localSession.principalId;
      principalName = localSession.principalName;
      accessToken = localSession.accessToken;
      idToken = localSession.idToken;
    }
  }

  if (principalId) authHeaders["X-MS-CLIENT-PRINCIPAL-ID"] = principalId;
  if (principalName) authHeaders["X-MS-CLIENT-PRINCIPAL-NAME"] = principalName;
  if (accessToken) authHeaders["X-MS-TOKEN-AAD-ACCESS-TOKEN"] = accessToken;
  if (idToken) authHeaders["X-MS-TOKEN-AAD-ID-TOKEN"] = idToken;

  return new HttpAgent({
    url: backendUrl,
    headers: authHeaders,
  });
}

export const POST = async (req: NextRequest) => {
  // Create a runtime with an authenticated agent for this request
  const runtime = new CopilotRuntime({
    agents: {
      my_agent: await createAuthenticatedAgent(req),
    },
    a2ui: {},
  });

  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};
