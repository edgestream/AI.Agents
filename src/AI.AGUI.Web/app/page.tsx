"use client";

import { useEffect, useState } from "react";
import { CopilotChat, CopilotKitProvider } from "@copilotkit/react-core/v2";
import { a2uiTheme } from "./theme";

type CatalogEntry = { id: string; displayName: string; route: string };

export default function Page() {
  const [catalog, setCatalog] = useState<CatalogEntry[]>([]);
  const [selectedId, setSelectedId] = useState<string>("");

  useEffect(() => {
    fetch("/api/applications")
      .then((r) => r.json())
      .then((apps: CatalogEntry[]) => {
        setCatalog(apps);
        if (apps.length > 0) setSelectedId(apps[0].id);
      })
      .catch(() => {
        const fallback: CatalogEntry[] = [{ id: "agui-agent", displayName: "AGUI Agent", route: "/agents/agui-agent" }];
        setCatalog(fallback);
        setSelectedId("agui-agent");
      });
  }, []);

  return (
    <CopilotKitProvider
      runtimeUrl="/api/copilotkit"
      a2ui={{ theme: a2uiTheme }}
      showDevConsole={true}
    >
      <main>
        {catalog.length > 1 && (
          <div style={{ padding: "8px 16px", borderBottom: "1px solid #e0e0e0", display: "flex", alignItems: "center", gap: 8 }}>
            <label htmlFor="agent-selector" style={{ fontSize: 14, color: "#666" }}>Agent:</label>
            <select
              id="agent-selector"
              value={selectedId}
              onChange={(e) => setSelectedId(e.target.value)}
              style={{ fontSize: 14, padding: "2px 8px", borderRadius: 4, border: "1px solid #ccc" }}
            >
              {catalog.map((app) => (
                <option key={app.id} value={app.id}>{app.displayName}</option>
              ))}
            </select>
          </div>
        )}
        <CopilotChat agentId={selectedId || "agui-agent"} labels={{ welcomeMessageText: "" }} />
      </main>
    </CopilotKitProvider>
  );
}
