# Feature: End-to-end A2UI context propagation and backend render planning

## Summary

Enable the frontend to publish CopilotKit A2UI context entries and update the backend agent contract so model responses can prefer structured A2UI rendering over plain text whenever a matching render path exists.

Today the web runtime enables CopilotKit A2UI middleware, but the app does not register an A2UI catalog on the frontend, so the backend does not receive the CopilotKit-generated context entries that describe:

- available A2UI components
- A2UI generation guidelines
- A2UI design guidelines

As a result, the backend can emit hard-coded A2UI operations when a specific tool is invoked, but it cannot reliably generate A2UI from frontend-provided rendering instructions.

## Problem

We want the frontend context shown in CopilotKit's inspector or context panel to reach the backend as rendering instructions, and we want the backend to use that context to generate real A2UI responses whenever possible.

The current gap is twofold:

1. The frontend turns on CopilotKit runtime A2UI support, but does not register a catalog with `<CopilotKit>`, so the CopilotKit provider has no catalog schema or default A2UI guidance to inject into request context.
2. The backend has explicit A2UI-producing tools, but no general mechanism for:
   - reading CopilotKit context entries
   - detecting available A2UI catalogs and render guidance
   - steering the model toward `render_a2ui` or equivalent structured render functions
   - preferring fixed-schema renderers when a domain renderer already exists

## Desired outcome

When the frontend declares a catalog and CopilotKit context is present, the backend should:

- see the A2UI schema and guidelines as part of the agent request context
- understand which components and props are allowed
- prefer domain-specific render functions first when they can satisfy the user request
- fall back to a general `render_a2ui` generation path when no fixed renderer applies
- return A2UI operations in a format the frontend can render without additional text parsing

## Proposed changes

### 1. Register an A2UI catalog in the web app

Update the frontend so `<CopilotKit>` is given an A2UI catalog instead of only `runtimeUrl` and `agent`.

Scope:

- define a catalog of supported components and renderers in the web app
- include the basic catalog plus any app-specific components
- pass the catalog through the `a2ui` prop on `<CopilotKit>`

Expected effect:

- CopilotKit will inject context entries equivalent to:
  - `A2UI Component Schema — available components for generating UI surfaces...`
  - `A2UI generation guidelines — protocol rules...`
  - `A2UI design guidelines — visual design rules...`
- these entries become available to the backend runtime in request context

### 2. Make the backend treat CopilotKit context as first-class input

Introduce a backend abstraction that extracts structured request context relevant to rendering.

Scope:

- read agent request context entries coming from CopilotKit
- detect A2UI schema and guidance entries by description
- normalize them into a backend model such as:
  - available catalog IDs
  - component schema JSON
  - generation guidelines
  - design guidelines
  - optional frontend tool definitions

Expected effect:

- the model layer can reason over rendering capabilities without relying on brittle prompt text assembly spread across handlers

### 3. Add an explicit render-planning step in the backend

Add a rendering planner that decides between:

- fixed-schema/domain render function
- dynamic A2UI generation function
- plain text response

Decision policy:

- use fixed-schema/domain renderers first for known shapes such as cards, search results, dashboards, forms, or workflow steps
- use dynamic `render_a2ui` generation when the request benefits from structured UI and the frontend catalog supports it
- fall back to text only when no suitable rendering path exists

Expected effect:

- render functions stop being ad hoc tools and become a deliberate capability selection mechanism

### 4. Add a general backend `render_a2ui` pathway

Add a generic backend tool or planner-owned function that emits A2UI operations using the frontend-provided schema and guidelines.

Scope:

- define a canonical render function contract, for example:
  - `surfaceId`
  - `catalogId`
  - `components`
  - optional `data`
- use a second LLM call or structured generation pass when needed
- force or strongly bias tool choice toward `render_a2ui` when the planner selects dynamic A2UI
- convert the result into `a2ui_operations` for the AG-UI stream

Expected effect:

- the backend can perform true protocol-aware A2UI generation instead of only returning pre-authored component payloads

### 5. Preserve and prioritize existing backend render functions

Existing domain renderers should not be replaced by generic A2UI generation.

Scope:

- keep functions like recipe or test-card renderers as first-class renderers
- document these as preferred render paths for recognized intents
- allow action-capable surfaces to continue using explicit backend action handlers

Expected effect:

- high-quality, known UIs remain deterministic
- dynamic A2UI is used as a fallback or expansion path, not as a downgrade

### 6. Add observability for render decisions

Expose why the backend chose text, fixed renderer, or dynamic A2UI.

Scope:

- log whether CopilotKit A2UI context was present
- log selected render strategy
- log catalog ID used for dynamic A2UI
- optionally emit AG-UI activity messages for debugging in development

Expected effect:

- failures become diagnosable without guessing whether the issue is frontend registration, transport, planning, or rendering

### 7. Add tests for the contract

Add tests that verify the end-to-end request contract instead of only individual render payloads.

Scope:

- frontend/runtime integration test that confirms A2UI context is present when catalog registration is enabled
- backend unit tests for context extraction and render-strategy selection
- backend tests that confirm dynamic A2UI generation reads CopilotKit context
- end-to-end test that verifies a prompt requiring structured UI produces rendered A2UI instead of plain text when the catalog is available

## Suggested implementation notes

- Keep the rendering decision outside individual tool handlers. Otherwise every agent/tool will reinvent the same prompt logic.
- Treat CopilotKit context entries as capability input, not as a passive blob of text.
- Prefer one canonical backend renderer contract for dynamic A2UI generation so multiple agents can share it.
- Preserve deterministic domain renderers for cases where the UI shape is known ahead of time.
- Avoid relying on the frontend inspector alone as proof of transport. The backend should log or expose the received context explicitly.

## Acceptance criteria

- The web app registers an A2UI catalog with CopilotKit.
- CopilotKit-generated A2UI context entries are present in backend request context.
- The backend can parse those entries into a structured rendering-capabilities model.
- The backend selects between fixed renderer, dynamic A2UI, and text response using a documented policy.
- When a request is suitable for structured UI and a compatible catalog is present, the backend emits A2UI operations rather than plain text whenever possible.
- Existing explicit render tools continue to work.
- Automated tests cover context propagation, strategy selection, and rendered output.

## References

- CopilotKit A2UI catalog context injection via `A2UICatalogContext`
- CopilotKit shared A2UI prompts (`A2UI_DEFAULT_GENERATION_GUIDELINES`, `A2UI_DEFAULT_DESIGN_GUIDELINES`)
- CopilotKit backend AG-UI proxy/runtime flow
- CopilotKit showcase examples for fixed-schema and dynamic-schema A2UI generation
