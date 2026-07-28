using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

public sealed class ChannelIdempotencyService(IChannelMutationIdempotencyRepository repository)
{
    private readonly IChannelMutationIdempotencyRepository _repository = repository;

    public async Task<bool> RegisterMutationAsync(
        string channel,
        string mutationScope,
        string idempotencyKey,
        string fingerprint)
    {
        ChannelMutationIdempotencyRecord? existing = await _repository.FindAsync(channel, mutationScope, idempotencyKey);
        if (existing != null)
        {
            return false;
        }

        await _repository.AddAsync(new ChannelMutationIdempotencyRecord
        {
            Channel = channel,
            MutationScope = mutationScope,
            IdempotencyKey = idempotencyKey,
            Fingerprint = fingerprint,
            CreatedAtUtc = DateTime.UtcNow
        });

        return true;
    }
}
