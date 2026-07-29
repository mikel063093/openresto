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
        Assert.Contains("n8n-test:", text, StringComparison.Ordinal);
        Assert.Contains("DB_TYPE: postgresdb", text, StringComparison.Ordinal);
        Assert.Contains("N8N_DEFAULT_BINARY_DATA_MODE: filesystem", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_sessions_data:/data/channel-state/sessions", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_dedupe_data:/data/channel-state/dedupe", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_ordering_data:/data/channel-state/ordering", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_replay_data:/data/channel-state/replay", text, StringComparison.Ordinal);
        Assert.Contains("n8n_test_outbound_data:/data/channel-state/outbound", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRestCompose_SegmentsNetworksSoN8nCannotReachBackendDirectly()
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

        Assert.Contains("test-rest-bot-internal", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest-app-internal", n8nBlock, StringComparison.Ordinal);
        Assert.Contains("test-rest-egress", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("dokploy-network", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("N8N_EDITOR_BASE_URL", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("WEBHOOK_URL", n8nBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("joypaw.tech", n8nBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRestTraefik_DoesNotExposeN8nOrBotDuringPhase3()
    {
        string text = ReadRepoFile("traefik/test-rest.yml");

        Assert.Contains("Host(`test-rest.joypaw.tech`)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Host(`n8n-test.joypaw.tech`)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("reservation-bot-test.joypaw.tech", text, StringComparison.Ordinal);
        Assert.DoesNotContain("http://n8n-test:5678", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvExample_DocumentsN8nOwnedSecretPlaceholders()
    {
        string text = ReadRepoFile(".env.example");

        Assert.Contains("N8N_TEST_META_VERIFY_TOKEN=", text, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_APP_SECRET=", text, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_META_ACCESS_TOKEN=", text, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_OPENAI_API_KEY=", text, StringComparison.Ordinal);
        Assert.Contains("N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY=", text, StringComparison.Ordinal);
        Assert.Contains("no en el bot", text, StringComparison.Ordinal);
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
