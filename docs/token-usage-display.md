# Token Usage Display Feature

This document describes the token usage display feature implementation.

## Overview

The token usage display feature shows the number of input and output tokens used by LLM/tool requests in the AG-UI interface. This helps users monitor their token consumption and understand the cost of their interactions with AI agents.

## Features

- **Real-time tracking**: Captures token usage from all LLM calls
- **Visual indicators**: Shows ↑ (up arrow) for input tokens and ↓ (down arrow) for output tokens
- **Non-intrusive UI**: Displays as a small floating card in the bottom-right corner
- **Auto-hide**: Card automatically hides after 10 seconds to avoid clutter
- **Historical data**: Maintains last 100 usage records for analysis

## Architecture

### Backend Components

1. **TokenUsageTrackingChatClient** (`src/Microsoft/Client/TokenUsageTrackingChatClient.cs`)
   - Wraps `IChatClient` to intercept LLM calls
   - Captures `UsageDetails` from `ChatResponse`
   - Records usage to `TokenUsageStore`

2. **TokenUsageStore** (`src/Microsoft/Client/TokenUsageStore.cs`)
   - Thread-safe in-memory store for recent usage records
   - Maintains up to 100 most recent records
   - Provides access to latest and recent usage data

3. **TokenUsageController** (`src/Microsoft/TokenUsageController.cs`)
   - REST API endpoints for retrieving usage data
   - `GET /api/token-usage/latest` - Returns most recent usage
   - `GET /api/token-usage/recent?count=N` - Returns N most recent records

### Frontend Components

1. **TokenUsageDisplay** (`src/Web/app/components/TokenUsageDisplay.tsx`)
   - React component that polls backend every 2 seconds
   - Displays floating card with token counts
   - Auto-hides after 10 seconds of display

### Data Flow

```
LLM Request → IChatClient → TokenUsageTrackingChatClient 
    → TokenUsageStore → API Endpoint → Frontend Component → UI Display
```

## API Reference

### GET /api/token-usage/latest

Returns the most recent token usage record.

**Response:**
```json
{
  "available": true,
  "timestamp": "2026-04-18T08:30:00Z",
  "inputTokens": 150,
  "outputTokens": 75,
  "totalTokens": 225
}
```

If no usage data is available:
```json
{
  "available": false
}
```

### GET /api/token-usage/recent?count=10

Returns the N most recent usage records (default: 10, max: 100).

**Response:**
```json
[
  {
    "timestamp": "2026-04-18T08:30:00Z",
    "inputTokens": 150,
    "outputTokens": 75,
    "totalTokens": 225
  },
  {
    "timestamp": "2026-04-18T08:29:45Z",
    "inputTokens": 120,
    "outputTokens": 60,
    "totalTokens": 180
  }
]
```

## Configuration

### Backend

The feature is automatically enabled when using Azure OpenAI client. The `TokenUsageTrackingChatClient` is registered in the dependency injection container in `src/Microsoft/Client/ServiceCollectionExtensions.cs`.

### Frontend

Set the backend URL via environment variable (defaults to `http://localhost:8000`):

```bash
NEXT_PUBLIC_BACKEND_URL=http://your-backend-url
```

Or when using the Next.js API route proxy (recommended):
```bash
BACKEND_URL=http://localhost:8000
```

## Limitations

- **Streaming calls**: Currently only tracks non-streaming calls (`GetResponseAsync`). Streaming calls (`GetStreamingResponseAsync`) don't expose usage details in the current version of Microsoft.Extensions.AI.
- **Polling interval**: Uses 2-second polling interval which may not capture usage immediately
- **In-memory storage**: Usage records are lost on application restart
- **Azure OpenAI only**: Currently only works with Azure OpenAI client configuration

## Future Enhancements

1. **Per-message tracking**: Associate usage with specific messages in the UI
2. **Streaming support**: Track usage from streaming responses when API supports it
3. **Persistent storage**: Store usage history in a database
4. **Real-time updates**: Use WebSocket or SSE for immediate updates instead of polling
5. **Usage analytics**: Add graphs and trends for usage over time
6. **Cost estimation**: Display estimated cost based on model pricing
7. **Azure AI Foundry support**: Extend tracking to Foundry-based agents

## Testing

### Unit Tests

Run unit tests to verify token tracking logic:

```bash
dotnet test Agents.slnx --filter "TestCategory!=ExternalDependency&TestCategory!=Live"
```

The `TokenUsageTrackingTests` class validates:
- Usage recording
- Chronological ordering
- Queue size limits

### Manual Testing

1. Start the application:
   ```bash
   docker compose up --build
   ```

2. Navigate to `http://localhost:3000`

3. Send a message to the AI agent

4. Observe the token usage card appearing in the bottom-right corner showing:
   - ↑ Input tokens (green)
   - ↓ Output tokens (blue)
   - Total tokens

5. The card should disappear after 10 seconds

## Troubleshooting

### Token usage not displaying

1. Check backend logs for any errors in `TokenUsageTrackingChatClient`
2. Verify API endpoint is accessible: `curl http://localhost:8000/api/token-usage/latest`
3. Check browser console for network errors
4. Ensure backend URL is configured correctly in frontend

### Incorrect token counts

1. Verify Azure OpenAI is returning `UsageDetails` in responses
2. Check logs for any exceptions in token tracking code
3. Test with a simple non-streaming call

### Performance issues

1. Adjust polling interval in `TokenUsageDisplay.tsx` (currently 2 seconds)
2. Reduce `count` parameter in recent usage API calls
3. Consider implementing debouncing or throttling

## Contributing

When contributing to this feature:

1. Maintain backward compatibility with existing APIs
2. Add unit tests for new functionality
3. Update this documentation
4. Follow existing code style and patterns
