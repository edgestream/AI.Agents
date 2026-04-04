"use client";

import { CopilotChat } from "@copilotkit/react-core/v2";

export default function Page() {
  return (
    <main className="chat-page">
      <section className="chat-shell">
        <CopilotChat labels={{ welcomeMessageText: "" }} chatView="chat-view" />
      </section>
    </main>
  );
}