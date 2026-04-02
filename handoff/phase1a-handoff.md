# Phase 1.a Handoff — CI Pipeline

**Completed by**: AI agent (Thinking Beast Mode)
**Date**: 2026-04-01
**Status**: Completed

---

### What Was Accomplished

- [x] Created `.github/workflows/ci.yml` — GitHub Actions CI pipeline
- [x] CI triggers on pushes to `main` and on pull requests targeting `main`
- [x] `build_and_test_dotnet` job: restores, builds, and tests the .NET 9.0 backend (solution-level)
- [x] `build_and_test_frontend` job: installs, builds, and tests the frontend (Node 22.x, `npx jest --passWithNoTests`)
- [x] Both jobs run in parallel on `ubuntu-latest`

### Verification Results

- [x] YAML syntax is valid
- [x] .NET SDK version set to `9.0.x` (matches the project's `net9.0` target framework)
- [x] Node.js version set to `22.x` (LTS, compatible with all project dependencies)
- [x] `dotnet restore` / `dotnet build` / `dotnet test` run at solution root (picks up all projects)
- [x] Frontend steps use `working-directory: ./frontend` and `npm ci` for deterministic installs
- [x] Frontend test step uses `npx jest --passWithNoTests` (avoids npm 11.x CLI config issue noted in Phase 1 handoff)

### Deviations from Plan

- **Interim phase**: This phase was not in the original `PLAN.md`. It was added as Phase 1.a between Phase 1 (Scaffolding) and Phase 2 (Data Model & Database) to establish CI early.
- **No Chromium verification**: The example CI included a `chromium-browser --version` step. This was omitted because no browser-based tests exist yet.

### Known Issues / Technical Debt

- **No backend tests exist yet**: `dotnet test` will discover zero tests and succeed. Once a test project is added in a later phase, the CI will automatically pick it up via the solution file.
- **No frontend tests exist yet**: `npx jest --passWithNoTests` exits 0 with no tests. Once tests are added, the `--passWithNoTests` flag can be removed if desired (it's harmless either way).
- **`npm ci` requires `package-lock.json` in source control**: Ensure `frontend/package-lock.json` is committed. If it's gitignored, the CI will fail.

### Key Decisions Made During Implementation

- Used `npx jest --passWithNoTests` instead of `npm test` to avoid npm 11.x CLI issues (consistent with Phase 1 handoff findings).
- Kept the two jobs independent (parallel execution) since they have no dependencies on each other.
- Used `npm ci` (not `npm install`) for deterministic, lockfile-based installs in CI.
- Did not add caching steps to keep the workflow as simple as possible; caching can be added later if build times warrant it.

---

### Environment State

**Backend**:
- Build status: Passes (unchanged from Phase 1)
- Running on: `https://localhost:5001`
- Database: Not yet created
- Packages installed: As planned in Phase 1

**Frontend**:
- Build status: Passes (unchanged from Phase 1)
- Dev server on: `http://localhost:5173`
- Packages installed: As planned in Phase 1

**Tests**:
- Backend tests: Not yet created (CI will pass with 0 tests)
- Frontend tests: Not yet created (CI will pass via `--passWithNoTests`)

**CI**:
- Workflow file: `.github/workflows/ci.yml`
- Triggers: push to `main`, PRs targeting `main`
- Jobs: `build_and_test_dotnet` (.NET 9.0), `build_and_test_frontend` (Node 22.x)

---

### Next Phase Entry Point

**Next phase**: Phase 2 — Data Model & Database (Dapper + FluentMigrator)

**Prerequisites confirmed**: Yes — backend project builds and runs, CI is in place.

**To start Phase 2**:
1. Read `PLAN.md`, section "Phase 2: Data Model & Database"
2. Read `handoff/phase1-handoff.md` for Phase 1 context
3. Run `cd backend/PollApp.Api && dotnet build` to confirm the baseline
4. Note: Project targets `net9.0`, FluentMigrator version is 8.0.1

**Files the next phase will primarily touch**:
- `backend/PollApp.Api/Entities/` (new — entity POCOs)
- `backend/PollApp.Api/Migrations/` (new — FluentMigrator migration classes)
- `backend/PollApp.Api/Repositories/` (new — interfaces + Dapper implementations)
- `backend/PollApp.Api/Program.cs` (add FluentMigrator registration + DI)
- `backend/PollApp.Api/appsettings.Development.json` (add connection string)

---

### Appendix: File Added

```
.github/
└── workflows/
    ├── ci.yml              ← NEW — CI pipeline
    └── example-ci.yml      ← reference file (pre-existing)
```
