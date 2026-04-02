# Phase 1 Handoff — Project Scaffolding

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-01
**Status**: Completed

---

### What Was Accomplished

- [x] `README.md` created with project description and setup instructions
- [x] `start.cmd` created — launches backend and frontend in separate windows
- [x] Backend scaffolded at `backend/PollApp.Api/` via `dotnet new webapi`
- [x] All NuGet packages installed: Dapper, Microsoft.Data.Sqlite, FluentMigrator (+Runner, +Runner.SQLite), OpenTelemetry (+Extensions.Hosting, +Instrumentation.AspNetCore, +Instrumentation.Http, +Exporter.Console)
- [x] WeatherForecast example removed; minimal `Program.cs` with health endpoint at `/` and CORS configured
- [x] Frontend scaffolded at `frontend/` via `create-vite` with `react-ts` template
- [x] Runtime packages installed: `react-router-dom`, `@microsoft/signalr`
- [x] Dev/test packages installed: `jest`, `ts-jest`, `@types/jest`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jest-environment-jsdom`, `identity-obj-proxy`, `ts-node`
- [x] `vite.config.ts` updated with proxy for `/api` and `/hubs` to `https://localhost:5001`
- [x] CORS configured in backend `Program.cs` for `http://localhost:5173`
- [x] `jest.config.ts` created with ts-jest preset, jsdom environment, CSS mocking, and jest-dom setup
- [x] `"test": "jest"` added to frontend `package.json` scripts
- [x] Backend launch profile updated to use `https://localhost:5001` / `http://localhost:5000`

### Verification Results

- [x] `cd backend/PollApp.Api && dotnet build` — succeeds with 0 warnings, 0 errors
- [x] `cd backend/PollApp.Api && dotnet run --launch-profile https` — starts on `https://localhost:5001`, GET `/` returns `{"status":"healthy"}`
- [x] `cd frontend && npm run dev` — starts on `http://localhost:5173`, returns HTTP 200
- [x] `cd frontend && npx jest --passWithNoTests` — exits with code 0
- [x] `start.cmd` — not independently verified (both individual servers confirmed working)

### Deviations from Plan

- **ts-node added as dev dependency**: Jest requires `ts-node` to load `.ts` config files. Added `ts-node` as an additional dev dependency not listed in the plan.
- **Jest config key fixed**: The plan specifies `setupFilesAfterSetup` which is not a valid Jest config key. Used the correct key `setupFilesAfterEnv` instead.
- **Jest config uses `@jest-config-loader` annotation**: Added `@jest-config-loader ts-node` and `@jest-config-loader-options {"transpileOnly": true}` docblocks to `jest.config.ts` to support ESM project (`"type": "module"` in package.json).
- **.NET SDK 9.0 used instead of 8.0**: The installed .NET SDK is 9.0.205, so the project targets `net9.0`. The plan says "8.0+", so this is within spec.
- **`Microsoft.AspNetCore.OpenApi`** package was included by the webapi template — left in place (harmless, can be removed later).
- **`npm test -- --passWithNoTests`** doesn't work with npm 11.x (warns about unknown cli config). Use `npx jest --passWithNoTests` instead.

### Known Issues / Technical Debt

- **Node.js engine warnings**: Node 23.5.0 is installed but some packages (Jest-related, eslint-visitor-keys) require `^18.14.0 || ^20.0.0 || ^22.0.0 || >=24.0.0`. These are only warnings — everything works, but switching to Node 22 LTS via nvm would eliminate them.
- **`@types/jest@30` with `jest@29`**: The latest `@types/jest` is v30 but `jest` is v29. This is a minor type mismatch that should not cause functional issues but could produce type errors in test files. Consider pinning `@types/jest@29` if type issues arise.
- **ASP.NET Core dev certificate not trusted**: The HTTPS dev cert shows a trust warning. Run `dotnet dev-certs https --trust` to resolve.
- **`Microsoft.AspNetCore.OpenApi` leftover**: Still referenced in the .csproj from the template. Can be removed when not needed.

### Key Decisions Made During Implementation

- Used `npx --yes create-vite@latest` to scaffold frontend (auto-accepts package install prompt).
- Backend uses launch profiles with `https://localhost:5001` and `http://localhost:5000` (updated from template defaults).
- Minimal `Program.cs` includes `AddControllers()` for future controller-based endpoints.
- Health check endpoint at `/` returns `{ "status": "healthy" }` as a minimal-API route.

---

### Environment State

**Backend**:
- Build status: Passes (0 warnings, 0 errors)
- Running on: `https://localhost:5001` (launch profile "https")
- Database: Not yet created
- Packages installed: As planned, plus `Microsoft.AspNetCore.OpenApi` (template default). Specific versions: Dapper 2.1.72, Microsoft.Data.Sqlite 10.0.5, FluentMigrator 8.0.1, OpenTelemetry 1.15.x

**Frontend**:
- Build status: Passes (Vite dev server starts successfully)
- Dev server on: `http://localhost:5173`
- Packages installed: As planned, plus `ts-node` (needed for Jest .ts config). Key versions: React 19.2.4, react-router-dom 7.13.2, @microsoft/signalr 10.0.0, Vite 8.0.3, jest 29.7.0, ts-jest 29.4.9

**Tests**:
- Backend tests: Not yet created
- Frontend tests: Not yet created (Jest configured, runs with `--passWithNoTests`)

---

### Next Phase Entry Point

**Next phase**: Phase 2 — Data Model & Database (Dapper + FluentMigrator)

**Prerequisites confirmed**: Yes — backend project builds and runs.

**To start Phase 2**:
1. Read `PLAN.md`, section "Phase 2: Data Model & Database"
2. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
3. The connection string should go in `appsettings.Development.json`
4. Note: The project targets `net9.0` (not `net8.0`), FluentMigrator version is 8.0.1 — verify API compatibility if the plan examples assume older versions

**Files the next phase will primarily touch**:
- `backend/PollApp.Api/Entities/` (new — entity POCOs)
- `backend/PollApp.Api/Migrations/` (new — FluentMigrator migration classes)
- `backend/PollApp.Api/Repositories/` (new — interfaces + Dapper implementations)
- `backend/PollApp.Api/Program.cs` (add FluentMigrator registration + DI)
- `backend/PollApp.Api/appsettings.Development.json` (add connection string)

---

### Appendix: File Tree Snapshot

```
PollMe2.0/
├── PLAN.md
├── PollMe2.0.code-workspace
├── PollMe2.0.sln
├── README.md
├── start.cmd
├── backend/
│   └── PollApp.Api/
│       ├── PollApp.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Properties/
│           └── launchSettings.json
├── frontend/
│   ├── .gitignore
│   ├── eslint.config.js
│   ├── index.html
│   ├── jest.config.ts
│   ├── package.json
│   ├── package-lock.json
│   ├── README.md
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── tsconfig.node.json
│   ├── vite.config.ts
│   ├── public/
│   │   ├── favicon.svg
│   │   └── icons.svg
│   └── src/
│       ├── App.css
│       ├── App.tsx
│       ├── index.css
│       ├── main.tsx
│       └── assets/
│           ├── hero.png
│           ├── react.svg
│           └── vite.svg
└── handoff/
    ├── HANDOFF_TEMPLATE.md
    ├── phase0-handoff.md
    └── phase1-handoff.md
```
