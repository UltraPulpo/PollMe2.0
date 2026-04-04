# Phase 2 Handoff — Data Model & Database (Dapper + FluentMigrator)

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-01
**Status**: Completed

---

### What Was Accomplished

- [x] Created 6 entity POCOs in `Entities/`: `Creator`, `Poll`, `PollOption`, `Vote`, `VoteChoice`, `PollType` (enum)
- [x] Created 5 FluentMigrator migration classes in `Migrations/`:
  - `Migration_001_CreateCreatorsTable` — with unique index on `SecretToken`
  - `Migration_002_CreatePollsTable` — FK to Creators
  - `Migration_003_CreatePollOptionsTable` — FK to Polls
  - `Migration_004_CreateVotesTable` — FK to Polls, unique composite index on `(PollId, VoterToken)`
  - `Migration_005_CreateVoteChoicesTable` — FKs to Votes and PollOptions
- [x] Added connection string `"Data Source=polls.db"` to `appsettings.Development.json`
- [x] Registered FluentMigrator in `Program.cs` with `AddSQLite()` and auto-migration on startup
- [x] Created 3 repository interfaces in `Repositories/`: `IPollRepository`, `IVoteRepository`, `ICreatorRepository`
- [x] Created 3 Dapper-backed repository implementations: `PollRepository`, `VoteRepository`, `CreatorRepository`
- [x] Created `GuidTypeHandler` — custom Dapper type handler for Guid ↔ SQLite TEXT conversion
- [x] Created `PollOptionResult` DTO class (in `IVoteRepository.cs`) for aggregated vote results
- [x] Registered all repositories in DI as `AddScoped`
- [x] Registered `GuidTypeHandler` with `SqlMapper.AddTypeHandler()` at startup

### Verification Results

- [x] `dotnet build` succeeds with 0 warnings, 0 errors
- [x] `dotnet run` starts and creates `polls.db` file (61 KB) with all 5 tables
- [x] All 5 migrations ran successfully (verified via migration runner console output):
  - `CREATE TABLE "Creators"` with PK and unique index on SecretToken
  - `CREATE TABLE "Polls"` with FK to Creators
  - `CREATE TABLE "PollOptions"` with FK to Polls
  - `CREATE TABLE "Votes"` with FK to Polls
  - `CREATE UNIQUE INDEX "IX_Votes_PollId_VoterToken"` on Votes(PollId, VoterToken)
  - `CREATE TABLE "VoteChoices"` with FKs to Votes and PollOptions
- [x] Health endpoint `GET /` returns `{"status":"healthy"}` after migrations

### Deviations from Plan

- **Added `GuidTypeHandler`**: The plan doesn't mention a Dapper type handler, but SQLite stores GUIDs as TEXT strings. Without it, Dapper cannot map between C# `Guid` properties and SQLite TEXT columns. This is a standard Dapper pattern for SQLite.
- **Added `PollOptionResult` class**: The plan's `IVoteRepository.GetResultsAsync` returns aggregated results (option text + vote count). Created a `PollOptionResult` class to hold this projection since it doesn't map to any single entity. Defined alongside the `IVoteRepository` interface.
- **Repository constructor uses `IConfiguration`**: The plan example shows `PollRepository(IConfiguration config)` which is what was implemented. The connection string is read from `IConfiguration` in each repository rather than injected as a raw string.
- **Guid parameters handled via `GuidTypeHandler`**: Repository methods pass `Guid` values directly in parameter objects; the custom Dapper `GuidTypeHandler` converts them to/from SQLite `TEXT` columns. DateTime values are formatted as ISO 8601 strings via `.ToString("O")`.
- **Boolean values passed as integers**: SQLite has no native boolean type, so `IsActive` is passed as `1`/`0` in INSERT/UPDATE statements.

### Known Issues / Technical Debt

- **`polls.db` is created in the project directory**: The `Data Source=polls.db` connection string creates the database relative to the working directory. During `dotnet run`, this is `backend/PollApp.Api/`. The file should be added to `.gitignore` if not already ignored.
- **No cascade delete at DB level**: SQLite foreign keys are not enforced by default (requires `PRAGMA foreign_keys = ON`). The `DeleteAsync` method in `PollRepository` manually deletes in FK order within a transaction. A future improvement could enable FK enforcement via a connection interceptor.
- **`Microsoft.AspNetCore.OpenApi` still referenced**: Leftover from Phase 1 template — harmless but unused.

### Key Decisions Made During Implementation

- Used `AsString(36)` for all GUID columns in migrations — stores as TEXT in SQLite, matching Dapper's `GuidTypeHandler` conversion.
- Used `AsString()` (unbounded TEXT) for `CreatedAtUtc` columns — stores ISO 8601 datetime strings.
- Used `AsBoolean()` for `IsActive` — FluentMigrator maps this to INTEGER in SQLite.
- Named foreign keys explicitly (e.g., `FK_Polls_Creators`) for clarity in error messages.
- Repository methods each create their own `SqliteConnection` — Dapper best practice for SQLite (no connection pooling needed).
- Transactional methods (`CreateAsync`, `DeleteAsync`, `CreateVoteAsync`) open the connection explicitly and use `BeginTransactionAsync` for atomicity.
- `GetResultsAsync` uses a `LEFT JOIN` from PollOptions to VoteChoices so options with zero votes still appear in results.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `https://localhost:5001` / `http://localhost:5000`
- Database: Created at `backend/PollApp.Api/polls.db` (5 tables + VersionInfo)
- Packages installed: As planned in Phase 1 (no new NuGet packages added — Dapper, Microsoft.Data.Sqlite, FluentMigrator already present)

**Frontend**:
- Build status: Passes (unchanged from Phase 1)
- Dev server on: `http://localhost:5173`
- Packages installed: Unchanged from Phase 1

**Tests**:
- Backend tests: Not yet created
- Frontend tests: Not yet created

---

### Next Phase Entry Point

**Next phase**: Phase 3 — Authentication — Magic Link / Cookie

**Prerequisites confirmed**: Yes — entities, repositories, and migrations are all in place and verified.

**To start Phase 3**:
1. Read `PLAN.md`, section "Phase 3: Authentication — Magic Link / Cookie"
2. Read this handoff (`handoff/phase2-handoff.md`) for Phase 2 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Note: `ICreatorRepository` already has `CreateAsync` and `GetBySecretTokenAsync` — Phase 3's `CreatorAuthService` will use these
5. The `GuidTypeHandler` is already registered — Guid ↔ TEXT mapping works throughout

**Files the next phase will primarily touch**:
- `backend/PollApp.Api/Services/ICreatorAuthService.cs` (new — creator auth interface)
- `backend/PollApp.Api/Services/CreatorAuthService.cs` (new — cookie-based creator resolution)
- `backend/PollApp.Api/Filters/CreatorRequiredAttribute.cs` (new — action filter for creator auth)
- `backend/PollApp.Api/Helpers/VoterTokenHelper.cs` (new — voter cookie helper)
- `backend/PollApp.Api/Program.cs` (register `ICreatorAuthService` in DI)

---

### Appendix: File Tree Snapshot

```
backend/PollApp.Api/
├── PollApp.Api.csproj
├── Program.cs                          ← MODIFIED — FluentMigrator + DI registration
├── appsettings.json
├── appsettings.Development.json        ← MODIFIED — added ConnectionStrings
├── polls.db                            ← NEW — SQLite database (created at runtime)
├── Properties/
│   └── launchSettings.json
├── Entities/                           ← NEW
│   ├── Creator.cs
│   ├── Poll.cs
│   ├── PollOption.cs
│   ├── PollType.cs
│   ├── Vote.cs
│   └── VoteChoice.cs
├── Migrations/                         ← NEW
│   ├── Migration_001_CreateCreatorsTable.cs
│   ├── Migration_002_CreatePollsTable.cs
│   ├── Migration_003_CreatePollOptionsTable.cs
│   ├── Migration_004_CreateVotesTable.cs
│   └── Migration_005_CreateVoteChoicesTable.cs
└── Repositories/                       ← NEW
    ├── CreatorRepository.cs
    ├── GuidTypeHandler.cs
    ├── ICreatorRepository.cs
    ├── IPollRepository.cs
    ├── IVoteRepository.cs
    ├── PollRepository.cs
    └── VoteRepository.cs
```
