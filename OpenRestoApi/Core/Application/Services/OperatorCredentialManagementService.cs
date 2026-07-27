using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OperatorCredentialManagementService(
    AppDbContext db,
    OperatorCredentialService credentialService,
    IAdminActorAccessor adminActorAccessor)
{
    private const int DefaultTtlHours = 8;
    private const int MaxTtlHours = 24;
    private const int MaxIdentifierLength = 200;
    private const int MaxNotesLength = 200;

    private readonly AppDbContext _db = db;
    private readonly OperatorCredentialService _credentialService = credentialService;
    private readonly IAdminActorAccessor _adminActorAccessor = adminActorAccessor;

    public async Task<IssueOperatorCredentialResponseDto> IssueAsync(
        IssueOperatorCredentialRequestDto request,
        DateTime? nowUtc = null)
    {
        DateTime now = nowUtc ?? DateTime.UtcNow;
        AdminActorSnapshot actor = await _adminActorAccessor.GetRequiredSnapshotAsync();
        string normalizedIdentifier = NormalizeIdentifier(request.Identifier);
        string? notes = NormalizeNotes(request.Notes);
        int ttlHours = request.TtlHours ?? DefaultTtlHours;
        ValidateRequest(normalizedIdentifier, request.RestaurantIds, ttlHours, notes);

        int[] restaurantIds = request.RestaurantIds
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        List<Restaurant> restaurants = await _db.Restaurants
            .Where(x => restaurantIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync();

        if (restaurants.Count != restaurantIds.Length)
        {
            throw new ValidationException("Selecciona al menos un restaurante válido.");
        }

        OperatorPrincipal principal = await _db.OperatorPrincipals
            .Include(x => x.RestaurantScopes)
            .ThenInclude(x => x.Restaurant)
            .SingleOrDefaultAsync(x => x.NormalizedIdentifier == normalizedIdentifier)
            ?? new OperatorPrincipal
            {
                Identifier = normalizedIdentifier,
                NormalizedIdentifier = normalizedIdentifier,
                CreatedAt = now,
                IsActive = true,
            };

        if (principal.Id == 0)
        {
            _db.OperatorPrincipals.Add(principal);
        }

        principal.Identifier = normalizedIdentifier;
        principal.NormalizedIdentifier = normalizedIdentifier;
        principal.IsActive = true;
        principal.UpdatedAt = now;

        HashSet<int> existingScopeIds = principal.RestaurantScopes
            .Select(x => x.RestaurantId)
            .ToHashSet();

        foreach (Restaurant restaurant in restaurants.Where(x => !existingScopeIds.Contains(x.Id)))
        {
            principal.RestaurantScopes.Add(new OperatorRestaurantScope
            {
                RestaurantId = restaurant.Id,
                Restaurant = restaurant,
                CreatedAt = now,
            });
        }

        return await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;

            await _db.SaveChangesAsync();

            IssuedOperatorCredential issued = await _credentialService.IssueAsync(
                principal.Id,
                TimeSpan.FromHours(ttlHours),
                notes,
                now);

            OperatorAgentCredential credential = await _db.OperatorAgentCredentials
                .Include(x => x.OperatorPrincipal)
                .ThenInclude(x => x.RestaurantScopes)
                .ThenInclude(x => x.Restaurant)
                .SingleAsync(x => x.Id == issued.CredentialId);

            _db.AdminCredentialManagementAudits.Add(CreateAudit(
                actor,
                credential,
                credential.OperatorPrincipal.NormalizedIdentifier,
                "ISSUE",
                now));
            await _db.SaveChangesAsync();

            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }

            return ToIssueDto(credential, issued.PlaintextToken);
        });
    }

    public async Task<IReadOnlyList<OperatorCredentialListItemDto>> ListAsync()
    {
        List<OperatorAgentCredential> credentials = await _db.OperatorAgentCredentials
            .AsNoTracking()
            .Include(x => x.OperatorPrincipal)
            .ThenInclude(x => x.RestaurantScopes)
            .ThenInclude(x => x.Restaurant)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync();

        return credentials.Select(ToListDto).ToList();
    }

    public async Task RevokeAsync(int credentialId, DateTime? nowUtc = null)
    {
        DateTime now = nowUtc ?? DateTime.UtcNow;
        AdminActorSnapshot actor = await _adminActorAccessor.GetRequiredSnapshotAsync();
        OperatorAgentCredential credential = await _db.OperatorAgentCredentials
            .Include(x => x.OperatorPrincipal)
            .ThenInclude(x => x.RestaurantScopes)
            .SingleOrDefaultAsync(x => x.Id == credentialId)
            ?? throw new NotFoundException("Credential not found.");

        if (!credential.RevokedAt.HasValue)
        {
            await _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = _db.Database.IsRelational()
                    ? await _db.Database.BeginTransactionAsync()
                    : null;

                credential.RevokedAt = now;
                _db.AdminCredentialManagementAudits.Add(CreateAudit(
                    actor,
                    credential,
                    credential.OperatorPrincipal.NormalizedIdentifier,
                    "REVOKE",
                    now));
                await _db.SaveChangesAsync();

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }
            });
        }
    }

    private static AdminCredentialManagementAudit CreateAudit(
        AdminActorSnapshot actor,
        OperatorAgentCredential credential,
        string targetOperatorIdentifier,
        string action,
        DateTime createdAtUtc)
    {
        int ttlHours = (int)Math.Round((credential.ExpiresAt - credential.IssuedAt).TotalHours, MidpointRounding.AwayFromZero);

        return new AdminCredentialManagementAudit
        {
            ActorAdminCredentialId = actor.AdminCredentialId,
            ActorEmailSnapshot = actor.NormalizedEmail,
            TargetOperatorPrincipalId = credential.OperatorPrincipalId,
            OperatorAgentCredentialId = credential.Id,
            CredentialKeyIdSnapshot = credential.CredentialKeyId,
            TargetOperatorIdentifierSnapshot = targetOperatorIdentifier,
            ScopeRestaurantIdsSnapshot = string.Join(",",
                credential.OperatorPrincipal.RestaurantScopes
                    .Select(x => x.RestaurantId)
                    .Distinct()
                    .OrderBy(x => x)),
            TtlHoursSnapshot = ttlHours > 0 ? ttlHours : null,
            Action = action,
            CreatedAtUtc = createdAtUtc,
        };
    }

    private static void ValidateRequest(
        string normalizedIdentifier,
        IReadOnlyCollection<int>? restaurantIds,
        int ttlHours,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(normalizedIdentifier))
        {
            throw new ValidationException("El identificador del operador es obligatorio.");
        }

        if (normalizedIdentifier.Length > MaxIdentifierLength)
        {
            throw new ValidationException($"El identificador no puede exceder {MaxIdentifierLength} caracteres.");
        }

        if (restaurantIds is null || restaurantIds.Count == 0)
        {
            throw new ValidationException("Selecciona al menos un restaurante válido.");
        }

        if (ttlHours <= 0 || ttlHours > MaxTtlHours)
        {
            throw new ValidationException($"La vigencia debe estar entre 1 y {MaxTtlHours} horas.");
        }

        if (notes is not null && notes.Length > MaxNotesLength)
        {
            throw new ValidationException($"Las notas no pueden exceder {MaxNotesLength} caracteres.");
        }
    }

    private static string NormalizeIdentifier(string? identifier) =>
        identifier?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    private static IssueOperatorCredentialResponseDto ToIssueDto(
        OperatorAgentCredential credential,
        string plaintextToken)
    {
        OperatorCredentialListItemDto metadata = ToListDto(credential);

        return new IssueOperatorCredentialResponseDto
        {
            CredentialId = metadata.CredentialId,
            Identifier = metadata.Identifier,
            CredentialKeyId = metadata.CredentialKeyId,
            IssuedAtUtc = metadata.IssuedAtUtc,
            ExpiresAtUtc = metadata.ExpiresAtUtc,
            RevokedAtUtc = metadata.RevokedAtUtc,
            LastUsedAtUtc = metadata.LastUsedAtUtc,
            Notes = metadata.Notes,
            Restaurants = metadata.Restaurants,
            PlaintextToken = plaintextToken,
        };
    }

    private static OperatorCredentialListItemDto ToListDto(OperatorAgentCredential credential)
    {
        return new OperatorCredentialListItemDto
        {
            CredentialId = credential.Id,
            Identifier = credential.OperatorPrincipal.NormalizedIdentifier,
            CredentialKeyId = credential.CredentialKeyId,
            IssuedAtUtc = credential.IssuedAt,
            ExpiresAtUtc = credential.ExpiresAt,
            RevokedAtUtc = credential.RevokedAt,
            LastUsedAtUtc = credential.LastUsedAt,
            Notes = credential.Notes,
            Restaurants = credential.OperatorPrincipal.RestaurantScopes
                .Where(x => x.Restaurant is not null)
                .OrderBy(x => x.Restaurant.Name)
                .Select(x => new OperatorCredentialScopeDto
                {
                    RestaurantId = x.RestaurantId,
                    RestaurantName = x.Restaurant.Name,
                })
                .ToList(),
        };
    }
}
