---
phase: 01-baseline-and-contracts
plan: 01
subsystem: planning
tags: [i18n, audit, contracts, frontend, backend]
requires: []
provides:
  - "Live-code localization map covering public UI, admin UI, backend API/email, formatting, and request-language seams"
  - "Canonical inventory counts reconciled against repo evidence"
affects: [public-ui, admin-ui, api, email, testing]
tech-stack:
  added: []
  patterns:
    - "Use inventories as backlog evidence, but ground execution in live repo seams"
    - "Treat locale formatting and request propagation as central seams, not scattered fixes"
key-files:
  created: [.planning/phases/01-baseline-and-contracts/01-01-SUMMARY.md]
  modified: []
key-decisions:
  - "Phase 1 uses the inventory manifests plus live code inspection as the audit baseline"
  - "Formatting migration is a first-class localization seam because multiple surfaces still use ambient locale defaults"
patterns-established:
  - "Future phases must name shared insertion points before broad copy migration"
requirements-completed: [QUAL-01]
coverage:
  - id: D1
    description: "Documented the current public/admin/backend localization backlog counts from repository inventories."
    requirement: "QUAL-01"
    verification:
      - kind: other
        ref: "node -e inventory count script"
        status: pass
    human_judgment: false
  - id: D2
    description: "Mapped the concrete frontend and backend files that later localization phases will modify."
    requirement: "QUAL-01"
    verification:
      - kind: other
        ref: "rg seams scan over frontend/backend localization files"
        status: pass
    human_judgment: false
duration: 18min
completed: 2026-07-23
status: complete
---

# Phase 1: Baseline And Contracts Summary

**Repository-backed localization map covering 79 public UI entries, 209 admin UI entries, 93 backend/email entries, and the concrete locale/formatting/request seams needed for execution**

## Performance

- **Duration:** 18 min
- **Started:** 2026-07-23T00:00:00Z
- **Completed:** 2026-07-23T00:18:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Confirmed the canonical backlog counts from `i18n/inventory/`: 79 public UI entries, 209 admin UI entries, and 93 backend API/email entries.
- Reconciled the prior bilingual plan with the live codebase, confirming that the frontend already has a partial locale provider/catalog but still uses `Locale = "en" | "es"` and a small catalog footprint.
- Identified the execution-critical seams for later phases: shared frontend locale provider/catalog, shared API client, inline locale-formatting hotspots, and backend exception/request pipeline message seams.

## Task Commits

Documentation-only phase work will be captured in the scoped Phase 1 commit after both plans complete.

## Files Created/Modified
- `.planning/phases/01-baseline-and-contracts/01-01-SUMMARY.md` - Audit summary for localization backlog counts and insertion points.

## Decisions Made
- Inventories remain the canonical backlog seed, but live repo inspection is the source of truth for execution.
- Locale-aware formatting is explicitly in scope because public/admin surfaces still use `toLocaleDateString(undefined, ...)`, `toLocaleTimeString(undefined, ...)`, and `toLocaleString(undefined, ...)`.
- Centralization points are already present:
  - Frontend locale state: `openresto-frontend/context/I18nContext.tsx`
  - Frontend locale normalization: `openresto-frontend/i18n/locale.ts`
  - Frontend copy catalog: `openresto-frontend/i18n/messages.ts`
  - Frontend HTTP seam: `openresto-frontend/api/client.ts`
  - Backend request bootstrap: `OpenRestoApi/Program.cs`
  - Backend exception/message seam: `OpenRestoApi/Infrastructure/Exceptions/GlobalExceptionHandler.cs`

## Deviations from Plan

None - plan executed as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Public UI execution can proceed with a clear migration target list from the inventory plus the shared formatting hotspots already identified.
- Backend execution can proceed later without re-auditing the request/message seams.
- Remaining known hot spots from the audit:
  - Current locale contract is still `en | es`, not explicit `en | es-CO`
  - Shared API client does not yet set `Accept-Language`
  - Multiple public/admin components still use ambient locale defaults for date/time rendering

---
*Phase: 01-baseline-and-contracts*
*Completed: 2026-07-23*
