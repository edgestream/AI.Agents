"use client";

import {
  CopilotKitCSSProperties,
  CopilotChat,
  AssistantMessage,
  UserMessage,
} from "@copilotkit/react-ui";
import { useRenderActivityMessage } from "@copilotkit/react-core/v2";
import { UserMenu } from "./components/UserAvatar";
import { TokenUsageDisplay } from "./components/TokenUsageDisplay";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function ActivityAwareRenderMessage(props: any) {
  const {
    message,
    messages,
    inProgress,
    index,
    isCurrentMessage,
    onRegenerate,
    onCopy,
    onThumbsUp,
    onThumbsDown,
    messageFeedback,
    markdownTagRenderers,
    ImageRenderer,
  } = props;
  const { renderActivityMessage } = useRenderActivityMessage();

  switch (message.role) {
    case "user":
      return (
        <UserMessage
          rawData={message}
          data-message-role="user"
          message={message}
          ImageRenderer={ImageRenderer}
          key={index}
        />
      );
    case "assistant":
      return (
        <AssistantMessage
          data-message-role="assistant"
          subComponent={message.generativeUI?.()}
          rawData={message}
          message={message}
          messages={messages}
          isLoading={inProgress && isCurrentMessage && !message.content}
          isGenerating={inProgress && isCurrentMessage && !!message.content}
          isCurrentMessage={isCurrentMessage}
          onRegenerate={() => onRegenerate?.(message.id)}
          onCopy={onCopy}
          onThumbsUp={onThumbsUp}
          onThumbsDown={onThumbsDown}
          feedback={messageFeedback?.[message.id] ?? null}
          markdownTagRenderers={markdownTagRenderers}
          ImageRenderer={ImageRenderer}
          key={index}
        />
      );
    case "activity":
      return renderActivityMessage(message) ?? null;
    default:
      return null;
  }
}

export default function Page() {
  return (
    <>
      {/* Header with user info */}
      <header className="flex-none border-b border-gray-200 bg-white">
        <div className="flex items-center justify-end px-4 py-2">
          <UserMenu />
        </div>
      </header>
      
      {/* Main chat area */}
      <main
        className="flex-1 min-h-0"
        style={
          { "--copilot-kit-primary-color": "#383b99" } as CopilotKitCSSProperties
        }
      >
        <CopilotChat
          className="h-full"
          disableSystemMessage={true}
          RenderMessage={ActivityAwareRenderMessage}
          labels={{
            title: "",
            initial: "👋 Hi! How can I help you today?",
          }}
        />
      </main>
      
      {/* Token usage display */}
      <TokenUsageDisplay />
    </>
  );
}