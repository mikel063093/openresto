namespace OpenRestoApi.Core.Domain;

public class AdminCredentialManagementAudit
{
    public int Id { get; set; }
    public int? ActorAdminCredentialId { get; set; }
    public AdminCredential? ActorAdminCredential { get; set; }
    public string ActorEmailSnapshot { get; set; } = string.Empty;
    public int? TargetOperatorPrincipalId { get; set; }
    public OperatorPrincipal? TargetOperatorPrincipal { get; set; }
    public int? OperatorAgentCredentialId { get; set; }
    public OperatorAgentCredential? OperatorAgentCredential { get; set; }
    public string? CredentialKeyIdSnapshot { get; set; }
    public string TargetOperatorIdentifierSnapshot { get; set; } = string.Empty;
    public string ScopeRestaurantIdsSnapshot { get; set; } = string.Empty;
    public int? TtlHoursSnapshot { get; set; }
    public string? ExpirationPresetSnapshot { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
