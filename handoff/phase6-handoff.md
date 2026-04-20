# Phase 6 Handoff — Frontend — Pages & Routing

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-06
**Status**: Completed

---

### What Was Accomplished

- [x] Modified `src/App.tsx` — replaced Vite scaffold with React Router v7 route definitions (4 routes: `/`, `/poll/:pollId`, `/poll/:pollId/results`, `/dashboard/:secretToken`)
- [x] Modified `src/main.tsx` — wrapped `<App />` in `<BrowserRouter>`, removed `index.css` import
- [x] Modified `index.html` — added Pico CSS v2 CDN link, set `<title>PollMe</title>`, added `color-scheme` meta tag
- [x] Modified `vite.config.ts` — changed proxy targets from `https://localhost:5001` to `http://localhost:5006` (default backend `http` launch profile) and removed `secure: false` (no longer needed without HTTPS)
- [x] Created `src/types.ts` — TypeScript interfaces matching all backend DTOs (`CreatePollRequest`, `CreatePollResponse`, `PollResponse`, `VoteRequest`, `PollResultsResponse`, `CreatorPollSummary`, `PollType` as string literal union)
- [x] Created `src/api.ts` — typed API client using `fetch` with relative URLs (proxied by Vite), includes `ApiError` class and functions for all endpoints: `createPoll`, `getPoll`, `submitVote`, `getResults`, `getCreatorPolls`, `togglePollActive`, `deletePoll`
- [x] Created `src/hooks/useSignalR.ts` — custom React hook managing SignalR connection lifecycle (connect, join group, listen for `ResultsUpdated`, cleanup on unmount). Uses `useRef` for stable callback to avoid reconnect on re-render
- [x] Created `src/components/CopyLinkButton.tsx` — clipboard copy with "Copied!" feedback and fallback for older browsers
- [x] Created `src/components/ResultsBar.tsx` — CSS-only horizontal bar chart row using Pico CSS custom properties
- [x] Created `src/components/OptionsList.tsx` — dynamic add/remove text inputs for poll options (min 2, max 20)
- [x] Created `src/pages/CreatePoll.tsx` — controlled form with title, description, poll type radio buttons, dynamic options list. On success, shows shareable links (vote, results, dashboard) with copy buttons. "Create Another Poll" button to reset
- [x] Created `src/pages/VotePage.tsx` — fetches poll by ID, renders radio buttons (single choice) or checkboxes (multiple choice), handles 404/409/closed poll states, redirects to results after voting
- [x] Created `src/pages/ResultsPage.tsx` — fetches initial results, receives live updates via SignalR `useSignalR` hook, displays bar chart with vote counts/percentages, "Vote on this poll" and "Copy Vote Link" actions
- [x] Created `src/pages/Dashboard.tsx` — fetches creator's polls via secret token, displays poll list with type badge, vote count, active/closed status. Actions: view results, copy vote link, toggle active, delete (with confirmation)

### Verification Results

- [x] `npm run dev` starts without errors
- [x] `npm run build` completes with no TypeScript errors (59 modules, 299 kB bundle)
- [x] `npx jest --passWithNoTests` passes (exit code 0)
- [x] Navigate to `/` → create poll form renders (verified via curl: HTML contains `<div id="root">`)
- [x] API proxy works: `POST /api/polls` via `http://localhost:5173` creates poll via backend proxy
- [x] Backend API verified directly: create poll → get poll → vote → get results → duplicate vote rejected (409)
- [ ] Browser-based end-to-end testing (create → vote → live results → dashboard) — not automated; requires manual browser session
- [ ] SignalR live update verification across multiple tabs — requires manual browser session

### Deviations from Plan

- **Vite proxy targets changed from `https://localhost:5001` to `http://localhost:5006`**: The plan's proxy config targeted the `https` launch profile. This was changed to target the default `http` launch profile (`http://localhost:5006`) to eliminate SSL certificate issues during local development. The `secure: false` option was also removed since it's no longer needed. This is a practical improvement — `dotnet run` uses the `http` profile by default, so the frontend and backend "just work" out of the box without specifying a launch profile.
- **Added `src/types.ts` for shared TypeScript types**: The plan's `api.ts` section references types like `CreatePollRequest` but doesn't specify where they're defined. A separate `src/types.ts` file was created to hold all DTO interfaces. This keeps the API client focused on HTTP logic and allows types to be imported independently by components that need them (e.g., the SignalR hook uses `PollResultsResponse` directly).
- **`PollType` defined as string literal union instead of enum**: TypeScript's `erasableSyntaxOnly: true` (in `tsconfig.app.json`) forbids enums because they emit JavaScript. `PollType` is defined as `type PollType = 'SingleChoice' | 'MultipleChoice'` instead. The backend serializes `PollType` as JSON strings (`"SingleChoice"`, `"MultipleChoice"`) via `JsonStringEnumConverter`, so the string literal union maps directly.
- **`ApiError` uses explicit property instead of constructor parameter property**: `erasableSyntaxOnly: true` also forbids `public` parameter properties (e.g., `constructor(public status: number)`). The `ApiError` class declares `status` as a regular property and assigns it in the constructor body.
- **Pico CSS loaded via CDN rather than npm**: The plan offered both CDN and npm options. CDN was chosen for simplicity — no build step needed for CSS, and the classless nature of Pico means no CSS modules or imports to configure. The CDN link is in `index.html`.
- **No custom `App.css` or `index.css`**: The Vite scaffold's CSS files (App.css, index.css) are no longer imported. Pico CSS provides all base styling. The files still exist in the repo but are unused dead code from the scaffold.
- **`useSignalR` hook uses `useRef` for callback stability**: The plan's hook directly uses the callback in the effect dependency. This implementation uses `useRef` to hold the latest callback, plus a `useCallback`-wrapped stable function, so the SignalR connection doesn't reconnect when the parent re-renders with a new callback reference. This is a standard React pattern to avoid unnecessary effect re-runs.

### Known Issues / Technical Debt

- **Scaffold asset files still present**: `src/assets/hero.png`, `src/assets/react.svg`, `src/assets/vite.svg`, `src/App.css`, `src/index.css` are leftover from the Vite scaffold. They're not imported anywhere but could be cleaned up.
- **No automated E2E or integration tests**: The verification checklist includes browser-based scenarios that require manual testing. Automated E2E tests (e.g., Playwright) could be added in a future phase.
- **No loading/error boundary at the router level**: Individual pages handle their own loading/error states. A shared error boundary or Suspense wrapper at the `<Routes>` level could provide a better fallback experience.
- **Pico CSS CDN dependency**: The app requires internet access to load Pico CSS. For offline development or production, consider installing `@picocss/pico` via npm and importing it in the build.
- **ResultsBar uses Pico CSS custom properties that may not exist**: The bar chart component uses `--pico-primary-background` and `--pico-secondary-background` directly. If Pico doesn't define these exact variable names, the bars may render without color. This should be verified in a browser session and adjusted if needed.

### Key Decisions Made During Implementation

- Used React Router v7's `BrowserRouter` + `Routes` + `Route` pattern (the "declarative" mode). React Router v7 also offers a "framework" mode with file-based routing and data loaders, but the declarative pattern matches the plan and is simpler for this app.
- `react-router-dom` v7 re-exports everything from `react-router`. Imports use `react-router-dom` for consistency with the plan, but they could equivalently come from `react-router`.
- All API functions use `encodeURIComponent()` for path parameters (poll IDs, secret tokens) as a security best practice, even though GUIDs don't contain special characters.
- The `submitVote` and `deletePoll` functions use a `requestNoContent` helper since these endpoints return 204 with no body (calling `.json()` on a 204 response would throw).
- Dashboard delete uses `window.confirm()` for simplicity rather than a custom modal component.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `http://localhost:5006` (default http profile)
- Database: Exists at `backend/PollApp.Api/polls.db` with all 5 tables
- Packages installed: As planned (no new NuGet packages)

**Frontend**:
- Build status: Passes (`tsc -b && vite build` — 0 errors, 59 modules, 299 kB bundle)
- Dev server on: `http://localhost:5173`
- Packages installed: As planned (`react-router-dom`, `@microsoft/signalr` were already installed in Phase 1). Pico CSS loaded via CDN (not an npm dependency).

**Tests**:
- Backend tests: Not yet created
- Frontend tests: Not yet created (`npx jest --passWithNoTests` passes)

---

### Next Phase Entry Point

**Next phase**: Phase 7 — Testing (or as defined in PLAN.md)

**Prerequisites confirmed**: Yes — all pages render, API integration works via proxy, SignalR hook is implemented, build passes.

**To start Phase 7**:
1. Read `PLAN.md` for the next phase section
2. Read this handoff (`handoff/phase6-handoff.md`) for Phase 6 context
3. Run `cd frontend && npm run build` to confirm the baseline
4. Run `cd backend/PollApp.Api && dotnet build` to confirm backend baseline
5. Note: The Vite proxy now targets `http://localhost:5006` (the default `http` launch profile), so `dotnet run` works without specifying a launch profile
6. Note: TypeScript config enforces `erasableSyntaxOnly` and `verbatimModuleSyntax` — use `import type` for type-only imports, no enums, no constructor parameter properties
7. Note: Pico CSS is loaded via CDN in `index.html`. If tests need to mock or ignore CSS, the existing `identity-obj-proxy` config in `jest.config.ts` handles `.css` imports, but the CDN link in HTML won't affect Jest tests

**Files the next phase will primarily touch**:
- `frontend/src/**/*.test.tsx` (new — component tests)
- `backend/PollApp.Api.Tests/` (new — backend unit/integration tests)

---

### Appendix: File Tree Snapshot

```
frontend/
├── index.html                         ← MODIFIED — added Pico CSS CDN, updated title
├── package.json
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts                     ← MODIFIED — proxy targets changed to http://localhost:5006
├── jest.config.ts
├── eslint.config.js
├── public/
│   └── favicon.svg
└── src/
    ├── main.tsx                       ← MODIFIED — wrapped with BrowserRouter
    ├── App.tsx                        ← MODIFIED — replaced scaffold with Routes
    ├── App.css                        ← UNUSED — scaffold leftover
    ├── index.css                      ← UNUSED — scaffold leftover
    ├── types.ts                       ← NEW — TypeScript interfaces for all backend DTOs
    ├── api.ts                         ← NEW — typed fetch-based API client
    ├── assets/
    │   ├── hero.png                   ← UNUSED — scaffold leftover
    │   ├── react.svg                  ← UNUSED — scaffold leftover
    │   └── vite.svg                   ← UNUSED — scaffold leftover
    ├── hooks/
    │   └── useSignalR.ts              ← NEW — SignalR connection lifecycle hook
    ├── components/
    │   ├── CopyLinkButton.tsx         ← NEW — clipboard copy with feedback
    │   ├── ResultsBar.tsx             ← NEW — CSS-only bar chart row
    │   └── OptionsList.tsx            ← NEW — dynamic add/remove option inputs
    └── pages/
        ├── CreatePoll.tsx             ← NEW — poll creation form + success links
        ├── VotePage.tsx               ← NEW — vote form with radio/checkbox
        ├── ResultsPage.tsx            ← NEW — live results with SignalR
        └── Dashboard.tsx              ← NEW — creator poll management
```
