# Repository Guidelines

## Project Structure & Module Organization
`src/` contains the main application code. `src/Server/` is the ASP.NET Core backend, `src/Web/` is the Next.js frontend, and the remaining folders under `src/` hold shared .NET libraries such as `Client`, `OpenAI`, `Microsoft`, `MCP`, `OAuth`, and `AGUI`. `tests/` mirrors the runtime code with `Server.Tests/` for backend tests, `E2ETests/` for browser flows, and `tests/samples/MealPlanner/` for sample-specific coverage. Deployment assets live in `deploy/k8s/`, infra definitions in `infra/`, automation scripts in `scripts/`, and runnable examples in `samples/`.

## Build, Test, and Development Commands
Use `docker compose up --build` to start the full stack with the repository-root `appsettings.Development.json`. For native development, run `dotnet run --project src/Server` for the backend and `npm run dev` from `src/Web/` for the frontend. Build the web app with `npm run build` and lint it with `npm run lint`. Run default .NET tests with `dotnet test --filter "TestCategory!=ExternalDependency&TestCategory!=Live"`. Run browser tests with `dotnet test tests/E2ETests` after the stack is already running.

## Coding Style & Naming Conventions
Follow standard C# conventions: 4-space indentation, PascalCase for public types and members, camelCase for locals and parameters, and one type per file named after the type. The .NET projects use nullable reference types and implicit usings, so keep nullability annotations accurate. In `src/Web/`, prefer TypeScript, keep React components in PascalCase files such as `UserAvatar.tsx`, and use ESLint (`npm run lint`) before opening a PR.

## Testing Guidelines
The repository uses MSTest for unit, integration, and Playwright-based end-to-end tests. Name test files with a `*Tests.cs` suffix and keep fixtures close to the tests that use them. Reserve `Live` and `ExternalDependency` categories for tests that need real services or network access. Install Playwright browsers once with `tests/E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium`.

## Commit & Pull Request Guidelines
Recent history favors short, imperative commit subjects such as `Add HTTP fetch tool (#221)` and `Refactor handoff orchestration (#218)`. Keep commits focused and reference the issue or PR number when applicable. Pull requests should describe the user-visible change, list validation steps, link the relevant issue, and include screenshots for frontend updates. Call out configuration changes explicitly when they affect `appsettings` or deployment manifests.
