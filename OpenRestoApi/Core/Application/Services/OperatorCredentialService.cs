using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OperatorCredentialService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<IssuedOperatorCredential> IssueAsync(
        int operatorPrincipalId,
        TimeSpan ttl,
        string? notes = null,
        DateTime? nowUtc = null)
    {
        DateTime issuedAt = nowUtc ?? DateTime.UtcNow;
        OperatorPrincipal op = await _db.OperatorPrincipals.SingleAsync(x => x.Id == operatorPrincipalId);

        string keyId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
        string secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        string token = $"ormcp.{keyId}.{secret}";

        var credential = new OperatorAgentCredential
        {
            OperatorPrincipalId = op.Id,
            CredentialKeyId = keyId,
            TokenDigest = ComputeDigest(secret),
            IssuedAt = issuedAt,
            ExpiresAt = issuedAt.Add(ttl),
            Notes = notes,
        };

        _db.OperatorAgentCredentials.Add(credential);
        await _db.SaveChangesAsync();

        return new IssuedOperatorCredential(credential.Id, keyId, token, credential.ExpiresAt);
    }

    public async Task<OperatorCredentialValidationResult?> ValidateAsync(string token, DateTime? nowUtc = null)
    {
        if (!TryParseToken(token, out string? keyId, out string? secret))
        {
            return null;
        }

        OperatorAgentCredential? credential = await _db.OperatorAgentCredentials
            .Include(x => x.OperatorPrincipal)
            .ThenInclude(x => x.RestaurantScopes)
            .SingleOrDefaultAsync(x => x.CredentialKeyId == keyId);

        if (credential is null)
        {
            return null;
        }

        if (!DigestMatches(credential.TokenDigest, secret!))
        {
            return null;
        }

        DateTime now = nowUtc ?? DateTime.UtcNow;
        if (credential.RevokedAt.HasValue || credential.ExpiresAt <= now || !credential.OperatorPrincipal.IsActive)
        {
            return null;
        }

        int[] restaurantIds = credential.OperatorPrincipal.RestaurantScopes
            .Select(x => x.RestaurantId)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (restaurantIds.Length == 0)
        {
            return null;
        }

        credential.LastUsedAt = now;
        await _db.SaveChangesAsync();

        return new OperatorCredentialValidationResult(credential.OperatorPrincipal, credential, restaurantIds);
    }

    private static bool TryParseToken(string token, out string? keyId, out string? secret)
    {
        keyId = null;
        secret = null;

        string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !string.Equals(parts[0], "ormcp", StringComparison.Ordinal))
        {
            return false;
        }

        keyId = parts[1];
        secret = parts[2];
        return !string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(secret);
    }

    private static string ComputeDigest(string secret)
    {
        byte[] bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool DigestMatches(string persistedDigest, string secret)
    {
        byte[] left = Convert.FromHexString(persistedDigest);
        byte[] right = Convert.FromHexString(ComputeDigest(secret));
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}

public sealed record IssuedOperatorCredential(
    int CredentialId,
    string CredentialKeyId,
    string PlaintextToken,
    DateTime ExpiresAtUtc);

public sealed record OperatorCredentialValidationResult(
    OperatorPrincipal Operator,
    OperatorAgentCredential Credential,
    IReadOnlyList<int> RestaurantIds);
