using System.Text.Json;
using Xunit;

namespace OpenRestoApi.Tests.Infrastructure;

public sealed class N8nPhase4WorkflowContractTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    private static readonly string WorkflowDirectory = Path.Combine(RepoRoot, "n8n", "test", "workflows");
    private static readonly string ContractDocumentPath = Path.Combine(RepoRoot, "n8n", "test", "docs", "contract.md");

    [Fact]
    public void Phase4WorkflowExports_AllExist_AndParseAsJson()
    {
        foreach (string fileName in ExpectedWorkflowFiles())
        {
            string path = Path.Combine(WorkflowDirectory, fileName);
            Assert.True(File.Exists(path), $"Missing workflow export: {fileName}");

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("name", out JsonElement workflowName));
            Assert.Contains("v1", workflowName.GetString(), StringComparison.Ordinal);
            Assert.True(root.TryGetProperty("versionId", out JsonElement versionId));
            Assert.False(string.IsNullOrWhiteSpace(versionId.GetString()));
            Assert.True(root.TryGetProperty("meta", out JsonElement meta));
            Assert.Equal("phase-4-v1", meta.GetProperty("openrestoWorkflowVersion").GetString());
            Assert.True(root.TryGetProperty("nodes", out JsonElement nodes));
            Assert.NotEmpty(nodes.EnumerateArray());
        }
    }

    [Fact]
    public void InboundRouter_EnforcesHmacBeforeParse_AndUsesOnlyBotContract()
    {
        using JsonDocument document = LoadWorkflow("whatsapp-inbound-router.json");
        JsonElement root = document.RootElement;
        JsonElement nodes = root.GetProperty("nodes");

        Assert.Contains(nodes.EnumerateArray(), node => NodeName(node) == "Verify Meta HMAC Before Parse");
        Assert.Contains(nodes.EnumerateArray(), node => NodeName(node) == "Parse Meta Envelope");
        Assert.Contains(nodes.EnumerateArray(), node => NodeName(node) == "Issue Assertion After Verified Sender");
        Assert.Contains(nodes.EnumerateArray(), node => NodeName(node) == "LLM Provider Abstraction");

        JsonElement botNode = nodes.EnumerateArray().Single(node => NodeName(node) == "Call Reservation Bot");
        string botUrl = botNode.GetProperty("parameters").GetProperty("url").GetString() ?? string.Empty;
        Assert.Equal("http://reservation-bot-test:8080/api/internal/reservation-bot/operations", botUrl);

        string workflowJson = File.ReadAllText(Path.Combine(WorkflowDirectory, "whatsapp-inbound-router.json"));
        Assert.DoesNotContain("/api/private/channels/whatsapp", workflowJson, StringComparison.Ordinal);
        Assert.DoesNotContain("test-rest.joypaw.tech", workflowJson, StringComparison.Ordinal);
        Assert.DoesNotContain("n8n-test.joypaw.tech", workflowJson, StringComparison.Ordinal);
        Assert.DoesNotContain("reservation-bot-test.joypaw.tech", workflowJson, StringComparison.Ordinal);
        Assert.Contains("tools: []", workflowJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Workflows_UsePlaceholderCredentials_AndContainNoObviousRealSecrets()
    {
        foreach (string fileName in ExpectedWorkflowFiles())
        {
            string text = File.ReadAllText(Path.Combine(WorkflowDirectory, fileName));
            Assert.DoesNotContain("sk-", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EAAG", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("joypaw.tech", text, StringComparison.OrdinalIgnoreCase);
        }

        string inbound = File.ReadAllText(Path.Combine(WorkflowDirectory, "whatsapp-inbound-router.json"));
        Assert.Contains("Reservation Bot Internal Credential (placeholder)", inbound, StringComparison.Ordinal);
        Assert.Contains("LLM Provider API Key (placeholder)", inbound, StringComparison.Ordinal);

        string templates = File.ReadAllText(Path.Combine(WorkflowDirectory, "whatsapp-template-messages.json"));
        Assert.Contains("Meta Access Token (placeholder)", templates, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmationAndObservability_WorkflowsCaptureRequiredPhase4Guards()
    {
        string confirmation = File.ReadAllText(Path.Combine(WorkflowDirectory, "whatsapp-confirmation-state-machine.json"));
        Assert.Contains("awaiting-confirmation", confirmation, StringComparison.Ordinal);
        Assert.Contains("Vas a reservar", confirmation, StringComparison.Ordinal);
        Assert.Contains("/data/channel-state/sessions", confirmation, StringComparison.Ordinal);

        string observability = File.ReadAllText(Path.Combine(WorkflowDirectory, "whatsapp-observability.json"));
        Assert.Contains("PII Redaction", observability, StringComparison.Ordinal);
        Assert.Contains("/data/channel-state/replay", observability, StringComparison.Ordinal);
        Assert.Contains("/data/channel-state/outbound", observability, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomerEmail", observability, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractDocument_RecordsSecurityInvariants_AndDualKeyFlow()
    {
        Assert.True(File.Exists(ContractDocumentPath), "Missing Phase 4 workflow contract document.");

        string text = File.ReadAllText(ContractDocumentPath);
        Assert.Contains("`n8n` valida Meta y es el único emisor de assertions", text, StringComparison.Ordinal);
        Assert.Contains("Ningún workflow llama de forma directa a `/api/private/channels/whatsapp/*`", text, StringComparison.Ordinal);
        Assert.Contains("LLM Provider API Key (placeholder)", text, StringComparison.Ordinal);
        Assert.Contains("OpenResto carga la nueva clave de verificación.", text, StringComparison.Ordinal);
        Assert.Contains("n8n` cambia `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID`", text, StringComparison.Ordinal);
        Assert.Contains("La Fase 6 define el edge público de webhook de prueba", text, StringComparison.Ordinal);
        Assert.Contains("el bot y la API privada de OpenResto permanecen privados", text, StringComparison.Ordinal);
    }

    private static JsonDocument LoadWorkflow(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(WorkflowDirectory, fileName)));

    private static IEnumerable<string> ExpectedWorkflowFiles()
    {
        yield return "whatsapp-meta-verification.json";
        yield return "whatsapp-inbound-router.json";
        yield return "whatsapp-confirmation-state-machine.json";
        yield return "whatsapp-handoff.json";
        yield return "whatsapp-observability.json";
        yield return "whatsapp-template-messages.json";
    }

    private static string NodeName(JsonElement node)
        => node.GetProperty("name").GetString() ?? string.Empty;
}
