namespace OpenRestoApi.Infrastructure.Auth;

public static class WhatsAppChannelAuthenticationDefaults
{
    public const string SchemeName = "WhatsAppChannelBearer";
    public const string AssertionHeader = "X-OpenResto-Channel-Assertion";
    public const string VerifiedPhoneClaim = "openresto:whatsapp:verified_phone";
    public const string VerifiedPhoneNormalizedClaim = "openresto:whatsapp:verified_phone_normalized";
    public const string ActionClaim = "openresto:channel_action";
    public const string ScopeClaim = "scope";
}
