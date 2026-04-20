# PollApp — Phased Implementation Plan

## Overview

A portfolio-quality polling web app where creators make single- or multiple-choice polls, share vote links, and track live results. Designed for **readability** and **explainability** — every implementation choice should be something you can walk an interviewer through.

### Tech Stack

| Layer | Technology | Why |
|---|---|---|
| Backend framework | ASP.NET Core Web API | Industry standard .NET backend |
| Data access | **Dapper** (micro-ORM) | Explicit SQL, portfolio differentiation vs EF Core |
| Migrations | **FluentMigrator** | Fluent C# API for schema changes, pairs with Dapper |
| Database | **SQLite** | Zero-config local dev, file-based |
| Real-time | **SignalR** | First-party ASP.NET Core WebSocket support, learning goal |
| Observability | **OpenTelemetry** | Industry-standard tracing/metrics, interview talking point |
| Frontend framework | **React** + **TypeScript** | Modern SPA stack |
| Frontend tooling | **Vite** | Fast dev server, industry standard for React+TS |
| Frontend tests | **Jest** + React Testing Library | Standard React testing stack |
| Backend tests | **xUnit** + **NSubstitute** + **FluentAssertions** | Modern .NET test stack |
| CSS | **Pico CSS** | Classless — semantic HTML looks good automatically |

### Monorepo Structure (Target)

```
PollMe2.0/
├── PLAN.md                  ← this file
├── handoff/                 ← phase handoff notes
│   ├── HANDOFF_TEMPLATE.md
│   ├── phase1-handoff.md
│   ├── phase2-handoff.md
│   └── ...
├── start.cmd                ← launches backend + frontend
├── README.md                ← setup instructions
├── backend/
│   └── PollApp.Api/
│       ├── Program.cs
│       ├── appsettings.Development.json
│       ├── Entities/
│       ├── Repositories/
│       ├── Services/
│       ├── Controllers/
│       ├── DTOs/
│       ├── Hubs/
│       ├── Filters/
│       ├── Migrations/
│       └── Telemetry/
├── backend/
│   └── PollApp.Api.Tests/
│       ├── Unit/
│       └── Integration/
└── frontend/
    ├── index.html
    ├── vite.config.ts
    ├── jest.config.ts
    ├── package.json
    └── src/
        ├── main.tsx
        ├── App.tsx
        ├── api.ts
        ├── hooks/
        ├── pages/
        ├── components/
        └── __tests__/
```

---

## Handoff Protocol

Each phase produces a **handoff note** in the `handoff/` folder. This allows different agents (or the same agent in a new session) to pick up work without losing context.

### At the END of a phase:

1. Read `handoff/HANDOFF_TEMPLATE.md`
2. Copy its structure into a new file: `handoff/phaseN-handoff.md`
3. Fill in every section honestly — especially "Known Issues" and "Deviations from Plan"
4. Verify the "Next Phase Entry Point" section is actionable

### At the START of a phase:

1. Read this file (`PLAN.md`) — specifically the section for the phase you are about to implement
2. Read `handoff/phase(N-1)-handoff.md` from the previous phase (if it exists)
3. Verify the prerequisites listed in the handoff are met before starting
4. If the handoff lists deviations from the plan, adjust your approach accordingly

### If no handoff note exists for the previous phase:

Explore the project structure and verify the state of the codebase yourself before starting work. Compare what exists against what the plan says should exist for all preceding phases.

---

## Phase 1: Project Scaffolding

**Goal**: Set up the monorepo structure, create backend and frontend projects, install all dependencies, configure Vite's dev proxy, and verify everything runs.

**Prerequisites**: .NET SDK (8.0+), Node.js (18+), npm installed.

### Steps

#### 1.1 — Create monorepo root

Create the `PollMe2.0/` root (if not already present) with the following top-level files:
- `README.md` — brief project description and setup instructions (shell commands to run both projects)
- `start.cmd` — Windows batch script that launches both backend and frontend:
  ```batch
  @echo off
  start "Backend" cmd /c "cd backend\PollApp.Api && dotnet run"
  start "Frontend" cmd /c "cd frontend && npm run dev"
  ```
- `handoff/` folder

#### 1.2 — Scaffold backend

```powershell
mkdir backend
cd backend
dotnet new webapi -n PollApp.Api --no-https false
cd PollApp.Api
```

Add NuGet packages:
```powershell
dotnet add package Dapper
dotnet add package Microsoft.Data.Sqlite
dotnet add package FluentMigrator
dotnet add package FluentMigrator.Runner
dotnet add package FluentMigrator.Runner.SQLite
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.Console
```

Verify: `dotnet build` succeeds.

Clean up the generated `WeatherForecast` example controller — remove it. Set up a minimal `Program.cs` that runs and returns 200 on `/`.

#### 1.3 — Scaffold frontend

```powershell
cd ../../
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

Add runtime packages:
```powershell
npm install react-router-dom @microsoft/signalr
```

Add dev/test packages:
```powershell
npm install -D jest ts-jest @types/jest @testing-library/react @testing-library/jest-dom @testing-library/user-event jest-environment-jsdom identity-obj-proxy ts-node
```

> **Note**: `ts-node` is required for Jest to load `.ts` config files.

#### 1.4 — Configure Vite proxy

**What Vite does**: Vite is two things — a **dev server** and a **production bundler**.

- **Dev server** (`npm run dev`): Serves files on `http://localhost:5173` with instant hot module replacement (HMR). When you save a `.tsx` file, the browser updates in <100ms without a full page reload. It does this by serving ES modules directly to the browser — no bundling during development.
- **Production build** (`npm run build`): Uses Rollup under the hood to bundle everything into optimized static files in `dist/`.
- **Config file**: `vite.config.ts` at the frontend root.

**Key Vite files**:
- `index.html` — the entry point (Vite uses this as the root, unlike CRA which hides it)
- `src/main.tsx` — React entry point, referenced by a `<script>` tag in `index.html`
- `vite.config.ts` — build/dev configuration

**Vite commands**:
- `npm run dev` — start dev server
- `npm run build` — production build
- `npm run preview` — preview the production build locally

Edit `vite.config.ts` to proxy API and SignalR requests to the backend:
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Vite dev server configuration.
// The 'proxy' setting tells Vite: "any request starting with /api or /hubs,
// forward it to the ASP.NET Core backend instead of trying to serve it as a file."
// This means the frontend can use relative URLs like '/api/polls' and they'll
// reach the backend automatically during development.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5006',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:5006',
        ws: true             // enable WebSocket proxying — required for SignalR
      }
    }
  }
})
```

#### 1.5 — Configure CORS in backend

In `Program.cs`, add CORS for the Vite dev server origin. With the proxy configured above, CORS is technically only needed if someone hits the API directly, but it's good practice:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for cookies
    });
});
// ...
app.UseCors();
```

#### 1.6 — Configure Jest

Create `jest.config.ts` in the `frontend/` root:
```typescript
/** @jest-config-loader ts-node */
/** @jest-config-loader-options {"transpileOnly": true} */
import type { Config } from 'jest';

const config: Config = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  moduleNameMapper: {
    '\\.(css|less|scss)$': 'identity-obj-proxy',  // mock CSS imports
  },
  // Polyfills required before any test module loads (e.g. TextEncoder for React Router v7)
  setupFiles: ['<rootDir>/jest.setup.ts'],
  setupFilesAfterEnv: ['@testing-library/jest-dom'],
  transform: {
    '^.+\\.tsx?$': [
      'ts-jest',
      {
        tsconfig: {
          jsx: 'react-jsx',
          module: 'CommonJS',
          moduleResolution: 'node',
          esModuleInterop: true,
          allowSyntheticDefaultImports: true,
          strict: true,
          lib: ['es2020', 'dom', 'dom.iterable'],
          types: ['jest', '@testing-library/jest-dom', 'node'],
        },
      },
    ],
  },
};

export default config;
```

> **Note**: The `@jest-config-loader` docblocks are required because the Vite project uses `"type": "module"` in `package.json`. The correct Jest key is `setupFilesAfterEnv` (not `setupFilesAfterSetup`).

> **Note**: The `transform` block is required because the Vite `tsconfig.app.json` uses `"module": "ESNext"`, `"moduleResolution": "bundler"`, and `"verbatimModuleSyntax": true` — all incompatible with Jest/CommonJS. Providing explicit tsconfig options via the ts-jest transform overrides those settings for the test environment.

Create `jest.setup.ts` in the `frontend/` root (NOT inside `src/__tests__/`):
```typescript
// Polyfill TextEncoder/TextDecoder for React Router v7 in jsdom environment.
// These are available in Node.js but not in older jsdom versions.
import { TextEncoder, TextDecoder } from 'util';

Object.assign(global, { TextEncoder, TextDecoder });
```

> **Important**: Place `jest.setup.ts` at `frontend/jest.setup.ts`, not inside `src/__tests__/`. Jest's default `testMatch` pattern picks up all files in `__tests__/` directories, which would cause the setup file to be treated as a test file and fail.

Add to `package.json` scripts: `"test": "jest"`

#### 1.7 — Verification

- [ ] `cd backend/PollApp.Api && dotnet build` — succeeds with no errors
- [ ] `cd backend/PollApp.Api && dotnet run` — starts on `https://localhost:5001`
- [ ] `cd frontend && npm run dev` — starts on `http://localhost:5173`, shows default Vite+React page
- [ ] `cd frontend && npx jest --passWithNoTests` — Jest runs (no tests yet, exits 0)
  > **Note**: `npm test -- --passWithNoTests` does not work with npm 11.x. Use `npx jest` directly.
- [ ] `start.cmd` from repo root launches both

---

## Phase 1.a: CI Pipeline (GitHub Actions)

**Goal**: Add a minimal GitHub Actions CI workflow so that every push to `main` and every pull request is automatically built and tested.

**Prerequisites**: Phase 1 complete (backend builds, frontend builds, Jest configured).

### Steps

#### 1.a.1 — Create `.github/workflows/ci.yml`

Two parallel jobs:

1. **Build & Test .NET** — `ubuntu-latest`, .NET 9.0.x
   - `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build`
2. **Build & Test Frontend** — `ubuntu-latest`, Node 22.x, `working-directory: ./frontend`
   - `npm ci` → `npm run build` → `npx jest --passWithNoTests`

#### 1.a.2 — Verification

- [ ] YAML is valid and uses pinned action versions (`actions/checkout@v4`, etc.)
- [ ] Push to `main` or open a PR triggers both jobs
- [ ] Both jobs pass green (no tests yet — `dotnet test` finds nothing, Jest uses `--passWithNoTests`)

---

## Phase 2: Data Model & Database (Dapper + FluentMigrator)

**Goal**: Define all database tables via FluentMigrator migrations, create plain C# entity classes, implement repository interfaces and Dapper-backed implementations, and verify migrations run on startup.

**Prerequisites**: Phase 1 complete (backend project builds and runs).

### Explanation: Why Dapper Instead of EF Core

- **Dapper** is a micro-ORM — it maps SQL query results to C# objects, but you write the SQL yourself.
- Every query is visible and explicit. No LINQ-to-SQL translation, no change tracking, no lazy loading magic.
- For a portfolio project, this signals "I understand SQL and chose a lightweight tool deliberately."
- The trade-off: you write more code per query, but each query is readable and debuggable.

### Steps

#### 2.1 — Entity classes

Create plain C# POCOs in `Entities/`. No attributes, no base classes — just properties whose names match database column names (Dapper maps by convention).

> **SQLite + Dapper note**: SQLite stores GUIDs as TEXT and has no native boolean type. You must register a custom `GuidTypeHandler` (a `SqlMapper.TypeHandler<Guid>`) at startup so Dapper can map between C# `Guid` properties and SQLite TEXT columns. Call `SqlMapper.AddTypeHandler(new GuidTypeHandler())` before building the app. With this handler in place, pass `Guid` parameters directly in INSERT/UPDATE statements and let Dapper bind them as TEXT. Continue to pass `DateTime` values via `.ToString("O")` (ISO 8601) and boolean values as `1`/`0`.

```csharp
// Entities/Poll.cs
// A plain C# class. Dapper maps SQL result columns to these properties by name.
// No [Table] attributes or [Column] decorations — Dapper doesn't use them.
public class Poll
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollType PollType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

Entities to create:
- `Creator` — `Id` (Guid), `SecretToken` (Guid), `DisplayName` (string?), `CreatedAtUtc`
- `Poll` — `Id`, `CreatorId`, `Title`, `Description?`, `PollType`, `IsActive`, `CreatedAtUtc`
- `PollOption` — `Id` (Guid), `PollId`, `Text`, `DisplayOrder` (int)
- `Vote` — `Id` (Guid), `PollId`, `VoterToken` (string), `CreatedAtUtc`
- `VoteChoice` — `Id` (Guid), `VoteId`, `PollOptionId`
- `PollType` — enum: `SingleChoice = 0`, `MultipleChoice = 1`

#### 2.2 — FluentMigrator migrations

Create one migration class per table in `Migrations/`. Each migration is a C# class with `Up()` (create) and `Down()` (rollback) methods using FluentMigrator's fluent API:

```csharp
// Migrations/Migration_001_CreateCreatorsTable.cs
using FluentMigrator;

[Migration(1)]
public class Migration_001_CreateCreatorsTable : Migration
{
    public override void Up()
    {
        Create.Table("Creators")
            .WithColumn("Id").AsString(36).PrimaryKey()       // GUID stored as TEXT in SQLite
            .WithColumn("SecretToken").AsString(36).NotNullable()
            .WithColumn("DisplayName").AsString(200).Nullable()
            .WithColumn("CreatedAtUtc").AsString().NotNullable();  // ISO 8601 string

        Create.Index("IX_Creators_SecretToken")
            .OnTable("Creators")
            .OnColumn("SecretToken").Unique();
    }

    public override void Down() => Delete.Table("Creators");
}
```

Migrations to create:
- `Migration_001_CreateCreatorsTable`
- `Migration_002_CreatePollsTable` — FK to Creators
- `Migration_003_CreatePollOptionsTable` — FK to Polls
- `Migration_004_CreateVotesTable` — FK to Polls, UNIQUE INDEX on (`PollId`, `VoterToken`)
- `Migration_005_CreateVoteChoicesTable` — FK to Votes, FK to PollOptions

#### 2.3 — Register FluentMigrator and run on startup

In `Program.cs`:
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register FluentMigrator — scans this assembly for Migration classes
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(Program).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// After building the app, run all pending migrations automatically.
// Great for local dev — just start the app and the DB is always up to date.
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
```

In `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=polls.db"
  }
}
```

#### 2.4 — Repository interfaces and Dapper implementations

Create in `Repositories/`:

**`IPollRepository`** / **`PollRepository`**:
- `CreateAsync(Poll poll, List<PollOption> options)` — INSERT poll + options in a transaction
- `GetByIdAsync(Guid id)` — SELECT poll by ID
- `GetWithOptionsAsync(Guid id)` — SELECT poll + its options (two queries, or a JOIN)
- `GetByCreatorIdAsync(Guid creatorId)` — SELECT all polls for a creator
- `UpdateIsActiveAsync(Guid id, bool isActive)` — UPDATE IsActive flag
- `DeleteAsync(Guid id)` — DELETE poll + cascade (options, votes, vote choices) in a transaction

**`IVoteRepository`** / **`VoteRepository`**:
- `CreateVoteAsync(Vote vote, List<VoteChoice> choices)` — INSERT vote + choices in a transaction
- `HasVotedAsync(Guid pollId, string voterToken)` — SELECT EXISTS with the unique index
- `GetResultsAsync(Guid pollId)` — SELECT option text, vote count GROUP BY option
- `GetVoteCountAsync(Guid pollId)` — SELECT COUNT of votes for a poll (added in Phase 4 for CreatorPollSummary)

**`ICreatorRepository`** / **`CreatorRepository`**:
- `CreateAsync(Creator creator)` — INSERT creator
- `GetBySecretTokenAsync(Guid secretToken)` — SELECT by SecretToken

Each method opens its own `SqliteConnection` (Dapper opens/closes per query — no long-lived connections needed):
```csharp
public class PollRepository : IPollRepository
{
    private readonly string _connectionString;

    public PollRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<Poll?> GetByIdAsync(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);

        // Dapper extension method: runs the SQL, maps each result row
        // to a Poll object by matching column names to property names.
        return await connection.QuerySingleOrDefaultAsync<Poll>(
            "SELECT * FROM Polls WHERE Id = @Id",
            new { Id = id.ToString() });
    }
}
```

Additional files to create in `Repositories/`:
- **`GuidTypeHandler`** — `SqlMapper.TypeHandler<Guid>` that converts between C# `Guid` and SQLite TEXT (see note in §2.1)
- **`PollOptionResult`** — a small projection class with `PollOptionId`, `Text`, `VoteCount` for `GetResultsAsync` results (this doesn't map to a single entity, so it lives alongside `IVoteRepository`)


#### 2.5 — Register repositories in DI

In `Program.cs`:
```csharp
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<ICreatorRepository, CreatorRepository>();
```

#### 2.6 — Verification

- [ ] `dotnet build` succeeds
- [ ] `dotnet run` starts and creates `polls.db` file with all 5 tables
- [ ] Inspect `polls.db` with a SQLite browser or `sqlite3 polls.db ".tables"` — all tables present
- [ ] Unique index on `Votes(PollId, VoterToken)` exists

> **Note**: `polls.db` is created relative to the working directory (i.e., `backend/PollApp.Api/`). Ensure it is listed in `.gitignore`.
>
> **Note**: SQLite does not enforce foreign keys by default — it requires `PRAGMA foreign_keys = ON` per connection. The repository `DeleteAsync` methods handle cascade deletes manually in FK order within a transaction. A future improvement could enable FK enforcement via a connection interceptor.

---

## Phase 3: Authentication — Magic Link / Cookie

**Goal**: Implement cookie-based creator identity and voter duplicate-prevention. No passwords, no OAuth — just HTTP-only cookies and secret GUIDs.

**Prerequisites**: Phase 2 complete (entities, repositories, migrations running).

### Explanation: How Auth Works in This App

There are two types of identity:

1. **Creator identity** — "who made this poll?"
   - Creator gets a `Creator` record with a random `SecretToken` GUID
   - `SecretToken` is stored in an HTTP-only cookie (`creator_token`) and also returned in the API response
   - The creator can bookmark their dashboard URL `/dashboard/{secretToken}` (magic link)
   - On subsequent requests, the cookie identifies them automatically; the magic link works from new browsers

2. **Voter identity** — "has this person already voted on this poll?"
   - On first vote, a random GUID is assigned as `VoterToken` and stored in an HTTP-only cookie (`voter_token`)
   - The `Votes` table has a UNIQUE INDEX on `(PollId, VoterToken)` — a second vote from the same token is rejected
   - This is bypassable (clear cookies = vote again) — a known and acceptable trade-off for a casual poll app

Both cookies use `HttpOnly = true` (no JS access, prevents XSS theft) and `SameSite = Strict` (only sent on same-origin requests).

### Steps

#### 3.1 — `ICreatorAuthService` / `CreatorAuthService`

```csharp
// Services/ICreatorAuthService.cs
public interface ICreatorAuthService
{
    // Resolves the current creator from the HTTP context.
    // Checks cookie first, then route parameter — returns null if neither present.
    Task<Creator?> GetCurrentCreatorAsync(HttpContext context);

    // Creates a new creator, persists it, and sets the cookie on the response.
    Task<Creator> CreateCreatorAsync(HttpContext context, string? displayName = null);
}
```

Implementation:
1. Check `context.Request.Cookies["creator_token"]` → if present, look up creator by SecretToken
2. Check `context.Request.RouteValues["secretToken"]` → if present, look up creator
3. Return `null` if neither found

When creating a new creator:
```csharp
var creator = new Creator
{
    Id = Guid.NewGuid(),
    SecretToken = Guid.NewGuid(),  // Cryptographically random via Guid.NewGuid()
    DisplayName = displayName,
    CreatedAtUtc = DateTime.UtcNow
};

await _creatorRepository.CreateAsync(creator);

context.Response.Cookies.Append("creator_token", creator.SecretToken.ToString(), new CookieOptions
{
    HttpOnly = true,              // Not accessible to JavaScript — prevents XSS theft
    SameSite = SameSiteMode.Strict,  // Cookie only sent on same-origin requests
    Secure = false,               // false for localhost; set true in production
    MaxAge = TimeSpan.FromDays(365)
});

return creator;
```

#### 3.2 — `[CreatorRequired]` action filter

```csharp
// Filters/CreatorRequiredAttribute.cs
// An action filter that runs before the controller action.
// If no creator can be resolved from the request, it short-circuits with 401.
public class CreatorRequiredAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authService = context.HttpContext.RequestServices
            .GetRequiredService<ICreatorAuthService>();
        var creator = await authService.GetCurrentCreatorAsync(context.HttpContext);

        if (creator == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Store creator in HttpContext.Items so the controller can access it
        context.HttpContext.Items["Creator"] = creator;
        await next();
    }
}
```

#### 3.3 — Voter token cookie helper

Create a helper method (or extension) for managing the voter token cookie:
```csharp
// Get or create the voter token from the request/response cookies
public static string GetOrCreateVoterToken(HttpContext context)
{
    if (context.Request.Cookies.TryGetValue("voter_token", out var existing))
        return existing;

    var token = Guid.NewGuid().ToString();
    context.Response.Cookies.Append("voter_token", token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Secure = false,
        MaxAge = TimeSpan.FromDays(365)
    });
    return token;
}
```

#### 3.4 — Register services in DI

```csharp
builder.Services.AddScoped<ICreatorAuthService, CreatorAuthService>();
```

#### 3.5 — Verification

- [ ] `dotnet build` succeeds
- [ ] `CreatorAuthService` can be instantiated (manual test or unit test)
- [ ] Cookie options are `HttpOnly=true`, `SameSite=Strict`

---

## Phase 4: API Endpoints

**Goal**: Implement all REST API endpoints with request validation, proper HTTP status codes, and ProblemDetails error responses.

**Prerequisites**: Phase 3 complete (auth services, repositories, migrations).

### Steps

#### 4.1 — DTO classes

Create in `DTOs/`:

```csharp
// DTOs/CreatePollRequest.cs
public class CreatePollRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public PollType PollType { get; set; }

    [Required, MinLength(2), MaxLength(20)]  // At least 2 options, at most 20
    public List<string> Options { get; set; } = new();
}
```

DTOs to create:
- `CreatePollRequest` — title, description?, pollType, options[]
- `CreatePollResponse` — pollId, secretToken, voteUrl, dashboardUrl
- `PollResponse` — id, title, description, pollType, isActive, options (id + text), createdAtUtc
- `VoteRequest` — optionIds[]
- `PollResultsResponse` — pollId, title, totalVotes, options (id, text, voteCount, percentage)
- `CreatorPollSummary` — id, title, pollType, totalVotes, isActive, createdAtUtc

#### 4.2 — `PollsController`

| Method | Route | Auth | Behavior |
|--------|-------|------|----------|
| POST | `/api/polls` | Auto-creates creator | Validate request → resolve/create creator from cookie → create poll + options → return `CreatePollResponse` with secretToken and links |
| GET | `/api/polls/{pollId}` | None | Fetch poll + options → return `PollResponse`. 404 if not found |
| POST | `/api/polls/{pollId}/vote` | None | Get/create voter token cookie → check if already voted (409 if yes) → validate optionIds (single-choice: exactly 1, multi: ≥1, all must belong to poll) → save vote + choices → return 204. (SignalR broadcast added in Phase 5) |
| GET | `/api/polls/{pollId}/results` | None | Fetch aggregated results → return `PollResultsResponse` |
| PATCH | `/api/polls/{pollId}` | `[CreatorRequired]` | Toggle `IsActive` → return updated poll. Verify the creator owns this poll (403 if not) |
| DELETE | `/api/polls/{pollId}` | `[CreatorRequired]` | Delete poll + cascade → return 204. Verify ownership (403 if not) |

#### 4.3 — `CreatorController`

| Method | Route | Auth | Behavior |
|--------|-------|------|----------|
| GET | `/api/creator/{secretToken}/polls` | Magic link (secretToken in route) | Look up creator by secretToken (404 if not found) → fetch all polls with summary vote counts → return `List<CreatorPollSummary>` |

#### 4.4 — Error responses

Register ProblemDetails services and middleware in `Program.cs`:
```csharp
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
app.UseStatusCodePages();
```

Also add `JsonStringEnumConverter` so `PollType` serializes as `"SingleChoice"`/`"MultipleChoice"` strings rather than `0`/`1`:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
```

Use ASP.NET Core's built-in `ProblemDetails` for all error responses:
```csharp
// Return structured errors following RFC 7807:
return Problem(
    statusCode: 409,
    title: "Already Voted",
    detail: "You have already voted on this poll."
);
```

#### 4.5 — Verification

- [ ] `dotnet build` succeeds
- [ ] POST `/api/polls` with valid body → 201 with poll data + secretToken
- [ ] GET `/api/polls/{id}` → 200 with poll + options
- [ ] POST `/api/polls/{id}/vote` → 204; second vote from same browser → 409
- [ ] GET `/api/polls/{id}/results` → 200 with vote counts
- [ ] GET `/api/creator/{token}/polls` → 200 with poll list
- [ ] PATCH `/api/polls/{id}` without creator cookie → 401
- [ ] PATCH `/api/polls/{id}` with wrong creator → 403
- [ ] DELETE `/api/polls/{id}` → 204; subsequent GET → 404
- [ ] Invalid request bodies → 400 with ProblemDetails validation errors

---

## Phase 5: Real-time Updates via SignalR

**Goal**: Add live result updates so that when someone votes, all viewers of that poll's results page see the change immediately via WebSocket.

**Prerequisites**: Phase 4 complete (vote endpoint works, results endpoint returns data).

### Explanation: How SignalR Works

SignalR is an ASP.NET Core library that abstracts real-time communication:

1. **Transport**: SignalR tries WebSocket first (persistent bidirectional connection), falls back to Server-Sent Events or long-polling if WebSocket isn't available. You don't manage the transport — SignalR does.
2. **Hub**: A server-side class (like a controller for real-time). Clients connect to a hub endpoint. The hub can send messages to connected clients.
3. **Groups**: A way to partition connected clients. Client joins a group (e.g., group name = pollId). The server can then broadcast to everyone in that group.
4. **Flow for this app**:
   - Client opens results page → connects to SignalR hub → calls `JoinPoll(pollId)` to join the group
   - Another client submits a vote → API endpoint saves the vote → API uses `IHubContext<PollHub>` to broadcast `ResultsUpdated` to the poll's group
   - All clients in the group receive the event and re-render with new data
   - Client leaves results page → cleanup disconnects from hub (unsubscribes automatically)

### Steps

#### 5.1 — Create `PollHub`

```csharp
// Hubs/PollHub.cs
// A SignalR Hub — clients connect via WebSocket and join groups by poll ID.
// This hub only handles group management. Actual broadcasting happens from
// the controller using IHubContext<PollHub> (you don't need to be "inside"
// the hub to send messages — IHubContext lets you send from anywhere).
public class PollHub : Hub
{
    // Called by the client when it opens the results page.
    // Adds this connection to a group named by the poll ID.
    public async Task JoinPoll(string pollId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, pollId);

    // Called by the client when it leaves the results page.
    // (Also happens automatically if the client disconnects.)
    public async Task LeavePoll(string pollId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, pollId);
}
```

#### 5.2 — Register SignalR in `Program.cs`

```csharp
builder.Services.AddSignalR();
// ...
app.MapHub<PollHub>("/hubs/poll");
```

#### 5.3 — Broadcast from vote endpoint

In `PollsController`, inject `IHubContext<PollHub>`:
```csharp
// After saving the vote, build a full PollResultsResponse and broadcast it.
// This sends the same JSON shape as GET /api/polls/{id}/results, so the
// frontend SignalR handler can directly replace its results state.
var results = await _voteRepository.GetResultsAsync(pollId);
var totalVotes = results.Sum(o => o.VoteCount);
var broadcastPayload = new PollResultsResponse
{
    PollId = pollId,
    Title = poll.Title,
    TotalVotes = totalVotes,
    Options = results.Select(o => new PollOptionResultResponse
    {
        Id = o.PollOptionId,
        Text = o.Text,
        VoteCount = o.VoteCount,
        Percentage = totalVotes > 0
            ? Math.Round((double)o.VoteCount / totalVotes * 100, 1)
            : 0
    }).ToList()
};
await _hubContext.Clients.Group(pollId.ToString())
    .SendAsync("ResultsUpdated", broadcastPayload);
```

**Key concept**: `IHubContext<PollHub>` lets you send SignalR messages from *outside* the hub class. This is the standard pattern — the hub handles connections, but business logic triggers broadcasts from services/controllers.

> **Note**: The broadcast payload is a full `PollResultsResponse` DTO (not the raw `PollOptionResult` list). This ensures the SignalR event delivers the same JSON shape as the REST endpoint, so the frontend `useSignalR` hook can set state directly without transformation.

#### 5.4 — Verification

- [ ] `dotnet build` succeeds
- [ ] Backend starts without errors with SignalR hub mapped
- [ ] Can connect to `/hubs/poll` using a SignalR test client or the frontend (Phase 6)
- [ ] Submitting a vote causes `ResultsUpdated` to fire for clients in the poll's group

---

## Phase 6: Frontend — Pages & Routing

**Goal**: Implement all 4 pages of the app with React + TypeScript + Vite. Connect to the API. Connect to SignalR for live results.

**Prerequisites**: Phase 4 complete (API endpoints work). Phase 5 complete (SignalR broadcasts).

### Explanation: React Patterns Used in This App

Every component uses React hooks extensively. Here's a primer on the patterns:

```tsx
// useState — stores a value. When you call the setter, React re-renders the component.
const [title, setTitle] = useState('');

// useEffect — runs side effects (API calls, subscriptions) after render.
// The dependency array [pollId] means: "re-run this only when pollId changes."
// The return function is cleanup — runs when the component unmounts or before re-running.
useEffect(() => {
  fetchPoll(pollId).then(setPoll);
  return () => { /* cleanup: cancel requests, close connections */ };
}, [pollId]);

// useParams — from react-router-dom. Extracts URL parameters.
const { pollId } = useParams<{ pollId: string }>();

// useNavigate — programmatic navigation.
const navigate = useNavigate();
navigate(`/poll/${pollId}/results`);
```

### Steps

#### 6.1 — Routing setup

`src/App.tsx`:
```tsx
import { Routes, Route } from 'react-router-dom';
import CreatePoll from './pages/CreatePoll';
import VotePage from './pages/VotePage';
import ResultsPage from './pages/ResultsPage';
import Dashboard from './pages/Dashboard';

// React Router v7 — declarative route definitions.
// Each <Route> maps a URL pattern to a page component.
// :pollId and :secretToken are URL parameters — extracted via useParams() in the page.
function App() {
  return (
    <Routes>
      <Route path="/" element={<CreatePoll />} />
      <Route path="/poll/:pollId" element={<VotePage />} />
      <Route path="/poll/:pollId/results" element={<ResultsPage />} />
      <Route path="/dashboard/:secretToken" element={<Dashboard />} />
    </Routes>
  );
}
```

`src/main.tsx`:
```tsx
import { BrowserRouter } from 'react-router-dom';

// BrowserRouter enables client-side routing.
// Wrap the entire app so all components can use useNavigate, useParams, etc.
ReactDOM.createRoot(document.getElementById('root')!).render(
  <BrowserRouter>
    <App />
  </BrowserRouter>
);
```

#### 6.2 — TypeScript types (`src/types.ts`)

Create a `types.ts` file with TypeScript interfaces matching all backend DTOs. Use string literal union for `PollType` instead of an enum because the Vite project's `tsconfig.app.json` has `erasableSyntaxOnly: true` (enums emit JavaScript and are not "erasable"). The backend serializes `PollType` as strings via `JsonStringEnumConverter`, so string literals match directly:

```typescript
export type PollType = 'SingleChoice' | 'MultipleChoice';

export interface CreatePollRequest { ... }
export interface CreatePollResponse { ... }
export interface PollResponse { ... }
export interface VoteRequest { ... }
export interface PollResultsResponse { ... }
export interface CreatorPollSummary { ... }
```

> **Note**: `erasableSyntaxOnly: true` also forbids constructor parameter properties (e.g., `constructor(public x: number)`). Declare class properties explicitly and assign in the constructor body instead.

#### 6.3 — API client (`src/api.ts`)

A thin typed wrapper over `fetch`. Vite's proxy forwards `/api/*` to the backend, so we use relative URLs:

```typescript
// api.ts — every API function is a simple async function.
// No axios, no complex abstractions — just fetch with types.

// Note: Using explicit property instead of constructor parameter property
// because tsconfig has erasableSyntaxOnly: true
export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new ApiError(res.status, text);
  }
  return res.json();
}

export const createPoll = (data: CreatePollRequest) =>
  request<CreatePollResponse>('/api/polls', {
    method: 'POST',
    body: JSON.stringify(data),
  });

// ... similar functions for other endpoints
```

#### 6.4 — SignalR hook (`src/hooks/useSignalR.ts`)

```typescript
// Custom hook that manages a SignalR connection for a specific poll.
// Connects on mount, joins poll group, invokes callback on ResultsUpdated, cleans up on unmount.
import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

export function useSignalR(pollId: string, onResultsUpdated: (results: PollResultsResponse) => void) {
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    // Build the connection — SignalR negotiates the best transport automatically
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/poll')           // Vite proxy forwards this to the backend
      .withAutomaticReconnect()        // Auto-reconnect if connection drops
      .build();

    connectionRef.current = connection;

    // Register the event handler BEFORE starting the connection
    connection.on('ResultsUpdated', (results: PollResultsResponse) => {
      onResultsUpdated(results);
    });

    // Start connection, then join the poll's group
    connection.start()
      .then(() => connection.invoke('JoinPoll', pollId))
      .catch(err => console.error('SignalR connection error:', err));

    // Cleanup: leave group and stop connection when component unmounts
    return () => {
      connection.invoke('LeavePoll', pollId)
        .catch(() => {})  // ignore errors during cleanup
        .finally(() => connection.stop());
    };
  }, [pollId]);  // Re-run if pollId changes
}
```

#### 6.5 — Create Poll page (`src/pages/CreatePoll.tsx`)

- Controlled form with `useState` for every field
- Dynamic options list: `useState<string[]>(['', ''])` (start with 2 empty options)
- Add Option button appends to the array; Remove button removes by index
- Poll type toggle: radio buttons for SingleChoice / MultipleChoice
- On submit: call `createPoll()`, on success show shareable links with copy-to-clipboard
- Error display for validation failures

#### 6.6 — Vote page (`src/pages/VotePage.tsx`)

- `useParams()` to get `pollId` from URL
- `useEffect` to fetch poll on mount
- Conditional rendering: radio buttons for SingleChoice, checkboxes for MultipleChoice
- `useState<string[]>` for selected option IDs
- Submit → `submitVote()` → `useNavigate` to results page
- Handle 409 (already voted): show message + link to results
- Handle 404: show "poll not found"
- Loading state while fetching

#### 6.7 — Results page (`src/pages/ResultsPage.tsx`)

- `useEffect` #1: fetch initial results
- `useSignalR(pollId, setResults)` for live updates
- CSS-only bar chart: each option is a `<div>` with `width` set to percentage, background color
- Display: option text, vote count, percentage, total votes
- Shareable vote link with copy button

#### 6.8 — Creator Dashboard (`src/pages/Dashboard.tsx`)

- `useParams()` to get `secretToken`
- Fetch polls via `getCreatorPolls(secretToken)`
- List each poll: title, type badge, vote count, active/inactive status
- Action buttons per poll: view results, copy vote link, toggle active, delete (with confirmation)
- "Create New Poll" link to `/`

#### 6.9 — Shared components

- `CopyLinkButton` — takes a URL, copies to clipboard, shows "Copied!" feedback
- `ResultsBar` — single row of the results chart (option text + bar + count)
- `OptionsList` — dynamic add/remove text inputs for poll options

#### 6.10 — Pico CSS

Add Pico CSS for clean default styling. Install via npm or add CDN link in `index.html`:
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@picocss/pico@2/css/pico.min.css">
```
Pico is a classless framework — semantic HTML (`<article>`, `<button>`, `<input>`, `<table>`) looks good with zero CSS classes.

#### 6.11 — Verification

- [ ] `npm run dev` starts without errors
- [ ] `npm run build` completes with no TypeScript errors
- [ ] Navigate to `/` → create poll form renders
- [ ] Create a poll → displays shareable vote link + dashboard link
- [ ] Navigate to `/poll/{id}` → vote form renders with correct options
- [ ] Vote → redirected to results page → results display
- [ ] Open results in two tabs → vote in a third → both tabs update live
- [ ] Navigate to `/dashboard/{token}` → lists the created poll
- [ ] Toggle poll active/inactive from dashboard
- [ ] Delete poll from dashboard → removed from list
- [ ] Copy link buttons work (clipboard API)
- [ ] Already voted → shows message on vote page

---

## Phase 7: Polish & Dev Experience

**Goal**: Error handling, styling, startup experience, and README documentation.

**Prerequisites**: Phase 6 complete (all pages functional).

### Steps

#### 7.1 — Backend error handling

Ensure all API errors return `ProblemDetails`:
```csharp
// In Program.cs — returns ProblemDetails for unhandled exceptions
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext context) =>
    Results.Problem(statusCode: 500, title: "Internal Server Error"));
```

> **Note on 404 handling**: `return NotFound()` in controller actions produces an empty 404 body. The `AddProblemDetails()` + `UseStatusCodePages()` pipeline registered in Phase 4 automatically detects empty error responses and rewrites them with a ProblemDetails body via `IProblemDetailsService`. **Do not replace `return NotFound()` with `return Problem(statusCode: 404)` in controller actions** — the middleware handles it, and changing working controller code is unnecessary.

> **Note on Phase 4 baseline**: Phase 4 already registered `builder.Services.AddProblemDetails()`, `app.UseExceptionHandler()` (no-arg form), and `app.UseStatusCodePages()`. Phase 7 upgrades the exception handler to the explicit `/error` route form. The `UseStatusCodePages()` call remains and is responsible for the 404/empty-body → ProblemDetails rewrite.

#### 7.2 — Frontend error handling

Add a simple error context/banner:
- React context that stores the latest error message
- Global error banner component that renders at the top of the page
- Pages use the error context (`setGlobalError`) for non-specific (unexpected) failures; this replaces any `alert()` calls in action handlers (e.g., Dashboard's `handleToggle`/`handleDelete`)
- Individual pages handle specific codes (409, 404, 403) with inline messages using local state

#### 7.3 — Readme

Root `README.md` with:
- Project description
- Architecture diagram (text-based)
- Prerequisites (SDK/Node versions)
- Setup instructions (`git clone`, `dotnet restore`, `npm install`)
- How to run (`start.cmd` or manual steps)
- API endpoint summary

#### 7.4 — Verification

- [ ] API returns ProblemDetails for 400, 401, 403, 404, 409, 500
- [ ] Frontend shows appropriate error messages
- [ ] `start.cmd` (created in Phase 1) launches both backend and frontend — verify it still works; no changes needed in Phase 7
- [ ] README instructions work from a clean clone

---

## Phase 8: OpenTelemetry

**Goal**: Add distributed tracing and metrics to the backend for observability. Console exporter for local dev; structured enough to add Jaeger/Zipkin later.

**Prerequisites**: Phase 4 complete (API endpoints exist to instrument).

**Can be done in parallel with Phases 5–7** — no dependencies on frontend or SignalR.

### Explanation: What OpenTelemetry Adds

OpenTelemetry gives you:
- **Traces**: Each HTTP request becomes a "trace" with one or more "spans." You can add custom spans for business logic (e.g., "CreatePoll" span inside the HTTP request span). In local dev, these print to the console.
- **Metrics**: Counters and histograms — e.g., total HTTP requests, request duration distribution.
- **Why it matters for interviews**: Shows you think about observability from the start, not as an afterthought. "I instrumented the app with OpenTelemetry so I could trace request latency and count votes per poll."

### Steps

#### 8.1 — `DiagnosticsConfig`

```csharp
// Telemetry/DiagnosticsConfig.cs
using System.Diagnostics;
using System.Diagnostics.Metrics;

// Central place for all custom telemetry sources.
// ActivitySource = OpenTelemetry "Tracer" (creates spans)
// Meter = OpenTelemetry "Meter" (creates metrics like counters and histograms)
public static class DiagnosticsConfig
{
    public const string ServiceName = "PollApp";

    // ActivitySource lets us create custom spans (called "Activities" in .NET).
    public static readonly ActivitySource Source = new(ServiceName);

    // Meter lets us create custom metrics (counters, histograms).
    public static readonly Meter Meter = new(ServiceName);

    // Example custom counter: how many votes have been cast total?
    public static readonly Counter<long> VoteCounter = Meter.CreateCounter<long>("pollapp.votes.count");
}
```

#### 8.2 — Configure OpenTelemetry in `Program.cs`

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DiagnosticsConfig.ServiceName)  // listen to our custom ActivitySource
        .AddAspNetCoreInstrumentation()             // auto-trace every HTTP request
        .AddHttpClientInstrumentation()             // auto-trace outgoing HTTP calls (if any)
        .AddConsoleExporter())                      // print traces to console for local dev
    .WithMetrics(metrics => metrics
        .AddMeter(DiagnosticsConfig.ServiceName)    // listen to our custom Meter
        .AddAspNetCoreInstrumentation()             // auto-collect HTTP metrics
        .AddConsoleExporter());                     // print metrics to console
```

#### 8.3 — Add custom spans to key operations

```csharp
// In PollsController.CreatePoll:
using var activity = DiagnosticsConfig.Source.StartActivity("CreatePoll");
activity?.SetTag("poll.title", request.Title);
activity?.SetTag("poll.optionCount", request.Options.Count);
activity?.SetTag("poll.type", request.PollType.ToString());

// In PollsController.Vote:
using var activity = DiagnosticsConfig.Source.StartActivity("SubmitVote");
activity?.SetTag("poll.id", pollId.ToString());
DiagnosticsConfig.VoteCounter.Add(1, new KeyValuePair<string, object?>("poll.id", pollId.ToString()));
```

#### 8.4 — Verification

- [ ] `dotnet build` succeeds
- [ ] On app startup, console shows OpenTelemetry initialization messages
- [ ] Create a poll → console shows trace with "CreatePoll" span + HTTP span
- [ ] Vote → console shows trace with "SubmitVote" span + vote counter increment
- [ ] Custom tags (poll.title, poll.id, etc.) appear in console trace output

---

## Phase 9: Testing

**Goal**: Unit tests and integration tests for backend, component and hook tests for frontend.

**Prerequisites**: All prior phases complete.

### Steps

#### 9.1 — Backend test project setup

```powershell
cd backend
dotnet new xunit -n PollApp.Api.Tests
cd PollApp.Api.Tests
dotnet add reference ../PollApp.Api/PollApp.Api.csproj
dotnet add package NSubstitute
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.15
dotnet add package Microsoft.Data.Sqlite
```

> **Note**: `Microsoft.AspNetCore.Mvc.Testing` must be pinned to `9.0.15`. The latest (10.x) requires `net10.0`; since the API targets `net9.0`, omitting the version will install 10.x and fail.

> **Note**: `Microsoft.Data.Sqlite` is required to call `SqliteConnection.ClearAllPools()` in `PollApiFactory.DisposeAsync`. Without it, `File.Delete` on the temp SQLite database file throws `IOException` because connections remain open in the connection pool after `base.DisposeAsync()` completes.

#### 9.2 — xUnit ↔ NUnit cheat sheet

Include this as a comment block at the top of the first test file:
```csharp
// ============================================================
// xUnit <-> NUnit Quick Reference (for devs coming from NUnit)
// ============================================================
// xUnit [Fact]               = NUnit [Test]              — a single test case
// xUnit [Theory]+[InlineData] = NUnit [TestCase]          — parameterized test
// xUnit constructor           = NUnit [SetUp]             — runs before EACH test
//   (xUnit creates a NEW class instance for every test — no shared state by default!)
// xUnit IDisposable.Dispose   = NUnit [TearDown]          — runs after each test
// xUnit IAsyncLifetime        = NUnit async [SetUp]/[TearDown]
// xUnit IClassFixture<T>      = NUnit [OneTimeSetUp]      — shared per test class
// xUnit ICollectionFixture<T> = shared across multiple test classes
// xUnit has NO [TestFixture]  — the test class itself is the fixture
// xUnit has NO Assert.That()  — use FluentAssertions instead: result.Should().Be(expected)
// ============================================================
```

#### 9.3 — Unit tests

Test business logic by mocking repositories with NSubstitute:

```csharp
// NSubstitute pattern:
// 1. Create a substitute (mock) for the interface
// 2. Set up return values for specific calls
// 3. Inject into the class under test
// 4. Call the method
// 5. Assert the result with FluentAssertions

var pollRepo = Substitute.For<IPollRepository>();
var voteRepo = Substitute.For<IVoteRepository>();
voteRepo.HasVotedAsync(pollId, voterToken).Returns(false);

// ... call the service/controller method ...

result.Should().BeOfType<NoContentResult>();
await voteRepo.Received(1).CreateVoteAsync(Arg.Any<Vote>(), Arg.Any<List<VoteChoice>>());
```

Unit test cases:
- **Poll creation**: title required → 400; options < 2 → 400; valid request → creates poll
- **Vote single-choice**: exactly 1 optionId → OK; 0 or 2+ → 400
- **Vote multi-choice**: ≥ 1 optionId → OK; 0 → 400
- **Double vote**: `HasVotedAsync` returns true → 409
- **Creator auth**: cookie present → returns creator; route param → returns creator; neither → null
- **Ownership check**: creator doesn't own poll → 403

#### 9.4 — Integration tests

Use `WebApplicationFactory<Program>` with a test SQLite database:

```csharp
public class PollsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PollsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Create a test HTTP client that talks to the real app pipeline
        // but with a test database
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override connection string to use a test DB
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreatePoll_ThenVote_ThenGetResults_FullCycle()
    {
        // 1. Create poll
        // 2. Vote
        // 3. Get results
        // 4. Assert vote count = 1
    }
}
```

Integration test cases:
- Full create → vote → results cycle
- Double vote → 409
- Creator dashboard returns only that creator's polls
- Delete poll → subsequent GET returns 404
- Toggle poll inactive → vote returns error (poll not active)

#### 9.5 — Frontend tests

Configure Jest (from Phase 1) and write tests:

- `api.test.ts` — mock global `fetch`, test each API function returns typed data / throws `ApiError`
- `CreatePoll.test.tsx` — render form, fill fields, submit, verify API called
- `VotePage.test.tsx` — render with mocked poll data, select options, submit
- `ResultsPage.test.tsx` — render with mocked results, verify bars display
- `useSignalR.test.ts` — mock `HubConnectionBuilder`, verify `JoinPoll` called on mount, cleanup on unmount

#### 9.6 — Verification

- [ ] `cd backend/PollApp.Api.Tests && dotnet test` — all tests pass
- [ ] `cd frontend && npm test` — all tests pass
- [ ] Test coverage covers the core flows: create, vote, results, auth

---

## Quick Reference: Phase Dependency Graph

```
Phase 1 (Scaffold)
  ├──→ Phase 2 (Data + Dapper + FluentMigrator)
  │      ├──→ Phase 3 (Auth: cookies + magic link)
  │      │      └──→ Phase 4 (API endpoints)
  │      │             └──→ Phase 5 (SignalR)
  │      └──→ Phase 8 (OpenTelemetry) ← can run parallel with 3/4/5
  ├──→ Phase 6 (Frontend) ← routing/layout parallel; wire API data after Phase 4, SignalR after Phase 5
  └──→ Phase 9 (Tests) ← after Phases 4 + 6
Phase 7 (Polish) ← after all functional phases
```
