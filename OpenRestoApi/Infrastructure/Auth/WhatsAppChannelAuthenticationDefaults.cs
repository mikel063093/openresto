namespace OpenRestoApi.Infrastructure.Auth;

public static class WhatsAppChannelAuthenticationDefaults
{
    public const string SchemeName = "WhatsAppChannelBearer";
    public const string VerifiedPhoneHeader = "X-WhatsApp-Verified-Phone";
    public const string VerifiedPhoneClaim = "openresto:whatsapp:verified_phone";
    public const string VerifiedPhoneNormalizedClaim = "openresto:whatsapp:verified_phone_normalized";
}
