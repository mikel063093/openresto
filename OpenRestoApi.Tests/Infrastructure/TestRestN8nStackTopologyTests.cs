namespace OpenRestoApi.Tests.Infrastructure;

public sealed class TestRestN8nStackTopologyTests
{
    [Fact]
    public void TestRestCompose_DefinesDurableN8nStateStack()
    {
        string text = ReadRepoFile("docker-compose.test-rest.yml");

        Assert.Contains("reservation-bot-test:", text, StringComparison.Ordinal);
        Assert.Contains("n8n-test-postgres:", text, StringComparison.Ordinal);
        Assert.Contains("n8n-test-volume-init:", text, StringComparison.Ordinal);
        Assert.Contains("n8n-test-workflow-init:", text, StringComparison.Ordinal);
        Assert.Contains("n8n-test:", text, StringComparison.Ordinal);
        Assert.Contains("n8n@${N8N_TEST_IMAGE_DIGEST:?Set N8N_TEST_IMAGE_DIGEST to an audited immutable sha256 digest}", text, StringComparison.Ordinal);
        Assert.DoesNotContain("N8N_TEST_IMAGE_TAG", text, StringComparison.Ordinal);
        Assert.Contains("import:workflow", text, StringComparison.Ordinal);
        Assert.Contains("--input=/workflows", text, StringComparison.Ordinal);
        Assert.Contains("n8n list:workflow", text, StringComparison.Ordinal);
        Assert.Contains("OpenResto WhatsApp Meta Verification v1", text, StringComparison.Ordinal);
        Assert.Contains("OpenResto WhatsApp Inbound Router v1", text, StringComparison.Ordinal);
        Assert.Contains("update:workflow --id=\"$$workflow_id\" --active=true", text, StringComparison.Ordinal);
        Assert.Contains("DB_TYPE: postgresdb", text, StringComparison.Ordinal);
        Assert.Contains("N8N_DEFAULT_BINARY_DATA_MODE: filesystem", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_sessions_data:/data/channel-state/sessions", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_dedupe_data:/data/channel-state/dedupe", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_ordering_data:/data/channel-state/ordering", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_replay_data:/data/channel-state/replay", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_outbound_data:/data/channel-state/outbound", text, StringComparison.Ordinal);
        Assert.Contains("WEBHOOK_URL: ${N8N_TEST_WEBHOOK_BASE_URL}", text, StringComparison.Ordinal);
        Assert.Contains("WhatsAppChannel__InternalCallerCredential: ${WhatsAppChannel__InternalCallerCredential}", text, StringComparison.Ordinal);
        Assert.Contains("WhatsAppChannel__Assertion__SigningKey: ${N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRestCompose_SegmentsNetworksSoN8nCannotReachBackendDirectly_WhileKeepingBotPrivate()
    {
        string text = ReadRepoFile("docker-compose.test-rest.yml");
        string botBlock = ExtractBlock(text, "  reservation-bot-test:", "  n8n-test-postgres:");
        string n8nBlock = ExtractBlock(text, "  n8n-test:", string.Empty);
        string backendBlock = ExtractBlock(text, "  backend:", "  frontend:");

        Assert.Contains("test-rest-app-internal", botBlock, StringComparison.Ordinal);
        Assert.Contains("test-rest-bot-internal", botBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("dokploy-network", botBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest-egress", botBlock, StringComparison.Ordinal);

        Assert.Contains("test-rest-app-internal", backendBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest-bot-internal", backendBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest-egress", backendBlock, StringComparison.Ordinal);

        Assert.Contains("test-rest-bot-internal", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest-app-internal", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("test-rest-egress", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("dokploy-network", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("N8N_EDITOR_BASE_URL", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("WEBHOOK_URL: ${N8N_TEST_WEBHOOK_BASE_URL}", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_APP_SECRET: ${N8N_TEST_META_APP_SECRET}", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_OPENAI_API_KEY: ${N8N_TEST_OPENAI_API_KEY}", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY: ${N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY}", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_BOT_INTERNAL_CREDENTIAL: ${N8N_TEST_BOT_INTERNAL_CREDENTIAL}", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("N8N_HOST: n8n-test.joypaw.tech", n8nBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRestTraefik_ExposesOnlyN8nWebhookPaths_AndKeepsBotPrivate()
    {
        string text = ReadRepoFile("traefik/test-rest.yml");

        Assert.Contains("Host(`test-rest.joypaw.tech`)", text, StringComparison.Ordinal);
        Assert.Contains("Host(`n8n-test.joypaw.tech`)", text, StringComparison.Ordinal);
        Assert.Contains("Path(`/webhook`)", text, StringComparison.Ordinal);
        Assert.Contains("PathPrefix(`/webhook/`)", text, StringComparison.Ordinal);
        Assert.Contains("Path(`/webhook-test`)", text, StringComparison.Ordinal);
        Assert.Contains("PathPrefix(`/webhook-test/`)", text, StringComparison.Ordinal);
        Assert.Contains("http://n8n-test:5678", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PathPrefix(`/rest`)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("metrics", text, StringComparison.Ordinal);
        Assert.DoesNotContain("reservation-bot-test.joypaw.tech", text, StringComparison.Ordinal);
        Assert.DoesNotContain("http://reservation-bot-test:8080", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvExample_AndPhase6Docs_RecordWebhookPlaceholder_AndOwnershipBoundaries()
    {
        string envText = ReadRepoFile(".env.example");
        string docText = ReadRepoFile("n8n/test/docs/phase6-topology.md");

        Assert.Contains("N8N_TEST_WEBHOOK_BASE_URL=", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_IMAGE_DIGEST=", envText, StringComparison.Ordinal);
        Assert.DoesNotContain("N8N_TEST_IMAGE_TAG", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_VERIFY_TOKEN=", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_APP_SECRET=", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_ACCESS_TOKEN=", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_OPENAI_API_KEY=", envText, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY=", envText, StringComparison.Ordinal);
        Assert.Contains("no en el bot", envText, StringComparison.Ordinal);

        Assert.Contains("Rutas públicas permitidas", docText, StringComparison.Ordinal);
        Assert.Contains("https://n8n-test.joypaw.tech/webhook/*", docText, StringComparison.Ordinal);
        Assert.Contains("https://n8n-test.joypaw.tech/", docText, StringComparison.Ordinal);
        Assert.Contains("reservation-bot-test", docText, StringComparison.Ordinal);
        Assert.Contains("/api/private/channels/whatsapp/*", docText, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(string relativePath)
    {
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string filePath = Path.Combine(repoRoot, relativePath);
        return File.ReadAllText(filePath);
    }

    private static string ExtractBlock(string text, string startMarker, string endMarker)
    {
        int startIndex = text.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"No se encontró el bloque '{startMarker}'.");

        int endIndex = string.IsNullOrEmpty(endMarker)
            ? text.Length
            : text.IndexOf(endMarker, startIndex, StringComparison.Ordinal);

        Assert.True(endIndex > startIndex, $"No se encontró el final del bloque '{startMarker}'.");
        return text[startIndex..endIndex];
    }
}
