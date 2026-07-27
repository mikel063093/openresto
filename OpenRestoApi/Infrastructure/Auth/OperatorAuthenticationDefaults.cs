namespace OpenRestoApi.Infrastructure.Auth;

public static class OperatorAuthenticationDefaults
{
    public const string SchemeName = "OperatorBearer";
    public const string OperatorIdClaim = "operator_id";
    public const string OperatorIdentifierClaim = "operator_identifier";
    public const string CredentialIdClaim = "operator_credential_id";
    public const string RestaurantIdClaim = "operator_restaurant_id";
}
