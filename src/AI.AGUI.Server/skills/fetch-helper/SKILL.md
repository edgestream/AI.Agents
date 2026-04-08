---
name: fetch-helper
description: Fetch the content of a URL and summarize or extract information from it. Use when the user asks to retrieve, read, browse, or look up a web page or online resource.
license: Apache-2.0
compatibility: Requires outbound network access
---

## fetch-helper

Use this skill when the user wants to retrieve information from a URL or asks you to browse a web page.

### Steps

1. Identify the URL the user wants to fetch. If no URL is given, ask for one.
2. Call the `fetch` tool with the URL to retrieve the page content.
3. Read the returned content and extract the relevant information requested by the user.
4. Present the result in a clear, concise format. Quote or paraphrase the source as appropriate.

### Edge cases

- If the URL returns an error or empty body, inform the user and suggest checking the URL.
- If the content is very long, summarize the most relevant sections.
- Do not follow redirects to unrelated domains without confirming with the user.

### Example

User: "What does the README at https://example.com/README.md say?"

1. Call `fetch` with `https://example.com/README.md`.
2. Summarize the key sections of the returned content.
3. Answer the user's question directly.
