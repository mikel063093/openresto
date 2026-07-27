namespace OpenRestoApi.Core.Domain;

public class OperatorAgentCredential
{
    public int Id { get; set; }
    public int OperatorPrincipalId { get; set; }
    public OperatorPrincipal OperatorPrincipal { get; set; } = null!;
    public string CredentialKeyId { get; set; } = string.Empty;
    public string TokenDigest { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? Notes { get; set; }
    public List<OperatorAgentCredentialScope> RestaurantScopes { get; set; } = [];
}
