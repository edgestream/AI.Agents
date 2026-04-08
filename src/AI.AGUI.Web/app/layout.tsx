import type { Metadata } from "next";
import { CopilotKit } from "@copilotkit/react-core"; 
import "@copilotkit/react-ui/v2/styles.css";
import "./globals.css";

export const metadata: Metadata = {
  title: "AGUIChat",
  description: "AGUIChat is an AI-powered conversational interface for your applications.",
};

export default function RootLayout({ children }: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body>
        <CopilotKit runtimeUrl="/api/copilotkit" agent="my_agent">
          {children}
        </CopilotKit>
      </body>
    </html>
  );
}