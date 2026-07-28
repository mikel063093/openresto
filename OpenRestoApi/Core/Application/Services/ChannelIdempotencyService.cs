using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

// The replay/conflict decision is kept separate from the repository's current
// SaveChanges-based persistence so a future channel reservation flow can reuse
// the same idempotency semantics inside one larger transaction.
public sealed class ChannelIdempotencyService(IChannelMutationIdempotencyRepository repository)
{
    private readonly IChannelMutationIdempotencyRepository _repository = repository;

    public async Task<ChannelMutationRegistrationResult> RegisterMutationAsync(
        string channel,
        string mutationScope,
        string idempotencyKey,
        string fingerprint)
    {
        ChannelMutationIdempotencyRecord? existing = await _repository.FindAsync(channel, mutationScope, idempotencyKey);
        if (existing != null)
        {
            return ClassifyExisting(existing, fingerprint);
        }

        var record = new ChannelMutationIdempotencyRecord
        {
            Channel = channel,
            MutationScope = mutationScope,
            IdempotencyKey = idempotencyKey,
            Fingerprint = fingerprint,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _repository.AddAsync(record);
            return new ChannelMutationRegistrationResult(ChannelMutationRegistrationOutcome.Registered, record);
        }
        catch (DbUpdateException)
        {
            ChannelMutationIdempotencyRecord? collided = await _repository.FindAsync(channel, mutationScope, idempotencyKey);
            if (collided != null)
            {
                return ClassifyExisting(collided, fingerprint);
            }

            throw;
        }
    }

    private static ChannelMutationRegistrationResult ClassifyExisting(
        ChannelMutationIdempotencyRecord existing,
        string fingerprint)
    {
        if (existing.Fingerprint == fingerprint)
        {
            return new ChannelMutationRegistrationResult(ChannelMutationRegistrationOutcome.Replayed, existing);
        }

        throw new ConflictException("The idempotency key was already used for a different mutation fingerprint.");
    }
}

public enum ChannelMutationRegistrationOutcome
{
    Registered = 1,
    Replayed = 2
}

public sealed record ChannelMutationRegistrationResult(
    ChannelMutationRegistrationOutcome Outcome,
    ChannelMutationIdempotencyRecord Record);
