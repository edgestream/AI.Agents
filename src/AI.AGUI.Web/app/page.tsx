"use client";

import { CopilotChat, CopilotKitProvider, createA2UIMessageRenderer } from "@copilotkit/react-core/v2";
import { a2uiTheme } from "./theme";

// Module-level — stable reference, avoids re-creation on re-render
const A2UIRenderer = createA2UIMessageRenderer({ theme: a2uiTheme });
const activityRenderers = [A2UIRenderer];

export default function Page() {
  return (
    <CopilotKitProvider
      runtimeUrl="/api/copilotkit"
      renderActivityMessages={activityRenderers}
    >
      <main>
        <CopilotChat agentId="my_agent" labels={{ welcomeMessageText: "" }}/>
      </main>
    </CopilotKitProvider>
  );
}