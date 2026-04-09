"use client";

import { CopilotChat } from "@copilotkit/react-core/v2";

export default function Page() {
  return (
    <main>
      <CopilotChat labels={{ welcomeMessageText: "" }}/>
    </main>
  );
}