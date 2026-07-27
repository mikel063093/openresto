# Requirements

## Source Of Truth For This Planning Pass
- User-confirmed decisions from the request.
- Verified repository evidence captured in `.planning/codebase/CODEBASE_MAP.md`.
- Existing in-repo planning inputs under `docs/plans/`.

## Confirmed Feature Requirements
- Feature name: `internal-operator-mcp`.
- Audience: internal operators only, never end customers or WhatsApp users.
- Ownership rule: "own bookings" means only the internal operator identity that created a reservation may read, modify, or cancel it.
- MVP tools/capabilities:
  - availability lookup
  - create reservation
  - list own reservations
  - get own reservation
  - modify own reservation
  - cancel own reservation
  - escalation/hand-off to a human
- Transport and hosting: remote MCP over HTTPS, hosted on the VPS.
- Agent credentials must be:
  - short-lived/expiring
  - revocable
  - scoped
  - limited to one or more restaurants
  - rate-limited

## Non-Functional Requirements
- Recommendations must stay compatible with ASP.NET Core, EF Core, SQLite, Docker Compose, and Nginx already in use.
- Do not rely on unverified infrastructure or managed services not present in the repo.
- Preserve current public booking and admin flows unless a later implementation phase explicitly changes them.
- Add server-side authorization as the security boundary; client/tool hints are not sufficient.
- Preserve startup migration conventions and existing test patterns.
