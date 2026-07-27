namespace OpenRestoApi.Core.Domain;

public class OperatorPrincipal
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string NormalizedIdentifier { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OperatorRestaurantScope> RestaurantScopes { get; set; } = [];
    public List<OperatorAgentCredential> Credentials { get; set; } = [];
}
