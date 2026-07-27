# Internal Operator MCP Self Review

## Checked
- Real MCP transport is implemented with `ModelContextProtocol.AspNetCore` and mounted at `/api/mcp/operator`.
- Operator tools are protocol-discoverable and invoke the operator-scoped services.
- Ownership, scope, malformed/missing credential, inactive/revoked/expired credential, escalation persistence, and notification queue compatibility are covered by tests.
- Fresh-install and upgrade migration paths are covered by dedicated migration tests.
- Live smoke used a running API process plus a real issued operator credential, not only the ASP.NET integration host.

## Residual Limitation
- The MCP route itself is not rate-limited in the `Testing` environment because the official client transport emits extra initialization traffic that makes the shared integration host nondeterministic. Production and non-testing environments still enforce the `operatorMcp` limiter, and the credential-partition behavior is verified through the operator HTTP route using the same limiter policy.
