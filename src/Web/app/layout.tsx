import type { Metadata } from "next";
import { CopilotKitShell } from "./components/CopilotKitShell";
import "./globals.css";
import "@copilotkit/react-ui/styles.css";

export const metadata: Metadata = {
  title: "Agents",
  description: "AGUIChat is an AI-powered conversational interface for your applications.",
};

export default function RootLayout({ children }: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className="antialiased flex flex-col h-screen">
        <CopilotKitShell>{children}</CopilotKitShell>
      </body>
    </html>
  );
}