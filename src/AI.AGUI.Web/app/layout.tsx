import type { Metadata } from "next";
import { CopilotKit } from "@copilotkit/react-core";
import "./globals.css";
import "@copilotkit/react-ui/styles.css";

export const metadata: Metadata = {
  title: "AGUIChat",
  description: "AGUIChat is an AI-powered conversational interface for your applications.",
};

export default function RootLayout({ children }: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className="antialiased">
        <CopilotKit runtimeUrl="/api/copilotkit" agent="my_agent">
          {children}
        </CopilotKit>
      </body>
    </html>
  );
}