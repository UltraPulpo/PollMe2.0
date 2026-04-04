# Phase 3 Handoff — Authentication — Magic Link / Cookie

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-03
**Status**: Completed

---

### What Was Accomplished

- [x] Created `Services/ICreatorAuthService.cs` — interface with `GetCurrentCreatorAsync` and `CreateCreatorAsync` methods
- [x] Created `Services/CreatorAuthService.cs` — cookie-based creator resolution and creation
  - Checks `creator_token` cookie first, then `secretToken` route parameter (magic link)
  - Creates new creators with random `SecretToken` GUID and sets `HttpOnly`/`SameSite=Strict` cookie
- [x] Created `Filters/CreatorRequiredAttribute.cs` — action filter that resolves creator from request; returns 401 if not found; stores creator in `HttpContext.Items["Creator"]`
- [x] Created `Helpers/VoterTokenHelper.cs` — static helper `GetOrCreateVoterToken(HttpContext)` that reads or creates `voter_token` cookie with `HttpOnly`/`SameSite=Strict`
- [x] Registered `ICreatorAuthService` as scoped service in `Program.cs`

### Verification Results

- [x] `dotnet build` succeeds with 0 warnings, 0 errors
- [x] `CreatorAuthService` can be instantiated (registered in DI, resolves via `ICreatorAuthService`)
- [x] Cookie options are `HttpOnly=true`, `SameSite=Strict` for both `creator_token` and `voter_token`

### Deviations from Plan

- None. Implementation follows the plan exactly as specified in Phase 3.

### Known Issues / Technical Debt

- **`Secure = false` on cookies**: Both `creator_token` and `voter_token` cookies have `Secure = false` for localhost development. This should be set to `true` in production to ensure cookies are only sent over HTTPS.
- **No rate limiting on creator creation**: `CreateCreatorAsync` creates a new creator on every call. Phase 4 will handle this by only creating creators during poll creation (auto-create pattern).

### Key Decisions Made During Implementation

- Used `Guid.TryParse` for defensive parsing of cookie and route values in `GetCurrentCreatorAsync` — prevents exceptions from malformed cookie values.
- Made `VoterTokenHelper` a static class rather than a DI service, matching the plan's suggestion of "a helper method (or extension)." This keeps it simple since it has no dependencies.
- The `CreatorRequiredAttribute` resolves `ICreatorAuthService` from `RequestServices` rather than constructor injection, since action filter attributes cannot use constructor DI directly in ASP.NET Core.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `https://localhost:5001` / `http://localhost:5000`
- Database: Exists at `backend/PollApp.Api/polls.db` (unchanged from Phase 2)
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

**Next phase**: Phase 4 — API Endpoints

**Prerequisites confirmed**: Yes — entities, repositories, migrations, and auth services are all in place.

**To start Phase 4**:
1. Read `PLAN.md`, section "Phase 4: API Endpoints"
2. Read this handoff (`handoff/phase3-handoff.md`) for Phase 3 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Note: `ICreatorAuthService` is registered and available for injection into controllers
5. Note: `CreatorRequiredAttribute` is ready to decorate controller actions that require creator identity — the resolved `Creator` is stored in `HttpContext.Items["Creator"]`
6. Note: `VoterTokenHelper.GetOrCreateVoterToken(HttpContext)` is a static method ready for use in the vote endpoint

**Files the next phase will primarily touch**:
- `backend/PollApp.Api/DTOs/` (new folder — request/response DTOs)
- `backend/PollApp.Api/Controllers/PollsController.cs` (new — poll CRUD + vote endpoints)
- `backend/PollApp.Api/Controllers/CreatorController.cs` (new — creator dashboard endpoint)
- `backend/PollApp.Api/Program.cs` (may need minor changes for error handling middleware)

---

### Appendix: File Tree Snapshot

```
backend/PollApp.Api/
├── PollApp.Api.csproj
├── Program.cs                          ← MODIFIED — added ICreatorAuthService DI registration
├── appsettings.json
├── appsettings.Development.json
├── polls.db
├── Properties/
│   └── launchSettings.json
├── Entities/
│   ├── Creator.cs
│   ├── Poll.cs
│   ├── PollOption.cs
│   ├── PollType.cs
│   ├── Vote.cs
│   └── VoteChoice.cs
├── Filters/                            ← NEW
│   └── CreatorRequiredAttribute.cs
├── Helpers/                            ← NEW
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
│   ├── IVoteRepository.cs
│   ├── PollRepository.cs
│   └── VoteRepository.cs
└── Services/                           ← NEW
    ├── CreatorAuthService.cs
    └── ICreatorAuthService.cs
```
