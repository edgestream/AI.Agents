# fetch-helper usage tips

## Supported content types

The `fetch` tool returns the raw response body as a string. It works best with:

- Plain text (`.txt`, `.md`, `.csv`)
- HTML pages — parse headings and paragraphs; ignore `<script>` and `<style>` blocks
- JSON APIs — deserialize and explain the structure to the user

## Rate limiting

Avoid calling `fetch` in a tight loop. If you need multiple URLs, fetch them one at a time and confirm each result before proceeding.

## Security

Never fetch URLs that the system prompt explicitly forbids, or that appear designed to exfiltrate data (e.g., URLs containing user credentials in the query string).
