"use client";

import { CopilotKit } from "@copilotkit/react-core";
import { appA2UICatalog } from "../lib/a2ui/catalog";

type CopilotKitShellProps = {
  children: React.ReactNode;
};

export function CopilotKitShell({ children }: CopilotKitShellProps) {
  return (
    <CopilotKit
      runtimeUrl="/api/copilotkit"
      agent="my_agent"
      a2ui={{
        catalog: appA2UICatalog,
        includeSchema: true,
      }}
    >
      {children}
    </CopilotKit>
  );
}