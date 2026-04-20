# PollMe

A portfolio-quality real-time polling app. Create single- or multiple-choice polls, share vote links, and watch live results update via SignalR — no account required.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Browser                             │
│  React 19 + TypeScript + Vite + Pico CSS                │
│                                                         │
│  / (CreatePoll)  /poll/:id (Vote)  /poll/:id/results    │
│  /dashboard/:token (Dashboard)                          │
└───────────────┬────────────────────────────────┬────────┘
                │  HTTP (REST)  /api/**           │ SignalR
                │  proxied by Vite dev server     │ /hubs/poll
                ▼                                 ▼
┌──────────────────────────────────────────────────────────┐
│              ASP.NET Core 9 Web API                      │
│                                                          │
│  PollsController   CreatorController                     │
│  CreatorAuthService (cookie + secret-token magic link)   │
│  VoterToken (cookie-based de-dup)                        │
│  PollHub (SignalR — broadcasts ResultsUpdated)           │
│                                                          │
│  Dapper + SQLite (polls.db)    FluentMigrator (schema)   │
│  OpenTelemetry → console traces & metrics (local dev)    │
└──────────────────────────────────────────────────────────┘
```

## Tech Stack

| Layer     | Technology                                        |
|-----------|---------------------------------------------------|
| Frontend  | React 19, TypeScript, Vite, React Router 7, Pico CSS (CDN) |
| Backend   | ASP.NET Core 9 Web API, Dapper, FluentMigrator, SQLite |
| Real-time | SignalR (ASP.NET Core + `@microsoft/signalr`)     |
| Observability | OpenTelemetry (console exporter, local dev)   |
| Tests     | xUnit + NSubstitute + FluentAssertions (backend), Jest + React Testing Library (frontend) |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (`dotnet --version` → `9.x.x`)
- [Node.js 20+](https://nodejs.org/) (`node --version` → `v20.x` or higher)
- npm (included with Node.js)

## Setup

```bash
# 1. Clone
git clone https://github.com/UltraPulpo/PollMe2.0.git
cd PollMe2.0

# 2. Restore backend dependencies
cd backend/PollApp.Api
dotnet restore
cd ../..

# 3. Install frontend dependencies
cd frontend
npm install
cd ..
```

## Running

### Option 1 — One command (Windows)

```bat
start.cmd
```

Opens two terminal windows: one for the backend, one for the frontend dev server.

### Option 2 — Manual (two terminals)

**Terminal 1 — Backend:**

```bash
cd backend/PollApp.Api
dotnet run
# Runs on http://localhost:5006
# SQLite database created automatically at backend/PollApp.Api/polls.db
```

**Terminal 2 — Frontend:**

```bash
cd frontend
npm run dev
# Runs on http://localhost:5173
# API requests proxied to http://localhost:5006
```

Open **http://localhost:5173** in your browser.

## Running Tests

**Backend:**

```bash
cd backend/PollApp.Api.Tests
dotnet test
```

**Frontend:**

```bash
cd frontend
npm test
```

## API Endpoints

All endpoints return `application/json`. Errors return [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457).

| Method   | Path                                    | Description                                      |
|----------|-----------------------------------------|--------------------------------------------------|
| `POST`   | `/api/polls`                            | Create a poll. Returns poll ID + secret token.   |
| `GET`    | `/api/polls/{pollId}`                   | Get poll details and options.                    |
| `POST`   | `/api/polls/{pollId}/vote`              | Submit a vote (cookie-based de-dup → 409 if dupe). |
| `GET`    | `/api/polls/{pollId}/results`           | Get aggregated vote counts and percentages.      |
| `PATCH`  | `/api/polls/{pollId}`                   | Toggle poll active/closed (creator auth required). |
| `DELETE` | `/api/polls/{pollId}`                   | Delete a poll (creator auth required).           |
| `GET`    | `/api/creator/{secretToken}/polls`      | List all polls for a creator (magic link).       |

### Creator auth

When you create a poll, the API sets a `creatorId` **cookie** and returns a `secretToken` in the response body. The secret token is the URL for the dashboard (`/dashboard/{secretToken}`). Creator-only endpoints (`PATCH`, `DELETE`) accept either the cookie or the `X-Creator-Token` header.

### SignalR

Connect to `/hubs/poll` and join a group with `JoinPoll(pollId)`. The server broadcasts `ResultsUpdated(PollResultsResponse)` after every vote.

