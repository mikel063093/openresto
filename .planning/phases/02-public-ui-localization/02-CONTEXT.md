# Phase 2: Public UI Localization - Context

**Gathered:** 2026-07-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Finish public experience localization and locale-aware shared formatting across guest flows. This phase covers public routes, public/shared components, browser-title/public metadata surfaces, and public formatting helpers. It does not translate restaurant-authored data from the API/database.

</domain>

<decisions>
## Implementation Decisions

### Locale and formatting
- **D-01:** Public UI should use explicit `en` and `es-CO` locale values. Any existing `es` bucket must migrate without breaking persisted user choice.
- **D-02:** Shared public formatting utilities and inline date/time renderers must use the active locale instead of ambient runtime defaults.
- **D-03:** Public browser/document titles and public accessibility labels count as product-owned copy and must be localized.

### Copy migration
- **D-04:** Public product-owned copy should move through the shared catalog/provider rather than screen-local hardcoded strings.
- **D-05:** Tenant-authored restaurant names, descriptions, highlight text, addresses, and social labels remain verbatim. Only fallbacks/default product copy is localized.

### Verification
- **D-06:** Public phase verification requires focused Jest coverage for localized routes/components/helpers, plus at least one guest-path Playwright smoke in both locales by final verification.
- **D-07:** Public phase execution should target the highest-density inventory hotspots first: `BookingForm`, `lookup`, `booking-confirmation`, `OverflowMenu`, `CalendarActions`, and shared common/layout components.

### the agent's Discretion
- Key naming in the frontend catalog, helper/API shape for formatting utilities, and exact test additions can be chosen as long as they preserve the decisions above and fit current frontend patterns.

</decisions>

<specifics>
## Specific Ideas

- Public inventory is concentrated enough for a bounded sweep:
  - `openresto-frontend/components/booking/BookingForm.tsx` — 17 entries
  - `openresto-frontend/app/(user)/lookup.tsx` — 13 entries
  - `openresto-frontend/components/layout/OverflowMenu.tsx` — 7 entries
  - `openresto-frontend/components/booking/CalendarActions.tsx` — 6 entries
  - `openresto-frontend/app/(user)/booking-confirmation/[bookingRef].tsx` — 5 entries
  - `openresto-frontend/components/common/ErrorScreen.tsx` — 5 entries
- Public formatting hotspots already found in audit:
  - `openresto-frontend/utils/formatters.ts`
  - `openresto-frontend/components/booking/BookingDetailRows.tsx`
  - `openresto-frontend/components/common/DatePicker.tsx`
  - `openresto-frontend/components/common/DatePicker.web.tsx`
  - `openresto-frontend/app/(user)/lookup.tsx`
  - `openresto-frontend/utils/notifications.ts`

</specifics>

<canonical_refs>
## Canonical References

### Roadmap and constraints
- `.planning/PROJECT.md` — scope, preserved-content rule, and contract constraints.
- `.planning/REQUIREMENTS.md` — `LOC-02`, `LOC-03`, `PUB-01`, `PUB-02`.
- `.planning/ROADMAP.md` — approved Phase 2 goal and plan structure.
- `.planning/STATE.md` — current project status after Phase 1 completion.
- `.planning/phases/01-baseline-and-contracts/01-CONTEXT.md` — locked baseline decisions and seams.
- `.planning/phases/01-baseline-and-contracts/01-01-SUMMARY.md` — inventory counts and hotspot mapping.
- `.planning/phases/01-baseline-and-contracts/01-02-SUMMARY.md` — preservation and verification contract.

### Implementation surfaces
- `openresto-frontend/context/I18nContext.tsx` — locale provider and explicit override persistence.
- `openresto-frontend/i18n/locale.ts` — locale normalization and browser detection.
- `openresto-frontend/i18n/messages.ts` — current public/admin catalog entry point.
- `openresto-frontend/utils/formatters.ts` and `openresto-frontend/utils/notifications.ts` — shared public formatting helpers.
- `openresto-frontend/app/(user)/**` — public route surfaces.
- `openresto-frontend/components/booking/**`, `components/common/**`, `components/layout/**`, `components/restaurant/**` — public/shared UI surfaces.

### Backlog inputs
- `i18n/inventory/public-ui.json` — public untranslated/review-needed backlog.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `useI18n()` already exists and is mounted in the app root, so public screens/components can read `locale` and `t()` without adding new provider structure.
- `messages.ts` already supports interpolation and simple plural behavior.
- `PageContainer`, `Navbar`, `Footer`, `OverflowMenu`, and `LanguageSelector` provide shared public UI entry points.

### Established Patterns
- Public screens already have focused Jest tests under `openresto-frontend/tests/app/(user)` and component tests under `openresto-frontend/tests/components/**`.
- Some product copy fallbacks already use `t()` in the public home screen, showing the intended direction of travel.
- Inline formatting is still common, so shared locale helpers should reduce repeated `toLocale*` usage rather than just patching each call ad hoc.

### Integration Points
- Locale contract changes in `locale.ts` and `I18nContext.tsx` will affect both public and admin phases, so Phase 2 should make those changes safely and update tests.
- Public route title/accessibility copy flows through route/component files, not a separate metadata layer.

</code_context>

<deferred>
## Deferred Ideas

- Admin route/component copy migration belongs to Phase 3.
- Centralized `Accept-Language` transport changes belong to Phase 4, even if Phase 2 prepares the locale primitives they depend on.
- Backend/email localization belongs to Phases 4 and 5.

</deferred>

---

*Phase: 02-public-ui-localization*
*Context gathered: 2026-07-23*
