namespace OpenRestoApi.Core.Domain;

public class OperatorRestaurantScope
{
    public int Id { get; set; }
    public int OperatorPrincipalId { get; set; }
    public OperatorPrincipal OperatorPrincipal { get; set; } = null!;
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
