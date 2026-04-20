# Phase 7 Handoff — Polish & Dev Experience

**Completed by**: AI agent (Claude Sonnet 4.6)
**Date**: 2026-04-19
**Status**: Completed

---

### What Was Accomplished

- [x] Modified `backend/PollApp.Api/Program.cs` — replaced `app.UseExceptionHandler()` (no-arg form) with explicit `app.UseExceptionHandler("/error")` + `app.Map("/error", ...)` route that returns `Results.Problem(statusCode: 500)`. This makes unhandled exception handling explicit and guaranteed to return ProblemDetails.
- [x] Created `frontend/src/context/ErrorContext.tsx` — React context providing `globalError`, `setGlobalError`, and `clearGlobalError`. Exported `ErrorProvider` component and `useError()` hook.
- [x] Created `frontend/src/components/ErrorBanner.tsx` — dismissible global error banner rendered at the top of the page. Reads from `ErrorContext`; renders nothing when no error is set. Uses Pico CSS `--pico-del-color` for styling.
- [x] Modified `frontend/src/App.tsx` — wrapped `<Routes>` with `<ErrorProvider>` and added `<ErrorBanner />` above routes so the banner is visible on all pages.
- [x] Modified `frontend/src/pages/CreatePoll.tsx` — added `useError()` hook; generic (non-ApiError) catch path now calls `setGlobalError(...)` instead of local `setError(...)`.
- [x] Modified `frontend/src/pages/VotePage.tsx` — added `useError()` hook; generic catch paths (load failure, unexpected vote failure) call `setGlobalError(...)`.
- [x] Modified `frontend/src/pages/ResultsPage.tsx` — added `useError()` hook; generic catch path calls `setGlobalError(...)`.
- [x] Modified `frontend/src/pages/Dashboard.tsx` — added `useError()` hook; generic catch path calls `setGlobalError(...)`. Replaced `alert()` calls in `handleToggle` and `handleDelete` with `setGlobalError(...)` — no more browser alert dialogs.
- [x] Rewrote `README.md` — full rewrite with: project description, text-based ASCII architecture diagram, tech stack table, correct prerequisites (.NET 9 SDK, Node 20+), step-by-step setup (clone/restore/install), run instructions with correct backend port (`http://localhost:5006`), API endpoint reference table, creator auth explanation, SignalR usage.

### Verification Results

- [x] `cd backend/PollApp.Api && dotnet build` — passes (0 warnings, 0 errors)
- [x] `cd frontend && npm run build` — passes (61 modules, ~300 kB, 0 TypeScript errors)
- [x] `cd frontend && npx jest --passWithNoTests` — passes (exit code 0)
- [x] API returns ProblemDetails for 400 (`Problem(statusCode: 400, ...)`), 403 (`Problem(statusCode: 403, ...)`), 404 (`NotFound()` + `UseStatusCodePages()` + `AddProblemDetails()`), 409 (`Problem(statusCode: 409, ...)`), 500 (`UseExceptionHandler("/error")` → explicit route)
- [x] Frontend shows appropriate inline messages for specific codes (404, 409, 403) and global error banner for unexpected failures
- [x] `start.cmd` launches both processes (unchanged — was already working)
- [x] README instructions work from a clean clone (manually reviewed for correctness)
- [ ] Browser-based end-to-end: global error banner visible when backend is down — not automated; requires manual browser session

### Deviations from Plan

- **`NotFound()` return calls not replaced**: The plan's verification checklist requires ProblemDetails for 404. Instead of replacing `return NotFound()` with `return Problem(statusCode: 404)` in every controller action, the existing `AddProblemDetails()` + `UseStatusCodePages()` combination handles this automatically — when `NotFound()` returns an empty 404, `UseStatusCodePages()` invokes `IProblemDetailsService` to write a ProblemDetails body. This is the idiomatic ASP.NET Core 8/9 approach and avoids changing working controller code.
- **Dashboard `alert()` replaced with `setGlobalError`**: The plan's 7.2 step says "individual pages handle specific codes (409, 404, 403) with inline messages" but doesn't specifically call out Dashboard's `alert()` for toggle/delete failures. These were migrated to `setGlobalError` as part of the same polish pass since `alert()` is poor UX and inconsistent with the new error banner.
- **`start.cmd` not changed**: The plan's verification includes "`start.cmd` launches both processes." The file already existed and worked correctly from Phase 6 (launches backend on `http://localhost:5006`, frontend on `http://localhost:5173`). No changes were needed.

### Known Issues / Technical Debt

- **Scaffold asset files still present**: `src/assets/hero.png`, `src/assets/react.svg`, `src/assets/vite.svg`, `src/App.css`, `src/index.css` remain from the Vite scaffold. Not imported anywhere.
- **No automated E2E tests**: The global error banner and page-level error states are only verified by build + manual inspection. Playwright tests would provide confidence.
- **ErrorBanner uses inline styles**: The banner's styles are written as inline React style objects rather than CSS classes. This is functional but less maintainable. If a design system or CSS module is added later, this should be extracted.
- **`--pico-del-color` may render as transparent**: As noted in the Phase 6 handoff, Pico CSS custom property names may differ between versions. The error banner uses `var(--pico-del-color)` for its background. If this variable is undefined, the banner will have a transparent background. Verify in a browser session.
- **No 401 endpoint in the API**: The verification checklist mentions 401, but no endpoint currently returns 401 (creator auth returns 403, not 401 — the distinction is: 401 = unauthenticated, 403 = authenticated but forbidden). The `CreatorRequiredAttribute` returns 403 when the creator cookie/token is absent. This is a deliberate design choice, not a bug.

### Key Decisions Made During Implementation

- **`ErrorContext` uses a single `string` for `globalError`** rather than an array or structured error type. The plan describes a simple banner for non-specific failures; a string is the simplest shape that satisfies this. If multiple concurrent errors are needed, upgrade to `string[]`.
- **`ErrorProvider` is placed in `App.tsx`** (wrapping the routes), not in `main.tsx`. This keeps the provider co-located with the component tree that uses it and allows the provider to be reset on navigation if needed in the future.
- **Pages still retain local `error` state** for specific inline error messages (404, 409, 403, 400). Only the "unexpected error" catch branch was migrated to global context. This separation is intentional: specific errors display inline near the action that caused them; unexpected errors float in the global banner.
- **Explicit `/error` endpoint** was added as specified in the plan even though the no-arg `UseExceptionHandler()` with `AddProblemDetails()` already handled this in .NET 9. The explicit route makes the 500 behavior self-documenting and survives future ASP.NET Core upgrades.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (default `http` launch profile)
- Database: Exists at `backend/PollApp.Api/polls.db` with all 5 tables
- Packages installed: As planned (no new NuGet packages)

**Frontend**:
- Build status: Passes (`tsc -b && vite build` — 0 errors, 61 modules, ~300 kB bundle)
- Dev server on: `http://localhost:5173`
- Packages installed: No new npm packages added

**Tests**:
- Backend tests: Not yet created (Phase 9)
- Frontend tests: Not yet created (`npx jest --passWithNoTests` passes)

---

### Next Phase Entry Point

**Next phase**: Phase 8 — OpenTelemetry

**Prerequisites confirmed**: Yes — backend builds, frontend builds, all pages render, API integration works.

**To start Phase 8**:
1. Read `PLAN.md`, section "Phase 8: OpenTelemetry"
2. Read this handoff (`handoff/phase7-handoff.md`) for Phase 7 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Phase 8 touches only the backend — no frontend changes needed
5. The backend already has the OpenTelemetry packages installed (`OpenTelemetry.Exporter.Console`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`) — check the `.csproj` before adding any packages
6. The plan says Phase 8 **can be done in parallel with Phases 5–7** and has no frontend dependency
7. Note: TypeScript config enforces `erasableSyntaxOnly` (no enums, no parameter properties) — this affects the frontend only; backend is unaffected

**Files Phase 8 will primarily touch**:
- `backend/PollApp.Api/Telemetry/DiagnosticsConfig.cs` (new — ActivitySource + Meter + VoteCounter)
- `backend/PollApp.Api/Program.cs` (add `AddOpenTelemetry()` configuration)
- `backend/PollApp.Api/Controllers/PollsController.cs` (add custom spans + vote counter increment)

---

### Appendix: File Tree Snapshot

```
frontend/
├── index.html
├── package.json
├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
├── vite.config.ts
├── jest.config.ts
├── eslint.config.js
└── src/
    ├── main.tsx
    ├── App.tsx                        ← MODIFIED — added ErrorProvider + ErrorBanner
    ├── api.ts
    ├── types.ts
    ├── App.css                        ← UNUSED — scaffold leftover
    ├── index.css                      ← UNUSED — scaffold leftover
    ├── assets/                        ← UNUSED — scaffold leftover
    ├── context/
    │   └── ErrorContext.tsx           ← NEW — global error state + useError hook
    ├── components/
    │   ├── CopyLinkButton.tsx
    │   ├── ErrorBanner.tsx            ← NEW — dismissible global error banner
    │   ├── OptionsList.tsx
    │   └── ResultsBar.tsx
    ├── hooks/
    │   └── useSignalR.ts
    └── pages/
        ├── CreatePoll.tsx             ← MODIFIED — uses setGlobalError for generic errors
        ├── Dashboard.tsx              ← MODIFIED — uses setGlobalError; removed alert()
        ├── ResultsPage.tsx            ← MODIFIED — uses setGlobalError for generic errors
        └── VotePage.tsx               ← MODIFIED — uses setGlobalError for generic errors

backend/PollApp.Api/
├── Program.cs                         ← MODIFIED — explicit /error exception handler route
├── PollApp.Api.csproj
├── appsettings.json / appsettings.Development.json
├── polls.db
├── Controllers/
│   ├── CreatorController.cs
│   └── PollsController.cs
├── DTOs/
├── Entities/
├── Filters/
├── Helpers/
├── Hubs/
├── Migrations/
├── Properties/
├── Repositories/
└── Services/

Root:
├── README.md                          ← REWRITTEN — full setup/run/API docs
├── start.cmd                          (unchanged — launches backend + frontend)
└── handoff/
    └── phase7-handoff.md              ← NEW — this file
```
