namespace OpenRestoApi.Tests.Infrastructure;

public sealed class NginxPrivateChannelExposureTests
{
    [Theory]
    [InlineData("nginx/default.conf.template")]
    [InlineData("nginx-vps/default.conf.template")]
    public void PublicNginxConfigs_DenyWhatsAppPrivateChannelRoute(string relativePath)
    {
        string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string filePath = Path.Combine(repoRoot, relativePath);
        string text = File.ReadAllText(filePath);

        Assert.Contains("location ^~ /api/private/channels/whatsapp/", text, StringComparison.Ordinal);
        Assert.Contains("return 404;", text, StringComparison.Ordinal);
    }
}
