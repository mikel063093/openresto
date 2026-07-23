# Requirements: OpenResto Localization Baseline

**Defined:** 2026-07-23
**Core Value:** Users and restaurant staff should receive clear, correctly localized product behavior in English or Colombian Spanish without breaking booking flows, admin operations, or existing contracts.

## v1 Requirements

### Locale Foundations

- [ ] **LOC-01**: The application resolves and persists English or Colombian Spanish as the active product locale without regressing explicit user choice.
- [ ] **LOC-02**: Public and admin browser metadata, accessibility labels, and shared formatting utilities use the active locale instead of ambient runtime defaults.
- [ ] **LOC-03**: Locale-aware formatting supports at least dates, times, and count/plural displays consistently across public and admin surfaces.

### Public UI

- [ ] **PUB-01**: All product-owned public navigation, search, booking, confirmation, lookup, error, and empty-state copy is available in `en` and `es-CO`.
- [ ] **PUB-02**: Public UI preserves restaurant-authored content exactly as authored and never machine-translates owner-managed data.
- [ ] **PUB-03**: Public UI request flows send the active locale to backend APIs through a central client path rather than ad hoc per-screen headers.

### Admin UI

- [ ] **ADM-01**: All product-owned admin navigation, dashboard, bookings, locations, notifications, settings, login, and modal copy is available in `en` and `es-CO`.
- [ ] **ADM-02**: Admin validation, accessibility text, keyboard help, and role-adjacent static descriptions are localized without changing canonical role identifiers in logic or contracts.
- [ ] **ADM-03**: Admin date/time labels and derived summaries use the active locale consistently across dashboards, booking tables, and detail cards.

### Backend API

- [ ] **API-01**: The backend resolves request language centrally from `Accept-Language` with English fallback and exposes localized user-facing messages without changing response shapes.
- [ ] **API-02**: Existing HTTP status codes, JSON keys, and machine-readable contracts remain unchanged while user-visible `message` values become locale-aware.
- [ ] **API-03**: Representative booking, auth, hold, admin, and validation responses are covered by tests in both `en` and `es-CO`.

### Emails and Notifications

- [ ] **MAIL-01**: Transactional booking/admin emails use localized subjects and product-owned body copy based on request or booking language with English fallback.
- [ ] **MAIL-02**: Localized email rendering preserves brand theming and does not translate tenant-authored restaurant content inserted into the template.

### Quality Gates

- [ ] **QUAL-01**: Frontend and backend inventories are reduced to a reviewable source of truth for remaining localization work and used to measure phase completion.
- [ ] **QUAL-02**: Regression checks catch newly introduced raw visible strings or unlocalized backend user messages outside approved allowlists.
- [ ] **QUAL-03**: Phase closure requires automated evidence from relevant Jest, Playwright, backend unit/integration, and localization-specific tests.

## v2 Requirements

### Future Localization Expansion

- **FUT-01**: Support additional locales beyond `en` and `es-CO`.
- **FUT-02**: Add translator-facing catalog workflow, review tooling, or external translation management.
- **FUT-03**: Add locale-aware SEO or route segmentation beyond the current product scope.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Machine translation of restaurant-owned content | Violates the explicit preservation requirement and can corrupt tenant intent |
| API schema redesign for localization | Existing clients and tests depend on current JSON/HTTP contracts |
| Shipping/deployment automation changes | The current request is onboarding and planning only |
| Adding third-party localization SaaS | Not required by the existing codebase or scope |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LOC-01 | Phase 1 | Pending |
| LOC-02 | Phase 2 | Pending |
| LOC-03 | Phase 2 | Pending |
| PUB-01 | Phase 2 | Pending |
| PUB-02 | Phase 2 | Pending |
| PUB-03 | Phase 4 | Pending |
| ADM-01 | Phase 3 | Pending |
| ADM-02 | Phase 3 | Pending |
| ADM-03 | Phase 3 | Pending |
| API-01 | Phase 4 | Pending |
| API-02 | Phase 4 | Pending |
| API-03 | Phase 4 | Pending |
| MAIL-01 | Phase 5 | Pending |
| MAIL-02 | Phase 5 | Pending |
| QUAL-01 | Phase 1 | Pending |
| QUAL-02 | Phase 6 | Pending |
| QUAL-03 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 17 total
- Mapped to phases: 17
- Unmapped: 0

---
*Requirements defined: 2026-07-23*
*Last updated: 2026-07-23 after brownfield onboarding baseline creation*
