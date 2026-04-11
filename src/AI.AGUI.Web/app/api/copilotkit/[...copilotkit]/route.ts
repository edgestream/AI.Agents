import { CopilotRuntime, createCopilotEndpoint } from "@copilotkit/runtime/v2";
import { HttpAgent } from "@ag-ui/client";

const BACKEND_URL = process.env.BACKEND_URL ?? "http://localhost:8000";

type CatalogEntry = { id: string; displayName: string; route: string };

let cachedAgents: Record<string, HttpAgent> | null = null;

async function loadAgents(): Promise<Record<string, HttpAgent>> {
  if (cachedAgents) return cachedAgents;
  try {
    const res = await fetch(`${BACKEND_URL}/applications`, { cache: "no-store" });
    const apps: CatalogEntry[] = await res.json();
    if (apps.length > 0) {
      cachedAgents = Object.fromEntries(
        apps.map((app) => [app.id, new HttpAgent({ url: `${BACKEND_URL}${app.route}` })])
      );
      return cachedAgents;
    }
  } catch {
    // fall through to default
  }
  cachedAgents = {
    "agui-agent": new HttpAgent({ url: `${BACKEND_URL}/agents/agui-agent` }),
  };
  return cachedAgents;
}

async function handleRequest(req: Request) {
  const agents = await loadAgents();
  const runtime = new CopilotRuntime({ agents, a2ui: {} });
  const honoApp = createCopilotEndpoint({ runtime, basePath: "/api/copilotkit" });
  return honoApp.fetch(req);
}

export const GET = handleRequest;
export const POST = handleRequest;
