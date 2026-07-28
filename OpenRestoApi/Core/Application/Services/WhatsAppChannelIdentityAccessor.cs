using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpenRestoApi.Infrastructure.Auth;

namespace OpenRestoApi.Core.Application.Services;

public sealed class WhatsAppChannelIdentityAccessor(IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public WhatsAppChannelIdentityContext GetCurrent()
    {
        ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No current HTTP user is available.");

        string? verifiedPhone = user.FindFirstValue(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneClaim);
        string? normalizedPhone = user.FindFirstValue(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneNormalizedClaim);
        if (string.IsNullOrWhiteSpace(verifiedPhone) || string.IsNullOrWhiteSpace(normalizedPhone))
        {
            throw new InvalidOperationException("WhatsApp channel claims are missing or invalid.");
        }

        return new WhatsAppChannelIdentityContext(
            verifiedPhone,
            normalizedPhone,
            _httpContextAccessor.HttpContext?.TraceIdentifier);
    }
}

public sealed record WhatsAppChannelIdentityContext(
    string VerifiedPhoneE164,
    string VerifiedPhoneNormalized,
    string? CorrelationId);
