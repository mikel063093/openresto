# Internal Operator MCP Self Review

## Checked
- Shared `BookingService` now rejects cross-restaurant `TableId` / `SectionId` combinations and mismatched table-section pairs for both create and update paths, protecting operator and non-operator callers from the same tenancy flaw.
- Operator escalations now validate and trim `Reason`, persist a durable `AdminNotification` intent before reporting success, and fail closed if queue enqueue cannot be completed.
- Successful operator availability reads, reservation list reads, and owned reservation reads now generate audit rows without copying guest PII.
- Operator audit rows now retain useful snapshots across operator/restaurant deletion via nullable FKs plus additive snapshot columns, and fresh-install + upgrade migration paths are covered by tests.
- Verification used the actual containerized .NET 10 preview SDK commands that passed in this workspace: targeted regression suite `66/66`, full backend suite `1203/1203`.

## Residual Limitation
- The queue remains bounded with `DropOldest`, so older background notifications can still be displaced under sustained pressure. The reviewed fix closes the escalation correctness gap by persisting intent first and surfacing enqueue failure instead of silently reporting success.
