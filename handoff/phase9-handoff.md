# Phase 9 Handoff — Testing

**Completed by**: AI agent (Claude Sonnet 4.6)
**Date**: 2026-04-19
**Status**: Completed

---

### What Was Accomplished

- [x] Added `public partial class Program {}` to `backend/PollApp.Api/Program.cs` (line 125) — required for `WebApplicationFactory<Program>` to compile.
- [x] Changed FluentMigrator registration in `Program.cs` to use a lazy `IConfiguration` lambda (`WithGlobalConnectionString(sp => ...)`) instead of a startup-time local variable — required for integration tests to inject the correct in-memory connection string.
- [x] Created `backend/PollApp.Api.Tests/PollApp.Api.Tests.csproj` — xUnit test project targeting `net9.0` with packages: NSubstitute 5.3.0, FluentAssertions 8.9.0, Microsoft.AspNetCore.Mvc.Testing 9.0.15, Microsoft.Data.Sqlite 10.0.6.
- [x] Added `PollApp.Api.Tests` to solution via `dotnet sln add`.
- [x] Created `backend/PollApp.Api.Tests/Unit/PollsControllerTests.cs` — 14 unit tests covering `GetPoll`, `Vote`, `CreatePoll`, `GetResults` action methods.
- [x] Created `backend/PollApp.Api.Tests/Unit/CreatorAuthServiceTests.cs` — 7 unit tests covering `CreatorAuthService` (cookie auth, route param, fallback).
- [x] Created `backend/PollApp.Api.Tests/Integration/PollApiFactory.cs` — custom `WebApplicationFactory` that provisions a temp SQLite file per test run and cleans up with `SqliteConnection.ClearAllPools()` on dispose.
- [x] Created `backend/PollApp.Api.Tests/Integration/PollsIntegrationTests.cs` — 7 integration tests: full poll lifecycle, double-vote rejection, dashboard isolation, soft-delete, toggle, 404, and 401/403.
- [x] Configured `frontend/jest.config.ts` — updated `transform` block to pass Jest-compatible tsconfig options (`jsx: 'react-jsx'`, `module: 'CommonJS'`, `moduleResolution: 'node'`, `lib: ['es2020','dom','dom.iterable']`, `types: ['jest','@testing-library/jest-dom','node']`); added `setupFiles` pointing to root-level `jest.setup.ts`.
- [x] Created `frontend/jest.setup.ts` — polyfills `TextEncoder`/`TextDecoder` from Node `util` for React Router v7 compatibility in jsdom.
- [x] Created `frontend/src/__tests__/api.test.ts` — 12 tests covering all 7 exported API functions plus `ApiError` class.
- [x] Created `frontend/src/__tests__/CreatePoll.test.tsx` — 6 tests covering form rendering, successful submission, inline error display, success state, and "Create Another Poll" reset.
- [x] Created `frontend/src/__tests__/VotePage.test.tsx` — 7 tests covering loading state, poll rendering, vote submission, double-vote error, generic error, and navigation.
- [x] Created `frontend/src/__tests__/ResultsPage.test.tsx` — 5 tests covering loading state, results display, real-time update via `useSignalR`, and error state.
- [x] Created `frontend/src/__tests__/useSignalR.test.ts` — 6 tests covering connection lifecycle, `onVote` callback firing, and cleanup on unmount.

### Verification Results

- [x] `cd backend && dotnet test PollApp.Api.Tests` — **30/30 passing**, 0 failures
- [x] `cd frontend && npm test` — **37/37 passing**, 0 failures
- [x] Unit tests cover `PollsController` and `CreatorAuthService` — ✅ (14 + 7 tests)
- [x] Integration tests use `WebApplicationFactory` with a real in-memory SQLite DB — ✅ (7 tests)
- [x] Frontend tests cover `api.ts`, `CreatePoll`, `VotePage`, `ResultsPage`, `useSignalR` — ✅
- [x] Backend build still passes after `Program.cs` modifications — ✅

### Deviations from Plan

- **`Microsoft.Data.Sqlite 10.0.6` added** (not in PLAN.md). Required to call `SqliteConnection.ClearAllPools()` before deleting the temp SQLite file in `PollApiFactory.DisposeAsync`. Without it, `File.Delete` throws `IOException` because connections remain pooled after `base.DisposeAsync()`.
- **`jest.setup.ts` placed at repo root of `frontend/`** rather than inside `src/__tests__/`. This is because Jest's default `testMatch` picks up all files in `__tests__/`, causing `setup.ts` to be treated as a test file. Placing it at `frontend/jest.setup.ts` avoids this.
- **`Microsoft.AspNetCore.Mvc.Testing` version pinned to `9.0.15`** (not latest). The latest 10.x package requires `net10.0`. Since the API targets `net9.0`, version `9.0.15` must be used explicitly — `dotnet add package` without a version would install 10.x and fail.

### Known Issues / Technical Debt

- **FluentAssertions 8.x license warning**: FA 8.x introduced a commercial license for certain use cases. The warning printed during test runs (`FluentAssertions requires a license...`) is non-fatal and can be suppressed via environment variable `FA_LICENSE`. This is acceptable for a personal/non-commercial project.
- **jsdom HTML5 form validation**: jsdom enforces `required` attribute validation, which blocks form `onSubmit` handlers when required fields are empty. Tests that exercise form submission must fill all `required` fields (not just the fields under test). This is the correct behavior and tests should be written this way, but it's worth knowing.
- **No snapshot tests**: Snapshot testing (e.g., `toMatchSnapshot()`) was not added. If visual regressions become a concern, add component snapshot tests.
- **No coverage threshold configured**: `jest.config.ts` does not set `coverageThreshold`. If coverage enforcement is desired in CI, add `coverageThreshold: { global: { lines: 80 } }` (or similar).

### Key Decisions Made During Implementation

- **Lazy FluentMigrator connection string**: `WithGlobalConnectionString(string)` captures its value at DI registration time, before `WebApplicationFactory.ConfigureAppConfiguration` adds the override. Changing to `WithGlobalConnectionString(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString(...))` makes it resolve lazily at migration execution time, after configuration overrides are applied.
- **`IHubContext` mock chain**: `SendAsync` on `IClientProxy` is an extension method and cannot be mocked with NSubstitute directly. The correct pattern is to mock `SendCoreAsync` on the `IClientProxy` interface. The full chain: `IHubContext.Clients → IHubClients; IHubClients.Group(...) → IClientProxy; IClientProxy.SendCoreAsync(...) → Task.CompletedTask`.
- **Temp file vs `:memory:` SQLite**: `:memory:` databases are connection-scoped in SQLite — each Dapper call opens a new connection and gets an empty database. A temp file (via `Path.Combine(Path.GetTempPath(), Guid + ".db")`) is used instead to allow connection pooling across the test run.
- **Cookie isolation via `NewClient()`**: Each integration test calls `_factory.NewClient()` to get a fresh `HttpClient` with its own cookie jar. This ensures creator identity (set via `Set-Cookie` on first request) is isolated per test.
- **`jest.mock` hoisting with variable capture**: `const mockNavigate = jest.fn()` declared before `jest.mock('react-router-dom', () => ({ useNavigate: () => mockNavigate }))` works because Jest hoists `jest.mock` calls to the top of the file but the factory function is invoked lazily (after module-level variable initialization). The `mockNavigate` variable is initialized by the time the factory runs.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (default `http` launch profile)
- Database: `backend/PollApp.Api/polls.db` (5 tables: Creators, Polls, Options, Votes, PollTokens)
- Packages installed: Added `Microsoft.Data.Sqlite 10.0.6` to test project (not in main API project)

**Frontend**:
- Build status: Passes (unchanged from Phase 8)
- Dev server on: `http://localhost:5173`
- Packages installed: No new npm packages added (Jest/RTL were already installed)

**Tests**:
- Backend tests: **30/30 passing** (`dotnet test PollApp.Api.Tests`)
- Frontend tests: **37/37 passing** (`npm test` in `frontend/`)

---

### Next Phase Entry Point

**Next phase**: Phase 10 — Deployment / Wrap-Up (see PLAN.md)

**Prerequisites confirmed**: Yes — all 30 backend + 37 frontend tests pass; backend builds; frontend builds.

**To start Phase 10**:
1. Read `PLAN.md`, section "Phase 10"
2. Read this handoff (`handoff/phase9-handoff.md`) for Phase 9 context
3. Run `cd backend && dotnet build` to confirm the baseline
4. Run `cd frontend && npm run build` to confirm the frontend baseline
5. Note: `polls.db` is in `backend/PollApp.Api/` — for deployment, the DB path is configured via `ConnectionStrings:DefaultConnection` in `appsettings.json`
6. Note: `start.cmd` at the repo root starts both backend and frontend dev servers — review it before deploying

**Files Phase 10 will primarily touch**:
- `backend/PollApp.Api/appsettings.json` (connection strings, environment config)
- `frontend/vite.config.ts` (proxy, base URL)
- Root-level deployment files (Dockerfile, docker-compose, CI/CD config — depending on deployment target)
- `README.md` (deployment instructions)

---

### Appendix: File Tree Snapshot

```
backend/
├── PollApp.Api/
│   ├── Program.cs                              ← MODIFIED — lazy FluentMigrator config + partial class Program
│   ├── PollApp.Api.csproj
│   ├── appsettings.json / appsettings.Development.json
│   ├── polls.db
│   ├── Controllers/
│   │   ├── CreatorController.cs
│   │   └── PollsController.cs
│   ├── DTOs/
│   ├── Entities/
│   ├── Filters/
│   ├── Helpers/
│   ├── Hubs/
│   ├── Migrations/
│   ├── Properties/
│   ├── Repositories/
│   ├── Services/
│   └── Telemetry/
│       └── DiagnosticsConfig.cs
└── PollApp.Api.Tests/                          ← NEW — entire directory
    ├── PollApp.Api.Tests.csproj
    ├── Integration/
    │   ├── PollApiFactory.cs
    │   └── PollsIntegrationTests.cs
    └── Unit/
        ├── CreatorAuthServiceTests.cs
        └── PollsControllerTests.cs

frontend/
├── jest.config.ts                              ← MODIFIED — ts-jest options + setupFiles
├── jest.setup.ts                               ← NEW — TextEncoder/TextDecoder polyfill
├── package.json (unchanged)
├── src/
│   ├── __tests__/                              ← NEW — all test files
│   │   ├── api.test.ts
│   │   ├── CreatePoll.test.tsx
│   │   ├── ResultsPage.test.tsx
│   │   ├── useSignalR.test.ts
│   │   └── VotePage.test.tsx
│   ├── api.ts
│   ├── App.tsx
│   ├── context/
│   ├── components/
│   ├── hooks/
│   ├── pages/
│   └── types.ts

Root:
├── PollMe2.0.sln                               ← MODIFIED — test project added
├── README.md
├── start.cmd
└── handoff/
    └── phase9-handoff.md                       ← NEW — this file
```
