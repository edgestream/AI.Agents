/**
 * Minimal AG-UI stub backend for E2E testing.
 *
 * Implements the two HTTP endpoints required by the real AGUIServer:
 *   GET  /health  – health check
 *   POST /        – AG-UI run endpoint; returns a pre-canned SSE response
 *
 * The SSE response follows the AG-UI event protocol so that the CopilotKit
 * HttpAgent in the frontend accepts it without modification.
 */

import { createServer } from "node:http";
import { randomUUID } from "node:crypto";

const PORT = process.env.PORT ?? 8080;

function writeEvent(res, data) {
  res.write(`data: ${JSON.stringify(data)}\n\n`);
}

createServer((req, res) => {
  if (req.method === "GET" && req.url === "/health") {
    res.writeHead(200, { "Content-Type": "text/plain" });
    res.end("OK");
    return;
  }

  if (req.method === "POST" && req.url === "/") {
    res.writeHead(200, {
      "Content-Type": "text/event-stream",
      "Cache-Control": "no-cache",
      Connection: "keep-alive",
      "Access-Control-Allow-Origin": "*",
    });

    const threadId = randomUUID();
    const runId = randomUUID();
    const messageId = randomUUID();

    writeEvent(res, { type: "RUN_STARTED", threadId, runId });
    writeEvent(res, { type: "TEXT_MESSAGE_START", messageId, role: "assistant" });
    writeEvent(res, { type: "TEXT_MESSAGE_CONTENT", messageId, delta: "Hello " });
    writeEvent(res, { type: "TEXT_MESSAGE_CONTENT", messageId, delta: "from stub!" });
    writeEvent(res, { type: "TEXT_MESSAGE_END", messageId });
    writeEvent(res, { type: "RUN_FINISHED", threadId, runId });

    res.end();
    return;
  }

  // Handle CORS pre-flight
  if (req.method === "OPTIONS") {
    res.writeHead(204, {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "POST, GET, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
    });
    res.end();
    return;
  }

  res.writeHead(404, { "Content-Type": "text/plain" });
  res.end("Not found");
}).listen(PORT, () => {
  console.log(`AG-UI stub backend listening on port ${PORT}`);
});
