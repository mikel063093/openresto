# Phase 3: Admin UI Localization - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Finish admin product-copy localization and admin-specific formatting behavior across login, layout, dashboard, bookings, notifications, locations, settings, and shared admin components. This phase covers product-owned labels, helper text, validation/error copy, keyboard/help text, browser-title/header surfaces, and admin-visible date/time summaries. It does not translate tenant-authored restaurant names, addresses, descriptions, highlights, tags, or other owner-managed content.

</domain>

<decisions>
## Implementation Decisions

### Locale and formatting
- **D-01:** Admin UI uses the same explicit `en` and `es-CO` locale contract already established in Phase 2.
- **D-02:** Admin browser/document titles, native stack titles, dashboard summaries, booking list labels, notification timestamps, and pause/extend time summaries must use the active locale instead of ambient runtime defaults.
- **D-03:** Role identifiers such as `SuperAdmin`, `BookingEditor`, and `BookingViewer` remain unchanged in logic, filtering, and API payload handling even when their visible descriptions are localized.

### Copy migration
- **D-04:** Product-owned admin copy should move through the shared catalog/provider rather than new screen-local translation maps.
- **D-05:** Existing backend/API messages displayed in the admin UI may remain as-is during this phase if they come from the server contract; Phase 4 will localize those centrally. Locally-authored validation/help/state copy in the admin frontend is in scope now.
- **D-06:** Tenant-authored restaurant content shown inside admin cards/forms stays verbatim. Product-owned placeholders, button labels, status labels, and helper text around those fields are translation targets.

### Verification
- **D-07:** Phase 3 verification requires focused admin Jest coverage for route shells, dashboard/bookings/notifications/locations/settings flows, and shared admin/common components in both locales.
- **D-08:** Final roadmap closure still requires an admin Playwright bilingual smoke, but Phase 3 must leave the route/component coverage ready for that later phase.
- **D-09:** Admin route-shell/login/navigation/title surfaces should migrate before deeper settings/notification/forms so the phase preserves a stable top-level experience while expanding to dense component hotspots.

### the agent's Discretion
- Exact message-key names, grouping of admin catalog entries, and which shared formatter helpers are reused versus extended can be chosen as long as the implementation preserves the decisions above and follows current frontend patterns.

</decisions>

<specifics>
## Specific Ideas

- Highest-density route/layout hotspots already visible in code:
  - `openresto-frontend/app/admin/login.tsx`
  - `openresto-frontend/app/admin/_layout.tsx`
  - `openresto-frontend/components/layout/AdminSidebar.tsx`
  - `openresto-frontend/app/admin/dashboard.tsx`
  - `openresto-frontend/app/admin/bookings/index.tsx`
  - `openresto-frontend/app/admin/notifications.tsx`
  - `openresto-frontend/app/admin/locations.tsx`
  - `openresto-frontend/app/admin/settings.tsx`
- Shared admin component hotspots with substantial product copy:
  - `openresto-frontend/components/admin/bookings/**`
  - `openresto-frontend/components/admin/notifications/**`
  - `openresto-frontend/components/admin/locations/**`
  - `openresto-frontend/components/admin/settings/**`
- Known admin formatting hotspots already visible in route code:
  - `openresto-frontend/app/admin/dashboard.tsx`
  - `openresto-frontend/app/admin/locations.tsx`
  - `openresto-frontend/components/admin/settings/EmailFailuresList.tsx`

</specifics>

<canonical_refs>
## Canonical References

### Roadmap and constraints
- `.planning/PROJECT.md` — scope, preserved-content rule, and contract constraints.
- `.planning/REQUIREMENTS.md` — `ADM-01`, `ADM-02`, `ADM-03`, plus `LOC-02` and `LOC-03` context from prior phases.
- `.planning/ROADMAP.md` — approved Phase 3 goal and plan structure.
- `.planning/STATE.md` — current project status after Phase 2 completion.
- `.planning/phases/01-baseline-and-contracts/01-01-SUMMARY.md` — admin seam inventory baseline.
- `.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md` — preservation and contract rules.
- `.planning/phases/02-public-ui-localization/02-VERIFICATION.md` — established frontend i18n verification approach.

### Implementation surfaces
- `openresto-frontend/i18n/messages.ts` — current catalog entry point.
- `openresto-frontend/context/I18nContext.tsx` and `openresto-frontend/i18n/locale.ts` — shared locale contract already updated in Phase 2.
- `openresto-frontend/utils/formatters.ts` — shared locale-aware formatting helpers.
- `openresto-frontend/app/admin/**` — route shells and admin pages.
- `openresto-frontend/components/layout/AdminSidebar.tsx` — shared admin navigation shell.
- `openresto-frontend/components/admin/**` and `openresto-frontend/components/common/KeyboardShortcutsHelp.tsx` — shared admin/common UI surfaces.

### Backlog inputs
- `i18n/inventory/admin-ui.json` — admin untranslated/review-needed backlog.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `useI18n()` and the shared catalog are already mounted app-wide, so admin routes/components can read `locale` and `t()` without new provider work.
- `fmtDate()` and related formatting helpers already exist from the public phase and should be reused or extended rather than adding new ambient `toLocale*` usage.
- Existing admin tests cover the major route and component families, which gives Phase 3 a broad verification surface to upgrade instead of creating test scaffolding from scratch.

### Established Patterns
- Admin shell/navigation already centralizes page titles, route guards, shortcuts, and top-level labels in `app/admin/_layout.tsx` and `components/layout/AdminSidebar.tsx`.
- Admin route screens commonly compose from shared admin components, so migrating those components will remove a large amount of repeated raw product copy.
- Some admin screens still rely heavily on inline strings and `toLocale*` calls, so this phase is a migration, not greenfield localization.

### Integration Points
- Dashboard, bookings, notifications, and locations routes all surface booking or restaurant data from API objects; only product-owned labels and summaries should move to the catalog.
- Settings components often mix product-owned field labels with tenant-authored values; tests must continue proving the values render unchanged while surrounding copy localizes.
- Browser/native title handling in `app/admin/_layout.tsx` should align with the catalog so route labels and shell navigation stay consistent.

</code_context>

<deferred>
## Deferred Ideas

- Centralized `Accept-Language` transport and backend-generated message localization belong to Phase 4.
- Email and outbound notification message localization belong to Phase 5.
- Regression-specific untranslated-string detection and final UAT closure belong to Phase 6.

</deferred>

---

*Phase: 03-admin-ui-localization*
*Context gathered: 2026-07-23*
