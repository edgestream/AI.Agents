"use client";

import {
  CopilotKitCSSProperties,
  CopilotChat,
  AssistantMessage,
  UserMessage,
} from "@copilotkit/react-ui";
import { useRenderActivityMessage } from "@copilotkit/react-core/v2";

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
    <main
      className="h-screen"
      style={
        { "--copilot-kit-primary-color": "#383b99" } as CopilotKitCSSProperties
      }
    >
      <CopilotChat
        className="h-full"
        disableSystemMessage={true}
        RenderMessage={ActivityAwareRenderMessage}
        labels={{
          title: "AGUIChat",
          initial: "👋 Hi! How can I help you today?",
        }}
      />
    </main>
  );
}