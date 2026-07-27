namespace OpenRestoApi.Core.Domain;

public class OperatorActionAudit
{
    public int Id { get; set; }
    public int? OperatorPrincipalId { get; set; }
    public OperatorPrincipal? OperatorPrincipal { get; set; }
    public int OperatorPrincipalIdSnapshot { get; set; }
    public int? OperatorAgentCredentialId { get; set; }
    public OperatorAgentCredential? OperatorAgentCredential { get; set; }
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public int RestaurantIdSnapshot { get; set; }
    public string RestaurantNameSnapshot { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
