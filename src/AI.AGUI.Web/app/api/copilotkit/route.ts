import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

const serviceAdapter = new ExperimentalEmptyAdapter();

/**
 * Creates an HttpAgent with forwarded authentication headers.
 */
function createAuthenticatedAgent(request: NextRequest): HttpAgent {
  const backendUrl = process.env.BACKEND_URL || "http://localhost:8000/";
  
  // Extract Easy Auth headers to forward to the backend
  const authHeaders: Record<string, string> = {};
  
  const principalId = request.headers.get("X-MS-CLIENT-PRINCIPAL-ID");
  const principalName = request.headers.get("X-MS-CLIENT-PRINCIPAL-NAME");
  const accessToken = request.headers.get("X-MS-TOKEN-AAD-ACCESS-TOKEN");
  const idToken = request.headers.get("X-MS-TOKEN-AAD-ID-TOKEN");
  
  if (principalId) authHeaders["X-MS-CLIENT-PRINCIPAL-ID"] = principalId;
  if (principalName) authHeaders["X-MS-CLIENT-PRINCIPAL-NAME"] = principalName;
  if (accessToken) authHeaders["X-MS-TOKEN-AAD-ACCESS-TOKEN"] = accessToken;
  if (idToken) authHeaders["X-MS-TOKEN-AAD-ID-TOKEN"] = idToken;
  
  // Also check for Authorization header (if frontend sends it)
  const authorizationHeader = request.headers.get("Authorization");
  if (authorizationHeader) {
    authHeaders["Authorization"] = authorizationHeader;
  }
  
  return new HttpAgent({
    url: backendUrl,
    headers: authHeaders,
  });
}

export const POST = async (req: NextRequest) => {
  // Create a runtime with an authenticated agent for this request
  const runtime = new CopilotRuntime({
    agents: {
      my_agent: createAuthenticatedAgent(req),
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
