# PollApp

A portfolio-quality polling web app where creators make single- or multiple-choice polls, share vote links, and track live results via SignalR.

## Tech Stack

- **Backend**: ASP.NET Core Web API, Dapper, FluentMigrator, SQLite, SignalR, OpenTelemetry
- **Frontend**: React + TypeScript, Vite, Pico CSS
- **Tests**: xUnit + NSubstitute + FluentAssertions (backend), Jest + React Testing Library (frontend)

## Prerequisites

- .NET SDK 8.0+
- Node.js 18+
- npm

## Quick Start

### Option 1: One command (Windows)

```batch
start.cmd
```

This launches both the backend and frontend in separate windows.

### Option 2: Manual

**Backend** (terminal 1):

```powershell
cd backend/PollApp.Api
dotnet run
```

Backend runs on `https://localhost:5001`.

**Frontend** (terminal 2):

```powershell
cd frontend
npm run dev
```

Frontend runs on `http://localhost:5173` with API requests proxied to the backend.

## Running Tests

**Backend tests**:

```powershell
cd backend/PollApp.Api.Tests
dotnet test
```

**Frontend tests**:

```powershell
cd frontend
npm test
```
