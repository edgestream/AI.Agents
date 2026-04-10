import { CopilotRuntime, createCopilotEndpoint } from "@copilotkit/runtime/v2";
import { HttpAgent } from "@ag-ui/client";

const runtime = new CopilotRuntime({
  agents: {
    my_agent: new HttpAgent({ url: process.env.BACKEND_URL || "http://localhost:8000/" }),
  },
  a2ui: {},
});

const honoApp = createCopilotEndpoint({
  runtime,
  basePath: "/api/copilotkit",
});

export const GET = (req: Request) => honoApp.fetch(req);
export const POST = (req: Request) => honoApp.fetch(req);
