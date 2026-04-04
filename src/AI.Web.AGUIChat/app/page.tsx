"use client";

import { CopilotChat } from "@copilotkit/react-core/v2";
import { ToolRenderers } from "./components/ToolRenderers";

export default function Page() {
  return (
    <main className="chat-page">
      <section className="chat-shell">
        <ToolRenderers />
        <CopilotChat labels={{ welcomeMessageText: "" }} chatView="chat-view" />
      </section>
    </main>
  );
}