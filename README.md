# AI.Web

AI.Web is a conversational AI interface built with a .NET backend ([AG-UI](https://docs.ag-ui.com) server) and a Next.js frontend ([CopilotKit](https://www.copilotkit.ai/) chat UI).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (see `global.json`)
- [Node.js 20](https://nodejs.org/)
- [Docker](https://www.docker.com/) and [Docker Compose](https://docs.docker.com/compose/) (for containerised development)

## Project structure

| Path | Description |
|------|-------------|
| `src/AI.Web.AGUIServer` | .NET backend – hosts the AG-UI agent endpoint |
| `src/AI.Web.AGUIChat` | Next.js frontend – CopilotKit chat UI |
| `tests/` | Integration and unit tests |
| `docker-compose.yml` | Compose file for local development |

## Running with Docker Compose

Docker Compose orchestrates both the backend and frontend containers with
health checks and internal networking so the frontend can reach the backend
automatically.

### 1. Set environment variables

The backend requires Azure OpenAI credentials. Create a `.env` file in the
project root (this file is git-ignored):

```bash
AZURE_OPENAI_ENDPOINT=https://<your-resource>.openai.azure.com/
AZURE_OPENAI_DEPLOYMENT_NAME=<your-deployment>
```

### 2. Start the services

```bash
docker compose up --build
```

This builds and starts both containers:

| Service | URL | Health check |
|---------|-----|--------------|
| **backend** | http://localhost:8080 | `GET /health` |
| **frontend** | http://localhost:3000 | `GET /` |

The frontend waits for the backend health check to pass before starting.

### 3. Stop the services

```bash
docker compose down
```

### Image and tagging conventions

Local builds tag images as `ai-web-aguiserver:latest` and
`ai-web-aguichat:latest`. The CI pipeline publishes to GitHub Container
Registry (`ghcr.io`) using branch, PR, SHA, and `latest` tags. See
`.github/workflows/ci.yml` for details.

## Running locally (without Docker)

### Backend

```bash
cd src/AI.Web.AGUIServer
dotnet run
```

### Frontend

```bash
cd src/AI.Web.AGUIChat
npm ci
npm run dev
```

## Running tests

```bash
dotnet test
```
