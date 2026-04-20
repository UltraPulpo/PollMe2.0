# Phase 8 Handoff — OpenTelemetry

**Completed by**: AI agent (Claude Sonnet 4.6)
**Date**: 2026-04-19
**Status**: Completed

---

### What Was Accomplished

- [x] Created `backend/PollApp.Api/Telemetry/DiagnosticsConfig.cs` — static class with `ServiceName` constant, `ActivitySource Source`, `Meter Meter`, and `Counter<long> VoteCounter` ("pollapp.votes.count").
- [x] Modified `backend/PollApp.Api/Program.cs` — added `using OpenTelemetry.Metrics`, `using OpenTelemetry.Trace`, `using PollApp.Api.Telemetry` imports; added `builder.Services.AddOpenTelemetry()` with `.WithTracing()` (AddSource, AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, AddConsoleExporter) and `.WithMetrics()` (AddMeter, AddAspNetCoreInstrumentation, AddConsoleExporter).
- [x] Modified `backend/PollApp.Api/Controllers/PollsController.cs` — added `using PollApp.Api.Telemetry` import; added `StartActivity("CreatePoll")` with tags (`poll.title`, `poll.optionCount`, `poll.type`) in `CreatePoll`; added `StartActivity("SubmitVote")` with tag (`poll.id`) and `VoteCounter.Add(1, ...)` call in `Vote`.

### Verification Results

- [x] `dotnet build` succeeds — 0 warnings, 0 errors (`PollApp.Api succeeded (5.1s)`)
- [ ] On app startup, console shows OpenTelemetry initialization messages — not automated; requires running the app
- [ ] Create a poll → console shows trace with "CreatePoll" span + HTTP span — not automated; requires running the app
- [ ] Vote → console shows trace with "SubmitVote" span + vote counter increment — not automated; requires running the app
- [ ] Custom tags (poll.title, poll.id, etc.) appear in console trace output — not automated; requires running the app

### Deviations from Plan

- None. The implementation follows the plan exactly: `DiagnosticsConfig.cs` matches the plan's code sample; `Program.cs` OpenTelemetry configuration matches; `PollsController.cs` span creation and vote counter increment match.

### Known Issues / Technical Debt

- **Console exporter verbosity**: The console exporter prints metrics on a periodic export cycle (default: every 60 seconds for metrics). In local dev this can be noisy. A future improvement would be to conditionally enable the console exporter only in Development environment, or reduce the export interval.
- **No resource attributes**: The `AddOpenTelemetry()` call does not use `.ConfigureResource(r => r.AddService(...))` — traces and metrics will not carry a `service.name` resource attribute in the exported data. This is fine for local console dev but would need to be added before exporting to a collector like Jaeger.
- **No sampling configured**: All traces are sampled by default (100%). For a production deployment this should be adjusted with `SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))` or similar.
- **Scaffold asset files still present**: `src/assets/hero.png`, `src/assets/react.svg`, `src/assets/vite.svg`, `src/App.css`, `src/index.css` remain from Phase 7 (not related to Phase 8).

### Key Decisions Made During Implementation

- **`DiagnosticsConfig` is a `static class`** (not a singleton service registered in DI). This matches the plan and is idiomatic for .NET OpenTelemetry — `ActivitySource` and `Meter` are designed to be static singletons; the SDK subscribes to them by name.
- **Namespace `PollApp.Api.Telemetry`** was added to `DiagnosticsConfig.cs` to match the project's namespace conventions and avoid polluting the global namespace.
- **`using var activity`** in controller actions ensures the span is ended (and its status recorded) when the action returns, even on early-return paths (404, 400, 409). The null-conditional `activity?.SetTag(...)` pattern means no NullReferenceException if the `ActivitySource` has no listeners.
- **OpenTelemetry packages were already present** in the `.csproj` from earlier phases — no new packages needed to be installed.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (default `http` launch profile)
- Database: Exists at `backend/PollApp.Api/polls.db` with all 5 tables
- Packages installed: As planned (OpenTelemetry packages were already in .csproj)

**Frontend**:
- Build status: Passes (from Phase 7 — no changes in Phase 8)
- Dev server on: `http://localhost:5173`
- Packages installed: No new npm packages added

**Tests**:
- Backend tests: Not yet created (Phase 9)
- Frontend tests: Not yet created (`npx jest --passWithNoTests` passes)

---

### Next Phase Entry Point

**Next phase**: Phase 9 — Testing

**Prerequisites confirmed**: Yes — backend builds, frontend builds, all pages render, API integration works, OpenTelemetry is configured.

**To start Phase 9**:
1. Read `PLAN.md`, section "Phase 9: Testing"
2. Read this handoff (`handoff/phase8-handoff.md`) for Phase 8 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Phase 9 requires creating a new test project: `cd backend && dotnet new xunit -n PollApp.Api.Tests`
5. The test project must reference the main project: `dotnet add reference ../PollApp.Api/PollApp.Api.csproj`
6. Install test packages: NSubstitute, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
7. Note: TypeScript config enforces `erasableSyntaxOnly` (no enums, no parameter properties) — affects frontend test files too
8. Note: Backend uses `net9.0`; integration tests using `WebApplicationFactory<Program>` require the test project to also target `net9.0` and the main `Program.cs` must be accessible (add `public partial class Program {}` at the bottom of `Program.cs` if needed for `WebApplicationFactory`)

**Files Phase 9 will primarily touch**:
- `backend/PollApp.Api.Tests/` (new project — Unit/ and Integration/ subdirectories)
- `backend/PollApp.Api/Program.cs` (may need `public partial class Program {}` for integration test factory)
- `frontend/src/__tests__/` (new test files)
- `PollMe2.0.sln` (add new test project to solution)

---

### Appendix: File Tree Snapshot

```
backend/PollApp.Api/
├── Program.cs                              ← MODIFIED — added AddOpenTelemetry() configuration
├── PollApp.Api.csproj                      (unchanged — OTel packages already present)
├── appsettings.json / appsettings.Development.json
├── polls.db
├── Controllers/
│   ├── CreatorController.cs
│   └── PollsController.cs                  ← MODIFIED — added custom spans + VoteCounter.Add
├── DTOs/
├── Entities/
├── Filters/
├── Helpers/
├── Hubs/
├── Migrations/
├── Properties/
├── Repositories/
├── Services/
└── Telemetry/
    └── DiagnosticsConfig.cs                ← NEW — ActivitySource + Meter + VoteCounter

frontend/ (unchanged from Phase 7)
├── index.html
├── package.json
├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
├── vite.config.ts
├── jest.config.ts
├── eslint.config.js
└── src/
    ├── main.tsx
    ├── App.tsx
    ├── api.ts
    ├── types.ts
    ├── context/
    │   └── ErrorContext.tsx
    ├── components/
    │   ├── CopyLinkButton.tsx
    │   ├── ErrorBanner.tsx
    │   ├── OptionsList.tsx
    │   └── ResultsBar.tsx
    ├── hooks/
    │   └── useSignalR.ts
    └── pages/
        ├── CreatePoll.tsx
        ├── Dashboard.tsx
        ├── ResultsPage.tsx
        └── VotePage.tsx

Root:
├── README.md
├── start.cmd
└── handoff/
    └── phase8-handoff.md                   ← NEW — this file
```
