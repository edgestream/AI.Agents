"use client";

import { CopilotChat, useDefaultRenderTool } from "@copilotkit/react-core/v2";

export default function Page() {
  useDefaultRenderTool();

  return (
    <main>
      <CopilotChat labels={{ welcomeMessageText: "" }}/>
    </main>
  );
}