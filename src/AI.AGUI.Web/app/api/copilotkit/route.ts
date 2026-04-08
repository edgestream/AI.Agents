import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

// 1. You can use any service adapter here for multi-agent support. We use
//    the empty adapter since we're only using one agent.
const serviceAdapter = new ExperimentalEmptyAdapter();

// 2. Create the CopilotRuntime instance and utilize the Microsoft Agent Framework
//    AG-UI integration to setup the connection. Passing `a2ui: {}` activates the
//    built-in A2UI renderer so the frontend automatically renders any A2UI JSONL
//    output emitted by the agent — no custom React components required.
const runtime = new CopilotRuntime({
  agents: {
    my_agent: new HttpAgent({ url: process.env.BACKEND_URL || "http://localhost:8000/" }),
  },
  a2ui: {},
});

// 3. Build a Next.js API route that handles the CopilotKit runtime requests.
export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });

  return handleRequest(req);
};