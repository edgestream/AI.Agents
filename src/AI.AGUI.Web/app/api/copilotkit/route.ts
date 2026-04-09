import {
  CopilotRuntime,
  createCopilotRuntimeHandler,
} from "@copilotkit/runtime/v2";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

// 1. Create the CopilotRuntime instance and utilize the Microsoft Agent Framework
//    AG-UI integration to setup the connection. Passing `a2ui: {}` activates the
//    built-in A2UI renderer so the frontend automatically renders any A2UI JSONL
//    output emitted by the agent — no custom React components required.
const runtime = new CopilotRuntime({
  agents: {
    default: new HttpAgent({ url: process.env.BACKEND_URL || "http://localhost:8000/" }),
  },
  a2ui: {},
});

// 2. Build a Next.js API route that handles the CopilotKit runtime requests.
const handler = createCopilotRuntimeHandler({
  runtime,
  basePath: "/api/copilotkit",
  mode: "single-route",
});

export const POST = (req: NextRequest) => handler(req);