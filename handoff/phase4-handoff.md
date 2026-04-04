# Phase 4 Handoff — API Endpoints

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-03
**Status**: Completed

---

### What Was Accomplished

- [x] Created `DTOs/CreatePollRequest.cs` — validated request with `[Required]`, `[MaxLength]`, `[MinLength]` attributes
- [x] Created `DTOs/CreatePollResponse.cs` — includes pollId, secretToken, voteUrl, dashboardUrl
- [x] Created `DTOs/PollResponse.cs` — includes nested `PollOptionResponse` class for options (id + text)
- [x] Created `DTOs/VoteRequest.cs` — validated list of optionIds with `[Required]`, `[MinLength(1)]`
- [x] Created `DTOs/PollResultsResponse.cs` — includes nested `PollOptionResultResponse` with id, text, voteCount, percentage
- [x] Created `DTOs/CreatorPollSummary.cs` — id, title, pollType, totalVotes, isActive, createdAtUtc
- [x] Created `Controllers/PollsController.cs` — 6 endpoints: POST create, GET poll, POST vote, GET results, PATCH toggle, DELETE
- [x] Created `Controllers/CreatorController.cs` — GET /api/creator/{secretToken}/polls
- [x] Added `GetVoteCountAsync(Guid pollId)` to `IVoteRepository` and `VoteRepository` for efficient vote counting in dashboard
- [x] Modified `Program.cs` — added `JsonStringEnumConverter`, `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`

### Verification Results

- [x] `dotnet build` succeeds with 0 warnings, 0 errors
- [x] POST `/api/polls` with valid body → 201 with poll data + secretToken + creator_token cookie
- [x] GET `/api/polls/{id}` → 200 with poll + options (PollType serialized as string "SingleChoice")
- [x] POST `/api/polls/{id}/vote` → 204; voter_token cookie set; second vote from same token → 409
- [x] GET `/api/polls/{id}/results` → 200 with vote counts and percentages
- [x] GET `/api/creator/{token}/polls` → 200 with poll list including totalVotes
- [x] PATCH `/api/polls/{id}` without creator cookie → 401
- [x] PATCH `/api/polls/{id}` with wrong creator → 403
- [x] PATCH `/api/polls/{id}` with correct creator → 200 with toggled isActive
- [x] DELETE `/api/polls/{id}` → 204; subsequent GET → 404
- [x] Invalid request bodies (missing title, too few options) → 400 with ProblemDetails validation errors
- [x] Vote with invalid optionIds → 400
- [x] GET nonexistent poll → 404
- [x] GET nonexistent creator → 404

### Deviations from Plan

- **Added `GetVoteCountAsync` to `IVoteRepository`**: The plan's `CreatorPollSummary` DTO requires a `TotalVotes` field per poll, but no existing repository method returned a simple vote count per poll. Added `GetVoteCountAsync(Guid pollId)` which runs `SELECT COUNT(1) FROM Votes WHERE PollId = @PollId`. This is more efficient than calling `GetResultsAsync` and summing option-level counts.
- **SignalR broadcast deferred to Phase 5**: The plan mentions "broadcast results via SignalR" in the vote endpoint, but SignalR setup is Phase 5. The vote endpoint has a comment placeholder (`// SignalR broadcast will be added in Phase 5`) and works correctly without it.
- **Added `JsonStringEnumConverter`**: Not explicitly in the Phase 4 plan, but necessary for `PollType` to serialize as `"SingleChoice"`/`"MultipleChoice"` strings rather than integers `0`/`1` in JSON responses. This matches the frontend expectations from Phase 6.
- **Added `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`**: The plan mentions ProblemDetails for error responses. Added these ASP.NET Core built-in services to ensure all error responses (including model validation, unhandled exceptions, and status codes) return RFC 7807 ProblemDetails format.

### Known Issues / Technical Debt

- **`CreatedAtUtc` serialization**: The `CreatedAtUtc` field is stored as ISO 8601 text in SQLite and read back by Dapper as a `DateTime`. The JSON output shows it with a timezone offset (e.g., `2026-04-03T20:36:10.2810385-07:00`) rather than UTC `Z` suffix. This is a data layer concern from Phase 2 and does not affect functionality.
- **N+1 query in `CreatorController.GetCreatorPolls`**: The endpoint fetches all polls for a creator, then calls `GetVoteCountAsync` for each poll individually. For creators with many polls, this could be optimized with a single aggregate query. Acceptable for current scale.
- **No rate limiting**: No rate limiting on poll creation or voting endpoints. Acceptable for a portfolio project.

### Key Decisions Made During Implementation

- Used `CreatedAtAction(nameof(GetPoll), ...)` in the create endpoint to return proper 201 with Location header pointing to the new poll's GET URL.
- Vote endpoint validates: (a) poll exists, (b) poll is active, (c) voter hasn't already voted, (d) all optionIds belong to this poll, (e) single-choice has exactly 1 option. Each returns a descriptive ProblemDetails response.
- The PATCH endpoint toggles `IsActive` (flips the current value) rather than requiring the client to specify the new value — matches the plan's "Toggle `IsActive`" description.
- Controller methods use `ControllerBase.Problem()` helper for structured error responses, which automatically formats as ProblemDetails JSON.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (http profile) or `https://localhost:5001` (https profile)
- Database: Exists at `backend/PollApp.Api/polls.db` with all 5 tables
- Packages installed: As planned (no new NuGet packages added)

**Frontend**:
- Build status: Passes (unchanged from Phase 1)
- Dev server on: `http://localhost:5173`
- Packages installed: Unchanged from Phase 1

**Tests**:
- Backend tests: Not yet created
- Frontend tests: Not yet created

---

### Next Phase Entry Point

**Next phase**: Phase 5 — Real-time Updates via SignalR

**Prerequisites confirmed**: Yes — all API endpoints are functional, vote endpoint works, results endpoint returns data.

**To start Phase 5**:
1. Read `PLAN.md`, section "Phase 5: Real-time Updates via SignalR"
2. Read this handoff (`handoff/phase4-handoff.md`) for Phase 4 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Note: The vote endpoint in `PollsController.Vote()` has a comment `// SignalR broadcast will be added in Phase 5` — inject `IHubContext<PollHub>` and add broadcast call there
5. Note: SignalR is already referenced by the frontend package `@microsoft/signalr` (installed in Phase 1), but the backend needs `builder.Services.AddSignalR()` and `app.MapHub<PollHub>("/hubs/poll")`
6. Note: The `GetResultsAsync` method in `VoteRepository` is ready to provide result data for broadcasting

**Files the next phase will primarily touch**:
- `backend/PollApp.Api/Hubs/PollHub.cs` (new — SignalR hub class)
- `backend/PollApp.Api/Controllers/PollsController.cs` (modify — add IHubContext injection and broadcast after vote)
- `backend/PollApp.Api/Program.cs` (modify — add `AddSignalR()` and `MapHub`)

---

### Appendix: File Tree Snapshot

```
backend/PollApp.Api/
├── PollApp.Api.csproj
├── Program.cs                          ← MODIFIED — added JsonStringEnumConverter, ProblemDetails, error handling
├── appsettings.json
├── appsettings.Development.json
├── polls.db
├── Properties/
│   └── launchSettings.json
├── Controllers/                        ← NEW
│   ├── PollsController.cs
│   └── CreatorController.cs
├── DTOs/                               ← NEW
│   ├── CreatePollRequest.cs
│   ├── CreatePollResponse.cs
│   ├── CreatorPollSummary.cs
│   ├── PollResponse.cs
│   ├── PollResultsResponse.cs
│   └── VoteRequest.cs
├── Entities/
│   ├── Creator.cs
│   ├── Poll.cs
│   ├── PollOption.cs
│   ├── PollType.cs
│   ├── Vote.cs
│   └── VoteChoice.cs
├── Filters/
│   └── CreatorRequiredAttribute.cs
├── Helpers/
│   └── VoterTokenHelper.cs
├── Migrations/
│   ├── Migration_001_CreateCreatorsTable.cs
│   ├── Migration_002_CreatePollsTable.cs
│   ├── Migration_003_CreatePollOptionsTable.cs
│   ├── Migration_004_CreateVotesTable.cs
│   └── Migration_005_CreateVoteChoicesTable.cs
├── Repositories/
│   ├── CreatorRepository.cs
│   ├── GuidTypeHandler.cs
│   ├── ICreatorRepository.cs
│   ├── IPollRepository.cs
│   ├── IVoteRepository.cs             ← MODIFIED — added GetVoteCountAsync, PollOptionResult stays here
│   ├── PollRepository.cs
│   └── VoteRepository.cs              ← MODIFIED — added GetVoteCountAsync implementation
└── Services/
    ├── CreatorAuthService.cs
    └── ICreatorAuthService.cs
```
