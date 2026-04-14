"use client";

import { CopilotKitCSSProperties, CopilotChat } from "@copilotkit/react-ui";

export default function Page() {
  return (
    <main
      className="h-screen"
      style={
        { "--copilot-kit-primary-color": "#383b99" } as CopilotKitCSSProperties
      }
    >
      <CopilotChat
        className="h-full"
        disableSystemMessage={true}
        labels={{
          title: "AGUIChat",
          initial: "👋 Hi! How can I help you today?",
        }}
      />
    </main>
  );
}