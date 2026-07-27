namespace OpenRestoApi.Core.Domain;

public class OperatorAgentCredentialScope
{
    public int Id { get; set; }
    public int OperatorAgentCredentialId { get; set; }
    public OperatorAgentCredential OperatorAgentCredential { get; set; } = null!;
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
