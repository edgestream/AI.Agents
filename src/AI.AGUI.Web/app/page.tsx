"use client";

import { CopilotChat, CopilotKitProvider } from "@copilotkit/react-core/v2";
import { a2uiTheme } from "./theme";

export default function Page() {
  return (
    <CopilotKitProvider
      runtimeUrl="/api/copilotkit"
      a2ui={{ theme: a2uiTheme }}
      showDevConsole={true}>
      <main>
        <CopilotChat agentId="my_agent" labels={{ welcomeMessageText: "" }} />
      </main>
    </CopilotKitProvider>
  );
}