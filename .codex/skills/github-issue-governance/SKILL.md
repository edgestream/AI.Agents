---
name: github-issue-governance
description: Create or update GitHub issues for this repository with required governance metadata. Use when Codex is asked to create a GitHub issue, especially Bug, Epic, Feature, or Task issues, or when issue type, labels, project backlog membership, parent epic, blocking relationships, or assignee metadata must be set correctly.
---

# GitHub Issue Governance

Use this skill whenever creating GitHub issues in `edgestream/AI.Agents`.

## Required Metadata

Always set these fields as metadata, not only in the issue body:

- Issue type: one of `Bug`, `Epic`, `Feature`, or `Task`.
- Labels: apply component/status labels that add information beyond the issue type. Do not add labels that duplicate the issue type metadata (for example, do not add `feature` to a `Feature` issue). For feature work, prefer relevant labels such as `frontend`, `backend`, `enhancement`, `documentation`, `testing`, or `infrastructure` when they apply.
- Assignee: assign the authenticated/current GitHub user.
- Project: add every `Epic` and `Feature` issue to `edgestream/projects/1` (`AI` backlog).

For `Feature` issues, also check relationship metadata:

- Parent epic: search existing open `Epic` issues and choose the best fit when one clearly applies.
- Dependencies: search existing open issues for likely predecessors or successors and set `blocked by` / `blocking` relationships when clear.
- If a relationship is plausible but ambiguous, ask before setting it.
- If the available tools cannot set a required relationship as metadata, say so explicitly and do not represent it only as description text.

## Workflow

1. Resolve repo context as `edgestream/AI.Agents` unless the user explicitly provides another repo.
2. Identify the intended issue type. If unclear, ask the user to choose one of `Bug`, `Epic`, `Feature`, or `Task`.
3. Use a short stakeholder-facing title. Prefer concise titles like `Visualize workflows and sub-agent execution` over implementation-heavy titles that include protocol, library, or framework names.
4. Inspect existing labels and choose labels that describe scope or intent without duplicating issue type metadata. For example, a `Feature` issue may use `frontend`, `backend`, and `enhancement`, but should not use `feature`.
5. Get the authenticated GitHub login and use it as the assignee.
6. For `Feature` issues:
   - Search open `Epic` issues for a parent candidate.
   - Include known broad epics in the candidate set even when they are not labeled `epic`; for workflow, AG-UI, CopilotKit, A2UI, extensibility, or user-experience work, consider #93 (`Extensibility and User Experience`) as a likely parent.
   - Search open issues for blockers or follow-up work using the request terms, component names, and related issue numbers.
   - Set metadata relationships only when confidence is high.
7. Create the issue with the selected type, labels, assignee, and body.
8. Add `Epic` and `Feature` issues to `edgestream/projects/1`.
9. Verify the created issue metadata after creation: type, labels, assignee, project membership, and relationships.

## Tool Guidance

Prefer the GitHub connector for issue reads, searches, labels, assignees, comments, and simple issue creation.

Use `gh` or GitHub GraphQL when the connector does not expose required metadata:

- issue type
- ProjectV2 membership
- parent/sub-issue relationships
- blocked by/blocking relationships

Do not silently downgrade missing metadata into prose. If GitHub API support or permissions are missing, report the exact field that could not be set and the command/API operation that failed.

## Issue Body Pattern

Keep bodies useful, but do not rely on them for required metadata.

Recommended sections:

- `## Summary`
- `## Motivation`
- `## Scope`
- `## Acceptance Criteria`
- `## Notes / Risks`

For feature issues, mention related context in prose only after metadata is set. Example: "This builds on the implementation from #210." The actual dependency or parent relationship must be set through GitHub metadata when applicable.
