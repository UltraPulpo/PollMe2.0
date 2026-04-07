# Phase 5 Handoff — Real-time Updates via SignalR

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-03
**Status**: Completed

---

### What Was Accomplished

- [x] Created `Hubs/PollHub.cs` — SignalR hub with `JoinPoll(string pollId)` and `LeavePoll(string pollId)` methods for group management
- [x] Modified `Program.cs` — added `builder.Services.AddSignalR()` and `app.MapHub<PollHub>("/hubs/poll")`
- [x] Modified `Controllers/PollsController.cs` — injected `IHubContext<PollHub>`, added broadcast of `PollResultsResponse` to the poll's SignalR group after each vote

### Verification Results

- [x] `dotnet build` succeeds with 0 warnings, 0 errors
- [x] Backend starts without errors with SignalR hub mapped
- [x] POST to `/hubs/poll/negotiate?negotiateVersion=1` returns 200 — SignalR negotiate endpoint is accessible
- [x] Submitting a vote triggers broadcast code without errors (vote returns 204 as before)
- [x] Duplicate vote prevention still works (second vote returns 409)
- [x] GET `/api/polls/{id}/results` still returns correct aggregated results after vote with broadcast

### Deviations from Plan

- **Broadcast payload is `PollResultsResponse` DTO instead of raw results list**: The plan's example broadcasts `results` (the raw `List<PollOptionResult>` from `GetResultsAsync`). The implementation broadcasts a full `PollResultsResponse` DTO with `PollId`, `Title`, `TotalVotes`, and `Options` (including calculated `Percentage` per option). This is more practical for the frontend — the client receives the exact same response shape it already expects from `GET /api/polls/{id}/results`, so the SignalR handler can directly replace the results state without any client-side transformation.

### Known Issues / Technical Debt

- **No SignalR authentication**: The hub accepts any connection. For a casual poll app this is acceptable — results are public data. If polls were private, hub connections would need auth.
- **Extra query after vote for broadcast**: After saving the vote, the endpoint runs `GetResultsAsync` to build the broadcast payload. This is one extra DB read per vote. For this app's scale it's negligible, but at high scale you'd consider computing the delta or caching.
- **WebSocket proxy requires `https` launch profile**: The Vite config proxies `/hubs` to `https://localhost:5001`, which corresponds to the `https` launch profile in `launchSettings.json`. The default `dotnet run` uses the `http` profile on port 5006. For frontend-backend integration, either run with `dotnet run --launch-profile https` or adjust the Vite proxy target to `http://localhost:5006`.

### Key Decisions Made During Implementation

- Broadcast the full `PollResultsResponse` DTO (with percentage calculations) rather than raw repository data. This ensures the SignalR `ResultsUpdated` event delivers the same JSON shape as the REST endpoint, simplifying frontend integration in Phase 6.
- Used `pollId.ToString()` as the SignalR group name, matching the string parameter in `JoinPoll`/`LeavePoll`. The frontend will call `connection.invoke('JoinPoll', pollId)` with the string poll ID.
- SignalR is built into ASP.NET Core — no additional NuGet package was needed. The `@microsoft/signalr` npm package was already installed in Phase 1 for the frontend.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (http profile) or `https://localhost:5001` (https profile)
- Database: Exists at `backend/PollApp.Api/polls.db` with all 5 tables
- Packages installed: As planned (no new NuGet packages — SignalR is built into ASP.NET Core)

**Frontend**:
- Build status: Passes (unchanged from Phase 1)
- Dev server on: `http://localhost:5173`
- Packages installed: `@microsoft/signalr` already installed in Phase 1

**Tests**:
- Backend tests: Not yet created
- Frontend tests: Not yet created

---

### Next Phase Entry Point

**Next phase**: Phase 6 — Frontend — Pages & Routing

**Prerequisites confirmed**: Yes — all API endpoints work, SignalR hub is mapped, broadcast fires on vote.

**To start Phase 6**:
1. Read `PLAN.md`, section "Phase 6: Frontend — Pages & Routing"
2. Read this handoff (`handoff/phase5-handoff.md`) for Phase 5 context
3. Run `cd frontend && npm run dev` to confirm the Vite dev server starts
4. Note: The SignalR `ResultsUpdated` event sends a `PollResultsResponse` JSON object (same shape as `GET /api/polls/{id}/results`). The `useSignalR` hook should type the callback parameter as `PollResultsResponse` and can set state directly.
5. Note: The Vite proxy for `/hubs` is already configured with `ws: true` for WebSocket support (configured in Phase 1). If running the backend with the default `http` profile (port 5006), update `vite.config.ts` proxy targets accordingly or run backend with `--launch-profile https`.
6. Note: The backend runs on `http://localhost:5006` by default (`http` launch profile) or `https://localhost:5001` (`https` launch profile). The Vite proxy currently targets `https://localhost:5001`.

**Files the next phase will primarily touch**:
- `frontend/src/App.tsx` (modify — add routing)
- `frontend/src/main.tsx` (modify — wrap with BrowserRouter)
- `frontend/src/api.ts` (new — API client)
- `frontend/src/hooks/useSignalR.ts` (new — SignalR hook)
- `frontend/src/pages/CreatePoll.tsx` (new)
- `frontend/src/pages/VotePage.tsx` (new)
- `frontend/src/pages/ResultsPage.tsx` (new)
- `frontend/src/pages/Dashboard.tsx` (new)
- `frontend/src/components/` (new — shared components)
- `frontend/index.html` (modify — add Pico CSS)

---

### Appendix: File Tree Snapshot

```
backend/PollApp.Api/
├── PollApp.Api.csproj
├── Program.cs                          ← MODIFIED — added AddSignalR(), MapHub<PollHub>
├── appsettings.json
├── appsettings.Development.json
├── polls.db
├── Properties/
│   └── launchSettings.json
├── Controllers/
│   ├── PollsController.cs             ← MODIFIED — injected IHubContext<PollHub>, broadcast after vote
│   └── CreatorController.cs
├── DTOs/
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
├── Hubs/
│   └── PollHub.cs                      ← NEW — SignalR hub with JoinPoll/LeavePoll
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
└── Services/
    ├── CreatorAuthService.cs
    └── ICreatorAuthService.cs
```
