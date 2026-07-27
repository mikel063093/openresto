namespace OpenRestoApi.Infrastructure.OpenApi;

internal static class OperatorMcpDocumentation
{
    public const string GuidePath = "/api-reference/operator-mcp";

    public static string OpenApiDescription =>
        """
        Protected OpenResto operator MCP guide: [/api-reference/operator-mcp](/api-reference/operator-mcp)

        The operator MCP integration uses Streamable HTTP / JSON-RPC on `POST /api/mcp/operator`. It is not an OpenAPI REST endpoint and must not be exercised through REST "Try it" flows as if it were a normal controller action. Use an MCP client that can send a Bearer header to the MCP endpoint instead.
        """;

    public static string HtmlPage =>
"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>OpenResto Operator MCP Guide</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f6f4ee;
      --panel: #fffdf8;
      --ink: #1f1f1c;
      --muted: #5e5b52;
      --line: #ded8cb;
      --accent: #8a3b12;
      --accent-soft: #f4e2d6;
      --code: #f1ede3;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif;
      background: radial-gradient(circle at top, #fffaf0 0%, var(--bg) 55%, #ece7db 100%);
      color: var(--ink);
      line-height: 1.55;
    }
    main {
      max-width: 980px;
      margin: 0 auto;
      padding: 32px 20px 56px;
    }
    section, header {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 18px;
      padding: 24px;
      box-shadow: 0 10px 30px rgba(57, 40, 24, 0.06);
      margin-bottom: 18px;
    }
    h1, h2, h3 { line-height: 1.2; margin: 0 0 12px; }
    h1 { font-size: 2rem; }
    h2 { font-size: 1.25rem; }
    p, li { color: var(--ink); }
    .lead {
      font-size: 1.05rem;
      color: var(--muted);
      margin-top: 0;
    }
    .callout {
      border-left: 5px solid var(--accent);
      background: var(--accent-soft);
      padding: 14px 16px;
      border-radius: 12px;
      margin: 14px 0 0;
    }
    .muted { color: var(--muted); }
    code, pre {
      font-family: "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
      font-size: 0.95rem;
    }
    code {
      background: var(--code);
      padding: 0.12rem 0.35rem;
      border-radius: 6px;
    }
    pre {
      background: #171512;
      color: #f8f3e8;
      border-radius: 14px;
      padding: 16px;
      overflow-x: auto;
      white-space: pre-wrap;
      word-break: break-word;
    }
    ul {
      padding-left: 1.2rem;
      margin: 0.4rem 0 0;
    }
    .grid {
      display: grid;
      gap: 16px;
    }
    @media (min-width: 860px) {
      .grid.two {
        grid-template-columns: 1fr 1fr;
      }
    }
    a { color: var(--accent); }
  </style>
</head>
<body>
<main>
  <header>
    <p class="muted">Protected reference for SuperAdmin-managed internal integrations.</p>
    <h1>OpenResto Operator MCP Guide</h1>
    <p class="lead">
      This integration is MCP over Streamable HTTP / JSON-RPC. It is not a REST OpenAPI operation.
    </p>
    <div class="callout">
      Use <code>POST /api/mcp/operator</code> only from an MCP client that can speak Streamable HTTP and send
      <code>Authorization: Bearer ...</code>. Do not use Scalar REST "Try it" against this endpoint as if it were a normal JSON REST controller.
    </div>
  </header>

  <section>
    <h2>Credential Issuance And Handling</h2>
    <ul>
      <li>Issue credentials in Settings as a SuperAdmin through the protected operator credential management flow.</li>
      <li>The plaintext token is displayed once, only at issuance time. Store it in your client secret manager immediately.</li>
      <li>Scope every credential to the specific restaurants it needs. A credential must have at least one restaurant scope.</li>
      <li>Default lifetime is 8 hours. Maximum lifetime is 24 hours.</li>
      <li>Revoke the credential immediately if it is lost, copied into the wrong system, or no longer required.</li>
      <li>Never paste or document a live bearer token. In examples, use only <code>OPENRESTO_MCP_TOKEN</code> and <code>[REDACTED]</code>.</li>
    </ul>
  </section>

  <section>
    <h2>Connection Model</h2>
    <ul>
      <li>Transport: Streamable HTTP</li>
      <li>Protocol: JSON-RPC carried over MCP HTTP transport</li>
      <li>Endpoint: <code>POST /api/mcp/operator</code></li>
      <li>Auth: <code>Authorization: Bearer [REDACTED]</code>, with the redacted value sourced from <code>OPENRESTO_MCP_TOKEN</code></li>
      <li>Client syntax can change across releases. Follow your installed client documentation, but preserve the same URL and Bearer-header principle.</li>
    </ul>
  </section>

  <section>
    <h2>Version-Tolerant Client Patterns</h2>
    <p class="muted">The snippets below are patterns, not pinned vendor syntax. Adapt field names to the client version you actually have installed.</p>

    <h3>Claude Desktop / Claude Code Style Remote HTTP MCP</h3>
    <pre>{
  "name": "openresto-operator",
  "transport": "streamable-http",
  "url": "https://YOUR_OPENRESTO_HOST/api/mcp/operator",
  "headers": {
    "Authorization": "Bearer [REDACTED]"
  },
  "secretSource": {
    "env": "OPENRESTO_MCP_TOKEN"
  }
}</pre>

    <h3>Codex MCP Config Pattern</h3>
    <pre>{
  "label": "openresto-operator",
  "type": "mcp",
  "transport": {
    "kind": "streamable-http",
    "url": "https://YOUR_OPENRESTO_HOST/api/mcp/operator",
    "headers": {
      "Authorization": "Bearer [REDACTED]"
    }
  },
  "env": {
    "OPENRESTO_MCP_TOKEN": "[REDACTED]"
  }
}</pre>

    <h3>Generic Streamable HTTP MCP Client</h3>
    <pre>{
  "transport": {
    "protocol": "mcp",
    "mode": "streamable-http",
    "endpoint": "https://YOUR_OPENRESTO_HOST/api/mcp/operator"
  },
  "requestHeaders": {
    "Authorization": "Bearer [REDACTED]"
  },
  "credentials": {
    "envVar": "OPENRESTO_MCP_TOKEN"
  }
}</pre>
  </section>

  <section>
    <h2>Available Tools</h2>
    <ul>
      <li><code>operator_get_availability</code>: availability lookup within the authenticated credential's restaurant scope.</li>
      <li><code>operator_create_reservation</code>: creates a reservation owned by the authenticated operator.</li>
      <li><code>operator_list_reservations</code>: lists reservations owned by the authenticated operator.</li>
      <li><code>operator_get_reservation</code>: fetches one reservation owned by the authenticated operator.</li>
      <li><code>operator_update_reservation</code>: updates one reservation owned by the authenticated operator.</li>
      <li><code>operator_cancel_reservation</code>: cancels one reservation owned by the authenticated operator.</li>
      <li><code>operator_escalate_reservation</code>: records an escalation audit and notifies the internal admin channel.</li>
    </ul>
  </section>

  <section class="grid two">
    <div>
      <h2>Scope And Ownership Boundaries</h2>
      <ul>
        <li>Restaurant scope is credential-specific, not just operator-specific. Two credentials for the same operator can have different restaurant access.</li>
        <li>Out-of-scope restaurant requests return <code>404</code> to avoid resource enumeration.</li>
        <li>MCP-created reservations are durably stamped as operator-owned.</li>
        <li>Read, update, cancel, and escalate operations are limited to reservations owned by the same operator credential identity.</li>
        <li>Reservation ownership remains enforced after creation; the tool set is intentionally not a cross-restaurant admin surface.</li>
      </ul>
    </div>
    <div>
      <h2>Auth, Rate Limits, And Troubleshooting</h2>
      <ul>
        <li><code>401 Unauthorized</code>: missing bearer token, malformed token, expired token, or revoked token.</li>
        <li><code>404 Not Found</code>: restaurant outside the credential scope, reservation not owned by the operator, or resource does not exist.</li>
        <li><code>429 Too Many Requests</code>: production MCP traffic is rate-limited per credential, not only per IP.</li>
        <li>If a previously working integration starts returning <code>401</code>, verify whether the token expired, was revoked, or was copied incorrectly.</li>
        <li>If only some restaurants fail with <code>404</code>, verify the credential was issued with those restaurant IDs selected.</li>
      </ul>
    </div>
  </section>

  <section>
    <h2>Operational Notes</h2>
    <ul>
      <li>OpenAPI examples and this guide are intentionally secret-free. They never emit a live token.</li>
      <li>Use the protected Settings surface to issue or revoke credentials; do not create long-lived shared tokens for multiple unrelated operators.</li>
      <li>If you rotate a credential, update the client secret store and remove the old token immediately.</li>
    </ul>
    <p class="muted">Protected Scalar reference: <a href="/api-reference">/api-reference</a></p>
  </section>
</main>
</body>
</html>
""";
}
