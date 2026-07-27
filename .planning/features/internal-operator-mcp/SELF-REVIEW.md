# Internal Operator MCP Self Review

## Checked
- Shared `BookingService` now rejects cross-restaurant `TableId` / `SectionId` combinations and mismatched table-section pairs for both create and update paths, protecting operator and non-operator callers from the same tenancy flaw.
- Operator escalations now validate and trim `Reason`, persist a durable `AdminNotification` intent before reporting success, and fail closed if queue enqueue cannot be completed.
- Successful operator availability reads, reservation list reads, and owned reservation reads now generate audit rows without copying guest PII.
- Operator audit rows now retain useful snapshots across operator/restaurant deletion via nullable FKs plus additive snapshot columns, and fresh-install + upgrade migration paths are covered by tests.
- Verification used the actual containerized .NET 10 preview SDK commands that passed in this workspace: targeted regression suite `66/66`, full backend suite `1203/1203`.

## Residual Limitation
- The queue remains bounded with `DropOldest`, so older background notifications can still be displaced under sustained pressure. The reviewed fix closes the escalation correctness gap by persisting intent first and surfacing enqueue failure instead of silently reporting success.

## 2026-07-27 Credential Management Slice Review
- Kept the change migration-free because the existing `OperatorPrincipal`, `OperatorRestaurantScope`, and `OperatorAgentCredential` tables already cover issuance, scoping, revocation, notes, and `LastUsedAt`.
- Isolated the admin management logic in a dedicated `OperatorCredentialManagementService` and `AdminOperatorCredentialsController` so the existing operator bearer validation path remained the single enforcement point for revocation and scope checks.
- The only collateral code adjustment outside the new slice was a small `UsersRolesCard` `useEffect` fix so the requested frontend typecheck could pass under the real install.
- Repo-wide frontend `npm run check` still fails because of pre-existing Prettier drift in five untouched files. Slice-local formatting/lint/typecheck/Jest checks passed.

## 2026-07-27 Credential Expiration Presets Slice Review
- Replaced the numeric admin TTL contract with a server-owned preset catalog so both issue validation and auth semantics are driven by the same allowlist instead of trusting frontend input.
- Stored finite expirations with UTC calendar semantics (`AddMonths` / `AddYears`) and modeled `never` as `NULL` expiry plus a durable `ExpirationPresetSnapshot`, avoiding fake sentinel timestamps or fake TTL hours in audit trails.
- Preserved legacy compatibility by keeping `TtlHoursSnapshot` nullable for historical consumers while making `ExpirationPresetSnapshot` authoritative for new longer-lived presets and no-expiry credentials.
- Kept the frontend secret boundary unchanged: the one-time token acknowledgement modal remains the only plaintext exposure, while the settings form now presents fixed es-CO chips and a high-risk warning for `No expira`.
