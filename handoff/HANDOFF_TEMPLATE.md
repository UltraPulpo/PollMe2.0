# Phase Handoff Template

> **Instructions**: Copy this template into a new file named `handoff/phaseN-handoff.md` at the end of each phase. Fill in every section. This file is the primary context for the next agent picking up work.

---

## Phase [N] Handoff — [Phase Title]

**Completed by**: [Agent/session identifier or "human"]
**Date**: [YYYY-MM-DD]
**Status**: [Completed | Partially Completed | Blocked]

---

### What Was Accomplished

> List every concrete deliverable: files created, packages installed, configurations changed. Be specific — file paths and names matter.

- [ ] ...
- [ ] ...

### Verification Results

> Copy the verification checklist from PLAN.md for this phase. Mark each item as passing or failing. If failing, explain why.

- [ ] ...
- [ ] ...

### Deviations from Plan

> List ANYTHING that was done differently from PLAN.md. Include why the deviation was made. If none, write "None."

- ...

### Known Issues / Technical Debt

> List any issues discovered, shortcuts taken, or things that will need attention later. If none, write "None."

- ...

### Key Decisions Made During Implementation

> List any non-trivial decisions that weren't in the plan (e.g., chose a specific library version, changed a file structure, added a helper class). These are important context for the next phase.

- ...

---

### Environment State

> Describe the current state of the project so the next agent can orient quickly.

**Backend**:
- Build status: [Passes / Fails — explain if fails]
- Running on: [URL, e.g., https://localhost:5001]
- Database: [Created / Not yet / Location of .db file]
- Packages installed: [List any NuGet packages added beyond what's in PLAN.md, or "As planned"]

**Frontend**:
- Build status: [Passes / Fails — explain if fails]
- Dev server on: [URL, e.g., http://localhost:5173]
- Packages installed: [List any npm packages added beyond what's in PLAN.md, or "As planned"]

**Tests**:
- Backend tests: [Passing / Failing / Not yet created]
- Frontend tests: [Passing / Failing / Not yet created]

---

### Next Phase Entry Point

> Tell the next agent exactly how to start. This section should be actionable — the next agent reads PLAN.md for Phase N+1 and this section for practical context.

**Next phase**: Phase [N+1] — [Title]

**Prerequisites confirmed**: [Yes / No — explain if no]

**To start Phase [N+1]**:
1. Read `PLAN.md`, section "Phase [N+1]"
2. [Any specific setup steps before starting — e.g., "Run `dotnet build` to confirm the baseline", "The connection string is in appsettings.Development.json"]
3. [Any gotchas — e.g., "I used FluentMigrator 4.x which has a different API for X", "The frontend proxy config uses `secure: false` because of self-signed certs"]

**Files the next phase will primarily touch**:
- ...
- ...

---

### Appendix: File Tree Snapshot

> Run `tree /F` (Windows) or `find . -type f` (Unix) and paste the relevant output. This gives the next agent a quick overview of what exists.

```
[paste file tree here]
```
